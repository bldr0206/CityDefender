// Этот скрипт вызывает события из игры, к нему обращаемся чтобы вызвать какое либо событие. 
// На него подписываемся чтобы слушать события


using UnityEngine;
using UnityEngine.Events;

public static class Actions
{
    static string debugColor = "#3aa6ff";

    #region Actions:

    // GAME
    public static UnityAction OnLevelStarted;
    public static void LevelStarted()
    {
        Debug.Log($"<color={debugColor}>LevelStarted</color>");
        OnLevelStarted?.Invoke();
    }

    public static UnityAction OnGamePaused;
    public static void GamePaused()
    {
        Debug.Log($"<color={debugColor}>GamePaused</color>");
        OnGamePaused?.Invoke();
    }

    public static UnityAction OnGameResumed;
    public static void GameResumed()
    {
        Debug.Log($"<color={debugColor}>GameResumed</color>");
        OnGameResumed?.Invoke();
    }

    public static UnityAction OnCutsceneStarted;
    public static void CutsceneStarted()
    {
        Debug.Log($"<color={debugColor}>CutsceneStarted</color>");
        OnCutsceneStarted?.Invoke();
    }

    public static UnityAction OnCutsceneEnded;
    public static void CutsceneEnded()
    {
        Debug.Log($"<color={debugColor}>CutsceneEnded</color>");
        OnCutsceneEnded?.Invoke();
    }

    public static UnityAction OnDialogueStarted;
    public static void DialogueStarted()
    {
        Debug.Log($"<color={debugColor}>DialogueStarted</color>");
        OnDialogueStarted?.Invoke();
    }

    public static UnityAction OnDialogueEnded;
    public static void DialogueEnded()
    {
        Debug.Log($"<color={debugColor}>DialogueEnded</color>");
        OnDialogueEnded?.Invoke();
    }

    public static UnityAction<int> OnPlayerMoneyChanged;
    public static void PlayerMoneyChanged(int currentMoney)
    {
        Debug.Log($"<color={debugColor}>PlayerMoneyChanged</color> {currentMoney}");
        OnPlayerMoneyChanged?.Invoke(currentMoney);
    }

    public static UnityAction OnPlayerReachedFinish;
    public static void PlayerReachedFinish()
    {
        Debug.Log($"<color={debugColor}>PlayerReachedFinish</color>");
        OnPlayerReachedFinish?.Invoke();
    }

    public static UnityAction<string> OnQuestDestinationReached;
    public static void QuestDestinationReached(string questId)
    {
        Debug.Log($"<color={debugColor}>QuestDestinationReached</color> {questId}");
        OnQuestDestinationReached?.Invoke(questId);
    }

    public static UnityAction<Transform> OnQuestTargetChanged;
    public static void QuestTargetChanged(Transform questTarget)
    {
        Debug.Log($"<color={debugColor}>QuestTargetChanged</color>");
        OnQuestTargetChanged?.Invoke(questTarget);
    }

    public static UnityAction<PickableItem> OnQuestPickableRegistered;
    public static void QuestPickableRegistered(PickableItem item)
    {
        Debug.Log($"<color={debugColor}>QuestPickableRegistered</color> {item.QuestId}");
        OnQuestPickableRegistered?.Invoke(item);
    }

    public static UnityAction<PickableItem> OnQuestPickableUnregistered;
    public static void QuestPickableUnregistered(PickableItem item)
    {
        Debug.Log($"<color={debugColor}>QuestPickableUnregistered</color> {item.QuestId}");
        OnQuestPickableUnregistered?.Invoke(item);
    }

    public static UnityAction<string> OnQuestItemTurnedIn;
    public static void QuestItemTurnedIn(string questId)
    {
        Debug.Log($"<color={debugColor}>QuestDeliverProgress</color> {questId}");
        OnQuestItemTurnedIn?.Invoke(questId);
    }

    public static UnityAction OnQuestCarryingPickablesChanged;
    public static void QuestCarryingPickablesChanged()
    {
        Debug.Log($"<color={debugColor}>QuestCarryingPickablesChanged</color>");
        OnQuestCarryingPickablesChanged?.Invoke();
    }

    public static UnityAction<Breakable> OnQuestBreakableRegistered;
    public static void QuestBreakableRegistered(Breakable breakable)
    {
        if (breakable == null) return;

        Debug.Log($"<color={debugColor}>QuestBreakableRegistered</color> {breakable.QuestId}");
        OnQuestBreakableRegistered?.Invoke(breakable);
    }

    public static UnityAction<Breakable> OnQuestBreakableUnregistered;
    public static void QuestBreakableUnregistered(Breakable breakable)
    {
        if (breakable == null) return;

        Debug.Log($"<color={debugColor}>QuestBreakableUnregistered</color> {breakable.QuestId}");
        OnQuestBreakableUnregistered?.Invoke(breakable);
    }

    public static UnityAction<string> OnQuestBreakableBroken;
    public static void QuestBreakableBroken(string questId)
    {
        Debug.Log($"<color={debugColor}>QuestBreakableBroken</color> {questId}");
        OnQuestBreakableBroken?.Invoke(questId);
    }

    public static UnityAction OnAgentHired;
    public static void AgentHired()
    {
        Debug.Log($"<color={debugColor}>AgentHired</color>");
        OnAgentHired?.Invoke();
    }

    // UI
    public static UnityAction OnNextLevelButtonPressed;
    public static void NextLevelButtonPressed()
    {
        Debug.Log($"<color={debugColor}>NextLevelButtonPressed</color>");
        OnNextLevelButtonPressed?.Invoke();
    }

    public static UnityAction<string> OnSaveRequested;
    public static void SaveRequested(string slotId)
    {
        Debug.Log($"<color={debugColor}>SaveRequested</color> {slotId}");
        OnSaveRequested?.Invoke(slotId);
    }

    public static UnityAction<string> OnLoadRequested;
    public static void LoadRequested(string slotId)
    {
        Debug.Log($"<color={debugColor}>LoadRequested</color> {slotId}");
        OnLoadRequested?.Invoke(slotId);
    }

    public static UnityAction<string> OnSaveCompleted;
    public static void SaveCompleted(string slotId)
    {
        Debug.Log($"<color={debugColor}>SaveCompleted</color> {slotId}");
        OnSaveCompleted?.Invoke(slotId);
    }

    public static UnityAction<string> OnLoadCompleted;
    public static void LoadCompleted(string slotId)
    {
        Debug.Log($"<color={debugColor}>LoadCompleted</color> {slotId}");
        OnLoadCompleted?.Invoke(slotId);
    }

    public static UnityAction<GameObject> OnWorldLootPickupReady;
    public static void WorldLootPickupReady(GameObject lootRoot)
    {
        OnWorldLootPickupReady?.Invoke(lootRoot);
    }

    #endregion
}