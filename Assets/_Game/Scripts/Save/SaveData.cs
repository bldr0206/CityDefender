using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SaveData
{
    public int version = 1;
    public string slotId;
    public string displayName;
    public string savedAtUtc;
    public string sceneName;
    public int levelIndex;
    public int money;
    public int hiredAgentsCount;
    public bool isLevelFinished;
    public SaveTransformData playerTransform;
    public QuestSaveData quest = new QuestSaveData();
    public string currentKeyCollectableId;
    public List<string> playerInventoryItemIds = new List<string>();
    public List<PickableItemSaveData> pickableItems = new List<PickableItemSaveData>();
    public List<CollectableSaveData> collectables = new List<CollectableSaveData>();
    public List<DoorSaveData> doors = new List<DoorSaveData>();
    public List<LiftSaveData> lifts = new List<LiftSaveData>();
    public List<AgentSaveData> agents = new List<AgentSaveData>();
}

[Serializable]
public class SaveSlotInfo
{
    public string slotId;
    public string filePath;
    public string displayName;
    public string savedAtUtc;
    public string sceneName;
    public int levelIndex;
    public int money;
}

[Serializable]
public class SaveTransformData
{
    public Vector3 position;
    public Quaternion rotation;

    public SaveTransformData()
    {
    }

    public SaveTransformData(Transform transform)
    {
        position = transform.position;
        rotation = transform.rotation;
    }

    public void ApplyTo(Transform transform)
    {
        transform.SetPositionAndRotation(position, rotation);
    }
}

[Serializable]
public class QuestSaveData
{
    public string currentQuestId;
    public int currentCollectAmount;
    public int currentCollectTarget;
    public List<string> completedQuestIds = new List<string>();
}

[Serializable]
public class PickableItemSaveData
{
    public string id;
    public bool isCollected;
    public bool isInInventory;
    public int inventoryIndex;
    public bool activeSelf;
    public SaveTransformData transform;
}

[Serializable]
public class CollectableSaveData
{
    public string id;
    public bool activeSelf;
    public SaveTransformData transform;
}

[Serializable]
public class DoorSaveData
{
    public string id;
    public bool isOpen;
}

[Serializable]
public class LiftSaveData
{
    public string id;
    public bool isAtTop;
}

[Serializable]
public class AgentSaveData
{
    public SaveTransformData transform;
}
