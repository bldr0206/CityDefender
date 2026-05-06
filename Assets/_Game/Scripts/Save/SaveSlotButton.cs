using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;
using Zenject;

public class SaveSlotButton : MonoBehaviour
{
    [SerializeField] string _slotId = "test";
    [SerializeField] SaveSlotAction _action;
    [SerializeField] Button _button;
    [SerializeField] TMP_Text _label;
    [SerializeField] LocalizedString _emptySlotText;

    LevelSaveController _saveController;

    [Inject]
    public void Construct(LevelSaveController saveController)
    {
        _saveController = saveController;
    }

    void Awake()
    {
        if (_button == null)
            _button = GetComponent<Button>();
    }

    void OnEnable()
    {
        if (_button != null)
            _button.onClick.AddListener(HandleClicked);

        Refresh();
    }

    void OnDisable()
    {
        if (_button != null)
            _button.onClick.RemoveListener(HandleClicked);
    }

    public void Refresh()
    {
        if (_label == null || _saveController == null) return;

        SaveSlotInfo slot = GetSlotInfo();
        string emptyText = _emptySlotText != null ? _emptySlotText.GetLocalizedString() : string.Empty;
        _label.text = slot != null
            ? $"{slot.displayName}\n{slot.savedAtUtc}"
            : emptyText;
    }

    void HandleClicked()
    {
        switch (_action)
        {
            case SaveSlotAction.Save:
                Actions.SaveRequested(_slotId);
                break;
            case SaveSlotAction.Load:
                Actions.LoadRequested(_slotId);
                break;
            case SaveSlotAction.Delete:
                _saveController.Delete(_slotId);
                Refresh();
                break;
        }
    }

    SaveSlotInfo GetSlotInfo()
    {
        var slots = _saveController.GetSlots();
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].slotId == _slotId)
                return slots[i];
        }

        return null;
    }
}

public enum SaveSlotAction
{
    Save,
    Load,
    Delete,
}
