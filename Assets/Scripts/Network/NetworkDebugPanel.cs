using UnityEngine;
using UnityEngine.UI;

namespace Network
{
    public class NetworkDebugPanel : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject panel;
        [SerializeField] private KeyCode toggleKey = KeyCode.F1;

        [Header("Toggles")]
        [SerializeField] private Toggle predictionToggle;
        [SerializeField] private Toggle reconciliationToggle;
        [SerializeField] private Toggle interpolationToggle;
        [SerializeField] private Toggle debugModeToggle;

        [Header("Sliders")]
        [SerializeField] private Slider minLatencySlider;
        [SerializeField] private Slider maxLatencySlider;
        [SerializeField] private Slider packetLossSlider;
        [SerializeField] private Slider interpolationDelaySlider;

        [Header("Labels")]
        [SerializeField] private Text minLatencyLabel;
        [SerializeField] private Text maxLatencyLabel;
        [SerializeField] private Text packetLossLabel;
        [SerializeField] private Text interpolationDelayLabel;
        [SerializeField] private Text statsLabel;

        private NetworkManager networkManager;
        private bool isInitialized;

        private void Start()
        {
            networkManager = NetworkManager.Instance;
            
            if (networkManager)
            {
                networkManager.OnNetworkReady += Initialize;
                if (networkManager.IsInitialized)
                {
                    Initialize();
                }
            }

            if (panel == null)
            {
                CreateDebugPanel();
            }

            if (panel != null)
            {
                panel.SetActive(false);
            }
        }

        private void Initialize()
        {
            SetupToggles();
            SetupSliders();
            isInitialized = true;
        }

        private void SetupToggles()
        {
            if (predictionToggle != null)
            {
                predictionToggle.isOn = true;
                predictionToggle.onValueChanged.AddListener(OnPredictionToggled);
            }

            if (reconciliationToggle != null)
            {
                reconciliationToggle.isOn = true;
                reconciliationToggle.onValueChanged.AddListener(OnReconciliationToggled);
            }

            if (interpolationToggle != null)
            {
                interpolationToggle.isOn = true;
                interpolationToggle.onValueChanged.AddListener(OnInterpolationToggled);
            }

            if (debugModeToggle != null)
            {
                debugModeToggle.isOn = false;
                debugModeToggle.onValueChanged.AddListener(OnDebugModeToggled);
            }
        }

        private void SetupSliders()
        {
            if (minLatencySlider != null)
            {
                minLatencySlider.minValue = 0;
                minLatencySlider.maxValue = 0.5f;
                minLatencySlider.value = 0.05f;
                minLatencySlider.onValueChanged.AddListener(OnMinLatencyChanged);
            }

            if (maxLatencySlider != null)
            {
                maxLatencySlider.minValue = 0;
                maxLatencySlider.maxValue = 1f;
                maxLatencySlider.value = 0.15f;
                maxLatencySlider.onValueChanged.AddListener(OnMaxLatencyChanged);
            }

            if (packetLossSlider != null)
            {
                packetLossSlider.minValue = 0;
                packetLossSlider.maxValue = 0.5f;
                packetLossSlider.value = 0;
                packetLossSlider.onValueChanged.AddListener(OnPacketLossChanged);
            }

            if (interpolationDelaySlider != null)
            {
                interpolationDelaySlider.minValue = 0.05f;
                interpolationDelaySlider.maxValue = 0.5f;
                interpolationDelaySlider.value = 0.1f;
                interpolationDelaySlider.onValueChanged.AddListener(OnInterpolationDelayChanged);
            }
        }

        private void Update()
        {
            if (UnityEngine.Input.GetKeyDown(toggleKey))
            {
                TogglePanel();
            }

            if (isInitialized && panel != null && panel.activeSelf)
            {
                UpdateStats();
            }
        }

        public void TogglePanel()
        {
            if (panel != null)
            {
                panel.SetActive(!panel.activeSelf);
            }
        }

        private void OnPredictionToggled(bool value)
        {
            networkManager?.SetPredictionEnabled(value);
        }

        private void OnReconciliationToggled(bool value)
        {
            networkManager?.SetReconciliationEnabled(value);
        }

        private void OnInterpolationToggled(bool value)
        {
            networkManager?.SetInterpolationEnabled(value);
        }

        private void OnDebugModeToggled(bool value)
        {
            networkManager?.SetDebugMode(value);
        }

        private void OnMinLatencyChanged(float value)
        {
            if (minLatencyLabel != null)
            {
                minLatencyLabel.text = $"Min Latency: {value * 1000:F0}ms";
            }
            
            float maxValue = maxLatencySlider != null ? maxLatencySlider.value : value;
            networkManager?.SetLatencySettings(value, Mathf.Max(value, maxValue));
        }

        private void OnMaxLatencyChanged(float value)
        {
            if (maxLatencyLabel != null)
            {
                maxLatencyLabel.text = $"Max Latency: {value * 1000:F0}ms";
            }

            float minValue = minLatencySlider != null ? minLatencySlider.value : 0;
            networkManager?.SetLatencySettings(minValue, Mathf.Max(minValue, value));
        }

        private void OnPacketLossChanged(float value)
        {
            if (packetLossLabel != null)
            {
                packetLossLabel.text = $"Packet Loss: {value * 100:F0}%";
            }
            networkManager?.SetPacketLoss(value);
        }

        private void OnInterpolationDelayChanged(float value)
        {
            if (interpolationDelayLabel != null)
            {
                interpolationDelayLabel.text = $"Interp Delay: {value * 1000:F0}ms";
            }
            networkManager?.SetInterpolationDelay(value);
        }

        private void UpdateStats()
        {
            if (statsLabel == null || networkManager?.Client == null) return;

            var client = networkManager.Client;
            string stats = $"Game Running: {client.IsRunning}\n";
            stats += $"Time Left: {client.RemainingTime:F1}s\n";

            int playerCount = 0;
            foreach (var _ in client.GetAllPlayers())
            {
                playerCount++;
            }
            stats += $"Players: {playerCount}";

            statsLabel.text = stats;
        }

        private void CreateDebugPanel()
        {
            var canvas = FindFirstObjectByType<Canvas>();
            if (!canvas)
            {
                var canvasObj = new GameObject("Debug Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            panel = new GameObject("Debug Panel");
            panel.transform.SetParent(canvas.transform, false);

            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 0);
            rect.anchorMax = new Vector2(1, 0);
            rect.pivot = new Vector2(1, 0);
            rect.anchoredPosition = new Vector2(-10, 10);
            rect.sizeDelta = new Vector2(280, 350);

            var bg = panel.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.85f);

            var layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(15, 15, 15, 15);
            layout.spacing = 8;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            CreateLabel(panel.transform, "Network Debug (F1)", 18, FontStyle.Bold);
            CreateToggle(panel.transform, "Prediction", ref predictionToggle);
            CreateToggle(panel.transform, "Reconciliation", ref reconciliationToggle);
            CreateToggle(panel.transform, "Interpolation", ref interpolationToggle);
            CreateToggle(panel.transform, "Debug Logging", ref debugModeToggle);

            CreateSliderWithLabel(panel.transform, "Min Latency: 50ms", ref minLatencySlider, ref minLatencyLabel);
            CreateSliderWithLabel(panel.transform, "Max Latency: 150ms", ref maxLatencySlider, ref maxLatencyLabel);
            CreateSliderWithLabel(panel.transform, "Packet Loss: 0%", ref packetLossSlider, ref packetLossLabel);
            CreateSliderWithLabel(panel.transform, "Interp Delay: 100ms", ref interpolationDelaySlider, ref interpolationDelayLabel);

            statsLabel = CreateLabel(panel.transform, "Stats...", 12, FontStyle.Normal);
        }

        private Text CreateLabel(Transform parent, string text, int fontSize, FontStyle style)
        {
            var obj = new GameObject("Label");
            obj.transform.SetParent(parent, false);

            var textComp = obj.AddComponent<Text>();
            textComp.text = text;
            textComp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComp.fontSize = fontSize;
            textComp.fontStyle = style;
            textComp.color = Color.white;

            var layoutElem = obj.AddComponent<LayoutElement>();
            layoutElem.minHeight = fontSize + 8;

            return textComp;
        }

        private void CreateToggle(Transform parent, string label, ref Toggle toggle)
        {
            var obj = new GameObject(label + " Toggle");
            obj.transform.SetParent(parent, false);

            var rect = obj.AddComponent<RectTransform>();
            
            var layout = obj.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.childAlignment = TextAnchor.MiddleLeft;

            var toggleObj = new GameObject("Toggle");
            toggleObj.transform.SetParent(obj.transform, false);

            var toggleBg = new GameObject("Background");
            toggleBg.transform.SetParent(toggleObj.transform, false);
            var bgImage = toggleBg.AddComponent<Image>();
            bgImage.color = new Color(0.3f, 0.3f, 0.3f);
            var bgRect = toggleBg.GetComponent<RectTransform>();
            bgRect.sizeDelta = new Vector2(20, 20);

            var checkmark = new GameObject("Checkmark");
            checkmark.transform.SetParent(toggleBg.transform, false);
            var checkImage = checkmark.AddComponent<Image>();
            checkImage.color = Color.green;
            var checkRect = checkmark.GetComponent<RectTransform>();
            checkRect.anchorMin = Vector2.zero;
            checkRect.anchorMax = Vector2.one;
            checkRect.sizeDelta = new Vector2(-6, -6);

            toggle = toggleObj.AddComponent<Toggle>();
            toggle.targetGraphic = bgImage;
            toggle.graphic = checkImage;
            toggle.isOn = true;

            var toggleLayout = toggleObj.AddComponent<LayoutElement>();
            toggleLayout.minWidth = 20;
            toggleLayout.minHeight = 20;

            var labelObj = new GameObject("Label");
            labelObj.transform.SetParent(obj.transform, false);
            var labelText = labelObj.AddComponent<Text>();
            labelText.text = label;
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 14;
            labelText.color = Color.white;

            var labelLayout = labelObj.AddComponent<LayoutElement>();
            labelLayout.flexibleWidth = 1;
            labelLayout.minHeight = 20;

            var objLayout = obj.AddComponent<LayoutElement>();
            objLayout.minHeight = 25;
        }

        private void CreateSliderWithLabel(Transform parent, string labelText, ref Slider slider, ref Text label)
        {
            var container = new GameObject("Slider Container");
            container.transform.SetParent(parent, false);

            var layout = container.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 2;

            label = CreateLabel(container.transform, labelText, 12, FontStyle.Normal);

            var sliderObj = new GameObject("Slider");
            sliderObj.transform.SetParent(container.transform, false);

            var sliderRect = sliderObj.AddComponent<RectTransform>();
            
            var bg = new GameObject("Background");
            bg.transform.SetParent(sliderObj.transform, false);
            var bgImage = bg.AddComponent<Image>();
            bgImage.color = new Color(0.3f, 0.3f, 0.3f);
            var bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            var fill = new GameObject("Fill Area");
            fill.transform.SetParent(sliderObj.transform, false);
            var fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = new Vector2(-20, 0);

            var fillImage = new GameObject("Fill");
            fillImage.transform.SetParent(fill.transform, false);
            var fillImg = fillImage.AddComponent<Image>();
            fillImg.color = new Color(0.2f, 0.6f, 0.2f);
            var fillImgRect = fillImage.GetComponent<RectTransform>();
            fillImgRect.anchorMin = Vector2.zero;
            fillImgRect.anchorMax = Vector2.one;
            fillImgRect.sizeDelta = Vector2.zero;

            var handle = new GameObject("Handle Slide Area");
            handle.transform.SetParent(sliderObj.transform, false);
            var handleRect = handle.AddComponent<RectTransform>();
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.one;
            handleRect.sizeDelta = new Vector2(-20, 0);

            var handleObj = new GameObject("Handle");
            handleObj.transform.SetParent(handle.transform, false);
            var handleImage = handleObj.AddComponent<Image>();
            handleImage.color = Color.white;
            var handleObjRect = handleObj.GetComponent<RectTransform>();
            handleObjRect.sizeDelta = new Vector2(20, 0);

            slider = sliderObj.AddComponent<Slider>();
            slider.fillRect = fillImgRect;
            slider.handleRect = handleObjRect;
            slider.targetGraphic = handleImage;

            var sliderLayout = sliderObj.AddComponent<LayoutElement>();
            sliderLayout.minHeight = 20;

            var containerLayout = container.AddComponent<LayoutElement>();
            containerLayout.minHeight = 40;
        }
    }
}