using ColorChargeTD.Data;
using ColorChargeTD.Product;
using UnityEngine;
using Zenject;

namespace ColorChargeTD.Presentation
{
    public sealed class MetaUpgradeBridge : MonoBehaviour
    {
        [SerializeField] private UpgradeDefinition upgradeDefinition;

        [Inject] private IProgressionService progressionService;

        public bool TryPurchaseConfiguredUpgrade()
        {
            return progressionService.TryPurchaseUpgrade(upgradeDefinition);
        }
    }
}
