using UnityEngine;
using UnityEngine.UI;
using Cylinder.Core;

namespace Cylinder.UI
{
    /// <summary>
    /// 공압 게이지 UI 표시
    /// </summary>
    public class GaugeUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PressureGauge _gauge;
        [SerializeField] private Image _fillImage;
        [SerializeField] private Text _gaugeText;

        private void Update()
        {
            if (_gauge == null)
                return;
            
            // 게이지 바 업데이트
            if (_fillImage != null)
            {
                _fillImage.fillAmount = _gauge.Ratio;
            }
            
            // 텍스트 업데이트
            if (_gaugeText != null)
            {
                _gaugeText.text = $"{_gauge.Current:F1} / {_gauge.Max:F0}";
            }
        }

        /// <summary>
        /// 게이지 참조 설정
        /// </summary>
        public void SetGauge(PressureGauge gauge)
        {
            _gauge = gauge;
        }
    }
}
