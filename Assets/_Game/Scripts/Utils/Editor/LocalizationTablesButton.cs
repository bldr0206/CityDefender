using UnityEditor;
using UnityEngine;

public static class LocalizationTablesButton
{
    public const float Width = 56f;
    const string MenuPath = "Window/Asset Management/Localization Tables";

    public static void Draw(Rect rect)
    {
        if (GUI.Button(rect, "Tables"))
            EditorApplication.ExecuteMenuItem(MenuPath);
    }
}
