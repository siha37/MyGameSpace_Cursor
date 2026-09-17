using UnityEngine;
using UnityEngine.UI;

namespace Cylinder.UI
{
    /// <summary>
    /// 우측 상단 Help 버튼과 조작법 팝업
    /// </summary>
    public class HelpUI : MonoBehaviour
    {
        public const string HelpBodyText =
            "【이동】\n" +
            "A / D : 질주 (누르는 동안 공압 게이지 충전)\n" +
            "Space : 점프 (질주 중 점프하면 더 멀리 나감)\n\n" +
            "【악셀 — 유일한 공격】\n" +
            "WASD 또는 방향키로 8방향을 누른 채 Shift\n" +
            "게이지 1칸 소모. 경로의 첫 적만 처형\n\n" +
            "지상에서는 아래쪽 악셀 불가\n" +
            "벽/천장에 닿으면 1초 부착 (점프 또는 악셀로 이탈)\n" +
            "천장에서는 방향키 없이 Shift만:\n" +
            "  우상으로 붙으면 우하 / 좌상으로 붙으면 좌하\n" +
            "  수직 상승으로 붙으면 직전 이동방향+하단\n" +
            "천장에서 좌우 수평 악셀 불가\n" +
            "악셀 중 반대 방향으로 Shift : 역분사 (0.5칸, 즉시 정지)\n" +
            "벽/천장에 못 닿으면 속도 20%로 낙하\n\n" +
            "【허수아비】\n" +
            "노란 테두리 = 전방 (+X / -X)\n" +
            "일정 거리 안에 들어오기 전에는 대기, 들어오면 추적\n" +
            "방향을 바꿀 때 0.8초 정지한 뒤 노란 표기가 반대쪽으로 바뀌고 걷는다\n" +
            "정면으로 악셀하면 취소 + 넉백 + 0.5초 조작 불가\n" +
            "뒤로 들어가 악셀하면 처형 (게이지 +1). 죽으면 다시 안 나옴\n\n" +
            "【게이지】\n" +
            "시작 4 / 최대 5\n" +
            "질주 중 충전 · 처치 +1 · 악셀 -1 · 역분사 -0.5";

        [SerializeField] private Button _helpButton;
        [SerializeField] private GameObject _popupRoot;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _overlayButton;

        private float _savedTimeScale = 1f;

        /// <summary>조작법 팝업이 열려 있으면 플레이어 입력을 막는다.</summary>
        public static bool IsOpen { get; private set; }

        /// <summary>
        /// 씬에 HelpUI가 없으면 Canvas에 붙인다.
        /// </summary>
        public static void EnsureInScene()
        {
            HelpUI existing = FindFirstObjectByType<HelpUI>();
            if (existing != null)
            {
                existing.BuildIfMissing();
                return;
            }

            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
                return;

            HelpUI help = canvas.gameObject.AddComponent<HelpUI>();
            help.BuildIfMissing();
        }

        private void Awake()
        {
            BuildIfMissing();
            WireButtons();
            SetPopupOpen(false, false);
        }

        private void OnDisable()
        {
            if (IsOpen)
                RestoreTimeScale();
            IsOpen = false;
        }

        private void Update()
        {
            if (!IsOpen)
                return;

            if (Input.GetKeyDown(KeyCode.Escape))
                ClosePopup();
        }

        /// <summary>
        /// Help 버튼 / 팝업 계층이 없으면 만든다.
        /// </summary>
        public void BuildIfMissing()
        {
            if (_helpButton == null)
            {
                Transform found = transform.Find("HelpButton");
                if (found != null)
                    _helpButton = found.GetComponent<Button>();
            }

            if (_popupRoot == null)
            {
                Transform found = transform.Find("HelpPopup");
                if (found != null)
                    _popupRoot = found.gameObject;
            }

            if (_helpButton == null)
                _helpButton = CreateHelpButton();

            if (_popupRoot == null)
                _popupRoot = CreatePopup();

            if (_closeButton == null && _popupRoot != null)
            {
                Transform close = _popupRoot.transform.Find("HelpPanel/CloseButton");
                if (close != null)
                    _closeButton = close.GetComponent<Button>();
            }

            if (_overlayButton == null && _popupRoot != null)
                _overlayButton = _popupRoot.GetComponent<Button>();

            RefreshHelpBody();
        }

        /// <summary>
        /// 조작법 팝업 열기
        /// </summary>
        public void OpenPopup()
        {
            SetPopupOpen(true, true);
        }

        /// <summary>
        /// 조작법 팝업 닫기
        /// </summary>
        public void ClosePopup()
        {
            SetPopupOpen(false, true);
        }

        private void WireButtons()
        {
            if (_helpButton != null)
            {
                _helpButton.onClick.RemoveListener(OpenPopup);
                _helpButton.onClick.AddListener(OpenPopup);
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(ClosePopup);
                _closeButton.onClick.AddListener(ClosePopup);
            }

            if (_overlayButton != null)
            {
                _overlayButton.onClick.RemoveListener(ClosePopup);
                _overlayButton.onClick.AddListener(ClosePopup);
            }
        }

        private void SetPopupOpen(bool open, bool changeTimeScale)
        {
            IsOpen = open;
            if (_popupRoot != null)
            {
                _popupRoot.SetActive(open);
                if (open)
                    _popupRoot.transform.SetAsLastSibling();
            }

            if (!changeTimeScale)
                return;

            if (open)
            {
                _savedTimeScale = Time.timeScale;
                if (_savedTimeScale <= 0.01f)
                    _savedTimeScale = 1f;
                Time.timeScale = 0f;
            }
            else
            {
                RestoreTimeScale();
            }
        }

        private void RestoreTimeScale()
        {
            Time.timeScale = _savedTimeScale > 0.01f ? _savedTimeScale : 1f;
        }

        private Button CreateHelpButton()
        {
            GameObject buttonGo = CreateUiObject("HelpButton", transform);
            Image image = buttonGo.AddComponent<Image>();
            image.color = new Color(0.12f, 0.12f, 0.14f, 0.92f);
            Button button = buttonGo.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.25f, 0.25f, 0.3f, 1f);
            colors.pressedColor = new Color(0.08f, 0.08f, 0.1f, 1f);
            button.colors = colors;

            RectTransform rt = buttonGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-16f, -16f);
            rt.sizeDelta = new Vector2(110f, 40f);

            GameObject labelGo = CreateUiObject("Label", buttonGo.transform);
            Text label = labelGo.AddComponent<Text>();
            ApplyFont(label, 22);
            label.text = "HELP";
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            Stretch(labelGo.GetComponent<RectTransform>());
            return button;
        }

        private GameObject CreatePopup()
        {
            GameObject overlay = CreateUiObject("HelpPopup", transform);
            Image dim = overlay.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.65f);
            Button overlayButton = overlay.AddComponent<Button>();
            overlayButton.transition = Selectable.Transition.None;
            _overlayButton = overlayButton;
            Stretch(overlay.GetComponent<RectTransform>());

            GameObject panel = CreateUiObject("HelpPanel", overlay.transform);
            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.1f, 0.1f, 0.12f, 0.98f);
            RectTransform panelRt = panel.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.anchoredPosition = Vector2.zero;
            panelRt.sizeDelta = new Vector2(720f, 560f);

            GameObject titleGo = CreateUiObject("Title", panel.transform);
            Text title = titleGo.AddComponent<Text>();
            ApplyFont(title, 28);
            title.text = "조작법";
            title.alignment = TextAnchor.MiddleCenter;
            title.color = new Color(1f, 0.86f, 0.12f, 1f);
            RectTransform titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -16f);
            titleRt.sizeDelta = new Vector2(-32f, 44f);

            GameObject bodyGo = CreateUiObject("Body", panel.transform);
            Text body = bodyGo.AddComponent<Text>();
            ApplyFont(body, 18);
            body.text = HelpBodyText;
            body.alignment = TextAnchor.UpperLeft;
            body.color = Color.white;
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform bodyRt = bodyGo.GetComponent<RectTransform>();
            bodyRt.anchorMin = new Vector2(0f, 0f);
            bodyRt.anchorMax = new Vector2(1f, 1f);
            bodyRt.offsetMin = new Vector2(28f, 72f);
            bodyRt.offsetMax = new Vector2(-28f, -68f);

            GameObject closeGo = CreateUiObject("CloseButton", panel.transform);
            Image closeImage = closeGo.AddComponent<Image>();
            closeImage.color = new Color(0.22f, 0.22f, 0.26f, 1f);
            Button close = closeGo.AddComponent<Button>();
            _closeButton = close;
            RectTransform closeRt = closeGo.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(0.5f, 0f);
            closeRt.anchorMax = new Vector2(0.5f, 0f);
            closeRt.pivot = new Vector2(0.5f, 0f);
            closeRt.anchoredPosition = new Vector2(0f, 16f);
            closeRt.sizeDelta = new Vector2(160f, 40f);

            GameObject closeLabelGo = CreateUiObject("Label", closeGo.transform);
            Text closeLabel = closeLabelGo.AddComponent<Text>();
            ApplyFont(closeLabel, 20);
            closeLabel.text = "닫기";
            closeLabel.alignment = TextAnchor.MiddleCenter;
            closeLabel.color = Color.white;
            Stretch(closeLabelGo.GetComponent<RectTransform>());

            overlay.SetActive(false);
            return overlay;
        }

        private void RefreshHelpBody()
        {
            if (_popupRoot == null)
                return;

            Transform body = _popupRoot.transform.Find("HelpPanel/Body");
            if (body == null)
                return;

            Text text = body.GetComponent<Text>();
            if (text != null)
                text.text = HelpBodyText;
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void ApplyFont(Text text, int size)
        {
            text.font = GetUiFont();
            text.fontSize = size;
            text.supportRichText = false;
        }

        /// <summary>
        /// 한글이 보이는 OS 폰트
        /// </summary>
        public static Font GetUiFont()
        {
            Font osFont = Font.CreateDynamicFontFromOSFont(
                new[] { "Malgun Gothic", "맑은 고딕", "Noto Sans CJK KR", "Apple SD Gothic Neo", "Arial Unicode MS" },
                24);
            if (osFont != null)
                return osFont;

            Font builtin = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (builtin != null)
                return builtin;

            return Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }
}
