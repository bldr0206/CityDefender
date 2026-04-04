using ColorChargeTD.Product;
using UnityEngine;
using Zenject;

namespace ColorChargeTD.Presentation
{
    public sealed class BootSceneEntryPoint : MonoBehaviour
    {
        [Inject] private IPlayerProfileService profileService;
        [Inject] private IGameNavigationService navigationService;

        private void Awake()
        {
            navigationService.Initialize(this);
        }

        private void Start()
        {
            profileService.Load();
            navigationService.OpenMainMenu();
        }
    }
}
