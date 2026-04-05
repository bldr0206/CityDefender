using ColorChargeTD.Data;
using UnityEngine;

namespace ColorChargeTD.Battle
{
    public sealed class PlacedStructureRuntimeModel
    {
        public PlacedStructureRuntimeModel(BuildSlotKind kind, PlaceableStructureDefinition definition, BuildSlotRuntimeDefinition slot)
        {
            Kind = kind;
            Definition = definition;
            Slot = slot;
        }

        public BuildSlotKind Kind { get; }
        public PlaceableStructureDefinition Definition { get; }
        public BuildSlotRuntimeDefinition Slot { get; }

        public float AuxiliaryIncomeElapsed { get; set; }

        public Vector3 WorldPosition => Slot.Position;
    }
}
