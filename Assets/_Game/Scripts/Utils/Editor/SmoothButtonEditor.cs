using UnityEditor;
using UnityEditor.UI;
using UnityEngine;

[CustomEditor(typeof(SmoothButton), true)]
[CanEditMultipleObjects]
public class SmoothButtonEditor : ButtonEditor
{
    private SerializedProperty _pressScale;
    private SerializedProperty _animationDuration;

    protected override void OnEnable()
    {
        base.OnEnable();
        _pressScale = serializedObject.FindProperty("pressScale");
        _animationDuration = serializedObject.FindProperty("animationDuration");
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        serializedObject.Update();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Animation Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_pressScale);
        EditorGUILayout.PropertyField(_animationDuration);

        serializedObject.ApplyModifiedProperties();
    }
}
