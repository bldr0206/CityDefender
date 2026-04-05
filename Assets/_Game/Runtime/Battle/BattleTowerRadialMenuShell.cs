using UnityEngine;
using UnityEngine.UI;

namespace ColorChargeTD.Battle
{
    public sealed class BattleTowerRadialMenuShell : MonoBehaviour
    {
        #region Serialized

        [SerializeField] private RectTransform wheel;
        [SerializeField] private Button blockerButton;

        #endregion

        public RectTransform Wheel => wheel;
        public Button BlockerButton => blockerButton;
    }
}
