using UnityEngine;

namespace Cylinder.Core
{
    /// <summary>
    /// 입력 처리 헬퍼
    /// - 이동 입력
    /// - 악셀 8방향
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
        /// WASD·방향키를 8방향으로 스냅한 악셀 조준
        /// </summary>
        public static Vector2 GetAccelAimInput()
        {
            float horizontal = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) horizontal -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) horizontal += 1f;
            
            float vertical = 0f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) vertical += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) vertical -= 1f;
            
            Vector2 raw = new Vector2(horizontal, vertical);
            if (raw.sqrMagnitude < 0.01f)
                return Vector2.zero;
            
            return SnapTo8Directions(raw);
        }

        /// <summary>
        /// 입력을 8방향 단위 벡터로 스냅
        /// </summary>
        public static Vector2 SnapTo8Directions(Vector2 input)
        {
            if (input.sqrMagnitude < 0.01f)
                return Vector2.zero;
            
            float angle = Mathf.Atan2(input.y, input.x);
            float step = Mathf.PI / 4f;
            float snapped = Mathf.Round(angle / step) * step;
            return new Vector2(Mathf.Cos(snapped), Mathf.Sin(snapped));
        }

        /// <summary>
        /// Shift 탭
        /// </summary>
        public static bool IsAccelPressedThisFrame()
        {
            return Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift);
        }
    }
}
