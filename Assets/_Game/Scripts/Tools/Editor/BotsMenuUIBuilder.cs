using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Одноразовый сборщик UI меню ботов в LevelCanvas.prefab: кнопка «Боты» в HUD
/// и экран Screen_BotsMenu (оверлей, панель характеристик, карусель, кнопка закрытия).
/// Идемпотентен: при повторном запуске пересоздаёт свои объекты заново.
/// </summary>
public static class BotsMenuUIBuilder
{
    private const string CanvasPrefabPath = "Assets/_Game/Prefabs/UI/LevelCanvas.prefab";
    private const string RoundedSpriteGuid = "89c628ec853b787498369eac3d1486e8";
    private const string OutlineSpriteGuid = "678c3eb7bd58fa14db63cc29c98a24de";
    private const string FontGuid = "ce76df0d669b717468d389122d12fe36";

    private static readonly Color OverlayColor = new Color(0.14f, 0.14f, 0.14f, 0.9f);   // токен color.overlay
    private static readonly Color ButtonColor = new Color(0.71f, 0.71f, 0.71f, 1f);      // токен color.button
    private static readonly Color ButtonLabelColor = new Color(0.15f, 0.15f, 0.15f, 1f);
    private static readonly Color PanelColor = new Color(0.09f, 0.1f, 0.12f, 0.65f);
    private static readonly Color CardColor = new Color(0.22f, 0.23f, 0.27f, 0.95f);
    private static readonly Color PortraitColor = new Color(0.45f, 0.47f, 0.52f, 1f);
    private static readonly Color MutedTextColor = new Color(1f, 1f, 1f, 0.7f);

    private static Sprite _roundedSprite;
    private static Sprite _outlineSprite;
    private static TMP_FontAsset _font;

    [MenuItem("Tools/Bots Menu/Build UI")]
    public static void Build()
    {
        _roundedSprite = LoadByGuid<Sprite>(RoundedSpriteGuid);
        _outlineSprite = LoadByGuid<Sprite>(OutlineSpriteGuid);
        _font = LoadByGuid<TMP_FontAsset>(FontGuid);

        GameObject canvasRoot = PrefabUtility.LoadPrefabContents(CanvasPrefabPath);
        try
        {
            Transform hud = canvasRoot.transform.Find("Screen_LevelHud");
            if (hud == null)
            {
                Debug.LogError($"BotsMenuUIBuilder: 'Screen_LevelHud' not found in {CanvasPrefabPath}");
                return;
            }

            DestroyExisting(hud, "Button - Bots");
            DestroyExisting(canvasRoot.transform, "Screen_BotsMenu");

            Button openButton = BuildOpenButton(hud, out TMP_Text openButtonText);
            BuildMenuScreen(canvasRoot.transform, openButton, openButtonText);

            PrefabUtility.SaveAsPrefabAsset(canvasRoot, CanvasPrefabPath);
            Debug.Log("BotsMenuUIBuilder: LevelCanvas.prefab updated (Button - Bots + Screen_BotsMenu).");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(canvasRoot);
        }
    }

    private static Button BuildOpenButton(Transform hud, out TMP_Text label)
    {
        RectTransform button = NewRect("Button - Bots", hud);
        SetAnchored(button, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(300f, 110f));
        button.SetAsLastSibling(); // поверх зоны джойстика — тап по кнопке достаётся кнопке

        Image image = AddImage(button, _roundedSprite, ButtonColor);
        SmoothButton smoothButton = button.gameObject.AddComponent<SmoothButton>();
        smoothButton.targetGraphic = image;

        label = AddText(button, "Text (TMP)", "Боты", 44f, TextAlignmentOptions.Center, ButtonLabelColor);
        Stretch((RectTransform)label.transform, 8f);
        return smoothButton;
    }

    private static void BuildMenuScreen(Transform canvasRoot, Button openButton, TMP_Text openButtonText)
    {
        RectTransform screen = NewRect("Screen_BotsMenu", canvasRoot);
        Stretch(screen, 0f);
        screen.SetAsLastSibling();

        RectTransform root = NewRect("Root", screen);
        Stretch(root, 0f);

        // Оверлей: перехватывает тапы, затемняет мир
        RectTransform overlay = NewRect("Overlay", root);
        Stretch(overlay, 0f);
        AddImage(overlay, null, OverlayColor);

        TMP_Text title = AddText(root, "Title", "Боты", 72f, TextAlignmentOptions.Center, Color.white);
        SetAnchored((RectTransform)title.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(700f, 90f));

        BotStatsPanel statsPanel = BuildStatsPanel(root);
        TMP_Text emptyText = BuildEmptyText(root);
        BotsCarousel carousel = BuildCarousel(root);
        Button closeButton = BuildCloseButton(root, out TMP_Text closeButtonText);

        // Экран и Root обязаны быть активны в ассете: Awake экрана сам скрывает Root.
        screen.gameObject.SetActive(true);
        root.gameObject.SetActive(true);

        BotsMenuScreen menuScreen = screen.gameObject.AddComponent<BotsMenuScreen>();
        var so = new SerializedObject(menuScreen);
        so.FindProperty("_root").objectReferenceValue = root.gameObject;
        so.FindProperty("_openButton").objectReferenceValue = openButton;
        so.FindProperty("_openButtonText").objectReferenceValue = openButtonText;
        so.FindProperty("_closeButton").objectReferenceValue = closeButton;
        so.FindProperty("_closeButtonText").objectReferenceValue = closeButtonText;
        so.FindProperty("_titleText").objectReferenceValue = title;
        so.FindProperty("_emptyText").objectReferenceValue = emptyText;
        so.FindProperty("_statsPanel").objectReferenceValue = statsPanel;
        so.FindProperty("_carousel").objectReferenceValue = carousel;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static BotStatsPanel BuildStatsPanel(RectTransform root)
    {
        RectTransform panel = NewRect("StatsPanel", root);
        panel.anchorMin = new Vector2(0.06f, 0.42f);
        panel.anchorMax = new Vector2(0.94f, 0.86f);
        panel.offsetMin = Vector2.zero;
        panel.offsetMax = Vector2.zero;
        AddImage(panel, _roundedSprite, PanelColor);
        AddScaleUpAnimation(panel);

        TMP_Text botName = AddText(panel, "BotName", "Бот 1", 52f, TextAlignmentOptions.Left, Color.white);
        SetTopStretch((RectTransform)botName.transform, 48f, -28f, 64f);

        TMP_Text specialization = AddText(panel, "Specialization", "Разнорабочий", 38f, TextAlignmentOptions.Left, MutedTextColor);
        SetTopStretch((RectTransform)specialization.transform, 48f, -100f, 48f);

        RectTransform rows = NewRect("Rows", panel);
        rows.anchorMin = Vector2.zero;
        rows.anchorMax = Vector2.one;
        rows.offsetMin = new Vector2(48f, 32f);
        rows.offsetMax = new Vector2(-48f, -170f);
        VerticalLayoutGroup layout = rows.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 10f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childAlignment = TextAnchor.UpperCenter;

        BotStatRowView rowTemplate = BuildStatRowTemplate(rows);

        BotStatsPanel statsPanel = panel.gameObject.AddComponent<BotStatsPanel>();
        var so = new SerializedObject(statsPanel);
        so.FindProperty("_botNameText").objectReferenceValue = botName;
        so.FindProperty("_specializationText").objectReferenceValue = specialization;
        so.FindProperty("_rowsRoot").objectReferenceValue = rows;
        so.FindProperty("_rowTemplate").objectReferenceValue = rowTemplate;
        so.ApplyModifiedPropertiesWithoutUndo();
        return statsPanel;
    }

    private static BotStatRowView BuildStatRowTemplate(RectTransform rowsRoot)
    {
        RectTransform row = NewRect("Row_Template", rowsRoot);
        row.sizeDelta = new Vector2(0f, 74f);

        TMP_Text name = AddText(row, "Name", "Характеристика", 40f, TextAlignmentOptions.MidlineLeft, Color.white);
        SetAnchorBand((RectTransform)name.transform, 0f, 0.5f);
        name.overflowMode = TextOverflowModes.Ellipsis;

        TMP_Text level = AddText(row, "Level", "Ур. 1", 36f, TextAlignmentOptions.Midline, MutedTextColor);
        SetAnchorBand((RectTransform)level.transform, 0.5f, 0.74f);

        TMP_Text value = AddText(row, "Value", "0", 44f, TextAlignmentOptions.MidlineRight, Color.white);
        SetAnchorBand((RectTransform)value.transform, 0.74f, 1f);

        BotStatRowView view = row.gameObject.AddComponent<BotStatRowView>();
        var so = new SerializedObject(view);
        so.FindProperty("_nameText").objectReferenceValue = name;
        so.FindProperty("_levelText").objectReferenceValue = level;
        so.FindProperty("_valueText").objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();

        row.gameObject.SetActive(false);
        return view;
    }

    private static TMP_Text BuildEmptyText(RectTransform root)
    {
        TMP_Text empty = AddText(root, "EmptyText", "У вас пока нет ботов", 48f, TextAlignmentOptions.Center, Color.white);
        RectTransform rect = (RectTransform)empty.transform;
        rect.anchorMin = new Vector2(0.1f, 0.4f);
        rect.anchorMax = new Vector2(0.9f, 0.6f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        empty.gameObject.SetActive(false);
        return empty;
    }

    private static BotsCarousel BuildCarousel(RectTransform root)
    {
        RectTransform carousel = NewRect("Carousel", root);
        carousel.anchorMin = new Vector2(0f, 0.15f);
        carousel.anchorMax = new Vector2(1f, 0.4f);
        carousel.offsetMin = Vector2.zero;
        carousel.offsetMax = Vector2.zero;
        AddScaleUpAnimation(carousel);

        RectTransform viewport = NewRect("Viewport", carousel);
        Stretch(viewport, 0f);
        viewport.gameObject.AddComponent<RectMask2D>();

        RectTransform content = NewRect("Content", viewport);
        content.anchorMin = new Vector2(0f, 0f);
        content.anchorMax = new Vector2(0f, 1f);
        content.pivot = new Vector2(0f, 0.5f);
        content.offsetMin = Vector2.zero;
        content.offsetMax = Vector2.zero;
        HorizontalLayoutGroup layout = content.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 28f;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childAlignment = TextAnchor.MiddleLeft;
        ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scrollRect = carousel.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = true;
        scrollRect.vertical = false;
        scrollRect.inertia = false;
        scrollRect.movementType = ScrollRect.MovementType.Elastic;
        scrollRect.viewport = viewport;
        scrollRect.content = content;

        BotCardView cardTemplate = BuildCardTemplate(content);

        BotsCarousel carouselView = carousel.gameObject.AddComponent<BotsCarousel>();
        var so = new SerializedObject(carouselView);
        so.FindProperty("_scrollRect").objectReferenceValue = scrollRect;
        so.FindProperty("_cardTemplate").objectReferenceValue = cardTemplate;
        so.ApplyModifiedPropertiesWithoutUndo();
        return carouselView;
    }

    private static BotCardView BuildCardTemplate(RectTransform content)
    {
        RectTransform card = NewRect("Card_Template", content);
        card.sizeDelta = new Vector2(300f, 400f);

        Image background = AddImage(card, _roundedSprite, CardColor);
        SmoothButton button = card.gameObject.AddComponent<SmoothButton>();
        button.targetGraphic = background;

        RectTransform portrait = NewRect("Portrait", card);
        SetAnchored(portrait, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(200f, 200f));
        AddImage(portrait, _roundedSprite, PortraitColor);

        TMP_Text name = AddText(card, "Name", "Бот 1", 40f, TextAlignmentOptions.Center, Color.white);
        RectTransform nameRect = (RectTransform)name.transform;
        SetAnchored(nameRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(268f, 56f));
        name.overflowMode = TextOverflowModes.Ellipsis;

        RectTransform outline = NewRect("SelectedOutline", card);
        Stretch(outline, -8f);
        AddImage(outline, _outlineSprite, Color.white).raycastTarget = false;
        outline.gameObject.SetActive(false);

        BotCardView view = card.gameObject.AddComponent<BotCardView>();
        var so = new SerializedObject(view);
        so.FindProperty("_button").objectReferenceValue = button;
        so.FindProperty("_nameText").objectReferenceValue = name;
        so.FindProperty("_selectedOutline").objectReferenceValue = outline.gameObject;
        so.ApplyModifiedPropertiesWithoutUndo();

        card.gameObject.SetActive(false);
        return view;
    }

    private static Button BuildCloseButton(RectTransform root, out TMP_Text label)
    {
        RectTransform button = NewRect("Button - Close", root);
        SetAnchored(button, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 90f), new Vector2(460f, 120f));

        Image image = AddImage(button, _roundedSprite, ButtonColor);
        SmoothButton smoothButton = button.gameObject.AddComponent<SmoothButton>();
        smoothButton.targetGraphic = image;

        label = AddText(button, "Text (TMP)", "Закрыть", 48f, TextAlignmentOptions.Center, ButtonLabelColor);
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

    private static void SetTopStretch(RectTransform rect, float sideOffset, float y, float height)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(sideOffset, 0f);
        rect.offsetMax = new Vector2(-sideOffset, 0f);
        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = new Vector2(rect.sizeDelta.x, height);
    }

    private static void SetAnchorBand(RectTransform rect, float minX, float maxX)
    {
        rect.anchorMin = new Vector2(minX, 0f);
        rect.anchorMax = new Vector2(maxX, 1f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
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
