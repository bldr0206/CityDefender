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
    [SerializeField] private Transform _modelRoot;
    [SerializeField] private string _questId;

    bool _isCollected;
    bool _isInteractionEnabled = true;
    string _spawnedByBreakableId;
    int _spawnLootEntryIndex = -1;
    SaveId _saveId;

    public string SaveId => GetSaveId().Id;
    public PickableItemType Type => _type;
    public string QuestId => _questId;
    public bool IsQuestBound => !string.IsNullOrEmpty(_questId);
    public bool IsCollected => _isCollected;
    public event Action<PickableItem> TakeClicked;

    bool SpawnedLootFromBreak => _spawnLootEntryIndex >= 0 && !string.IsNullOrEmpty(_spawnedByBreakableId);

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

    public void SetSpawnedFromBreakable(string breakableSaveId, int lootEntryIndex)
    {
        _spawnedByBreakableId = breakableSaveId ?? string.Empty;
        _spawnLootEntryIndex = lootEntryIndex;
    }

    public void FinishLootReveal()
    {
        if (_isCollected) return;

        if (!IsQuestBound)
            SetInteractionEnabled(true);
    }

    void ApplySpawnSnapshot(string breakableId, int lootEntryIndex)
    {
        _spawnedByBreakableId = breakableId ?? string.Empty;
        _spawnLootEntryIndex = lootEntryIndex;
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
            spawnedLootFromBreak = SpawnedLootFromBreak,
            spawnedByBreakableId = _spawnedByBreakableId,
            lootEntryIndex = _spawnLootEntryIndex,
        };
    }

    public void RestoreSaveData(PickableItemSaveData data)
    {
        _isCollected = data.isCollected;
        TakeClicked = null;

        if (data.spawnedLootFromBreak)
            ApplySpawnSnapshot(data.spawnedByBreakableId, data.lootEntryIndex);
        else
        {
            _spawnedByBreakableId = string.Empty;
            _spawnLootEntryIndex = -1;
        }

        if (data.transform != null)
            data.transform.ApplyTo(transform);

        bool isOnGround = data.activeSelf && !data.isInInventory && !data.isCollected;
        if (isOnGround && _modelRoot != null)
        {
            _modelRoot.localPosition = Vector3.zero;
            _modelRoot.localRotation = Quaternion.identity;
        }

        gameObject.SetActive(isOnGround);
        HideUI();
    }

    public void RestoreAsInventoryItem(Transform parent, int stackIndex, float itemYOffset)
    {
        _isCollected = true;
        TakeClicked = null;
        gameObject.SetActive(true);
        transform.SetParent(parent, false);
        transform.localScale = Vector3.one;
        Vector3 targetWorld = parent.position + Vector3.up * (stackIndex * itemYOffset);
        transform.localPosition = parent.InverseTransformPoint(targetWorld);
        transform.localRotation = Quaternion.Euler(180f, 0f, 90f);
        HideUI();
    }

    public void DiscardExcessAfterLoad()
    {
        TakeClicked = null;
        _isCollected = true;
        HideUI();
        gameObject.SetActive(false);
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
