using System;
using UnityEngine;
using UnityEngine.UI;

namespace ColorChargeTD.Presentation
{
    public sealed class BattlePauseMenuView : MonoBehaviour
    {
        #region References

        [SerializeField] private Button pauseToggleButton;
        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button toMenuButton;
        [SerializeField] private Text pauseButtonLabel;
        [SerializeField] private Text resumeButtonLabel;
        [SerializeField] private Text toMenuButtonLabel;

        #endregion

        #region Events

        private Action onPauseOpen;
        private Action onResume;
        private Action onToMenu;

        #endregion

        #region UnityLifecycle

        private void Awake()
        {
            ApplyDefaultLabels();

            if (pauseToggleButton != null)
            {
                pauseToggleButton.onClick.AddListener(() => onPauseOpen?.Invoke());
            }

            if (resumeButton != null)
            {
                resumeButton.onClick.AddListener(() => onResume?.Invoke());
            }

            if (toMenuButton != null)
            {
                toMenuButton.onClick.AddListener(() => onToMenu?.Invoke());
            }

            SetOverlayVisible(false);
        }

        private void OnDestroy()
        {
            if (pauseToggleButton != null)
            {
                pauseToggleButton.onClick.RemoveAllListeners();
            }

            if (resumeButton != null)
            {
                resumeButton.onClick.RemoveAllListeners();
            }

            if (toMenuButton != null)
            {
                toMenuButton.onClick.RemoveAllListeners();
            }
        }

        #endregion

        #region PublicAPI

        public void SetHandlers(Action pauseOpen, Action resume, Action toMenu)
        {
            onPauseOpen = pauseOpen;
            onResume = resume;
            onToMenu = toMenu;
        }

        public void SetOverlayVisible(bool visible)
        {
            if (overlayRoot != null)
            {
                overlayRoot.SetActive(visible);
            }
        }

        public void SetPauseCornerButtonVisible(bool visible)
        {
            if (pauseToggleButton != null)
            {
                pauseToggleButton.gameObject.SetActive(visible);
            }
        }

        #endregion

        #region Labels

        private void ApplyDefaultLabels()
        {
            if (pauseButtonLabel != null)
            {
                pauseButtonLabel.text = "II";
            }

            if (resumeButtonLabel != null)
            {
                resumeButtonLabel.text = "Resume";
            }

            if (toMenuButtonLabel != null)
            {
                toMenuButtonLabel.text = "To menu";
            }
        }

        #endregion
    }
}
