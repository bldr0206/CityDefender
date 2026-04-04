using ColorChargeTD.Battle;
using ColorChargeTD.Product;
using UnityEngine;
using Zenject;

namespace ColorChargeTD.Presentation
{
    public sealed class BattlePauseMenuController : MonoBehaviour
    {
        #region References

        [SerializeField] private BattlePauseMenuView view;
        [SerializeField] private LevelSessionController levelSession;

        #endregion

        #region Injected

        [Inject] private IGameNavigationService navigationService;

        #endregion

        #region State

        private bool isPaused;

        #endregion

        #region UnityLifecycle

        private void Start()
        {
            if (view == null)
            {
                Debug.LogWarning("BattlePauseMenuController is missing BattlePauseMenuView.");
                return;
            }

            view.SetHandlers(OnPauseOpenClicked, OnResumeClicked, OnToMenuClicked);
        }

        private void OnDestroy()
        {
            if (isPaused)
            {
                Time.timeScale = 1f;
            }
        }

        #endregion

        #region Handlers

        private void OnPauseOpenClicked()
        {
            if (isPaused)
            {
                return;
            }

            Pause();
        }

        private void OnResumeClicked()
        {
            Resume();
        }

        private void OnToMenuClicked()
        {
            Time.timeScale = 1f;
            isPaused = false;
            view?.SetOverlayVisible(false);
            navigationService?.OpenMainMenu();
        }

        private void Pause()
        {
            levelSession?.CloseRadialBuildMenuIfOpen();
            isPaused = true;
            Time.timeScale = 0f;
            view?.SetOverlayVisible(true);
        }

        private void Resume()
        {
            isPaused = false;
            Time.timeScale = 1f;
            view?.SetOverlayVisible(false);
        }

        #endregion
    }
}
