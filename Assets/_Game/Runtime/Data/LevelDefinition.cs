using UnityEngine;

namespace ColorChargeTD.Data
{
    [CreateAssetMenu(menuName = "Color Charge TD/Content/Level Definition", fileName = "LevelDefinition")]
    public sealed class LevelDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string levelId = "level-01";
        [SerializeField] private string displayName = "Level 1";
        [SerializeField] [TextArea] private string tutorialFocus = "Teach one timing lesson per level.";

        [Header("Content")]
        [SerializeField] private GameObject layoutPrefab;
        [SerializeField] private WaveDefinition waveSet;
        [SerializeField] private LevelUnlockRule unlockRule = LevelUnlockRule.CreateFirstLevel();
        [SerializeField] private LevelRewardDefinition reward = LevelRewardDefinition.Default;

        [Header("Session")]
        [SerializeField] private int startingResourceOverride = -1;

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

        public void ValidateInto(System.Collections.Generic.List<ContentValidationMessage> messages)
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
        }
    }
}
