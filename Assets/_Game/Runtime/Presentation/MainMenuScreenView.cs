using UnityEngine;
using UnityEngine.Events;
using Zenject;

namespace ColorChargeTD.Presentation
{
    public sealed class MainMenuScreenView : MonoBehaviour
    {
        [Inject] private NavigationCommandRouter navigationCommandRouter;

        [SerializeField] private UnityEvent playRequested;
        [SerializeField] private UnityEvent upgradesRequested;
        [SerializeField] private UnityEvent shopRequested;
        [SerializeField] private UnityEvent settingsRequested;

        public void RequestPlay()
        {
            navigationCommandRouter?.StartFirstLevel();
            playRequested?.Invoke();
        }
        public void RequestUpgrades() => upgradesRequested?.Invoke();
        public void RequestShop() => shopRequested?.Invoke();
        public void RequestSettings() => settingsRequested?.Invoke();
    }
}
