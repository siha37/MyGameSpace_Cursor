using UnityEngine;
using Cylinder.Core;

namespace Cylinder.Player
{
    /// <summary>
    /// 플레이어 이동 시스템
    /// - 기본 이동 = 질주
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
        private bool _keepAccelFallVelocity;
        private int _runDirection;

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
            
            if (_isRunning && CanChargeFromRun())
            {
                _gauge.ChargeFromRunning(Time.fixedDeltaTime);
            }
        }

        /// <summary>
        /// 이동 처리. A/D 홀드가 곧 질주다.
        /// </summary>
        public void ProcessMovement(float horizontal)
        {
            if (_controller.CurrentState == PlayerState.Accel ||
                _controller.CurrentState == PlayerState.AccelReverse ||
                _controller.CurrentState == PlayerState.WallStick ||
                _controller.CurrentState == PlayerState.CeilingStick)
            {
                StopRunCharge();
                return;
            }

            if (_keepAccelFallVelocity)
            {
                if (_isGrounded)
                {
                    _keepAccelFallVelocity = false;
                    _currentVelocityX = _rb.linearVelocity.x;
                }
                else
                {
                    _rb.linearVelocity = new Vector2(_currentVelocityX, _rb.linearVelocity.y);
                    UpdateMovementState();
                    return;
                }
            }
            
            float targetSpeed = 0f;
            bool wantsMove = Mathf.Abs(horizontal) > 0.01f;
            
            if (wantsMove)
            {
                _isRunning = true;
                _runDirection = horizontal > 0f ? 1 : -1;
                targetSpeed = _runDirection * GameConstants.P_RUN_SPEED;
            }
            else
            {
                _isRunning = false;
            }
            
            float accel = GameConstants.P_MOVE_ACCEL;
            
            if (Mathf.Sign(_currentVelocityX) != Mathf.Sign(targetSpeed) && 
                Mathf.Abs(_currentVelocityX) > 0.1f && 
                Mathf.Abs(targetSpeed) > 0.1f)
            {
                accel = GameConstants.P_MOVE_DECEL;
            }
            
            if (Mathf.Abs(targetSpeed) < 0.1f && Mathf.Abs(_currentVelocityX) > 0.1f)
            {
                accel = GameConstants.P_MOVE_DECEL;
            }
            
            if (!_isGrounded)
            {
                if (_isRunning)
                {
                    _currentVelocityX = targetSpeed;
                }
                else
                {
                    accel *= GameConstants.P_AIR_CONTROL;
                }
            }
            
            if (!(_isRunning && !_isGrounded))
            {
                _currentVelocityX = Mathf.MoveTowards(_currentVelocityX, targetSpeed, accel * Time.deltaTime);
            }
            
            _rb.linearVelocity = new Vector2(_currentVelocityX, _rb.linearVelocity.y);
            
            UpdateMovementState();
        }

        /// <summary>
        /// 점프 시도
        /// </summary>
        public void TryJump()
        {
            if (_controller.CurrentState == PlayerState.WallStick ||
                _controller.CurrentState == PlayerState.CeilingStick)
            {
                DetachFromSurface();
                PerformJump();
                return;
            }
            
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
            float jumpVelocity = Mathf.Sqrt(2f * Mathf.Abs(Physics2D.gravity.y) * _rb.gravityScale * GameConstants.P_JUMP_HEIGHT);
            
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, jumpVelocity);
            _controller.SetState(PlayerState.Jump);
        }

        /// <summary>
        /// 지면 체크
        /// </summary>
        private void UpdateGroundCheck()
        {
            float rayDistance = 0.1f;
            RaycastHit2D hit = Physics2D.Raycast(_rb.position, Vector2.down, rayDistance, LayerMask.GetMask("Ground"));
            
            _isGrounded = hit.collider != null && _rb.linearVelocity.y <= 0.1f;
        }

        /// <summary>
        /// 이동 상태 갱신
        /// </summary>
        private void UpdateMovementState()
        {
            if (_controller.CurrentState == PlayerState.Accel ||
                _controller.CurrentState == PlayerState.AccelReverse ||
                _controller.CurrentState == PlayerState.WallStick ||
                _controller.CurrentState == PlayerState.CeilingStick)
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
        /// 악셀이 벽/천장에 못 닿았을 때 대쉬 속도의 20%를 낙하까지 유지한다.
        /// </summary>
        public void OnAccelCarryFall(Vector2 leftoverVelocity)
        {
            _currentVelocityX = leftoverVelocity.x;
            _keepAccelFallVelocity = leftoverVelocity.sqrMagnitude > 0.01f;
        }

        /// <summary>
        /// 악셀이 시작되면 질주 게이지 충전을 즉시 끊는다.
        /// </summary>
        public void StopRunCharge()
        {
            _isRunning = false;
        }

        private bool CanChargeFromRun()
        {
            PlayerState state = _controller.CurrentState;
            if (state == PlayerState.Accel ||
                state == PlayerState.AccelReverse ||
                state == PlayerState.WallStick ||
                state == PlayerState.CeilingStick ||
                state == PlayerState.Stunned ||
                state == PlayerState.Dead)
            {
                return false;
            }
            
            return true;
        }
    }
}
