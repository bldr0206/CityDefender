using System;
using ColorChargeTD.Domain;
using ColorChargeTD.Profile;
using UnityEngine;
using UnityEngine.UI;

namespace ColorChargeTD.Presentation
{
    public sealed class ResultScreenView : MonoBehaviour
    {
        #region SerializedRefs

        [SerializeField] private GameObject contentRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Text detailText;
        [SerializeField] private Button primaryButton;
        [SerializeField] private Text primaryButtonCaption;
        [SerializeField] private GameObject restartLevelRow;
        [SerializeField] private Button restartLevelButton;

        #endregion

        #region State

        private Action primaryAction;
        private Action restartLevelAction;

        #endregion

        #region UnityLifecycle

        private void Awake()
        {
            EnsureHierarchyFromCodeIfNeeded();
            WireClickHandlers();
        }

        private void OnDestroy()
        {
            if (primaryButton != null)
            {
                primaryButton.onClick.RemoveListener(OnPrimaryClicked);
            }

            if (restartLevelButton != null)
            {
                restartLevelButton.onClick.RemoveListener(OnRestartLevelClicked);
            }
        }

        #endregion

        #region Hierarchy

        private void EnsureHierarchyFromCodeIfNeeded()
        {
            if (contentRoot != null)
            {
                return;
            }

            ResultScreenViewLayout.BuiltRefs built = ResultScreenViewLayout.BuildUnder(transform);
            contentRoot = built.contentRoot;
            titleText = built.titleText;
            detailText = built.detailText;
            primaryButton = built.primaryButton;
            primaryButtonCaption = built.primaryButtonCaption;
            restartLevelRow = built.restartLevelRow;
            restartLevelButton = built.restartLevelButton;
        }

        private void WireClickHandlers()
        {
            if (primaryButton != null)
            {
                primaryButton.onClick.AddListener(OnPrimaryClicked);
            }

            if (restartLevelButton != null)
            {
                restartLevelButton.onClick.AddListener(OnRestartLevelClicked);
            }
        }

        #endregion

        #region PublicAPI

        public void SetPanelVisible(bool visible)
        {
            if (contentRoot != null)
            {
                contentRoot.SetActive(visible);
            }
        }

        public void Bind(BattleResultModel result, Action onVictoryContinue, Action onDefeatRetry, Action onVictoryRestartLevel)
        {
            EnsureHierarchyFromCodeIfNeeded();

            if (result.Outcome == BattleOutcome.Victory)
            {
                titleText.text = "Victory";
                detailText.text = $"Credits earned: {result.AwardedSoftCurrency}";
                if (result.UnlockedNextLevel && !string.IsNullOrWhiteSpace(result.NextLevelId))
                {
                    detailText.text += "\nNext level unlocked.";
                }

                bool hasNext = !string.IsNullOrWhiteSpace(result.NextLevelId);
                primaryButtonCaption.text = hasNext ? "Continue" : "Play again";
                primaryAction = onVictoryContinue;
                restartLevelAction = onVictoryRestartLevel;
                if (restartLevelRow != null)
                {
                    restartLevelRow.SetActive(true);
                }
            }
            else
            {
                titleText.text = "Defeat";
                detailText.text = "Your city took too much damage.";
                primaryButtonCaption.text = "Retry";
                primaryAction = onDefeatRetry;
                restartLevelAction = null;
                if (restartLevelRow != null)
                {
                    restartLevelRow.SetActive(false);
                }
            }
        }

        #endregion

        #region Input

        private void OnPrimaryClicked()
        {
            primaryAction?.Invoke();
        }

        private void OnRestartLevelClicked()
        {
            restartLevelAction?.Invoke();
        }

        #endregion
    }
}
