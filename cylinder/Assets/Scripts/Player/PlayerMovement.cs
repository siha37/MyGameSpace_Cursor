using UnityEngine;
using Cylinder.Core;

namespace Cylinder.Player
{
    /// <summary>
    /// 플레이어 이동 시스템
    /// - 보행, 달리기, 점프
    /// - 방향 전환 관성
    /// </summary>
    public class PlayerMovement
    {
        private readonly PlayerController _controller;
        private readonly Rigidbody2D _rb;
        private readonly PressureGauge _gauge;
        
        private float _currentVelocityX;
        private bool _isGrounded;
        private bool _isRunning;
        private int _runDirection; // 1: 우, -1: 좌, 0: 없음
        
        // 달리기 2연타 감지
        private float _lastTapTimeA;
        private float _lastTapTimeD;
        private bool _isHoldingA;
        private bool _isHoldingD;

        public PlayerMovement(PlayerController controller, Rigidbody2D rb, PressureGauge gauge)
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
            UpdateGroundCheck();
            
            // 달리기 중 게이지 충전
            if (_isRunning)
            {
                _gauge.ChargeFromRunning(Time.fixedDeltaTime);
            }
        }

        /// <summary>
        /// 이동 처리
        /// </summary>
        public void ProcessMovement(float horizontal)
        {
            // 악셀/부착 상태에서는 이동 불가
            if (_controller.CurrentState == PlayerState.Accel ||
                _controller.CurrentState == PlayerState.AccelReverse ||
                _controller.CurrentState == PlayerState.WallStick ||
                _controller.CurrentState == PlayerState.CeilingStick ||
                _controller.CurrentState == PlayerState.Attack)
            {
                return;
            }
            
            // 달리기 2연타 감지
            HandleDoubleTapForRun(horizontal);
            
            // 속도 계산
            float targetSpeed = 0f;
            
            if (_isRunning)
            {
                // 달리기 중
                targetSpeed = _runDirection * GameConstants.P_RUN_SPEED;
                
                // 홀드 확인 (달리기 유지 조건)
                bool isHoldingRunKey = (_runDirection > 0 && Input.GetKey(KeyCode.D)) ||
                                       (_runDirection < 0 && Input.GetKey(KeyCode.A));
                
                if (!isHoldingRunKey)
                {
                    // 홀드를 떼면 달리기 종료
                    _isRunning = false;
                }
            }
            else if (horizontal != 0f)
            {
                // 일반 보행
                targetSpeed = horizontal * GameConstants.P_MOVE_SPEED;
            }
            
            // 관성 적용 (방향 전환 시 감속 -> 영점 -> 재가속)
            float accel = GameConstants.P_MOVE_ACCEL;
            
            // 방향이 반대면 감속 사용
            if (Mathf.Sign(_currentVelocityX) != Mathf.Sign(targetSpeed) && 
                Mathf.Abs(_currentVelocityX) > 0.1f && 
                Mathf.Abs(targetSpeed) > 0.1f)
            {
                accel = GameConstants.P_MOVE_DECEL;
            }
            
            // 정지할 때도 감속 적용
            if (Mathf.Abs(targetSpeed) < 0.1f && Mathf.Abs(_currentVelocityX) > 0.1f)
            {
                accel = GameConstants.P_MOVE_DECEL;
            }
            
            // 공중이면 제어력 감소 (단, 질주 점프 중에는 속도 유지)
            if (!_isGrounded)
            {
                if (_isRunning)
                {
                    // 질주 점프 중에는 수평 속도 유지
                    _currentVelocityX = targetSpeed;
                }
                else
                {
                    accel *= GameConstants.P_AIR_CONTROL;
                }
            }
            
            // 속도 갱신
            if (!(_isRunning && !_isGrounded)) // 질주 점프가 아닐 때만 점진적 변화
            {
                _currentVelocityX = Mathf.MoveTowards(_currentVelocityX, targetSpeed, accel * Time.deltaTime);
            }
            
            // 속도 적용
            _rb.velocity = new Vector2(_currentVelocityX, _rb.velocity.y);
            
            // 상태 갱신
            UpdateMovementState();
        }

        /// <summary>
        /// 달리기 2연타 감지
        /// </summary>
        private void HandleDoubleTapForRun(float horizontal)
        {
            // A 키 처리
            bool wasHoldingA = _isHoldingA;
            _isHoldingA = Input.GetKey(KeyCode.A);
            
            if (_isHoldingA && !wasHoldingA)
            {
                // A 키를 새로 눌렀을 때
                if (Time.time - _lastTapTimeA < GameConstants.P_RUN_TAP)
                {
                    // 2연타 성공
                    _isRunning = true;
                    _runDirection = -1;
                }
                _lastTapTimeA = Time.time;
            }
            
            // D 키 처리
            bool wasHoldingD = _isHoldingD;
            _isHoldingD = Input.GetKey(KeyCode.D);
            
            if (_isHoldingD && !wasHoldingD)
            {
                // D 키를 새로 눌렀을 때
                if (Time.time - _lastTapTimeD < GameConstants.P_RUN_TAP)
                {
                    // 2연타 성공
                    _isRunning = true;
                    _runDirection = 1;
                }
                _lastTapTimeD = Time.time;
            }
        }

        /// <summary>
        /// 점프 시도
        /// </summary>
        public void TryJump()
        {
            // 부착 상태에서는 점프로 이탈 가능
            if (_controller.CurrentState == PlayerState.WallStick ||
                _controller.CurrentState == PlayerState.CeilingStick)
            {
                DetachFromSurface();
                PerformJump();
                return;
            }
            
            // 지상에서만 점프
            if (_isGrounded && _controller.CurrentState != PlayerState.Accel)
            {
                PerformJump();
            }
        }

        /// <summary>
        /// 점프 실행
        /// </summary>
        private void PerformJump()
        {
            // 점프 높이에서 필요한 초기 속도 계산
            float jumpVelocity = Mathf.Sqrt(2f * Mathf.Abs(Physics2D.gravity.y) * _rb.gravityScale * GameConstants.P_JUMP_HEIGHT);
            
            _rb.velocity = new Vector2(_rb.velocity.x, jumpVelocity);
            _controller.SetState(PlayerState.Jump);
        }

        /// <summary>
        /// 지면 체크
        /// </summary>
        private void UpdateGroundCheck()
        {
            // 레이캐스트로 지면 체크 (플레이어 발 밑 0.1 유닛)
            float rayDistance = 0.1f;
            RaycastHit2D hit = Physics2D.Raycast(_rb.position, Vector2.down, rayDistance, LayerMask.GetMask("Ground"));
            
            // 속도가 아래쪽이고 충돌이 있으면 지상
            _isGrounded = hit.collider != null && _rb.velocity.y <= 0.1f;
        }

        /// <summary>
        /// 이동 상태 갱신
        /// </summary>
        private void UpdateMovementState()
        {
            if (_controller.CurrentState == PlayerState.Accel ||
                _controller.CurrentState == PlayerState.AccelReverse ||
                _controller.CurrentState == PlayerState.WallStick ||
                _controller.CurrentState == PlayerState.CeilingStick ||
                _controller.CurrentState == PlayerState.Attack)
            {
                return;
            }
            
            if (!_isGrounded)
            {
                _controller.SetState(PlayerState.Jump);
            }
            else if (_isRunning)
            {
                _controller.SetState(PlayerState.Run);
            }
            else if (Mathf.Abs(_currentVelocityX) > 0.1f)
            {
                _controller.SetState(PlayerState.Move);
            }
            else
            {
                _controller.SetState(PlayerState.Idle);
            }
        }

        /// <summary>
        /// 표면에서 이탈
        /// </summary>
        public void DetachFromSurface()
        {
            _rb.gravityScale = 1f;
            _controller.SetState(PlayerState.Jump);
        }

        /// <summary>
        /// 악셀 후 착지 시 호출
        /// </summary>
        public void OnLandFromAccel()
        {
            _currentVelocityX = _rb.velocity.x;
        }
    }
}
