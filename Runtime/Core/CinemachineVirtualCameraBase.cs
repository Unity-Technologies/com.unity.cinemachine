using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Cinemachine
{
    /// <summary>
    /// Base class for a Monobehaviour that represents a Virtual Camera within the Unity scene.
    ///
    /// This is intended to be attached to an empty Transform GameObject.
    /// Inherited classes can be either standalone virtual cameras such
    /// as CinemachineVirtualCamera, or meta-cameras such as
    /// CinemachineClearShot or CinemachineFreeLook.
    ///
    /// A CinemachineVirtualCameraBase exposes a Priority property.  When the behaviour is
    /// enabled in the game, the Virtual Camera is automatically placed in a queue
    /// maintained by the static CinemachineCore singleton.
    /// The queue is sorted by priority.  When a Unity camera is equipped with a
    /// CinemachineBrain behaviour, the brain will choose the camera
    /// at the head of the queue.  If you have multiple Unity cameras with CinemachineBrain
    /// behaviours (say in a split-screen context), then you can filter the queue by
    /// setting the culling flags on the virtual cameras.  The culling mask of the
    /// Unity Camera will then act as a filter for the brain.  Apart from this,
    /// there is nothing that prevents a virtual camera from controlling multiple
    /// Unity cameras simultaneously.
    /// </summary>
    [SaveDuringPlay]
    public abstract class CinemachineVirtualCameraBase : MonoBehaviour, ICinemachineCamera
    {
        /// <summary>Inspector control - Use for hiding sections of the Inspector UI.</summary>
        [HideInInspector, SerializeField, NoSaveDuringPlay]
        public string[] m_ExcludedPropertiesInInspector = new string[] { "m_Script" };

        /// <summary>Inspector control - Use for enabling sections of the Inspector UI.</summary>
        [HideInInspector, SerializeField, NoSaveDuringPlay]
        public CinemachineCore.Stage[] m_LockStageInInspector;

        /// <summary>Version that was last streamed, for upgrading legacy</summary>
        public int ValidatingStreamVersion
        {
            get { return m_OnValidateCalled ? m_ValidatingStreamVersion : CinemachineCore.kStreamingVersion; }
            private set { m_ValidatingStreamVersion = value; }
        }
        private int m_ValidatingStreamVersion = 0;
        private bool m_OnValidateCalled = false;

        [HideInInspector, SerializeField, NoSaveDuringPlay]
        private int m_StreamingVersion;

        /// <summary>The priority will determine which camera becomes active based on the
        /// state of other cameras and this camera.  Higher numbers have greater priority.
        /// </summary>
        [NoSaveDuringPlay]
        [Tooltip("The priority will determine which camera becomes active based on the state of other cameras and this camera.  Higher numbers have greater priority.")]
        public int m_Priority = 10;

        /// <summary>
        /// How often to update a virtual camera when it is in Standby mode
        /// </summary>
        public enum StandbyUpdateMode
        {
            /// <summary>Only update if the virtual camera is Live</summary>
            Never,
            /// <summary>Update the virtual camera every frame, even when it is not Live</summary>
            Always,
            /// <summary>Update the virtual camera occasionally, the exact frequency depends
            /// on how many other virtual cameras are in Standby</summary>
            RoundRobin
        };

        /// <summary>When the virtual camera is not live, this is how often the virtual camera will
        /// be updated.  Set this to tune for performance. Most of the time Never is fine, unless
        /// the virtual camera is doing shot evaluation.
        /// </summary>
        [Tooltip("When the virtual camera is not live, this is how often the virtual camera will be updated.  "
            + "Set this to tune for performance. Most of the time Never is fine, "
            + "unless the virtual camera is doing shot evaluation.")]
        public StandbyUpdateMode m_StandbyUpdate = StandbyUpdateMode.RoundRobin;

        /// <summary>
        /// A delegate to hook into the state calculation pipeline.
        /// This will be called after each pipeline stage, to allow others to hook into the pipeline.
        /// See CinemachineCore.Stage.
        /// </summary>
        /// <param name="extension">The extension to add.</param>
        public virtual void AddExtension(CinemachineExtension extension)
        {
            if (mExtensions == null)
                mExtensions = new List<CinemachineExtension>();
            else
                mExtensions.Remove(extension);
            mExtensions.Add(extension);
        }

        /// <summary>Remove a Pipeline stage hook callback.</summary>
        /// <param name="extension">The extension to remove.</param>
        public virtual void RemoveExtension(CinemachineExtension extension)
        {
            if (mExtensions != null)
                mExtensions.Remove(extension);
        }

        /// <summary> THe extensions connected to this vcam</summary>
        List<CinemachineExtension> mExtensions;

        /// <summary>
        /// Invokes the PostPipelineStageDelegate for this camera, and up the hierarchy for all
        /// parent cameras (if any).
        /// Implementaion must be sure to call this after each pipeline stage, to allow
        /// other services to hook into the pipeline.
        /// See CinemachineCore.Stage.
        /// </summary>
        protected void InvokePostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage,
            ref CameraState newState, float deltaTime)
        {
            if (mExtensions != null)
            {
                for (int i = 0; i < mExtensions.Count; ++i)
                {
                    var e = mExtensions[i];
                    if (e == null)
                    {
                        // Object was deleted (possibly because of Undo in the editor)
                        mExtensions.RemoveAt(i);
                        --i;
                    }
                    else if (e.enabled)
                        e.InvokePostPipelineStageCallback(vcam, stage, ref newState, deltaTime);
                }
            }
            CinemachineVirtualCameraBase parent = ParentCamera as CinemachineVirtualCameraBase;
            if (parent != null)
                parent.InvokePostPipelineStageCallback(vcam, stage, ref newState, deltaTime);
        }

        /// <summary>Get the name of the Virtual Camera.  Base implementation
        /// returns the owner GameObject's name.</summary>
        public string Name { get { return name; } }

        /// <summary>Gets a brief debug description of this virtual camera, for use when displayiong debug info</summary>
        public virtual string Description { get { return ""; }}

        /// <summary>Get the Priority of the virtual camera.  This determines its placement
        /// in the CinemachineCore's queue of eligible shots.</summary>
        public int Priority { get { return m_Priority; } set { m_Priority = value; } }

        /// <summary>Hint for blending to and from this virtual camera</summary>
        public enum BlendHint
        {
            /// <summary>Standard linear position and aim blend</summary>
            None,
            /// <summary>Spherical blend about LookAt target position if there is a LookAt target, linear blend between LookAt targets</summary>
            SphericalPosition,
            /// <summary>Cylindrical blend about LookAt target position if there is a LookAt target (vertical co-ordinate is linearly interpolated), linear blend between LookAt targets</summary>
            CylindricalPosition,
            /// <summary>Standard linear position blend, radial blend between LookAt targets</summary>
            ScreenSpaceAimWhenTargetsDiffer
        }

        /// <summary>Applies a position blend hint to the camera state</summary>
        protected void ApplyPositionBlendMethod(ref CameraState state, BlendHint hint)
        {
            switch (hint)
            {
                default:
                    break;
                case BlendHint.SphericalPosition:
                    state.BlendHint |= CameraState.BlendHintValue.SphericalPositionBlend;
                    break;
                case BlendHint.CylindricalPosition:
                    state.BlendHint |= CameraState.BlendHintValue.CylindricalPositionBlend;
                    break;
                case BlendHint.ScreenSpaceAimWhenTargetsDiffer:
                    state.BlendHint |= CameraState.BlendHintValue.RadialAimBlend;
                    break;
            }
        }

        /// <summary>The GameObject owner of the Virtual Camera behaviour.</summary>
        public GameObject VirtualCameraGameObject
        {
            get
            {
                if (this == null)
                    return null; // object deleted
                return gameObject;
            }
        }

        public bool IsValid { get { return !(this == null); } }

        /// <summary>The CameraState object holds all of the information
        /// necessary to position the Unity camera.  It is the output of this class.</summary>
        public abstract CameraState State { get; }

        /// <summary>Support for meta-virtual-cameras.  This is the situation where a
        /// virtual camera is in fact the public face of a private army of virtual cameras, which
        /// it manages on its own.  This method gets the VirtualCamera owner, if any.
        /// Private armies are implemented as Transform children of the parent vcam.</summary>
        public ICinemachineCamera ParentCamera
        {
            get
            {
                if (!mSlaveStatusUpdated || !Application.isPlaying)
                    UpdateSlaveStatus();
                return m_parentVcam;
            }
        }

        /// <summary>Check whether the vcam a live child of this camera.
        /// This base class implementation always returns false.</summary>
        /// <param name="vcam">The Virtual Camera to check</param>
        /// <returns>True if the vcam is currently actively influencing the state of this vcam</returns>
        public virtual bool IsLiveChild(ICinemachineCamera vcam) { return false; }

        /// <summary>Get the LookAt target for the Aim component in the Cinemachine pipeline.</summary>
        public abstract Transform LookAt { get; set; }

        /// <summary>Get the Follow target for the Body component in the Cinemachine pipeline.</summary>
        public abstract Transform Follow { get; set; }

        /// <summary>Set this to force the next update to ignore deltaTime and reset itself</summary>
        public bool PreviousStateIsValid
        {
            get
            {
                if (LookAt != m_previousLookAtTarget)
                {
                    m_previousLookAtTarget = LookAt;
                    m_previousStateIsValid = false;
                }
                if (Follow != m_previousFollowTarget)
                {
                    m_previousFollowTarget = Follow;
                    m_previousStateIsValid = false;
                }
                return m_previousStateIsValid;
            }
            set
            {
                m_previousStateIsValid = value;
            }
        }
        private bool m_previousStateIsValid;
        private Transform m_previousLookAtTarget;
        private Transform m_previousFollowTarget;

        /// <summary>
        /// Update the camera's state.
        /// The implementation must guarantee against multiple calls per frame, and should
        /// use CinemachineCore.UpdateVirtualCamera(ICinemachineCamera, Vector3, float), which
        /// has protection against multiple calls per frame.
        /// </summary>
        /// <param name="worldUp">Default world Up, set by the CinemachineBrain</param>
        /// <param name="deltaTime">Delta time for time-based effects (ignore if less than 0)</param>
        public void UpdateCameraState(Vector3 worldUp, float deltaTime)
        {
            CinemachineCore.Instance.UpdateVirtualCamera(this, worldUp, deltaTime);
        }

        /// <summary>Internal use only.  Do not call this method.
        /// Called by CinemachineCore at designated update time
        /// so the vcam can position itself and track its targets.
        /// Do not call this method.  Let the framework do it at the appropriate time</summary>
        /// <param name="worldUp">Default world Up, set by the CinemachineBrain</param>
        /// <param name="deltaTime">Delta time for time-based effects (ignore if less than 0)</param>
        public abstract void InternalUpdateCameraState(Vector3 worldUp, float deltaTime);

        /// <summary> Collection of parameters that influence how this virtual camera transitions from
        /// other virtual cameras </summary>
        [Serializable]
        public struct TransitionParams
        {
            /// <summary>Hint for blending positions to and from this virtual camera</summary>
            [Tooltip("Hint for blending positions to and from this virtual camera")]
            [FormerlySerializedAs("m_PositionBlending")]
            public BlendHint m_BlendHint;

            /// <summary>When this virtual camera goes Live, attempt to force the position to be the same as the current position of the Unity Camera</summary>
            [Tooltip("When this virtual camera goes Live, attempt to force the position to be the same as the current position of the Unity Camera")]
            public bool m_InheritPosition;

            /// <summary>This event fires when the virtual camera goes Live</summary>
            [Tooltip("This event fires when the virtual camera goes Live")]
            public CinemachineBrain.VcamActivatedEvent m_OnCameraLive;
        }

        /// <summary>Notification that this virtual camera is going live.
        /// Base class implementation must be called by any overridden method.</summary>
        /// <param name="fromCam">The camera being deactivated.  May be null.</param>
        /// <param name="worldUp">Default world Up, set by the CinemachineBrain</param>
        /// <param name="deltaTime">Delta time for time-based effects (ignore if less than or equal to 0)</param>
        public virtual void OnTransitionFromCamera(
            ICinemachineCamera fromCam, Vector3 worldUp, float deltaTime)
        {
            if (!gameObject.activeInHierarchy)
                PreviousStateIsValid = false;
        }

        /// <summary>Maintains the global vcam registry.  Always call the base class implementation.</summary>
        protected virtual void OnDestroy()
        {
            CinemachineCore.Instance.RemoveActiveCamera(this);
        }

        /// <summary>Base class implementation makes sure the priority queue remains up-to-date.</summary>
        protected virtual void OnTransformParentChanged()
        {
            CinemachineCore.Instance.CameraDestroyed(this);
            CinemachineCore.Instance.CameraAwakened(this);
            UpdateSlaveStatus();
            UpdateVcamPoolStatus();
        }

        /// <summary>Base class implementation does nothing.</summary>
        protected virtual void Start()
        {
        }

        /// <summary>Enforce bounds for fields, when changed in inspector.
        /// Call base class implementation at the beginning of overridden method.
        /// After base method is called, ValidatingStreamVersion will be valid.</summary>
        protected virtual void OnValidate()
        {
            m_OnValidateCalled = true;
            ValidatingStreamVersion = m_StreamingVersion;
            m_StreamingVersion = CinemachineCore.kStreamingVersion;
        }

        /// <summary>Base class implementation adds the virtual camera from the priority queue.</summary>
        protected virtual void OnEnable()
        {
            // Sanity check - if another vcam component is enabled, shut down
            var vcamComponents = GetComponents<CinemachineVirtualCameraBase>();
            for (int i = 0; i < vcamComponents.Length; ++i)
            {
                if (vcamComponents[i].enabled && vcamComponents[i] != this)
                {
                    Debug.LogError(Name
                        + " has multiple CinemachineVirtualCameraBase-derived components.  Disabling "
                        + GetType().Name + ".");
                    enabled = false;
                }
            }
            UpdateSlaveStatus();
            UpdateVcamPoolStatus();    // Add to queue
            if (!CinemachineCore.Instance.IsLive(this))
                PreviousStateIsValid = false;
            CinemachineCore.Instance.CameraAwakened(this);
        }

        /// <summary>Base class implementation makes sure the priority queue remains up-to-date.</summary>
        protected virtual void OnDisable()
        {
            UpdateVcamPoolStatus();    // Remove from queue
            CinemachineCore.Instance.CameraDestroyed(this);
        }

        /// <summary>Base class implementation makes sure the priority queue remains up-to-date.</summary>
        protected virtual void Update()
        {
            if (m_Priority != m_QueuePriority)
                UpdateVcamPoolStatus();
        }

        private bool mSlaveStatusUpdated = false;
        private CinemachineVirtualCameraBase m_parentVcam = null;

        private void UpdateSlaveStatus()
        {
            mSlaveStatusUpdated = true;
            m_parentVcam = null;
            Transform p = transform.parent;
            if (p != null)
                m_parentVcam = p.GetComponent<CinemachineVirtualCameraBase>();
        }

        /// <summary>Returns this vcam's LookAt target, or if that is null, will retrun
        /// the parent vcam's LookAt target.</summary>
        /// <param name="localLookAt">This vcam's LookAt value.</param>
        /// <returns>The same value, or the parent's if null and a parent exists.</returns>
        protected Transform ResolveLookAt(Transform localLookAt)
        {
            Transform lookAt = localLookAt;
            if (lookAt == null && ParentCamera != null)
                lookAt = ParentCamera.LookAt; // Parent provides default
            return lookAt;
        }

        /// <summary>Returns this vcam's Follow target, or if that is null, will retrun
        /// the parent vcam's Follow target.</summary>
        /// <param name="localFollow">This vcam's Follow value.</param>
        /// <returns>The same value, or the parent's if null and a parent exists.</returns>
        protected Transform ResolveFollow(Transform localFollow)
        {
            Transform follow = localFollow;
            if (follow == null && ParentCamera != null)
                follow = ParentCamera.Follow; // Parent provides default
            return follow;
        }

        private int m_QueuePriority = int.MaxValue;
        private void UpdateVcamPoolStatus()
        {
            CinemachineCore.Instance.RemoveActiveCamera(this);
            if (m_parentVcam == null && isActiveAndEnabled)
                CinemachineCore.Instance.AddActiveCamera(this);
            m_QueuePriority = m_Priority;
        }

        /// <summary>When multiple virtual cameras have the highest priority, there is
        /// sometimes the need to push one to the top, making it the current Live camera if
        /// it shares the highest priority in the queue with its peers.
        ///
        /// This happens automatically when a
        /// new vcam is enabled: the most recent one goes to the top of the priority subqueue.
        /// Use this method to push a vcam to the top of its priority peers.
        /// If it and its peers share the highest priority, then this vcam will become Live.</summary>
        public void MoveToTopOfPrioritySubqueue()
        {
            UpdateVcamPoolStatus();
        }

        /// <summary>This is called to notify the component that a target got warped,
        /// so that the component can update its internal state to make the camera
        /// also warp seamlessy.</summary>
        /// <param name="target">The object that was warped</param>
        /// <param name="positionDelta">The amount the target's position changed</param>
        public virtual void OnTargetObjectWarped(Transform target, Vector3 positionDelta)
        {
            // inform the extensions
            if (mExtensions != null)
            {
                for (int i = 0; i < mExtensions.Count; ++i)
                    mExtensions[i].OnTargetObjectWarped(target, positionDelta);
            }
        }

        /// <summary>Create a blend between 2 virtual cameras, taking into account
        /// any existing active blend.</summary>
        protected CinemachineBlend CreateBlend(
            ICinemachineCamera camA, ICinemachineCamera camB,
            CinemachineBlendDefinition blendDef,
            CinemachineBlend activeBlend)
        {
            if (blendDef.BlendCurve == null || blendDef.m_Time <= 0 || (camA == null && camB == null))
                return null;
            if (activeBlend != null)
            {
//                if (activeBlend.Uses(camB))
//                    camA = new StaticPointVirtualCamera(activeBlend.State, "Mid-Blend");
//                else
                    camA = new BlendSourceVirtualCamera(activeBlend);
            }
            else if (camA == null)
                camA = new StaticPointVirtualCamera(State, "(none)");
            return new CinemachineBlend(camA, camB, blendDef.BlendCurve, blendDef.m_Time, 0);
        }

        /// <summary>
        /// Create a camera state based on the current transform of this vcam
        /// </summary>
        /// <param name="worldUp">Current World Up direction, as provided by the brain</param>
        /// <param name="lens">Lens settings to serve as base, will be combined with lens from brain, if any</param>
        /// <returns></returns>
        protected CameraState PullStateFromVirtualCamera(Vector3 worldUp, ref LensSettings lens)
        {
            CameraState state = CameraState.Default;
            state.RawPosition = transform.position;
            state.RawOrientation = transform.rotation;
            state.ReferenceUp = worldUp;

            CinemachineBrain brain = CinemachineCore.Instance.FindPotentialTargetBrain(this);
            if (brain != null)
                lens.SnapshotCameraReadOnlyProperties(brain.OutputCamera);

            state.Lens = lens;
            return state;
        }
    }
}
