using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Unity.Cinemachine
{
    /// <summary>
    /// This is a virtual camera "manager" that owns and manages a collection
    /// of child Cm Cameras.
    /// </summary>
    public abstract class CinemachineCameraManagerBase : CinemachineVirtualCameraBase, ICinemachineMixer
    {
        /// <summary>If enabled, a default target will be available.  It will be used
        /// if a child rig needs a target and doesn't specify one itself.</summary>
        [Serializable]
        public struct DefaultTargetSettings
        {
            /// <summary>If enabled, a default target will be available.  It will be used
            /// if a child rig needs a target and doesn't specify one itself.</summary>
            [Tooltip("If enabled, a default target will be available.  It will be used "
                + "if a child rig needs a target and doesn't specify one itself.")]
            public bool Enabled;

            /// <summary>Default target for the camera children, which may be used if the child rig
            /// does not specify a target of its own.</summary>
            [NoSaveDuringPlay]
            [Tooltip("Default target for the camera children, which may be used if the child rig "
                + "does not specify a target of its own.")]
            public CameraTarget Target;
        }

        /// <summary>If enabled, a default target will be available.  It will be used
        /// if a child rig needs a target and doesn't specify one itself.</summary>
        [FoldoutWithEnabledButton]
        public DefaultTargetSettings DefaultTarget;

        /// <summary>
        /// The blend which is used if you don't explicitly define a blend between two Virtual Camera children.
        /// </summary>
        [Tooltip("The blend which is used if you don't explicitly define a blend between two Virtual Camera children")]
        [FormerlySerializedAs("m_DefaultBlend")]
        public CinemachineBlendDefinition DefaultBlend = new (CinemachineBlendDefinition.Styles.EaseInOut, 0.5f);

        /// <summary>
        /// This is the asset which contains custom settings for specific child blends.
        /// </summary>
        [Tooltip("This is the asset which contains custom settings for specific child blends")]
        [FormerlySerializedAs("m_CustomBlends")]
        [EmbeddedBlenderSettingsProperty]
        public CinemachineBlenderSettings CustomBlends = null;

        List<CinemachineVirtualCameraBase> m_ChildCameras;
        int m_ChildCountCache; // used for invalidating child cache
        readonly BlendManager m_BlendManager = new ();
        CameraState m_State = CameraState.Default;
        ICinemachineCamera m_TransitioningFrom;

        /// <summary>Reset the component to default values.</summary>
        protected virtual void Reset()
        {
            Priority = default;
            OutputChannel = OutputChannels.Default;
            DefaultTarget = default;
            InvalidateCameraCache();
        }

        /// <summary>
        /// Standard MonoBehaviour OnEnable.  Derived classes must call base class implementation.
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();
            m_BlendManager.OnEnable();
            m_BlendManager.LookupBlendDelegate = LookupBlend;
            InvalidateCameraCache();
        }

        /// <summary>
        /// Standard MonoBehaviour OnDisable.  Derived classes must call base class implementation.
        /// </summary>
        protected override void OnDisable()
        {
            m_BlendManager.OnDisable();
            base.OnDisable();
        }

        /// <inheritdoc />
        public override string Description => m_BlendManager.Description;

        /// <inheritdoc />
        public override CameraState State => m_State;

        /// <inheritdoc />
        public virtual bool IsLiveChild(ICinemachineCamera cam, bool dominantChildOnly = false)
            => m_BlendManager.IsLive(cam);

        /// <summary>The list of child cameras.  These are just the immediate children in the hierarchy.</summary>
        public List<CinemachineVirtualCameraBase> ChildCameras
        {
            get
            {
                UpdateCameraCache();
                return m_ChildCameras;
            }
        }

        /// <inheritdoc />
        public override bool PreviousStateIsValid
        {
            get => base.PreviousStateIsValid;
            set
            {
                base.PreviousStateIsValid = value;
                // Only propagate to the children when we're invalidating the state
                if (value == false)
                    for (int i = 0; m_ChildCameras != null && i < m_ChildCameras.Count; ++i)
                        m_ChildCameras[i].PreviousStateIsValid = value;
            }
        }

        /// <summary>Is there a blend in progress?</summary>
        public bool IsBlending => m_BlendManager.IsBlending;

        /// <summary>
        /// Get the current blend in progress.  Returns null if none.
        /// It is also possible to set the current blend, but this is not a recommended usage
        /// unless it is to set the active blend to null, which will force completion of the blend.
        /// </summary>
        public CinemachineBlend ActiveBlend
        {
            get => PreviousStateIsValid ? m_BlendManager.ActiveBlend : null;
            set => m_BlendManager.ActiveBlend = value;
        }

        /// <summary>
        /// Get the current active camera.  Will return null if no camera is active.
        /// </summary>
        public ICinemachineCamera LiveChild => PreviousStateIsValid ? m_BlendManager.ActiveVirtualCamera : null;

        /// <summary>Get the current LookAt target.  Returns parent's LookAt if parent
        /// is non-null and no specific LookAt defined for this camera</summary>
        public override Transform LookAt
        {
            get
            {
                if (!DefaultTarget.Enabled)
                    return null;
                return ResolveLookAt(DefaultTarget.Target.CustomLookAtTarget
                    ? DefaultTarget.Target.LookAtTarget : DefaultTarget.Target.TrackingTarget);
            }
            set
            {
                DefaultTarget.Enabled = true;
                DefaultTarget.Target.CustomLookAtTarget = true;
                DefaultTarget.Target.LookAtTarget = value;
            }
        }

        /// <summary>Get the current Follow target.  Returns parent's Follow if parent
        /// is non-null and no specific Follow defined for this camera</summary>
        public override Transform Follow
        {
            get
            {
                if (!DefaultTarget.Enabled)
                    return null;
                return ResolveFollow(DefaultTarget.Target.TrackingTarget);
            }
            set
            {
                DefaultTarget.Enabled = true;
                DefaultTarget.Target.TrackingTarget = value;
            }
        }

        /// <summary>Internal use only.  Do not call this method.
        /// Called by CinemachineCore at designated update time
        /// so the vcam can position itself and track its targets.  This implementation
        /// updates all the children, chooses the best one, and implements any required blending.</summary>
        /// <param name="worldUp">Default world Up, set by the CinemachineBrain</param>
        /// <param name="deltaTime">Delta time for time-based effects (ignore if less than or equal to 0)</param>
        public override void InternalUpdateCameraState(Vector3 worldUp, float deltaTime)
        {
            UpdateTargetCache();
            UpdateCameraCache();
            if (!PreviousStateIsValid)
                ResetLiveChild();

            // Choose the best camera - auto-activate it if it's inactive
            var best = ChooseCurrentCamera(worldUp, deltaTime);
            if (best != null && !best.gameObject.activeInHierarchy)
            {
                best.gameObject.SetActive(true);
                best.UpdateCameraState(worldUp, deltaTime);
            }
            SetLiveChild(best, worldUp, deltaTime);

            // Special case to handle being called from OnTransitionFromCamera() - GML todo: fix this
            if (m_TransitioningFrom != null && !IsBlending && LiveChild != null)
            {
                LiveChild.OnCameraActivated(new ICinemachineCamera.ActivationEventParams
                {
                    Origin = this,
                    OutgoingCamera = m_TransitioningFrom,
                    IncomingCamera = LiveChild,
                    IsCut = false,
                    WorldUp = worldUp,
                    DeltaTime = deltaTime
                });
            }

            FinalizeCameraState(deltaTime);
            m_TransitioningFrom = null;
            PreviousStateIsValid = true;
        }

        /// <summary>Find a blend curve for blending from one child camera to another.</summary>
        /// <param name="outgoing">The camera we're blending from.</param>
        /// <param name="incoming">The camera we're blending to.</param>
        /// <returns>The blend to use for this camera transition.</returns>
        protected virtual CinemachineBlendDefinition LookupBlend(ICinemachineCamera outgoing, ICinemachineCamera incoming)
        {
            return CinemachineBlenderSettings.LookupBlend(outgoing, incoming, DefaultBlend, CustomBlends, this);
        }

        /// <summary>
        /// Choose the appropriate current camera from among the ChildCameras, based on current state.
        /// If the returned camera is different from the current camera, an appropriate transition will be made.
        /// </summary>
        /// <param name="worldUp">Default world Up, set by the CinemachineBrain</param>
        /// <param name="deltaTime">Delta time for time-based effects (ignore if less than or equal to 0)</param>
        /// <returns>The current child camera that should be active. Must be present in ChildCameras.</returns>
        protected abstract CinemachineVirtualCameraBase ChooseCurrentCamera(Vector3 worldUp, float deltaTime);

        /// <summary>This is called to notify the vcam that a target got warped,
        /// so that the vcam can update its internal state to make the camera
        /// also warp seamlessly.</summary>
        /// <param name="target">The object that was warped</param>
        /// <param name="positionDelta">The amount the target's position changed</param>
        public override void OnTargetObjectWarped(Transform target, Vector3 positionDelta)
        {
            UpdateCameraCache();
            for (int i = 0; i < m_ChildCameras.Count; ++i)
                m_ChildCameras[i].OnTargetObjectWarped(target, positionDelta);
            base.OnTargetObjectWarped(target, positionDelta);
        }

        /// <summary>
        /// Force the virtual camera to assume a given position and orientation
        /// </summary>
        /// <param name="pos">World-space position to take</param>
        /// <param name="rot">World-space orientation to take</param>
        public override void ForceCameraPosition(Vector3 pos, Quaternion rot)
        {
            UpdateCameraCache();
            for (int i = 0; i < m_ChildCameras.Count; ++i)
                m_ChildCameras[i].ForceCameraPosition(pos, rot);
            base.ForceCameraPosition(pos, rot);
        }

        /// <summary>Notification that this virtual camera is going live.</summary>
        /// <param name="fromCam">The camera being deactivated.  May be null.</param>
        /// <param name="worldUp">Default world Up, set by the CinemachineBrain</param>
        /// <param name="deltaTime">Delta time for time-based effects (ignore if less than or equal to 0)</param>
        public override void OnTransitionFromCamera(
            ICinemachineCamera fromCam, Vector3 worldUp, float deltaTime)
        {
            base.OnTransitionFromCamera(fromCam, worldUp, deltaTime);
            m_TransitioningFrom  = fromCam;
            InvokeOnTransitionInExtensions(fromCam, worldUp, deltaTime);
            InternalUpdateCameraState(worldUp, deltaTime);
        }

        /// <summary>Force a rebuild of the child camera cache.
        /// Call this if CinemachineCamera children are added or removed dynamically</summary>
        public void InvalidateCameraCache()
        {
            m_ChildCameras = null;
            PreviousStateIsValid = false;
        }

        /// <summary>Rebuild the camera cache if it's been invalidated</summary>
        /// <returns>True if a cache rebuild was performed, false if cache is up to date.</returns>
        protected virtual bool UpdateCameraCache()
        {
            var childCount = transform.childCount;
            if (m_ChildCameras != null && m_ChildCountCache == childCount)
                return false;
            PreviousStateIsValid = false;
            m_ChildCameras = new();
            m_ChildCountCache = childCount;
            GetComponentsInChildren(true, m_ChildCameras);
            for (int i = m_ChildCameras.Count-1; i >= 0; --i)
                if (m_ChildCameras[i].transform.parent != transform)
                    m_ChildCameras.RemoveAt(i);
            return true;
        }

        /// <summary>Makes sure the internal child cache is up to date</summary>
        protected virtual void OnTransformChildrenChanged() => InvalidateCameraCache();

        /// <summary>
        /// Set the current active camera.  All necessary blends will be created, and events generated.
        /// </summary>
        /// <param name="activeCamera">Current active camera</param>
        /// <param name="worldUp">Current world up</param>
        /// <param name="deltaTime">Current deltaTime applicable for this frame</param>
        protected void SetLiveChild(
            ICinemachineCamera activeCamera, Vector3 worldUp, float deltaTime)
        {
            m_BlendManager.UpdateRootFrame(this, activeCamera, worldUp, deltaTime);
            m_BlendManager.ComputeCurrentBlend();
            m_BlendManager.ProcessActiveCamera(this, worldUp, deltaTime);
        }

        /// <summary>Cancel current active camera and all blends</summary>
        protected void ResetLiveChild() => m_BlendManager.ResetRootFrame();

        /// <summary>At the end of InternalUpdateCameraState, call this to finalize the state</summary>
        /// <param name="deltaTime">Current deltaTime for this frame</param>
        protected void FinalizeCameraState(float deltaTime)
        {
            m_State = m_BlendManager.CameraState;
            InvokePostPipelineStageCallback(this, CinemachineCore.Stage.Finalize, ref m_State, deltaTime);
        }
    }
}
