using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class DebugSaveMenu : MonoBehaviour
{
    [SerializeField] string _slotId = "debug";
    [SerializeField] Button _saveButton;
    [SerializeField] Button _loadButton;
    [SerializeField] Button _resetGameButton;
    [SerializeField] SaveFileDialog _saveFileDialog;

    LevelSaveController _saveController;

    [Inject]
    public void Construct(LevelSaveController saveController)
    {
        _saveController = saveController;
    }

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

        if (_resetGameButton != null)
            _resetGameButton.onClick.AddListener(ResetGame);
    }

    void OnDisable()
    {
        if (_saveButton != null)
            _saveButton.onClick.RemoveListener(Save);

        if (_loadButton != null)
            _loadButton.onClick.RemoveListener(Load);

        if (_resetGameButton != null)
            _resetGameButton.onClick.RemoveListener(ResetGame);
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

    void ResetGame()
    {
        if (_saveFileDialog != null)
            _saveFileDialog.Close();

        _saveController.ResetAutoProgressAndReload();
    }
}
