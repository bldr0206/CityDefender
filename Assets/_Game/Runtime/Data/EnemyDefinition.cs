using ColorChargeTD.Domain;
using UnityEngine;

namespace ColorChargeTD.Data
{
    [CreateAssetMenu(menuName = "Color Charge TD/Content/Enemy Definition", fileName = "EnemyDefinition")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string enemyId = "enemy-red-basic";
        [SerializeField] private string displayName = "Red Creep";
        [SerializeField] private ColorCharge color = ColorCharge.Red;

        [Header("Stats")]
        [SerializeField] private int hitPoints = 3;
        [SerializeField] private float speed = 1f;
        [SerializeField] private int baseReward = 1;

        [Header("Presentation")]
        [SerializeField] private GameObject prefab;
        [SerializeField] [Min(0.01f)] private float visualScale = 1f;

        public string EnemyId => enemyId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? enemyId : displayName;
        public ColorCharge Color => color;
        public int HitPoints => Mathf.Max(1, hitPoints);
        public float Speed => Mathf.Max(0.1f, speed);
        public int BaseReward => Mathf.Max(0, baseReward);
        public GameObject Prefab => prefab;
        public float VisualScale => Mathf.Max(0.01f, visualScale);

        public void ValidateInto(System.Collections.Generic.List<ContentValidationMessage> messages)
        {
            if (string.IsNullOrWhiteSpace(enemyId))
            {
                messages.Add(ContentValidationMessage.Error(name, "EnemyId is required."));
            }

            if (hitPoints <= 0)
            {
                messages.Add(ContentValidationMessage.Error(name, "Hit points must be greater than zero."));
            }

            if (prefab == null)
            {
                messages.Add(ContentValidationMessage.Warning(name, "Enemy prefab reference is missing."));
            }
        }
    }
}
