using System.Collections.Generic;
using ColorChargeTD.Data;
using ColorChargeTD.Domain;
using ColorChargeTD.Presentation;
using UnityEditor;
using UnityEngine;

namespace ColorChargeTD.Editor
{
    public sealed class GameContentValidatorWindow : EditorWindow
    {
        private readonly List<ContentValidationMessage> messages = new List<ContentValidationMessage>();
        private Vector2 scrollPosition;
        private GameContentConfig contentConfig;

        [MenuItem("Tools/Color Charge TD/Validate Content")]
        public static void Open()
        {
            GameContentValidatorWindow window = GetWindow<GameContentValidatorWindow>("Color Charge Validator");
            window.minSize = new Vector2(700f, 400f);
            window.RunValidation();
        }

        private void OnEnable()
        {
            if (contentConfig == null)
            {
                contentConfig = FindFirstAsset<GameContentConfig>();
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            contentConfig = (GameContentConfig)EditorGUILayout.ObjectField("Game Content", contentConfig, typeof(GameContentConfig), false);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Validate"))
                {
                    RunValidation();
                }

                if (GUILayout.Button("Use First Asset"))
                {
                    contentConfig = FindFirstAsset<GameContentConfig>();
                    RunValidation();
                }
            }

            EditorGUILayout.Space();
            DrawSummary();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            for (int i = 0; i < messages.Count; i++)
            {
                DrawMessage(messages[i]);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawSummary()
        {
            int errorCount = 0;
            int warningCount = 0;

            for (int i = 0; i < messages.Count; i++)
            {
                switch (messages[i].Severity)
                {
                    case ValidationSeverity.Error:
                        errorCount++;
                        break;

                    case ValidationSeverity.Warning:
                        warningCount++;
                        break;
                }
            }

            EditorGUILayout.HelpBox("Errors: " + errorCount + " | Warnings: " + warningCount + " | Messages: " + messages.Count, MessageType.Info);
        }

        private void DrawMessage(ContentValidationMessage message)
        {
            MessageType messageType = MessageType.None;
            switch (message.Severity)
            {
                case ValidationSeverity.Info:
                    messageType = MessageType.Info;
                    break;

                case ValidationSeverity.Warning:
                    messageType = MessageType.Warning;
                    break;

                case ValidationSeverity.Error:
                    messageType = MessageType.Error;
                    break;
            }

            EditorGUILayout.HelpBox("[" + message.Source + "] " + message.Message, messageType);
        }

        private void RunValidation()
        {
            messages.Clear();

            if (contentConfig == null)
            {
                messages.Add(ContentValidationMessage.Error("Validator", "GameContentConfig asset is required."));
                return;
            }

            contentConfig.ValidateInto(messages);
            ValidateLevelLayouts(contentConfig.LevelCatalog);
        }

        private void ValidateLevelLayouts(LevelCatalogDefinition levelCatalog)
        {
            if (levelCatalog == null)
            {
                return;
            }

            for (int i = 0; i < levelCatalog.Levels.Count; i++)
            {
                LevelDefinition levelDefinition = levelCatalog.Levels[i];
                if (levelDefinition == null || levelDefinition.LayoutPrefab == null)
                {
                    continue;
                }

                LevelLayoutAuthoring layoutAuthoring = levelDefinition.LayoutPrefab.GetComponent<LevelLayoutAuthoring>();
                if (layoutAuthoring == null)
                {
                    messages.Add(ContentValidationMessage.Error(levelDefinition.name, "Layout prefab must contain LevelLayoutAuthoring."));
                    continue;
                }

                if (!layoutAuthoring.TryBuildDefinition(out _, out string error))
                {
                    messages.Add(ContentValidationMessage.Error(levelDefinition.name, error));
                }
            }
        }

        private static TAsset FindFirstAsset<TAsset>() where TAsset : Object
        {
            string[] guids = AssetDatabase.FindAssets("t:" + typeof(TAsset).Name);
            if (guids.Length == 0)
            {
                return null;
            }

            string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<TAsset>(assetPath);
        }
    }
}
