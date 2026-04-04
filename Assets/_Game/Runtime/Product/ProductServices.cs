using System;
using System.Collections.Generic;
using ColorChargeTD.Core;
using ColorChargeTD.Data;
using ColorChargeTD.Domain;
using ColorChargeTD.Profile;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ColorChargeTD.Product
{
    public interface ISaveService
    {
        void Save<T>(string key, T data);
        bool TryLoad<T>(string key, out T data) where T : new();
    }

    public interface IGameContentService
    {
        GameContentConfig Content { get; }
        GameBalanceConfig BalanceConfig { get; }
        LevelCatalogDefinition LevelCatalog { get; }
        IReadOnlyList<UpgradeDefinition> Upgrades { get; }
        IReadOnlyList<TowerDefinition> Towers { get; }
        TowerDefinition GetTowerById(string towerId);
        UpgradeDefinition GetUpgradeById(string upgradeId);
    }

    public interface IPlayerProfileService
    {
        PlayerProfileData CurrentProfile { get; }
        event Action<PlayerProfileData> ProfileChanged;
        void Load();
        void Save();
        void Update(Action<PlayerProfileData> mutation);
    }

    public interface IProgressionService
    {
        IReadOnlyList<LevelCardViewModel> BuildLevelCards();
        bool IsLevelUnlocked(string levelId);
        bool TryPurchaseUpgrade(UpgradeDefinition upgradeDefinition);
        BattleResultModel ApplyBattleResult(LevelDefinition levelDefinition, BattleOutcome outcome, int awardedCurrency);
    }

    public interface IGameNavigationService
    {
        BattleResultModel LastBattleResult { get; }
        void Initialize(MonoBehaviour runner);
        void OpenMainMenu();
        void OpenLevelSelect();
        void OpenMeta();
        void StartSelectedLevel();
        void OpenBattleResult(BattleResultModel resultModel);
    }

    public sealed class PlayerPrefsJsonSaveService : ISaveService
    {
        public void Save<T>(string key, T data)
        {
            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(key, json);
            PlayerPrefs.Save();
        }

        public bool TryLoad<T>(string key, out T data) where T : new()
        {
            if (!PlayerPrefs.HasKey(key))
            {
                data = new T();
                return false;
            }

            string json = PlayerPrefs.GetString(key);
            if (string.IsNullOrWhiteSpace(json))
            {
                data = new T();
                return false;
            }

            data = JsonUtility.FromJson<T>(json);
            if (data == null)
            {
                data = new T();
                return false;
            }

            return true;
        }
    }

    public sealed class GameContentService : IGameContentService
    {
        private readonly Dictionary<string, TowerDefinition> towersById = new Dictionary<string, TowerDefinition>(StringComparer.Ordinal);
        private readonly Dictionary<string, UpgradeDefinition> upgradesById = new Dictionary<string, UpgradeDefinition>(StringComparer.Ordinal);

        public GameContentService(GameContentConfig content)
        {
            Content = content;

            if (content != null)
            {
                CacheTowerDefinitions(content.Towers);
                CacheUpgradeDefinitions(content.Upgrades);
            }
        }

        public GameContentConfig Content { get; }

        public GameBalanceConfig BalanceConfig => Content != null ? Content.BalanceConfig : null;

        public LevelCatalogDefinition LevelCatalog => Content != null ? Content.LevelCatalog : null;

        public IReadOnlyList<UpgradeDefinition> Upgrades => Content != null ? Content.Upgrades : Array.Empty<UpgradeDefinition>();

        public IReadOnlyList<TowerDefinition> Towers => Content != null ? Content.Towers : Array.Empty<TowerDefinition>();

        public TowerDefinition GetTowerById(string towerId)
        {
            towersById.TryGetValue(towerId ?? string.Empty, out TowerDefinition tower);
            return tower;
        }

        public UpgradeDefinition GetUpgradeById(string upgradeId)
        {
            upgradesById.TryGetValue(upgradeId ?? string.Empty, out UpgradeDefinition upgrade);
            return upgrade;
        }

        private void CacheTowerDefinitions(IReadOnlyList<TowerDefinition> towers)
        {
            for (int i = 0; i < towers.Count; i++)
            {
                TowerDefinition tower = towers[i];
                if (tower == null || string.IsNullOrWhiteSpace(tower.TowerId))
                {
                    continue;
                }

                towersById[tower.TowerId] = tower;
            }
        }

        private void CacheUpgradeDefinitions(IReadOnlyList<UpgradeDefinition> upgrades)
        {
            for (int i = 0; i < upgrades.Count; i++)
            {
                UpgradeDefinition upgrade = upgrades[i];
                if (upgrade == null || string.IsNullOrWhiteSpace(upgrade.UpgradeId))
                {
                    continue;
                }

                upgradesById[upgrade.UpgradeId] = upgrade;
            }
        }
    }

    public sealed class PlayerProfileService : IPlayerProfileService
    {
        private const string SaveKey = "color-charge-td-profile";
        private readonly ISaveService saveService;
        private readonly IGameContentService contentService;

        public PlayerProfileService(ISaveService saveService, IGameContentService contentService)
        {
            this.saveService = saveService;
            this.contentService = contentService;
            CurrentProfile = new PlayerProfileData();
        }

        public PlayerProfileData CurrentProfile { get; private set; }

        public event Action<PlayerProfileData> ProfileChanged;

        public void Load()
        {
            if (!saveService.TryLoad(SaveKey, out PlayerProfileData loadedProfile))
            {
                loadedProfile = new PlayerProfileData();
            }

            loadedProfile.EnsureLevelAvailability(contentService.LevelCatalog);
            CurrentProfile = loadedProfile;
            Save();
            ProfileChanged?.Invoke(CurrentProfile);
        }

        public void Save()
        {
            saveService.Save(SaveKey, CurrentProfile);
        }

        public void Update(Action<PlayerProfileData> mutation)
        {
            mutation?.Invoke(CurrentProfile);
            Save();
            ProfileChanged?.Invoke(CurrentProfile);
        }
    }

    public sealed class ProgressionService : IProgressionService
    {
        private readonly IGameContentService contentService;
        private readonly IPlayerProfileService profileService;
        private readonly ILevelSelectionService levelSelectionService;

        public ProgressionService(
            IGameContentService contentService,
            IPlayerProfileService profileService,
            ILevelSelectionService levelSelectionService)
        {
            this.contentService = contentService;
            this.profileService = profileService;
            this.levelSelectionService = levelSelectionService;
        }

        public IReadOnlyList<LevelCardViewModel> BuildLevelCards()
        {
            List<LevelCardViewModel> cards = new List<LevelCardViewModel>();
            LevelCatalogDefinition levelCatalog = contentService.LevelCatalog;
            if (levelCatalog == null)
            {
                return cards;
            }

            for (int i = 0; i < levelCatalog.Levels.Count; i++)
            {
                LevelDefinition level = levelCatalog.Levels[i];
                if (level == null)
                {
                    continue;
                }

                LevelProgressData progress = profileService.CurrentProfile.GetLevelProgress(level.LevelId);
                cards.Add(new LevelCardViewModel
                {
                    LevelId = level.LevelId,
                    DisplayName = level.DisplayName,
                    TutorialFocus = level.TutorialFocus,
                    IsUnlocked = progress.CompletionState != LevelCompletionState.Locked,
                    IsSelected = string.Equals(levelSelectionService.SelectedLevelId, level.LevelId, StringComparison.Ordinal),
                    BestStars = progress.BestStars,
                });
            }

            return cards;
        }

        public bool IsLevelUnlocked(string levelId)
        {
            return profileService.CurrentProfile.IsUnlocked(levelId);
        }

        public bool TryPurchaseUpgrade(UpgradeDefinition upgradeDefinition)
        {
            if (upgradeDefinition == null)
            {
                return false;
            }

            PlayerProfileData profile = profileService.CurrentProfile;
            int currentLevel = profile.GetUpgradeLevel(upgradeDefinition.UpgradeId);
            if (currentLevel >= upgradeDefinition.MaxLevel)
            {
                return false;
            }

            int cost = upgradeDefinition.GetCostForLevel(currentLevel);
            if (profile.SoftCurrency < cost)
            {
                return false;
            }

            profileService.Update(current =>
            {
                current.SoftCurrency -= cost;
                current.SetUpgradeLevel(upgradeDefinition.UpgradeId, currentLevel + 1);
            });

            return true;
        }

        public BattleResultModel ApplyBattleResult(LevelDefinition levelDefinition, BattleOutcome outcome, int awardedCurrency)
        {
            BattleResultModel result = new BattleResultModel
            {
                LevelId = levelDefinition != null ? levelDefinition.LevelId : string.Empty,
                Outcome = outcome,
                AwardedSoftCurrency = awardedCurrency,
                UnlockedNextLevel = false,
                NextLevelId = string.Empty,
            };

            if (levelDefinition == null)
            {
                return result;
            }

            profileService.Update(profile =>
            {
                bool firstCompletion = profile.ApplyLevelResult(levelDefinition.LevelId, outcome, awardedCurrency);
                profile.LastSelectedLevelId = levelDefinition.LevelId;

                if (outcome != BattleOutcome.Victory)
                {
                    return;
                }

                string nextLevelId = TryUnlockNextLevel(levelDefinition.LevelId, profile, out bool unlockedNextLevel);
                result.NextLevelId = nextLevelId;
                result.UnlockedNextLevel = unlockedNextLevel;

                if (firstCompletion)
                {
                    profile.SoftCurrency += levelDefinition.Reward.FirstCompletionBonus;
                    result.AwardedSoftCurrency += levelDefinition.Reward.FirstCompletionBonus;
                }
            });

            return result;
        }

        private string TryUnlockNextLevel(string completedLevelId, PlayerProfileData profile, out bool unlockedNextLevel)
        {
            unlockedNextLevel = false;

            LevelCatalogDefinition catalog = contentService.LevelCatalog;
            if (catalog == null)
            {
                return string.Empty;
            }

            for (int i = 0; i < catalog.Levels.Count; i++)
            {
                LevelDefinition candidate = catalog.Levels[i];
                if (candidate == null)
                {
                    continue;
                }

                if (!string.Equals(candidate.UnlockRule.RequiredLevelId, completedLevelId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!profile.IsUnlocked(candidate.LevelId))
                {
                    profile.UnlockLevel(candidate.LevelId);
                    unlockedNextLevel = true;
                }

                return candidate.LevelId;
            }

            return string.Empty;
        }
    }

    public sealed class GameNavigationService : IGameNavigationService
    {
        private readonly ISceneLoader sceneLoader;
        private readonly IGameStateMachine stateMachine;
        private readonly ILevelSelectionService levelSelectionService;

        private MonoBehaviour runner;

        public GameNavigationService(
            ISceneLoader sceneLoader,
            IGameStateMachine stateMachine,
            ILevelSelectionService levelSelectionService)
        {
            this.sceneLoader = sceneLoader;
            this.stateMachine = stateMachine;
            this.levelSelectionService = levelSelectionService;
        }

        public BattleResultModel LastBattleResult { get; private set; }

        public void Initialize(MonoBehaviour runner)
        {
            this.runner = runner;
        }

        public void OpenMainMenu()
        {
            stateMachine.Enter(GameFlowState.Menu);
            LoadScene(GameSceneIds.MainMenu);
        }

        public void OpenLevelSelect()
        {
            stateMachine.Enter(GameFlowState.Menu);
            LoadScene(GameSceneIds.MainMenu);
        }

        public void OpenMeta()
        {
            stateMachine.Enter(GameFlowState.Meta);
            LoadScene(GameSceneIds.Meta);
        }

        public void StartSelectedLevel()
        {
            if (string.IsNullOrWhiteSpace(levelSelectionService.SelectedLevelId))
            {
                Debug.LogWarning("Cannot start battle without a selected level.");
                return;
            }

            stateMachine.Enter(GameFlowState.BattleLoading);
            LoadScene(GameSceneIds.Battle, () => stateMachine.Enter(GameFlowState.Battle));
        }

        public void OpenBattleResult(BattleResultModel resultModel)
        {
            LastBattleResult = resultModel;
            stateMachine.Enter(GameFlowState.Result);
            LoadScene(GameSceneIds.Meta);
        }

        private void LoadScene(string sceneName, Action onLoaded = null)
        {
            if (runner == null)
            {
                Debug.LogWarning("Navigation runner is not initialized.");
                return;
            }

            sceneLoader.LoadScene(runner, sceneName, LoadSceneMode.Single, onLoaded);
        }
    }
}
