using UnityEngine;
using Cylinder.Core;

namespace Cylinder.Player
{
    /// <summary>
    /// 플레이어 공격 시스템
    /// - 마우스 커서 방향 근접 공격
    /// - 범위 판정
    /// </summary>
    public class PlayerAttack
    {
        private readonly PlayerController _controller;
        private bool _isAttacking;
        private float _attackTimer;
        private Vector2 _attackDirection;
        private bool _hasHitChecked;

        public PlayerAttack(PlayerController controller)
        {
            _controller = controller;
        }

        /// <summary>
        /// 공격 시도
        /// </summary>
        public void TryAttack(Vector2 direction)
        {
            // 악셀, 부착 중에는 공격 불가
            if (_controller.CurrentState == PlayerState.Accel ||
                _controller.CurrentState == PlayerState.AccelReverse ||
                _controller.CurrentState == PlayerState.WallStick ||
                _controller.CurrentState == PlayerState.CeilingStick ||
                _isAttacking)
            {
                return;
            }
            
            StartAttack(direction);
        }

        /// <summary>
        /// 공격 시작
        /// </summary>
        private void StartAttack(Vector2 direction)
        {
            _isAttacking = true;
            _attackTimer = 0f;
            _attackDirection = direction;
            _hasHitChecked = false;
            _controller.SetState(PlayerState.Attack);
        }

        /// <summary>
        /// 공격 업데이트 (PlayerController의 Update에서 호출)
        /// </summary>
        public void Update()
        {
            if (!_isAttacking)
                return;
            
            _attackTimer += Time.deltaTime;
            
            // 공격 판정 시점
            if (!_hasHitChecked && _attackTimer >= GameConstants.P_ATK_ACTIVE)
            {
                PerformAttackHitCheck(_attackDirection);
                _hasHitChecked = true;
            }
            
            // 공격 종료 (판정 + 후딜)
            if (_attackTimer >= GameConstants.P_ATK_ACTIVE + GameConstants.P_ATK_RECOVER)
            {
                _isAttacking = false;
                _controller.SetState(PlayerState.Idle);
            }
        }

        /// <summary>
        /// 공격 판정 체크
        /// </summary>
        private void PerformAttackHitCheck(Vector2 direction)
        {
            // 박스캐스트로 범위 체크
            RaycastHit2D[] hits = Physics2D.BoxCastAll(
                _controller.transform.position,
                new Vector2(GameConstants.P_ATK_WIDTH, GameConstants.P_ATK_WIDTH),
                0f,
                direction,
                GameConstants.P_ATK_RANGE,
                LayerMask.GetMask("Enemy")
            );
            
            foreach (var hit in hits)
            {
                var enemy = hit.collider.GetComponent<IDamageable>();
                if (enemy != null)
                {
                    enemy.TakeDamage();
                    _controller.OnKillEnemy();
                }
            }
        }
    }
}
