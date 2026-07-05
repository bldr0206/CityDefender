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
    GameObject _levelRoot;
    Dictionary<string, Breakable> _breakableLookupBySaveId;

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

        _levelRoot = levelRoot;
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

    public SaveData PeekAutoSaveData()
    {
        return _saveService.LoadAutoSave();
    }

    public void SaveAutoCheckpoint()
    {
        SaveData data = CaptureSaveData("autosave");
        data.displayName = "Auto-save";
        _saveService.SaveAutoSave(data);
    }

    public void ResetAutoProgressAndReload()
    {
        _saveService.DeleteAutoSave();
        Game.ClearPendingLoadSlot();
        ResetBlockingSequences();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public string GetDefaultSaveName()
    {
        string questName = _questManager != null ? _questManager.GetCurrentQuestSaveName() : "Level";
        return $"{questName}_{DateTime.Now:yyyy-MM-dd_HH-mm}";
    }

    public void ApplyLoadedData(SaveData data, bool resumeFromAutoCheckpoint = false)
    {
        if (data == null) return;

        _breakableLookupBySaveId = BuildBreakableLookup();

        ResetBlockingSequences();
        _levelValuesManager.SetMoney(data.money);
        RestoreLifts(data.lifts);
        _playerController.RestoreSaveData(data.playerTransform);

        RestoreBreakables(data.breakables);
        RestoreCollectables(data.collectables);
        RestorePickablesAndInventory(data);
        RestoreDoors(data.doors);
        int restoredAgentCount = RestoreAgents(data.agents);
        Game.SetHiredAgentsCount(restoredAgentCount);
        if (_questManager != null)
            _questManager.RestoreSaveData(data.quest, resumeFromAutoCheckpoint);
        Actions.LoadCompleted(data.slotId);
        Game.ClearPendingLoadSlot();
        _breakableLookupBySaveId = null;
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
            playerInventoryItemIds = _playerCollector.CaptureInventoryItemIds(),
        };
        SaveData.SetHeldKeyPickableSaveId(data, _playerCollector.CaptureHeldKeyPickableSaveId());

        CapturePickableItems(data);
        CaptureCollectables(data);
        CaptureDoors(data);
        CaptureLifts(data);
        CaptureBreakables(data);
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
        string heldKeyId = SaveData.ResolveHeldKeyPickableSaveId(data);
        List<PickableItem> items = GetPickablesInLevelOrdered();
        for (int i = 0; i < items.Count; i++)
        {
            PickableItem item = items[i];
            int inventoryIndex = _playerCollector.GetInventoryIndex(item);
            bool carriedKey =
                !string.IsNullOrEmpty(heldKeyId)
                && item.SaveId == heldKeyId
                && item.Type == PickableItemType.Key;

            data.pickableItems.Add(
                item.CaptureSaveData(inventoryIndex >= 0, inventoryIndex, carriedKey));
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

    void CaptureBreakables(SaveData data)
    {
        foreach (Breakable b in BuildBreakableLookup().Values)
            data.breakables.Add(b.CaptureSaveData());
    }

    /// <summary>Все Breakable под корнем уровня (включая неактивные в иерархии); дубликаты SaveId — последний в иерархии, как при RestoreBreakables.</summary>
    public List<Breakable> GetBreakablesInLevel()
    {
        return new List<Breakable>(BuildBreakableLookup().Values);
    }

    void CaptureAgents(SaveData data)
    {
        List<Agent> agents = SaveableRegistry.GetAll<Agent>();
        for (int i = 0; i < agents.Count; i++)
        {
            if (agents[i].IsForSale) continue;
            data.agents.Add(agents[i].CaptureSaveData());
        }
    }

    void RestorePickablesAndInventory(SaveData data)
    {
        Dictionary<string, Queue<PickableItem>> pools = CreatePickableRestoreQueues();
        string heldKeyId = SaveData.ResolveHeldKeyPickableSaveId(data);

        _playerCollector.ClearHeldDoorKeySlotOnly();
        _playerCollector.RestoreInventory(data.playerInventoryItemIds, pools);

        if (data.pickableItems != null)
        {
            for (int i = 0; i < data.pickableItems.Count; i++)
            {
                PickableItemSaveData itemData = data.pickableItems[i];
                if (itemData.isInInventory) continue;

                if (itemData.isCarriedAsDoorKey)
                {
                    if (
                        pools.TryGetValue(itemData.id, out Queue<PickableItem> qHeld)
                        && qHeld.Count > 0
                        )
                        _playerCollector.RestoreHeldKeyPickable(qHeld.Dequeue());
                    continue;
                }

                if (!pools.TryGetValue(itemData.id, out Queue<PickableItem> q) || q.Count == 0)
                {
                    TryRestoreOrphanDroppedPickable(itemData);
                    continue;
                }

                PickableItem item = q.Dequeue();
                item.RestoreSaveData(itemData);
            }
        }

        if (
            !_playerCollector.HoldsDoorKey
            && !string.IsNullOrEmpty(heldKeyId)
            )
        {
            if (
                pools.TryGetValue(heldKeyId, out Queue<PickableItem> qKey)
                && qKey.Count > 0
                )
                _playerCollector.RestoreHeldKeyPickable(qKey.Dequeue());
            else if (
                SaveableRegistry.TryGet(
                    heldKeyId,
                    out PickableItem keyPickable
                    )
                )
                _playerCollector.RestoreHeldKeyPickable(keyPickable);
        }

        DiscardLeftoverPickablePools(pools);
    }

    List<PickableItem> GetPickablesInLevelOrdered()
    {
        List<PickableItem> list;
        if (_levelRoot != null)
            list = new List<PickableItem>(_levelRoot.GetComponentsInChildren<PickableItem>(true));
        else
            list = SaveableRegistry.GetAll<PickableItem>();

        list.Sort(ComparePickablesForSaveOrder);
        return list;
    }

    static int ComparePickablesForSaveOrder(PickableItem a, PickableItem b)
    {
        if (ReferenceEquals(a, b)) return 0;
        int c = string.CompareOrdinal(a.SaveId, b.SaveId);
        if (c != 0) return c;
        return a.GetInstanceID().CompareTo(b.GetInstanceID());
    }

    Dictionary<string, Queue<PickableItem>> CreatePickableRestoreQueues()
    {
        Dictionary<string, List<PickableItem>> map = new Dictionary<string, List<PickableItem>>();
        List<PickableItem> all = GetPickablesInLevelOrdered();
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

    static void DiscardLeftoverPickablePools(Dictionary<string, Queue<PickableItem>> pools)
    {
        foreach (KeyValuePair<string, Queue<PickableItem>> kvp in pools)
        {
            Queue<PickableItem> q = kvp.Value;
            while (q.Count > 0)
                q.Dequeue().DiscardExcessAfterLoad();
        }
    }

    void TryRestoreOrphanDroppedCollectable(CollectableSaveData data)
    {
        if (data == null || !data.spawnedLootFromBreak) return;
        if (data.lootEntryIndex < 0 || string.IsNullOrEmpty(data.spawnedByBreakableId)) return;
        if (!TryResolveBreakable(data.spawnedByBreakableId, out Breakable breakable)) return;
        breakable.TryRestoreDroppedCollectable(data);
    }

    void TryRestoreOrphanDroppedPickable(PickableItemSaveData data)
    {
        if (data == null || !data.spawnedLootFromBreak) return;
        if (data.lootEntryIndex < 0 || string.IsNullOrEmpty(data.spawnedByBreakableId)) return;
        if (!TryResolveBreakable(data.spawnedByBreakableId, out Breakable breakable)) return;
        breakable.TryRestoreDroppedPickable(data);
    }

    void RestoreCollectables(List<CollectableSaveData> collectables)
    {
        if (collectables == null) return;

        for (int i = 0; i < collectables.Count; i++)
        {
            CollectableSaveData collectableData = collectables[i];
            if (SaveableRegistry.TryGet(collectableData.id, out Collectable collectable))
                collectable.RestoreSaveData(collectableData);
            else
                TryRestoreOrphanDroppedCollectable(collectableData);
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

    void RestoreBreakables(List<BreakableSaveData> breakables)
    {
        if (breakables == null) return;

        Dictionary<string, Breakable> byId = _breakableLookupBySaveId;

        for (int i = 0; i < breakables.Count; i++)
        {
            BreakableSaveData breakableData = breakables[i];
            if (string.IsNullOrEmpty(breakableData?.id)) continue;

            Breakable breakable = null;
            if (byId == null || !byId.TryGetValue(breakableData.id, out breakable))
                SaveableRegistry.TryGet(breakableData.id, out breakable);

            if (breakable != null)
                breakable.RestoreSaveData(breakableData);
        }
    }

    Dictionary<string, Breakable> BuildBreakableLookup()
    {
        var map = new Dictionary<string, Breakable>();
        if (_levelRoot == null) return map;

        Breakable[] list = _levelRoot.GetComponentsInChildren<Breakable>(true);
        for (int i = 0; i < list.Length; i++)
        {
            Breakable b = list[i];
            if (b == null) continue;

            string id = b.SaveId;
            if (string.IsNullOrEmpty(id)) continue;

            if (map.TryGetValue(id, out Breakable previous) && previous != b)
                Debug.LogWarning($"Duplicate Breakable SaveId '{id}' on {_levelRoot.name}. Using last in hierarchy.");

            map[id] = b;
        }

        return map;
    }

    bool TryResolveBreakable(string id, out Breakable breakable)
    {
        breakable = null;
        if (string.IsNullOrEmpty(id)) return false;

        if (SaveableRegistry.TryGet(id, out breakable))
            return true;

        if (_breakableLookupBySaveId != null && _breakableLookupBySaveId.TryGetValue(id, out breakable))
            return true;

        return false;
    }

    int RestoreAgents(List<AgentSaveData> agents)
    {
        if (_traderNPC == null || agents == null) return 0;

        List<Agent> currentAgents = SaveableRegistry.GetAll<Agent>();
        for (int i = 0; i < currentAgents.Count; i++)
        {
            if (currentAgents[i].IsForSale) continue;
            UnityEngine.Object.Destroy(currentAgents[i].gameObject);
        }

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
