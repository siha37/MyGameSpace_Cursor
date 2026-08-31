using UnityEngine;
using Cylinder.Core;

namespace Cylinder.CameraSystem
{
    /// <summary>
    /// 플레이어를 부드럽게 따라가는 카메라
    /// - 수평/수직 감쇠 적용
    /// </summary>
    public class SmoothFollowCamera : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform _target;
        
        [Header("Settings")]
        [SerializeField] private float _zOffset = -10f;
        
        private Vector3 _velocityX;
        private Vector3 _velocityY;

        private void LateUpdate()
        {
            if (_target == null)
                return;
            
            Vector3 currentPos = transform.position;
            Vector3 targetPos = _target.position;
            
            // 수평 추적 (더 빠름)
            float smoothX = Mathf.SmoothDamp(
                currentPos.x, 
                targetPos.x, 
                ref _velocityX.x, 
                GameConstants.CAM_DAMP_X
            );
            
            // 수직 추적 (더 느림)
            float smoothY = Mathf.SmoothDamp(
                currentPos.y, 
                targetPos.y, 
                ref _velocityY.y, 
                GameConstants.CAM_DAMP_Y
            );
            
            transform.position = new Vector3(smoothX, smoothY, targetPos.z + _zOffset);
        }

        /// <summary>
        /// 타겟 설정
        /// </summary>
        public void SetTarget(Transform target)
        {
            _target = target;
        }
    }
}
