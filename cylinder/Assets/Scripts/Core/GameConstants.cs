using UnityEngine;

namespace Cylinder.Core
{
    /// <summary>
    /// 게임 전역 상수 관리 (기획문서 9장 파라미터)
    /// </summary>
    public static class GameConstants
    {
        // 플레이어 이동
        public const float P_MOVE_SPEED = 6f;
        public const float P_MOVE_ACCEL = 40f;
        public const float P_MOVE_DECEL = 50f;
        public const float P_RUN_SPEED = 11f;
        public const float P_JUMP_HEIGHT = 2.2f;
        public const float P_AIR_CONTROL = 0.5f;
        public const float P_BODY_WIDTH = 1.0f;
        public const float P_BODY_HEIGHT = 1.2f;
        public const float P_BODY_OFFSET_Y = 0.6f;
        
        // 악셀 시스템
        public const float AX_COST = 1.0f;
        public const float AX_REVERSE_COST = 0.5f;
        public const float AX_DIST = 6.0f;
        public const float AX_TIME = 0.12f;
        public const float AX_HIT_WIDTH = 1.0f;
        public const float AX_HIT_HEIGHT = 1.2f;
        public const float AX_STICK = 1.0f;
        public const float AX_RECOVERY = 0.1f;
        public const float AX_REVERSE_DOT = -0.9f;
        public const float AX_FORBIDDEN_AXIS = 0.1f;
        public const float AX_MISS_SPEED_SCALE = 0.2f;
        
        // 공압 게이지
        public const float GAUGE_START = 4.0f;
        public const float GAUGE_MAX = 5.0f;
        public const float GAUGE_RUN_RATE = 0.7f;
        public const float GAUGE_KILL = 1.0f;
        
        // 카메라
        public const float CAM_DAMP_X = 0.12f;
        public const float CAM_DAMP_Y = 0.20f;
        
        // 더미
        public const float DUMMY_CHASE_SPEED = 5f;
        public const float DUMMY_STOP_RANGE = 0.6f;
        public const float DUMMY_TURN_TIME = 0.8f;
        public const float DUMMY_AGGRO_RANGE = 8f;
        public const float DUMMY_AGGRO_EXIT = 2f;
        public const float DUMMY_FRONT_BLOCK_DOT = -0.1f;
        public const float P_KNOCKBACK_SPEED = 10f;
        public const float P_STUN_TIME = 0.5f;
        public const float FALL_RESET_Y = -5f;
        
        // 비의도 벽 부착 방지
        public const float WALL_SEPARATION = 0.03f;
        public const float WALL_NORMAL_X = 0.7f;

        public static float AccelSpeed
        {
            get { return AX_DIST / AX_TIME; }
        }
    }
}
