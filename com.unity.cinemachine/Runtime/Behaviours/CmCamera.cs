using UnityEngine;
using System;

namespace Cinemachine
{
    /// <summary>
    /// This behaviour is intended to be attached to an empty GameObject,
    /// and it represents a Cinemachine Camera within the Unity scene.
    ///
    /// The CM Camera will animate its Transform according to the rules contained
    /// in its CinemachineComponent pipeline (Aim, Body, and Noise).  When the CM
    /// camera is Live, the Unity camera will assume the position and orientation
    /// of the CM camera.
    ///
    /// A CM camera is not a camera. Instead, it can be thought of as a camera controller,
    /// not unlike a cameraman. It can drive the Unity Camera and control its position,
    /// rotation, lens settings, and PostProcessing effects. Each CM Camera owns
    /// its own Cinemachine Component Pipeline, through which you can provide the instructions
    /// for procedurally tracking specific game objects.  An empty procedural pipeline
    /// will result in a passive CM camera, which can be controlled in the same way as
    /// an ordinary GameObject.
    ///
    /// A CM camera is very lightweight, and does no rendering of its own. It merely
    /// tracks interesting GameObjects, and positions itself accordingly. A typical game
    /// can have dozens of CM cameras, each set up to follow a particular character
    /// or capture a particular event.
    ///
    /// A CM Camera can be in any of three states:
    ///
    /// * **Live**: The CM camera is actively controlling the Unity Camera. The
    /// CM camera is tracking its targets and being updated every frame.
    /// * **Standby**: The CM camera is tracking its targets and being updated
    /// every frame, but no Unity Camera is actively being controlled by it. This is
    /// the state of a CM camera that is enabled in the scene but perhaps at a
    /// lower priority than the Live CM camera.
    /// * **Disabled**: The CM camera is present but disabled in the scene. It is
    /// not actively tracking its targets and so consumes no processing power. However,
    /// the CM camera can be made live from the Timeline.
    ///
    /// The Unity Camera can be driven by any CM camera in the scene. The game
    /// logic can choose the CM camera to make live by manipulating the CM
    /// camerass enabled flags and/or its priority, based on game logic.
    ///
    /// In order to be driven by a CM camera, the Unity Camera must have a CinemachineBrain
    /// behaviour, which will select the most eligible CM camera based on its priority
    /// or on other criteria, and will manage blending.
    /// </summary>
    /// 
    [DisallowMultipleComponent]
    [ExecuteAlways]
    [AddComponentMenu("Cinemachine/CmCamera")]
    [HelpURL(Documentation.BaseURL + "manual/CmCamera.html")]
    public sealed class CmCamera : CinemachineVirtualCameraBase, ISerializationCallbackReceiver
    {
        /// <summary>The Tracking and LookAt targets for this camera.</summary>
        [NoSaveDuringPlay]
        [Tooltip("Specifies the Tracking and LookAt targets for this camera.")]
        public CameraTarget Target;

        /// <summary>Specifies the LensSettings of this camera.
        /// These settings will be transferred to the Unity camera when the CM Camera is live.</summary>
        [Tooltip("Specifies the lens properties of this Virtual Camera.  This generally mirrors the "
            + "Unity Camera's lens settings, and will be used to drive the Unity camera when the vcam is active.")]
        public LensSettings Lens = LensSettings.Default;

        /// <summary>Parameters that influence how this CmCamera transitions from other CmCameras.</summary>
        [Tooltip("Parameters that influence how this CmCamera transitions from other CmCameras")]
        [HideFoldout]
        public TransitionParams Transitions;

        void Reset()
        {
            Target = default;
            PriorityAndChannel = OutputChannel.Default;
            Lens = LensSettings.Default;
        }

        /// <summary>Validates the settings avter inspector edit</summary>
        void OnValidate()
        {
            Lens.Validate();
        }

        /// <summary>The current camera state, which will applied to the Unity Camera</summary>
        public override CameraState State { get => m_State; }

        /// <summary>The current camera state, which will applied to the Unity Camera</summary>
        CameraState m_State = CameraState.Default;

        /// <summary>Get the current LookAt target.  Returns parent's LookAt if parent
        /// is non-null and no specific LookAt defined for this camera</summary>
        public override Transform LookAt
        {
            get { return ResolveLookAt(Target.CustomLookAtTarget ? Target.LookAtTarget : Target.TrackingTarget); }
            set { Target.CustomLookAtTarget = true; Target.LookAtTarget = value; }
        }

        /// <summary>Get the current Follow target.  Returns parent's Follow if parent
        /// is non-null and no specific Follow defined for this camera</summary>
        public override Transform Follow
        {
            get { return ResolveFollow(Target.TrackingTarget); }
            set { Target.TrackingTarget = value; }
        }

        /// <summary>Returns the TransitionParams settings</summary>
        /// <returns>The TransitionParams settings</returns>
        public override TransitionParams GetTransitionParams() => Transitions;

        /// <summary>This is called to notify the CM camera that a target got warped,
        /// so that the CM camera can update its internal state to make the camera
        /// also warp seamlessly.</summary>
        /// <param name="target">The object that was warped</param>
        /// <param name="positionDelta">The amount the target's position changed</param>
        public override void OnTargetObjectWarped(Transform target, Vector3 positionDelta)
        {
            if (target == Follow)
            {
                transform.position += positionDelta;
                m_State.RawPosition += positionDelta;
            }
            
            UpdatePipelineCache();
            for (int i = 0; i < m_Pipeline.Length; ++i)
            {
                if (m_Pipeline[i] != null)
                    m_Pipeline[i].OnTargetObjectWarped(target, positionDelta);
            }
            base.OnTargetObjectWarped(target, positionDelta);
        }

        /// <summary>
        /// Force the CM camera to assume a given position and orientation
        /// </summary>
        /// <param name="pos">Worldspace position to take</param>
        /// <param name="rot">Worldspace orientation to take</param>
        public override void ForceCameraPosition(Vector3 pos, Quaternion rot)
        {
            PreviousStateIsValid = false;
            transform.SetPositionAndRotation(pos, rot);
            m_State.RawPosition = pos;
            m_State.RawOrientation = rot;

            UpdatePipelineCache();
            for (int i = 0; i < m_Pipeline.Length; ++i)
                if (m_Pipeline[i] != null)
                    m_Pipeline[i].ForceCameraPosition(pos, rot);

            base.ForceCameraPosition(pos, rot);
        }
        
        /// <summary>
        /// Query components and extensions for the maximum damping time.
        /// </summary>
        /// <returns>Highest damping setting in this CM camera</returns>
        public override float GetMaxDampTime()
        {
            float maxDamp = base.GetMaxDampTime();
            UpdatePipelineCache();
            for (int i = 0; i < m_Pipeline.Length; ++i)
                if (m_Pipeline[i] != null)
                    maxDamp = Mathf.Max(maxDamp, m_Pipeline[i].GetMaxDampTime());
            return maxDamp;
        }

        /// <summary>Handle transition from another CM camera.  InheritPosition is implemented here.</summary>
        /// <param name="fromCam">The camera being deactivated.  May be null.</param>
        /// <param name="worldUp">Default world Up, set by the CinemachineBrain</param>
        /// <param name="deltaTime">Delta time for time-based effects (ignore if less than or equal to 0)</param>
        public override void OnTransitionFromCamera(
            ICinemachineCamera fromCam, Vector3 worldUp, float deltaTime)
        {
            base.OnTransitionFromCamera(fromCam, worldUp, deltaTime);
            InvokeOnTransitionInExtensions(fromCam, worldUp, deltaTime);
            bool forceUpdate = false;

            // Cant't inherit position if already live, because there will be a pop
            if (Transitions.InheritPosition && fromCam != null && !CinemachineCore.Instance.IsLiveInBlend(this))
            {
                var state = fromCam.State;
                ForceCameraPosition(state.GetFinalPosition(), state.GetFinalOrientation());
            }
            UpdatePipelineCache();
            for (int i = 0; i < m_Pipeline.Length; ++i)
                if (m_Pipeline[i] != null && m_Pipeline[i].OnTransitionFromCamera(fromCam, worldUp, deltaTime, ref Transitions))
                    forceUpdate = true;

            if (!forceUpdate)
                UpdateCameraState(worldUp, deltaTime);
            else
            {
                // GML todo: why twice?  Isn't once enough?  Check this.
                InternalUpdateCameraState(worldUp, deltaTime);
                InternalUpdateCameraState(worldUp, deltaTime);
            }

            if (Transitions.Events.OnCameraLive != null)
                Transitions.Events.OnCameraLive.Invoke(this, fromCam);
        }

        /// <summary>Internal use only.  Called by CinemachineCore at designated update time
        /// so the vcam can position itself and track its targets.</summary>
        /// <param name="worldUp">Default world Up, set by the CinemachineBrain</param>
        /// <param name="deltaTime">Delta time for time-based effects (ignore if less than 0)</param>
        public override void InternalUpdateCameraState(Vector3 worldUp, float deltaTime)
        {
            UpdateTargetCache();

            FollowTargetAttachment = 1;
            LookAtTargetAttachment = 1;
            if (deltaTime < 0)
                PreviousStateIsValid = false;

            // Initialize the camera state, in case the game object got moved in the editor
            m_State = PullStateFromVirtualCamera(worldUp, ref Lens);

            // Do our stuff
            var lookAt = LookAt;
            if (lookAt != null)
                m_State.ReferenceLookAt = (LookAtTargetAsVcam != null) 
                    ? LookAtTargetAsVcam.State.GetFinalPosition() : TargetPositionCache.GetTargetPosition(lookAt);
            InvokeComponentPipeline(ref m_State, deltaTime);
            ApplyPositionBlendMethod(ref m_State, Transitions.BlendHint);

            // Push the raw position back to the game object's transform, so it
            // moves along with the camera.
            if (Follow != null)
                transform.position = State.RawPosition;
            if (LookAt != null)
                transform.rotation = State.RawOrientation;
            
            // Signal that it's all done
            PreviousStateIsValid = true;
        }

        CameraState InvokeComponentPipeline(ref CameraState state, float deltaTime)
        {
            // Extensions first
            InvokePrePipelineMutateCameraStateCallback(this, ref state, deltaTime);

            // Apply the component pipeline
            UpdatePipelineCache();
            for (CinemachineCore.Stage stage = CinemachineCore.Stage.Body;
                stage <= CinemachineCore.Stage.Finalize; ++stage)
            {
                var c = m_Pipeline[(int)stage];
                if (c != null && c.IsValid)
                    c.PrePipelineMutateCameraState(ref state, deltaTime);
            }
            CinemachineComponentBase postAimBody = null;
            for (CinemachineCore.Stage stage = CinemachineCore.Stage.Body;
                stage <= CinemachineCore.Stage.Finalize; ++stage)
            {
                var c = m_Pipeline[(int)stage];
                if (c != null && c.IsValid)
                {
                    if (stage == CinemachineCore.Stage.Body && c.BodyAppliesAfterAim)
                    {
                        postAimBody = c;
                        continue; // do the body stage of the pipeline after Aim
                    }
                    c.MutateCameraState(ref state, deltaTime);
                }
                InvokePostPipelineStageCallback(this, stage, ref state, deltaTime);
                if (stage == CinemachineCore.Stage.Aim)
                {
                    if (c == null)
                        state.BlendHint |= CameraState.BlendHintValue.IgnoreLookAtTarget; // no aim
                    // If we have saved a Body for after Aim, do it now
                    if (postAimBody != null)
                    {
                        postAimBody.MutateCameraState(ref state, deltaTime);
                        InvokePostPipelineStageCallback(this, CinemachineCore.Stage.Body, ref state, deltaTime);
                    }
                }
            }

            return state;
        }

        /// <summary>A CinemachineComponentBase has just been added or removed.  Pipeline cache will be rebuilt</summary>
        internal void InvalidatePipelineCache() => m_Pipeline = null;

        /// For unit tests
        internal bool PipelineCacheInvalidated => m_Pipeline == null;

        /// For unit tests
        internal Type PeekPipelineCacheType(CinemachineCore.Stage stage) 
            => m_Pipeline[(int)stage] == null ? null : m_Pipeline[(int)stage].GetType();

        CinemachineComponentBase[] m_Pipeline;

        void UpdatePipelineCache()
        {
            if (m_Pipeline == null)
            {
                m_Pipeline = new CinemachineComponentBase[Enum.GetValues(typeof(CinemachineCore.Stage)).Length];
                var components = GetComponents<CinemachineComponentBase>();
                for (int i = 0; i < components.Length; ++i)
                {
                    if (components[i].enabled)
                    {
#if UNITY_EDITOR
                        if (m_Pipeline[(int)components[i].Stage] != null)
                            Debug.LogWarning("Multiple " + components[i].Stage + " components on " + name);
#endif
                        m_Pipeline[(int)components[i].Stage] = components[i];
                    }
                }
            }
        }

        /// <summary>Get the component set for a specific stage in the pipeline.</summary>
        /// <param name="stage">The stage for which we want the component</param>
        /// <returns>The Cinemachine component for that stage, or null if not present.</returns>
        public override CinemachineComponentBase GetCinemachineComponent(CinemachineCore.Stage stage)
        {
            UpdatePipelineCache();
            var i = (int)stage;
            return i >= 0 && i < m_Pipeline.Length ? m_Pipeline[i] : null;
        }

        // This prevents the sensor size from dirtying the scene in the event of aspect ratio change
        internal override void OnBeforeSerialize()
        {
            if (!Lens.IsPhysicalCamera) 
                Lens.SensorSize = Vector2.one;
        }
    }
}
