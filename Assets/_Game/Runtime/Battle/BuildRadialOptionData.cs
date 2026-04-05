using System.Collections.Generic;
using ColorChargeTD.Data;

namespace ColorChargeTD.Battle
{
    public static class BattleRadialOptionIds
    {
        public const string TowerDamageUpgrade = "tower-damage-upgrade";
        public const string TowerDamageUpgradeMaxed = "tower-damage-upgrade-maxed";
    }

    public readonly struct BuildRadialOptionData
    {
        public readonly string OptionId;
        public readonly string DisplayName;
        public readonly int BuildCost;
        public readonly bool CanPurchase;

        public BuildRadialOptionData(string optionId, string displayName, int buildCost, bool canPurchase = true)
        {
            OptionId = optionId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            BuildCost = buildCost;
            CanPurchase = canPurchase;
        }

        public static BuildRadialOptionData FromTower(TowerDefinition tower)
        {
            if (tower == null)
            {
                return default;
            }

            return new BuildRadialOptionData(tower.TowerId, tower.DisplayName, tower.BuildCost);
        }

        public static List<BuildRadialOptionData> ListFromTowers(IReadOnlyList<TowerDefinition> towers)
        {
            List<BuildRadialOptionData> list = new List<BuildRadialOptionData>();
            if (towers == null)
            {
                return list;
            }

            for (int i = 0; i < towers.Count; i++)
            {
                TowerDefinition t = towers[i];
                if (t != null && !string.IsNullOrWhiteSpace(t.TowerId))
                {
                    list.Add(FromTower(t));
                }
            }

            return list;
        }

        public static List<BuildRadialOptionData> ListFromAuxiliaries(IList<AuxiliaryBuildingDefinition> defs)
        {
            List<BuildRadialOptionData> list = new List<BuildRadialOptionData>();
            if (defs == null)
            {
                return list;
            }

            int n = defs.Count;
            for (int i = 0; i < n; i++)
            {
                AuxiliaryBuildingDefinition d = defs[i];
                if (d != null && !string.IsNullOrWhiteSpace(d.StructureId))
                {
                    list.Add(new BuildRadialOptionData(d.StructureId, d.DisplayName, d.BuildCost));
                }
            }

            return list;
        }

        public static List<BuildRadialOptionData> ListFromRoadTraps(RoadTrapDefinition[] defs)
        {
            List<BuildRadialOptionData> list = new List<BuildRadialOptionData>();
            if (defs == null)
            {
                return list;
            }

            for (int i = 0; i < defs.Length; i++)
            {
                RoadTrapDefinition d = defs[i];
                if (d != null && !string.IsNullOrWhiteSpace(d.StructureId))
                {
                    list.Add(new BuildRadialOptionData(d.StructureId, d.DisplayName, d.BuildCost));
                }
            }

            return list;
        }
    }
}
