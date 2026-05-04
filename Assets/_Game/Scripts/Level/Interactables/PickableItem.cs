using System;
using UnityEngine;

public enum PickableItemType
{
    Bottle = 0
}

public class PickableItem : MonoBehaviour
{
    [SerializeField] private PickableItemType _type = PickableItemType.Bottle;
    [SerializeField] private GameObject _uiRoot;
    [SerializeField] private string _questId;

    bool _isCollected;
    bool _isInteractionEnabled = true;

    public PickableItemType Type => _type;
    public string QuestId => _questId;
    public bool IsQuestBound => !string.IsNullOrEmpty(_questId);
    public event Action<PickableItem> TakeClicked;

    void OnEnable()
    {
        if (IsQuestBound)
        {
            SetInteractionEnabled(false);
            RegisterQuestPickable();
        }
    }

    void OnDisable()
    {
        if (IsQuestBound)
            Actions.QuestPickableUnregistered(this);
    }

    void Start()
    {
        HideUI();
        RegisterQuestPickable();
    }

    public void ShowUI()
    {
        if (_isCollected || !_isInteractionEnabled) return;

        _uiRoot.SetActive(true);
    }

    public void HideUI()
    {
        _uiRoot.SetActive(false);
    }

    public void TakeButtonClicked()
    {
        if (_isCollected || !_isInteractionEnabled) return;

        TakeClicked?.Invoke(this);
    }

    public void SetInteractionEnabled(bool isEnabled)
    {
        _isInteractionEnabled = isEnabled;

        if (!isEnabled)
            HideUI();
    }

    public void Collect()
    {
        _isCollected = true;
        TakeClicked = null;
        HideUI();
    }

    void RegisterQuestPickable()
    {
        if (IsQuestBound && !_isCollected)
            Actions.QuestPickableRegistered(this);
    }
}
