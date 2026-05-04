using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;

[CustomPropertyDrawer(typeof(DialogueLine))]
public class DialogueLineDrawer : PropertyDrawer
{
    private const float Spacing = 2f;
    private static readonly Dictionary<string, string> Filters = new Dictionary<string, string>();

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        Rect row = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(row, property.isExpanded, label, true);

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            DrawLine(row, property);
            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        int lineCount = property.isExpanded ? 5 : 1;
        return lineCount * EditorGUIUtility.singleLineHeight + (lineCount - 1) * Spacing;
    }

    private static void DrawLine(Rect row, SerializedProperty property)
    {
        DrawProperty(ref row, property.FindPropertyRelative("speaker"));
        DrawTextKey(ref row, property);
        DrawProperty(ref row, property.FindPropertyRelative("characterImage"));
    }

    private static void DrawProperty(ref Rect row, SerializedProperty property)
    {
        row.y += EditorGUIUtility.singleLineHeight + Spacing;
        EditorGUI.PropertyField(row, property);
    }

    private static void DrawTextKey(ref Rect row, SerializedProperty property)
    {
        row.y += EditorGUIUtility.singleLineHeight + Spacing;

        SerializedProperty textKey = property.FindPropertyRelative("textKey");
        string[] keys = GetKeys(property);
        string filterKey = GetFilterKey(property);
        Filters.TryGetValue(filterKey, out string filter);

        filter = EditorGUI.TextField(row, "Key Filter", filter);
        Filters[filterKey] = filter;

        row.y += EditorGUIUtility.singleLineHeight + Spacing;
        Rect textKeyRect = row;
        textKeyRect.width -= LocalizationTablesButton.Width + Spacing;

        Rect buttonRect = new Rect(textKeyRect.xMax + Spacing, row.y, LocalizationTablesButton.Width, EditorGUIUtility.singleLineHeight);

        if (keys.Length == 0)
        {
            EditorGUI.PropertyField(textKeyRect, textKey);
            LocalizationTablesButton.Draw(buttonRect);
            return;
        }

        string[] filteredKeys = string.IsNullOrWhiteSpace(filter)
            ? keys
            : keys.Where(key => key.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0).ToArray();

        List<string> values = new List<string> { string.Empty };
        List<string> labels = new List<string> { "None" };

        if (!string.IsNullOrEmpty(textKey.stringValue) && !filteredKeys.Contains(textKey.stringValue))
        {
            values.Add(textKey.stringValue);
            labels.Add(keys.Contains(textKey.stringValue)
                ? $"{textKey.stringValue} (selected)"
                : $"{textKey.stringValue} (missing)");
        }

        values.AddRange(filteredKeys);
        labels.AddRange(filteredKeys);

        int selectedIndex = Mathf.Max(0, values.IndexOf(textKey.stringValue));
        selectedIndex = EditorGUI.Popup(textKeyRect, textKey.displayName, selectedIndex, labels.ToArray());
        textKey.stringValue = values[selectedIndex];
        LocalizationTablesButton.Draw(buttonRect);
    }

    private static string GetFilterKey(SerializedProperty property)
    {
        return $"{property.serializedObject.targetObject.GetInstanceID()}:{property.propertyPath}";
    }

    private static string[] GetKeys(SerializedProperty property)
    {
        if (property.serializedObject.targetObject is not DialogueData dialogue || !dialogue.HasTextTable)
            return new string[0];

        return LocalizationEditorSettings.GetStringTableCollection(dialogue.TextTable)
            ?.SharedData
            ?.Entries
            .Select(entry => entry.Key)
            .Where(key => !string.IsNullOrEmpty(key))
            .ToArray() ?? new string[0];
    }
}
