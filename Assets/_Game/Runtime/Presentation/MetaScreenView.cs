using UnityEngine;
using UnityEngine.Events;

namespace ColorChargeTD.Presentation
{
    public sealed class MetaScreenView : MonoBehaviour
    {
        [SerializeField] private UnityEvent retryRequested;
        [SerializeField] private UnityEvent nextLevelRequested;
        [SerializeField] private UnityEvent levelSelectRequested;
        [SerializeField] private UnityEvent upgradesRequested;
        [SerializeField] private UnityEvent shopRequested;

        public void RequestRetry() => retryRequested?.Invoke();
        public void RequestNextLevel() => nextLevelRequested?.Invoke();
        public void RequestLevelSelect() => levelSelectRequested?.Invoke();
        public void RequestUpgrades() => upgradesRequested?.Invoke();
        public void RequestShop() => shopRequested?.Invoke();
    }
}
