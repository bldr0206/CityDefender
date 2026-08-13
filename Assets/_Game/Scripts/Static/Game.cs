// Этот файл используется для хранения переменных и статических методов касаемых игры. 
// Напирмер он будет знать текущий уровень, сколько у игрока валюты и другие подобные штуки

using UnityEngine;

public static class Game
{
    public static bool IsPaused { get; private set; }
    public static bool HasHiredBots => _hiredBotsCount > 0;
    public static int HiredBotsCount => _hiredBotsCount;
    public static int CurrentLevelIndex { get; private set; }
    public static bool IsLevelFinished { get; private set; }
    public static string PendingLoadSlotId { get; private set; }
    public static string PendingLoadFilePath { get; private set; }

    private static int _hiredBotsCount;

    public static void SetCurrentLevelIndex(int levelIndex)
    {
        CurrentLevelIndex = levelIndex;
    }

    public static void RegisterHiredBot()
    {
        _hiredBotsCount++;
    }

    public static void SetHiredBotsCount(int count)
    {
        _hiredBotsCount = Mathf.Max(0, count);
    }

    public static void SetLevelFinished(bool isFinished)
    {
        IsLevelFinished = isFinished;
    }

    public static void ResetHiredBots()
    {
        _hiredBotsCount = 0;
    }

    public static void SetPendingLoadSlot(string slotId)
    {
        PendingLoadSlotId = slotId;
        PendingLoadFilePath = null;
    }

    public static void SetPendingLoadFile(string filePath)
    {
        PendingLoadFilePath = filePath;
        PendingLoadSlotId = null;
    }

    public static void ClearPendingLoadSlot()
    {
        PendingLoadSlotId = null;
        PendingLoadFilePath = null;
    }

    public static void PauseGame()
    {
        if (IsPaused) return;

        IsPaused = true;
        Time.timeScale = 0f;
        Actions.GamePaused();
    }

    public static void ResumeGame()
    {
        if (!IsPaused) return;

        IsPaused = false;
        Time.timeScale = 1f;
        Actions.GameResumed();
    }
}
