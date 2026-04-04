using ColorChargeTD.Product;
using UnityEngine;
using Zenject;

namespace ColorChargeTD.Presentation
{
    public sealed class SceneNavigationBridge : MonoBehaviour
    {
        [Inject] private IGameNavigationService navigationService;

        private void Awake()
        {
            navigationService.Initialize(this);
        }
    }
}
