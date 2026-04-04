using ColorChargeTD.Domain;
using UnityEngine;

namespace ColorChargeTD.Data
{
    [CreateAssetMenu(menuName = "Color Charge TD/Content/Upgrade Definition", fileName = "UpgradeDefinition")]
    public sealed class UpgradeDefinition : ScriptableObject
    {
        [SerializeField] private string upgradeId = "upgrade-start-resource";
        [SerializeField] private string displayName = "Start Resource";
        [SerializeField] [TextArea] private string description = "Adds a small amount of starting currency.";
        [SerializeField] private UpgradeEffectType effectType = UpgradeEffectType.StartingResourceBonus;
        [SerializeField] private int maxLevel = 1;
        [SerializeField] private int[] levelCosts = new int[] { 25 };
        [SerializeField] private float[] levelValues = new float[] { 25f };

        public string UpgradeId => upgradeId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? upgradeId : displayName;
        public string Description => description;
        public UpgradeEffectType EffectType => effectType;
        public int MaxLevel => Mathf.Max(1, maxLevel);

        public int GetCostForLevel(int level)
        {
            return GetValueFromArray(levelCosts, level, 0);
        }

        public float GetValueForLevel(int level)
        {
            return GetValueFromArray(levelValues, level, 0f);
        }

        public void ValidateInto(System.Collections.Generic.List<ContentValidationMessage> messages)
        {
            if (string.IsNullOrWhiteSpace(upgradeId))
            {
                messages.Add(ContentValidationMessage.Error(name, "UpgradeId is required."));
            }

            if (levelCosts == null || levelCosts.Length == 0)
            {
                messages.Add(ContentValidationMessage.Error(name, "At least one level cost is required."));
            }

            if (levelValues == null || levelValues.Length == 0)
            {
                messages.Add(ContentValidationMessage.Error(name, "At least one level value is required."));
            }
        }

        private static T GetValueFromArray<T>(T[] values, int level, T fallback)
        {
            if (values == null || values.Length == 0)
            {
                return fallback;
            }

            int index = Mathf.Clamp(level, 0, values.Length - 1);
            return values[index];
        }
    }
}
