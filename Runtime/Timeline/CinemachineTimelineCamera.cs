#if !UNITY_2019_1_OR_NEWER
#define CINEMACHINE_TIMELINE
#endif
#if CINEMACHINE_TIMELINE

using UnityEngine;
using Cinemachine.Utility;
using System.Collections.Generic;
using System;
using UnityEngine.Playables;

namespace Cinemachine
{
    /// <summary>
    /// This component can be used in combination with <see cref="CinemachineCameraTrack"/>
    /// to blend between multiple cameras using Timeline.
    /// 
    /// Instead of modifying the <see cref="CinemachineBrain"/>'s state in a direct manner
    /// like <see cref="CinemachineBrainTrack"/> does, this component merely acts as another
    /// virtual camera.
    /// Therefore, this camera itself can be blended, mixed or referenced in all other places
    /// where a <see cref="CinemachineVirtualCameraBase"/> or <see cref="ICinemachineCamera"/>
    /// is used.
    /// 
    /// Before playback of an associated <see cref="CinemachineCameraTrack"/> the camera
    /// stays uninitialized. Make sure to update the <see cref="PlayableDirector"/> before you set it Live.
    /// After the track's playback the last known state is being kept.
    /// </summary>
    [DocumentationSorting(DocumentationSortingAttribute.Level.UserRef)]
    [DisallowMultipleComponent]
    [ExecuteAlways]
    [ExcludeFromPreset]
    [AddComponentMenu("Cinemachine/" + nameof(CinemachineTimelineCamera))]
    //[HelpURL(Documentation.BaseURL + "manual/CinemachineMixingCamera.html")]
    public class CinemachineTimelineCamera : CinemachineVirtualCameraBase
    {
        /// <summary>Blended camera state</summary>
        private CameraState m_State = CameraState.Default;

        /// <summary>Keeps track of the previous state</summary>
        private CameraState m_LastState = CameraState.Default;

        /// <summary>Keeps track of the blended cameras, maximum of 2</summary>
        private CinemachineVirtualCameraBase[] m_BlendCameras = new CinemachineVirtualCameraBase[2];

        /// <summary>Current blend weight</summary>
        private float m_BlendWeight;

        /// <summary>The blended CameraState</summary>
        public override CameraState State { get { return m_State; } }

        /// <summary>Not used</summary>
        public override Transform LookAt { get; set; }

        /// <summary>Not used</summary>
        public override Transform Follow { get; set; }

        /// <summary>This is called to notify the vcam that a target got warped,
        /// so that the vcam can update its internal state to make the camera
        /// also warp seamlessy.</summary>
        /// <param name="target">The object that was warped</param>
        /// <param name="positionDelta">The amount the target's position changed</param>
        public override void OnTargetObjectWarped(Transform target, Vector3 positionDelta)
        {
            foreach (var bvcam in m_BlendCameras)
            {
                if (bvcam == null)
                    continue;
                bvcam.OnTargetObjectWarped(target, positionDelta);
            }
            base.OnTargetObjectWarped(target, positionDelta);
        }

        /// <summary>
        /// Force the virtual camera to assume a given position and orientation
        /// </summary>
        /// <param name="pos">Worldspace pposition to take</param>
        /// <param name="rot">Worldspace orientation to take</param>
        public override void ForceCameraPosition(Vector3 pos, Quaternion rot)
        {
            foreach (var bvcam in m_BlendCameras)
            {
                if (bvcam == null)
                    continue;
                bvcam.ForceCameraPosition(pos, rot);
            }
            base.ForceCameraPosition(pos, rot);
        }

        /// <summary>Check whether the vcam a live child of this camera.</summary>
        /// <param name="vcam">The Virtual Camera to check</param>
        /// <param name="dominantChildOnly">If true, will only return true if this vcam is the dominat live child</param>
        /// <returns>True if the vcam is currently actively influencing the state of this vcam</returns>
        public override bool IsLiveChild(ICinemachineCamera vcam, bool dominantChildOnly = false)
        {
            if (dominantChildOnly && m_BlendWeight > 0 && m_BlendWeight < 1)
            {
                if (m_BlendWeight < 0.5)
                    return vcam == (ICinemachineCamera)m_BlendCameras[0];
                else
                    return vcam == (ICinemachineCamera)m_BlendCameras[1];
            }
            else
            {
                foreach (var bvcam in m_BlendCameras)
                {
                    if (bvcam == null)
                        continue;

                    if (vcam == (ICinemachineCamera)bvcam)
                        return true;
                }
            }

            return false;
        }

        /// <summary>Notification that this virtual camera is going live.</summary>
        /// <param name="fromCam">The camera being deactivated.  May be null.</param>
        /// <param name="worldUp">Default world Up, set by the CinemachineBrain</param>
        /// <param name="deltaTime">Delta time for time-based effects (ignore if less than or equal to 0)</param>
        public override void OnTransitionFromCamera(
            ICinemachineCamera fromCam, Vector3 worldUp, float deltaTime)
        {
            base.OnTransitionFromCamera(fromCam, worldUp, deltaTime);
            InvokeOnTransitionInExtensions(fromCam, worldUp, deltaTime);
            foreach (var bvcam in m_BlendCameras)
            {
                if (bvcam == null)
                    continue;
                bvcam.OnTransitionFromCamera(fromCam, worldUp, deltaTime);
            }
            InternalUpdateCameraState(worldUp, deltaTime);
        }

        /// <summary>Internal use only.  Do not call this methid.
        /// Called by CinemachineCore at designated update time
        /// so the vcam can position itself and track its targets.
        /// Blends between two cameras set by <see cref="CinemachineCameraTrack"/>.
        /// </summary>
        /// <param name="worldUp">Default world Up, set by the CinemachineBrain</param>
        /// <param name="deltaTime">Delta time for time-based effects (ignore if less than 0)</param>
        public override void InternalUpdateCameraState(Vector3 worldUp, float deltaTime)
        {
            // Make sure we update our cameras before use.
            int camCount = 0;
            foreach (var bvcam in m_BlendCameras)
            {
                if (bvcam == null)
                    continue;
                camCount++;

                // Check if camera is registered as child of this instance. If it's not, we might need to update it manually.
                if (bvcam.ParentCamera != (ICinemachineCamera)this)
                {
                    // We could also check if bvcam.m_StandbyUpdate == StandbyUpdateMode.Never and warn if it's not.
                    // E.g. damping might not work correctly if updated two times in the same frame as would happen
                    // for StandbyUpdateMode.RoundRobin and StandbyUpdateMode.Always.
                    bvcam.InternalUpdateCameraState(worldUp, deltaTime);
                }
            }

            // If we got two cameras we need to actually blend.
            if (camCount == 2)
                m_State = CameraState.Lerp(m_BlendCameras[0].State, m_BlendCameras[1].State, m_BlendWeight);
            // If there's just the one it has to be the secondary by definition.
            else if (camCount == 1)
                m_State = m_BlendCameras[1].State;
            // Otherwise keep the camera where it is and save its state.
            else
                m_State = m_LastState;

            // We need to store the old state so extensions do not get applied multiple times.
            m_LastState = m_State;

            InvokePostPipelineStageCallback(
                this, CinemachineCore.Stage.Finalize, ref m_State, deltaTime);
        }

        /// <summary>
        /// Blends between two camera's states.
        /// Designed to work with Timeline and <see cref="CinemachineCameraTrack"/>. Not intended for other uses.
        /// </summary>
        /// <param name="camA">First camera or null if there's no blend going on.</param>
        /// <param name="camB">Second or only camera</param>
        /// <param name="blendWeight">The blend weight: (0-1) blends between camA and camB; 1 selects camB; 0 should never happen in context of Timeline</param>
        internal void SetBlendParameters(ICinemachineCamera camA, ICinemachineCamera camB, float blendWeight)
        {
            m_BlendCameras[0] = (CinemachineVirtualCameraBase)camA;
            m_BlendCameras[1] = (CinemachineVirtualCameraBase)camB;
            foreach (var bvcam in m_BlendCameras)
            {
                if (bvcam == null)
                    continue;
                bvcam.EnsureStarted();
            }
            m_BlendWeight = blendWeight;
        }
    }
}
#endif