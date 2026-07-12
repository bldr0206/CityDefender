using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Одноразовый сборщик меню паузы в LevelCanvas.prefab: экран Screen_PauseMenu
/// (оверлей, заголовок, дебажный слайдер скорости 1–10, кнопка «Продолжить»).
/// Кнопкой открытия служит уже существующая Button-Pause в HUD.
/// Идемпотентен: при повторном запуске пересоздаёт свой экран заново.
/// </summary>
public static class PauseMenuUIBuilder
{
    private const string CanvasPrefabPath = "Assets/_Game/Prefabs/UI/LevelCanvas.prefab";
    private const string RoundedSpriteGuid = "89c628ec853b787498369eac3d1486e8";
    private const string FontGuid = "ce76df0d669b717468d389122d12fe36";

    private static readonly Color OverlayColor = new Color(0.14f, 0.14f, 0.14f, 0.9f);   // токен color.overlay
    private static readonly Color ButtonColor = new Color(0.71f, 0.71f, 0.71f, 1f);      // токен color.button
    private static readonly Color ButtonLabelColor = new Color(0.15f, 0.15f, 0.15f, 1f);
    private static readonly Color ProgressColor = new Color(0.23f, 0.81f, 0f, 1f);       // токен color.progress
    private static readonly Color PanelColor = new Color(0.09f, 0.1f, 0.12f, 0.65f);
    private static readonly Color TrackColor = new Color(0.2f, 0.21f, 0.25f, 1f);
    private static readonly Color MutedTextColor = new Color(1f, 1f, 1f, 0.7f);

    private static Sprite _roundedSprite;
    private static TMP_FontAsset _font;

    [MenuItem("Tools/Pause Menu/Build UI")]
    public static void Build()
    {
        _roundedSprite = LoadByGuid<Sprite>(RoundedSpriteGuid);
        _font = LoadByGuid<TMP_FontAsset>(FontGuid);

        GameObject canvasRoot = PrefabUtility.LoadPrefabContents(CanvasPrefabPath);
        try
        {
            Transform hud = canvasRoot.transform.Find("Screen_LevelHud");
            if (hud == null)
            {
                Debug.LogError($"PauseMenuUIBuilder: 'Screen_LevelHud' not found in {CanvasPrefabPath}");
                return;
            }

            Button openButton = hud.Find("Button-Pause")?.GetComponent<Button>();
            if (openButton == null)
            {
                Debug.LogError("PauseMenuUIBuilder: 'Button-Pause' with Button component not found in HUD.");
                return;
            }

            DestroyExisting(canvasRoot.transform, "Screen_PauseMenu");
            BuildMenuScreen(canvasRoot.transform, openButton);

            PrefabUtility.SaveAsPrefabAsset(canvasRoot, CanvasPrefabPath);
            Debug.Log("PauseMenuUIBuilder: LevelCanvas.prefab updated (Screen_PauseMenu).");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(canvasRoot);
        }
    }

    private static void BuildMenuScreen(Transform canvasRoot, Button openButton)
    {
        RectTransform screen = NewRect("Screen_PauseMenu", canvasRoot);
        Stretch(screen, 0f);
        screen.SetAsLastSibling();

        RectTransform root = NewRect("Root", screen);
        Stretch(root, 0f);

        // Оверлей: перехватывает тапы, затемняет мир
        RectTransform overlay = NewRect("Overlay", root);
        Stretch(overlay, 0f);
        AddImage(overlay, null, OverlayColor);

        TMP_Text title = AddText(root, "Title", "Пауза", 72f, TextAlignmentOptions.Center, Color.white);
        SetAnchored((RectTransform)title.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(700f, 100f));

        Slider speedSlider = BuildSpeedPanel(root, out TMP_Text speedValueText);
        Button resumeButton = BuildResumeButton(root);

        // Экран и Root обязаны быть активны в ассете: Awake экрана сам скрывает Root.
        screen.gameObject.SetActive(true);
        root.gameObject.SetActive(true);

        PauseMenuScreen menuScreen = screen.gameObject.AddComponent<PauseMenuScreen>();
        var so = new SerializedObject(menuScreen);
        so.FindProperty("_root").objectReferenceValue = root.gameObject;
        so.FindProperty("_openButton").objectReferenceValue = openButton;
        so.FindProperty("_resumeButton").objectReferenceValue = resumeButton;
        so.FindProperty("_speedSlider").objectReferenceValue = speedSlider;
        so.FindProperty("_speedValueText").objectReferenceValue = speedValueText;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Slider BuildSpeedPanel(RectTransform root, out TMP_Text valueText)
    {
        RectTransform panel = NewRect("SpeedPanel", root);
        panel.anchorMin = new Vector2(0.08f, 0.36f);
        panel.anchorMax = new Vector2(0.92f, 0.62f);
        panel.offsetMin = Vector2.zero;
        panel.offsetMax = Vector2.zero;
        AddImage(panel, _roundedSprite, PanelColor);
        AddScaleUpAnimation(panel);

        TMP_Text label = AddText(panel, "Label", "Скорость перемещения (1–10)", 38f, TextAlignmentOptions.Center, MutedTextColor);
        SetBand((RectTransform)label.transform, 0.62f, 0.94f, 40f);

        valueText = AddText(panel, "Value", "5", 96f, TextAlignmentOptions.Center, Color.white);
        SetBand((RectTransform)valueText.transform, 0.30f, 0.62f, 40f);

        return BuildSlider(panel);
    }

    private static Slider BuildSlider(RectTransform panel)
    {
        var resources = new DefaultControls.Resources { standard = _roundedSprite };
        GameObject sliderGo = DefaultControls.CreateSlider(resources);
        sliderGo.name = "SpeedSlider";
        sliderGo.layer = 5;

        RectTransform rect = (RectTransform)sliderGo.transform;
        rect.SetParent(panel, false);
        SetBand(rect, 0.06f, 0.26f, 60f);
        rect.offsetMin = new Vector2(rect.offsetMin.x + 48f, rect.offsetMin.y);
        rect.offsetMax = new Vector2(rect.offsetMax.x - 48f, rect.offsetMax.y);

        Slider slider = sliderGo.GetComponent<Slider>();
        slider.minValue = 1f;
        slider.maxValue = 10f;
        slider.wholeNumbers = true;   // дискретно, без float
        slider.value = 5f;

        StyleSliderPart(sliderGo, "Background", TrackColor);
        StyleSliderPart(sliderGo, "Fill Area/Fill", ProgressColor);

        // Крупный хэндл под тач
        Transform handle = sliderGo.transform.Find("Handle Slide Area/Handle");
        if (handle != null)
        {
            StyleImage(handle.GetComponent<Image>(), ButtonColor);
            ((RectTransform)handle).sizeDelta = new Vector2(64f, 64f);
        }

        return slider;
    }

    private static void StyleSliderPart(GameObject sliderGo, string childPath, Color color)
    {
        Transform part = sliderGo.transform.Find(childPath);
        if (part != null)
            StyleImage(part.GetComponent<Image>(), color);
    }

    private static void StyleImage(Image image, Color color)
    {
        if (image == null) return;
        image.sprite = _roundedSprite;
        image.color = color;
        image.type = _roundedSprite != null ? Image.Type.Sliced : Image.Type.Simple;
    }

    private static Button BuildResumeButton(RectTransform root)
    {
        RectTransform button = NewRect("Button - Resume", root);
        SetAnchored(button, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 120f), new Vector2(460f, 120f));

        Image image = AddImage(button, _roundedSprite, ButtonColor);
        SmoothButton smoothButton = button.gameObject.AddComponent<SmoothButton>();
        smoothButton.targetGraphic = image;

        TMP_Text label = AddText(button, "Text (TMP)", "Продолжить", 48f, TextAlignmentOptions.Center, ButtonLabelColor);
        Stretch((RectTransform)label.transform, 8f);
        return smoothButton;
    }

    // ---- примитивы ----

    private static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        return rect;
    }

    private static Image AddImage(RectTransform target, Sprite sprite, Color color)
    {
        Image image = target.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        return image;
    }

    private static TMP_Text AddText(RectTransform parent, string name, string placeholder, float fontSize, TextAlignmentOptions alignment, Color color)
    {
        RectTransform rect = NewRect(name, parent);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        if (_font != null)
            text.font = _font;
        text.text = placeholder;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    private static void AddScaleUpAnimation(RectTransform target)
    {
        AnimateOnEnable animation = target.gameObject.AddComponent<AnimateOnEnable>();
        var so = new SerializedObject(animation);
        so.FindProperty("preset").enumValueIndex = 0; // ScaleUp
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetAnchored(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    // Горизонтальная полоса внутри родителя по нормализованным Y [minY, maxY] с боковым отступом.
    private static void SetBand(RectTransform rect, float minY, float maxY, float sideInset)
    {
        rect.anchorMin = new Vector2(0f, minY);
        rect.anchorMax = new Vector2(1f, maxY);
        rect.offsetMin = new Vector2(sideInset, 0f);
        rect.offsetMax = new Vector2(-sideInset, 0f);
    }

    private static void Stretch(RectTransform rect, float inset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }

    private static T LoadByGuid<T>(string guid) where T : Object
    {
        string path = AssetDatabase.GUIDToAssetPath(guid);
        return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<T>(path);
    }

    private static void DestroyExisting(Transform parent, string childName)
    {
        Transform existing = parent.Find(childName);
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);
    }
}
