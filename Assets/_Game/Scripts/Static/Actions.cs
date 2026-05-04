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
        Debug.Log($"<color={debugColor}>QuestItemTurnedIn</color> {questId}");
        OnQuestItemTurnedIn?.Invoke(questId);
    }

    // UI
    public static UnityAction OnNextLevelButtonPressed;
    public static void NextLevelButtonPressed()
    {
        Debug.Log($"<color={debugColor}>NextLevelButtonPressed</color>");
        OnNextLevelButtonPressed?.Invoke();
    }

    #endregion
}