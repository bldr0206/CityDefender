using System;
using ColorChargeTD.Data;
using ColorChargeTD.Profile;
using UnityEngine;

namespace ColorChargeTD.Product
{
    internal static class LevelProgressionResolver
    {
        public static string ResolveNextAfterCompletion(
            string completedNormalized,
            PlayerProfileData profile,
            LevelCatalogDefinition catalog,
            ref bool unlockedNextLevel)
        {
            unlockedNextLevel = false;

            if (catalog == null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("[ColorChargeTD] TryUnlockNextLevel: LevelCatalog is null.");
#endif
                return string.Empty;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (catalog.Levels.Count < 2)
            {
                Debug.LogWarning("[ColorChargeTD] Level catalog has fewer than 2 levels; next level after victory may be unresolved.");
            }
#endif

            int completedIndex = -1;
            for (int i = 0; i < catalog.Levels.Count; i++)
            {
                LevelDefinition level = catalog.Levels[i];
                if (level == null)
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.LogWarning("[ColorChargeTD] Level catalog has a null entry at index " + i + ".");
#endif
                    continue;
                }

                if (string.Equals(level.LevelId, completedNormalized, StringComparison.OrdinalIgnoreCase))
                {
                    completedIndex = i;
                    break;
                }
            }

            for (int i = 0; i < catalog.Levels.Count; i++)
            {
                LevelDefinition candidate = catalog.Levels[i];
                if (candidate == null)
                {
                    continue;
                }

                string required = candidate.UnlockRule.RequiredLevelId;
                if (string.IsNullOrWhiteSpace(required))
                {
                    continue;
                }

                if (!string.Equals(required.Trim(), completedNormalized, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return UnlockAndReturn(candidate, profile, ref unlockedNextLevel);
            }

            if (completedIndex >= 0 && completedIndex + 1 < catalog.Levels.Count)
            {
                LevelDefinition nextLinear = catalog.Levels[completedIndex + 1];
                if (nextLinear != null)
                {
                    return UnlockAndReturn(nextLinear, profile, ref unlockedNextLevel);
                }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning(
                    "[ColorChargeTD] Level catalog entry at index " + (completedIndex + 1) + " is null (broken or missing LevelDefinition reference). Fix LevelCatalog asset.");
#endif
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning(
                "[ColorChargeTD] Could not resolve next level after '" + completedNormalized + "' (completedIndex=" + completedIndex + ", catalogCount=" + catalog.Levels.Count + ").");
#endif
            return string.Empty;
        }

        private static string UnlockAndReturn(LevelDefinition candidate, PlayerProfileData profile, ref bool unlockedNextLevel)
        {
            if (!profile.IsUnlocked(candidate.LevelId))
            {
                profile.UnlockLevel(candidate.LevelId);
                unlockedNextLevel = true;
            }

            return candidate.LevelId;
        }
    }
}
