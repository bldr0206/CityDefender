using System;
using System.Collections;
using System.Collections.Generic;
using ColorChargeTD.Core;
using ColorChargeTD.Data;
using ColorChargeTD.Domain;
using ColorChargeTD.Presentation;
using ColorChargeTD.Profile;
using ColorChargeTD.Product;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Zenject;

namespace ColorChargeTD.Battle
{
    public sealed class LevelSessionController : MonoBehaviour
    {
        [Header("Runtime Setup")]
        [SerializeField] private LevelDefinition levelDefinitionOverride;
        [SerializeField] private LevelLayoutAuthoring layoutOverride;
        [SerializeField] private bool autoStartSelectedLevel = true;
        [SerializeField] [Min(0.05f)] private float waveProgressFillSpeed = 2.5f;

        [Header("Optional Views")]
        [SerializeField] private BattleHudView hudView;
        [SerializeField] private BattleCoinFlyoutPresenter coinFlyoutPresenter;
        [SerializeField] private BattleDamageFlyoutPresenter damageFlyoutPresenter;

        [Header("Path route markers")]
        [SerializeField] private GameObject pathRouteMarkerPrefab;
        [SerializeField] [Min(0.05f)] private float pathMarkerSpacing = 1.2f;
        [SerializeField] private float pathMarkerYOffset = 0.01f;

        [Header("Tower radial menu")]
        [SerializeField] private GameObject towerRadialMenuShellPrefab;
        [SerializeField] private GameObject towerRadialOptionPrefab;

        [Header("Build slots")]
        [SerializeField] private GameObject buildSlotPlusVisualPrefab;

        private readonly List<TowerRuntimeModel> towers = new List<TowerRuntimeModel>();
        private readonly List<EnemyRuntimeModel> enemies = new List<EnemyRuntimeModel>();
        private readonly List<PlacedStructureRuntimeModel> placedStructures = new List<PlacedStructureRuntimeModel>();

        private WaveSpawnerSystem waveSpawnerSystem;
        private EnemyPathSystem enemyPathSystem;
        private TowerTargetingSystem towerTargetingSystem;
        private TowerChargeSystem towerChargeSystem;
        private DamageResolver damageResolver;
        private ProjectileHitScheduler projectileHitScheduler;
        private BattleResultService battleResultService;
        private BattlePresentationSystem presentationSystem;
        private LevelSessionRuntime session;
        private LevelDefinition activeLevelDefinition;
        private IReadOnlyList<TowerDefinition> buildableTowers;
        private GameObject activeLayoutInstance;
        private bool ownsActiveLayoutInstance;
        private bool isConfigured;

        private readonly List<BuildSlotWorldHandle> buildSlotHandles = new List<BuildSlotWorldHandle>();
        private GameObject buildSlotHandlesRoot;
        private BattleTowerRadialMenu radialBuildMenu;
        private float smoothedWaveProgress;
        private Coroutine deferredBattleResultNavigation;

        [Inject] private IGameContentService contentService;
        [Inject] private ILevelSelectionService levelSelectionService;
        [Inject] private IProgressionService progressionService;
        [Inject] private IGameNavigationService navigationService;
        [Inject] private IPlayerProfileService profileService;
        [Inject(Optional = true)] private GameFlowPresentationSettings flowPresentationSettings;

        #region UnityLifecycle
        private void Start()
        {
            if (hudView != null)
            {
                hudView.SetStartWaveClickedHandler(HandleStartWaveClicked);
            }

            if (autoStartSelectedLevel)
            {
                StartConfiguredLevel();
            }
        }

        private void Update()
        {
            if (!isConfigured || session == null)
            {
                return;
            }

            if (session.IsFinished)
            {
                if (deferredBattleResultNavigation != null)
                {
                    TickPostVictoryWaveProgressHud();
                }

                return;
            }

            if (Time.timeScale <= 0f)
            {
                return;
            }

            HandleBuildSlotInput();

            float deltaTime = Time.deltaTime;
            TickAuxiliaryIncome(deltaTime);
            TickEnemyCrowdControl(deltaTime);
            waveSpawnerSystem.Tick(deltaTime, enemies);
            enemyPathSystem.Tick(deltaTime, enemies, session);
            towerChargeSystem.Tick(deltaTime, towers, ShouldRechargeTowerAmmo());
            projectileHitScheduler.Tick(deltaTime);
            towerTargetingSystem.Tick(deltaTime, towers, enemies, damageResolver, projectileHitScheduler, HandleTowerFiredForPresentation);
            presentationSystem.Sync(towers, enemies, placedStructures);

            UpdateHud();

            BattleOutcome outcome = battleResultService.Evaluate(session, waveSpawnerSystem, enemies);
            if (outcome == BattleOutcome.None)
            {
                return;
            }

            FinishSession(outcome);
        }
        #endregion

        #region SessionSetup
        public void StartConfiguredLevel()
        {
            LevelDefinition targetLevel = ResolveLevelDefinition();
            if (targetLevel == null)
            {
                Debug.LogWarning("LevelSessionController could not resolve a level definition.");
                return;
            }

            StartLevel(targetLevel);
        }

        public void StartLevel(LevelDefinition levelDefinition)
        {
            ResetSessionState();

            activeLevelDefinition = levelDefinition;
            buildableTowers = levelDefinition.ResolveBuildableTowers(contentService.Towers);
            LevelLayoutAuthoring layoutAuthoring = ResolveLayout(levelDefinition);
            if (layoutAuthoring == null)
            {
                Debug.LogWarning("Cannot start level without a LevelLayoutAuthoring.");
                return;
            }

            if (!layoutAuthoring.TryBuildDefinition(out LevelLayoutRuntimeDefinition layoutDefinition, out string error))
            {
                Debug.LogWarning(error);
                return;
            }

            towers.Clear();
            enemies.Clear();
            placedStructures.Clear();

            session = new LevelSessionRuntime(
                levelDefinition,
                layoutDefinition,
                contentService.BalanceConfig,
                ResolveStartingChargeBonus(),
                ResolveStartingResource(levelDefinition));

            waveSpawnerSystem = new WaveSpawnerSystem(levelDefinition.WaveSet, layoutDefinition);
            enemyPathSystem = new EnemyPathSystem(HandleEnemyKilledForCoinFlyout, RegisterWaveKillForHud);
            damageResolver = new DamageResolver(contentService.BalanceConfig, HandleEnemyDamagedForFlyout);
            projectileHitScheduler = new ProjectileHitScheduler(damageResolver);
            towerChargeSystem = new TowerChargeSystem();
            towerTargetingSystem = new TowerTargetingSystem();
            battleResultService = new BattleResultService();
            presentationSystem = new BattlePresentationSystem();
            presentationSystem.Initialize(layoutAuthoring.transform);
            presentationSystem.BuildPathRouteMarkers(
                layoutDefinition,
                levelDefinition.WaveSet,
                pathRouteMarkerPrefab,
                pathMarkerSpacing,
                pathMarkerYOffset);
            presentationSystem.Sync(towers, enemies, placedStructures);
            CreateBuildSlotHandles(layoutAuthoring.transform, layoutDefinition);
            isConfigured = true;

            UpdateHud();
        }

        public string ActiveLevelId =>
            activeLevelDefinition != null ? activeLevelDefinition.LevelId : string.Empty;

        public bool TryPlaceTower(string towerId, string slotId)
        {
            if (!isConfigured || session == null || session.IsFinished)
            {
                return false;
            }

            TowerDefinition towerDefinition = contentService.GetTowerById(towerId);
            if (towerDefinition == null || !IsTowerInBuildableList(towerDefinition))
            {
                return false;
            }

            BuildSlotRuntimeDefinition slot = session.FindSlot(slotId);
            if (string.IsNullOrWhiteSpace(slot.SlotId) || session.IsSlotOccupied(slot.SlotId))
            {
                return false;
            }

            if (slot.Kind != BuildSlotKind.Tower)
            {
                return false;
            }

            if (session.CurrentResource < towerDefinition.BuildCost)
            {
                return false;
            }

            session.OccupySlot(slot.SlotId);
            TowerRuntimeModel tower = new TowerRuntimeModel(towerDefinition, slot, 1f);
            towers.Add(tower);
            session.CurrentResource -= towerDefinition.BuildCost;
            presentationSystem.Sync(towers, enemies, placedStructures);
            UpdateHud();
            return true;
        }

        public bool TryPlacePlaceableStructure(string structureId, string slotId)
        {
            if (!isConfigured || session == null || session.IsFinished)
            {
                return false;
            }

            BuildSlotRuntimeDefinition slot = session.FindSlot(slotId);
            if (string.IsNullOrWhiteSpace(slot.SlotId) || session.IsSlotOccupied(slot.SlotId))
            {
                return false;
            }

            PlaceableStructureDefinition def = null;
            if (slot.Kind == BuildSlotKind.Auxiliary)
            {
                def = FindAuxiliaryOnSlot(slot, structureId);
            }
            else if (slot.Kind == BuildSlotKind.RoadTrap)
            {
                def = FindRoadTrapOnSlot(slot, structureId);
            }
            else
            {
                return false;
            }

            if (def == null)
            {
                return false;
            }

            AuxiliaryBuildingDefinition auxGate = def as AuxiliaryBuildingDefinition;
            if (auxGate != null && !IsAuxiliaryUnlockedForBuild(auxGate))
            {
                return false;
            }

            if (session.CurrentResource < def.BuildCost)
            {
                return false;
            }

            session.OccupySlot(slot.SlotId);
            session.CurrentResource -= def.BuildCost;
            placedStructures.Add(new PlacedStructureRuntimeModel(slot.Kind, def, slot));
            presentationSystem.Sync(towers, enemies, placedStructures);
            UpdateHud();
            return true;
        }

        private LevelDefinition ResolveLevelDefinition()
        {
            if (levelDefinitionOverride != null)
            {
                return levelDefinitionOverride;
            }

            LevelCatalogDefinition catalog = contentService.LevelCatalog;
            if (catalog == null)
            {
                return null;
            }

            string levelId = levelSelectionService.SelectedLevelId;
            if (string.IsNullOrWhiteSpace(levelId) && catalog.Levels.Count > 0)
            {
                return catalog.Levels[0];
            }

            return catalog.GetLevelById(levelId);
        }

        private LevelLayoutAuthoring ResolveLayout(LevelDefinition levelDefinition)
        {
            if (layoutOverride != null)
            {
                activeLayoutInstance = layoutOverride.gameObject;
                ownsActiveLayoutInstance = false;
                return layoutOverride;
            }

            if (levelDefinition == null || levelDefinition.LayoutPrefab == null)
            {
                return null;
            }

            activeLayoutInstance = Instantiate(levelDefinition.LayoutPrefab);
            ownsActiveLayoutInstance = true;
            return activeLayoutInstance.GetComponent<LevelLayoutAuthoring>();
        }

        private int ResolveStartingResource(LevelDefinition levelDefinition)
        {
            int baseValue = levelDefinition.ResolveStartingResource(contentService.BalanceConfig);
            int bonus = ResolveUpgradeValue(UpgradeEffectType.StartingResourceBonus);
            return baseValue + bonus;
        }

        private float ResolveStartingChargeBonus()
        {
            float bonus = ResolveUpgradeValue(UpgradeEffectType.StartingChargeBonus);
            return Mathf.Clamp01(bonus);
        }

        private int ResolveUpgradeValue(UpgradeEffectType effectType)
        {
            int sum = 0;

            for (int i = 0; i < contentService.Upgrades.Count; i++)
            {
                UpgradeDefinition upgrade = contentService.Upgrades[i];
                if (upgrade == null || upgrade.EffectType != effectType)
                {
                    continue;
                }

                int level = profileService.CurrentProfile.GetUpgradeLevel(upgrade.UpgradeId);

                for (int index = 0; index < level; index++)
                {
                    sum += Mathf.RoundToInt(upgrade.GetValueForLevel(index));
                }
            }

            return sum;
        }
        #endregion

        #region SessionFinish
        private void FinishSession(BattleOutcome outcome)
        {
            session.Finish(outcome);

            DisposeRadialBuildMenu();

            if (hudView != null)
            {
                hudView.SetStartWaveVisible(false);
            }

            int reward = outcome == BattleOutcome.Victory
                ? activeLevelDefinition.Reward.SoftCurrency + session.AccumulatedKillReward
                : session.AccumulatedKillReward;

            BattleResultModel result = progressionService.ApplyBattleResult(activeLevelDefinition, outcome, reward);

            float victoryDelay = outcome == BattleOutcome.Victory && flowPresentationSettings != null
                ? flowPresentationSettings.VictoryResultScreenDelaySeconds
                : 0f;

            if (victoryDelay > 0f)
            {
                StopDeferredBattleResultNavigation();
                deferredBattleResultNavigation = StartCoroutine(OpenBattleResultAfterDelay(result, victoryDelay));
            }
            else
            {
                if (outcome == BattleOutcome.Victory && hudView != null)
                {
                    smoothedWaveProgress = 1f;
                    hudView.SetWaveProgress(1f);
                }

                navigationService.OpenBattleResult(result);
            }
        }

        private void TickPostVictoryWaveProgressHud()
        {
            if (hudView == null || waveSpawnerSystem == null)
            {
                return;
            }

            float dt = Time.unscaledDeltaTime;
            smoothedWaveProgress = Mathf.MoveTowards(smoothedWaveProgress, 1f, waveProgressFillSpeed * dt);
            hudView.SetWaveProgress(smoothedWaveProgress);
            hudView.SetWaveLabel(waveSpawnerSystem.DisplayWaveNumber, waveSpawnerSystem.TotalWaveGroups);
        }

        private IEnumerator OpenBattleResultAfterDelay(BattleResultModel result, float delaySeconds)
        {
            yield return new WaitForSecondsRealtime(delaySeconds);
            deferredBattleResultNavigation = null;
            navigationService.OpenBattleResult(result);
        }

        private void StopDeferredBattleResultNavigation()
        {
            if (deferredBattleResultNavigation == null)
            {
                return;
            }

            StopCoroutine(deferredBattleResultNavigation);
            deferredBattleResultNavigation = null;
        }

        private void UpdateHud()
        {
            if (session != null)
            {
                RefreshBuildSlotVisuals();
            }

            if (hudView == null || session == null)
            {
                return;
            }

            if (activeLevelDefinition != null)
            {
                hudView.SetLevelTitle(activeLevelDefinition.DisplayName);
            }
            else
            {
                hudView.SetLevelTitle(string.Empty);
            }

            hudView.SetResource(session.CurrentResource);
            hudView.SetLives(session.RemainingLives);

            if (waveSpawnerSystem != null)
            {
                hudView.SetWaveLabel(waveSpawnerSystem.DisplayWaveNumber, waveSpawnerSystem.TotalWaveGroups);
                float targetWaveProgress = waveSpawnerSystem.ProgressNormalized;
                if (waveSpawnerSystem.IsComplete && enemies.Count == 0)
                {
                    targetWaveProgress = 1f;
                }

                smoothedWaveProgress = Mathf.MoveTowards(
                    smoothedWaveProgress,
                    targetWaveProgress,
                    waveProgressFillSpeed * Time.deltaTime);
                hudView.SetWaveProgress(smoothedWaveProgress);
            }
            else
            {
                hudView.SetWaveLabel(0, 0);
                smoothedWaveProgress = 0f;
                hudView.SetWaveProgress(0f);
            }

            bool waitingForWaveStart = waveSpawnerSystem != null
                && waveSpawnerSystem.NeedsPlayerStart
                && enemies.Count == 0;
            bool hasTowers = towers.Count > 0 || placedStructures.Count > 0;

            if (waitingForWaveStart)
            {
                hudView.SetStartWaveVisible(true);
                hudView.SetPlaceTowerHintVisible(!hasTowers);
                hudView.SetStartWaveButtonVisible(hasTowers);
            }
            else
            {
                hudView.SetStartWaveVisible(false);
                hudView.SetPlaceTowerHintVisible(false);
                hudView.SetStartWaveButtonVisible(false);
            }
        }
        #endregion

        #region BuildSlots

        private void CreateBuildSlotHandles(Transform layoutRoot, LevelLayoutRuntimeDefinition layout)
        {
            ClearBuildSlotHandles();

            buildSlotHandlesRoot = new GameObject("BuildSlotHandles");
            buildSlotHandlesRoot.transform.SetParent(layoutRoot, false);

            BuildSlotRuntimeDefinition[] slots = layout.Slots;
            for (int i = 0; i < slots.Length; i++)
            {
                BuildSlotRuntimeDefinition slot = slots[i];
                if (string.IsNullOrWhiteSpace(slot.SlotId))
                {
                    continue;
                }

                GameObject go = new GameObject("Handle_" + slot.SlotId);
                go.transform.SetParent(buildSlotHandlesRoot.transform, false);
                go.transform.position = slot.Position;
                BuildSlotWorldHandle handle = go.AddComponent<BuildSlotWorldHandle>();
                handle.Initialize(slot, buildSlotPlusVisualPrefab);
                buildSlotHandles.Add(handle);
            }
        }

        private void RefreshBuildSlotVisuals()
        {
            for (int i = 0; i < buildSlotHandles.Count; i++)
            {
                BuildSlotWorldHandle handle = buildSlotHandles[i];
                if (handle == null)
                {
                    continue;
                }

                BuildSlotRuntimeDefinition slot = session.FindSlot(handle.SlotId);
                if (string.IsNullOrWhiteSpace(slot.SlotId))
                {
                    continue;
                }

                bool empty = !session.IsSlotOccupied(handle.SlotId);
                int minCost = GetMinimumBuildCostForSlot(slot);
                bool hasBuildOptions = minCost < int.MaxValue;
                bool showPlusForBuild = empty && hasBuildOptions;

                bool occupiedTower = !empty && slot.Kind == BuildSlotKind.Tower;
                TowerRuntimeModel towerOnSlot = occupiedTower ? FindTowerBySlotId(handle.SlotId) : null;

                bool showPlusForUpgrade = false;
                int upgradeCost = int.MaxValue;
                GameBalanceConfig balance = contentService != null ? contentService.BalanceConfig : null;
                if (towerOnSlot != null && balance != null
                    && towerOnSlot.CanApplyDamageUpgrade(balance.TowerDamageUpgradeMaxLevel))
                {
                    showPlusForUpgrade = true;
                    upgradeCost = balance.TowerDamageUpgradeCost;
                }

                bool showPlus = showPlusForBuild || showPlusForUpgrade;
                bool raycastEnabled = (empty && hasBuildOptions) || towerOnSlot != null;

                handle.SetPlusVisible(showPlus);
                handle.SetSlotRaycastEnabled(raycastEnabled);

                if (showPlus)
                {
                    bool affordable = showPlusForBuild && session.CurrentResource >= minCost
                        || showPlusForUpgrade && session.CurrentResource >= upgradeCost;
                    handle.SetAffordableVisual(affordable);
                }
            }
        }

        private int GetMinimumBuildCostForSlot(BuildSlotRuntimeDefinition slot)
        {
            switch (slot.Kind)
            {
                case BuildSlotKind.Auxiliary:
                    return GetMinimumUnlockedAuxiliaryBuildCost(slot.AllowedAuxiliaryBuildings);
                case BuildSlotKind.RoadTrap:
                    return GetMinimumStructureCost(slot.AllowedRoadTraps);
                default:
                    return GetMinimumTowerBuildCost(buildableTowers);
            }
        }

        private int GetMinimumUnlockedAuxiliaryBuildCost(AuxiliaryBuildingDefinition[] defs)
        {
            if (defs == null || defs.Length == 0)
            {
                return int.MaxValue;
            }

            int min = int.MaxValue;
            for (int i = 0; i < defs.Length; i++)
            {
                AuxiliaryBuildingDefinition d = defs[i];
                if (d == null || string.IsNullOrWhiteSpace(d.StructureId))
                {
                    continue;
                }

                if (!IsAuxiliaryUnlockedForBuild(d))
                {
                    continue;
                }

                int c = d.BuildCost;
                if (c < min)
                {
                    min = c;
                }
            }

            return min;
        }

        private bool IsAuxiliaryUnlockedForBuild(AuxiliaryBuildingDefinition def)
        {
            if (def == null)
            {
                return false;
            }

            if (!def.RequiresPlacedTowerToBuild)
            {
                return true;
            }

            return towers.Count > 0;
        }

        private List<AuxiliaryBuildingDefinition> BuildUnlockedAuxiliaryList(AuxiliaryBuildingDefinition[] defs)
        {
            List<AuxiliaryBuildingDefinition> list = new List<AuxiliaryBuildingDefinition>();
            if (defs == null)
            {
                return list;
            }

            for (int i = 0; i < defs.Length; i++)
            {
                AuxiliaryBuildingDefinition d = defs[i];
                if (d == null || string.IsNullOrWhiteSpace(d.StructureId))
                {
                    continue;
                }

                if (IsAuxiliaryUnlockedForBuild(d))
                {
                    list.Add(d);
                }
            }

            return list;
        }

        private static int GetMinimumStructureCost<T>(T[] defs) where T : PlaceableStructureDefinition
        {
            if (defs == null || defs.Length == 0)
            {
                return int.MaxValue;
            }

            int min = int.MaxValue;
            for (int i = 0; i < defs.Length; i++)
            {
                T def = defs[i];
                if (def == null || string.IsNullOrWhiteSpace(def.StructureId))
                {
                    continue;
                }

                int c = def.BuildCost;
                if (c < min)
                {
                    min = c;
                }
            }

            return min;
        }

        private static int GetMinimumTowerBuildCost(IReadOnlyList<TowerDefinition> towers)
        {
            if (towers == null)
            {
                return int.MaxValue;
            }

            int min = int.MaxValue;
            for (int i = 0; i < towers.Count; i++)
            {
                TowerDefinition t = towers[i];
                if (t == null || string.IsNullOrWhiteSpace(t.TowerId))
                {
                    continue;
                }

                int c = t.BuildCost;
                if (c < min)
                {
                    min = c;
                }
            }

            return min;
        }

        private void HandleBuildSlotInput()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
            {
                return;
            }

            if (radialBuildMenu != null && radialBuildMenu.IsOpen)
            {
                return;
            }

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            Camera cam = Camera.main;
            if (cam == null)
            {
                return;
            }

            Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
            if (!Physics.Raycast(ray, out RaycastHit hit, 500f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
            {
                return;
            }

            BuildSlotWorldHandle handle = hit.collider.GetComponentInParent<BuildSlotWorldHandle>();
            if (handle == null)
            {
                return;
            }

            BuildSlotRuntimeDefinition clickedSlot = session.FindSlot(handle.SlotId);
            if (string.IsNullOrWhiteSpace(clickedSlot.SlotId))
            {
                return;
            }

            if (session.IsSlotOccupied(handle.SlotId))
            {
                if (clickedSlot.Kind == BuildSlotKind.Tower)
                {
                    TowerRuntimeModel towerOnSlot = FindTowerBySlotId(handle.SlotId);
                    if (towerOnSlot != null)
                    {
                        Vector3 anchorWorld = handle.transform.position + Vector3.up * 0.45f;
                        Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, anchorWorld);
                        OpenRadialTowerUpgradeMenu(screen, handle.SlotId, towerOnSlot);
                    }
                }

                return;
            }

            Vector3 anchorWorldFree = handle.transform.position + Vector3.up * 0.45f;
            Vector2 screenFree = RectTransformUtility.WorldToScreenPoint(cam, anchorWorldFree);
            OpenRadialBuildMenu(screenFree, handle.SlotId);
        }

        private void OpenRadialBuildMenu(Vector2 screenPosition, string slotId)
        {
            if (session == null)
            {
                return;
            }

            BuildSlotRuntimeDefinition slot = session.FindSlot(slotId);
            if (string.IsNullOrWhiteSpace(slot.SlotId))
            {
                return;
            }

            List<BuildRadialOptionData> options;
            Func<string, string, bool> tryPlace;

            switch (slot.Kind)
            {
                case BuildSlotKind.Tower:
                    options = BuildRadialOptionData.ListFromTowers(buildableTowers);
                    tryPlace = TryPlaceTower;
                    break;

                case BuildSlotKind.Auxiliary:
                    options = BuildRadialOptionData.ListFromAuxiliaries(BuildUnlockedAuxiliaryList(slot.AllowedAuxiliaryBuildings));
                    tryPlace = TryPlacePlaceableStructure;
                    break;

                case BuildSlotKind.RoadTrap:
                    options = BuildRadialOptionData.ListFromRoadTraps(slot.AllowedRoadTraps);
                    tryPlace = TryPlacePlaceableStructure;
                    break;

                default:
                    return;
            }

            if (options == null || options.Count == 0)
            {
                return;
            }

            if (radialBuildMenu == null)
            {
                radialBuildMenu = new BattleTowerRadialMenu();
            }

            radialBuildMenu.Show(
                screenPosition,
                slotId,
                options,
                session.CurrentResource,
                tryPlace,
                UpdateHud,
                towerRadialMenuShellPrefab,
                towerRadialOptionPrefab);
        }

        #region Tower damage upgrade (battle)

        private void OpenRadialTowerUpgradeMenu(Vector2 screenPosition, string slotId, TowerRuntimeModel tower)
        {
            if (session == null || tower == null || contentService == null || contentService.BalanceConfig == null)
            {
                return;
            }

            GameBalanceConfig balance = contentService.BalanceConfig;
            List<BuildRadialOptionData> options = new List<BuildRadialOptionData>(1);
            if (tower.CanApplyDamageUpgrade(balance.TowerDamageUpgradeMaxLevel))
            {
                options.Add(
                    new BuildRadialOptionData(
                        BattleRadialOptionIds.TowerDamageUpgrade,
                        "Upgrade damage",
                        balance.TowerDamageUpgradeCost));
            }
            else
            {
                options.Add(
                    new BuildRadialOptionData(
                        BattleRadialOptionIds.TowerDamageUpgradeMaxed,
                        "Fully upgraded",
                        0,
                        false));
            }

            if (radialBuildMenu == null)
            {
                radialBuildMenu = new BattleTowerRadialMenu();
            }

            radialBuildMenu.Show(
                screenPosition,
                slotId,
                options,
                session.CurrentResource,
                TryTowerSlotUpgradeRadialAction,
                UpdateHud,
                towerRadialMenuShellPrefab,
                towerRadialOptionPrefab);
        }

        private bool TryTowerSlotUpgradeRadialAction(string optionId, string slotId)
        {
            if (!string.Equals(optionId, BattleRadialOptionIds.TowerDamageUpgrade, StringComparison.Ordinal))
            {
                return false;
            }

            TowerRuntimeModel tower = FindTowerBySlotId(slotId);
            if (tower == null)
            {
                return false;
            }

            return TryPurchaseTowerDamageUpgrade(tower);
        }

        private bool TryPurchaseTowerDamageUpgrade(TowerRuntimeModel tower)
        {
            if (!isConfigured || session == null || session.IsFinished || tower == null || contentService == null
                || contentService.BalanceConfig == null)
            {
                return false;
            }

            GameBalanceConfig balance = contentService.BalanceConfig;
            if (!tower.CanApplyDamageUpgrade(balance.TowerDamageUpgradeMaxLevel))
            {
                return false;
            }

            if (session.CurrentResource < balance.TowerDamageUpgradeCost)
            {
                return false;
            }

            session.CurrentResource -= balance.TowerDamageUpgradeCost;
            tower.ApplyDamageUpgrade();
            presentationSystem.Sync(towers, enemies, placedStructures);
            UpdateHud();
            return true;
        }

        private TowerRuntimeModel FindTowerBySlotId(string slotId)
        {
            if (string.IsNullOrWhiteSpace(slotId))
            {
                return null;
            }

            for (int i = 0; i < towers.Count; i++)
            {
                TowerRuntimeModel t = towers[i];
                if (t == null || string.IsNullOrWhiteSpace(t.Slot.SlotId))
                {
                    continue;
                }

                if (string.Equals(t.Slot.SlotId, slotId, StringComparison.Ordinal))
                {
                    return t;
                }
            }

            return null;
        }

        #endregion

        private static AuxiliaryBuildingDefinition FindAuxiliaryOnSlot(BuildSlotRuntimeDefinition slot, string structureId)
        {
            AuxiliaryBuildingDefinition[] arr = slot.AllowedAuxiliaryBuildings;
            if (arr == null)
            {
                return null;
            }

            for (int i = 0; i < arr.Length; i++)
            {
                AuxiliaryBuildingDefinition d = arr[i];
                if (d != null && string.Equals(d.StructureId, structureId, StringComparison.Ordinal))
                {
                    return d;
                }
            }

            return null;
        }

        private static RoadTrapDefinition FindRoadTrapOnSlot(BuildSlotRuntimeDefinition slot, string structureId)
        {
            RoadTrapDefinition[] arr = slot.AllowedRoadTraps;
            if (arr == null)
            {
                return null;
            }

            for (int i = 0; i < arr.Length; i++)
            {
                RoadTrapDefinition d = arr[i];
                if (d != null && string.Equals(d.StructureId, structureId, StringComparison.Ordinal))
                {
                    return d;
                }
            }

            return null;
        }

        private bool IsTowerInBuildableList(TowerDefinition towerDefinition)
        {
            if (buildableTowers == null || buildableTowers.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < buildableTowers.Count; i++)
            {
                if (buildableTowers[i] == towerDefinition)
                {
                    return true;
                }
            }

            return false;
        }

        private void ClearBuildSlotHandles()
        {
            buildSlotHandles.Clear();
            buildSlotHandlesRoot = null;
        }

        private void DisposeRadialBuildMenu()
        {
            if (radialBuildMenu != null)
            {
                radialBuildMenu.Dispose();
                radialBuildMenu = null;
            }
        }

        public void CloseRadialBuildMenuIfOpen()
        {
            DisposeRadialBuildMenu();
        }

        #endregion

        #region AuxiliaryIncome

        private bool ShouldRechargeTowerAmmo()
        {
            if (session == null || session.IsFinished || waveSpawnerSystem == null)
            {
                return false;
            }

            if (waveSpawnerSystem.IsComplete && enemies.Count == 0)
            {
                return false;
            }

            if (waveSpawnerSystem.NeedsPlayerStart && enemies.Count == 0)
            {
                return false;
            }

            return true;
        }

        private void TickAuxiliaryIncome(float deltaTime)
        {
            if (placedStructures.Count == 0 || !ShouldRechargeTowerAmmo())
            {
                return;
            }

            for (int i = 0; i < placedStructures.Count; i++)
            {
                PlacedStructureRuntimeModel model = placedStructures[i];
                if (model == null || model.Kind != BuildSlotKind.Auxiliary)
                {
                    continue;
                }

                AuxiliaryBuildingDefinition aux = model.Definition as AuxiliaryBuildingDefinition;
                if (aux == null || !aux.HasPeriodicIncome)
                {
                    continue;
                }

                float period = aux.IncomePeriodSeconds;
                model.AuxiliaryIncomeElapsed += deltaTime;
                while (model.AuxiliaryIncomeElapsed >= period)
                {
                    model.AuxiliaryIncomeElapsed -= period;
                    session.CurrentResource += aux.IncomeAmount;
                    Vector3 flyOrigin = model.Slot.Position + Vector3.up * 0.55f;
                    coinFlyoutPresenter?.OnPeriodicIncomePayout(flyOrigin, aux.IncomeAmount);
                }
            }
        }

        #endregion

        #region StartWave

        private void HandleEnemyKilledForCoinFlyout(Vector3 worldPosition, int reward)
        {
            coinFlyoutPresenter?.OnEnemyKilled(worldPosition, reward);
        }

        private void RegisterWaveKillForHud()
        {
            waveSpawnerSystem?.RegisterEnemyKill();
        }

        private void HandleEnemyDamagedForFlyout(EnemyRuntimeModel enemy, int damage)
        {
            if (enemy == null)
            {
                return;
            }

            damageFlyoutPresenter?.ShowDamage(enemy.Position, damage);
            presentationSystem?.NotifyEnemyDamaged(enemy);
        }

        private void HandleTowerFiredForPresentation(TowerRuntimeModel tower, EnemyRuntimeModel enemy, float travelTime)
        {
            if (presentationSystem != null)
            {
                presentationSystem.NotifyTowerFired(tower, enemy, travelTime);
            }
        }

        private void TickEnemyCrowdControl(float deltaTime)
        {
            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyRuntimeModel enemy = enemies[i];
                if (enemy != null)
                {
                    enemy.TickCrowdControl(deltaTime);
                }
            }
        }

        private void HandleStartWaveClicked()
        {
            if (!isConfigured || session == null || session.IsFinished || waveSpawnerSystem == null)
            {
                return;
            }

            waveSpawnerSystem.AcknowledgeStartWave();
            UpdateHud();
        }

        #endregion

        #region Cleanup
        private void OnDestroy()
        {
            StopDeferredBattleResultNavigation();

            if (hudView != null)
            {
                hudView.SetStartWaveClickedHandler(null);
            }

            ResetSessionState();
        }

        private void ResetSessionState()
        {
            StopDeferredBattleResultNavigation();

            isConfigured = false;
            smoothedWaveProgress = 0f;
            towers.Clear();
            enemies.Clear();
            placedStructures.Clear();
            session = null;
            activeLevelDefinition = null;
            buildableTowers = null;

            DisposeRadialBuildMenu();
            ClearBuildSlotHandles();

            if (presentationSystem != null)
            {
                presentationSystem.Dispose();
                presentationSystem = null;
            }

            projectileHitScheduler = null;

            if (ownsActiveLayoutInstance && activeLayoutInstance != null)
            {
                Destroy(activeLayoutInstance);
            }

            activeLayoutInstance = null;
            ownsActiveLayoutInstance = false;
        }
        #endregion
    }
}
