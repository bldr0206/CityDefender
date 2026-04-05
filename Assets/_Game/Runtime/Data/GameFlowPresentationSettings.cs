using UnityEngine;

namespace ColorChargeTD.Data
{
    [CreateAssetMenu(menuName = "Color Charge TD/Presentation/Game Flow Presentation", fileName = "GameFlowPresentationSettings")]
    public sealed class GameFlowPresentationSettings : ScriptableObject
    {
        [SerializeField] [Min(0f)] private float victoryResultScreenDelaySeconds = 1.5f;

        public float VictoryResultScreenDelaySeconds => Mathf.Max(0f, victoryResultScreenDelaySeconds);
    }
}
