using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;

namespace MultitoolTracks
{
    [CustomEditor(typeof(TweenTransformClip))]
    public class TweenTransformClipInspector : Editor
    {
        SerializedProperty m_TweenPosition;
        SerializedProperty m_TweenScale;
        SerializedProperty m_TweenRotation;
        SerializedProperty m_StartTarget;
        SerializedProperty m_EndTarget;
        SerializedProperty m_StartPositionOffset;
        SerializedProperty m_EndPositionOffset;
        SerializedProperty m_StartRotationOffsetEuler;
        SerializedProperty m_EndRotationOffsetEuler;
        SerializedProperty m_StartPosition;
        SerializedProperty m_EndPosition;
        SerializedProperty m_StartScale;
        SerializedProperty m_EndScale;
        SerializedProperty m_StartScaleUniform;
        SerializedProperty m_EndScaleUniform;
        SerializedProperty m_StartRotationEuler;
        SerializedProperty m_EndRotationEuler;
        SerializedProperty m_PositionSourceMode;
        SerializedProperty m_RotationSourceMode;
        SerializedProperty m_PositionAxisMode;
        SerializedProperty m_ScaleAxisMode;
        SerializedProperty m_RotationAxisMode;
        SerializedProperty m_PositionEasing;
        SerializedProperty m_ScaleEasing;
        SerializedProperty m_RotationEasing;
        SerializedProperty m_Space;
        SerializedProperty m_RotationMode;
        SerializedProperty m_ScaleMode;
        SerializedProperty m_ScaleValueMode;

        const float k_SectionSpacing = 4f;
        const float k_ToggleWidth = 18f;
        const float k_ToggleFoldoutGap = 12f;

        static void DrawVector3Colored(SerializedProperty prop, GUIContent label)
        {
            var rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
            var v = prop.vector3Value;
            var values = new[] { v.x, v.y, v.z };
            var subLabels = new[] { new GUIContent("X"), new GUIContent("Y"), new GUIContent("Z") };

            EditorGUI.BeginChangeCheck();
            EditorGUI.MultiFloatField(rect, label, subLabels, values);
            if (EditorGUI.EndChangeCheck())
                prop.vector3Value = new Vector3(values[0], values[1], values[2]);
        }

        void OnEnable()
        {
            m_TweenPosition = serializedObject.FindProperty("tweenPosition");
            m_TweenScale = serializedObject.FindProperty("tweenScale");
            m_TweenRotation = serializedObject.FindProperty("tweenRotation");
            m_StartTarget = serializedObject.FindProperty("startTarget");
            m_EndTarget = serializedObject.FindProperty("endTarget");
            m_StartPositionOffset = serializedObject.FindProperty("startPositionOffset");
            m_EndPositionOffset = serializedObject.FindProperty("endPositionOffset");
            m_StartRotationOffsetEuler = serializedObject.FindProperty("startRotationOffsetEuler");
            m_EndRotationOffsetEuler = serializedObject.FindProperty("endRotationOffsetEuler");
            m_StartPosition = serializedObject.FindProperty("startPosition");
            m_EndPosition = serializedObject.FindProperty("endPosition");
            m_StartScale = serializedObject.FindProperty("startScale");
            m_EndScale = serializedObject.FindProperty("endScale");
            m_StartScaleUniform = serializedObject.FindProperty("startScaleUniform");
            m_EndScaleUniform = serializedObject.FindProperty("endScaleUniform");
            m_StartRotationEuler = serializedObject.FindProperty("startRotationEuler");
            m_EndRotationEuler = serializedObject.FindProperty("endRotationEuler");
            m_PositionSourceMode = serializedObject.FindProperty("positionSourceMode");
            m_RotationSourceMode = serializedObject.FindProperty("rotationSourceMode");
            m_PositionAxisMode = serializedObject.FindProperty("positionAxisMode");
            m_ScaleAxisMode = serializedObject.FindProperty("scaleAxisMode");
            m_RotationAxisMode = serializedObject.FindProperty("rotationAxisMode");
            m_PositionEasing = serializedObject.FindProperty("positionEasing");
            m_ScaleEasing = serializedObject.FindProperty("scaleEasing");
            m_RotationEasing = serializedObject.FindProperty("rotationEasing");
            m_Space = serializedObject.FindProperty("space");
            m_RotationMode = serializedObject.FindProperty("rotationMode");
            m_ScaleMode = serializedObject.FindProperty("scaleMode");
            m_ScaleValueMode = serializedObject.FindProperty("scaleValueMode");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var posTarget = (SourceMode)m_PositionSourceMode.enumValueIndex == SourceMode.Target;
            var rotTarget = (SourceMode)m_RotationSourceMode.enumValueIndex == SourceMode.Target;

            DrawFoldoutSection("Position", m_TweenPosition, () => DrawPositionContent(posTarget, rotTarget));
            DrawFoldoutSection("Scale", m_TweenScale, DrawScaleContent);
            DrawFoldoutSection("Rotation", m_TweenRotation, () => DrawRotationContent(rotTarget));

            if (serializedObject.ApplyModifiedProperties())
                TimelineEditor.Refresh(UnityEditor.Timeline.RefreshReason.ContentsModified);
        }

        void DrawFoldoutSection(string title, SerializedProperty toggle, System.Action drawContent)
        {
            var key = "TweenTransformClip." + title + "." + target.GetInstanceID();
            var expanded = SessionState.GetBool(key, true);

            var rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
            var toggleRect = new Rect(rect.x, rect.y, k_ToggleWidth, rect.height);
            var foldoutRect = new Rect(rect.x + k_ToggleWidth + k_ToggleFoldoutGap, rect.y,
                rect.width - k_ToggleWidth - k_ToggleFoldoutGap, rect.height);

            EditorGUI.PropertyField(toggleRect, toggle, GUIContent.none);
            expanded = EditorGUI.Foldout(foldoutRect, expanded, title, true, EditorStyles.foldoutHeader);
            SessionState.SetBool(key, expanded);

            if (expanded)
            {
                EditorGUI.BeginDisabledGroup(!toggle.boolValue);
                EditorGUI.indentLevel++;
                drawContent();
                EditorGUI.indentLevel--;
                EditorGUI.EndDisabledGroup();
            }
            EditorGUILayout.Space(k_SectionSpacing);
        }

        void DrawPositionContent(bool useTarget, bool rotUsesTarget)
        {
            EditorGUILayout.PropertyField(m_PositionEasing, new GUIContent("Easing", "How the tween accelerates"));
            EditorGUILayout.PropertyField(m_Space, new GUIContent("Space", "World = scene coords, Local = relative to parent"));
            EditorGUILayout.PropertyField(m_PositionSourceMode, new GUIContent("Source", "Target = copy from transforms, Value = use numbers"));

            if (useTarget || rotUsesTarget)
            {
                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField("Reference Transforms", EditorStyles.miniLabel);
                EditorGUILayout.PropertyField(m_StartTarget, new GUIContent("Start", "Transform to copy start position/rotation from"));
                if (useTarget)
                    DrawVector3Colored(m_StartPositionOffset, new GUIContent("Offset", "Position offset added to start reference transform"));
                if (rotUsesTarget)
                    DrawVector3Colored(m_StartRotationOffsetEuler, new GUIContent("Rot Offset", "Euler offset (degrees) applied to start reference rotation"));

                EditorGUILayout.Space(6);

                EditorGUILayout.PropertyField(m_EndTarget, new GUIContent("End", "Transform to copy end position/rotation from"));
                if (useTarget)
                    DrawVector3Colored(m_EndPositionOffset, new GUIContent("Offset", "Position offset added to end reference transform"));
                if (rotUsesTarget)
                    DrawVector3Colored(m_EndRotationOffsetEuler, new GUIContent("Rot Offset", "Euler offset (degrees) applied to end reference rotation"));
            }

            if (!useTarget)
            {
                DrawVector3Colored(m_StartPosition, new GUIContent("Start", "Start position (X, Y, Z)"));
                DrawVector3Colored(m_EndPosition, new GUIContent("End", "End position (X, Y, Z)"));
            }
            EditorGUILayout.PropertyField(m_PositionAxisMode, new GUIContent("Axes", "Which axes to tween"));
        }

        void DrawScaleContent()
        {
            EditorGUILayout.PropertyField(m_ScaleEasing, new GUIContent("Easing", "How the tween accelerates"));
            EditorGUILayout.PropertyField(m_ScaleMode, new GUIContent("Mode", "Local = absolute values, Relative = multipliers (1 = no change, 2 = double)"));
            EditorGUILayout.PropertyField(m_ScaleValueMode, new GUIContent("Value", "Uniform = single float for all axes, PerAxis = separate X/Y/Z"));

            var isUniform = (ScaleValueMode)m_ScaleValueMode.enumValueIndex == ScaleValueMode.Uniform;
            if (isUniform)
            {
                EditorGUILayout.PropertyField(m_StartScaleUniform, new GUIContent("Start", "Start scale (all axes)"));
                EditorGUILayout.PropertyField(m_EndScaleUniform, new GUIContent("End", "End scale (all axes)"));
            }
            else
            {
                DrawVector3Colored(m_StartScale, new GUIContent("Start", "Start scale or multiplier per axis"));
                DrawVector3Colored(m_EndScale, new GUIContent("End", "End scale or multiplier per axis"));
                EditorGUILayout.PropertyField(m_ScaleAxisMode, new GUIContent("Axes", "Which axes to tween"));
            }
        }

        void DrawRotationContent(bool useTarget)
        {
            EditorGUILayout.PropertyField(m_RotationEasing, new GUIContent("Easing", "How the tween accelerates"));
            EditorGUILayout.PropertyField(m_RotationMode, new GUIContent("Mode", "Local = absolute euler, Relative = delta applied to initial"));
            EditorGUILayout.PropertyField(m_RotationSourceMode, new GUIContent("Source", "Target = copy from transforms, Value = use euler angles"));
            if (!useTarget)
            {
                DrawVector3Colored(m_StartRotationEuler, new GUIContent("Start", "Start rotation in degrees (X, Y, Z)"));
                DrawVector3Colored(m_EndRotationEuler, new GUIContent("End", "End rotation in degrees (X, Y, Z)"));
            }
            EditorGUILayout.PropertyField(m_RotationAxisMode, new GUIContent("Axes", "Which axes to tween"));
        }
    }
}
