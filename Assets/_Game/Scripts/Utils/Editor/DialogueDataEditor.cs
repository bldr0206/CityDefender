using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DialogueData))]
public class DialogueDataEditor : Editor
{
    const float Spacing = 2f;

    SerializedProperty _textTable;
    SerializedProperty _lines;

    void OnEnable()
    {
        _textTable = serializedObject.FindProperty("textTable");
        _lines = serializedObject.FindProperty("lines");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawQuestManagerShortcut();

        DrawTextTable();
        EditorGUILayout.PropertyField(_lines, true);

        serializedObject.ApplyModifiedProperties();
    }

    void DrawQuestManagerShortcut()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button(new GUIContent("Quest Manager", "Выделить Quest Manager на загруженной сцене (предпочтительно тот, где используется этот диалог).")))
            {
                QuestManager manager = FindQuestManagerForDialogue((DialogueData)target);
                if (manager != null)
                {
                    Selection.activeObject = manager;
                    EditorGUIUtility.PingObject(manager);
                }
                else
                {
                    EditorUtility.DisplayDialog(
                        "Quest Manager",
                        "На загруженных сценах не найден QuestManager.",
                        "OK");
                }
            }
        }

        EditorGUILayout.Space(4f);
    }

    static QuestManager FindQuestManagerForDialogue(DialogueData dialogue)
    {
        QuestManager[] all = Resources.FindObjectsOfTypeAll<QuestManager>();
        QuestManager firstInScene = null;

        foreach (QuestManager m in all)
        {
            if (m == null) continue;
            GameObject go = m.gameObject;
            if (!go.scene.IsValid() || !go.scene.isLoaded || EditorUtility.IsPersistent(go))
                continue;

            if (firstInScene == null)
                firstInScene = m;

            if (dialogue != null && QuestManagerReferencesDialogue(m, dialogue))
                return m;
        }

        return firstInScene;
    }

    static bool QuestManagerReferencesDialogue(QuestManager manager, DialogueData dialogue)
    {
        SerializedObject mgrSo = new SerializedObject(manager);

        SerializedProperty legacyQuests = mgrSo.FindProperty("_quests");

        if (legacyQuests != null && legacyQuests.isArray && QuestArrayReferencesDialogue(legacyQuests, dialogue))
            return true;

        SerializedProperty cfgProp = mgrSo.FindProperty("_questConfig");
        if (cfgProp != null && cfgProp.objectReferenceValue != null)
        {
            SerializedObject cfgSo = new SerializedObject(cfgProp.objectReferenceValue);
            SerializedProperty q = cfgSo.FindProperty("_quests");
            if (q != null && q.isArray && QuestArrayReferencesDialogue(q, dialogue))
                return true;
        }

        return false;
    }

    static bool QuestArrayReferencesDialogue(SerializedProperty quests, DialogueData dialogue)
    {
        for (int i = 0; i < quests.arraySize; i++)
        {
            SerializedProperty quest = quests.GetArrayElementAtIndex(i);

            if (SequenceReferencesDialogue(quest.FindPropertyRelative("startSequence"), dialogue))
                return true;
            if (SequenceReferencesDialogue(quest.FindPropertyRelative("endSequence"), dialogue))
                return true;
        }

        return false;
    }

    static bool SequenceReferencesDialogue(SerializedProperty sequence, DialogueData dialogue)
    {
        if (sequence == null || !sequence.isArray)
            return false;

        for (int i = 0; i < sequence.arraySize; i++)
        {
            SerializedProperty step = sequence.GetArrayElementAtIndex(i);
            SerializedProperty dialogueData = step.FindPropertyRelative("dialogueData");
            if (dialogueData != null && dialogueData.objectReferenceValue == dialogue)
                return true;
        }

        return false;
    }

    void DrawTextTable()
    {
        Rect row = EditorGUILayout.GetControlRect(true, EditorGUI.GetPropertyHeight(_textTable, true));
        Rect tableRect = row;
        tableRect.width -= LocalizationTablesButton.Width + Spacing;

        Rect buttonRect = new Rect(tableRect.xMax + Spacing, row.y, LocalizationTablesButton.Width, EditorGUIUtility.singleLineHeight);

        EditorGUI.PropertyField(tableRect, _textTable, true);
        LocalizationTablesButton.Draw(buttonRect);
    }
}
