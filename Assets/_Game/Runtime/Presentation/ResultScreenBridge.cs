using ColorChargeTD.Product;
using UnityEngine;
using Zenject;

namespace ColorChargeTD.Presentation
{
    public sealed class ResultScreenBridge : MonoBehaviour
    {
        [SerializeField] private ResultScreenView targetView;

        [Inject] private IGameNavigationService navigationService;

        private void Start()
        {
            if (targetView != null)
            {
                targetView.Bind(navigationService.LastBattleResult);
            }
        }
    }
}
