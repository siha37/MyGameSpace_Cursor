using UnityEngine;
using System.Collections;
using Cylinder.Core;

namespace Cylinder.Enemy
{
    /// <summary>
    /// 허수아비 (MVP 테스트용)
    /// - 중력으로 지면에 착지
    /// - P와는 물리 통과, 공격/악셀 판정은 유지
    /// - 1피격 즉사, 3초 후 리스폰
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class Dummy : MonoBehaviour, IDamageable
    {
        [Header("Settings")]
        [SerializeField] private GameObject _visualObject;
        
        private Rigidbody2D _rb;
        private Collider2D _collider;
        private Vector3 _spawnPosition;
        private bool _isDead;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            if (_rb == null)
                _rb = gameObject.AddComponent<Rigidbody2D>();
            
            _collider = GetComponent<Collider2D>();
            _spawnPosition = transform.position;
            ConfigurePhysics();
            IgnorePlayerCollision();
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
        /// 사망 처리
        /// </summary>
        private void Die()
        {
            _isDead = true;
            
            if (_visualObject != null)
                _visualObject.SetActive(false);
            
            if (_collider != null)
                _collider.enabled = false;
            
            if (_rb != null)
            {
                _rb.linearVelocity = Vector2.zero;
                _rb.simulated = false;
            }
            
            StartCoroutine(RespawnCoroutine());
        }

        private IEnumerator RespawnCoroutine()
        {
            yield return new WaitForSeconds(GameConstants.DUMMY_RESPAWN);
            Respawn();
        }

        /// <summary>
        /// 리스폰 후 다시 낙하해 지면에 붙는다
        /// </summary>
        private void Respawn()
        {
            _isDead = false;
            transform.position = _spawnPosition;
            
            if (_visualObject != null)
                _visualObject.SetActive(true);
            
            if (_collider != null)
                _collider.enabled = true;
            
            if (_rb != null)
            {
                _rb.linearVelocity = Vector2.zero;
                _rb.simulated = true;
            }
        }

        private void ConfigurePhysics()
        {
            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.gravityScale = 1f;
            _rb.freezeRotation = true;
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionX;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rb.simulated = true;
        }

        private static void IgnorePlayerCollision()
        {
            int player = LayerMask.NameToLayer("Player");
            int enemy = LayerMask.NameToLayer("Enemy");
            if (player >= 0 && enemy >= 0)
                Physics2D.IgnoreLayerCollision(player, enemy, true);
        }
    }
}
