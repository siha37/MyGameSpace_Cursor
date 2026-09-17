using UnityEngine;
using Cylinder.Core;

namespace Cylinder.Enemy
{
    /// <summary>
    /// 허수아비 (MVP 테스트용)
    /// - 지상에서만 P를 좌우 추적
    /// - 어그로 범위 밖이면 대기, 범위 안이면 추적
    /// - 이동 방향 = 전방. 전방 테두리에 노란 스프라이트
    /// - 방향 전환 0.8s 동안 정지·기존 전방 유지. 끝나면 전방(+X/-X)만 스냅
    /// - 전면 악셀은 차단, 후면은 1피격
    /// - 사망 후 리스폰하지 않음
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class Dummy : MonoBehaviour, IDamageable
    {
        [Header("Settings")]
        [SerializeField] private GameObject _visualObject;
        [SerializeField] private Transform _frontMarker;
        
        private static readonly Color FrontMarkerColor = new Color(1f, 0.86f, 0.12f, 1f);
        
        private Rigidbody2D _rb;
        private Collider2D _collider;
        private Transform _player;
        private Transform _visual;
        private bool _isDead;
        private bool _isGrounded;
        private bool _isChasing;
        private int _facing = 1;
        private int _pendingFacing = 1;
        private bool _isTurning;
        private float _turnElapsed;

        /// <summary>전방. 1 우, -1 좌. 이동 방향과 같다.</summary>
        public int FacingDirection => _facing;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            if (_rb == null)
                _rb = gameObject.AddComponent<Rigidbody2D>();
            
            _collider = GetComponent<Collider2D>();
            ConfigurePhysics();
            EnsureFrontMarker();
            CacheVisual();
            FaceDirection(1f);
            IgnorePlayerCollision();
        }

        private void FixedUpdate()
        {
            if (_isDead)
                return;
            
            UpdateGroundCheck();
            UpdateGroundAi();
        }

        /// <summary>
        /// P가 전방에서 마주보고 이 방향으로 악셀하면 차단한다.
        /// </summary>
        public bool IsBlockingFront(Vector2 attackDir, Vector2 attackerPos, int attackerFacing)
        {
            if (_isDead)
                return false;
            
            Vector2 forward = new Vector2(_facing, 0f);
            bool facingEachOther = attackerFacing * _facing < 0;
            bool playerInFront = (attackerPos.x - _rb.position.x) * _facing > 0f;
            bool accelIntoFace = Vector2.Dot(attackDir.normalized, forward) < GameConstants.DUMMY_FRONT_BLOCK_DOT;
            return facingEachOther && playerInFront && accelIntoFace;
        }

        /// <summary>
        /// 피해 받기
        /// </summary>
        public void TakeDamage()
        {
            if (_isDead)
                return;
            
            Die();
        }

        /// <summary>
        /// 사망 처리. 다시 생성하지 않는다.
        /// </summary>
        private void Die()
        {
            _isDead = true;
            _isTurning = false;
            _isChasing = false;
            
            if (_visualObject != null)
                _visualObject.SetActive(false);
            if (_frontMarker != null)
                _frontMarker.gameObject.SetActive(false);
            
            if (_collider != null)
                _collider.enabled = false;
            
            if (_rb != null)
            {
                _rb.linearVelocity = Vector2.zero;
                _rb.simulated = false;
            }
            
            Destroy(gameObject);
        }

        private void ConfigurePhysics()
        {
            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.gravityScale = 1f;
            _rb.freezeRotation = true;
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rb.simulated = true;
            
            PhysicsMaterial2D material = new PhysicsMaterial2D("DummyNoFriction")
            {
                friction = 0f,
                bounciness = 0f
            };
            _rb.sharedMaterial = material;
            if (_collider != null)
                _collider.sharedMaterial = material;
            
            ApplyBodySize();
        }

        private void ApplyBodySize()
        {
            BoxCollider2D box = _collider as BoxCollider2D;
            if (box != null)
            {
                box.size = new Vector2(GameConstants.P_BODY_WIDTH, GameConstants.P_BODY_HEIGHT);
                box.offset = new Vector2(0f, GameConstants.P_BODY_OFFSET_Y);
            }

            CacheVisual();
            if (_visual != null)
            {
                _visual.localScale = new Vector3(GameConstants.P_BODY_WIDTH, GameConstants.P_BODY_HEIGHT, 1f);
                _visual.localPosition = new Vector3(0f, GameConstants.P_BODY_OFFSET_Y, 0f);
                _visual.localRotation = Quaternion.identity;
            }
        }

        private void CacheVisual()
        {
            if (_visualObject != null)
                _visual = _visualObject.transform;
            else
                _visual = transform.Find("Visual");
        }

        private void EnsureFrontMarker()
        {
            if (_frontMarker == null)
            {
                Transform existing = transform.Find("FrontMarker");
                if (existing != null)
                    _frontMarker = existing;
            }
            
            if (_frontMarker == null)
            {
                GameObject marker = new GameObject("FrontMarker");
                marker.transform.SetParent(transform, false);
                marker.layer = gameObject.layer;
                SpriteRenderer renderer = marker.AddComponent<SpriteRenderer>();
                renderer.sprite = GetBodySprite();
                renderer.color = FrontMarkerColor;
                renderer.sortingOrder = 2;
                _frontMarker = marker.transform;
            }
            
            SpriteRenderer markerRenderer = _frontMarker.GetComponent<SpriteRenderer>();
            if (markerRenderer != null)
            {
                markerRenderer.color = FrontMarkerColor;
                markerRenderer.sortingOrder = 2;
                if (markerRenderer.sprite == null)
                    markerRenderer.sprite = GetBodySprite();
            }
            
            _frontMarker.localScale = new Vector3(0.18f, 1.28f, 1f);
            UpdateFrontMarkerPosition();
        }

        private Sprite GetBodySprite()
        {
            if (_visualObject != null)
            {
                SpriteRenderer body = _visualObject.GetComponent<SpriteRenderer>();
                if (body != null && body.sprite != null)
                    return body.sprite;
            }
            
            Texture2D texture = Texture2D.whiteTexture;
            return Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                texture.width);
        }

        private void UpdateFrontMarkerPosition()
        {
            if (_frontMarker == null)
                return;
            
            float edgeX = _facing * (GameConstants.P_BODY_WIDTH * 0.5f);
            _frontMarker.localPosition = new Vector3(edgeX, GameConstants.P_BODY_OFFSET_Y, 0f);
        }

        private void UpdateGroundCheck()
        {
            RaycastHit2D hit = Physics2D.Raycast(
                _rb.position,
                Vector2.down,
                0.15f,
                LayerMask.GetMask("Ground"));
            _isGrounded = hit.collider != null && _rb.linearVelocity.y <= 0.15f;
        }

        /// <summary>
        /// 지상에 있을 때만 행동한다. 점프하지 않는다.
        /// 어그로 밖이면 대기, 안이면 P의 X를 추적한다.
        /// 방향이 바뀌면 0.8초 정지하고 기존 전방을 유지한다.
        /// 시간이 끝나면 전방(+X/-X)과 노란 표기만 스냅하고 걷는다. 몸통은 회전하지 않는다.
        /// </summary>
        private void UpdateGroundAi()
        {
            if (!_isGrounded)
            {
                _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
                return;
            }

            if (!TryGetPlayer(out Transform player))
            {
                EnterWait();
                return;
            }

            float distance = Vector2.Distance(_rb.position, player.position);
            if (!_isChasing)
            {
                if (distance > GameConstants.DUMMY_AGGRO_RANGE)
                {
                    EnterWait();
                    return;
                }

                _isChasing = true;
            }
            else if (distance > GameConstants.DUMMY_AGGRO_RANGE + GameConstants.DUMMY_AGGRO_EXIT)
            {
                EnterWait();
                return;
            }
            
            if (_isTurning)
            {
                UpdateTurn();
                return;
            }
            
            float deltaX = player.position.x - _rb.position.x;
            int wanted = 0;
            if (Mathf.Abs(deltaX) > 0.05f)
                wanted = deltaX > 0f ? 1 : -1;
            
            if (wanted != 0 && wanted != _facing)
            {
                BeginTurn(wanted);
                return;
            }
            
            if (Mathf.Abs(deltaX) <= GameConstants.DUMMY_STOP_RANGE)
            {
                _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
                return;
            }
            
            if (IsLedgeAhead(_facing))
            {
                _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
                return;
            }
            
            _rb.linearVelocity = new Vector2(_facing * GameConstants.DUMMY_CHASE_SPEED, _rb.linearVelocity.y);
        }

        private void EnterWait()
        {
            _isChasing = false;
            _isTurning = false;
            _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
        }

        private void BeginTurn(int newFacing)
        {
            _pendingFacing = newFacing;
            _isTurning = true;
            _turnElapsed = 0f;
            _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
        }

        private void UpdateTurn()
        {
            _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
            _turnElapsed += Time.fixedDeltaTime;
            if (_turnElapsed < GameConstants.DUMMY_TURN_TIME)
                return;

            _isTurning = false;
            FaceDirection(_pendingFacing);
        }

        private bool IsLedgeAhead(float dir)
        {
            Vector2 origin = _rb.position + new Vector2(dir * 0.55f, 0.05f);
            RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, 0.35f, LayerMask.GetMask("Ground"));
            return hit.collider == null;
        }

        private void FaceDirection(float dir)
        {
            if (Mathf.Abs(dir) < 0.01f)
                return;
            
            _facing = dir > 0f ? 1 : -1;
            UpdateFrontMarkerPosition();
        }

        private bool TryGetPlayer(out Transform player)
        {
            if (_player != null)
            {
                player = _player;
                return true;
            }
            
            GameObject found = GameObject.FindGameObjectWithTag("Player");
            if (found == null)
            {
                player = null;
                return false;
            }
            
            _player = found.transform;
            player = _player;
            return true;
        }

        private static void IgnorePlayerCollision()
        {
            int player = LayerMask.NameToLayer("Player");
            int enemy = LayerMask.NameToLayer("Enemy");
            if (player >= 0 && enemy >= 0)
                Physics2D.IgnoreLayerCollision(player, enemy, true);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = _isChasing ? Color.yellow : Color.gray;
            Gizmos.DrawWireSphere(transform.position, GameConstants.DUMMY_AGGRO_RANGE);
        }
    }
}
