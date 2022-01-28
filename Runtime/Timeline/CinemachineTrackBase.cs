#if !UNITY_2019_1_OR_NEWER
#define CINEMACHINE_TIMELINE
#endif
#if CINEMACHINE_TIMELINE

using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

internal abstract class CinemachineTrackBase<T> : TrackAsset
    where T : class, IPlayableBehaviour, new()
{
    /// <summary>
    /// TrackAsset implementation
    /// </summary>
    /// <param name="graph"></param>
    /// <param name="go"></param>
    /// <param name="inputCount"></param>
    /// <returns></returns>
    public override Playable CreateTrackMixer(
        PlayableGraph graph, GameObject go, int inputCount)
    {
#if !UNITY_2019_2_OR_NEWER
            // Hack to set the display name of the clip to match the vcam
            foreach (var c in GetClips())
            {
                CinemachineShot shot = (CinemachineShot)c.asset;
                CinemachineVirtualCameraBase vcam = shot.VirtualCamera.Resolve(graph.GetResolver());
                if (vcam != null)
                    c.displayName = vcam.Name;
            }
#endif
        var mixer = ScriptPlayable<T>.Create(graph);
        mixer.SetInputCount(inputCount);
        return mixer;
    }
}
#endif
