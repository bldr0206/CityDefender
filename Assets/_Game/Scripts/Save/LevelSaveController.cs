using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

public class LevelSaveController : IInitializable, IDisposable
{
    SaveService _saveService;
    LevelValuesManager _levelValuesManager;
    PlayerController _playerController;
    PlayerCollector _playerCollector;
    DialogueScreen _dialogueScreen;
    QuestManager _questManager;
    TraderNPC _traderNPC;

    [Inject]
    public void Construct(
        SaveService saveService,
        LevelValuesManager levelValuesManager,
        PlayerController playerController,
        PlayerCollector playerCollector,
        DialogueScreen dialogueScreen)
    {
        _saveService = saveService;
        _levelValuesManager = levelValuesManager;
        _playerController = playerController;
        _playerCollector = playerCollector;
        _dialogueScreen = dialogueScreen;
    }

    public void Initialize()
    {
        Actions.OnSaveRequested += Save;
        Actions.OnLoadRequested += Load;
    }

    public void Dispose()
    {
        Actions.OnSaveRequested -= Save;
        Actions.OnLoadRequested -= Load;
    }

    public List<SaveSlotInfo> GetSlots()
    {
        return _saveService.GetSlots();
    }

    public void SetLevelContext(GameObject levelRoot)
    {
        if (levelRoot == null) return;

        _questManager = levelRoot.GetComponentInChildren<QuestManager>(true);
        _traderNPC = levelRoot.GetComponentInChildren<TraderNPC>(true);
    }

    public void Save(string slotId)
    {
        SaveData data = CaptureSaveData(slotId);
        _saveService.Save(slotId, data);
        Actions.SaveCompleted(slotId);
    }

    public string SaveToFile(string fileName, string displayName)
    {
        SaveData data = CaptureSaveData(fileName);
        data.displayName = displayName;
        string path = _saveService.SaveFile(fileName, data);
        Actions.SaveCompleted(data.slotId);
        return path;
    }

    public void Load(string slotId)
    {
        if (_saveService.Load(slotId) == null)
        {
            Debug.LogWarning($"Save slot not found: {slotId}");
            return;
        }

        ResetBlockingSequences();
        Game.SetPendingLoadSlot(slotId);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void LoadFromFile(string filePath)
    {
        if (_saveService.LoadFile(filePath) == null)
        {
            Debug.LogWarning($"Save file not found: {filePath}");
            return;
        }

        ResetBlockingSequences();
        Game.SetPendingLoadFile(filePath);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void Delete(string slotId)
    {
        _saveService.Delete(slotId);
    }

    public void DeleteFile(string filePath)
    {
        _saveService.DeleteFile(filePath);
    }

    public SaveData LoadPendingSaveData()
    {
        if (!string.IsNullOrEmpty(Game.PendingLoadFilePath))
        {
            SaveData fileData = _saveService.LoadFile(Game.PendingLoadFilePath);
            if (fileData == null)
                Game.ClearPendingLoadSlot();

            return fileData;
        }

        if (string.IsNullOrEmpty(Game.PendingLoadSlotId))
            return null;

        SaveData data = _saveService.Load(Game.PendingLoadSlotId);
        if (data == null)
            Game.ClearPendingLoadSlot();

        return data;
    }

    public string GetDefaultSaveName()
    {
        string questName = _questManager != null ? _questManager.GetCurrentQuestSaveName() : "Level";
        return $"{questName}_{DateTime.Now:yyyy-MM-dd_HH-mm}";
    }

    public void ApplyLoadedData(SaveData data)
    {
        if (data == null) return;

        ResetBlockingSequences();
        _levelValuesManager.SetMoney(data.money);
        RestoreLifts(data.lifts);
        _playerController.RestoreSaveData(data.playerTransform);

        RestoreCollectables(data.collectables);
        RestorePickablesAndInventory(data);
        _playerCollector.RestoreCurrentKey(data.currentKeyCollectableId);
        RestoreDoors(data.doors);
        int restoredAgentCount = RestoreAgents(data.agents);
        if (_questManager != null)
            _questManager.RestoreSaveData(data.quest);

        Game.SetHiredAgentsCount(restoredAgentCount);
        Actions.LoadCompleted(data.slotId);
        Game.ClearPendingLoadSlot();
    }

    SaveData CaptureSaveData(string slotId)
    {
        SaveData data = new SaveData
        {
            slotId = slotId,
            displayName = slotId,
            sceneName = SceneManager.GetActiveScene().name,
            levelIndex = Game.CurrentLevelIndex,
            money = _levelValuesManager.GetMoney(),
            hiredAgentsCount = Game.HiredAgentsCount,
            isLevelFinished = Game.IsLevelFinished,
            playerTransform = _playerController.CaptureSaveData(),
            quest = _questManager != null ? _questManager.CaptureSaveData() : new QuestSaveData(),
            currentKeyCollectableId = _playerCollector.CaptureCurrentKeyId(),
            playerInventoryItemIds = _playerCollector.CaptureInventoryItemIds(),
        };

        CapturePickableItems(data);
        CaptureCollectables(data);
        CaptureDoors(data);
        CaptureLifts(data);
        CaptureAgents(data);
        return data;
    }

    void ResetBlockingSequences()
    {
        _dialogueScreen.Cancel();
        QuestCutscene.CancelAllActive();
    }

    void CapturePickableItems(SaveData data)
    {
        List<PickableItem> items = SaveableRegistry.GetAll<PickableItem>();
        for (int i = 0; i < items.Count; i++)
        {
            PickableItem item = items[i];
            int inventoryIndex = _playerCollector.GetInventoryIndex(item);
            data.pickableItems.Add(item.CaptureSaveData(inventoryIndex >= 0, inventoryIndex));
        }
    }

    void CaptureCollectables(SaveData data)
    {
        List<Collectable> collectables = SaveableRegistry.GetAll<Collectable>();
        for (int i = 0; i < collectables.Count; i++)
            data.collectables.Add(collectables[i].CaptureSaveData());
    }

    void CaptureDoors(SaveData data)
    {
        List<Door> doors = SaveableRegistry.GetAll<Door>();
        for (int i = 0; i < doors.Count; i++)
            data.doors.Add(doors[i].CaptureSaveData());
    }

    void CaptureLifts(SaveData data)
    {
        List<Lift> lifts = SaveableRegistry.GetAll<Lift>();
        for (int i = 0; i < lifts.Count; i++)
            data.lifts.Add(lifts[i].CaptureSaveData());
    }

    void CaptureAgents(SaveData data)
    {
        List<Agent> agents = SaveableRegistry.GetAll<Agent>();
        for (int i = 0; i < agents.Count; i++)
            data.agents.Add(agents[i].CaptureSaveData());
    }

    void RestorePickablesAndInventory(SaveData data)
    {
        Dictionary<string, Queue<PickableItem>> pools = CreatePickableRestoreQueues();
        _playerCollector.RestoreInventory(data.playerInventoryItemIds, pools);

        if (data.pickableItems == null) return;

        for (int i = 0; i < data.pickableItems.Count; i++)
        {
            PickableItemSaveData itemData = data.pickableItems[i];
            if (itemData.isInInventory) continue;
            if (!pools.TryGetValue(itemData.id, out Queue<PickableItem> q) || q.Count == 0) continue;

            PickableItem item = q.Dequeue();
            item.RestoreSaveData(itemData);
        }
    }

    static Dictionary<string, Queue<PickableItem>> CreatePickableRestoreQueues()
    {
        Dictionary<string, List<PickableItem>> map = new Dictionary<string, List<PickableItem>>();
        List<PickableItem> all = SaveableRegistry.GetAll<PickableItem>();
        for (int i = 0; i < all.Count; i++)
        {
            PickableItem p = all[i];
            string id = p.SaveId;
            if (!map.TryGetValue(id, out List<PickableItem> list))
            {
                list = new List<PickableItem>();
                map[id] = list;
            }

            list.Add(p);
        }

        Dictionary<string, Queue<PickableItem>> queues = new Dictionary<string, Queue<PickableItem>>();
        foreach (KeyValuePair<string, List<PickableItem>> kvp in map)
        {
            kvp.Value.Sort((a, b) => a.GetInstanceID().CompareTo(b.GetInstanceID()));
            queues[kvp.Key] = new Queue<PickableItem>(kvp.Value);
        }

        return queues;
    }

    void RestoreCollectables(List<CollectableSaveData> collectables)
    {
        if (collectables == null) return;

        for (int i = 0; i < collectables.Count; i++)
        {
            CollectableSaveData collectableData = collectables[i];
            if (SaveableRegistry.TryGet(collectableData.id, out Collectable collectable))
                collectable.RestoreSaveData(collectableData);
        }
    }

    void RestoreDoors(List<DoorSaveData> doors)
    {
        if (doors == null) return;

        for (int i = 0; i < doors.Count; i++)
        {
            DoorSaveData doorData = doors[i];
            if (SaveableRegistry.TryGet(doorData.id, out Door door))
                door.RestoreSaveData(doorData);
        }
    }

    void RestoreLifts(List<LiftSaveData> lifts)
    {
        if (lifts == null) return;

        for (int i = 0; i < lifts.Count; i++)
        {
            LiftSaveData liftData = lifts[i];
            if (SaveableRegistry.TryGet(liftData.id, out Lift lift))
                lift.RestoreSaveData(liftData);
        }
    }

    int RestoreAgents(List<AgentSaveData> agents)
    {
        if (_traderNPC == null || agents == null) return 0;

        List<Agent> currentAgents = SaveableRegistry.GetAll<Agent>();
        for (int i = 0; i < currentAgents.Count; i++)
            UnityEngine.Object.Destroy(currentAgents[i].gameObject);

        int restoredCount = 0;
        for (int i = 0; i < agents.Count; i++)
        {
            AgentSaveData agentData = agents[i];
            Agent agent = _traderNPC.SpawnAgent(agentData.transform.position, agentData.transform.rotation);
            if (agent != null)
            {
                agent.RestoreSaveData(agentData);
                restoredCount++;
            }
        }

        return restoredCount;
    }
}
