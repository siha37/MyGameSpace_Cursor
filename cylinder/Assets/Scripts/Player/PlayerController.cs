using UnityEngine;
using Cylinder.Core;

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
        private PlayerAttack _attack;
        
        private PlayerState _currentState = PlayerState.Idle;
        private float _mapMinY;

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
            _attack = new PlayerAttack(this);
            ApplyNoFrictionMaterial();
            
            // 맵 최저점 계산 (임시로 리스폰 포인트 기준)
            if (_respawnPoint != null)
                _mapMinY = _respawnPoint.position.y;
        }

        private void Update()
        {
            // 낙하 체크 (MVP: 맵 최저점 -5)
            if (transform.position.y < _mapMinY + GameConstants.FALL_RESET_Y)
            {
                RespawnAtStart();
                return;
            }
            
            HandleInput();
            
            // 공격 업데이트
            _attack.Update();
        }

        private void FixedUpdate()
        {
            _movement.FixedUpdate();
            _accel.FixedUpdate();
        }

        /// <summary>
        /// 입력 처리
        /// </summary>
        private void HandleInput()
        {
            if (_currentState == PlayerState.Dead)
                return;

            Vector2 moveInput = InputHelper.GetMovementInput();

            // 악셀 입력 (Shift + 방향). 대쉬 중에는 역분사만 내부에서 허용
            if (_currentState != PlayerState.Attack &&
                InputHelper.IsShiftPressed() &&
                moveInput.magnitude > 0.1f)
            {
                _accel.TryAccel(InputHelper.SnapTo8Directions(moveInput));
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
                case PlayerState.Attack:
                    return;
            }
            
            // 점프 입력
            if (Input.GetKeyDown(KeyCode.Space))
            {
                _movement.TryJump();
            }
            
            // 공격 입력 (마우스 좌클릭)
            if (Input.GetMouseButtonDown(0))
            {
                _attack.TryAttack(GetMouseDirection());
            }
            
            // 이동 처리
            _movement.ProcessMovement(moveInput.x);
        }

        /// <summary>
        /// 마우스 커서 방향 계산
        /// </summary>
        private Vector2 GetMouseDirection()
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 direction = (mousePos - transform.position).normalized;
            
            // 커서 방향에 따라 전방 갱신
            if (direction.x > 0) FacingDirection = 1;
            else if (direction.x < 0) FacingDirection = -1;
            
            return direction;
        }

        /// <summary>
        /// 상태 변경
        /// </summary>
        public void SetState(PlayerState newState)
        {
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
            
            // 플레이어 위치
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.1f);
            
            // 전방 방향
            Gizmos.color = Color.blue;
            Vector3 forward = new Vector3(FacingDirection, 0, 0) * 0.5f;
            Gizmos.DrawLine(transform.position, transform.position + forward);
        }
    }
}
