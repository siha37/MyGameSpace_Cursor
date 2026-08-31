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
        private Vector2 _stickNormal; // 부착 표면의 법선 벡터
        
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
            // 역분사 체크
            if (_isAcceling && IsOppositeDirection(direction, _accelDirection))
            {
                if (_gauge.TryConsume(GameConstants.AX_REVERSE_COST))
                {
                    StopAccel();
                    _controller.SetState(PlayerState.AccelReverse);
                    return;
                }
            }
            
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
            
            _rb.gravityScale = 0f; // 중력 무시
            _controller.SetState(PlayerState.Accel);
            
            // 부착 상태 해제
            if (_isSticking)
            {
                _isSticking = false;
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
            
            // 이동
            float speed = GameConstants.AX_DIST / GameConstants.AX_TIME;
            Vector2 velocity = _accelDirection * speed;
            
            // 경로 상 적 체크 (레이캐스트)
            RaycastHit2D hit = Physics2D.Raycast(_rb.position, _accelDirection, speed * Time.fixedDeltaTime, LayerMask.GetMask("Enemy"));
            
            if (hit.collider != null)
            {
                // 적 처치
                var enemy = hit.collider.GetComponent<IDamageable>();
                if (enemy != null)
                {
                    enemy.TakeDamage();
                    _controller.OnKillEnemy();
                }
                
                // 적 위치에서 정지
                _rb.position = hit.point;
                EndAccel();
                return;
            }
            
            // 벽/천장 체크
            hit = Physics2D.Raycast(_rb.position, _accelDirection, speed * Time.fixedDeltaTime, LayerMask.GetMask("Ground"));
            
            if (hit.collider != null)
            {
                // 표면에 도달
                _rb.position = hit.point;
                AttachToSurface(hit.normal);
                return;
            }
            
            // 이동 적용
            _rb.velocity = velocity;
        }

        /// <summary>
        /// 악셀 종료
        /// </summary>
        private void EndAccel()
        {
            _isAcceling = false;
            _rb.gravityScale = 1f;
            _rb.velocity = Vector2.zero;
            _controller.SetState(PlayerState.Idle);
        }

        /// <summary>
        /// 악셀 중단 (역분사)
        /// </summary>
        private void StopAccel()
        {
            _isAcceling = false;
            _rb.gravityScale = 1f;
            _rb.velocity = Vector2.zero;
        }

        /// <summary>
        /// 표면 부착
        /// </summary>
        private void AttachToSurface(Vector2 normal)
        {
            _isAcceling = false;
            _stickNormal = normal;
            
            // 바닥이면 부착하지 않고 착지
            if (Vector2.Dot(normal, Vector2.up) > 0.7f)
            {
                _rb.gravityScale = 1f;
                _rb.velocity = Vector2.zero;
                _controller.SetState(PlayerState.Idle);
                return;
            }
            
            // 벽/천장 부착
            _isSticking = true;
            _stickElapsed = 0f;
            _rb.gravityScale = 0f;
            _rb.velocity = Vector2.zero;
            
            // 표면 타입 판정 (법선 벡터 기반)
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
            _isSticking = false;
            _rb.gravityScale = 1f;
            _controller.SetState(PlayerState.Jump);
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
    }
}
