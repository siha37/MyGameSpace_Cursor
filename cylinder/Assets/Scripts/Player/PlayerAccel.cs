using UnityEngine;
using Cylinder.Core;
using Cylinder.Enemy;

namespace Cylinder.Player
{
    /// <summary>
    /// 플레이어 악셀 시스템
    /// - 8방향 키보드 대쉬
    /// - 벽/천장 부착
    /// - 역분사
    /// - 경로 처치
    /// </summary>
    public class PlayerAccel
    {
        private const float StickProbe = 0.25f;
        private const RigidbodyConstraints2D BodyConstraints =
            RigidbodyConstraints2D.FreezeRotation;
        private const RigidbodyConstraints2D StickConstraints =
            RigidbodyConstraints2D.FreezeAll;

        private readonly PlayerController _controller;
        private readonly Rigidbody2D _rb;
        private readonly PressureGauge _gauge;
        private readonly ContactPoint2D[] _contactBuffer = new ContactPoint2D[8];
        
        private bool _isAcceling;
        private Vector2 _accelDirection;
        private Vector2 _accelStartPos;
        private float _accelElapsed;
        
        private bool _isSticking;
        private float _stickElapsed;
        private SurfaceType _stickSurface;
        private Vector2 _stickNormal;
        private Vector2 _stickPosition;
        private Collider2D _stickCollider;
        private Collider2D _ignoredStickCollider;
        private float _nextAccelAllowedTime;
        private Vector2 _ceilingExitDir;
        private int _preAccelMoveX = 1;
        
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
            if (_isAcceling)
            {
                if (IsOppositeDirection(direction, _accelDirection) &&
                    _gauge.TryConsume(GameConstants.AX_REVERSE_COST))
                {
                    StopAccel();
                    BeginAccelRecovery();
                    SetStateAfterAccel();
                }
                return;
            }

            if (Time.time < _nextAccelAllowedTime)
                return;
            
            if (_controller.CurrentState == PlayerState.Stunned)
                return;
            
            if (!IsAccelDirectionAllowed(direction))
                return;
            
            if (!_gauge.TryConsume(GameConstants.AX_COST))
                return;
            
            StartAccel(direction);
        }

        /// <summary>
        /// 천장 부착 중 Shift. 방향키 없이 같은 쪽 하단 대각으로 이탈한다.
        /// </summary>
        public void TryAccelFromCeiling()
        {
            if (_controller.CurrentState != PlayerState.CeilingStick)
                return;
            
            if (_ceilingExitDir.sqrMagnitude < 0.01f)
            {
                _ceilingExitDir = InputHelper.SnapTo8Directions(
                    new Vector2(_controller.FacingDirection, -1f));
            }
            
            TryAccel(_ceilingExitDir);
        }

        /// <summary>
        /// 지면 여부. 악셀 종료 후 공중이면 Jump로 두어 전방향 조준이 유지되게 한다.
        /// </summary>
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
        /// 악셀 후 착지/체공 상태
        /// </summary>
        private void SetStateAfterAccel()
        {
            _controller.SetState(IsOnGround() ? PlayerState.Idle : PlayerState.Jump);
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
            _preAccelMoveX = _controller.FacingDirection != 0 ? _controller.FacingDirection : 1;
            
            RestoreBodyConstraints();
            _rb.gravityScale = 0f;
            _rb.linearVelocity = Vector2.zero;
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
            float dt = Time.fixedDeltaTime;
            _accelElapsed += dt;
            
            float speed = GameConstants.AccelSpeed;
            float intendedNow = Mathf.Min(_accelElapsed * speed, GameConstants.AX_DIST);
            float intendedPrev = Mathf.Min((_accelElapsed - dt) * speed, GameConstants.AX_DIST);
            float step = Mathf.Max(0f, intendedNow - intendedPrev);
            bool lastFrame = _accelElapsed >= GameConstants.AX_TIME - 0.0001f ||
                             intendedNow >= GameConstants.AX_DIST - 0.0001f;
            
            Vector2 dir = _accelDirection;
            Vector2 bodyOrigin = GetBodyOrigin();
            Vector2 hitBoxSize = new Vector2(GameConstants.AX_HIT_WIDTH, GameConstants.AX_HIT_HEIGHT);
            
            RaycastHit2D enemyHit = Physics2D.BoxCast(
                bodyOrigin,
                hitBoxSize,
                0f,
                dir,
                step,
                LayerMask.GetMask("Enemy"));
            
            if (enemyHit.collider != null)
            {
                Dummy dummy = enemyHit.collider.GetComponent<Dummy>();
                if (dummy != null && dummy.IsBlockingFront(dir, _rb.position, _controller.FacingDirection))
                {
                    float travel = Mathf.Max(0f, enemyHit.distance);
                    _rb.position += dir * travel;
                    CancelAccelIntoFront();
                    return;
                }
                
                var enemy = enemyHit.collider.GetComponent<IDamageable>();
                if (enemy != null)
                {
                    float travel = Mathf.Max(0f, enemyHit.distance);
                    _rb.position += dir * travel;
                    _rb.linearVelocity = Vector2.zero;
                    enemy.TakeDamage();
                    _controller.OnKillEnemy();
                    EndAccel(false);
                    return;
                }
            }
            
            float probe = step + StickProbe;
            if (TryGetBlockingGroundHit(bodyOrigin, hitBoxSize, dir, probe, out RaycastHit2D hit))
            {
                _rb.position += dir * Mathf.Max(0f, hit.distance);
                if (AttachToSurface(hit.collider, hit.normal))
                    return;
            }
            
            _rb.linearVelocity = Vector2.zero;
            _rb.position += dir * step;
            
            if (TryAttachFromTouchedSurface())
                return;
            
            if (lastFrame)
                EndAccel(true);
        }

        /// <summary>
        /// 악셀 종료. 벽/천장에 못 닿으면 대쉬 속도의 20%로 낮춘 뒤 낙하한다.
        /// </summary>
        private void EndAccel(bool keepDashVelocity)
        {
            _isAcceling = false;
            _ignoredStickCollider = null;
            RestoreBodyConstraints();
            _rb.gravityScale = 1f;
            BeginAccelRecovery();

            bool grounded = IsOnGround();
            if (keepDashVelocity && !grounded)
            {
                Vector2 leftover = _accelDirection * GameConstants.AccelSpeed * GameConstants.AX_MISS_SPEED_SCALE;
                _rb.linearVelocity = leftover;
                _controller.SetState(PlayerState.Jump);
                _controller.OnAccelCarryFall(leftover);
                return;
            }

            _rb.linearVelocity = Vector2.zero;
            SetStateAfterAccel();
            _controller.OnAccelCarryFall(Vector2.zero);
        }

        /// <summary>
        /// 전방 대치. 악셀을 취소하고 반대 방향으로 넉백한다.
        /// </summary>
        private void CancelAccelIntoFront()
        {
            _isAcceling = false;
            _ignoredStickCollider = null;
            RestoreBodyConstraints();
            _rb.gravityScale = 1f;
            BeginAccelRecovery();
            Vector2 knockbackDir = -_accelDirection;
            _controller.ApplyFrontBlockKnockback(knockbackDir);
        }

        /// <summary>
        /// 악셀 중단 (역분사)
        /// </summary>
        private void StopAccel()
        {
            _isAcceling = false;
            _ignoredStickCollider = null;
            RestoreBodyConstraints();
            _rb.gravityScale = 1f;
            _rb.linearVelocity = Vector2.zero;
            _controller.OnAccelCarryFall(Vector2.zero);
        }

        /// <summary>
        /// 표면 부착. 발밑 지면 스침은 무시하고 false.
        /// </summary>
        private bool AttachToSurface(Collider2D surface, Vector2 normal)
        {
            Vector2 resolvedNormal = normal.sqrMagnitude > 0.01f ? normal.normalized : Vector2.down;
            if (IsFloorGraze(resolvedNormal, _accelDirection))
                return false;

            _isAcceling = false;
            _ignoredStickCollider = null;
            _stickNormal = resolvedNormal;
            BeginAccelRecovery();
            _controller.OnAccelCarryFall(Vector2.zero);
            
            if (IsFloorNormal(_stickNormal))
            {
                RestoreBodyConstraints();
                _rb.gravityScale = 1f;
                _rb.linearVelocity = Vector2.zero;
                _controller.SetState(PlayerState.Idle);
                return true;
            }
            
            _isSticking = true;
            _stickCollider = surface;
            _stickElapsed = 0f;
            _stickPosition = _rb.position;
            _rb.gravityScale = 0f;
            _rb.linearVelocity = Vector2.zero;
            _rb.constraints = StickConstraints;
            _rb.position = _stickPosition;
            
            if (Mathf.Abs(_stickNormal.x) > 0.7f)
            {
                _stickSurface = SurfaceType.Wall;
                _controller.SetState(PlayerState.WallStick);
            }
            else
            {
                _stickSurface = SurfaceType.Ceiling;
                CacheCeilingExitDir();
                _controller.SetState(PlayerState.CeilingStick);
            }

            return true;
        }

        /// <summary>
        /// 부착 업데이트
        /// </summary>
        private void UpdateStick()
        {
            _rb.linearVelocity = Vector2.zero;
            _rb.position = _stickPosition;
            _rb.gravityScale = 0f;
            _rb.constraints = StickConstraints;
            
            _stickElapsed += Time.fixedDeltaTime;
            
            if (_stickElapsed >= GameConstants.AX_STICK)
            {
                if (_stickSurface == SurfaceType.Wall || _stickSurface == SurfaceType.Ceiling)
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
            RestoreBodyConstraints();
            _rb.gravityScale = 1f;
        }

        /// <summary>
        /// 반대 방향인지 체크
        /// </summary>
        private bool IsOppositeDirection(Vector2 dir1, Vector2 dir2)
        {
            if (dir1.sqrMagnitude < 0.01f || dir2.sqrMagnitude < 0.01f)
                return false;
            
            return Vector2.Dot(dir1.normalized, dir2.normalized) < GameConstants.AX_REVERSE_DOT;
        }

        /// <summary>
        /// 현재 상태에서 악셀 방향이 허용되는지 체크
        /// </summary>
        private bool IsAccelDirectionAllowed(Vector2 dir)
        {
            bool isGrounded = _controller.CurrentState != PlayerState.Jump && 
                            _controller.CurrentState != PlayerState.WallStick && 
                            _controller.CurrentState != PlayerState.CeilingStick;
            
            if (isGrounded)
            {
                if (dir.y < -GameConstants.AX_FORBIDDEN_AXIS)
                    return false;
                
                return true;
            }
            
            if (_controller.CurrentState == PlayerState.CeilingStick)
            {
                if (dir.y >= -GameConstants.AX_FORBIDDEN_AXIS)
                    return false;
                
                return true;
            }
            
            if (_controller.CurrentState == PlayerState.WallStick)
            {
                if (Vector2.Dot(dir, _stickNormal) < -GameConstants.AX_FORBIDDEN_AXIS)
                    return false;
                
                return true;
            }
            
            return true;
        }

        /// <summary>
        /// 악셀 가속 종료 후 재시전 대기
        /// </summary>
        private void BeginAccelRecovery()
        {
            _nextAccelAllowedTime = Time.time + GameConstants.AX_RECOVERY;
        }

        /// <summary>
        /// 천장 이탈 방향. 우상→우하, 좌상→좌하, 수직 상승→악셀 전 이동방향+하단.
        /// </summary>
        private void CacheCeilingExitDir()
        {
            Vector2 inDir = _accelDirection;
            if (inDir.x > GameConstants.AX_FORBIDDEN_AXIS && inDir.y > GameConstants.AX_FORBIDDEN_AXIS)
            {
                _ceilingExitDir = InputHelper.SnapTo8Directions(new Vector2(1f, -1f));
                return;
            }
            
            if (inDir.x < -GameConstants.AX_FORBIDDEN_AXIS && inDir.y > GameConstants.AX_FORBIDDEN_AXIS)
            {
                _ceilingExitDir = InputHelper.SnapTo8Directions(new Vector2(-1f, -1f));
                return;
            }
            
            int moveX = _preAccelMoveX != 0 ? _preAccelMoveX : _controller.FacingDirection;
            if (moveX == 0)
                moveX = 1;
            _ceilingExitDir = InputHelper.SnapTo8Directions(new Vector2(moveX, -1f));
        }

        private Vector2 GetBodyOrigin()
        {
            return _rb.position + Vector2.up * GameConstants.P_BODY_OFFSET_Y;
        }

        private void RestoreBodyConstraints()
        {
            _rb.constraints = BodyConstraints;
        }

        /// <summary>
        /// 악셀 경로가 벽/천장과 겹치면 그 지점에 부착한다.
        /// </summary>
        private bool TryAttachFromTouchedSurface()
        {
            int count = _rb.GetContacts(_contactBuffer);
            Collider2D bestCollider = null;
            Vector2 bestNormal = Vector2.zero;
            float bestScore = float.MaxValue;
            int groundLayer = LayerMask.NameToLayer("Ground");
            
            for (int i = 0; i < count; i++)
            {
                ContactPoint2D contact = _contactBuffer[i];
                if (contact.collider == null)
                    continue;
                if (contact.collider.gameObject.layer != groundLayer)
                    continue;
                if (_ignoredStickCollider != null && contact.collider == _ignoredStickCollider)
                    continue;
                
                Vector2 normal = contact.normal;
                if (IsFloorGraze(normal, _accelDirection))
                    continue;
                
                float intoSurface = Vector2.Dot(_accelDirection, -normal);
                if (intoSurface < 0.05f)
                    continue;
                
                float score = contact.separation;
                if (score < bestScore)
                {
                    bestScore = score;
                    bestCollider = contact.collider;
                    bestNormal = normal;
                }
            }
            
            if (bestCollider != null)
            {
                return AttachToSurface(bestCollider, bestNormal);
            }
            
            Vector2 origin = GetBodyOrigin();
            Vector2 size = new Vector2(
                GameConstants.AX_HIT_WIDTH + 0.08f,
                GameConstants.AX_HIT_HEIGHT + 0.08f);
            Collider2D overlap = Physics2D.OverlapBox(origin, size, 0f, LayerMask.GetMask("Ground"));
            if (overlap == null)
                return false;
            if (_ignoredStickCollider != null && overlap == _ignoredStickCollider)
                return false;
            
            Vector2 closest = overlap.ClosestPoint(origin);
            Vector2 fromSurface = origin - closest;
            Vector2 normalFromOverlap = fromSurface.sqrMagnitude > 0.0001f
                ? fromSurface.normalized
                : -_accelDirection;
            
            if (IsFloorGraze(normalFromOverlap, _accelDirection))
                return false;
            if (Vector2.Dot(_accelDirection, -normalFromOverlap) < 0.05f)
                return false;
            
            return AttachToSurface(overlap, normalFromOverlap);
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
                
                if (IsFloorGraze(candidate.normal, dir))
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

        private static bool IsFloorNormal(Vector2 normal)
        {
            return Vector2.Dot(normal, Vector2.up) > 0.7f;
        }

        /// <summary>
        /// 좌우·상향 악셀이 발밑 지면을 벽/바닥 도달로 오인하지 않게 한다.
        /// 하향 악셀만 지면 착지로 처리한다.
        /// </summary>
        private static bool IsFloorGraze(Vector2 normal, Vector2 dir)
        {
            if (!IsFloorNormal(normal))
                return false;
            
            return dir.y > -GameConstants.AX_FORBIDDEN_AXIS;
        }
    }
}
