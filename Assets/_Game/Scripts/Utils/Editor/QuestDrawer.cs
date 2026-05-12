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

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        Rect row = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        SerializedProperty type = property.FindPropertyRelative("type");

        EditorGUI.PropertyField(row, type);

        row.y += EditorGUIUtility.singleLineHeight + Spacing;
        EditorGUI.PropertyField(row, GetStepProperty(property, type));

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight * 2f + Spacing;
    }

    static SerializedProperty GetStepProperty(SerializedProperty property, SerializedProperty type)
    {
        return (QuestSequenceStepType)type.enumValueIndex == QuestSequenceStepType.Cutscene
            ? property.FindPropertyRelative("cutscenePrefab")
            : property.FindPropertyRelative("dialogueData");
    }
}
