using UnityEditor;
using UnityEngine;

/// <summary>Утилиты для QuestLevelConfig: назначить конфиг префабу и выгрузить устаревшие поля QM при их наличии (на откате YAML).</summary>
public static class QuestLevelConfigMigrationTools
{
    public const string LevelPrefabPath = "Assets/_Game/Prefabs/Game/Levels/Level.prefab";

    const string DefaultConfigAssetPath = "Assets/_Game/Content/Levels/QuestLevel_Main.asset";

    [MenuItem("Tools/Quest/Assign Main QuestLevelConfig To Level Prefab")]
    public static void AssignMainConfigToLevelPrefab()
    {
        QuestLevelConfig cfg = AssetDatabase.LoadAssetAtPath<QuestLevelConfig>(DefaultConfigAssetPath);
        if (cfg == null)
        {
            EditorUtility.DisplayDialog("Quest", $"Не найден ресурс по пути:\n{DefaultConfigAssetPath}", "OK");
            return;
        }

        AssignConfigAtPrefabPath(LevelPrefabPath, cfg);
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Quest", "QuestLevel_Main назначен на QuestManager в префабе Level.", "OK");
    }

    /// <summary>Копирует устаревшие поля <c>_quests</c> и <c>_questDestinationMarkerPrefab</c>, если они ещё присутствуют у QuestManager после отката префаба и т.п.</summary>
    [MenuItem("Tools/Quest/Copy Embedded Quest Serialized Data From Level Prefab To New Config…")]
    public static void CopyEmbeddedQuestDataFromLevelPrefabToNewAsset()
    {
        if (!AssetDatabase.LoadAssetAtPath<GameObject>(LevelPrefabPath))
        {
            EditorUtility.DisplayDialog("Quest", $"Префаб не найден:\n{LevelPrefabPath}", "OK");
            return;
        }

        string assetPath =
            EditorUtility.SaveFilePanelInProject("Новый QuestLevelConfig", "QuestLevel_Copy", "asset", string.Empty);

        if (string.IsNullOrEmpty(assetPath))
            return;

        GameObject prefabContents = PrefabUtility.LoadPrefabContents(LevelPrefabPath);
        try
        {
            QuestManager manager = prefabContents.GetComponentInChildren<QuestManager>(true);
            if (manager == null)
            {
                EditorUtility.DisplayDialog("Quest", "Внутри префаба Level нет QuestManager.", "OK");
                return;
            }

            SerializedObject src = new SerializedObject(manager);
            SerializedProperty questsSrc = src.FindProperty("_quests");
            SerializedProperty markerSrc = src.FindProperty("_questDestinationMarkerPrefab");

            bool hasEmbeddedData = questsSrc != null || markerSrc != null;
            if (!hasEmbeddedData)
            {
                EditorUtility.DisplayDialog(
                    "Quest",
                    "В QuestManager этого префаба нет сохранённых полей _quests / _questDestinationMarkerPrefab "
                    + "(сериализованных старыми версиями скрипта). Скопировать нечего.",
                    "OK");
                return;
            }

            QuestLevelConfig cfg = ScriptableObject.CreateInstance<QuestLevelConfig>();

            SerializedObject dest = new SerializedObject(cfg);
            SerializedProperty questsDest = dest.FindProperty("_quests");
            if (questsSrc != null && questsDest != null)
                SerializedPropertyLegacyCopyUtility.Copy(questsDest, questsSrc);

            SerializedProperty markerDest = dest.FindProperty("_questDestinationMarkerPrefab");
            if (markerSrc != null && markerDest != null)
                SerializedPropertyLegacyCopyUtility.Copy(markerDest, markerSrc);

            dest.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(cfg, assetPath);
            bool assignNow = EditorUtility.DisplayDialog(
                "Quest",
                "Тексты квестов скопированы в новый ресурс.\nЗаписать ссылку QuestLevelConfig в QuestManager этого префаба и удалить встроенные данные?",
                "Да",
                "Нет");

            if (assignNow)
            {
                SerializedObject mgrFresh = new SerializedObject(manager);

                SerializedProperty cfgPropDest = mgrFresh.FindProperty("_questConfig");
                if (cfgPropDest != null)
                    cfgPropDest.objectReferenceValue = cfg;

                SerializedProperty questsOnManager = mgrFresh.FindProperty("_quests");
                if (questsOnManager != null && questsOnManager.isArray)
                    questsOnManager.ClearArray();

                SerializedProperty markerRem = mgrFresh.FindProperty("_questDestinationMarkerPrefab");
                if (markerRem != null)
                    markerRem.objectReferenceValue = null;

                mgrFresh.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(prefabContents, LevelPrefabPath);
            }

            AssetDatabase.SaveAssets();
            Selection.activeObject = cfg;
            EditorGUIUtility.PingObject(cfg);

            EditorUtility.DisplayDialog("Quest", "Готово. Проверьте ссылки на Transform префаба в инспекторе ресурса.", "OK");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabContents);
        }
    }

    static void AssignConfigAtPrefabPath(string prefabPath, QuestLevelConfig cfg)
    {
        GameObject prefabContents = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            QuestManager manager = prefabContents.GetComponentInChildren<QuestManager>(true);
            if (manager == null)
                return;

            SerializedObject mgrSo = new SerializedObject(manager);
            SerializedProperty p = mgrSo.FindProperty("_questConfig");
            if (p == null)
                return;

            p.objectReferenceValue = cfg;
            mgrSo.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(prefabContents, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabContents);
        }
    }
}

/// <summary>Рекурсивное копирование полей через <see cref="SerializedProperty"/>, если <c>CopyFromSerializedProperty</c> недоступен в вашей сборке Unity.</summary>
static class SerializedPropertyLegacyCopyUtility
{
    public static void Copy(SerializedProperty dest, SerializedProperty src)
    {
        if (src == null || dest == null)
            return;

        if (src.isArray && dest.isArray)
        {
            dest.ClearArray();
            dest.arraySize = src.arraySize;
            for (int i = 0; i < src.arraySize; i++)
                Copy(dest.GetArrayElementAtIndex(i), src.GetArrayElementAtIndex(i));
            return;
        }

        if (src.hasChildren)
        {
            SerializedProperty srcIter = src.Copy();
            SerializedProperty dstIter = dest.Copy();
            if (!srcIter.Next(true))
                return;
            dstIter.Next(true);
            Copy(dstIter, srcIter);
            while (srcIter.Next(false))
            {
                if (!dstIter.Next(false))
                    break;
                Copy(dstIter, srcIter);
            }
            return;
        }

        switch (src.propertyType)
        {
            case SerializedPropertyType.Integer:
            case SerializedPropertyType.LayerMask:
            case SerializedPropertyType.Character:
                dest.intValue = src.intValue;
                break;
            case SerializedPropertyType.Boolean:
                dest.boolValue = src.boolValue;
                break;
            case SerializedPropertyType.Float:
                dest.floatValue = src.floatValue;
                break;
            case SerializedPropertyType.String:
                dest.stringValue = src.stringValue;
                break;
            case SerializedPropertyType.Color:
                dest.colorValue = src.colorValue;
                break;
            case SerializedPropertyType.ObjectReference:
                dest.objectReferenceValue = src.objectReferenceValue;
                break;
            case SerializedPropertyType.Enum:
                dest.enumValueIndex = src.enumValueIndex;
                break;
            case SerializedPropertyType.Vector2:
                dest.vector2Value = src.vector2Value;
                break;
            case SerializedPropertyType.Vector3:
                dest.vector3Value = src.vector3Value;
                break;
            case SerializedPropertyType.Vector4:
                dest.vector4Value = src.vector4Value;
                break;
            case SerializedPropertyType.Rect:
                dest.rectValue = src.rectValue;
                break;
            case SerializedPropertyType.AnimationCurve:
                dest.animationCurveValue = src.animationCurveValue;
                break;
            case SerializedPropertyType.Bounds:
                dest.boundsValue = src.boundsValue;
                break;
            case SerializedPropertyType.Quaternion:
                dest.quaternionValue = src.quaternionValue;
                break;
            case SerializedPropertyType.Vector2Int:
                dest.vector2IntValue = src.vector2IntValue;
                break;
            case SerializedPropertyType.Vector3Int:
                dest.vector3IntValue = src.vector3IntValue;
                break;
            case SerializedPropertyType.RectInt:
                dest.rectIntValue = src.rectIntValue;
                break;
            case SerializedPropertyType.BoundsInt:
                dest.boundsIntValue = src.boundsIntValue;
                break;
            case SerializedPropertyType.ExposedReference:
            case SerializedPropertyType.ManagedReference:
            case SerializedPropertyType.Generic:
            case SerializedPropertyType.ArraySize:
            case SerializedPropertyType.FixedBufferSize:
                break;
        }
    }
}
