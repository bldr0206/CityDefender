using System.Collections.Generic;
using UnityEngine;

namespace ColorChargeTD.Data
{
    [CreateAssetMenu(menuName = "Color Charge TD/Content/Level Definition", fileName = "LevelDefinition")]
    public sealed class LevelDefinition : ScriptableObject
    {
        #region Identity
        [Header("Identity")]
        [SerializeField] private string levelId = "level-01";
        [SerializeField] private string displayName = "Level 1";
        [SerializeField] [TextArea] private string tutorialFocus = "Teach one timing lesson per level.";
        #endregion

        #region Content
        [Header("Content")]
        [SerializeField] private GameObject layoutPrefab;
        [SerializeField] private WaveDefinition waveSet;
        [SerializeField] private LevelUnlockRule unlockRule = LevelUnlockRule.CreateFirstLevel();
        [SerializeField] private LevelRewardDefinition reward = LevelRewardDefinition.Default;
        #endregion

        #region Build
        [Header("Build")]
        [SerializeField] private bool useTowerBuildAllowlist;
        [SerializeField] private List<TowerDefinition> allowedTowers = new List<TowerDefinition>();
        #endregion

        #region Session
        [Header("Session")]
        [SerializeField] private int startingResourceOverride = -1;
        #endregion

        public string LevelId => levelId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? levelId : displayName;
        public string TutorialFocus => tutorialFocus;
        public GameObject LayoutPrefab => layoutPrefab;
        public WaveDefinition WaveSet => waveSet;
        public LevelUnlockRule UnlockRule => unlockRule;
        public LevelRewardDefinition Reward => reward;

        public int ResolveStartingResource(GameBalanceConfig balanceConfig)
        {
            if (startingResourceOverride >= 0)
            {
                return startingResourceOverride;
            }

            return balanceConfig != null ? balanceConfig.DefaultStartingResource : 0;
        }

        public IReadOnlyList<TowerDefinition> ResolveBuildableTowers(IReadOnlyList<TowerDefinition> catalog)
        {
            if (catalog == null || catalog.Count == 0)
            {
                return catalog;
            }

            if (!useTowerBuildAllowlist || allowedTowers == null || allowedTowers.Count == 0)
            {
                return catalog;
            }

            List<TowerDefinition> resolved = new List<TowerDefinition>();
            for (int i = 0; i < allowedTowers.Count; i++)
            {
                TowerDefinition tower = allowedTowers[i];
                if (tower != null)
                {
                    resolved.Add(tower);
                }
            }

            return resolved.Count > 0 ? resolved : catalog;
        }

        public void ValidateInto(List<ContentValidationMessage> messages)
        {
            if (string.IsNullOrWhiteSpace(levelId))
            {
                messages.Add(ContentValidationMessage.Error(name, "LevelId is required."));
            }

            if (layoutPrefab == null)
            {
                messages.Add(ContentValidationMessage.Error(name, "Layout prefab reference is missing."));
            }

            if (waveSet == null)
            {
                messages.Add(ContentValidationMessage.Error(name, "Wave definition reference is missing."));
            }

            if (reward.SoftCurrency < 0)
            {
                messages.Add(ContentValidationMessage.Error(name, "Reward cannot be negative."));
            }

            if (useTowerBuildAllowlist)
            {
                bool hasTower = false;
                if (allowedTowers != null)
                {
                    for (int i = 0; i < allowedTowers.Count; i++)
                    {
                        if (allowedTowers[i] != null)
                        {
                            hasTower = true;
                            break;
                        }
                    }
                }

                if (!hasTower)
                {
                    messages.Add(ContentValidationMessage.Error(name, "Tower build allowlist is enabled but no towers are assigned."));
                }
            }
        }
    }
}
