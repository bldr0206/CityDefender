using System.Collections.Generic;
using ColorChargeTD.Data;
using ColorChargeTD.Domain;
using ColorChargeTD.Presentation;
using UnityEditor;
using UnityEngine;

namespace ColorChargeTD.Editor
{
    public static class MvpLevelOneGenerator
    {
        private const string RootFolder = "Assets/_Game";
        private const string ArtFolder = RootFolder + "/Art";
        private const string MaterialsFolder = ArtFolder + "/Materials";
        private const string PrefabsFolder = RootFolder + "/Prefabs";
        private const string LayoutsFolder = PrefabsFolder + "/Layouts";
        private const string ContentFolder = RootFolder + "/Content";
        private const string TowersFolder = ContentFolder + "/Towers";
        private const string EnemiesFolder = ContentFolder + "/Enemies";
        private const string WavesFolder = ContentFolder + "/Waves";
        private const string LevelsFolder = ContentFolder + "/Levels";
        private const string LevelOneFolder = LevelsFolder + "/Level01";
        private const string BootstrapFolder = ContentFolder + "/Bootstrap";
        private const string ResourcesFolder = RootFolder + "/Resources";

        private const string GameContentAssetPath = ResourcesFolder + "/GameContentConfig.asset";
        private const string LevelCatalogAssetPath = BootstrapFolder + "/LevelCatalog.asset";
        private const string LevelOneLayoutPrefabPath = LayoutsFolder + "/Level01_LongS.prefab";
        private const string RedTowerAssetPath = TowersFolder + "/Tower_Red_Basic.asset";
        private const string RedEnemyAssetPath = EnemiesFolder + "/Enemy_Red_Slow.asset";
        private const string LevelOneWaveAssetPath = WavesFolder + "/Wave_Level01_Buffer.asset";
        private const string LevelOneAssetPath = LevelOneFolder + "/Level01_Buffer.asset";
        private const string GrassMaterialPath = MaterialsFolder + "/Grid_Grass.mat";
        private const string PathMaterialPath = MaterialsFolder + "/Grid_Path.mat";
        private const string SlotMaterialPath = MaterialsFolder + "/Grid_Slot.mat";
        private const string ProjectilesFolder = PrefabsFolder + "/Projectiles";
        private const string TowerPrefabAssetPath = PrefabsFolder + "/Towers/Tower_basic.prefab";
        private const string ProjectilePrefabAssetPath = ProjectilesFolder + "/TowerProjectile_basic.prefab";
        private const string EnemyPrefabAssetPath = PrefabsFolder + "/Enemies/Enemy_basic.prefab";

        private static readonly Vector3[] LevelOneWaypointPositions =
        {
            new Vector3(-6f, 0f, 5f),
            new Vector3(-4f, 0f, 2f),
            new Vector3(-1f, 0f, 5f),
            new Vector3(3f, 0f, 1f),
            new Vector3(-1f, 0f, -2f),
            new Vector3(3f, 0f, -5f),
            new Vector3(6f, 0f, -5f),
        };

        private static readonly Vector3[] LevelOneSlotPositions =
        {
            new Vector3(-3f, 0f, 4f),
            new Vector3(1f, 0f, 4f),
            new Vector3(2f, 0f, -2f),
        };

        [MenuItem("Tools/Color Charge TD/Generate MVP Level 1")]
        public static void GenerateLevelOne()
        {
            SceneBootstrapGenerator.GenerateBootstrapScenes();

            EnsureFolder(PrefabsFolder);
            EnsureFolder(ProjectilesFolder);
            EnsureFolder(LayoutsFolder);
            EnsureFolder(ArtFolder);
            EnsureFolder(MaterialsFolder);
            EnsureFolder(ContentFolder);
            EnsureFolder(TowersFolder);
            EnsureFolder(EnemiesFolder);
            EnsureFolder(WavesFolder);
            EnsureFolder(LevelsFolder);
            EnsureFolder(LevelOneFolder);

            TowerDefinition redTower = EnsureRedTowerDefinition();
            EnemyDefinition redEnemy = EnsureRedEnemyDefinition();
            EnsureGridMaterials();
            GameObject layoutPrefab = EnsureLevelOneLayoutPrefab();
            WaveDefinition waveDefinition = EnsureLevelOneWave(redEnemy);
            LevelDefinition levelDefinition = EnsureLevelOneDefinition(layoutPrefab, waveDefinition);

            RegisterInGameContent(redTower, redEnemy);
            RegisterInLevelCatalog(levelDefinition);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Color Charge TD MVP Level 1 assets were generated.");
        }

        #region Definitions
        private static TowerDefinition EnsureRedTowerDefinition()
        {
            TowerDefinition towerDefinition = LoadOrCreateAsset<TowerDefinition>(RedTowerAssetPath);
            towerDefinition.name = "Tower_Red_Basic";

            SerializedObject serializedObject = new SerializedObject(towerDefinition);
            serializedObject.FindProperty("towerId").stringValue = "tower-red-basic";
            serializedObject.FindProperty("displayName").stringValue = "Red Basic Tower";
            serializedObject.FindProperty("color").enumValueIndex = (int)ColorCharge.Red;
            serializedObject.FindProperty("buildCost").intValue = 50;
            serializedObject.FindProperty("damagePerShot").intValue = 1;
            serializedObject.FindProperty("capacity").intValue = 3;
            serializedObject.FindProperty("productionPerSecond").floatValue = 3f;
            serializedObject.FindProperty("fireRatePerSecond").floatValue = 1f;
            serializedObject.FindProperty("range").floatValue = 4f;
            serializedObject.FindProperty("overchargeMultiplier").floatValue = 2f;
            serializedObject.FindProperty("projectileTravelTime").floatValue = 0.25f;
            serializedObject.FindProperty("prefab").objectReferenceValue = LoadPrefabAsset(TowerPrefabAssetPath);
            serializedObject.FindProperty("projectilePrefab").objectReferenceValue = LoadPrefabAsset(ProjectilePrefabAssetPath);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(towerDefinition);
            return towerDefinition;
        }

        private static EnemyDefinition EnsureRedEnemyDefinition()
        {
            EnemyDefinition enemyDefinition = LoadOrCreateAsset<EnemyDefinition>(RedEnemyAssetPath);
            enemyDefinition.name = "Enemy_Red_Slow";

            SerializedObject serializedObject = new SerializedObject(enemyDefinition);
            serializedObject.FindProperty("enemyId").stringValue = "enemy-red-slow";
            serializedObject.FindProperty("displayName").stringValue = "Red Slow Enemy";
            serializedObject.FindProperty("color").enumValueIndex = (int)ColorCharge.Red;
            serializedObject.FindProperty("hitPoints").intValue = 3;
            serializedObject.FindProperty("speed").floatValue = 1f;
            serializedObject.FindProperty("baseReward").intValue = 1;
            serializedObject.FindProperty("prefab").objectReferenceValue = LoadPrefabAsset(EnemyPrefabAssetPath);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(enemyDefinition);
            return enemyDefinition;
        }

        private static WaveDefinition EnsureLevelOneWave(EnemyDefinition enemyDefinition)
        {
            WaveDefinition waveDefinition = LoadOrCreateAsset<WaveDefinition>(LevelOneWaveAssetPath);
            waveDefinition.name = "Wave_Level01_Buffer";

            SerializedObject serializedObject = new SerializedObject(waveDefinition);
            SerializedProperty groupsProperty = serializedObject.FindProperty("groups");
            groupsProperty.arraySize = 1;

            SerializedProperty groupProperty = groupsProperty.GetArrayElementAtIndex(0);
            groupProperty.FindPropertyRelative("enemy").objectReferenceValue = enemyDefinition;
            groupProperty.FindPropertyRelative("pathId").stringValue = "path-main";
            groupProperty.FindPropertyRelative("count").intValue = 6;
            groupProperty.FindPropertyRelative("startDelay").floatValue = 0.75f;
            groupProperty.FindPropertyRelative("spawnInterval").floatValue = 0.9f;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(waveDefinition);
            return waveDefinition;
        }

        private static LevelDefinition EnsureLevelOneDefinition(GameObject layoutPrefab, WaveDefinition waveDefinition)
        {
            LevelDefinition levelDefinition = LoadOrCreateAsset<LevelDefinition>(LevelOneAssetPath);
            levelDefinition.name = "Level01_Buffer";

            SerializedObject serializedObject = new SerializedObject(levelDefinition);
            serializedObject.FindProperty("levelId").stringValue = "level-01";
            serializedObject.FindProperty("displayName").stringValue = "Level 1 - Buffer Exists";
            serializedObject.FindProperty("tutorialFocus").stringValue = "Teach that towers fire in bursts and depend on stored charge.";
            serializedObject.FindProperty("layoutPrefab").objectReferenceValue = layoutPrefab;
            serializedObject.FindProperty("waveSet").objectReferenceValue = waveDefinition;
            serializedObject.FindProperty("startingResourceOverride").intValue = 125;

            SerializedProperty rewardProperty = serializedObject.FindProperty("reward");
            rewardProperty.FindPropertyRelative("softCurrency").intValue = 25;
            rewardProperty.FindPropertyRelative("firstCompletionBonus").intValue = 10;

            SerializedProperty unlockRuleProperty = serializedObject.FindProperty("unlockRule");
            unlockRuleProperty.FindPropertyRelative("unlockedByDefault").boolValue = true;
            unlockRuleProperty.FindPropertyRelative("requiredLevelId").stringValue = string.Empty;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(levelDefinition);
            return levelDefinition;
        }
        #endregion

        #region LayoutPrefab
        private static GameObject EnsureLevelOneLayoutPrefab()
        {
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(LevelOneLayoutPrefabPath);
            if (existingPrefab != null)
            {
                GameObject prefabRoot = PrefabUtility.LoadPrefabContents(LevelOneLayoutPrefabPath);
                ConfigureLevelOneLayoutPrefab(prefabRoot);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, LevelOneLayoutPrefabPath);
                PrefabUtility.UnloadPrefabContents(prefabRoot);
                return AssetDatabase.LoadAssetAtPath<GameObject>(LevelOneLayoutPrefabPath);
            }

            GameObject prefab = new GameObject("Level01_LongS");
            ConfigureLevelOneLayoutPrefab(prefab);
            PrefabUtility.SaveAsPrefabAsset(prefab, LevelOneLayoutPrefabPath);
            Object.DestroyImmediate(prefab);
            return AssetDatabase.LoadAssetAtPath<GameObject>(LevelOneLayoutPrefabPath);
        }

        private static void ConfigureLevelOneLayoutPrefab(GameObject root)
        {
            DeleteAllChildren(root.transform);

            LevelLayoutAuthoring layoutAuthoring = GetOrAddComponent<LevelLayoutAuthoring>(root);
            SetString(layoutAuthoring, "layoutId", "layout-level-01");
            SetBool(layoutAuthoring, "autoCollectChildren", true);

            GameObject pathsRoot = CreateChild(root, "Paths");
            GameObject pathRoot = CreateChild(pathsRoot, "Path_Main");
            PathAuthoring pathAuthoring = pathRoot.AddComponent<PathAuthoring>();
            SetString(pathAuthoring, "pathId", "path-main");
            SetEnum(pathAuthoring, "movePattern", (int)EnemyMovePattern.SingleLane);
            SetBool(pathAuthoring, "autoCollectChildren", true);

            for (int i = 0; i < LevelOneWaypointPositions.Length; i++)
            {
                GameObject waypoint = CreateChild(pathRoot, "WP_" + i.ToString("00"));
                waypoint.transform.localPosition = LevelOneWaypointPositions[i];
            }

            GameObject buildSlotsRoot = CreateChild(root, "BuildSlots");
            CreateBuildSlot(buildSlotsRoot, "Slot_01", LevelOneSlotPositions[0], 1.4f);
            CreateBuildSlot(buildSlotsRoot, "Slot_02", LevelOneSlotPositions[1], 1.4f);
            CreateBuildSlot(buildSlotsRoot, "Slot_03", LevelOneSlotPositions[2], 1.4f);

            GameObject baseRoot = CreateChild(root, "Base");
            baseRoot.transform.localPosition = new Vector3(6f, 0f, -5f);
            baseRoot.AddComponent<BaseTargetAuthoring>();

            GameObject artRoot = CreateChild(root, "Art");
            CreateChild(root, "Debug");

            UpdateCollectedReferences(layoutAuthoring, pathAuthoring);
            GenerateGridArt(artRoot.transform);
            EditorUtility.SetDirty(root);
        }

        private static void CreateBuildSlot(GameObject parent, string slotId, Vector3 localPosition, float radius)
        {
            GameObject slotRoot = CreateChild(parent, slotId);
            slotRoot.transform.localPosition = localPosition;

            BuildSlotAuthoring buildSlotAuthoring = slotRoot.AddComponent<BuildSlotAuthoring>();
            SetString(buildSlotAuthoring, "slotId", slotId.ToLowerInvariant().Replace("_", "-"));
            SetFloat(buildSlotAuthoring, "radius", radius);
        }

        private static void UpdateCollectedReferences(LevelLayoutAuthoring layoutAuthoring, PathAuthoring pathAuthoring)
        {
            SerializedObject layoutObject = new SerializedObject(layoutAuthoring);
            layoutObject.FindProperty("paths").arraySize = 1;
            layoutObject.FindProperty("paths").GetArrayElementAtIndex(0).objectReferenceValue = pathAuthoring;

            BuildSlotAuthoring[] slots = layoutAuthoring.GetComponentsInChildren<BuildSlotAuthoring>(true);
            SerializedProperty slotsProperty = layoutObject.FindProperty("buildSlots");
            slotsProperty.arraySize = slots.Length;
            for (int i = 0; i < slots.Length; i++)
            {
                slotsProperty.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
            }

            layoutObject.FindProperty("baseTarget").objectReferenceValue = layoutAuthoring.GetComponentInChildren<BaseTargetAuthoring>(true);
            layoutObject.ApplyModifiedPropertiesWithoutUndo();

            Transform[] waypoints = pathAuthoring.GetComponentsInChildren<Transform>(true);
            int waypointCount = Mathf.Max(0, waypoints.Length - 1);

            SerializedObject pathObject = new SerializedObject(pathAuthoring);
            SerializedProperty waypointProperty = pathObject.FindProperty("waypoints");
            waypointProperty.arraySize = waypointCount;

            int targetIndex = 0;
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] == pathAuthoring.transform)
                {
                    continue;
                }

                waypointProperty.GetArrayElementAtIndex(targetIndex).objectReferenceValue = waypoints[i];
                targetIndex++;
            }

            pathObject.ApplyModifiedPropertiesWithoutUndo();
        }
        #endregion

        #region MaterialsAndArt
        private static void EnsureGridMaterials()
        {
            EnsureGridMaterial(
                GrassMaterialPath,
                "Grid_Grass",
                new Color(0.23f, 0.5f, 0.23f, 1f));

            EnsureGridMaterial(
                PathMaterialPath,
                "Grid_Path",
                new Color(0.82f, 0.67f, 0.42f, 1f));

            EnsureGridMaterial(
                SlotMaterialPath,
                "Grid_Slot",
                new Color(0.45f, 0.55f, 0.72f, 1f));
        }

        private static void GenerateGridArt(Transform artRoot)
        {
            DeleteAllChildren(artRoot);

            GameObject grassRoot = CreateChild(artRoot.gameObject, "Ground_Grass");
            GameObject pathRoot = CreateChild(artRoot.gameObject, "Ground_Path");
            GameObject slotsRoot = CreateChild(artRoot.gameObject, "Ground_Slots");

            Material grassMaterial = AssetDatabase.LoadAssetAtPath<Material>(GrassMaterialPath);
            Material pathMaterial = AssetDatabase.LoadAssetAtPath<Material>(PathMaterialPath);
            Material slotMaterial = AssetDatabase.LoadAssetAtPath<Material>(SlotMaterialPath);

            HashSet<Vector2Int> slotCells = new HashSet<Vector2Int>();
            for (int i = 0; i < LevelOneSlotPositions.Length; i++)
            {
                slotCells.Add(ToGridCell(LevelOneSlotPositions[i]));
            }

            BoundsInt gridBounds = CalculateGridBounds(LevelOneWaypointPositions, LevelOneSlotPositions);
            for (int z = gridBounds.zMin; z < gridBounds.zMax; z++)
            {
                for (int x = gridBounds.xMin; x < gridBounds.xMax; x++)
                {
                    Vector2Int cell = new Vector2Int(x, z);
                    Vector3 tilePosition = new Vector3(x, 0f, z);

                    if (slotCells.Contains(cell))
                    {
                        CreateTile(slotsRoot.transform, "SlotTile_" + x + "_" + z, tilePosition, new Vector3(1f, 0.4f, 1f), slotMaterial);
                        continue;
                    }

                    bool isPathCell = IsPathCell(cell, LevelOneWaypointPositions, 0.72f);
                    if (isPathCell)
                    {
                        CreateTile(pathRoot.transform, "PathTile_" + x + "_" + z, tilePosition, new Vector3(1f, 0.22f, 1f), pathMaterial);
                        continue;
                    }

                    CreateTile(grassRoot.transform, "GrassTile_" + x + "_" + z, tilePosition, new Vector3(1f, 0.18f, 1f), grassMaterial);
                }
            }
        }

        private static void EnsureGridMaterial(string materialPath, string materialName, Color color)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }

                material = new Material(shader);
                AssetDatabase.CreateAsset(material, materialPath);
            }

            material.name = materialName;
            material.color = color;

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            EditorUtility.SetDirty(material);
        }

        private static void CreateTile(Transform parent, string name, Vector3 gridPosition, Vector3 scale, Material material)
        {
            GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tile.name = name;
            tile.transform.SetParent(parent, false);
            tile.transform.localPosition = new Vector3(gridPosition.x, -scale.y * 0.5f, gridPosition.z);
            tile.transform.localScale = scale;

            Collider collider = tile.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }

            MeshRenderer renderer = tile.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        private static BoundsInt CalculateGridBounds(Vector3[] waypoints, Vector3[] slots)
        {
            int minX = int.MaxValue;
            int maxX = int.MinValue;
            int minZ = int.MaxValue;
            int maxZ = int.MinValue;

            IncludePositions(waypoints, ref minX, ref maxX, ref minZ, ref maxZ);
            IncludePositions(slots, ref minX, ref maxX, ref minZ, ref maxZ);

            minX -= 2;
            maxX += 3;
            minZ -= 2;
            maxZ += 3;

            return new BoundsInt(minX, 0, minZ, maxX - minX, 1, maxZ - minZ);
        }

        private static void IncludePositions(Vector3[] positions, ref int minX, ref int maxX, ref int minZ, ref int maxZ)
        {
            for (int i = 0; i < positions.Length; i++)
            {
                Vector2Int cell = ToGridCell(positions[i]);
                minX = Mathf.Min(minX, cell.x);
                maxX = Mathf.Max(maxX, cell.x);
                minZ = Mathf.Min(minZ, cell.y);
                maxZ = Mathf.Max(maxZ, cell.y);
            }
        }

        private static bool IsPathCell(Vector2Int cell, Vector3[] waypoints, float threshold)
        {
            Vector2 cellPoint = new Vector2(cell.x, cell.y);

            for (int i = 0; i < waypoints.Length - 1; i++)
            {
                Vector2 start = new Vector2(waypoints[i].x, waypoints[i].z);
                Vector2 end = new Vector2(waypoints[i + 1].x, waypoints[i + 1].z);
                float distance = DistanceToSegment(cellPoint, start, end);
                if (distance <= threshold)
                {
                    return true;
                }
            }

            return false;
        }

        private static float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end)
        {
            Vector2 segment = end - start;
            float sqrMagnitude = segment.sqrMagnitude;
            if (sqrMagnitude <= Mathf.Epsilon)
            {
                return Vector2.Distance(point, start);
            }

            float t = Mathf.Clamp01(Vector2.Dot(point - start, segment) / sqrMagnitude);
            Vector2 projection = start + segment * t;
            return Vector2.Distance(point, projection);
        }

        private static Vector2Int ToGridCell(Vector3 position)
        {
            return new Vector2Int(Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.z));
        }
        #endregion

        #region Registration
        private static void RegisterInGameContent(TowerDefinition towerDefinition, EnemyDefinition enemyDefinition)
        {
            GameContentConfig gameContent = AssetDatabase.LoadAssetAtPath<GameContentConfig>(GameContentAssetPath);
            if (gameContent == null)
            {
                return;
            }

            SerializedObject serializedObject = new SerializedObject(gameContent);
            AddUniqueObjectReference(serializedObject.FindProperty("towers"), towerDefinition);
            AddUniqueObjectReference(serializedObject.FindProperty("enemies"), enemyDefinition);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(gameContent);
        }

        private static void RegisterInLevelCatalog(LevelDefinition levelDefinition)
        {
            LevelCatalogDefinition levelCatalog = AssetDatabase.LoadAssetAtPath<LevelCatalogDefinition>(LevelCatalogAssetPath);
            if (levelCatalog == null)
            {
                return;
            }

            SerializedObject serializedObject = new SerializedObject(levelCatalog);
            AddUniqueObjectReference(serializedObject.FindProperty("levels"), levelDefinition);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(levelCatalog);
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

        private static GameObject LoadPrefabAsset(string assetPath)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null)
            {
                Debug.LogWarning("Color Charge TD could not find prefab asset at path: " + assetPath);
            }

            return prefab;
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

        private static GameObject CreateChild(GameObject parent, string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent.transform, false);
            return child;
        }

        private static void DeleteAllChildren(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(root.GetChild(i).gameObject);
            }
        }

        private static void AddUniqueObjectReference(SerializedProperty listProperty, Object asset)
        {
            for (int i = 0; i < listProperty.arraySize; i++)
            {
                if (listProperty.GetArrayElementAtIndex(i).objectReferenceValue == asset)
                {
                    return;
                }
            }

            int nextIndex = listProperty.arraySize;
            listProperty.arraySize++;
            listProperty.GetArrayElementAtIndex(nextIndex).objectReferenceValue = asset;
        }

        private static void SetString(Object target, string propertyName, string value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(propertyName).stringValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetBool(Object target, string propertyName, bool value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(propertyName).boolValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetFloat(Object target, string propertyName, float value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(propertyName).floatValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetEnum(Object target, string propertyName, int enumValueIndex)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(propertyName).enumValueIndex = enumValueIndex;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }
        #endregion
    }
}
