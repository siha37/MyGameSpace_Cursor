using UnityEngine;

namespace Cylinder.Core
{
    /// <summary>
    /// 입력 처리 헬퍼
    /// - 방향 입력 정규화
    /// - 8방향 스냅
    /// </summary>
    public static class InputHelper
    {
        /// <summary>
        /// WASD 입력을 2D 벡터로 변환
        /// </summary>
        public static Vector2 GetMovementInput()
        {
            float horizontal = 0f;
            if (Input.GetKey(KeyCode.A)) horizontal -= 1f;
            if (Input.GetKey(KeyCode.D)) horizontal += 1f;
            
            float vertical = 0f;
            if (Input.GetKey(KeyCode.W)) vertical += 1f;
            if (Input.GetKey(KeyCode.S)) vertical -= 1f;
            
            return new Vector2(horizontal, vertical);
        }

        /// <summary>
        /// 벡터를 8방향 중 하나로 스냅
        /// </summary>
        public static Vector2 SnapTo8Directions(Vector2 input)
        {
            if (input.magnitude < 0.1f)
                return Vector2.zero;
            
            // 8방향 각도 계산
            float angle = Mathf.Atan2(input.y, input.x) * Mathf.Rad2Deg;
            
            // 45도 단위로 스냅
            float snappedAngle = Mathf.Round(angle / 45f) * 45f;
            float rad = snappedAngle * Mathf.Deg2Rad;
            
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;
        }

        /// <summary>
        /// Shift 키 눌림 여부
        /// </summary>
        public static bool IsShiftPressed()
        {
            return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        }
    }
}
