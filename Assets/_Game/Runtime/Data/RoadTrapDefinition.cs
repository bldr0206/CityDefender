using System.Collections.Generic;
using UnityEngine;

namespace ColorChargeTD.Data
{
    [CreateAssetMenu(menuName = "Color Charge TD/Content/Road Trap Definition", fileName = "RoadTrapDefinition")]
    public sealed class RoadTrapDefinition : PlaceableStructureDefinition
    {
        public void ValidateInto(List<ContentValidationMessage> messages)
        {
            ValidateCore(messages, name, "Road trap");
        }
    }
}
