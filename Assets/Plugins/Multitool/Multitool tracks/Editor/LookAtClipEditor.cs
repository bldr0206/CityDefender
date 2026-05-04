using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;

namespace MultitoolTracks
{
    [CustomTimelineEditor(typeof(LookAtClip))]
    public class LookAtClipEditor : ClipEditor
    {
        public override ClipDrawOptions GetClipOptions(TimelineClip clip)
        {
            var options = base.GetClipOptions(clip);
            options.displayClipName = string.IsNullOrEmpty(GetTargetLabel(clip));
            return options;
        }

        public override void DrawBackground(TimelineClip clip, ClipBackgroundRegion region)
        {
            var label = GetTargetLabel(clip);
            if (!string.IsNullOrEmpty(label))
                EditorGUI.LabelField(region.position, label, EditorStyles.miniLabel);
        }

        static string GetTargetLabel(TimelineClip clip)
        {
            var asset = clip.asset as LookAtClip;
            if (asset == null)
                return null;

            var director = TimelineEditor.inspectedDirector;
            if (director == null)
                return null;

            var target = director.GetReferenceValue(asset.target.exposedName, out bool found) as Transform;
            if (found && target != null)
                return $"LookAt {target.gameObject.name}";

            return null;
        }
    }
}
