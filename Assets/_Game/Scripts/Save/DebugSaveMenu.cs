using UnityEngine;
using UnityEngine.UI;

public class DebugSaveMenu : MonoBehaviour
{
    [SerializeField] string _slotId = "debug";
    [SerializeField] Button _saveButton;
    [SerializeField] Button _loadButton;
    [SerializeField] SaveFileDialog _saveFileDialog;

    void Awake()
    {
        if (_saveFileDialog == null)
            _saveFileDialog = GetComponent<SaveFileDialog>();
    }

    void OnEnable()
    {
        if (_saveButton != null)
            _saveButton.onClick.AddListener(Save);

        if (_loadButton != null)
            _loadButton.onClick.AddListener(Load);
    }

    void OnDisable()
    {
        if (_saveButton != null)
            _saveButton.onClick.RemoveListener(Save);

        if (_loadButton != null)
            _loadButton.onClick.RemoveListener(Load);
    }

    void Save()
    {
        if (_saveFileDialog != null)
            _saveFileDialog.OpenSave();
        else
            Actions.SaveRequested(_slotId);
    }

    void Load()
    {
        if (_saveFileDialog != null)
            _saveFileDialog.OpenLoad();
        else
            Actions.LoadRequested(_slotId);
    }
}
