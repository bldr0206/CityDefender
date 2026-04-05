using ColorChargeTD.Domain;
using UnityEngine;

namespace ColorChargeTD.Data
{
    [CreateAssetMenu(menuName = "Color Charge TD/Content/Tower Definition", fileName = "TowerDefinition")]
    public sealed class TowerDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string towerId = "tower-red-basic";
        [SerializeField] private string displayName = "Tower";
        [SerializeField] private ColorCharge color = ColorCharge.Red;

        [Header("Economy")]
        [SerializeField] private int buildCost = 50;

        [Header("Combat")]
        [SerializeField] private int damagePerShot = 1;
        [SerializeField] private int capacity = 3;
        [SerializeField] private float productionPerSecond = 3f;
        [SerializeField] private float fireRatePerSecond = 1f;
        [SerializeField] private float range = 4f;
        [SerializeField] private float overchargeMultiplier = 2f;
        [SerializeField] private float projectileSpeed = 8f;
        [SerializeField] private float projectileArcPeakHeight = 3f;
        [SerializeField] private float turretTraverseDegreesPerSecond = 320f;
        [SerializeField] private float turretFireAlignToleranceDegrees = 12f;

        [Header("Presentation")]
        [SerializeField] private GameObject prefab;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private AudioClip fireSfx;
        [SerializeField] private GameObject muzzleVfxPrefab;

        public string TowerId => towerId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? towerId : displayName;
        public ColorCharge Color => color;
        public int BuildCost => buildCost;
        public int DamagePerShot => damagePerShot;
        public int Capacity => Mathf.Max(1, capacity);
        public float ProductionPerSecond => Mathf.Max(0f, productionPerSecond);
        public float FireRatePerSecond => Mathf.Max(0.1f, fireRatePerSecond);
        public float Range => Mathf.Max(0.1f, range);
        public float OverchargeMultiplier => Mathf.Max(1f, overchargeMultiplier);
        public float ProjectileSpeed => Mathf.Max(0.01f, projectileSpeed);
        public float ProjectileArcPeakHeight => Mathf.Max(0f, projectileArcPeakHeight);
        public float TurretTraverseDegreesPerSecond => Mathf.Max(30f, turretTraverseDegreesPerSecond);
        public float TurretFireAlignToleranceDegrees => Mathf.Clamp(turretFireAlignToleranceDegrees, 1f, 45f);
        public GameObject Prefab => prefab;
        public GameObject ProjectilePrefab => projectilePrefab;
        public AudioClip FireSfx => fireSfx;
        public GameObject MuzzleVfxPrefab => muzzleVfxPrefab;

        public void ValidateInto(System.Collections.Generic.List<ContentValidationMessage> messages)
        {
            if (string.IsNullOrWhiteSpace(towerId))
            {
                messages.Add(ContentValidationMessage.Error(name, "TowerId is required."));
            }

            if (buildCost <= 0)
            {
                messages.Add(ContentValidationMessage.Error(name, "Build cost must be greater than zero."));
            }

            if (damagePerShot <= 0)
            {
                messages.Add(ContentValidationMessage.Error(name, "Damage per shot must be greater than zero."));
            }

            if (projectileSpeed <= 0f)
            {
                messages.Add(ContentValidationMessage.Error(name, "Projectile speed must be greater than zero."));
            }

            if (prefab == null)
            {
                messages.Add(ContentValidationMessage.Warning(name, "Tower prefab reference is missing."));
            }
        }
    }
}
