using UnityEngine;
using UnityEngine.Playables;

namespace MultitoolTracks
{
    public class LookAtMixerBehaviour : PlayableBehaviour
    {
        bool m_ShouldInitialize = true;
        Quaternion m_InitialRotation;

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var trackBinding = playerData as Transform;
            if (trackBinding == null)
                return;

            InitializeIfNecessary(trackBinding);

            Quaternion accumRotation = Quaternion.identity;
            float totalWeight = 0f;

            int inputCount = playable.GetInputCount();
            for (int i = 0; i < inputCount; i++)
            {
                float inputWeight = playable.GetInputWeight(i);
                if (inputWeight <= 0f)
                    continue;

                Playable input = playable.GetInput(i);
                var lookAtInput = ((ScriptPlayable<LookAtBehaviour>)input).GetBehaviour();

                if (lookAtInput.target == null)
                    continue;

                Vector3 direction = lookAtInput.target.position - trackBinding.position;
                if (direction.sqrMagnitude < 1e-10f)
                    continue;

                Quaternion desiredRotation = Quaternion.LookRotation(direction, lookAtInput.upVector);

                if (totalWeight <= 0f)
                    accumRotation = desiredRotation;
                else
                    accumRotation = Quaternion.Slerp(accumRotation, desiredRotation, inputWeight / (totalWeight + inputWeight));

                totalWeight += inputWeight;
            }

            float blendWeight = Mathf.Clamp01(totalWeight);
            trackBinding.rotation = blendWeight > 0f
                ? Quaternion.Slerp(m_InitialRotation, accumRotation, blendWeight)
                : m_InitialRotation;
        }

        void InitializeIfNecessary(Transform transform)
        {
            if (m_ShouldInitialize)
            {
                m_InitialRotation = transform.rotation;
                m_ShouldInitialize = false;
            }
        }
    }
}
