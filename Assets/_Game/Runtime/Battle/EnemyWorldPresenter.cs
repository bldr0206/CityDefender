using ColorChargeTD.Data;
using UnityEngine;

namespace ColorChargeTD.Battle
{
    public sealed class EnemyWorldPresenter : MonoBehaviour
    {
        [SerializeField] private Transform visualRoot;
        [SerializeField] private EnemyPresentationProfile profile;
        [SerializeField] private Animator animator;

        #region Animator cache
        private int speedParamHash;
        private int hitTriggerParamHash;
        private int stunnedBoolParamHash;
        private bool animatorParamsCached;
        private EnemyPresentationProfile animatorParamsSource;
        #endregion

        #region State
        private Vector3 visualBaseLocalPosition;
        private Quaternion visualBaseLocalRotation;
        private Vector3 visualBaseLocalScale;
        private bool visualDefaultsCached;

        private Vector3 lastWorldPosition;
        private bool hasLastWorldPosition;
        private float movePhase;

        private float hitTimeRemaining;
        private float hitDurationTotal;
        #endregion

        #region Unity lifecycle
        private void Awake()
        {
            EnsureVisualRoot();
            CacheVisualDefaultsIfNeeded();
            InvalidateAnimatorParamCache();
        }

        private void OnValidate()
        {
            EnsureVisualRoot();
            InvalidateAnimatorParamCache();
        }
        #endregion

        #region Public API
        public void SetPresentationProfile(EnemyPresentationProfile newProfile)
        {
            if (ReferenceEquals(profile, newProfile))
            {
                return;
            }

            profile = newProfile;
            InvalidateAnimatorParamCache();
        }

        public void NotifyHit()
        {
            float duration = profile != null ? profile.HitReactDuration : 0.14f;
            hitDurationTotal = Mathf.Max(hitDurationTotal, duration);
            hitTimeRemaining = Mathf.Max(hitTimeRemaining, duration);
            FireAnimatorHit();
        }

        public void Sync(EnemyRuntimeModel model, float deltaTime)
        {
            EnsureVisualRoot();
            CacheVisualDefaultsIfNeeded();

            if (visualRoot == null)
            {
                return;
            }

            EnemyPresentationProfile p = profile;
            Vector3 worldPos = transform.position;
            bool isMoving = hasLastWorldPosition && (worldPos - lastWorldPosition).sqrMagnitude > 1e-10f;
            lastWorldPosition = worldPos;
            hasLastWorldPosition = true;

            if (hitTimeRemaining > 0f)
            {
                hitTimeRemaining = Mathf.Max(0f, hitTimeRemaining - deltaTime);
                if (hitTimeRemaining <= 0f)
                {
                    hitDurationTotal = 0f;
                }
            }

            bool stunned = model != null && model.StunTimeRemaining > 0f;
            EnsureAnimatorParamHashesForProfile(p);
            UpdateAnimator(p, isMoving, stunned);

            float bobFreq = p != null ? p.MoveBobFrequency : 8f;
            movePhase += deltaTime * bobFreq;

            float ampY = p != null ? p.MoveBobAmplitudeY : 0.06f;
            float pitchDeg = p != null ? p.MoveBobPitchDegrees : 5f;

            Vector3 localPos = visualBaseLocalPosition;
            Quaternion localRot = visualBaseLocalRotation;
            Vector3 localScale = visualBaseLocalScale;

            if (hitTimeRemaining > 0f)
            {
                float total = Mathf.Max(0.001f, hitDurationTotal);
                float t = 1f - hitTimeRemaining / total;
                float env = EvaluateHitEnvelope(p, t);
                float punch = p != null ? p.HitVerticalPunch : 0.08f;
                localPos.y += punch * env;
                float xz = p != null ? p.HitSquashXZ : 0.88f;
                float yStretch = p != null ? p.HitSquashY : 1.12f;
                localScale = Vector3.Lerp(
                    visualBaseLocalScale,
                    new Vector3(
                        visualBaseLocalScale.x * xz,
                        visualBaseLocalScale.y * yStretch,
                        visualBaseLocalScale.z * xz),
                    env);
            }
            else if (stunned)
            {
                float freqMul = p != null ? p.StunBobFrequencyMultiplier : 0.4f;
                float stunPhase = movePhase * freqMul;
                float stunAmp = ampY * (p != null ? p.StunBobAmplitudeScale : 0.35f);
                localPos.y += stunAmp * Mathf.Abs(Mathf.Sin(stunPhase));
                float side = p != null ? p.StunSideSwayAmplitude : 0.045f;
                localPos.x += side * Mathf.Sin(stunPhase * 1.3f);
                float pWobble = p != null ? p.StunPitchWobbleDegrees : 6f;
                float rWobble = p != null ? p.StunRollWobbleDegrees : 5f;
                localRot *= Quaternion.Euler(
                    pWobble * Mathf.Sin(stunPhase * 0.7f),
                    0f,
                    rWobble * Mathf.Cos(stunPhase * 0.9f));
            }
            else if (isMoving)
            {
                localPos.y += ampY * Mathf.Abs(Mathf.Sin(movePhase));
                localRot *= Quaternion.Euler(pitchDeg * Mathf.Sin(movePhase * 2f), 0f, 0f);
            }

            visualRoot.localPosition = localPos;
            visualRoot.localRotation = localRot;
            visualRoot.localScale = localScale;
        }
        #endregion

        #region Internals
        private void EnsureVisualRoot()
        {
            if (visualRoot != null)
            {
                return;
            }

            Transform found = transform.Find("View");
            visualRoot = found != null ? found : transform;
        }

        private void CacheVisualDefaultsIfNeeded()
        {
            if (visualDefaultsCached || visualRoot == null)
            {
                return;
            }

            visualBaseLocalPosition = visualRoot.localPosition;
            visualBaseLocalRotation = visualRoot.localRotation;
            visualBaseLocalScale = visualRoot.localScale;
            visualDefaultsCached = true;
        }

        private void InvalidateAnimatorParamCache()
        {
            animatorParamsCached = false;
            animatorParamsSource = null;
        }

        private void EnsureAnimatorParamHashesForProfile(EnemyPresentationProfile p)
        {
            if (animator == null)
            {
                return;
            }

            if (animatorParamsCached && ReferenceEquals(p, animatorParamsSource))
            {
                return;
            }

            animatorParamsSource = p;
            animatorParamsCached = false;
            if (p == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(p.AnimatorSpeedFloatParam))
            {
                speedParamHash = Animator.StringToHash(p.AnimatorSpeedFloatParam);
            }

            if (!string.IsNullOrEmpty(p.AnimatorHitTriggerParam))
            {
                hitTriggerParamHash = Animator.StringToHash(p.AnimatorHitTriggerParam);
            }

            if (!string.IsNullOrEmpty(p.AnimatorStunnedBoolParam))
            {
                stunnedBoolParamHash = Animator.StringToHash(p.AnimatorStunnedBoolParam);
            }

            animatorParamsCached = true;
        }

        private static float EvaluateHitEnvelope(EnemyPresentationProfile p, float normalizedTime)
        {
            AnimationCurve curve = p != null ? p.HitEnvelope : null;
            if (curve != null && curve.length > 0)
            {
                return Mathf.Clamp01(curve.Evaluate(normalizedTime));
            }

            return Mathf.Clamp01(Mathf.Sin(normalizedTime * Mathf.PI));
        }

        private void UpdateAnimator(EnemyPresentationProfile p, bool isMoving, bool stunned)
        {
            if (animator == null || p == null || !animatorParamsCached)
            {
                return;
            }

            if (!string.IsNullOrEmpty(p.AnimatorSpeedFloatParam))
            {
                animator.SetFloat(speedParamHash, isMoving ? 1f : 0f);
            }

            if (!string.IsNullOrEmpty(p.AnimatorStunnedBoolParam))
            {
                animator.SetBool(stunnedBoolParamHash, stunned);
            }
        }

        private void FireAnimatorHit()
        {
            EnemyPresentationProfile p = profile;
            if (animator == null || p == null || string.IsNullOrEmpty(p.AnimatorHitTriggerParam))
            {
                return;
            }

            EnsureAnimatorParamHashesForProfile(p);
            if (!animatorParamsCached)
            {
                return;
            }

            animator.SetTrigger(hitTriggerParamHash);
        }
        #endregion
    }
}
