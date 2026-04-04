using System;
using System.Collections.Generic;
using UnityEngine;

namespace ColorChargeTD.Data
{
    [CreateAssetMenu(menuName = "Color Charge TD/Content/Level Catalog", fileName = "LevelCatalog")]
    public sealed class LevelCatalogDefinition : ScriptableObject
    {
        [SerializeField] private List<LevelDefinition> levels = new List<LevelDefinition>();

        public IReadOnlyList<LevelDefinition> Levels => levels;

        public LevelDefinition GetLevelById(string levelId)
        {
            if (string.IsNullOrWhiteSpace(levelId))
            {
                return null;
            }

            string needle = levelId.Trim();
            for (int i = 0; i < levels.Count; i++)
            {
                LevelDefinition level = levels[i];
                if (level != null && string.Equals(level.LevelId, needle, StringComparison.OrdinalIgnoreCase))
                {
                    return level;
                }
            }

            return null;
        }

        public void ValidateInto(List<ContentValidationMessage> messages)
        {
            if (levels.Count < 2)
            {
                messages.Add(ContentValidationMessage.Warning(name, "Level catalog has fewer than 2 levels (MVP expects a linear sequence)."));
            }

            HashSet<string> uniqueLevelIds = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < levels.Count; i++)
            {
                LevelDefinition level = levels[i];
                if (level == null)
                {
                    messages.Add(ContentValidationMessage.Error(name, "Catalog contains a null level reference."));
                    continue;
                }

                level.ValidateInto(messages);

                if (!uniqueLevelIds.Add(level.LevelId))
                {
                    messages.Add(ContentValidationMessage.Error(level.name, "LevelId must be unique inside the catalog."));
                }
            }
        }
    }
}
