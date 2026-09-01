using UnityEngine;
using Cylinder.Core;

namespace Cylinder.Player
{
    /// <summary>
    /// 플레이어 악셀 시스템
    /// - 8방향 대쉬
    /// - 벽/천장 부착
    /// - 역분사
    /// - 경로 처치
    /// </summary>
    public class PlayerAccel
    {
        private readonly PlayerController _controller;
        private readonly Rigidbody2D _rb;
        private readonly PressureGauge _gauge;
        
        private bool _isAcceling;
        private Vector2 _accelDirection;
        private Vector2 _accelStartPos;
        private float _accelElapsed;
        
        private bool _isSticking;
        private float _stickElapsed;
        private SurfaceType _stickSurface;
        private Vector2 _stickNormal;
        private Collider2D _stickCollider;
        private Collider2D _ignoredStickCollider;
        private float _nextAccelAllowedTime;
        
        private enum SurfaceType { None, Wall, Ceiling }

        public PlayerAccel(PlayerController controller, Rigidbody2D rb, PressureGauge gauge)
        {
            _controller = controller;
            _rb = rb;
            _gauge = gauge;
        }

        /// <summary>
        /// 물리 업데이트
        /// </summary>
        public void FixedUpdate()
        {
            if (_isAcceling)
            {
                UpdateAccel();
            }
            else if (_isSticking)
            {
                UpdateStick();
            }
        }

        /// <summary>
        /// 악셀 시도
        /// </summary>
        public void TryAccel(Vector2 direction)
        {
            // 대쉬 중 역분사만 허용. 그 외 재시전은 불가
            if (_isAcceling)
            {
                if (IsOppositeDirection(direction, _accelDirection) &&
                    _gauge.TryConsume(GameConstants.AX_REVERSE_COST))
                {
                    StopAccel();
                    BeginAccelRecovery();
                    _controller.SetState(PlayerState.Idle);
                }
                return;
            }

            if (Time.time < _nextAccelAllowedTime)
                return;
            
            // 방향 제한 체크
            if (!IsAccelDirectionAllowed(direction))
                return;
            
            // 게이지 소모
            if (!_gauge.TryConsume(GameConstants.AX_COST))
                return;
            
            // 악셀 시작
            StartAccel(direction);
        }

        /// <summary>
        /// 악셀 시작
        /// </summary>
        private void StartAccel(Vector2 direction)
        {
            _isAcceling = true;
            _accelDirection = direction.normalized;
            _accelStartPos = _rb.position;
            _accelElapsed = 0f;
            
            _rb.gravityScale = 0f;
            _controller.SetState(PlayerState.Accel);
            
            if (_isSticking)
            {
                _ignoredStickCollider = _stickCollider;
                if (_stickNormal.sqrMagnitude > 0.01f)
                    _rb.position += _stickNormal.normalized * 0.12f;
                
                _isSticking = false;
                _stickCollider = null;
            }
        }

        /// <summary>
        /// 악셀 업데이트
        /// </summary>
        private void UpdateAccel()
        {
            _accelElapsed += Time.fixedDeltaTime;
            
            float progress = _accelElapsed / GameConstants.AX_TIME;
            
            if (progress >= 1f)
            {
                // 악셀 종료
                EndAccel();
                return;
            }
            
            float speed = GameConstants.AX_DIST / GameConstants.AX_TIME;
            float step = speed * Time.fixedDeltaTime;
            Vector2 dir = _accelDirection;
            Vector2 bodyOrigin = GetBodyOrigin();
            Vector2 hitBoxSize = new Vector2(GameConstants.AX_HIT_WIDTH, 1.6f);
            
            RaycastHit2D enemyHit = Physics2D.BoxCast(
                bodyOrigin,
                hitBoxSize,
                0f,
                dir,
                step,
                LayerMask.GetMask("Enemy"));
            
            if (enemyHit.collider != null)
            {
                var enemy = enemyHit.collider.GetComponent<IDamageable>();
                if (enemy != null)
                {
                    float travel = Mathf.Max(0f, enemyHit.distance);
                    _rb.position += dir * travel;
                    _rb.linearVelocity = Vector2.zero;
                    enemy.TakeDamage();
                    _controller.OnKillEnemy();
                    EndAccel();
                    return;
                }
            }
            
            if (TryGetBlockingGroundHit(bodyOrigin, hitBoxSize, dir, step, out RaycastHit2D hit))
            {
                _rb.position += dir * Mathf.Max(0f, hit.distance);
                AttachToSurface(hit.collider, hit.normal);
                return;
            }
            
            _rb.linearVelocity = dir * speed;
        }

        /// <summary>
        /// 악셀 종료
        /// </summary>
        private void EndAccel()
        {
            _isAcceling = false;
            _ignoredStickCollider = null;
            _rb.gravityScale = 1f;
            _rb.linearVelocity = Vector2.zero;
            BeginAccelRecovery();
            _controller.SetState(PlayerState.Idle);
        }

        /// <summary>
        /// 악셀 중단 (역분사)
        /// </summary>
        private void StopAccel()
        {
            _isAcceling = false;
            _ignoredStickCollider = null;
            _rb.gravityScale = 1f;
            _rb.linearVelocity = Vector2.zero;
        }

        /// <summary>
        /// 표면 부착
        /// </summary>
        private void AttachToSurface(Collider2D surface, Vector2 normal)
        {
            _isAcceling = false;
            _ignoredStickCollider = null;
            _stickNormal = normal;
            BeginAccelRecovery();
            
            if (Vector2.Dot(normal, Vector2.up) > 0.7f)
            {
                _rb.gravityScale = 1f;
                _rb.linearVelocity = Vector2.zero;
                _controller.SetState(PlayerState.Idle);
                return;
            }
            
            _isSticking = true;
            _stickCollider = surface;
            _stickElapsed = 0f;
            _rb.gravityScale = 0f;
            _rb.linearVelocity = Vector2.zero;
            
            if (Mathf.Abs(normal.x) > 0.7f)
            {
                _stickSurface = SurfaceType.Wall;
                _controller.SetState(PlayerState.WallStick);
            }
            else
            {
                _stickSurface = SurfaceType.Ceiling;
                _controller.SetState(PlayerState.CeilingStick);
            }
        }

        /// <summary>
        /// 부착 업데이트
        /// </summary>
        private void UpdateStick()
        {
            _stickElapsed += Time.fixedDeltaTime;
            
            if (_stickElapsed >= GameConstants.AX_STICK)
            {
                // 부착 시간 종료 - 낙하
                DetachFromSurface();
            }
        }

        /// <summary>
        /// 표면에서 이탈
        /// </summary>
        private void DetachFromSurface()
        {
            ReleaseStick();
            _controller.SetState(PlayerState.Jump);
        }

        /// <summary>
        /// 점프 이탈 등. 1초 부착은 악셀로 도달한 경우만 유지한다.
        /// </summary>
        public void ReleaseStick()
        {
            if (_stickNormal.sqrMagnitude > 0.01f)
                _rb.position += _stickNormal.normalized * 0.08f;
            
            _isSticking = false;
            _stickCollider = null;
            _ignoredStickCollider = null;
            _rb.gravityScale = 1f;
        }

        /// <summary>
        /// 반대 방향인지 체크
        /// </summary>
        private bool IsOppositeDirection(Vector2 dir1, Vector2 dir2)
        {
            dir1 = NormalizeToAxis(dir1);
            dir2 = NormalizeToAxis(dir2);
            
            return Vector2.Dot(dir1, dir2) < -0.9f;
        }

        /// <summary>
        /// 8방향을 축으로 정규화
        /// </summary>
        private Vector2 NormalizeToAxis(Vector2 dir)
        {
            float absX = Mathf.Abs(dir.x);
            float absY = Mathf.Abs(dir.y);
            
            // 상하좌우
            if (absX < 0.1f) return new Vector2(0, Mathf.Sign(dir.y));
            if (absY < 0.1f) return new Vector2(Mathf.Sign(dir.x), 0);
            
            // 대각선
            return new Vector2(Mathf.Sign(dir.x), Mathf.Sign(dir.y)).normalized;
        }

        /// <summary>
        /// 현재 상태에서 악셀 방향이 허용되는지 체크
        /// </summary>
        private bool IsAccelDirectionAllowed(Vector2 dir)
        {
            bool isGrounded = _controller.CurrentState != PlayerState.Jump && 
                            _controller.CurrentState != PlayerState.WallStick && 
                            _controller.CurrentState != PlayerState.CeilingStick;
            
            // 지상: 하, 좌하, 우하 금지 (아래쪽 방향 모두 차단)
            if (isGrounded)
            {
                // Y 성분이 음수(아래쪽)면 금지
                if (dir.y < -0.1f)
                    return false;
                
                return true;
            }
            
            // 천장 부착: 좌, 우, 좌하, 우하만 허용
            if (_controller.CurrentState == PlayerState.CeilingStick)
            {
                if (dir.y > 0.1f) return false; // 상 금지
                return true;
            }
            
            // 벽 부착: 벽 쪽 방향 금지
            if (_controller.CurrentState == PlayerState.WallStick)
            {
                // 벽 방향 판정 (표면 법선 벡터 사용)
                // 법선이 좌측을 가리키면 우측 벽, 우측을 가리키면 좌측 벽
                float wallNormalX = _stickNormal.x;
                
                // 벽으로 향하는 방향 금지 (법선 반대 방향)
                if (Vector2.Dot(dir, _stickNormal) < -0.1f)
                    return false;
                
                return true;
            }
            
            // 공중: 모든 방향 허용
            return true;
        }

        /// <summary>
        /// 악셀 가속 종료 후 재시전 대기
        /// </summary>
        private void BeginAccelRecovery()
        {
            _nextAccelAllowedTime = Time.time + GameConstants.AX_RECOVERY;
        }

        private Vector2 GetBodyOrigin()
        {
            return _rb.position + Vector2.up * 0.9f;
        }

        private bool TryGetBlockingGroundHit(Vector2 origin, Vector2 size, Vector2 dir, float distance, out RaycastHit2D hit)
        {
            hit = default;
            RaycastHit2D[] hits = Physics2D.BoxCastAll(origin, size, 0f, dir, distance, LayerMask.GetMask("Ground"));
            float best = float.MaxValue;
            bool found = false;
            
            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit2D candidate = hits[i];
                if (candidate.collider == null)
                    continue;
                
                if (_ignoredStickCollider != null && candidate.collider == _ignoredStickCollider)
                    continue;
                
                if (candidate.distance < 0.05f && Vector2.Dot(dir, candidate.normal) > 0.01f)
                    continue;
                
                if (candidate.distance < best)
                {
                    best = candidate.distance;
                    hit = candidate;
                    found = true;
                }
            }
            
            return found;
        }
    }
}
