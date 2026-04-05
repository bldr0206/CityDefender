using ColorChargeTD.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ColorChargeTD.Editor
{
    public static class ResultScreenPrefabBuilder
    {
        public const string ResultScreenPrefabPath = "Assets/_Game/Prefabs/UI/ResultScreen.prefab";
        private const string MetaScenePath = "Assets/_Game/Scenes/Meta.unity";

        [MenuItem("Tools/Color Charge TD/UI/Rebuild Result Screen Prefab")]
        public static void RebuildResultScreenPrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(ResultScreenPrefabPath);
            try
            {
                Transform content = root.transform.Find("Content");
                if (content != null)
                {
                    Object.DestroyImmediate(content.gameObject);
                }

                ResultScreenView view = root.GetComponent<ResultScreenView>();
                if (view == null)
                {
                    Debug.LogError("ResultScreen prefab root must have ResultScreenView.");
                    return;
                }

                ResultScreenViewLayout.BuiltRefs built = ResultScreenViewLayout.BuildUnder(root.transform);
                SerializedObject serializedView = new SerializedObject(view);
                serializedView.FindProperty("contentRoot").objectReferenceValue = built.contentRoot;
                serializedView.FindProperty("titleText").objectReferenceValue = built.titleText;
                serializedView.FindProperty("detailText").objectReferenceValue = built.detailText;
                serializedView.FindProperty("primaryButton").objectReferenceValue = built.primaryButton;
                serializedView.FindProperty("primaryButtonCaption").objectReferenceValue = built.primaryButtonCaption;
                serializedView.FindProperty("restartLevelRow").objectReferenceValue = built.restartLevelRow;
                serializedView.FindProperty("restartLevelButton").objectReferenceValue = built.restartLevelButton;
                serializedView.ApplyModifiedPropertiesWithoutUndo();

                EditorUtility.SetDirty(root);
                PrefabUtility.SaveAsPrefabAsset(root, ResultScreenPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Result Screen prefab rebuilt at " + ResultScreenPrefabPath);
        }

        [MenuItem("Tools/Color Charge TD/UI/Replace Result Screen In Meta Scene With Prefab")]
        public static void ReplaceResultScreenInMetaSceneWithPrefab()
        {
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ResultScreenPrefabPath);
            if (prefabAsset == null)
            {
                Debug.LogError("Missing prefab: " + ResultScreenPrefabPath);
                return;
            }

            Scene metaScene = EditorSceneManager.OpenScene(MetaScenePath, OpenSceneMode.Single);
            GameObject canvasGo = GameObject.Find("MetaCanvas");
            if (canvasGo == null)
            {
                Debug.LogError("Meta scene has no MetaCanvas.");
                return;
            }

            Transform oldResult = canvasGo.transform.Find("ResultScreen");
            if (oldResult == null)
            {
                Debug.LogError("MetaCanvas has no ResultScreen child.");
                return;
            }

            ResultScreenBridge oldBridge = oldResult.GetComponent<ResultScreenBridge>();
            if (oldBridge == null)
            {
                Debug.LogError("ResultScreen is missing ResultScreenBridge.");
                return;
            }

            SerializedObject oldBridgeSo = new SerializedObject(oldBridge);
            Object metaScreenRoot = oldBridgeSo.FindProperty("metaScreenRoot").objectReferenceValue;
            Object commandRouter = oldBridgeSo.FindProperty("commandRouter").objectReferenceValue;
            int siblingIndex = oldResult.GetSiblingIndex();

            Object.DestroyImmediate(oldResult.gameObject);

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset, canvasGo.transform);
            instance.name = "ResultScreen";
            instance.transform.SetSiblingIndex(siblingIndex);

            ResultScreenBridge newBridge = instance.GetComponent<ResultScreenBridge>();
            SerializedObject newBridgeSo = new SerializedObject(newBridge);
            newBridgeSo.FindProperty("targetView").objectReferenceValue = instance.GetComponent<ResultScreenView>();
            newBridgeSo.FindProperty("metaScreenRoot").objectReferenceValue = metaScreenRoot;
            newBridgeSo.FindProperty("commandRouter").objectReferenceValue = commandRouter;
            newBridgeSo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(metaScene);
            EditorSceneManager.SaveScene(metaScene);
            Debug.Log("Meta scene ResultScreen is now a prefab instance.");
        }
    }
}
