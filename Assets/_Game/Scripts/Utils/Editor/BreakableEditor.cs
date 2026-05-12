using UnityEditor;

[CustomEditor(typeof(Breakable))]
[CanEditMultipleObjects]
public class BreakableEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawPropertiesExcluding(serializedObject, "_questId");
        QuestIdEditorUtility.DrawQuestIdPopup(serializedObject.FindProperty("_questId"));

        serializedObject.ApplyModifiedProperties();
    }
}
