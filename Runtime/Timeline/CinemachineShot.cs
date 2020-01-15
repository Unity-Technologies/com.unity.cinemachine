#if !UNITY_2019_1_OR_NEWER
#define CINEMACHINE_TIMELINE
#endif
#if CINEMACHINE_TIMELINE

using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Cinemachine;

//namespace Cinemachine.Timeline
//{
/// <summary>
/// Internal use only.  Not part of the public API.
/// </summary>
    public sealed class CinemachineShot : PlayableAsset, IPropertyPreview
    {
#if !UNITY_2019_2_OR_NEWER
    [HideInInspector]
#endif
    public string DisplayName;

    public ExposedReference<CinemachineVirtualCameraBase> VirtualCamera;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<CinemachineShotPlayable>.Create(graph);
            playable.GetBehaviour().VirtualCamera = VirtualCamera.Resolve(graph.GetResolver());
            return playable;
        }

        // IPropertyPreview implementation
        public void GatherProperties(PlayableDirector director, IPropertyCollector driver)
        {
            driver.AddFromName<Transform>("m_LocalPosition.x");
            driver.AddFromName<Transform>("m_LocalPosition.y");
            driver.AddFromName<Transform>("m_LocalPosition.z");
            driver.AddFromName<Transform>("m_LocalRotation.x");
            driver.AddFromName<Transform>("m_LocalRotation.y");
            driver.AddFromName<Transform>("m_LocalRotation.z");

            driver.AddFromName<Camera>("field of view");
            driver.AddFromName<Camera>("near clip plane");
            driver.AddFromName<Camera>("far clip plane");
        }
    }
//}
#endif
