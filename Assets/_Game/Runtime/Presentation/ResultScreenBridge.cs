using ColorChargeTD.Core;
using ColorChargeTD.Domain;
using ColorChargeTD.Product;
using UnityEngine;
using Zenject;

namespace ColorChargeTD.Presentation
{
    public sealed class ResultScreenBridge : MonoBehaviour
    {
        #region References

        [SerializeField] private ResultScreenView targetView;
        [SerializeField] private GameObject metaScreenRoot;
        [SerializeField] private NavigationCommandRouter commandRouter;

        #endregion

        #region Injected

        [Inject] private IGameNavigationService navigationService;
        [Inject] private IGameStateMachine gameStateMachine;

        #endregion

        #region UnityLifecycle

        private void Start()
        {
            if (targetView == null || commandRouter == null)
            {
                Debug.LogWarning("ResultScreenBridge is missing required references.");
                return;
            }

            if (gameStateMachine.CurrentState != GameFlowState.Result)
            {
                targetView.SetPanelVisible(false);
                return;
            }

            if (metaScreenRoot != null)
            {
                metaScreenRoot.SetActive(false);
            }

            targetView.SetPanelVisible(true);
            targetView.Bind(
                navigationService.LastBattleResult,
                commandRouter.ContinueAfterVictory,
                commandRouter.RetryLastLevel,
                commandRouter.RetryLastLevel);
        }

        #endregion
    }
}
