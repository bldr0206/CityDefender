using ColorChargeTD.Data;
using UnityEngine;

namespace ColorChargeTD.Presentation
{
    public static class BuildSlotVisualPalette
    {
        public static Color GizmoCircleColor(BuildSlotKind kind)
        {
            switch (kind)
            {
                case BuildSlotKind.Auxiliary:
                    return new Color(0.25f, 0.65f, 0.95f, 0.9f);
                case BuildSlotKind.RoadTrap:
                    return new Color(0.95f, 0.55f, 0.2f, 0.9f);
                default:
                    return new Color(0.2f, 0.8f, 0.4f, 0.85f);
            }
        }

        public static Color PlusBaseColor(BuildSlotKind kind)
        {
            switch (kind)
            {
                case BuildSlotKind.Auxiliary:
                    return new Color(0.55f, 0.82f, 1f, 1f);
                case BuildSlotKind.RoadTrap:
                    return new Color(1f, 0.72f, 0.38f, 1f);
                default:
                    return new Color(0.85f, 1f, 0.9f, 1f);
            }
        }

        public static Color PlusAffordableTint(BuildSlotKind kind, bool affordable)
        {
            Color baseColor = PlusBaseColor(kind);
            if (affordable)
            {
                return baseColor;
            }

            baseColor.a = 0.38f;
            baseColor.r *= 0.55f;
            baseColor.g *= 0.55f;
            baseColor.b *= 0.55f;
            return baseColor;
        }
    }
}
