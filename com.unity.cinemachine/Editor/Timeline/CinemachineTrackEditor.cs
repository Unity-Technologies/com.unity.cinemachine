using UnityEditor.Timeline;
using UnityEngine.Timeline;

namespace Cinemachine.Editor
{
    [CustomTimelineEditor(typeof(CinemachineTrack))]
    public class CinemachineTrackEditor : TrackEditor
    {
        public override void OnCreate(TrackAsset track, TrackAsset copiedFrom)
        {
            base.OnCreate(track, copiedFrom);
        }
    }
}
