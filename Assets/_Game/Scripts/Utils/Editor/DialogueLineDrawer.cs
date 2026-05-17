using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

[CustomPropertyDrawer(typeof(DialogueLine))]
public class DialogueLineDrawer : PropertyDrawer
{
    private const float Spacing = 2f;
    private const float CharacterImagePreviewSize = 64f;
    private const string MainLocalizationCollectionName = "Localisation_main";
    static readonly Regex s_LineArrayIndex = new Regex(@"lines\.Array\.data\[(\d+)\]$", RegexOptions.Compiled);

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
        if (!property.isExpanded)
            return EditorGUIUtility.singleLineHeight;

        float h = EditorGUIUtility.singleLineHeight;
        h += EditorGUIUtility.singleLineHeight + Spacing;
        h += (EditorGUIUtility.singleLineHeight + Spacing) * 2f;
        h += EditorGUIUtility.singleLineHeight + Spacing;
        SerializedProperty img = property.FindPropertyRelative("characterImage");
        if (img != null && img.objectReferenceValue is Sprite sprite && sprite.texture != null)
            h += Spacing + GetPreviewRowHeight(property);

        return h;
    }

    static float GetPreviewRowHeight(SerializedProperty lineProperty)
    {
        float textColumnW = GetPreviewTextColumnWidth();
        string preview = GetPreviewLocalizedText(lineProperty);
        float textH = string.IsNullOrEmpty(preview)
            ? EditorGUIUtility.singleLineHeight
            : EditorStyles.wordWrappedMiniLabel.CalcHeight(new GUIContent(preview), textColumnW);
        return Mathf.Max(CharacterImagePreviewSize, textH);
    }

    static float GetPreviewTextColumnWidth()
    {
        float lineW = Mathf.Max(120f, EditorGUIUtility.currentViewWidth - 56f);
        return Mathf.Max(40f, lineW - CharacterImagePreviewSize - Spacing);
    }

    private static void DrawLine(Rect row, SerializedProperty property)
    {
        DrawProperty(ref row, property.FindPropertyRelative("speaker"));
        DrawTextKey(ref row, property);
        DrawProperty(ref row, property.FindPropertyRelative("characterImage"));
        SerializedProperty characterImage = property.FindPropertyRelative("characterImage");
        if (characterImage != null && characterImage.objectReferenceValue is Sprite sprite && sprite.texture != null)
        {
            row.y += EditorGUIUtility.singleLineHeight + Spacing;
            DrawPreviewRow(property, sprite, ref row);
        }
    }

    static void DrawPreviewRow(SerializedProperty lineProperty, Sprite sprite, ref Rect row)
    {
        var speaker = (DialogueSpeaker)lineProperty.FindPropertyRelative("speaker").enumValueIndex;
        string text = GetPreviewLocalizedText(lineProperty);
        GUIStyle wrap = EditorStyles.wordWrappedMiniLabel;

        Rect indentedForW = EditorGUI.IndentedRect(new Rect(row.x, row.y, row.width, 1f));
        float textW = Mathf.Max(40f, indentedForW.width - CharacterImagePreviewSize - Spacing);
        float textH = string.IsNullOrEmpty(text)
            ? EditorGUIUtility.singleLineHeight
            : wrap.CalcHeight(new GUIContent(text), textW);
        float rowH = Mathf.Max(CharacterImagePreviewSize, textH);

        Rect indented = EditorGUI.IndentedRect(new Rect(row.x, row.y, row.width, rowH));

        Rect imageRect;
        Rect textRect;
        if (speaker == DialogueSpeaker.FirstCharacter)
        {
            imageRect = new Rect(indented.xMin, indented.yMin, CharacterImagePreviewSize, CharacterImagePreviewSize);
            textRect = new Rect(imageRect.xMax + Spacing, indented.yMin, textW, rowH);
        }
        else
        {
            imageRect = new Rect(indented.xMax - CharacterImagePreviewSize, indented.yMin, CharacterImagePreviewSize, CharacterImagePreviewSize);
            textRect = new Rect(indented.xMin, indented.yMin, textW, rowH);
        }

        if (rowH > CharacterImagePreviewSize)
            imageRect.y = indented.yMin + (rowH - CharacterImagePreviewSize) * 0.5f;

        EditorGUI.DrawRect(imageRect, new Color(0.12f, 0.12f, 0.12f, 1f));
        DrawSpritePreview(imageRect, sprite);
        GUI.Label(textRect, text, wrap);
    }

    static string GetPreviewLocalizedText(SerializedProperty lineProperty)
    {
        SerializedProperty keyProp = lineProperty.FindPropertyRelative("textKey");
        if (keyProp == null)
            return string.Empty;

        string key = keyProp.stringValue;
        if (string.IsNullOrWhiteSpace(key))
            return string.Empty;

        if (lineProperty.serializedObject.targetObject is not DialogueData dialogue || !dialogue.HasTextTable)
            return $"[{key}]";

        StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection(dialogue.TextTable);
        if (collection != null)
        {
            StringTable table = GetStringTableForInspectorPreview(collection);
            if (table != null)
            {
                StringTableEntry entry = table.GetEntry(key);
                if (entry != null && !string.IsNullOrEmpty(entry.Value))
                    return entry.Value;
            }
        }

        try
        {
            var loc = new LocalizedString(dialogue.TextTable, key);
            string s = loc.GetLocalizedString();
            return string.IsNullOrEmpty(s) ? $"[{key}]" : s;
        }
        catch
        {
            return $"[{key}]";
        }
    }

    static StringTable GetStringTableForInspectorPreview(StringTableCollection collection)
    {
        if (LocalizationSettings.HasSettings && LocalizationSettings.SelectedLocale != null)
        {
            var selected = collection.GetTable(LocalizationSettings.SelectedLocale.Identifier) as StringTable;
            if (selected != null)
                return selected;
        }

        foreach (StringTable st in collection.StringTables)
        {
            if (st == null) continue;
            string code = st.LocaleIdentifier.Code;
            if (code.StartsWith("en", StringComparison.OrdinalIgnoreCase))
                return st;
        }

        return collection.StringTables.Count > 0 ? collection.StringTables[0] : null;
    }

    static void DrawSpritePreview(Rect rect, Sprite sprite)
    {
        Texture2D tex = sprite.texture;
        Rect tr = sprite.textureRect;
        Rect uv = new Rect(tr.x / tex.width, tr.y / tex.height, tr.width / tex.width, tr.height / tex.height);
        GUI.DrawTextureWithTexCoords(rect, tex, uv, true);
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
        float keyRowButtonsWidth = LocalizationTablesButton.Width * 2 + Spacing * 2;
        textKeyRect.width -= keyRowButtonsWidth;

        Rect createRect = new Rect(textKeyRect.xMax + Spacing, row.y, LocalizationTablesButton.Width, EditorGUIUtility.singleLineHeight);
        Rect tableRect = new Rect(createRect.xMax + Spacing, row.y, LocalizationTablesButton.Width, EditorGUIUtility.singleLineHeight);

        if (keys.Length == 0)
        {
            EditorGUI.PropertyField(textKeyRect, textKey);
            if (GUI.Button(createRect, "Create"))
                CreateLocalizationEntryForLine(property, textKey);
            LocalizationTablesButton.Draw(tableRect);
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
        if (GUI.Button(createRect, "Create"))
            CreateLocalizationEntryForLine(property, textKey);
        LocalizationTablesButton.Draw(tableRect);
    }

    static void CreateLocalizationEntryForLine(SerializedProperty lineProperty, SerializedProperty textKeyProp)
    {
        if (lineProperty.serializedObject.targetObject is not DialogueData dialogue)
            return;

        var collection = LocalizationEditorSettings.GetStringTableCollection(MainLocalizationCollectionName);
        if (collection == null)
        {
            EditorUtility.DisplayDialog("Локализация", $"Не найдена String Table Collection «{MainLocalizationCollectionName}».", "OK");
            return;
        }

        int lineIndex1 = GetDialogueLineIndex1Based(lineProperty.propertyPath);
        if (lineIndex1 < 1)
        {
            Debug.LogWarning($"DialogueLine: не удалось определить индекс строки для пути «{lineProperty.propertyPath}».");
            return;
        }

        string assetName = string.IsNullOrWhiteSpace(dialogue.name) ? "dialogue" : SanitizeLocalizationKeyPart(dialogue.name);
        string baseKey = $"{assetName}_{lineIndex1}";
        string finalKey = baseKey;
        for (int n = 2; collection.SharedData.Contains(finalKey); n++)
            finalKey = $"{baseKey}_{n}";

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Create localization key");

        Undo.RecordObject(collection, "Create localization key");
        Undo.RecordObject(collection.SharedData, "Create localization key");
        foreach (StringTable table in collection.StringTables)
            Undo.RecordObject(table, "Create localization key");
        Undo.RecordObject(dialogue, "Create localization key");

        foreach (StringTable table in collection.StringTables)
        {
            table.AddEntry(finalKey, string.Empty);
            EditorUtility.SetDirty(table);
        }

        EditorUtility.SetDirty(collection.SharedData);
        EditorUtility.SetDirty(collection);

        SerializedProperty textTable = lineProperty.serializedObject.FindProperty("textTable");
        if (textTable != null)
        {
            SerializedProperty nameProp = textTable.FindPropertyRelative("m_TableCollectionName");
            if (nameProp != null && nameProp.stringValue != MainLocalizationCollectionName)
                nameProp.stringValue = MainLocalizationCollectionName;
        }

        textKeyProp.stringValue = finalKey;
        lineProperty.serializedObject.ApplyModifiedProperties();

        LocalizationEditorSettings.EditorEvents.RaiseCollectionModified(collection, collection);
        AssetDatabase.SaveAssets();
    }

    static int GetDialogueLineIndex1Based(string propertyPath)
    {
        Match m = s_LineArrayIndex.Match(propertyPath);
        if (!m.Success)
            return -1;
        if (int.TryParse(m.Groups[1].Value, out int i))
            return i + 1;
        return -1;
    }

    static string SanitizeLocalizationKeyPart(string name)
    {
        var sb = new StringBuilder(name.Length);
        foreach (char c in name)
        {
            if (char.IsLetterOrDigit(c) || c == '_' || c == '.' || c == '-')
                sb.Append(c);
            else
                sb.Append('_');
        }
        string s = sb.ToString().Trim(' ', '_', '.');
        return string.IsNullOrEmpty(s) ? "dialogue" : s;
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
