using System.Collections.Generic;
using ColorChargeTD.Domain;
using UnityEngine;

namespace ColorChargeTD.Data
{
    [CreateAssetMenu(menuName = "Color Charge TD/Content/Game Content", fileName = "GameContentConfig")]
    public sealed class GameContentConfig : ScriptableObject
    {
        [SerializeField] private GameBalanceConfig balanceConfig;
        [SerializeField] private LevelCatalogDefinition levelCatalog;
        [SerializeField] private List<TowerDefinition> towers = new List<TowerDefinition>();
        [SerializeField] private List<EnemyDefinition> enemies = new List<EnemyDefinition>();
        [SerializeField] private List<UpgradeDefinition> upgrades = new List<UpgradeDefinition>();

        public GameBalanceConfig BalanceConfig => balanceConfig;
        public LevelCatalogDefinition LevelCatalog => levelCatalog;
        public IReadOnlyList<TowerDefinition> Towers => towers;
        public IReadOnlyList<EnemyDefinition> Enemies => enemies;
        public IReadOnlyList<UpgradeDefinition> Upgrades => upgrades;

        public void ValidateInto(List<ContentValidationMessage> messages)
        {
            if (balanceConfig == null)
            {
                messages.Add(ContentValidationMessage.Error(name, "Balance config reference is missing."));
            }

            if (levelCatalog == null)
            {
                messages.Add(ContentValidationMessage.Error(name, "Level catalog reference is missing."));
            }
            else
            {
                levelCatalog.ValidateInto(messages);
            }

            ValidateCollection(towers, "tower", messages);
            ValidateCollection(enemies, "enemy", messages);
            ValidateCollection(upgrades, "upgrade", messages);

            for (int i = 0; i < towers.Count; i++)
            {
                if (towers[i] != null)
                {
                    towers[i].ValidateInto(messages);
                }
            }

            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] != null)
                {
                    enemies[i].ValidateInto(messages);
                }
            }

            for (int i = 0; i < upgrades.Count; i++)
            {
                if (upgrades[i] != null)
                {
                    upgrades[i].ValidateInto(messages);
                }
            }
        }

        private void ValidateCollection<TAsset>(List<TAsset> assets, string label, List<ContentValidationMessage> messages)
            where TAsset : UnityEngine.Object
        {
            if (assets == null || assets.Count == 0)
            {
                messages.Add(ContentValidationMessage.Warning(name, "Game content does not include any " + label + " definitions."));
                return;
            }

            for (int i = 0; i < assets.Count; i++)
            {
                if (assets[i] == null)
                {
                    messages.Add(ContentValidationMessage.Error(name, "Game content contains a null " + label + " reference."));
                }
            }
        }
    }
}
