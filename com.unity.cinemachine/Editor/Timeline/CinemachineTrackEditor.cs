#if CINEMACHINE_TIMELINE
using UnityEditor.Timeline;
using UnityEngine.Timeline;

namespace Unity.Cinemachine.Editor
{
    [CustomTimelineEditor(typeof(CinemachineTrack))]
    class CinemachineTrackEditor : TrackEditor
    {
        public override void OnCreate(TrackAsset track, TrackAsset copiedFrom)
        {
            base.OnCreate(track, copiedFrom);
            if (CinemachineCore.BrainCount == 1)
                TimelineEditor.inspectedDirector.SetGenericBinding(track, CinemachineCore.GetActiveBrain(0));
        }
    }
}
#endif
