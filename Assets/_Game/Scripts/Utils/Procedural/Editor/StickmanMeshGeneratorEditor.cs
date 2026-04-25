using UnityEditor;
using UnityEngine;

namespace _Game.Scripts.Utils.Procedural.Editor
{
    [CustomEditor(typeof(StickmanMeshGenerator))]
    public sealed class StickmanMeshGeneratorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Generate Mesh"))
                {
                    foreach (Object t in targets)
                    {
                        if (t is StickmanMeshGenerator gen)
                        {
                            Undo.RecordObject(gen.GetComponent<MeshFilter>(), "Generate Stickman Mesh");
                            gen.GenerateMesh();
                            EditorUtility.SetDirty(gen);
                        }
                    }
                }
            }
        }
    }
}

