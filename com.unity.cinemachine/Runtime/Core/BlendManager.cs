using UnityEngine;

namespace Unity.Cinemachine
{
    /// <summary>
    /// This class manages a current active camera and an stack of camera override frames.
    /// It takes care of looking up BlendDefinitions when blends need to be created,
    /// and it generates ActivationEvents for camweras when necessary.
    /// </summary>
    internal class BlendManager : ICameraOverrideStack
    {
        // Current Brain State - result of all frames.  Blend camB is "current" camera always
        CinemachineBlend m_CurrentLiveCameras = new (null, null, null, 0, 0);
        ICinemachineCamera m_OutgoingCamera;
        readonly CameraBlendStack m_BlendStack = new();
        ICinemachineMixer m_Mixer;

        /// <summary>Call this when owner object is enabled</summary>
        public void OnEnable(ICinemachineMixer mixer)
        {
            m_BlendStack.OnEnable();
            m_Mixer = mixer;
        }

         /// <summary>Call this when owner object is disabled</summary>
        public void OnDisable()
        {
            m_BlendStack.OnDisable();
            m_Mixer = null;
        }
        
        /// <summary>Has OnEnable been called?</summary>
        public bool IsInitialized => m_BlendStack.IsInitialized;

        /// <inheritdoc />
        public int SetCameraOverride(
            int overrideId,
            ICinemachineCamera camA, ICinemachineCamera camB,
            float weightB, float deltaTime) => m_BlendStack.SetCameraOverride(overrideId, camA, camB, weightB, deltaTime);

        /// <inheritdoc />
        public void ReleaseCameraOverride(int overrideId) => m_BlendStack.ReleaseCameraOverride(overrideId);
        
        // GML todo: delete this
        /// <summary>Get the default world up for the virtual cameras.</summary>
        public Vector3 DefaultWorldUp => Vector3.up;

        /// <summary>Get the current active virtual camera.</summary>
        public ICinemachineCamera ActiveVirtualCamera => DeepCamBFromBlend(m_CurrentLiveCameras);

        static ICinemachineCamera DeepCamBFromBlend(CinemachineBlend blend)
        {
            ICinemachineCamera vcam = blend.CamB;
            while (vcam != null)
            {
                if (!vcam.IsValid)
                    return null;    // deleted!
                if (vcam is not BlendSourceVirtualCamera bs)
                    break;
                vcam = bs.Blend.CamB;
            }
            return vcam;
        }

        /// <summary>
        /// Get the current blend in progress.  Returns null if none.
        /// It is also possible to set the current blend, but this is not a recommended usage.
        /// </summary>
        public CinemachineBlend ActiveBlend
        {
            get
            {
                if (m_CurrentLiveCameras.CamA == null || m_CurrentLiveCameras.Equals(null) || m_CurrentLiveCameras.IsComplete)
                    return null;
                return m_CurrentLiveCameras;
            }
            set => m_BlendStack.SetRootBlend(value);
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
            if (cam == m_CurrentLiveCameras.CamA)
                return true;
            if (m_CurrentLiveCameras.CamA is BlendSourceVirtualCamera b && b.Blend.Uses(cam))
                return true;
            return false;
        }

        /// <summary>
        /// True if the ICinemachineCamera is the current active camera,
        /// or part of a current blend, either directly or indirectly because its parents are live.
        /// </summary>
        /// <param name="vcam">The camera to test whether it is live</param>
        /// <param name="dominantChildOnly">If true, will only return true if this vcam is the dominant live child</param>
        /// <returns>True if the camera is live (directly or indirectly)
        /// or part of a blend in progress.</returns>
        public bool IsLive(ICinemachineCamera cam, bool dominantChildOnly = false) => m_CurrentLiveCameras.Uses(cam);
        
        /// <summary>Clear the state of the root frame: no current camera, no blend.</summary>
        public void ResetRootFrame() => m_BlendStack.ResetRootFrame();
        
        /// <summary>
        /// Call this every frame with the current active camera of the root frame.
        /// </summary>
        /// <param name="incomingCamera">Current active camera (pre-override)</param>
        /// <param name="deltaTime">How much time has elapsed, for computing blends</param>
        /// <param name="lookupBlend">Delegate to use to find a blend definition, when a blend is being created</param>
        public void UpdateRootFrame(
            ICinemachineCamera incomingCamera, float deltaTime, 
            CinemachineBlendDefinition.LookupBlendDelegate lookupBlend)
        {
            m_BlendStack.UpdateRootFrame(incomingCamera, deltaTime, lookupBlend);
        }

        /// <summary>
        /// Compute the current blend, taking into account
        /// the in-game camera and all the active overrides.  Caller may optionally
        /// exclude n topmost overrides.
        /// </summary>
        public void ComputeCurrentBlend() => m_BlendStack.ComputeCurrentBlend(ref m_CurrentLiveCameras, 0);

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
        public ICinemachineCamera ProcessActiveCamera(Vector3 up, float deltaTime)
        {
            var incomingCamera = ActiveVirtualCamera;
            if (incomingCamera != null && incomingCamera.IsValid)
            {
                // Has the current camera changed this frame?
                if (m_OutgoingCamera != null && !m_OutgoingCamera.IsValid)
                    m_OutgoingCamera = null; // object was deleted
                if (incomingCamera != m_OutgoingCamera)
                {
                    // Generate ActivationEvents
                    var evt = new ICinemachineCamera.ActivationEventParams
                    {
                        Origin = m_Mixer,
                        OutgoingCamera = m_OutgoingCamera,
                        IncomingCamera = incomingCamera,
                        IsCut = !IsBlending || (m_OutgoingCamera != null && !ActiveBlend.Uses(m_OutgoingCamera)),
                        WorldUp = up,
                        DeltaTime = deltaTime
                    };
                    incomingCamera.OnCameraActivated(evt);
                    m_Mixer.OnCameraActivated(evt);
                    CinemachineCore.CameraActivatedEvent.Invoke(evt);

                    // Re-update in case it's inactive
                    incomingCamera.UpdateCameraState(up, deltaTime);
                }
            }
            m_OutgoingCamera = incomingCamera;
            return incomingCamera;
        }

        /// <summary>Get the current state, representing the active camera and blend state.</summary>
        public CameraState CameraState => m_CurrentLiveCameras.State;

        /// <summary>
        /// Get the current active deltaTime override, defined in the topmost override frame.
        /// </summary>
        /// <returns>The active deltaTime override, or -1 for none</returns>
        public float GetDeltaTimeOverride() => m_BlendStack.GetDeltaTimeOverride();
    }
}
