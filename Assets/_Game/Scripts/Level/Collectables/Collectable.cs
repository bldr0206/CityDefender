using UnityEngine;

public enum CollectableType
{
    Money = 0,
    Key = 1
}

[RequireComponent(typeof(SaveId))]
public class Collectable : MonoBehaviour
{
    public CollectableType type;
    public int value = 1;
    SaveId _saveId;

    public string SaveId => GetSaveId().Id;

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
        };
    }

    public void RestoreSaveData(CollectableSaveData data)
    {
        if (data.transform != null)
            data.transform.ApplyTo(transform);

        gameObject.SetActive(data.activeSelf);
    }

    public void SetCollected()
    {
        gameObject.SetActive(false);
    }

    SaveId GetSaveId()
    {
        if (_saveId == null && !TryGetComponent(out _saveId))
            _saveId = gameObject.AddComponent<SaveId>();

        return _saveId;
    }
}



