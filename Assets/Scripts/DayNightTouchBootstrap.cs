using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Light))]
public class DayNightTouchBootstrap : MonoBehaviour
{
    private DayNightCycleController controller;
    private Slider timeSlider;
    private Text timeLabel;
    private Text stateLabel;
    private Button playButton;
    private Text playButtonLabel;
    private bool uiBuilt;

    private void Awake()
    {
        controller = GetComponent<DayNightCycleController>();
        if (controller == null)
        {
            controller = gameObject.AddComponent<DayNightCycleController>();
        }

        controller.sunLight = GetComponent<Light>();
        controller.targetCamera = Camera.main;

        EnsureEventSystem();
        EnsureCanvas();
        RefreshUi();
    }

    private void Update()
    {
        if (!uiBuilt)
        {
            EnsureCanvas();
        }

        RefreshUi();
    }

    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        eventSystem.transform.SetParent(null);
    }

    private void EnsureCanvas()
    {
        Canvas existingCanvas = FindObjectOfType<Canvas>();
        if (existingCanvas != null && existingCanvas.transform.Find("DayNightHUD") != null)
        {
            uiBuilt = true;
            return;
        }

        Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        Sprite sprite = Resources.GetBuiltinResource<Sprite>("UISprite.psd");

        GameObject canvasObject = existingCanvas != null ? existingCanvas.gameObject : new GameObject("RuntimeCanvas");
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = canvasObject.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        if (canvasObject.GetComponent<GraphicRaycaster>() == null)
        {
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        GameObject panel = CreateUiObject("DayNightHUD", canvas.transform, typeof(Image));
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.sizeDelta = new Vector2(760f, 190f);
        panelRect.anchoredPosition = new Vector2(0f, 24f);
        Image panelImage = panel.GetComponent<Image>();
        panelImage.sprite = sprite;
        panelImage.type = Image.Type.Sliced;
        panelImage.color = new Color(0.05f, 0.08f, 0.12f, 0.82f);

        CreateText("Title", panel.transform, font, 30, TextAnchor.MiddleCenter, "Simulador solar de unidad", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -26f), new Vector2(680f, 36f));
        timeLabel = CreateText("TimeLabel", panel.transform, font, 28, TextAnchor.MiddleCenter, string.Empty, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -66f), new Vector2(260f, 34f));
        stateLabel = CreateText("StateLabel", panel.transform, font, 20, TextAnchor.MiddleCenter, string.Empty, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -96f), new Vector2(320f, 26f));

        timeSlider = CreateSlider(panel.transform, sprite);
        RectTransform sliderRect = timeSlider.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.5f, 0f);
        sliderRect.anchorMax = new Vector2(0.5f, 0f);
        sliderRect.pivot = new Vector2(0.5f, 0f);
        sliderRect.sizeDelta = new Vector2(540f, 40f);
        sliderRect.anchoredPosition = new Vector2(0f, 54f);
        timeSlider.minValue = 0f;
        timeSlider.maxValue = 24f;
        timeSlider.wholeNumbers = false;
        timeSlider.onValueChanged.AddListener(OnSliderChanged);

        CreateHourMarker(panel.transform, font, "00:00", new Vector2(-270f, 24f));
        CreateHourMarker(panel.transform, font, "06:00", new Vector2(-136f, 24f));
        CreateHourMarker(panel.transform, font, "12:00", new Vector2(0f, 24f));
        CreateHourMarker(panel.transform, font, "18:00", new Vector2(136f, 24f));
        CreateHourMarker(panel.transform, font, "24:00", new Vector2(270f, 24f));

        CreateStepButton(panel.transform, font, sprite, "-15m", new Vector2(-318f, 56f), -0.25f);
        CreateStepButton(panel.transform, font, sprite, "+15m", new Vector2(318f, 56f), 0.25f);
        playButton = CreateActionButton(panel.transform, font, sprite, "PlayButton", new Vector2(-235f, 138f), "▶ Auto", TogglePlayback);
        playButtonLabel = playButton.GetComponentInChildren<Text>();
        CreateActionButton(panel.transform, font, sprite, "MorningButton", new Vector2(-65f, 138f), "08:00", delegate { controller.SetTimeOfDay(8f); RefreshUi(); });
        CreateActionButton(panel.transform, font, sprite, "NoonButton", new Vector2(65f, 138f), "12:00", delegate { controller.SetTimeOfDay(12f); RefreshUi(); });
        CreateActionButton(panel.transform, font, sprite, "SunsetButton", new Vector2(195f, 138f), "18:30", delegate { controller.SetTimeOfDay(18.5f); RefreshUi(); });

        uiBuilt = true;
    }

    private void OnSliderChanged(float hour)
    {
        controller.SetTimeOfDay(hour);
        RefreshUi();
    }

    private void TogglePlayback()
    {
        controller.TogglePlayback();
        RefreshUi();
    }

    private void RefreshUi()
    {
        if (controller == null)
        {
            return;
        }

        if (timeSlider != null)
        {
            if (Mathf.Abs(timeSlider.value - controller.currentHour) > 0.001f)
            {
                timeSlider.onValueChanged.RemoveListener(OnSliderChanged);
                timeSlider.value = controller.currentHour;
                timeSlider.onValueChanged.AddListener(OnSliderChanged);
            }
        }

        if (timeLabel != null)
        {
            timeLabel.text = "Hora: " + controller.GetFormattedHour();
        }

        if (stateLabel != null)
        {
            stateLabel.text = controller.IsDaylight ? "Sol visible" : "Luna visible";
        }

        if (playButtonLabel != null)
        {
            playButtonLabel.text = controller.playTime ? "❚❚ Pausa" : "▶ Auto";
        }
    }

    private void CreateStepButton(Transform parent, Font font, Sprite sprite, string label, Vector2 position, float deltaHours)
    {
        CreateActionButton(parent, font, sprite, label + "Button", position, label, delegate
        {
            controller.StepTime(deltaHours);
            RefreshUi();
        });
    }

    private Button CreateActionButton(Transform parent, Font font, Sprite sprite, string name, Vector2 position, string label, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = CreateUiObject(name, parent, typeof(Image), typeof(Button));
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0f, 0f);
        rect.sizeDelta = new Vector2(110f, 40f);
        rect.anchoredPosition = position;

        Image image = buttonObject.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.color = new Color(0.16f, 0.32f, 0.48f, 0.95f);

        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.9f, 0.95f, 1f, 1f);
        colors.pressedColor = new Color(0.8f, 0.88f, 0.98f, 1f);
        button.colors = colors;
        button.onClick.AddListener(onClick);

        CreateText("Label", buttonObject.transform, font, 18, TextAnchor.MiddleCenter, label, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        return button;
    }

    private Slider CreateSlider(Transform parent, Sprite sprite)
    {
        GameObject root = CreateUiObject("TimeSlider", parent, typeof(RectTransform), typeof(Slider));
        Slider slider = root.GetComponent<Slider>();

        GameObject background = CreateUiObject("Background", root.transform, typeof(Image));
        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0f, 0.25f);
        backgroundRect.anchorMax = new Vector2(1f, 0.75f);
        backgroundRect.offsetMin = new Vector2(0f, 0f);
        backgroundRect.offsetMax = new Vector2(0f, 0f);
        Image backgroundImage = background.GetComponent<Image>();
        backgroundImage.sprite = sprite;
        backgroundImage.type = Image.Type.Sliced;
        backgroundImage.color = new Color(0.12f, 0.16f, 0.2f, 0.95f);

        GameObject fillArea = CreateUiObject("Fill Area", root.transform, typeof(RectTransform));
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0f);
        fillAreaRect.anchorMax = new Vector2(1f, 1f);
        fillAreaRect.offsetMin = new Vector2(10f, 8f);
        fillAreaRect.offsetMax = new Vector2(-10f, -8f);

        GameObject fill = CreateUiObject("Fill", fillArea.transform, typeof(Image));
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        Image fillImage = fill.GetComponent<Image>();
        fillImage.sprite = sprite;
        fillImage.type = Image.Type.Sliced;
        fillImage.color = new Color(0.96f, 0.73f, 0.2f, 1f);

        GameObject handleArea = CreateUiObject("Handle Slide Area", root.transform, typeof(RectTransform));
        RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>();
        handleAreaRect.anchorMin = new Vector2(0f, 0f);
        handleAreaRect.anchorMax = new Vector2(1f, 1f);
        handleAreaRect.offsetMin = new Vector2(18f, 0f);
        handleAreaRect.offsetMax = new Vector2(-18f, 0f);

        GameObject handle = CreateUiObject("Handle", handleArea.transform, typeof(Image));
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(28f, 48f);
        Image handleImage = handle.GetComponent<Image>();
        handleImage.sprite = sprite;
        handleImage.type = Image.Type.Sliced;
        handleImage.color = new Color(0.95f, 0.95f, 1f, 1f);

        slider.targetGraphic = handleImage;
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.direction = Slider.Direction.LeftToRight;

        return slider;
    }

    private void CreateHourMarker(Transform parent, Font font, string label, Vector2 position)
    {
        CreateText(label, parent, font, 16, TextAnchor.MiddleCenter, label, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), position, new Vector2(64f, 20f));
    }

    private Text CreateText(string name, Transform parent, Font font, int size, TextAnchor alignment, string content, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject textObject = CreateUiObject(name, parent, typeof(Text));
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        Text text = textObject.GetComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = Color.white;
        text.text = content;
        return text;
    }

    private GameObject CreateUiObject(string name, Transform parent, params System.Type[] components)
    {
        GameObject gameObject = new GameObject(name, components);
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        if (rect == null)
        {
            rect = gameObject.AddComponent<RectTransform>();
        }

        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }
}
