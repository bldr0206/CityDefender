using System;
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

        private readonly List<TowerRuntimeModel> towers = new List<TowerRuntimeModel>();
        private readonly List<EnemyRuntimeModel> enemies = new List<EnemyRuntimeModel>();

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
        private GameObject activeLayoutInstance;
        private bool ownsActiveLayoutInstance;
        private bool isConfigured;

        private readonly List<BuildSlotWorldHandle> buildSlotHandles = new List<BuildSlotWorldHandle>();
        private GameObject buildSlotHandlesRoot;
        private BattleTowerRadialMenu radialBuildMenu;
        private float smoothedWaveProgress;

        [Inject] private IGameContentService contentService;
        [Inject] private ILevelSelectionService levelSelectionService;
        [Inject] private IProgressionService progressionService;
        [Inject] private IGameNavigationService navigationService;
        [Inject] private IPlayerProfileService profileService;

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
            if (!isConfigured || session == null || session.IsFinished)
            {
                return;
            }

            if (Time.timeScale <= 0f)
            {
                return;
            }

            HandleBuildSlotInput();

            float deltaTime = Time.deltaTime;
            waveSpawnerSystem.Tick(deltaTime, enemies);
            enemyPathSystem.Tick(deltaTime, enemies, session);
            towerChargeSystem.Tick(deltaTime, towers);
            projectileHitScheduler.Tick(deltaTime);
            towerTargetingSystem.Tick(deltaTime, towers, enemies, damageResolver, projectileHitScheduler, HandleTowerFiredForPresentation);
            presentationSystem.Sync(towers, enemies);

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
            presentationSystem.Sync(towers, enemies);
            CreateBuildSlotHandles(layoutAuthoring.transform, layoutDefinition);
            isConfigured = true;

            UpdateHud();
        }

        public bool TryPlaceTower(string towerId, string slotId)
        {
            if (!isConfigured || session == null || session.IsFinished)
            {
                return false;
            }

            TowerDefinition towerDefinition = contentService.GetTowerById(towerId);
            if (towerDefinition == null)
            {
                return false;
            }

            BuildSlotRuntimeDefinition slot = session.FindSlot(slotId);
            if (string.IsNullOrWhiteSpace(slot.SlotId) || session.IsSlotOccupied(slot.SlotId))
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
            presentationSystem.Sync(towers, enemies);
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
            navigationService.OpenBattleResult(result);
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

            int totalSlots = session.TotalBuildSlotCount;
            int freeSlots = totalSlots - session.OccupiedBuildSlotCount;
            hudView.SetSlots(freeSlots, totalSlots);

            bool waitingForWaveStart = waveSpawnerSystem != null && waveSpawnerSystem.NeedsPlayerStart;
            bool hasTowers = towers.Count > 0;

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
                handle.Initialize(slot);
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

                bool empty = !session.IsSlotOccupied(handle.SlotId);
                handle.SetBuildableVisible(empty);
            }
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

            if (session.IsSlotOccupied(handle.SlotId))
            {
                return;
            }

            Vector3 anchorWorld = handle.transform.position + Vector3.up * 0.45f;
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, anchorWorld);
            OpenRadialBuildMenu(screen, handle.SlotId);
        }

        private void OpenRadialBuildMenu(Vector2 screenPosition, string slotId)
        {
            if (radialBuildMenu == null)
            {
                radialBuildMenu = new BattleTowerRadialMenu();
            }

            radialBuildMenu.Show(
                screenPosition,
                slotId,
                contentService.Towers,
                session.CurrentResource,
                TryPlaceTower,
                UpdateHud);
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
        }

        private void HandleTowerFiredForPresentation(TowerRuntimeModel tower, EnemyRuntimeModel enemy)
        {
            if (presentationSystem != null)
            {
                presentationSystem.NotifyTowerFired(tower, enemy);
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
            if (hudView != null)
            {
                hudView.SetStartWaveClickedHandler(null);
            }

            ResetSessionState();
        }

        private void ResetSessionState()
        {
            isConfigured = false;
            smoothedWaveProgress = 0f;
            towers.Clear();
            enemies.Clear();
            session = null;
            activeLevelDefinition = null;

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
