using System;
using UnityEngine;

public enum PickableItemType
{
    Bottle = 0
}

[RequireComponent(typeof(SaveId))]
public class PickableItem : MonoBehaviour
{
    [SerializeField] private PickableItemType _type = PickableItemType.Bottle;
    [SerializeField] private GameObject _uiRoot;
    [SerializeField] private string _questId;

    bool _isCollected;
    bool _isInteractionEnabled = true;
    SaveId _saveId;

    public string SaveId => GetSaveId().Id;
    public PickableItemType Type => _type;
    public string QuestId => _questId;
    public bool IsQuestBound => !string.IsNullOrEmpty(_questId);
    public bool IsCollected => _isCollected;
    public event Action<PickableItem> TakeClicked;

    void Awake()
    {
        GetSaveId();
    }

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

    public PickableItemSaveData CaptureSaveData(bool isInInventory, int inventoryIndex)
    {
        return new PickableItemSaveData
        {
            id = SaveId,
            isCollected = _isCollected,
            isInInventory = isInInventory,
            inventoryIndex = inventoryIndex,
            activeSelf = gameObject.activeSelf,
            transform = new SaveTransformData(transform),
        };
    }

    public void RestoreSaveData(PickableItemSaveData data)
    {
        _isCollected = data.isCollected;
        TakeClicked = null;

        if (data.transform != null)
            data.transform.ApplyTo(transform);

        gameObject.SetActive(data.activeSelf && !data.isInInventory && !data.isCollected);
        HideUI();
    }

    public void RestoreAsInventoryItem(Transform parent, int stackIndex, float itemYOffset)
    {
        _isCollected = true;
        TakeClicked = null;
        gameObject.SetActive(true);
        transform.SetParent(parent);
        Vector3 targetWorld = parent.position + Vector3.up * (stackIndex * itemYOffset);
        transform.localPosition = parent.InverseTransformPoint(targetWorld);
        transform.localRotation = Quaternion.Euler(180f, 0f, 90f);
        HideUI();
    }

    void RegisterQuestPickable()
    {
        if (IsQuestBound && !_isCollected)
            Actions.QuestPickableRegistered(this);
    }

    SaveId GetSaveId()
    {
        if (_saveId == null && !TryGetComponent(out _saveId))
            _saveId = gameObject.AddComponent<SaveId>();

        return _saveId;
    }
}
