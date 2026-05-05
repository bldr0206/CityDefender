using UnityEngine;
using UnityEngine.Playables;

namespace MultitoolTracks
{
    public class TweenTransformMixerBehaviour : PlayableBehaviour
    {
        bool m_ShouldInitialize = true;
        Vector3 m_InitialPosition;
        Vector3 m_InitialLocalPosition;
        Quaternion m_InitialRotation;
        Quaternion m_InitialLocalRotation;
        Vector3 m_InitialScale;

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var trackBinding = playerData as Transform;
            if (trackBinding == null)
                return;

            InitializeIfNecessary(trackBinding);

            var useLocal = GetSpace(playable) == Space.Local;
            var initialPos = useLocal ? m_InitialLocalPosition : m_InitialPosition;
            var initialRot = useLocal ? m_InitialLocalRotation : m_InitialRotation;

            Vector3 accumPosition = Vector3.zero;
            Vector3 accumScale = Vector3.zero;
            Quaternion accumRotation = Quaternion.identity;

            float totalPosWeight = 0f;
            float totalScaleWeight = 0f;
            float totalRotWeight = 0f;

            int inputCount = playable.GetInputCount();
            for (int i = 0; i < inputCount; i++)
            {
                float inputWeight = playable.GetInputWeight(i);
                if (inputWeight <= 0f)
                    continue;

                Playable input = playable.GetInput(i);
                float progress = (float)(input.GetTime() / input.GetDuration());
                var behaviour = ((ScriptPlayable<TweenTransformBehaviour>)input).GetBehaviour();

                float tPos = EasingUtility.Evaluate(behaviour.positionEasing, progress);
                float tScale = EasingUtility.Evaluate(behaviour.scaleEasing, progress);
                float tRot = EasingUtility.Evaluate(behaviour.rotationEasing, progress);

                if (behaviour.tweenPosition)
                    AccumulatePosition(behaviour, tPos, inputWeight, useLocal, initialPos, ref accumPosition, ref totalPosWeight);
                if (behaviour.tweenScale)
                    AccumulateScale(behaviour, tScale, inputWeight, ref accumScale, ref totalScaleWeight);
                if (behaviour.tweenRotation)
                    AccumulateRotation(behaviour, tRot, inputWeight, useLocal, ref accumRotation, ref totalRotWeight);
            }

            if (totalPosWeight > 0f)
                ApplyPosition(trackBinding, useLocal, accumPosition, totalPosWeight, initialPos);
            if (totalScaleWeight > 0f)
                ApplyScale(trackBinding, accumScale, totalScaleWeight);
            if (totalRotWeight > 0f)
                ApplyRotation(trackBinding, useLocal, accumRotation, totalRotWeight, initialRot);
        }

        void InitializeIfNecessary(Transform t)
        {
            if (m_ShouldInitialize)
            {
                m_InitialPosition = t.position;
                m_InitialLocalPosition = t.localPosition;
                m_InitialRotation = t.rotation;
                m_InitialLocalRotation = t.localRotation;
                m_InitialScale = t.localScale;
                m_ShouldInitialize = false;
            }
        }

        static Space GetSpace(Playable playable)
        {
            if (playable.GetInputCount() == 0)
                return Space.Local;
            var b = ((ScriptPlayable<TweenTransformBehaviour>)playable.GetInput(0)).GetBehaviour();
            return b != null ? b.space : Space.Local;
        }

        void AccumulatePosition(TweenTransformBehaviour b, float t, float weight, bool useLocal, Vector3 initial,
            ref Vector3 accum, ref float totalWeight)
        {
            Vector3 start = GetStartPosition(b, useLocal);
            Vector3 end = GetEndPosition(b, useLocal);
            Vector3 lerped = Vector3.Lerp(start, end, t);

            Vector3 desired = new Vector3(
                GetPositionAxisValue(b, lerped, initial, 0),
                GetPositionAxisValue(b, lerped, initial, 1),
                GetPositionAxisValue(b, lerped, initial, 2)
            );
            accum += desired * weight;
            totalWeight += weight;
        }

        static float GetPositionAxisValue(TweenTransformBehaviour b, Vector3 value, Vector3 initial, int axis)
        {
            if (!EasingUtility.HasAxis(b.positionAxisMode, axis))
                return initial[axis];

            return value[axis];
        }

        void AccumulateScale(TweenTransformBehaviour b, float t, float weight, ref Vector3 accum, ref float totalWeight)
        {
            Vector3 lerped;
            if (b.scaleValueMode == ScaleValueMode.Uniform)
            {
                float u = Mathf.Lerp(b.startScaleUniform, b.endScaleUniform, t);
                lerped = new Vector3(u, u, u);
            }
            else
            {
                lerped = new Vector3(
                    EasingUtility.HasAxis(b.scaleAxisMode, 0) ? Mathf.Lerp(b.startScale.x, b.endScale.x, t) : 1f,
                    EasingUtility.HasAxis(b.scaleAxisMode, 1) ? Mathf.Lerp(b.startScale.y, b.endScale.y, t) : 1f,
                    EasingUtility.HasAxis(b.scaleAxisMode, 2) ? Mathf.Lerp(b.startScale.z, b.endScale.z, t) : 1f
                );
            }

            if (b.scaleMode == ScaleMode.Relative)
            {
                Vector3 desired = new Vector3(
                    m_InitialScale.x * lerped.x,
                    m_InitialScale.y * lerped.y,
                    m_InitialScale.z * lerped.z
                );
                accum += desired * weight;
            }
            else
            {
                Vector3 desired = b.scaleValueMode == ScaleValueMode.Uniform
                    ? lerped
                    : new Vector3(
                        EasingUtility.HasAxis(b.scaleAxisMode, 0) ? lerped.x : m_InitialScale.x,
                        EasingUtility.HasAxis(b.scaleAxisMode, 1) ? lerped.y : m_InitialScale.y,
                        EasingUtility.HasAxis(b.scaleAxisMode, 2) ? lerped.z : m_InitialScale.z
                    );
                accum += desired * weight;
            }
            totalWeight += weight;
        }

        void AccumulateRotation(TweenTransformBehaviour b, float t, float weight, bool useLocal,
            ref Quaternion accum, ref float totalWeight)
        {
            Quaternion startRot = GetStartRotation(b, useLocal);
            Quaternion endRot = GetEndRotation(b, useLocal);
            Quaternion desired;

            if (b.rotationMode == RotationMode.Relative)
            {
                Quaternion deltaRot = Quaternion.Slerp(startRot, endRot, t);
                var initialRot = useLocal ? m_InitialLocalRotation : m_InitialRotation;
                desired = initialRot * deltaRot;
            }
            else
            {
                desired = Quaternion.Slerp(startRot, endRot, t);
            }

            if (totalWeight <= 0f)
                accum = desired;
            else
                accum = Quaternion.Slerp(accum, desired, weight / (totalWeight + weight));
            totalWeight += weight;
        }

        Vector3 GetStartPosition(TweenTransformBehaviour b, bool useLocal)
        {
            if (b.positionSourceMode == SourceMode.Target && b.startTarget != null)
            {
                var pos = useLocal ? b.startTarget.localPosition : b.startTarget.position;
                return pos + b.startPositionOffset;
            }
            return b.startPosition;
        }

        Vector3 GetEndPosition(TweenTransformBehaviour b, bool useLocal)
        {
            if (b.positionSourceMode == SourceMode.Target && b.endTarget != null)
            {
                var pos = useLocal ? b.endTarget.localPosition : b.endTarget.position;
                return pos + b.endPositionOffset;
            }
            return b.endPosition;
        }

        Quaternion GetStartRotation(TweenTransformBehaviour b, bool useLocal)
        {
            if (b.rotationSourceMode == SourceMode.Target && b.startTarget != null)
            {
                var rot = useLocal ? b.startTarget.localRotation : b.startTarget.rotation;
                return rot * Quaternion.Euler(b.startRotationOffsetEuler);
            }
            return Quaternion.Euler(b.startRotationEuler);
        }

        Quaternion GetEndRotation(TweenTransformBehaviour b, bool useLocal)
        {
            if (b.rotationSourceMode == SourceMode.Target && b.endTarget != null)
            {
                var rot = useLocal ? b.endTarget.localRotation : b.endTarget.rotation;
                return rot * Quaternion.Euler(b.endRotationOffsetEuler);
            }
            return Quaternion.Euler(b.endRotationEuler);
        }

        void ApplyPosition(Transform t, bool useLocal, Vector3 accum, float totalWeight, Vector3 initial)
        {
            float w = Mathf.Clamp01(totalWeight);
            Vector3 result = accum + initial * (1f - w);
            if (useLocal)
                t.localPosition = result;
            else
                t.position = result;
        }

        void ApplyScale(Transform t, Vector3 accum, float totalWeight)
        {
            float w = Mathf.Clamp01(totalWeight);
            t.localScale = accum + m_InitialScale * (1f - w);
        }

        void ApplyRotation(Transform t, bool useLocal, Quaternion accum, float totalWeight, Quaternion initial)
        {
            float w = Mathf.Clamp01(totalWeight);
            Quaternion result = w > 0f ? Quaternion.Slerp(initial, accum, w) : initial;
            if (useLocal)
                t.localRotation = result;
            else
                t.rotation = result;
        }
    }
}
