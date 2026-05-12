using UnityEngine;

public enum CollectableType
{
    Money = 0
}

[RequireComponent(typeof(SaveId))]
public class Collectable : MonoBehaviour
{
    public CollectableType type;
    public int value = 1;

    [SerializeField] bool _canCollect = true;
    string _spawnedByBreakableId;
    int _spawnLootEntryIndex = -1;

    SaveId _saveId;

    public string SaveId => GetSaveId().Id;
    public bool CanCollect => _canCollect && gameObject.activeSelf;

    bool SpawnedLootFromBreak => _spawnLootEntryIndex >= 0 && !string.IsNullOrEmpty(_spawnedByBreakableId);

    void Awake()
    {
        GetSaveId();
    }

    public CollectableSaveData CaptureSaveData()
    {
        return new CollectableSaveData
        {
            id = SaveId,
            activeSelf = gameObject.activeSelf,
            transform = new SaveTransformData(transform),
            spawnedLootFromBreak = SpawnedLootFromBreak,
            spawnedByBreakableId = _spawnedByBreakableId,
            lootEntryIndex = _spawnLootEntryIndex,
        };
    }

    public void RestoreSaveData(CollectableSaveData data)
    {
        if (data.spawnedLootFromBreak)
            ApplySpawnSnapshot(data.spawnedByBreakableId, data.lootEntryIndex);
        else
        {
            _spawnedByBreakableId = string.Empty;
            _spawnLootEntryIndex = -1;
        }

        if (data.transform != null)
            data.transform.ApplyTo(transform);

        gameObject.SetActive(data.activeSelf);
    }

    public void SetCollected()
    {
        gameObject.SetActive(false);
    }

    public void SetCanCollect(bool canCollect)
    {
        _canCollect = canCollect;
    }

    public void SetSpawnedFromBreakable(string breakableSaveId, int lootEntryIndex)
    {
        _spawnedByBreakableId = breakableSaveId;
        _spawnLootEntryIndex = lootEntryIndex;
    }

    void ApplySpawnSnapshot(string breakableId, int lootEntryIndex)
    {
        _spawnedByBreakableId = breakableId ?? string.Empty;
        _spawnLootEntryIndex = lootEntryIndex;
    }

    SaveId GetSaveId()
    {
        if (_saveId == null && !TryGetComponent(out _saveId))
            _saveId = gameObject.AddComponent<SaveId>();

        return _saveId;
    }
}
