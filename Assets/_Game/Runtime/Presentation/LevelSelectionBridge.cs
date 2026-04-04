using System;
using System.Collections.Generic;
using ColorChargeTD.Core;
using ColorChargeTD.Product;
using UnityEngine;
using Zenject;

namespace ColorChargeTD.Presentation
{
    public sealed class LevelSelectionBridge : MonoBehaviour
    {
        [Inject] private ILevelSelectionService levelSelectionService;
        [Inject] private IProgressionService progressionService;

        public event Action<IReadOnlyList<Profile.LevelCardViewModel>> CardsChanged;

        private void Start()
        {
            Refresh();
        }

        public void SelectLevel(string levelId)
        {
            levelSelectionService.SelectLevel(levelId);
            Refresh();
        }

        public void Refresh()
        {
            CardsChanged?.Invoke(progressionService.BuildLevelCards());
        }
    }
}
