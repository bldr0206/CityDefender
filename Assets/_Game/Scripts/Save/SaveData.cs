using System;
using System.Collections.Generic;
using CityDef.Gameplay.Logic;
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
    public int hiredBotsCount;
    public bool isLevelFinished;
    public SaveTransformData playerTransform;
    public QuestSaveData quest = new QuestSaveData();
    /// <summary>Pickable SaveId ключа «в руке» (новое поле).</summary>
    public string currentKeyPickableId;
    /// <summary>Старое имя JSON-поля: для JsonUtility сохранять зеркально с <see cref="currentKeyPickableId"/>; старые сейвы читаются через <see cref="ResolveHeldKeyPickableSaveId"/>.</summary>
    public string currentKeyCollectableId;

    public static string ResolveHeldKeyPickableSaveId(SaveData data)
    {
        if (data == null) return null;
        if (!string.IsNullOrEmpty(data.currentKeyPickableId)) return data.currentKeyPickableId;
        return data.currentKeyCollectableId;
    }

    public static void SetHeldKeyPickableSaveId(SaveData data, string id)
    {
        if (data == null) return;
        data.currentKeyPickableId = id;
        data.currentKeyCollectableId = id;
    }
    public List<string> playerInventoryItemIds = new List<string>();
    public List<PickableItemSaveData> pickableItems = new List<PickableItemSaveData>();
    public List<CollectableSaveData> collectables = new List<CollectableSaveData>();
    public List<DoorSaveData> doors = new List<DoorSaveData>();
    public List<LiftSaveData> lifts = new List<LiftSaveData>();
    public List<BreakableSaveData> breakables = new List<BreakableSaveData>();
    public List<BotSaveData> bots = new List<BotSaveData>();
    public List<SequenceTriggerSaveData> sequenceTriggers = new List<SequenceTriggerSaveData>();
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
    /// <summary>Прогресс счётчика для Deliver Item и Break Breakables.</summary>
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
    public bool isCarriedAsDoorKey;
    public int inventoryIndex;
    public bool activeSelf;
    public SaveTransformData transform;
    public bool spawnedLootFromBreak;
    public string spawnedByBreakableId;
    public int lootEntryIndex = -1;
}

[Serializable]
public class CollectableSaveData
{
    public string id;
    public bool activeSelf;
    public SaveTransformData transform;
    public bool spawnedLootFromBreak;
    public string spawnedByBreakableId;
    public int lootEntryIndex = -1;
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
public class BreakableSaveData
{
    public string id;
    public int health;
    public bool isBroken;
    public int lootIdCounter;
}

[Serializable]
public class BotSaveData
{
    public SaveTransformData transform;
    public string specializationId;
    public List<BotStatLevelSaveData> statLevels = new List<BotStatLevelSaveData>();
}

[Serializable]
public class SequenceTriggerSaveData
{
    public string id;
    public bool consumed;
}
