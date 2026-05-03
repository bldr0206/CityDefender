// Этот файл используется для хранения переменных и статических методов касаемых игры. 
// Напирмер он будет знать текущий уровень, сколько у игрока валюты и другие подобные штуки

using UnityEngine;

public static class Game
{
    public static bool IsPaused { get; private set; }

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
