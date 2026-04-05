using System.Collections.Generic;
using UnityEngine;

namespace ColorChargeTD.Data
{
    public abstract class PlaceableStructureDefinition : ScriptableObject
    {
        [SerializeField] private string structureId = "structure-id";
        [SerializeField] private string displayName = "Structure";
        [SerializeField] private int buildCost = 25;
        [SerializeField] private GameObject prefab;

        public string StructureId => structureId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? structureId : displayName;
        public int BuildCost => buildCost;
        public GameObject Prefab => prefab;

        protected void ValidateCore(List<ContentValidationMessage> messages, string sourceAssetName, string label)
        {
            if (string.IsNullOrWhiteSpace(structureId))
            {
                messages.Add(ContentValidationMessage.Error(sourceAssetName, label + " StructureId is required."));
            }

            if (buildCost <= 0)
            {
                messages.Add(ContentValidationMessage.Error(sourceAssetName, label + " build cost must be greater than zero."));
            }

            if (prefab == null)
            {
                messages.Add(ContentValidationMessage.Warning(sourceAssetName, label + " prefab reference is missing."));
            }
        }
    }
}
