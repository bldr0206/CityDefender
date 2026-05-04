using Unity.Cinemachine;
using UnityEngine.Playables;

public class CinemachinePriorityMixerBehaviour : PlayableBehaviour
{
    public CinemachineVirtualCameraBase Camera;

    private PrioritySettings _originalPriority;
    private bool _hasOriginalPriority;

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        CinemachineVirtualCameraBase camera = playerData as CinemachineVirtualCameraBase ?? Camera;
        if (camera == null)
            return;

        Camera = camera;

        if (!_hasOriginalPriority)
        {
            _originalPriority = camera.Priority;
            _hasOriginalPriority = true;
        }

        CinemachinePriorityBehaviour activeClip = GetActiveClip(playable);
        if (activeClip == null)
        {
            RestorePriority();
            return;
        }

        camera.Priority = activeClip.Priority;
    }

    public override void OnGraphStop(Playable playable)
    {
        RestorePriority();
    }

    public override void OnPlayableDestroy(Playable playable)
    {
        RestorePriority();
    }

    private CinemachinePriorityBehaviour GetActiveClip(Playable playable)
    {
        CinemachinePriorityBehaviour activeClip = null;
        float activeWeight = 0;

        int inputCount = playable.GetInputCount();
        for (int i = 0; i < inputCount; i++)
        {
            float weight = playable.GetInputWeight(i);
            if (weight <= activeWeight)
                continue;

            activeWeight = weight;
            activeClip = ((ScriptPlayable<CinemachinePriorityBehaviour>)playable.GetInput(i)).GetBehaviour();
        }

        return activeClip;
    }

    private void RestorePriority()
    {
        if (!_hasOriginalPriority || Camera == null)
            return;

        Camera.Priority = _originalPriority;
        _hasOriginalPriority = false;
    }
}
