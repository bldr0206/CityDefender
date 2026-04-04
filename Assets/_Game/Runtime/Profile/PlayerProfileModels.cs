using System;
using System.Collections.Generic;
using ColorChargeTD.Data;
using ColorChargeTD.Domain;
using UnityEngine;

namespace ColorChargeTD.Profile
{
    [Serializable]
    public sealed class PlayerProfileData
    {
        [SerializeField] private int softCurrency;
        [SerializeField] private string lastSelectedLevelId = string.Empty;
        [SerializeField] private List<LevelProgressData> levelProgress = new List<LevelProgressData>();
        [SerializeField] private List<UpgradeProgressData> upgradeProgress = new List<UpgradeProgressData>();

        public int SoftCurrency
        {
            get => softCurrency;
            set => softCurrency = Mathf.Max(0, value);
        }

        public string LastSelectedLevelId
        {
            get => lastSelectedLevelId;
            set => lastSelectedLevelId = value ?? string.Empty;
        }

        public IReadOnlyList<LevelProgressData> LevelProgress => levelProgress;
        public IReadOnlyList<UpgradeProgressData> UpgradeProgress => upgradeProgress;

        #region LevelProgress
        public LevelProgressData GetLevelProgress(string levelId)
        {
            for (int i = 0; i < levelProgress.Count; i++)
            {
                LevelProgressData progress = levelProgress[i];
                if (string.Equals(progress.LevelId, levelId, StringComparison.Ordinal))
                {
                    return progress;
                }
            }

            return LevelProgressData.CreateLocked(levelId);
        }

        public void EnsureLevelAvailability(LevelCatalogDefinition levelCatalog)
        {
            if (levelCatalog == null)
            {
                return;
            }

            for (int i = 0; i < levelCatalog.Levels.Count; i++)
            {
                LevelDefinition level = levelCatalog.Levels[i];
                if (level == null)
                {
                    continue;
                }

                if (HasLevel(level.LevelId))
                {
                    continue;
                }

                LevelCompletionState state = level.UnlockRule.UnlockedByDefault
                    ? LevelCompletionState.Unlocked
                    : LevelCompletionState.Locked;

                levelProgress.Add(new LevelProgressData(level.LevelId, state, 0, false));
            }
        }

        public bool HasLevel(string levelId)
        {
            for (int i = 0; i < levelProgress.Count; i++)
            {
                if (string.Equals(levelProgress[i].LevelId, levelId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsUnlocked(string levelId)
        {
            return GetLevelProgress(levelId).CompletionState != LevelCompletionState.Locked;
        }

        public void UnlockLevel(string levelId)
        {
            for (int i = 0; i < levelProgress.Count; i++)
            {
                if (!string.Equals(levelProgress[i].LevelId, levelId, StringComparison.Ordinal))
                {
                    continue;
                }

                LevelProgressData progress = levelProgress[i];
                if (progress.CompletionState == LevelCompletionState.Locked)
                {
                    progress.SetCompletionState(LevelCompletionState.Unlocked);
                    levelProgress[i] = progress;
                }

                return;
            }

            levelProgress.Add(new LevelProgressData(levelId, LevelCompletionState.Unlocked, 0, false));
        }

        public bool ApplyLevelResult(string levelId, BattleOutcome outcome, int awardedCurrency)
        {
            bool firstCompletion = false;

            for (int i = 0; i < levelProgress.Count; i++)
            {
                if (!string.Equals(levelProgress[i].LevelId, levelId, StringComparison.Ordinal))
                {
                    continue;
                }

                LevelProgressData progress = levelProgress[i];
                if (outcome == BattleOutcome.Victory)
                {
                    firstCompletion = !progress.HasCompletedOnce;
                    progress.MarkCompleted();
                }

                levelProgress[i] = progress;
                SoftCurrency += awardedCurrency;
                return firstCompletion;
            }

            LevelProgressData newProgress = new LevelProgressData(levelId, LevelCompletionState.Unlocked, 0, false);
            if (outcome == BattleOutcome.Victory)
            {
                firstCompletion = true;
                newProgress.MarkCompleted();
            }

            levelProgress.Add(newProgress);
            SoftCurrency += awardedCurrency;
            return firstCompletion;
        }
        #endregion

        #region UpgradeProgress
        public int GetUpgradeLevel(string upgradeId)
        {
            for (int i = 0; i < upgradeProgress.Count; i++)
            {
                if (string.Equals(upgradeProgress[i].UpgradeId, upgradeId, StringComparison.Ordinal))
                {
                    return upgradeProgress[i].Level;
                }
            }

            return 0;
        }

        public void SetUpgradeLevel(string upgradeId, int level)
        {
            for (int i = 0; i < upgradeProgress.Count; i++)
            {
                if (!string.Equals(upgradeProgress[i].UpgradeId, upgradeId, StringComparison.Ordinal))
                {
                    continue;
                }

                UpgradeProgressData progress = upgradeProgress[i];
                progress.Level = Mathf.Max(0, level);
                upgradeProgress[i] = progress;
                return;
            }

            upgradeProgress.Add(new UpgradeProgressData(upgradeId, Mathf.Max(0, level)));
        }
        #endregion
    }

    [Serializable]
    public struct LevelProgressData
    {
        [SerializeField] private string levelId;
        [SerializeField] private LevelCompletionState completionState;
        [SerializeField] private int bestStars;
        [SerializeField] private bool hasCompletedOnce;

        public string LevelId => levelId;
        public LevelCompletionState CompletionState => completionState;
        public int BestStars => bestStars;
        public bool HasCompletedOnce => hasCompletedOnce;

        public LevelProgressData(string levelId, LevelCompletionState completionState, int bestStars, bool hasCompletedOnce)
        {
            this.levelId = levelId ?? string.Empty;
            this.completionState = completionState;
            this.bestStars = Mathf.Clamp(bestStars, 0, 3);
            this.hasCompletedOnce = hasCompletedOnce;
        }

        public static LevelProgressData CreateLocked(string levelId)
        {
            return new LevelProgressData(levelId, LevelCompletionState.Locked, 0, false);
        }

        public void SetCompletionState(LevelCompletionState state)
        {
            completionState = state;
        }

        public void MarkCompleted(int stars = 1)
        {
            completionState = LevelCompletionState.Completed;
            hasCompletedOnce = true;
            bestStars = Mathf.Max(bestStars, Mathf.Clamp(stars, 0, 3));
        }
    }

    [Serializable]
    public struct UpgradeProgressData
    {
        [SerializeField] private string upgradeId;
        [SerializeField] private int level;

        public string UpgradeId => upgradeId;

        public int Level
        {
            get => level;
            set => level = Mathf.Max(0, value);
        }

        public UpgradeProgressData(string upgradeId, int level)
        {
            this.upgradeId = upgradeId ?? string.Empty;
            this.level = Mathf.Max(0, level);
        }
    }

    [Serializable]
    public struct LevelCardViewModel
    {
        public string LevelId;
        public string DisplayName;
        public string TutorialFocus;
        public bool IsUnlocked;
        public bool IsSelected;
        public int BestStars;
    }

    [Serializable]
    public struct BattleResultModel
    {
        public string LevelId;
        public BattleOutcome Outcome;
        public int AwardedSoftCurrency;
        public bool UnlockedNextLevel;
        public string NextLevelId;
    }
}
