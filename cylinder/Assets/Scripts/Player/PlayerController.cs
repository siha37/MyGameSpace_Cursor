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
            // 상태별 입력 제한
            switch (_currentState)
            {
                case PlayerState.Accel:
                case PlayerState.AccelReverse:
                case PlayerState.WallStick:
                case PlayerState.CeilingStick:
                case PlayerState.Attack:
                case PlayerState.Dead:
                    return; // 이 상태들에서는 새 입력 무시
            }
            
            // 이동 입력
            float horizontal = 0f;
            if (Input.GetKey(KeyCode.A)) horizontal -= 1f;
            if (Input.GetKey(KeyCode.D)) horizontal += 1f;
            
            float vertical = 0f;
            if (Input.GetKey(KeyCode.W)) vertical += 1f;
            if (Input.GetKey(KeyCode.S)) vertical -= 1f;
            
            // 악셀 입력 (Shift + 방향)
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                if (horizontal != 0f || vertical != 0f)
                {
                    Vector2 accelDir = new Vector2(horizontal, vertical).normalized;
                    _accel.TryAccel(accelDir);
                    return;
                }
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
            _movement.ProcessMovement(horizontal);
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
                _rb.velocity = Vector2.zero;
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
    }
}
