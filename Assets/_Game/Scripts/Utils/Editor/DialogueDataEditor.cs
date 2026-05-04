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

        DrawTextTable();
        EditorGUILayout.PropertyField(_lines, true);

        serializedObject.ApplyModifiedProperties();
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
