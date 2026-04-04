using System.Collections.Generic;
using ColorChargeTD.Battle;
using ColorChargeTD.Core;
using ColorChargeTD.Data;
using ColorChargeTD.Installers;
using ColorChargeTD.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zenject;

namespace ColorChargeTD.Editor
{
    public static class SceneBootstrapGenerator
    {
        private const string RootFolder = "Assets/_Game";
        private const string ScenesFolder = RootFolder + "/Scenes";
        private const string ContentFolder = RootFolder + "/Content/Bootstrap";
        private const string ResourcesFolder = RootFolder + "/Resources";
        private const string ProjectContextPrefabPath = ResourcesFolder + "/ProjectContext.prefab";
        private const string GameBalanceAssetPath = ContentFolder + "/GameBalanceConfig.asset";
        private const string LevelCatalogAssetPath = ContentFolder + "/LevelCatalog.asset";
        private const string GameContentAssetPath = ResourcesFolder + "/GameContentConfig.asset";
        private const string BootScenePath = ScenesFolder + "/Boot.unity";
        private const string MainMenuScenePath = ScenesFolder + "/MainMenu.unity";
        private const string MetaScenePath = ScenesFolder + "/Meta.unity";
        private const string BattleScenePath = ScenesFolder + "/Battle.unity";

        [MenuItem("Tools/Color Charge TD/Generate Bootstrap Scenes")]
        public static void GenerateBootstrapScenes()
        {
            EnsureFolder(RootFolder);
            EnsureFolder(ScenesFolder);
            EnsureFolder(ContentFolder);
            EnsureFolder(ResourcesFolder);

            GameContentConfig gameContentConfig = EnsureBootstrapContent();
            EnsureProjectContextPrefab(gameContentConfig);

            EnsureBootScene();
            EnsureMainMenuScene();
            EnsureMetaScene();
            EnsureBattleScene();
            UpdateBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Color Charge TD bootstrap scenes were generated.");
        }

        #region Assets
        private static GameContentConfig EnsureBootstrapContent()
        {
            GameBalanceConfig balanceConfig = LoadOrCreateAsset<GameBalanceConfig>(GameBalanceAssetPath);
            LevelCatalogDefinition levelCatalog = LoadOrCreateAsset<LevelCatalogDefinition>(LevelCatalogAssetPath);
            GameContentConfig gameContentConfig = LoadOrCreateAsset<GameContentConfig>(GameContentAssetPath);

            SerializedObject serializedContent = new SerializedObject(gameContentConfig);
            serializedContent.FindProperty("balanceConfig").objectReferenceValue = balanceConfig;
            serializedContent.FindProperty("levelCatalog").objectReferenceValue = levelCatalog;
            serializedContent.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(gameContentConfig);

            return gameContentConfig;
        }

        private static void EnsureProjectContextPrefab(GameContentConfig gameContentConfig)
        {
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectContextPrefabPath);
            if (existingPrefab != null)
            {
                GameObject prefabRoot = PrefabUtility.LoadPrefabContents(ProjectContextPrefabPath);
                ConfigureProjectContextPrefab(prefabRoot, gameContentConfig);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, ProjectContextPrefabPath);
                PrefabUtility.UnloadPrefabContents(prefabRoot);
                return;
            }

            GameObject projectContextRoot = new GameObject("ProjectContext");
            ConfigureProjectContextPrefab(projectContextRoot, gameContentConfig);
            PrefabUtility.SaveAsPrefabAsset(projectContextRoot, ProjectContextPrefabPath);
            Object.DestroyImmediate(projectContextRoot);
        }

        private static void ConfigureProjectContextPrefab(GameObject root, GameContentConfig gameContentConfig)
        {
            ProjectContext projectContext = GetOrAddComponent<ProjectContext>(root);
            ProjectInstaller projectInstaller = GetOrAddComponent<ProjectInstaller>(root);
            projectContext.Installers = new[] { projectInstaller };

            SerializedObject serializedInstaller = new SerializedObject(projectInstaller);
            serializedInstaller.FindProperty("gameContentConfig").objectReferenceValue = gameContentConfig;
            serializedInstaller.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(root);
        }
        #endregion

        #region SceneGeneration
        private static void EnsureBootScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(BootScenePath) != null)
            {
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = GameSceneIds.Boot;

            SceneContext sceneContext = CreateSceneContext<BootSceneInstaller>("SceneContext");
            sceneContext.ParentNewObjectsUnderSceneContext = true;

            GameObject appRoot = CreateRoot("App");
            appRoot.AddComponent<BootSceneEntryPoint>();
            CreateCameraRig();
            CreateDirectionalLight();

            EditorSceneManager.SaveScene(scene, BootScenePath);
        }

        private static void EnsureMainMenuScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuScenePath) != null)
            {
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = GameSceneIds.MainMenu;

            SceneContext sceneContext = CreateSceneContext<MainMenuSceneInstaller>("SceneContext");
            sceneContext.ParentNewObjectsUnderSceneContext = true;

            GameObject systemsRoot = CreateRoot("Systems");
            systemsRoot.AddComponent<SceneNavigationBridge>();
            systemsRoot.AddComponent<NavigationCommandRouter>();
            systemsRoot.AddComponent<LevelSelectionBridge>();

            Canvas canvas = CreateCanvas("MainMenuCanvas");
            GameObject mainMenuScreen = CreateChild(canvas.gameObject, "MainMenuScreen");
            mainMenuScreen.AddComponent<MainMenuScreenView>();

            GameObject levelSelectScreen = CreateChild(canvas.gameObject, "LevelSelectScreen");
            levelSelectScreen.AddComponent<LevelSelectScreenView>();

            CreateEventSystem();
            CreateCameraRig();
            CreateDirectionalLight();

            EditorSceneManager.SaveScene(scene, MainMenuScenePath);
        }

        private static void EnsureMetaScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MetaScenePath) != null)
            {
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = GameSceneIds.Meta;

            SceneContext sceneContext = CreateSceneContext<MetaSceneInstaller>("SceneContext");
            sceneContext.ParentNewObjectsUnderSceneContext = true;

            GameObject systemsRoot = CreateRoot("Systems");
            systemsRoot.AddComponent<SceneNavigationBridge>();
            systemsRoot.AddComponent<NavigationCommandRouter>();

            Canvas canvas = CreateCanvas("MetaCanvas");
            GameObject metaScreen = CreateChild(canvas.gameObject, "MetaScreen");
            metaScreen.AddComponent<MetaScreenView>();

            GameObject resultScreen = CreateChild(canvas.gameObject, "ResultScreen");
            ResultScreenView resultScreenView = resultScreen.AddComponent<ResultScreenView>();
            ResultScreenBridge resultScreenBridge = resultScreen.AddComponent<ResultScreenBridge>();
            SetObjectReference(resultScreenBridge, "targetView", resultScreenView);

            CreateEventSystem();
            CreateCameraRig();
            CreateDirectionalLight();

            EditorSceneManager.SaveScene(scene, MetaScenePath);
        }

        private static void EnsureBattleScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(BattleScenePath) != null)
            {
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = GameSceneIds.Battle;

            SceneContext sceneContext = CreateSceneContext<BattleSceneInstaller>("SceneContext");
            sceneContext.ParentNewObjectsUnderSceneContext = true;

            GameObject systemsRoot = CreateRoot("Systems");
            systemsRoot.AddComponent<SceneNavigationBridge>();

            Canvas canvas = CreateCanvas("BattleCanvas");
            GameObject hudRoot = CreateChild(canvas.gameObject, "BattleHud");
            BattleHudView hudView = hudRoot.AddComponent<BattleHudView>();

            GameObject sessionRoot = CreateRoot("BattleSession");
            LevelSessionController levelSessionController = sessionRoot.AddComponent<LevelSessionController>();
            SetBool(levelSessionController, "autoStartSelectedLevel", false);
            SetObjectReference(levelSessionController, "hudView", hudView);

            CreateEventSystem();
            CreateCameraRig();
            CreateDirectionalLight();

            EditorSceneManager.SaveScene(scene, BattleScenePath);
        }

        private static void UpdateBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(BootScenePath, true),
                new EditorBuildSettingsScene(MainMenuScenePath, true),
                new EditorBuildSettingsScene(MetaScenePath, true),
                new EditorBuildSettingsScene(BattleScenePath, true),
            };
        }
        #endregion

        #region SceneHelpers
        private static SceneContext CreateSceneContext<TInstaller>(string name)
            where TInstaller : MonoInstaller
        {
            GameObject contextRoot = CreateRoot(name);
            SceneContext sceneContext = contextRoot.AddComponent<SceneContext>();
            TInstaller installer = contextRoot.AddComponent<TInstaller>();
            sceneContext.Installers = new[] { installer };
            return sceneContext;
        }

        private static GameObject CreateRoot(string name)
        {
            GameObject root = new GameObject(name);
            SceneManager.MoveGameObjectToScene(root, SceneManager.GetActiveScene());
            return root;
        }

        private static GameObject CreateChild(GameObject parent, string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent.transform, false);
            return child;
        }

        private static Canvas CreateCanvas(string name)
        {
            GameObject canvasRoot = CreateRoot(name);
            Canvas canvas = canvasRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasRoot.AddComponent<CanvasScaler>();
            canvasRoot.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static void CreateEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystemRoot = CreateRoot("EventSystem");
            eventSystemRoot.AddComponent<EventSystem>();
            eventSystemRoot.AddComponent<StandaloneInputModule>();
        }

        private static void CreateCameraRig()
        {
            if (Camera.main != null)
            {
                return;
            }

            GameObject cameraRoot = CreateRoot("Main Camera");
            Camera camera = cameraRoot.AddComponent<Camera>();
            camera.tag = "MainCamera";
            cameraRoot.transform.position = new Vector3(0f, 10f, -10f);
            cameraRoot.transform.rotation = Quaternion.Euler(35f, 0f, 0f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.09f, 0.1f, 0.12f, 1f);
        }

        private static void CreateDirectionalLight()
        {
            GameObject lightRoot = CreateRoot("Directional Light");
            Light light = lightRoot.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            lightRoot.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }
        #endregion

        #region Utility
        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string parentPath = folderPath.Substring(0, folderPath.LastIndexOf('/'));
            string folderName = folderPath.Substring(folderPath.LastIndexOf('/') + 1);
            EnsureFolder(parentPath);
            AssetDatabase.CreateFolder(parentPath, folderName);
        }

        private static TAsset LoadOrCreateAsset<TAsset>(string assetPath)
            where TAsset : ScriptableObject
        {
            TAsset existing = AssetDatabase.LoadAssetAtPath<TAsset>(assetPath);
            if (existing != null)
            {
                return existing;
            }

            TAsset asset = ScriptableObject.CreateInstance<TAsset>();
            AssetDatabase.CreateAsset(asset, assetPath);
            return asset;
        }

        private static TComponent GetOrAddComponent<TComponent>(GameObject gameObject)
            where TComponent : Component
        {
            TComponent component = gameObject.GetComponent<TComponent>();
            if (component == null)
            {
                component = gameObject.AddComponent<TComponent>();
            }

            return component;
        }

        private static void SetBool(Object target, string propertyName, bool value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(propertyName).boolValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetObjectReference(Object target, string propertyName, Object value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(propertyName).objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }
        #endregion
    }
}
