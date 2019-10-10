#if !UNITY_2019_1_OR_NEWER
#define CINEMACHINE_TIMELINE
#endif

#if CINEMACHINE_TIMELINE && UNITY_2019_2_OR_NEWER

using UnityEngine.Timeline;
using UnityEditor.Timeline;

[CustomTimelineEditor(typeof(CinemachineShot))]
public class CinemachineShotClipEditor : ClipEditor
{
    public override ClipDrawOptions GetClipOptions(TimelineClip clip)
    {
        var shotClip = (CinemachineShot) clip.asset;
        var clipOptions = base.GetClipOptions(clip);
        if (shotClip != null)
        {
            var director = TimelineEditor.inspectedDirector;
            if (director != null)
            {
                var vcam = shotClip.VirtualCamera.Resolve(director);
                if (vcam == null)
                    clipOptions.errorText = "A virtual camera must be assigned.";
                else
                    clipOptions.tooltip = vcam.Name;
            }
        }
        return clipOptions;
    }

    public override void OnClipChanged(TimelineClip clip)
    {
        var shotClip = (CinemachineShot) clip.asset;
        if (shotClip == null)
            return;
        if (shotClip.DisplayName.Length != 0)
            clip.displayName = shotClip.DisplayName;
        else
        {
            var director = TimelineEditor.inspectedDirector;
            if (director != null)
            {
                var vcam = shotClip.VirtualCamera.Resolve(director);
                if (vcam != null)
                    clip.displayName = vcam.Name;
            }
        }
    }
}

#endif
