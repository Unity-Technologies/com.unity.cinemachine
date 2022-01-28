#if !UNITY_2019_1_OR_NEWER
#define CINEMACHINE_TIMELINE
#endif
#if CINEMACHINE_TIMELINE

using System;
using UnityEngine.Timeline;
using Cinemachine;

/// <summary>
/// Timeline track for Cinemachine virtual camera activation
/// </summary>
[Serializable]
[TrackClipType(typeof(CinemachineShot))]
[TrackBindingType(typeof(CinemachineTimelineCamera), TrackBindingFlags.AllowCreateComponent)]
[TrackColor(0.53f, 0.0f, 0.08f)]
internal sealed class CinemachineCameraTrack : CinemachineTrackBase<CinemachineCameraMixer>
{ }
#endif
