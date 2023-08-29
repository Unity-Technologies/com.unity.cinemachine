#if CINEMACHINE_TIMELINE

using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Unity.Cinemachine
{
    /// <summary>
    /// Internal use only.  Not part of the public API.
    /// </summary>
    public sealed class CinemachineShot : PlayableAsset, IPropertyPreview
    {
        /// <summary>The name to display on the track.  If empty, the CmCamera's name will be used.</summary>
        [Tooltip("The name to display on the track.  If empty, the CmCamera's name will be used.")]
        public string DisplayName;

        /// <summary>The Cinemachine camera to use for this shot</summary>
        [Tooltip("The Cinemachine camera to use for this shot")]
        public ExposedReference<CinemachineVirtualCameraBase> VirtualCamera;

        /// <summary>PlayableAsset implementation</summary>
        /// <param name="graph"></param>
        /// <param name="owner"></param>
        /// <returns></returns>
        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<CinemachineShotPlayable>.Create(graph);
            playable.GetBehaviour().VirtualCamera = VirtualCamera.Resolve(graph.GetResolver());
            return playable;
        }

        /// <summary>IPropertyPreview implementation</summary>
        /// <param name="director"></param>
        /// <param name="driver"></param>
        public void GatherProperties(PlayableDirector director, IPropertyCollector driver)
        {
            driver.AddFromName<Transform>("m_LocalPosition.x");
            driver.AddFromName<Transform>("m_LocalPosition.y");
            driver.AddFromName<Transform>("m_LocalPosition.z");
            driver.AddFromName<Transform>("m_LocalRotation.x");
            driver.AddFromName<Transform>("m_LocalRotation.y");
            driver.AddFromName<Transform>("m_LocalRotation.z");
            driver.AddFromName<Transform>("m_LocalRotation.w");

            driver.AddFromName<Camera>("field of view");
            driver.AddFromName<Camera>("near clip plane");
            driver.AddFromName<Camera>("far clip plane");
        }
    }
}
#endif
