using UnityEngine;

namespace ColorChargeTD.Data
{
    [CreateAssetMenu(menuName = "Color Charge TD/Content/Enemy Presentation Profile", fileName = "EnemyPresentationProfile")]
    public sealed class EnemyPresentationProfile : ScriptableObject
    {
        [Header("Move bob (while root is moving)")]
        [SerializeField] [Min(0.1f)] private float moveBobFrequency = 8f;
        [SerializeField] [Min(0f)] private float moveBobAmplitudeY = 0.06f;
        [SerializeField] private float moveBobPitchDegrees = 5f;

        [Header("Hit react")]
        [SerializeField] [Min(0.01f)] private float hitReactDuration = 0.14f;
        [SerializeField] [Min(0f)] private float hitVerticalPunch = 0.08f;
        [SerializeField] [Min(0.01f)] private float hitSquashXZ = 0.88f;
        [SerializeField] [Min(0.01f)] private float hitSquashY = 1.12f;
        [SerializeField] private AnimationCurve hitEnvelope;

        [Header("Stun visual")]
        [SerializeField] [Min(0.01f)] private float stunBobFrequencyMultiplier = 0.4f;
        [SerializeField] [Min(0f)] private float stunSideSwayAmplitude = 0.045f;
        [SerializeField] private float stunPitchWobbleDegrees = 6f;
        [SerializeField] private float stunRollWobbleDegrees = 5f;
        [SerializeField] [Min(0f)] private float stunBobAmplitudeScale = 0.35f;

        [Header("Animator (optional)")]
        [SerializeField] private string animatorSpeedFloatParam = string.Empty;
        [SerializeField] private string animatorHitTriggerParam = string.Empty;
        [SerializeField] private string animatorStunnedBoolParam = string.Empty;

        public float MoveBobFrequency => moveBobFrequency;
        public float MoveBobAmplitudeY => moveBobAmplitudeY;
        public float MoveBobPitchDegrees => moveBobPitchDegrees;
        public float HitReactDuration => hitReactDuration;
        public float HitVerticalPunch => hitVerticalPunch;
        public float HitSquashXZ => hitSquashXZ;
        public float HitSquashY => hitSquashY;
        public AnimationCurve HitEnvelope => hitEnvelope;
        public float StunBobFrequencyMultiplier => stunBobFrequencyMultiplier;
        public float StunSideSwayAmplitude => stunSideSwayAmplitude;
        public float StunPitchWobbleDegrees => stunPitchWobbleDegrees;
        public float StunRollWobbleDegrees => stunRollWobbleDegrees;
        public float StunBobAmplitudeScale => stunBobAmplitudeScale;
        public string AnimatorSpeedFloatParam => animatorSpeedFloatParam;
        public string AnimatorHitTriggerParam => animatorHitTriggerParam;
        public string AnimatorStunnedBoolParam => animatorStunnedBoolParam;
    }
}
