using ColorChargeTD.Core;
using ColorChargeTD.Data;
using ColorChargeTD.Domain;
using ColorChargeTD.Product;
using ColorChargeTD.Profile;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace ColorChargeTD.Presentation
{
    public sealed class NavigationCommandRouter : MonoBehaviour
    {
        [Inject] private IGameNavigationService navigationService;
        [Inject] private ILevelSelectionService levelSelectionService;
        [Inject] private IGameContentService contentService;
        [Inject] private IProgressionService progressionService;

        #region NavigationCommands

        public void OpenMainMenu()
        {
            navigationService.OpenMainMenu();
        }

        public void OpenLevelSelect()
        {
            navigationService.OpenLevelSelect();
        }

        public void OpenMeta()
        {
            navigationService.OpenMeta();
        }

        public void StartSelectedLevel()
        {
            navigationService.StartSelectedLevel();
        }

        public void StartFirstLevel()
        {
            LevelCatalogDefinition catalog = contentService.LevelCatalog;
            if (catalog == null)
            {
                Debug.LogWarning("Level catalog is missing.");
                return;
            }

            IReadOnlyList<LevelDefinition> levels = catalog.Levels;
            LevelDefinition firstLevel = null;
            for (int i = 0; i < levels.Count; i++)
            {
                if (levels[i] != null)
                {
                    firstLevel = levels[i];
                    break;
                }
            }

            if (firstLevel == null || string.IsNullOrWhiteSpace(firstLevel.LevelId))
            {
                Debug.LogWarning("No valid first level in catalog.");
                return;
            }

            levelSelectionService.SelectLevel(firstLevel.LevelId);
            navigationService.StartSelectedLevel();
        }

        public void RetryLastLevel()
        {
            string retryLevelId = navigationService.LastBattleResult.LevelId;
            if (string.IsNullOrWhiteSpace(retryLevelId))
            {
                return;
            }

            levelSelectionService.SelectLevel(retryLevelId);
            navigationService.StartSelectedLevel();
        }

        public void StartNextLevel()
        {
            string nextLevelId = navigationService.LastBattleResult.NextLevelId;
            if (string.IsNullOrWhiteSpace(nextLevelId))
            {
                return;
            }

            levelSelectionService.SelectLevel(nextLevelId);
            navigationService.StartSelectedLevel();
        }

        public void ContinueAfterVictory()
        {
            BattleResultModel last = navigationService.LastBattleResult;
            if (last.Outcome != BattleOutcome.Victory)
            {
                return;
            }

            string nextId = last.NextLevelId;
            if (string.IsNullOrWhiteSpace(nextId) && !string.IsNullOrWhiteSpace(last.LevelId))
            {
                progressionService.TryResolveNextLevelAfterVictory(last.LevelId, out nextId);
            }

            if (!string.IsNullOrWhiteSpace(nextId))
            {
                levelSelectionService.SelectLevel(nextId);
                navigationService.StartSelectedLevel();
                return;
            }

            StartFirstLevel();
        }

        #endregion
    }
}
