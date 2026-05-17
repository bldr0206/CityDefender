using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>Выпадающий список quest id из QuestManager (открытые объекты и префабы).</summary>
public static class QuestIdEditorUtility
{
    const string NoneOption = "<None>";

    public static void DrawQuestIdPopup(SerializedProperty questId, string label = "Quest Id")
    {
        if (questId == null)
            return;

        List<string> questIds = GetQuestIds();
        List<string> options = new List<string> { NoneOption };
        options.AddRange(questIds);

        string currentValue = questId.hasMultipleDifferentValues ? NoneOption : questId.stringValue;
        int currentIndex = string.IsNullOrEmpty(currentValue) ? 0 : options.IndexOf(currentValue);

        if (currentIndex < 0)
        {
            options.Add($"{currentValue} (missing)");
            currentIndex = options.Count - 1;
        }

        EditorGUI.showMixedValue = questId.hasMultipleDifferentValues;
        int selectedIndex = EditorGUILayout.Popup(label, currentIndex, options.ToArray());
        EditorGUI.showMixedValue = false;

        if (selectedIndex == currentIndex) return;

        questId.stringValue = selectedIndex == 0
            ? string.Empty
            : options[selectedIndex].Replace(" (missing)", string.Empty);
    }

    static List<string> GetQuestIds()
    {
        HashSet<string> ids = new HashSet<string>();
        AddQuestIdsFromOpenObjects(ids);
        AddQuestIdsFromPrefabs(ids);
        AddQuestIdsFromQuestConfigs(ids);

        List<string> result = new List<string>(ids);
        result.Sort();
        return result;
    }

    static void AddQuestIdsFromOpenObjects(HashSet<string> ids)
    {
        QuestManager[] managers = Resources.FindObjectsOfTypeAll<QuestManager>();

        for (int i = 0; i < managers.Length; i++)
            AddQuestIdsFromQuestManager(ids, managers[i]);
    }

    static void AddQuestIdsFromPrefabs(HashSet<string> ids)
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab");

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            QuestManager[] managers = prefab.GetComponentsInChildren<QuestManager>(true);
            for (int j = 0; j < managers.Length; j++)
                AddQuestIdsFromQuestManager(ids, managers[j]);
        }
    }

    static void AddQuestIdsFromQuestConfigs(HashSet<string> ids)
    {
        string[] cfgGuids = AssetDatabase.FindAssets("t:" + nameof(QuestLevelConfig));

        for (int i = 0; i < cfgGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(cfgGuids[i]);
            QuestLevelConfig cfg = AssetDatabase.LoadAssetAtPath<QuestLevelConfig>(path);
            if (cfg != null)
                AddQuestIdsFromQuestList(ids, new SerializedObject(cfg).FindProperty("_quests"));
        }
    }

    static void AddQuestIdsFromQuestManager(HashSet<string> ids, QuestManager manager)
    {
        if (manager == null) return;

        SerializedObject mgrSo = new SerializedObject(manager);
        SerializedProperty legacyQuests = mgrSo.FindProperty("_quests");

        AddQuestIdsFromQuestList(ids, legacyQuests);

        SerializedProperty cfgProp = mgrSo.FindProperty("_questConfig");

        QuestLevelConfig cfg = cfgProp?.objectReferenceValue as QuestLevelConfig;
        if (cfg == null)
            return;

        AddQuestIdsFromQuestList(ids, new SerializedObject(cfg).FindProperty("_quests"));
    }

    static void AddQuestIdsFromQuestList(HashSet<string> ids, SerializedProperty quests)
    {
        if (quests == null || !quests.isArray) return;

        for (int i = 0; i < quests.arraySize; i++)
        {
            SerializedProperty id = quests.GetArrayElementAtIndex(i).FindPropertyRelative("id");

            if (id != null && !string.IsNullOrEmpty(id.stringValue))
                ids.Add(id.stringValue);
        }
    }
}
