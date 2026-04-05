using System.Collections.Generic;
using UnityEngine;

namespace ColorChargeTD.Data
{
    [CreateAssetMenu(menuName = "Color Charge TD/Content/Auxiliary Building Definition", fileName = "AuxiliaryBuildingDefinition")]
    public sealed class AuxiliaryBuildingDefinition : PlaceableStructureDefinition
    {
        [Header("Unlock")]
        [SerializeField] private bool requiresPlacedTower;

        [Header("Passive income")]
        [Tooltip("Currency added to the player each time the period elapses.")]
        [SerializeField] [Min(0)] private int incomeAmount = 1;
        [Tooltip("Time in seconds between income payouts.")]
        [SerializeField] [Min(0.05f)] private float incomePeriodSeconds = 1f;

        public bool RequiresPlacedTowerToBuild => requiresPlacedTower;
        public int IncomeAmount => incomeAmount;
        public float IncomePeriodSeconds => incomePeriodSeconds;
        public bool HasPeriodicIncome => incomeAmount > 0 && incomePeriodSeconds >= 0.05f;

        public void ValidateInto(List<ContentValidationMessage> messages)
        {
            ValidateCore(messages, name, "Auxiliary building");

            if (incomeAmount < 0)
            {
                messages.Add(ContentValidationMessage.Warning(name, "Auxiliary building income amount cannot be negative."));
            }

            if (incomeAmount > 0 && incomePeriodSeconds < 0.05f)
            {
                messages.Add(ContentValidationMessage.Warning(name, "Auxiliary building income period should be at least 0.05 seconds when income is enabled."));
            }
        }
    }
}
