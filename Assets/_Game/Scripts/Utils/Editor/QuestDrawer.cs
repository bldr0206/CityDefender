using System;
using System.IO;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(Quest))]
public class QuestDrawer : PropertyDrawer
{
    const float Spacing = 2f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        float totalHeight = GetPropertyHeight(property, label);
        Rect backgroundRect = new Rect(position.x, position.y, position.width, totalHeight);
        EditorGUI.DrawRect(backgroundRect, BackgroundTintForQuestType(GetQuestType(property)));

        EditorGUI.BeginProperty(position, label, property);

        Rect row = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(row, property.isExpanded, label, true);

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            DrawQuest(ref row, property);
            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded)
            return EditorGUIUtility.singleLineHeight;

        float height = EditorGUIUtility.singleLineHeight;
        height += GetPropertyHeight(property, "id");
        height += GetPropertyHeight(property, "title");
        height += GetPropertyHeight(property, "type");
        if (GetQuestType(property) == QuestType.ReachPoint || GetQuestType(property) == QuestType.OwnAgents)
            height += GetPropertyHeight(property, "targetPoint");

        if (GetQuestType(property) == QuestType.DeliverItem)
        {
            height += GetPropertyHeight(property, "collectTurnInPoint");
            height += GetPropertyHeight(property, "collectAlwaysShowTurnInPointer");
        }

        height += GetPropertyHeight(property, "requiredAmount");
        height += GetPropertyHeight(property, "startSequence");
        height += GetPropertyHeight(property, "endSequence");

        return height;
    }

    static void DrawQuest(ref Rect row, SerializedProperty property)
    {
        DrawProperty(ref row, property.FindPropertyRelative("id"));
        DrawTitle(ref row, property.FindPropertyRelative("title"));
        DrawProperty(ref row, property.FindPropertyRelative("type"));
        if (GetQuestType(property) == QuestType.ReachPoint || GetQuestType(property) == QuestType.OwnAgents)
            DrawProperty(ref row, property.FindPropertyRelative("targetPoint"));

        if (GetQuestType(property) == QuestType.DeliverItem)
        {
            DrawProperty(ref row, property.FindPropertyRelative("collectTurnInPoint"));
            DrawProperty(ref row, property.FindPropertyRelative("collectAlwaysShowTurnInPointer"));
        }

        DrawProperty(ref row, property.FindPropertyRelative("requiredAmount"));
        DrawProperty(ref row, property.FindPropertyRelative("startSequence"));
        DrawProperty(ref row, property.FindPropertyRelative("endSequence"));
    }

    static QuestType GetQuestType(SerializedProperty property)
    {
        return (QuestType)property.FindPropertyRelative("type").enumValueIndex;
    }

    /// <summary>Пастельная подложка под элемент квеста в списке (Dark/Light).</summary>
    static Color BackgroundTintForQuestType(QuestType type)
    {
        float a = EditorGUIUtility.isProSkin ? 0.14f : 0.2f;
        switch (type)
        {
            case QuestType.ReachPoint:
                return new Color(0.4f, 0.62f, 0.95f, a);
            case QuestType.DeliverItem:
                return new Color(0.42f, 0.78f, 0.52f, a);
            case QuestType.OwnAgents:
                return new Color(0.92f, 0.68f, 0.35f, a);
            case QuestType.BreakBreakables:
                return new Color(0.88f, 0.48f, 0.5f, a);
            default:
                return new Color(0.55f, 0.55f, 0.58f, a * 0.65f);
        }
    }

    static void DrawProperty(ref Rect row, SerializedProperty property)
    {
        MoveToNextRow(ref row);
        row.height = EditorGUI.GetPropertyHeight(property, true);
        EditorGUI.PropertyField(row, property, true);
    }

    static void DrawTitle(ref Rect row, SerializedProperty title)
    {
        MoveToNextRow(ref row);
        row.height = EditorGUI.GetPropertyHeight(title, true);

        Rect titleRect = row;
        titleRect.width -= LocalizationTablesButton.Width + Spacing;

        Rect buttonRect = new Rect(titleRect.xMax + Spacing, row.y, LocalizationTablesButton.Width, EditorGUIUtility.singleLineHeight);

        EditorGUI.PropertyField(titleRect, title, true);
        LocalizationTablesButton.Draw(buttonRect);
    }

    static float GetPropertyHeight(SerializedProperty property, string name)
    {
        return EditorGUI.GetPropertyHeight(property.FindPropertyRelative(name), true) + Spacing;
    }

    static void MoveToNextRow(ref Rect row)
    {
        row.y += row.height + Spacing;
    }
}

[CustomPropertyDrawer(typeof(QuestSequenceStep))]
public class QuestSequenceStepDrawer : PropertyDrawer
{
    const float Spacing = 2f;
    const float DialogueActionButtonWidth = 56f;
    const string DialoguesFolder = "Assets/_Game/Content/Dialogues";
    const string DefaultDialogueTextTableCollection = "Localisation_main";

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        Rect row = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        SerializedProperty type = property.FindPropertyRelative("type");

        EditorGUI.PropertyField(row, type);

        row.y += EditorGUIUtility.singleLineHeight + Spacing;
        SerializedProperty stepProp = GetStepProperty(property, type);
        if ((QuestSequenceStepType)type.enumValueIndex == QuestSequenceStepType.Dialogue)
            DrawDialogueDataRow(row, property, stepProp);
        else
            EditorGUI.PropertyField(row, stepProp, true);

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight * 2f + Spacing;
    }

    static void DrawDialogueDataRow(Rect row, SerializedProperty stepProperty, SerializedProperty dialogueProp)
    {
        bool assigned = dialogueProp.objectReferenceValue != null;
        bool canCreate = false;
        string baseName = null;
        bool isStart = false;
        if (!assigned)
            canCreate = TryGetDialogueAssetBaseName(stepProperty, out baseName, out isStart);

        Rect main = row;
        if (assigned || canCreate)
            main.width -= DialogueActionButtonWidth + Spacing;

        EditorGUI.PropertyField(main, dialogueProp, true);

        if (!assigned && !canCreate)
            return;

        Rect btn = new Rect(main.xMax + Spacing, row.y, DialogueActionButtonWidth, EditorGUIUtility.singleLineHeight);
        if (assigned)
        {
            if (GUI.Button(btn, "Edit"))
            {
                var d = (DialogueData)dialogueProp.objectReferenceValue;
                Selection.activeObject = d;
                EditorGUIUtility.PingObject(d);
            }
        }
        else if (GUI.Button(btn, "Create"))
            CreateAndAssignDialogue(stepProperty, dialogueProp, baseName, isStart);
    }

    static void CreateAndAssignDialogue(SerializedProperty stepProperty, SerializedProperty dialogueProp, string baseName, bool isStart)
    {
        EnsureFolderExists(DialoguesFolder);
        string suffix = isStart ? "_start" : "_end";
        string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{DialoguesFolder}/{baseName}{suffix}.asset");
        var instance = ScriptableObject.CreateInstance<DialogueData>();
        var dialogueSo = new SerializedObject(instance);
        SerializedProperty textTable = dialogueSo.FindProperty("textTable");
        if (textTable != null)
        {
            SerializedProperty collectionName = textTable.FindPropertyRelative("m_TableCollectionName");
            if (collectionName != null)
                collectionName.stringValue = DefaultDialogueTextTableCollection;
        }

        dialogueSo.ApplyModifiedPropertiesWithoutUndo();

        foreach (UnityEngine.Object t in stepProperty.serializedObject.targetObjects)
            Undo.RecordObject(t, "Create Dialogue Data");

        AssetDatabase.CreateAsset(instance, assetPath);
        Undo.RegisterCreatedObjectUndo(instance, "Create Dialogue Data");
        dialogueProp.objectReferenceValue = instance;
        stepProperty.serializedObject.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
        Selection.activeObject = instance;
        EditorGUIUtility.PingObject(instance);
    }

    static bool TryGetDialogueAssetBaseName(SerializedProperty stepProperty, out string baseName, out bool isStart)
    {
        baseName = null;
        isStart = false;
        string questPath = TrimQuestPathFromSequenceStep(stepProperty.propertyPath, out isStart);
        if (questPath == null)
            return false;

        SerializedProperty questRoot = stepProperty.serializedObject.FindProperty(questPath);
        if (questRoot == null)
            return false;

        SerializedProperty idProp = questRoot.FindPropertyRelative("id");
        if (idProp == null)
            return false;

        string id = idProp.stringValue;
        baseName = string.IsNullOrWhiteSpace(id) ? "dialogue" : SanitizeFileName(id);
        return true;
    }

    static string TrimQuestPathFromSequenceStep(string path, out bool isStartSequence)
    {
        const string startTail = ".startSequence.Array.data[";
        const string endTail = ".endSequence.Array.data[";
        isStartSequence = false;

        int i = path.LastIndexOf(startTail, StringComparison.Ordinal);
        if (i >= 0 && TryEndsWithArrayIndexCloser(path, i + startTail.Length))
        {
            isStartSequence = true;
            return path.Substring(0, i);
        }

        i = path.LastIndexOf(endTail, StringComparison.Ordinal);
        if (i >= 0 && TryEndsWithArrayIndexCloser(path, i + endTail.Length))
        {
            isStartSequence = false;
            return path.Substring(0, i);
        }

        return null;
    }

    static bool TryEndsWithArrayIndexCloser(string path, int indexAfterBracket)
    {
        int close = path.IndexOf(']', indexAfterBracket);
        return close >= 0 && close == path.Length - 1;
    }

    static string SanitizeFileName(string s)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            s = s.Replace(c, '_');
        s = s.Trim();
        return string.IsNullOrEmpty(s) ? "dialogue" : s;
    }

    static void EnsureFolderExists(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath)) return;
        string parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/') ?? "Assets";
        string name = Path.GetFileName(folderPath);
        EnsureFolderExists(parent);
        AssetDatabase.CreateFolder(parent, name);
    }

    static SerializedProperty GetStepProperty(SerializedProperty property, SerializedProperty type)
    {
        switch ((QuestSequenceStepType)type.enumValueIndex)
        {
            case QuestSequenceStepType.Cutscene:
                return property.FindPropertyRelative("cutscenePrefab");
            case QuestSequenceStepType.Dialogue:
                return property.FindPropertyRelative("dialogueData");
            case QuestSequenceStepType.Pause:
                return property.FindPropertyRelative("pauseDuration");
            default:
                return property.FindPropertyRelative("dialogueData");
        }
    }
}
