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

    // UI
    public static UnityAction OnNextLevelButtonPressed;
    public static void NextLevelButtonPressed()
    {
        Debug.Log($"<color={debugColor}>NextLevelButtonPressed</color>");
        OnNextLevelButtonPressed?.Invoke();
    }

    #endregion
}