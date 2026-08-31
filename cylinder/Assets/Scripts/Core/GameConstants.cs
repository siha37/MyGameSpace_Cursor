using UnityEngine;

namespace Cylinder.Core
{
    /// <summary>
    /// 게임 전역 상수 관리 (기획문서 9장 파라미터)
    /// </summary>
    public static class GameConstants
    {
        // 플레이어 이동
        public const float P_MOVE_SPEED = 6f;           // 보행 최고 속도
        public const float P_MOVE_ACCEL = 40f;          // 보행 가속 (/s²)
        public const float P_MOVE_DECEL = 50f;          // 방향 전환 감속 (/s²)
        public const float P_RUN_TAP = 0.25f;           // 달리기 2연타 윈도우 (s)
        public const float P_RUN_SPEED = 11f;           // 달리기 최고 속도
        public const float P_JUMP_HEIGHT = 2.2f;        // 점프 높이
        public const float P_AIR_CONTROL = 0.5f;        // 공중 좌우 가속 (지상의 50%)
        
        // 플레이어 공격
        public const float P_ATK_RANGE = 1.4f;          // 공격 전방 거리
        public const float P_ATK_WIDTH = 0.8f;          // 공격 두께
        public const float P_ATK_ACTIVE = 0.08f;        // 공격 판정 유지 (s)
        public const float P_ATK_RECOVER = 0.12f;       // 후딜 (s)
        
        // 악셀 시스템
        public const float AX_COST = 1.0f;              // 악셀 소모
        public const float AX_REVERSE_COST = 0.5f;      // 역분사 소모
        public const float AX_DIST = 4.0f;              // 악셀 거리
        public const float AX_TIME = 0.12f;             // 악셀 소요 시간 (s)
        public const float AX_HIT_WIDTH = 0.6f;         // 경로 판정 두께
        public const float AX_STICK = 1.0f;             // 벽/천장 부착 시간 (s)
        
        // 공압 게이지
        public const float GAUGE_START = 2.0f;          // 시작 게이지
        public const float GAUGE_MAX = 4.0f;            // 게이지 최대
        public const float GAUGE_RUN_RATE = 0.7f;       // 달리기 충전 속도 (/s)
        public const float GAUGE_KILL = 1.0f;           // 처치 충전
        
        // 카메라
        public const float CAM_DAMP_X = 0.12f;          // 카메라 수평 감쇠 (s)
        public const float CAM_DAMP_Y = 0.20f;          // 카메라 수직 감쇠 (s)
        
        // 더미/리스폰
        public const float DUMMY_RESPAWN = 3.0f;        // 허수아비 리스폰 (s)
        public const float FALL_RESET_Y = -5f;          // 낙하 리셋 (맵 최저점 기준 상대값)
    }
}
