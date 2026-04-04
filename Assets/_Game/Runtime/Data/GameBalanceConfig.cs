using UnityEngine;

namespace ColorChargeTD.Data
{
    [CreateAssetMenu(menuName = "Color Charge TD/Content/Game Balance", fileName = "GameBalanceConfig")]
    public sealed class GameBalanceConfig : ScriptableObject
    {
        [SerializeField] private int defaultStartingResource = 125;
        [SerializeField] private int defaultTowerCost = 50;
        [SerializeField] private int defaultTowerCapacity = 3;
        [SerializeField] private float defaultProductionPerSecond = 3f;
        [SerializeField] private float defaultFireRatePerSecond = 1f;
        [SerializeField] private int baseLives = 10;
        [SerializeField] private bool enableOvercharge = false;

        public int DefaultStartingResource => Mathf.Max(0, defaultStartingResource);
        public int DefaultTowerCost => Mathf.Max(0, defaultTowerCost);
        public int DefaultTowerCapacity => Mathf.Max(1, defaultTowerCapacity);
        public float DefaultProductionPerSecond => Mathf.Max(0f, defaultProductionPerSecond);
        public float DefaultFireRatePerSecond => Mathf.Max(0.1f, defaultFireRatePerSecond);
        public int BaseLives => Mathf.Max(1, baseLives);
        public bool EnableOvercharge => enableOvercharge;
    }
}
