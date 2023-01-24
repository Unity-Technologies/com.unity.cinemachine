#if CINEMACHINE_TIMELINE

using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Cinemachine
{
    /// <summary>
    /// Timeline track for CinemachineCamera activation
    /// </summary>
    [Serializable]
    [TrackClipType(typeof(CinemachineShot))]
    [TrackBindingType(typeof(CinemachineBrain), TrackBindingFlags.None)]
    [TrackColor(0.53f, 0.0f, 0.08f)]
    public class CinemachineTrack : TrackAsset
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
            var mixer = ScriptPlayable<CinemachineMixer>.Create(graph);
            mixer.SetInputCount(inputCount);
            return mixer;
        }
    }
}
#endif
