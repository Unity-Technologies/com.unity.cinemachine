using System.Collections.Generic;
using UnityEngine;

namespace Unity.Cinemachine
{
    /// <summary>
    /// This class manages a current active camera and an stack of camera override frames.
    /// It takes care of looking up BlendDefinitions when blends need to be created,
    /// and it generates ActivationEvents for camweras when necessary.
    /// </summary>
    class BlendManager : CameraBlendStack
    {
        // Current blend State - result of all frames.  Blend camB is "current" camera always
        CinemachineBlend m_CurrentLiveCameras = new ();

        // Blend state last frame, used for computing deltas
        CinemachineBlend m_PreviousLiveCameras = new ();

        // This is to control GC allocs when generating camera deactivated events
        List<ICinemachineCamera> m_CameraCache = new();

        /// <summary>Get the current active virtual camera.</summary>
        public ICinemachineCamera ActiveVirtualCamera => DeepCamBFromBlend(m_CurrentLiveCameras);

        static ICinemachineCamera DeepCamBFromBlend(CinemachineBlend blend)
        {
            var cam = blend?.CamB;
            while (cam is NestedBlendSource bs)
                cam = bs.Blend.CamB;
            if (cam != null && cam.IsValid)
                return cam;
            return null;
        }

        /// <summary>
        /// Get the current blend in progress.  Returns null if none.
        /// It is also possible to set the current blend, but this is not a recommended usage.
        /// </summary>
        public CinemachineBlend ActiveBlend
        {
            get
            {
                if (m_CurrentLiveCameras.CamA == null || m_CurrentLiveCameras.IsComplete)
                    return null;
                return m_CurrentLiveCameras;
            }
            set => SetRootBlend(value);
        }

        /// <summary>
        /// Is there a blend in progress?
        /// </summary>
        public bool IsBlending => ActiveBlend != null;

        /// <summary>Describe the current blend state</summary>
        public string Description
        {
            get
            {
                if (ActiveVirtualCamera == null)
                    return "[(none)]";
                var sb = CinemachineDebug.SBFromPool();
                sb.Append("[");
                sb.Append(IsBlending ? ActiveBlend.Description : ActiveVirtualCamera.Name);
                sb.Append("]");
                var text = sb.ToString();
                CinemachineDebug.ReturnToPool(sb);
                return text;
            }
        }

        /// <summary>
        /// Checks if the vcam is live as part of an outgoing blend.  
        /// Does not check whether the vcam is also the current active vcam.
        /// </summary>
        /// <param name="vcam">The virtual camera to check</param>
        /// <returns>True if the virtual camera is part of a live outgoing blend, false otherwise</returns>
        public bool IsLiveInBlend(ICinemachineCamera cam)
        {
            // Ignore m_CurrentLiveCameras.CamB
            if (cam != null)
            {
                if (cam == m_CurrentLiveCameras.CamA)
                    return true;
                if (m_CurrentLiveCameras.CamA is NestedBlendSource b && b.Blend.Uses(cam))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// True if the ICinemachineCamera is the current active camera,
        /// or part of a current blend, either directly or indirectly because its parents are live.
        /// </summary>
        /// <param name="vcam">The camera to test whether it is live</param>
        /// <returns>True if the camera is live (directly or indirectly)
        /// or part of a blend in progress.</returns>
        public bool IsLive(ICinemachineCamera cam) => m_CurrentLiveCameras.Uses(cam);

        /// <summary>Get the current state, representing the active camera and blend state.</summary>
        public CameraState CameraState => m_CurrentLiveCameras.State;

        /// <summary>
        /// Compute the current blend, taking into account
        /// the in-game camera and all the active overrides.  Caller may optionally
        /// exclude n topmost overrides.
        /// </summary>
        public void ComputeCurrentBlend() 
        {
            m_PreviousLiveCameras.CopyFrom(m_CurrentLiveCameras);
            ProcessOverrideFrames(ref m_CurrentLiveCameras, 0);
        }

        /// <summary>
        /// Special support for fixed update: cameras that have been enabled
        /// since the last physics frame can be updated now.
        /// <param name="up">Current world up</param>
        /// <param name="deltaTime">Current delta time for this update frame</param>
        /// </summary>
        public void RefreshCurrentCameraState(Vector3 up, float deltaTime) 
            => m_CurrentLiveCameras.UpdateCameraState(up, deltaTime);

        /// <summary>
        /// Once the current blend has been computed, process it and generate camera activation events.
        /// </summary>
        /// <param name="up">Current world up</param>
        /// <param name="deltaTime">Current delta time for this update frame</param>
        /// <returns>Active virtual camera this frame</returns>
        public ICinemachineCamera ProcessActiveCamera(ICinemachineMixer mixer, Vector3 up, float deltaTime)
        {
            // Send deactivation events
            m_CameraCache.Clear();
            CollectLiveCameras(m_PreviousLiveCameras, ref m_CameraCache);
            for (int i = 0; i < m_CameraCache.Count; ++i)
                if (!IsLive(m_CameraCache[i]))
                    CinemachineCore.CameraDeactivatedEvent.Invoke(mixer, m_CameraCache[i]);

            // Process newly activated cameras
            var incomingCamera = ActiveVirtualCamera;
            if (incomingCamera != null && incomingCamera.IsValid)
            {
                // Has the current camera changed this frame?
                var outgoingCamera = DeepCamBFromBlend(m_PreviousLiveCameras);
                if (outgoingCamera != null && !outgoingCamera.IsValid)
                    outgoingCamera = null; // object was deleted

                if (incomingCamera == outgoingCamera)
                {
                    // Send a blend completeed event if appropriate
                    if (m_PreviousLiveCameras.CamA != null && m_CurrentLiveCameras.CamA == null)
                        CinemachineCore.BlendFinishedEvent.Invoke(mixer, incomingCamera);
                }
                else
                {
                    // Generate ActivationEvents
                    var eventOutgoing = outgoingCamera;
                    if (IsBlending)
                    {
                        eventOutgoing = new NestedBlendSource(ActiveBlend);
                        eventOutgoing.UpdateCameraState(up, deltaTime);
                    }
                    var evt = new ICinemachineCamera.ActivationEventParams
                    {
                        Origin = mixer,
                        OutgoingCamera = eventOutgoing,
                        IncomingCamera = incomingCamera,
                        IsCut = !IsBlending,// does not work with snapshots: || !IsLive(outgoingCamera),
                        WorldUp = up,
                        DeltaTime = deltaTime
                    };
                    incomingCamera.OnCameraActivated(evt);
                    mixer.OnCameraActivated(evt);
                    CinemachineCore.CameraActivatedEvent.Invoke(evt);

                    // Re-update in case it's inactive
                    incomingCamera.UpdateCameraState(up, deltaTime);
                }
            }
            return incomingCamera;

            // local method - find all the live cameras in a blend
            static void CollectLiveCameras(CinemachineBlend blend, ref List<ICinemachineCamera> cams)
            {
                if (blend.CamA is NestedBlendSource a && a.Blend != null)
                    CollectLiveCameras(a.Blend, ref cams);
                else if (blend.CamA != null)
                    cams.Add(blend.CamA);

                if (blend.CamB is NestedBlendSource b && b.Blend != null)
                    CollectLiveCameras(b.Blend, ref cams);
                else if (blend.CamB != null)
                    cams.Add(blend.CamB);
            }
        }
    }
}
