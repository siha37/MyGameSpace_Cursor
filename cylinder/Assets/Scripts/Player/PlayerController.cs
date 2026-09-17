using UnityEngine;
using Cylinder.Core;
using Cylinder.UI;

namespace Cylinder.Player
{
    /// <summary>
    /// 플레이어 메인 컨트롤러
    /// - 입력 처리 및 하위 시스템 조율
    /// - 상태 관리
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(PressureGauge))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private Transform _respawnPoint;
        
        [Header("Debug")]
        [SerializeField] private bool _showDebugGizmos = true;
        
        private Rigidbody2D _rb;
        private PressureGauge _gauge;
        private PlayerMovement _movement;
        private PlayerAccel _accel;
        private ContactPoint2D[] _contactBuffer = new ContactPoint2D[8];
        
        private PlayerState _currentState = PlayerState.Idle;
        private float _mapMinY;
        private float _stunUntil;

        /// <summary>현재 상태</summary>
        public PlayerState CurrentState => _currentState;
        
        /// <summary>플레이어가 바라보는 방향 (1: 우, -1: 좌)</summary>
        public int FacingDirection { get; private set; } = 1;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _gauge = GetComponent<PressureGauge>();
            _movement = new PlayerMovement(this, _rb, _gauge);
            _accel = new PlayerAccel(this, _rb, _gauge);
            ApplyBodySize();
            ApplyNoFrictionMaterial();
            HelpUI.EnsureInScene();
            
            if (_respawnPoint != null)
                _mapMinY = _respawnPoint.position.y;
        }

        private void Update()
        {
            if (transform.position.y < _mapMinY + GameConstants.FALL_RESET_Y)
            {
                RespawnAtStart();
                return;
            }
            
            if (HelpUI.IsOpen)
                return;
            if (_currentState == PlayerState.Stunned)
            {
                if (Time.time >= _stunUntil)
                    SetState(IsOnGround() ? PlayerState.Idle : PlayerState.Jump);
                else
                    return;
            }
            
            HandleInput();
        }

        private void FixedUpdate()
        {
            _movement.FixedUpdate();
            _accel.FixedUpdate();
            PreventUnintendedWallStick();
        }

        /// <summary>
        /// 입력 처리
        /// </summary>
        private void HandleInput()
        {
            if (HelpUI.IsOpen)
                return;

            if (_currentState == PlayerState.Dead || _currentState == PlayerState.Stunned)
                return;

            Vector2 moveInput = InputHelper.GetMovementInput();
            Vector2 accelDir = InputHelper.GetAccelAimInput();

            if (accelDir.x > 0.05f) FacingDirection = 1;
            else if (accelDir.x < -0.05f) FacingDirection = -1;
            else if (moveInput.x > 0.05f) FacingDirection = 1;
            else if (moveInput.x < -0.05f) FacingDirection = -1;

            if (InputHelper.IsAccelPressedThisFrame())
            {
                if (_currentState == PlayerState.CeilingStick)
                    _accel.TryAccelFromCeiling();
                else if (accelDir.sqrMagnitude > 0.01f)
                    _accel.TryAccel(accelDir);
                return;
            }

            if (_currentState == PlayerState.WallStick ||
                _currentState == PlayerState.CeilingStick)
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    _accel.ReleaseStick();
                    _movement.TryJump();
                }
                return;
            }

            switch (_currentState)
            {
                case PlayerState.Accel:
                case PlayerState.AccelReverse:
                    return;
            }
            
            if (Input.GetKeyDown(KeyCode.Space))
            {
                _movement.TryJump();
            }
            
            _movement.ProcessMovement(moveInput.x);
        }

        /// <summary>
        /// 상태 변경
        /// </summary>
        public void SetState(PlayerState newState)
        {
            if (newState == PlayerState.Accel ||
                newState == PlayerState.AccelReverse ||
                newState == PlayerState.WallStick ||
                newState == PlayerState.CeilingStick ||
                newState == PlayerState.Stunned)
            {
                _movement.StopRunCharge();
            }
            
            _currentState = newState;
        }

        /// <summary>
        /// 시작점 리스폰
        /// </summary>
        private void RespawnAtStart()
        {
            if (_respawnPoint != null)
            {
                transform.position = _respawnPoint.position;
                _rb.linearVelocity = Vector2.zero;
                _gauge.ResetToStart();
                _stunUntil = 0f;
                _movement.OnAccelCarryFall(Vector2.zero);
                SetState(PlayerState.Idle);
            }
        }

        /// <summary>
        /// 처치 보상
        /// </summary>
        public void OnKillEnemy()
        {
            _gauge.GainFromKill();
        }

        /// <summary>
        /// 악셀이 벽/천장에 못 닿고 끝났을 때 잔여 속도(20%)를 이동 쪽에 넘긴다.
        /// </summary>
        public void OnAccelCarryFall(Vector2 leftoverVelocity)
        {
            _movement.OnAccelCarryFall(leftoverVelocity);
        }

        /// <summary>
        /// 허수아비 전방 차단. 악셀 반대 방향으로 넉백하고 0.5s 조작 불가.
        /// </summary>
        public void ApplyFrontBlockKnockback(Vector2 knockbackDir)
        {
            Vector2 dir = knockbackDir.sqrMagnitude > 0.01f ? knockbackDir.normalized : new Vector2(-FacingDirection, 0f);
            _rb.gravityScale = 1f;
            _rb.linearVelocity = dir * GameConstants.P_KNOCKBACK_SPEED;
            _stunUntil = Time.time + GameConstants.P_STUN_TIME;
            _movement.OnAccelCarryFall(Vector2.zero);
            SetState(PlayerState.Stunned);
        }

        private bool IsOnGround()
        {
            RaycastHit2D hit = Physics2D.Raycast(
                _rb.position,
                Vector2.down,
                0.15f,
                LayerMask.GetMask("Ground"));
            return hit.collider != null;
        }

        /// <summary>
        /// 몸통 비율 1:1.2
        /// </summary>
        private void ApplyBodySize()
        {
            CapsuleCollider2D capsule = GetComponent<CapsuleCollider2D>();
            if (capsule != null)
            {
                capsule.size = new Vector2(GameConstants.P_BODY_WIDTH, GameConstants.P_BODY_HEIGHT);
                capsule.offset = new Vector2(0f, GameConstants.P_BODY_OFFSET_Y);
            }

            Transform visual = transform.Find("Visual");
            if (visual != null)
            {
                visual.localScale = new Vector3(GameConstants.P_BODY_WIDTH, GameConstants.P_BODY_HEIGHT, 1f);
                visual.localPosition = new Vector3(0f, GameConstants.P_BODY_OFFSET_Y, 0f);
            }
        }

        /// <summary>
        /// 악셀 부착이 아닐 때 벽 마찰로 매달리지 않게 한다.
        /// </summary>
        private void PreventUnintendedWallStick()
        {
            if (_currentState == PlayerState.WallStick ||
                _currentState == PlayerState.CeilingStick ||
                _currentState == PlayerState.Accel)
            {
                return;
            }
            
            int count = _rb.GetContacts(_contactBuffer);
            if (count <= 0)
                return;
            
            Vector2 velocity = _rb.linearVelocity;
            Vector2 separation = Vector2.zero;
            int groundLayer = LayerMask.NameToLayer("Ground");
            
            for (int i = 0; i < count; i++)
            {
                ContactPoint2D contact = _contactBuffer[i];
                if (contact.collider == null)
                    continue;
                if (contact.collider.gameObject.layer != groundLayer)
                    continue;
                
                Vector2 normal = contact.normal;
                if (Mathf.Abs(normal.x) < GameConstants.WALL_NORMAL_X)
                    continue;
                
                float intoWall = Vector2.Dot(velocity, -normal);
                if (intoWall > 0f)
                    velocity += normal * intoWall;
                
                if (contact.separation < 0.02f)
                    separation += normal * GameConstants.WALL_SEPARATION;
            }
            
            _rb.linearVelocity = velocity;
            if (separation.sqrMagnitude > 0f)
                _rb.position += separation;
        }

        private void ApplyNoFrictionMaterial()
        {
            PhysicsMaterial2D material = new PhysicsMaterial2D("PlayerNoFriction")
            {
                friction = 0f,
                bounciness = 0f
            };
            _rb.sharedMaterial = material;
            
            Collider2D body = GetComponent<Collider2D>();
            if (body != null)
                body.sharedMaterial = material;
        }

        /// <summary>
        /// 디버그 기즈모 그리기
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!_showDebugGizmos)
                return;
            
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.1f);
            
            Gizmos.color = Color.blue;
            Vector3 forward = new Vector3(FacingDirection, 0, 0) * 0.5f;
            Gizmos.DrawLine(transform.position, transform.position + forward);
            
            if (Application.isPlaying)
            {
                Vector2 accelDir = InputHelper.GetAccelAimInput();
                if (accelDir.sqrMagnitude > 0.01f)
                {
                    Gizmos.color = Color.cyan;
                    Vector3 origin = transform.position + Vector3.up * GameConstants.P_BODY_OFFSET_Y;
                    Gizmos.DrawLine(origin, origin + (Vector3)(accelDir * 1.5f));
                }
            }
        }
    }
}
