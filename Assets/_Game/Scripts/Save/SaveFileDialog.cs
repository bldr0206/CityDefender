using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class SaveFileDialog : MonoBehaviour
{
    [SerializeField] GameObject _root;
    [SerializeField] TMP_Text _titleText;
    [SerializeField] TMP_InputField _nameInput;
    [SerializeField] Transform _listRoot;
    [SerializeField] Button _confirmButton;
    [SerializeField] TMP_Text _confirmText;

    LevelSaveController _saveController;
    SaveService _saveService;
    Button _openFolderButton;
    SaveFileDialogMode _mode;
    string _selectedFilePath;

    [Inject]
    public void Construct(LevelSaveController saveController, SaveService saveService)
    {
        _saveController = saveController;
        _saveService = saveService;
    }

    void Awake()
    {
        if (_root == null)
            BuildUi();

        _confirmButton.onClick.AddListener(Confirm);
        _nameInput.onValueChanged.AddListener(_ => UpdateConfirmButton());
        if (_openFolderButton != null)
            _openFolderButton.onClick.AddListener(OpenSaveFolderInFileManager);
        Close();
    }

    void OnDestroy()
    {
        _confirmButton.onClick.RemoveListener(Confirm);
        _nameInput.onValueChanged.RemoveAllListeners();
        if (_openFolderButton != null)
            _openFolderButton.onClick.RemoveListener(OpenSaveFolderInFileManager);
    }

    public void OpenSave()
    {
        _mode = SaveFileDialogMode.Save;
        _selectedFilePath = null;
        _titleText.text = "Save Game";
        _confirmText.text = "Save";
        _nameInput.gameObject.SetActive(true);
        _nameInput.text = _saveController.GetDefaultSaveName();
        RefreshSlots();
        _root.SetActive(true);
        UpdateConfirmButton();
    }

    public void OpenLoad()
    {
        _mode = SaveFileDialogMode.Load;
        _selectedFilePath = null;
        _titleText.text = "Load Game";
        _confirmText.text = "Load";
        _nameInput.gameObject.SetActive(false);
        RefreshSlots();
        _root.SetActive(true);
        UpdateConfirmButton();
    }

    public void Close()
    {
        _root.SetActive(false);
    }

    void OpenSaveFolderInFileManager()
    {
        string path = Path.GetFullPath(_saveService.SaveFolderPath);
        try
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = path,
                UseShellExecute = true,
            });
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            Process.Start("open", path);
#elif UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
            Process.Start("xdg-open", path);
#else
            Application.OpenURL("file://" + path.Replace("\\", "/"));
#endif
        }
        catch (System.Exception exception)
        {
            UnityEngine.Debug.LogWarning($"Could not open save folder: {exception.Message}");
        }
    }

    void Confirm()
    {
        if (_mode == SaveFileDialogMode.Save)
        {
            string saveName = _nameInput.text.Trim();
            _saveController.SaveToFile(saveName, saveName);
            Close();
            return;
        }

        if (!string.IsNullOrEmpty(_selectedFilePath))
            _saveController.LoadFromFile(_selectedFilePath);
    }

    void RefreshSlots()
    {
        ClearList();

        List<SaveSlotInfo> slots = _saveController.GetSlots();
        if (slots.Count == 0)
        {
            GameObject emptyRow = CreateSizedRect("Empty Row", _listRoot, 84f);
            LayoutElement emptyLayout = emptyRow.GetComponent<LayoutElement>();
            emptyLayout.minHeight = 84f;
            emptyLayout.flexibleHeight = 0f;
            TMP_Text emptyText = CreateText("No saves found", emptyRow.transform, 24, TextAlignmentOptions.MidlineLeft);
            emptyText.margin = new Vector4(16f, 0f, 16f, 0f);
            return;
        }

        for (int i = 0; i < slots.Count; i++)
            CreateSlotButton(slots[i]);
    }

    void CreateSlotButton(SaveSlotInfo slot)
    {
        const float rowHeight = 84f;
        GameObject row = CreateSizedRect("Save Row", _listRoot, rowHeight);
        LayoutElement rowLayout = row.GetComponent<LayoutElement>();
        rowLayout.minHeight = rowHeight;
        rowLayout.preferredHeight = rowHeight;
        rowLayout.flexibleHeight = 0f;

        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;

        Button button = CreateButton(slot.displayName, row.transform);
        SetFlexibleWidth(button.gameObject, 1f);
        Button deleteButton = CreateButton("Delete", row.transform);
        SetFixedWidth(deleteButton.gameObject, 150f);
        deleteButton.targetGraphic.color = new Color(0.65f, 0.08f, 0.08f, 1f);
        ConfigureSlotNameLabel(button.GetComponentInChildren<TMP_Text>());
        string filePath = slot.filePath;
        string displayName = slot.displayName;

        button.onClick.AddListener(() =>
        {
            _selectedFilePath = filePath;
            if (_mode == SaveFileDialogMode.Save)
                _nameInput.text = displayName;

            UpdateConfirmButton();
        });

        deleteButton.onClick.AddListener(() =>
        {
            _saveController.DeleteFile(filePath);
            if (_selectedFilePath == filePath)
                _selectedFilePath = null;

            RefreshSlots();
            UpdateConfirmButton();
        });
    }

    void ClearList()
    {
        for (int i = _listRoot.childCount - 1; i >= 0; i--)
            Destroy(_listRoot.GetChild(i).gameObject);
    }

    void UpdateConfirmButton()
    {
        _confirmButton.interactable = _mode == SaveFileDialogMode.Save
            ? !string.IsNullOrWhiteSpace(_nameInput.text)
            : !string.IsNullOrEmpty(_selectedFilePath);
    }

    void BuildUi()
    {
        _root = CreateRect("Save File Dialog", transform, Vector2.zero, Vector2.one);
        _root.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);

        GameObject panel = CreateRect("Panel", _root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(760f, 900f);
        panel.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.08f, 0.95f);

        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(32, 32, 32, 32);
        layout.spacing = 18f;
        layout.childControlHeight = true;
        layout.childControlWidth = true;

        _titleText = CreateText("Save Game", panel.transform, 40, TextAlignmentOptions.Center);
        _nameInput = CreateInput(panel.transform);
        _listRoot = CreateListRoot(panel.transform);

        HorizontalLayoutGroup buttons = CreateButtonsRoot(panel.transform);
        _confirmButton = CreateButton("Save", buttons.transform);
        _confirmText = _confirmButton.GetComponentInChildren<TMP_Text>();
        _openFolderButton = CreateButton("Open folder", buttons.transform);
        SetFixedWidth(_openFolderButton.gameObject, 200f);
        CreateButton("Cancel", buttons.transform).onClick.AddListener(Close);
    }

    GameObject CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return obj;
    }

    TMP_Text CreateText(string text, Transform parent, int fontSize, TextAlignmentOptions alignment)
    {
        GameObject obj = new GameObject("Text", typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TMP_Text label = obj.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.alignment = alignment;
        label.color = Color.white;
        return label;
    }

    TMP_InputField CreateInput(Transform parent)
    {
        GameObject obj = CreateSizedRect("Name Input", parent, 96f);
        Image image = obj.AddComponent<Image>();
        image.color = new Color(0.16f, 0.16f, 0.16f, 1f);

        TMP_InputField input = obj.AddComponent<TMP_InputField>();
        GameObject textArea = CreateRect("Text Area", obj.transform, Vector2.zero, Vector2.one);
        textArea.AddComponent<RectMask2D>();
        RectTransform viewport = textArea.GetComponent<RectTransform>();
        viewport.offsetMin = new Vector2(18f, 0f);
        viewport.offsetMax = new Vector2(-18f, 0f);

        TMP_Text text = CreateText(string.Empty, textArea.transform, 28, TextAlignmentOptions.MidlineLeft);
        TMP_Text placeholder = CreateText("Save name", textArea.transform, 28, TextAlignmentOptions.MidlineLeft);
        placeholder.color = Color.gray;

        input.textComponent = text;
        input.textViewport = viewport;
        input.placeholder = placeholder;
        input.targetGraphic = image;
        return input;
    }

    Transform CreateListRoot(Transform parent)
    {
        const float scrollbarWidth = 16f;
        const float scrollbarGap = 4f;

        GameObject scrollGo = CreateSizedRect("Saves Scroll", parent, 520f);
        ScrollRect scroll = scrollGo.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 40f;
        scroll.inertia = true;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

        GameObject viewport = CreateRect("Viewport", scrollGo.transform, Vector2.zero, Vector2.one);
        viewport.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);
        viewport.AddComponent<RectMask2D>();
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = new Vector2(-(scrollbarWidth + scrollbarGap), 0f);

        Scrollbar scrollbar = CreateVerticalScrollbar(scrollGo.transform, scrollbarWidth);
        scroll.verticalScrollbar = scrollbar;

        GameObject content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 0f);

        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = viewportRect;
        scroll.content = contentRect;

        return content.transform;
    }

    Scrollbar CreateVerticalScrollbar(Transform parent, float width)
    {
        GameObject root = new GameObject("Scrollbar Vertical", typeof(RectTransform));
        root.transform.SetParent(parent, false);
        RectTransform rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = new Vector2(1f, 0f);
        rootRt.anchorMax = new Vector2(1f, 1f);
        rootRt.pivot = new Vector2(1f, 0.5f);
        rootRt.sizeDelta = new Vector2(width, 0f);
        rootRt.anchoredPosition = Vector2.zero;

        Image track = root.AddComponent<Image>();
        track.color = new Color(0.12f, 0.12f, 0.12f, 1f);

        GameObject sliding = new GameObject("Sliding Area", typeof(RectTransform));
        sliding.transform.SetParent(root.transform, false);
        RectTransform slidingRt = sliding.GetComponent<RectTransform>();
        slidingRt.anchorMin = Vector2.zero;
        slidingRt.anchorMax = Vector2.one;
        slidingRt.offsetMin = new Vector2(2f, 4f);
        slidingRt.offsetMax = new Vector2(-2f, -4f);

        GameObject handle = new GameObject("Handle", typeof(RectTransform));
        handle.transform.SetParent(sliding.transform, false);
        RectTransform handleRt = handle.GetComponent<RectTransform>();
        handleRt.anchorMin = Vector2.zero;
        handleRt.anchorMax = Vector2.one;
        handleRt.sizeDelta = Vector2.zero;

        Image handleImg = handle.AddComponent<Image>();
        handleImg.color = new Color(0.52f, 0.52f, 0.52f, 1f);

        Scrollbar scrollbar = root.AddComponent<Scrollbar>();
        scrollbar.targetGraphic = handleImg;
        scrollbar.handleRect = handleRt;
        scrollbar.direction = Scrollbar.Direction.BottomToTop;

        return scrollbar;
    }

    static void ConfigureSlotNameLabel(TMP_Text text)
    {
        if (text == null) return;

        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.margin = new Vector4(16f, 0f, 12f, 0f);
    }

    HorizontalLayoutGroup CreateButtonsRoot(Transform parent)
    {
        GameObject obj = CreateSizedRect("Buttons", parent, 96f);
        HorizontalLayoutGroup layout = obj.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 18f;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        return layout;
    }

    Button CreateButton(string text, Transform parent)
    {
        GameObject obj = CreateSizedRect(text, parent, 84f);
        Image image = obj.AddComponent<Image>();
        image.color = new Color(0.16f, 0.16f, 0.16f, 1f);

        Button button = obj.AddComponent<Button>();
        button.targetGraphic = image;
        CreateText(text, obj.transform, 28, TextAlignmentOptions.Center);
        return button;
    }

    void SetFlexibleWidth(GameObject obj, float flexibleWidth)
    {
        LayoutElement layout = obj.GetComponent<LayoutElement>();
        if (layout != null)
            layout.flexibleWidth = flexibleWidth;
    }

    void SetFixedWidth(GameObject obj, float width)
    {
        LayoutElement layout = obj.GetComponent<LayoutElement>();
        if (layout == null) return;

        layout.minWidth = width;
        layout.preferredWidth = width;
        layout.flexibleWidth = 0f;
    }

    GameObject CreateSizedRect(string name, Transform parent, float height)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        LayoutElement layout = obj.AddComponent<LayoutElement>();
        layout.preferredHeight = height;
        return obj;
    }
}

public enum SaveFileDialogMode
{
    Save,
    Load,
}
