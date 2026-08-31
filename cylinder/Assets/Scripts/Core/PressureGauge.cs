using UnityEngine;

namespace Cylinder.Core
{
    /// <summary>
    /// 공압 게이지 관리
    /// - 악셀 자원
    /// - 달리기로 충전, 처치로 획득
    /// </summary>
    public class PressureGauge : MonoBehaviour
    {
        private float _currentGauge;
        
        /// <summary>현재 게이지 값</summary>
        public float Current => _currentGauge;
        
        /// <summary>게이지 최대치</summary>
        public float Max => GameConstants.GAUGE_MAX;
        
        /// <summary>게이지 비율 (0~1)</summary>
        public float Ratio => _currentGauge / GameConstants.GAUGE_MAX;

        private void Awake()
        {
            _currentGauge = GameConstants.GAUGE_START;
        }

        /// <summary>
        /// 게이지 소모 시도
        /// </summary>
        /// <param name="amount">소모량</param>
        /// <returns>소모 성공 여부</returns>
        public bool TryConsume(float amount)
        {
            if (_currentGauge < amount)
                return false;
            
            _currentGauge -= amount;
            return true;
        }

        /// <summary>
        /// 게이지 충전 (달리기)
        /// </summary>
        /// <param name="deltaTime">프레임 시간</param>
        public void ChargeFromRunning(float deltaTime)
        {
            _currentGauge = Mathf.Min(_currentGauge + GameConstants.GAUGE_RUN_RATE * deltaTime, GameConstants.GAUGE_MAX);
        }

        /// <summary>
        /// 게이지 획득 (처치)
        /// </summary>
        public void GainFromKill()
        {
            _currentGauge = Mathf.Min(_currentGauge + GameConstants.GAUGE_KILL, GameConstants.GAUGE_MAX);
        }

        /// <summary>
        /// 게이지 초기화 (리스폰)
        /// </summary>
        public void ResetToStart()
        {
            _currentGauge = GameConstants.GAUGE_START;
        }
    }
}
