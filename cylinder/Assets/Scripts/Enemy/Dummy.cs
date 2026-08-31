using UnityEngine;
using System.Collections;
using Cylinder.Core;

namespace Cylinder.Enemy
{
    /// <summary>
    /// 허수아비 (MVP 테스트용)
    /// - 정지 상태
    /// - 1피격 즉사
    /// - 3초 후 리스폰
    /// </summary>
    public class Dummy : MonoBehaviour, IDamageable
    {
        [Header("Settings")]
        [SerializeField] private GameObject _visualObject;
        
        private Vector3 _spawnPosition;
        private bool _isDead;

        private void Awake()
        {
            _spawnPosition = transform.position;
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
            
            // 비주얼 숨김
            if (_visualObject != null)
                _visualObject.SetActive(false);
            
            // 콜라이더 비활성화
            var collider = GetComponent<Collider2D>();
            if (collider != null)
                collider.enabled = false;
            
            // 리스폰 예약
            StartCoroutine(RespawnCoroutine());
        }

        /// <summary>
        /// 리스폰 코루틴
        /// </summary>
        private IEnumerator RespawnCoroutine()
        {
            yield return new WaitForSeconds(GameConstants.DUMMY_RESPAWN);
            
            Respawn();
        }

        /// <summary>
        /// 리스폰
        /// </summary>
        private void Respawn()
        {
            _isDead = false;
            transform.position = _spawnPosition;
            
            // 비주얼 표시
            if (_visualObject != null)
                _visualObject.SetActive(true);
            
            // 콜라이더 활성화
            var collider = GetComponent<Collider2D>();
            if (collider != null)
                collider.enabled = true;
        }
    }
}
