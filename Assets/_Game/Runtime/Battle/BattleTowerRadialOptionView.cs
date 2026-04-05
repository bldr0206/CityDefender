using ColorChargeTD.Data;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ColorChargeTD.Battle
{
    public sealed class BattleTowerRadialOptionView : MonoBehaviour
    {
        #region Serialized

        [SerializeField] private Image backgroundImage;
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text costLabel;

        [SerializeField] private Color affordableBackground = new Color(0.25f, 0.45f, 0.38f, 0.95f);
        [SerializeField] private Color unaffordableBackground = new Color(0.35f, 0.35f, 0.35f, 0.85f);
        [SerializeField] private Color affordableCost = new Color(1f, 0.92f, 0.35f, 1f);
        [SerializeField] private Color unaffordableCost = new Color(0.55f, 0.52f, 0.42f, 1f);

        #endregion

        #region PublicAPI

        public void Bind(TowerDefinition tower, bool affordable, UnityAction onClick)
        {
            if (tower == null)
            {
                return;
            }

            Bind(new BuildRadialOptionData(tower.TowerId, tower.DisplayName, tower.BuildCost), affordable, onClick);
        }

        public void Bind(BuildRadialOptionData option, bool affordable, UnityAction onClick)
        {
            if (string.IsNullOrWhiteSpace(option.OptionId))
            {
                return;
            }

            if (nameLabel != null)
            {
                nameLabel.text = option.DisplayName;
                nameLabel.color = new Color(1f, 1f, 1f, 0.92f);
            }

            if (costLabel != null)
            {
                if (option.CanPurchase)
                {
                    costLabel.text = option.BuildCost.ToString();
                    costLabel.color = affordable ? affordableCost : unaffordableCost;
                }
                else
                {
                    costLabel.text = string.Empty;
                    costLabel.color = unaffordableCost;
                }
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = affordable ? affordableBackground : unaffordableBackground;
            }

            if (button != null)
            {
                button.interactable = option.CanPurchase && affordable;
                button.onClick.RemoveAllListeners();
                if (onClick != null)
                {
                    button.onClick.AddListener(onClick);
                }
            }
        }

        #endregion
    }
}
