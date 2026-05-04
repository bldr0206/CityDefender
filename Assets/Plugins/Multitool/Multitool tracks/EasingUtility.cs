using UnityEngine;

namespace MultitoolTracks
{
    public enum EasingType
    {
        Linear,
        InSine,
        OutSine,
        InOutSine,
        InQuad,
        OutQuad,
        InOutQuad,
        InCubic,
        OutCubic,
        InOutCubic,
        InQuart,
        OutQuart,
        InOutQuart,
        InQuint,
        OutQuint,
        InOutQuint,
        InExpo,
        OutExpo,
        InOutExpo,
        InCirc,
        OutCirc,
        InOutCirc,
        InBack,
        OutBack,
        InOutBack,
        InElastic,
        OutElastic,
        InOutElastic,
        InBounce,
        OutBounce,
        InOutBounce
    }

    public enum AxisMode
    {
        All,
        X,
        Y,
        Z,
        XY,
        XZ,
        YZ
    }

    public enum SourceMode
    {
        Target,
        Value
    }

    public enum Space
    {
        World,
        Local
    }

    public enum RotationMode
    {
        Local,
        Relative
    }

    public enum ScaleMode
    {
        Local,
        Relative
    }

    public enum ScaleValueMode
    {
        Uniform,
        PerAxis
    }

    public static class EasingUtility
    {
        const float c1 = 2.70158f;
        const float c2 = 1.70158f;
        const float c3 = 2.5949095f;

        public static float Evaluate(EasingType type, float t)
        {
            t = Mathf.Clamp01(t);
            return type switch
            {
                EasingType.Linear => t,
                EasingType.InSine => 1f - Mathf.Cos(t * Mathf.PI * 0.5f),
                EasingType.OutSine => Mathf.Sin(t * Mathf.PI * 0.5f),
                EasingType.InOutSine => -(Mathf.Cos(Mathf.PI * t) - 1f) * 0.5f,
                EasingType.InQuad => t * t,
                EasingType.OutQuad => 1f - (1f - t) * (1f - t),
                EasingType.InOutQuad => t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) * 0.5f,
                EasingType.InCubic => t * t * t,
                EasingType.OutCubic => 1f - Mathf.Pow(1f - t, 3f),
                EasingType.InOutCubic => t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) * 0.5f,
                EasingType.InQuart => t * t * t * t,
                EasingType.OutQuart => 1f - Mathf.Pow(1f - t, 4f),
                EasingType.InOutQuart => t < 0.5f ? 8f * t * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 4f) * 0.5f,
                EasingType.InQuint => t * t * t * t * t,
                EasingType.OutQuint => 1f - Mathf.Pow(1f - t, 5f),
                EasingType.InOutQuint => t < 0.5f ? 16f * t * t * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 5f) * 0.5f,
                EasingType.InExpo => t <= 0f ? 0f : Mathf.Pow(2f, 10f * t - 10f),
                EasingType.OutExpo => t >= 1f ? 1f : 1f - Mathf.Pow(2f, -10f * t),
                EasingType.InOutExpo => t <= 0f ? 0f : t >= 1f ? 1f : t < 0.5f ? Mathf.Pow(2f, 20f * t - 10f) * 0.5f : 1f - Mathf.Pow(2f, -20f * t + 10f) * 0.5f,
                EasingType.InCirc => 1f - Mathf.Sqrt(1f - t * t),
                EasingType.OutCirc => Mathf.Sqrt(1f - (t - 1f) * (t - 1f)),
                EasingType.InOutCirc => t < 0.5f ? (1f - Mathf.Sqrt(1f - 4f * t * t)) * 0.5f : (Mathf.Sqrt(1f - (2f - 2f * t) * (2f - 2f * t)) + 1f) * 0.5f,
                EasingType.InBack => c1 * t * t * t - c2 * t * t,
                EasingType.OutBack => 1f + c1 * Mathf.Pow(t - 1f, 3f) + c2 * Mathf.Pow(t - 1f, 2f),
                EasingType.InOutBack => t < 0.5f ? 0.5f * (4f * t * t * ((c3 + 1f) * 2f * t - c3)) : 0.5f * (Mathf.Pow(2f * t - 2f, 2f) * ((c3 + 1f) * (2f * t - 2f) + c3) + 2f),
                EasingType.InElastic => t <= 0f ? 0f : t >= 1f ? 1f : -Mathf.Pow(2f, 10f * t - 10f) * Mathf.Sin((t * 10f - 10.75f) * (2f * Mathf.PI / 3f)),
                EasingType.OutElastic => t <= 0f ? 0f : t >= 1f ? 1f : Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * (2f * Mathf.PI / 3f)) + 1f,
                EasingType.InOutElastic => t <= 0f ? 0f : t >= 1f ? 1f : t < 0.5f ? -(Mathf.Pow(2f, 20f * t - 10f) * Mathf.Sin((20f * t - 11.125f) * (2f * Mathf.PI / 4.5f))) * 0.5f : Mathf.Pow(2f, -20f * t + 10f) * Mathf.Sin((20f * t - 11.125f) * (2f * Mathf.PI / 4.5f)) * 0.5f + 1f,
                EasingType.InBounce => 1f - BounceOut(1f - t),
                EasingType.OutBounce => BounceOut(t),
                EasingType.InOutBounce => t < 0.5f ? (1f - BounceOut(1f - 2f * t)) * 0.5f : (1f + BounceOut(2f * t - 1f)) * 0.5f,
                _ => t
            };
        }

        static float BounceOut(float t)
        {
            const float n1 = 7.5625f;
            const float d1 = 2.75f;
            if (t < 1f / d1) return n1 * t * t;
            if (t < 2f / d1) return n1 * (t -= 1.5f / d1) * t + 0.75f;
            if (t < 2.5f / d1) return n1 * (t -= 2.25f / d1) * t + 0.9375f;
            return n1 * (t -= 2.625f / d1) * t + 0.984375f;
        }

        public static bool HasAxis(AxisMode mode, int axis)
        {
            return mode switch
            {
                AxisMode.All => true,
                AxisMode.X => axis == 0,
                AxisMode.Y => axis == 1,
                AxisMode.Z => axis == 2,
                AxisMode.XY => axis == 0 || axis == 1,
                AxisMode.XZ => axis == 0 || axis == 2,
                AxisMode.YZ => axis == 1 || axis == 2,
                _ => false
            };
        }
    }
}
