using System.Collections.Generic;
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
    SaveFileDialogMode _mode;
    string _selectedFilePath;

    [Inject]
    public void Construct(LevelSaveController saveController)
    {
        _saveController = saveController;
    }

    void Awake()
    {
        if (_root == null)
            BuildUi();

        _confirmButton.onClick.AddListener(Confirm);
        _nameInput.onValueChanged.AddListener(_ => UpdateConfirmButton());
        Close();
    }

    void OnDestroy()
    {
        _confirmButton.onClick.RemoveListener(Confirm);
        _nameInput.onValueChanged.RemoveAllListeners();
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
            CreateText("No saves found", _listRoot, 24, TextAlignmentOptions.Center);
            return;
        }

        for (int i = 0; i < slots.Count; i++)
            CreateSlotButton(slots[i]);
    }

    void CreateSlotButton(SaveSlotInfo slot)
    {
        GameObject row = CreateSizedRect("Save Row", _listRoot, 84f);
        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10f;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = false;

        Button button = CreateButton(slot.displayName, row.transform);
        SetFlexibleWidth(button.gameObject, 1f);
        Button deleteButton = CreateButton("Delete", row.transform);
        SetFixedWidth(deleteButton.gameObject, 150f);
        deleteButton.targetGraphic.color = new Color(0.65f, 0.08f, 0.08f, 1f);
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
        GameObject obj = CreateSizedRect("Saves List", parent, 520f);
        VerticalLayoutGroup layout = obj.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 10f;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        return obj.transform;
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
