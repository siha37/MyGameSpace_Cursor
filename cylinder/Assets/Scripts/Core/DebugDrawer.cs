using UnityEngine;

namespace Cylinder.Core
{
    /// <summary>
    /// 디버그 시각화 유틸리티
    /// </summary>
    public static class DebugDrawer
    {
        /// <summary>
        /// 공격 범위 그리기
        /// </summary>
        public static void DrawAttackRange(Vector3 origin, Vector2 direction, float range, float width, Color color)
        {
#if UNITY_EDITOR
            Vector3 perpendicular = new Vector3(-direction.y, direction.x, 0) * width * 0.5f;
            Vector3 forward = new Vector3(direction.x, direction.y, 0) * range;
            
            Vector3 p1 = origin + perpendicular;
            Vector3 p2 = origin - perpendicular;
            Vector3 p3 = origin + forward + perpendicular;
            Vector3 p4 = origin + forward - perpendicular;
            
            Debug.DrawLine(p1, p2, color);
            Debug.DrawLine(p3, p4, color);
            Debug.DrawLine(p1, p3, color);
            Debug.DrawLine(p2, p4, color);
#endif
        }

        /// <summary>
        /// 악셀 경로 그리기
        /// </summary>
        public static void DrawAccelPath(Vector3 origin, Vector2 direction, float distance, Color color)
        {
#if UNITY_EDITOR
            Vector3 end = origin + new Vector3(direction.x, direction.y, 0) * distance;
            Debug.DrawLine(origin, end, color);
            
            // 화살표
            Vector3 arrowBase = end - new Vector3(direction.x, direction.y, 0) * 0.2f;
            Vector3 perpendicular = new Vector3(-direction.y, direction.x, 0) * 0.1f;
            Debug.DrawLine(end, arrowBase + perpendicular, color);
            Debug.DrawLine(end, arrowBase - perpendicular, color);
#endif
        }
    }
}
