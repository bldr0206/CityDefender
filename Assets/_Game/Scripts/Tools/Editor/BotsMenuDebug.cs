using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary> Временные проверочные команды меню ботов (play mode). </summary>
public static class BotsMenuDebug
{
    [MenuItem("Tools/Bots Menu/Debug Open (Play Mode)")]
    public static void Open()
    {
        if (!Application.isPlaying) return;

        BotsMenuScreen screen = Object.FindFirstObjectByType<BotsMenuScreen>(FindObjectsInactive.Include);
        if (screen == null)
        {
            Debug.LogError("BotsMenuDebug: BotsMenuScreen not found in scene.");
            return;
        }

        screen.Open();
        Debug.Log($"BotsMenuDebug: Open() called. Paused={Game.IsPaused}");
    }

    [MenuItem("Tools/Bots Menu/Debug Close (Play Mode)")]
    public static void Close()
    {
        if (!Application.isPlaying) return;

        BotsMenuScreen screen = Object.FindFirstObjectByType<BotsMenuScreen>(FindObjectsInactive.Include);
        if (screen == null) return;

        screen.Close();
        Debug.Log($"BotsMenuDebug: Close() called. Paused={Game.IsPaused}");
    }

    [MenuItem("Tools/Bots Menu/Debug Screenshot (Play Mode)")]
    public static void Screenshot()
    {
        if (!Application.isPlaying) return;

        string path = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", "Temp", "bots_menu_screenshot.png"));
        ScreenCapture.CaptureScreenshot(path);
        Debug.Log($"BotsMenuDebug: screenshot -> {path}");
    }

    [MenuItem("Tools/Bots Menu/Debug Hire Test Bot (Play Mode)")]
    public static void HireTestBot()
    {
        if (!Application.isPlaying) return;

        TraderNPC trader = null;
        foreach (TraderNPC candidate in Object.FindObjectsByType<TraderNPC>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Object prefabRef = new SerializedObject(candidate).FindProperty("_botPrefab").objectReferenceValue;
            Debug.Log($"BotsMenuDebug: trader '{candidate.name}' active={candidate.isActiveAndEnabled} botPrefab={(prefabRef != null ? prefabRef.name : "NULL")}", candidate);
            if (prefabRef != null && candidate.isActiveAndEnabled && trader == null)
                trader = candidate;
        }

        PlayerController player = Object.FindFirstObjectByType<PlayerController>();
        if (trader == null || player == null)
        {
            Debug.LogError("BotsMenuDebug: no usable TraderNPC (with bot prefab) or PlayerController found.");
            return;
        }

        Bot bot = trader.SpawnBot(player.transform.position + Vector3.right, Quaternion.identity);
        if (bot == null)
        {
            Debug.LogError("BotsMenuDebug: SpawnBot returned null.");
            return;
        }

        Game.RegisterHiredBot();
        bot.SellToPlayer();
        Debug.Log($"BotsMenuDebug: test bot hired. Mining={bot.Stats.GetValue(CityDef.Gameplay.Logic.BotStatType.Mining)}, spec={(bot.Specialization != null ? bot.Specialization.Id : "NULL")}");
    }
}
