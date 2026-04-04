using System;
using System.Collections.Generic;
using UnityEngine;

namespace ColorChargeTD.Presentation
{
    public sealed class LevelSelectScreenView : MonoBehaviour
    {
        private IReadOnlyList<Profile.LevelCardViewModel> currentCards = Array.Empty<Profile.LevelCardViewModel>();

        public IReadOnlyList<Profile.LevelCardViewModel> CurrentCards => currentCards;

        public void Bind(IReadOnlyList<Profile.LevelCardViewModel> cards)
        {
            currentCards = cards ?? Array.Empty<Profile.LevelCardViewModel>();
        }
    }
}
