using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;

[CustomTimelineEditor(typeof(CinemachinePriorityClip))]
public class CinemachinePriorityClipEditor : ClipEditor
{
    public override ClipDrawOptions GetClipOptions(TimelineClip clip)
    {
        ClipDrawOptions options = base.GetClipOptions(clip);
        options.displayClipName = false;
        return options;
    }

    public override void DrawBackground(TimelineClip clip, ClipBackgroundRegion region)
    {
        if (clip.asset is not CinemachinePriorityClip priorityClip)
            return;

        GUI.Label(region.position, $"Camera Priority: {priorityClip.Priority}", EditorStyles.whiteLabel);
    }
}
