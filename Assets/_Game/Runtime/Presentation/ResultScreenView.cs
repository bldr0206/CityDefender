using ColorChargeTD.Domain;
using UnityEngine;

namespace ColorChargeTD.Presentation
{
    public sealed class ResultScreenView : MonoBehaviour
    {
        [SerializeField] private string levelId;
        [SerializeField] private BattleOutcome outcome;
        [SerializeField] private int awardedSoftCurrency;
        [SerializeField] private bool unlockedNextLevel;
        [SerializeField] private string nextLevelId;

        public void Bind(Profile.BattleResultModel result)
        {
            levelId = result.LevelId;
            outcome = result.Outcome;
            awardedSoftCurrency = result.AwardedSoftCurrency;
            unlockedNextLevel = result.UnlockedNextLevel;
            nextLevelId = result.NextLevelId;
        }
    }
}
