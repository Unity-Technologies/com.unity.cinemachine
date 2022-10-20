using System;
using System.Collections.Generic;
using System.Linq;
using Cinemachine.Utility;
using UnityEngine;
using UnityEngine.Serialization;

namespace Cinemachine
{
    /// <summary>
    /// Base class for a Monobehaviour that represents a Virtual Camera within the Unity scene.
    ///
    /// This is intended to be attached to an empty Transform GameObject.
    /// Inherited classes can be either standalone virtual cameras such
    /// as CmCamera, or meta-cameras such as
    /// CinemachineClearShot or CinemachineBlendListCamera.
    ///
    /// A CinemachineVirtualCameraBase exposes a OutputChannel property.  When the behaviour is
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
    public abstract class CinemachineVirtualCameraBase : MonoBehaviour, ICinemachineCamera, ISerializationCallbackReceiver
    {
        /// <summary>Priority can be used to control which Cm Camera is live when multiple CM Cameras are 
        /// active simultaneously.  The most-recently-activated CmCamera will take control, unless there 
        /// is another Cm Camera active with a higher priority.  In general, the most-recently-activated 
        /// highest-priority CmCamera will control the main camera. 
        /// 
        /// The default priority is 0.  Often it is sufficient to leave the default setting.  
        /// In special cases where you want a CmCamera to have a higher or lower priority than 0, 
        /// the value can be set here.
        /// </summary>
        [NoSaveDuringPlay]
        [Tooltip("Priority can be used to control which Cm Camera is live when multiple CM Cameras are "
            + "active simultaneously.  The most-recently-activated CmCamera will take control, unless there "
            + "is another Cm Camera active with a higher priority.  In general, the most-recently-activated "
            + "highest-priority CmCamera will control the main camera. \n\n"
            + "The default priority is 0.  Often it is sufficient to leave the default setting.  "
            + "In special cases where you want a CmCamera to have a higher or lower priority than 0, "
            + "the value can be set here.")]
        [FoldoutWithEnabledButton]
        public OutputChannel PriorityAndChannel = OutputChannel.Default;

        /// <summary>A sequence number that represents object activation order of vcams.  
        /// Used for priority sorting.</summary>
        [FormerlySerializedAs("m_ActivationId")]
        internal int ActivationId;

        int m_QueuePriority = int.MaxValue;

        /// <summary>
        /// This must be set every frame at the start of the pipeline to relax the virtual camera's
        /// attachment to the target.  Range is 0...1.  
        /// 1 is full attachment, and is the normal state.
        /// 0 is no attachment, and virtual camera will behave as if no Follow 
        /// targets are set.
        /// </summary>
        [NonSerialized]
        public float FollowTargetAttachment;

        /// <summary>
        /// This must be set every frame at the start of the pipeline to relax the virtual camera's
        /// attachment to the target.  Range is 0...1.  
        /// 1 is full attachment, and is the normal state.
        /// 0 is no attachment, and virtual camera will behave as if no LookAt
        /// targets are set.
        /// </summary>
        [NonSerialized]
        public float LookAtTargetAttachment;

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
        [FormerlySerializedAs("m_StandbyUpdate")]
        public StandbyUpdateMode StandbyUpdate = StandbyUpdateMode.RoundRobin;

        //============================================================================
        // Legacy streaming support

        [HideInInspector, SerializeField, NoSaveDuringPlay]
        int m_StreamingVersion;

        /// <summary>Post-Serialization handler - performs legacy upgrade</summary>
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            if (m_StreamingVersion < CinemachineCore.kStreamingVersion)
                LegacyUpgradeMayBeCalledFromThread(m_StreamingVersion);
            m_StreamingVersion = CinemachineCore.kStreamingVersion;
        }

        /// <summary>Pre-Serialization handler - delegates to derived classes</summary>
        void ISerializationCallbackReceiver.OnBeforeSerialize() 
        {
            m_StreamingVersion = CinemachineCore.kStreamingVersion;
            OnBeforeSerialize();
        }

        /// <summary>
        /// Override this to handle any upgrades necessitated by a streaming version change.
        /// Note that since this method is not called from the main thread, there are many things
        /// it cannot do, including checking a unity object for null.
        /// </summary>
        /// <param name="streamedVersion">The version that was streamed</param>
        internal protected virtual void LegacyUpgradeMayBeCalledFromThread(int streamedVersion)
        {
            if (streamedVersion < 20220601)
                PriorityAndChannel.SetPriority(m_LegacyPriority);
        }

        [HideInInspector, SerializeField, FormerlySerializedAs("m_Priority")]
        int m_LegacyPriority = 10;

        /// <summary>Obsolete field - use Priority instead</summary>
        // GML Upgradable does not work because we can't auto-upgrade an int field to an int property :-/
        //[Obsolete("m_Priority has been removed.  Please use Priority. (UnityUpgradable) -> Priority", false)]
        [Obsolete("m_Priority has been removed.  Please use Priority.", false)]
        public int m_Priority { get => Priority; set => Priority = value; }

        //============================================================================

        internal virtual void OnBeforeSerialize() {}

        /// <summary>
        /// Query components and extensions for the maximum damping time.
        /// Base class implementation queries extensions.
        /// Only used in editor for timeline scrubbing.
        /// </summary>
        /// <returns>Highest damping setting in this vcam</returns>
        public virtual float GetMaxDampTime()
        {
            float maxDamp = 0;
            if (Extensions != null)
                for (int i = 0; i < Extensions.Count; ++i)
                    maxDamp = Mathf.Max(maxDamp, Extensions[i].GetMaxDampTime());
            return maxDamp;
        }

        /// <summary>Get a damped version of a quantity.  This is the portion of the
        /// quantity that will take effect over the given time.
        /// This method takes the target attachment into account.  For general
        /// damping without consideration of target attachment, use Damper.Damp()</summary>
        /// <param name="initial">The amount that will be damped</param>
        /// <param name="dampTime">The rate of damping.  This is the time it would
        /// take to reduce the original amount to a negligible percentage</param>
        /// <param name="deltaTime">The time over which to damp</param>
        /// <returns>The damped amount.  This will be the original amount scaled by
        /// a value between 0 and 1.</returns>
        public float DetachedFollowTargetDamp(float initial, float dampTime, float deltaTime)
        {
            dampTime = Mathf.Lerp(Mathf.Max(1, dampTime), dampTime, FollowTargetAttachment);
            deltaTime = Mathf.Lerp(0, deltaTime, FollowTargetAttachment);
            return Damper.Damp(initial, dampTime, deltaTime);
        }

        /// <summary>Get a damped version of a quantity.  This is the portion of the
        /// quantity that will take effect over the given time.
        /// This method takes the target attachment into account.  For general
        /// damping without consideration of target attachment, use Damper.Damp()</summary>
        /// <param name="initial">The amount that will be damped</param>
        /// <param name="dampTime">The rate of damping.  This is the time it would
        /// take to reduce the original amount to a negligible percentage</param>
        /// <param name="deltaTime">The time over which to damp</param>
        /// <returns>The damped amount.  This will be the original amount scaled by
        /// a value between 0 and 1.</returns>
        public Vector3 DetachedFollowTargetDamp(Vector3 initial, Vector3 dampTime, float deltaTime)
        {
            dampTime = Vector3.Lerp(Vector3.Max(Vector3.one, dampTime), dampTime, FollowTargetAttachment);
            deltaTime = Mathf.Lerp(0, deltaTime, FollowTargetAttachment);
            return Damper.Damp(initial, dampTime, deltaTime);
        }

        /// <summary>Get a damped version of a quantity.  This is the portion of the
        /// quantity that will take effect over the given time.
        /// This method takes the target attachment into account.  For general
        /// damping without consideration of target attachment, use Damper.Damp()</summary>
        /// <param name="initial">The amount that will be damped</param>
        /// <param name="dampTime">The rate of damping.  This is the time it would
        /// take to reduce the original amount to a negligible percentage</param>
        /// <param name="deltaTime">The time over which to damp</param>
        /// <returns>The damped amount.  This will be the original amount scaled by
        /// a value between 0 and 1.</returns>
        public Vector3 DetachedFollowTargetDamp(Vector3 initial, float dampTime, float deltaTime)
        {
            dampTime = Mathf.Lerp(Mathf.Max(1, dampTime), dampTime, FollowTargetAttachment);
            deltaTime = Mathf.Lerp(0, deltaTime, FollowTargetAttachment);
            return Damper.Damp(initial, dampTime, deltaTime);
        }

        /// <summary>Get a damped version of a quantity.  This is the portion of the
        /// quantity that will take effect over the given time.
        /// This method takes the target attachment into account.  For general
        /// damping without consideration of target attachment, use Damper.Damp()</summary>
        /// <param name="initial">The amount that will be damped</param>
        /// <param name="dampTime">The rate of damping.  This is the time it would
        /// take to reduce the original amount to a negligible percentage</param>
        /// <param name="deltaTime">The time over which to damp</param>
        /// <returns>The damped amount.  This will be the original amount scaled by
        /// a value between 0 and 1.</returns>
        public float DetachedLookAtTargetDamp(float initial, float dampTime, float deltaTime)
        {
            dampTime = Mathf.Lerp(Mathf.Max(1, dampTime), dampTime, LookAtTargetAttachment);
            deltaTime = Mathf.Lerp(0, deltaTime, LookAtTargetAttachment);
            return Damper.Damp(initial, dampTime, deltaTime);
        }

        /// <summary>Get a damped version of a quantity.  This is the portion of the
        /// quantity that will take effect over the given time.
        /// This method takes the target attachment into account.  For general
        /// damping without consideration of target attachment, use Damper.Damp()</summary>
        /// <param name="initial">The amount that will be damped</param>
        /// <param name="dampTime">The rate of damping.  This is the time it would
        /// take to reduce the original amount to a negligible percentage</param>
        /// <param name="deltaTime">The time over which to damp</param>
        /// <returns>The damped amount.  This will be the original amount scaled by
        /// a value between 0 and 1.</returns>
        public Vector3 DetachedLookAtTargetDamp(Vector3 initial, Vector3 dampTime, float deltaTime)
        {
            dampTime = Vector3.Lerp(Vector3.Max(Vector3.one, dampTime), dampTime, LookAtTargetAttachment);
            deltaTime = Mathf.Lerp(0, deltaTime, LookAtTargetAttachment);
            return Damper.Damp(initial, dampTime, deltaTime);
        }

        /// <summary>Get a damped version of a quantity.  This is the portion of the
        /// quantity that will take effect over the given time.
        /// This method takes the target attachment into account.  For general
        /// damping without consideration of target attachment, use Damper.Damp()</summary>
        /// <param name="initial">The amount that will be damped</param>
        /// <param name="dampTime">The rate of damping.  This is the time it would
        /// take to reduce the original amount to a negligible percentage</param>
        /// <param name="deltaTime">The time over which to damp</param>
        /// <returns>The damped amount.  This will be the original amount scaled by
        /// a value between 0 and 1.</returns>
        public Vector3 DetachedLookAtTargetDamp(Vector3 initial, float dampTime, float deltaTime)
        {
            dampTime = Mathf.Lerp(Mathf.Max(1, dampTime), dampTime, LookAtTargetAttachment);
            deltaTime = Mathf.Lerp(0, deltaTime, LookAtTargetAttachment);
            return Damper.Damp(initial, dampTime, deltaTime);
        }

        /// <summary>
        /// A delegate to hook into the state calculation pipeline.
        /// This will be called after each pipeline stage, to allow others to hook into the pipeline.
        /// See CinemachineCore.Stage.
        /// </summary>
        /// <param name="extension">The extension to add.</param>
        internal void AddExtension(CinemachineExtension extension)
        {
            if (Extensions == null)
                Extensions = new List<CinemachineExtension>();
            else
                Extensions.Remove(extension);
            Extensions.Add(extension);
        }

        /// <summary>Remove a Pipeline stage hook callback.</summary>
        /// <param name="extension">The extension to remove.</param>
        internal void RemoveExtension(CinemachineExtension extension)
        {
            if (Extensions != null)
                Extensions.Remove(extension);
        }

        /// <summary> The extensions connected to this vcam</summary>
        internal List<CinemachineExtension> Extensions { get; private set; }

        /// <summary>
        /// Invokes the PostPipelineStageDelegate for this camera, and up the hierarchy for all
        /// parent cameras (if any).
        /// Implementaion must be sure to call this after each pipeline stage, to allow
        /// other services to hook into the pipeline.
        /// See CinemachineCore.Stage.
        /// </summary>
        /// <param name="vcam">The virtual camera being processed</param>
        /// <param name="stage">The current pipeline stage</param>
        /// <param name="newState">The current virtual camera state</param>
        /// <param name="deltaTime">The current applicable deltaTime</param>
        protected void InvokePostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage,
            ref CameraState newState, float deltaTime)
        {
            if (Extensions != null)
            {
                for (int i = 0; i < Extensions.Count; ++i)
                {
                    var e = Extensions[i];
                    if (e == null)
                    {
                        // Object was deleted (possibly because of Undo in the editor)
                        Extensions.RemoveAt(i);
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
        
        /// <summary>
        /// Invokes the PrePipelineMutateCameraStateCallback for this camera, 
        /// and up the hierarchy for all parent cameras (if any).
        /// Implementaion must be sure to call this after each pipeline stage, to allow
        /// other services to hook into the pipeline.
        /// See CinemachineCore.Stage.
        /// </summary>
        /// <param name="vcam">The virtual camera being processed</param>
        /// <param name="newState">The current virtual camera state</param>
        /// <param name="deltaTime">The current applicable deltaTime</param>
        protected void InvokePrePipelineMutateCameraStateCallback(
            CinemachineVirtualCameraBase vcam, ref CameraState newState, float deltaTime)
        {
            if (Extensions != null)
            {
                for (int i = 0; i < Extensions.Count; ++i)
                {
                    var e = Extensions[i];
                    if (e == null)
                    {
                        // Object was deleted (possibly because of Undo in the editor)
                        Extensions.RemoveAt(i);
                        --i;
                    }
                    else if (e.enabled)
                        e.PrePipelineMutateCameraStateCallback(vcam, ref newState, deltaTime);
                }
            }
            CinemachineVirtualCameraBase parent = ParentCamera as CinemachineVirtualCameraBase;
            if (parent != null)
                parent.InvokePrePipelineMutateCameraStateCallback(vcam, ref newState, deltaTime);
        }

        /// <summary>
        /// Invokes the OnTransitionFromCamera for all extensions on this camera
        /// </summary>
        /// <param name="fromCam">The camera being deactivated.  May be null.</param>
        /// <param name="worldUp">Default world Up, set by the CinemachineBrain</param>
        /// <param name="deltaTime">Delta time for time-based effects (ignore if less than or equal to 0)</param>
        /// <returns>True to request a vcam update of internal state</returns>
        protected bool InvokeOnTransitionInExtensions(
            ICinemachineCamera fromCam, Vector3 worldUp, float deltaTime)
        {
            bool forceUpdate = false;
            if (Extensions != null)
            {
                for (int i = 0; i < Extensions.Count; ++i)
                {
                    var e = Extensions[i];
                    if (e == null)
                    {
                        // Object was deleted (possibly because of Undo in the editor)
                        Extensions.RemoveAt(i);
                        --i;
                    }
                    else if (e.enabled && e.OnTransitionFromCamera(fromCam, worldUp, deltaTime))
                        forceUpdate = true;
                }
            }
            return forceUpdate;
        }

        /// <summary>Get the name of the Virtual Camera.  Base implementation
        /// returns the owner GameObject's name.</summary>
        public string Name => name;

        /// <summary>Gets a brief debug description of this virtual camera, for use when displayiong debug info</summary>
        public virtual string Description => "";

        /// <summary>Get the Priority of the virtual camera.  This determines its placement
        /// in the CinemachineCore's queue of eligible shots.</summary>
        public int Priority 
        { 
            get => PriorityAndChannel.GetPriority();
            set => PriorityAndChannel.SetPriority(value);
        }

        
        /// <summary>Get the effective output channel mask.</summary>
        /// <returns>Returns the effective output channel mask, when Custom Priority is enabled.
        /// Returns Channels.Default otherwise.</returns>
        public OutputChannel.Channels GetChannel() => PriorityAndChannel.GetChannel();

        /// <summary>Hint for transitioning to and from this virtual camera</summary>
        [Flags]
        public enum BlendHint
        {
            /// <summary>Spherical blend about Tracking target position</summary>
            SphericalPosition = 1,
            /// <summary>Cylindrical blend about Tracking target position (vertical co-ordinate is linearly interpolated)</summary>
            CylindricalPosition = 2,
            /// <summary>Screen-space blend between LookAt targets instead of world space lerp of target position</summary>
            ScreenSpaceAimWhenTargetsDiffer = 4,
            /// <summary>When this virtual camera goes Live, attempt to force the position to be the same 
            /// as the current position of the Unity Camera</summary>
            InheritPosition = 8
        }

        /// <summary>Applies a position blend hint to a camera state</summary>
        /// <param name="state">The state to apply the hint to</param>
        /// <param name="hint">The hint to apply</param>
        protected void ApplyPositionBlendMethod(ref CameraState state, BlendHint hint)
        {
            if ((hint & BlendHint.SphericalPosition) != 0)
                state.BlendHint |= CameraState.BlendHintValue.SphericalPositionBlend;
            if ((hint & BlendHint.CylindricalPosition) != 0)
                state.BlendHint |= CameraState.BlendHintValue.CylindricalPositionBlend;
            if ((hint & BlendHint.ScreenSpaceAimWhenTargetsDiffer) != 0)
                state.BlendHint |= CameraState.BlendHintValue.RadialAimBlend;
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

        /// <summary>Returns false if the object has been deleted</summary>
        public bool IsValid => !(this == null);

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
                if (!m_SlaveStatusUpdated || !Application.isPlaying)
                    UpdateSlaveStatus();
                return m_ParentVcam;
            }
        }

        /// <summary>Returns the camera's TransitionParams settings</summary>
        /// <returns>The camera's TransitionParams settings</returns>
        public abstract TransitionParams GetTransitionParams();

        /// <summary>Check whether the vcam a live child of this camera.
        /// This base class implementation always returns false.</summary>
        /// <param name="vcam">The Virtual Camera to check</param>
        /// <param name="dominantChildOnly">If true, will only return true if this vcam is the dominant live child</param>
        /// <returns>True if the vcam is currently actively influencing the state of this vcam</returns>
        public virtual bool IsLiveChild(ICinemachineCamera vcam, bool dominantChildOnly = false) { return false; }

        /// <summary>Get the LookAt target for the Aim component in the Cinemachine pipeline.</summary>
        public abstract Transform LookAt { get; set; }

        /// <summary>Get the Follow target for the Body component in the Cinemachine pipeline.</summary>
        public abstract Transform Follow { get; set; }

        /// <summary>Set this to force the next update to ignore deltaTime and reset itself</summary>
        public virtual bool PreviousStateIsValid { get; set; }

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
            /// <summary>Hint for transitioning to and from this CmCamera.  Hints can be combined, although 
            /// not all combinations make sense.  In the case of conflicting hints, Cinemachine will 
            /// make an arbitrary choice.</summary>
            [Tooltip("Hint for transitioning to and from this CmCamera.  Hints can be combined, although "
                + "not all combinations make sense.  In the case of conflicting hints, Cinemachine will "
                + "make an arbitrary choice.")]
            public BlendHint BlendHint;

            /// <summary>Shortcut to read InheritPosition flag in BlendHint</summary>
            public bool InheritPosition => (BlendHint & BlendHint.InheritPosition) != 0;

            /// <summary>
            /// These events fire when a transition occurs
            /// </summary>
            [Serializable]
            public struct TransitionEvents
            {
                /// <summary>This event fires when the CmCamera goes Live</summary>
                [Tooltip("This event fires when the CmCamera goes Live")]
                public CinemachineBrain.VcamActivatedEvent OnCameraLive;
            }
            /// <summary>
            /// These events fire when a transition occurs
            /// </summary>
            [Tooltip("These events fire when a transition occurs")]
            public TransitionEvents Events;
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
            CinemachineCore.Instance.CameraDestroyed(this);
        }

        /// <summary>Base class implementation makes sure the priority queue remains up-to-date.</summary>
        protected virtual void OnTransformParentChanged()
        {
            CinemachineCore.Instance.CameraDisabled(this);
            CinemachineCore.Instance.CameraEnabled(this);
            UpdateSlaveStatus();
            UpdateVcamPoolStatus();
        }

        bool m_WasStarted;

        /// <summary>Derived classes should call base class implementation.</summary>
        protected virtual void Start()
        {
            m_WasStarted = true;
        }
        
        /// <summary>
        /// Called on inactive object when being artificially activated by timeline.
        /// This is necessary because Awake() isn't called on inactive gameObjects.
        /// </summary>
        internal void EnsureStarted()
        {
            if (!m_WasStarted)
            {
                m_WasStarted = true;
                var extensions = GetComponentsInChildren<CinemachineExtension>();
                for (int i = 0; i < extensions.Length; ++i)
                    extensions[i].EnsureStarted();
            }
        }

#if UNITY_EDITOR
        [UnityEditor.Callbacks.DidReloadScripts]
        static void OnScriptReload()
        {
            var vcams = Resources.FindObjectsOfTypeAll(
                typeof(CinemachineVirtualCameraBase)) as CinemachineVirtualCameraBase[];
            foreach (var vcam in vcams)
                vcam.LookAtTargetChanged = vcam.FollowTargetChanged = true;
        }
#endif

        /// <summary>Base class implementation adds the virtual camera from the priority queue.</summary>
        protected virtual void OnEnable()
        {
            UpdateSlaveStatus();
            UpdateVcamPoolStatus();    // Add to queue
            if (!CinemachineCore.Instance.IsLive(this))
                PreviousStateIsValid = false;
            CinemachineCore.Instance.CameraEnabled(this);
            InvalidateCachedTargets();
            // Sanity check - if another vcam component is enabled, shut down
            var vcamComponents = GetComponents<CinemachineVirtualCameraBase>();
            for (int i = 0; i < vcamComponents.Length; ++i)
            {
                if (vcamComponents[i].enabled && vcamComponents[i] != this)
                {
                    Debug.LogWarning(Name
                        + " has multiple CinemachineVirtualCameraBase-derived components.  Disabling "
                        + GetType().Name + ".");
                    enabled = false;
                }
            }
        }

        /// <summary>Base class implementation makes sure the priority queue remains up-to-date.</summary>
        protected virtual void OnDisable()
        {
            UpdateVcamPoolStatus();    // Remove from queue
            CinemachineCore.Instance.CameraDisabled(this);
        }

        /// <summary>Base class implementation makes sure the priority queue remains up-to-date.</summary>
        protected virtual void Update()
        {
            if (Priority != m_QueuePriority)
            {
                UpdateVcamPoolStatus(); // Force a re-sort
            }
        }

        bool m_SlaveStatusUpdated = false;
        CinemachineVirtualCameraBase m_ParentVcam = null;

        void UpdateSlaveStatus()
        {
            m_SlaveStatusUpdated = true;
            m_ParentVcam = null;
            Transform p = transform.parent;
            if (p != null)
                p.TryGetComponent(out m_ParentVcam);
        }

        /// <summary>Returns this vcam's LookAt target, or if that is null, will retrun
        /// the parent vcam's LookAt target.</summary>
        /// <param name="localLookAt">This vcam's LookAt value.</param>
        /// <returns>The same value, or the parent's if null and a parent exists.</returns>
        public Transform ResolveLookAt(Transform localLookAt)
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
        public Transform ResolveFollow(Transform localFollow)
        {
            Transform follow = localFollow;
            if (follow == null && ParentCamera != null)
                follow = ParentCamera.Follow; // Parent provides default
            return follow;
        }

        void UpdateVcamPoolStatus()
        {
            CinemachineCore.Instance.RemoveActiveCamera(this);
            if (m_ParentVcam == null && isActiveAndEnabled)
                CinemachineCore.Instance.AddActiveCamera(this);
            m_QueuePriority = Priority;
        }

        /// <summary>When multiple virtual cameras have the highest priority, there is
        /// sometimes the need to push one to the top, making it the current Live camera if
        /// it shares the highest priority in the queue with its peers.
        ///
        /// This happens automatically when a
        /// new vcam is enabled: the most recent one goes to the top of the priority subqueue.
        /// Use this method to push a vcam to the top of its priority peers.
        /// If it and its peers share the highest priority, then this vcam will become Live.</summary>
        [Obsolete("Please use Prioritize()")]
        public void MoveToTopOfPrioritySubqueue() => Prioritize();

        /// <summary>When multiple Cm Cameras have the highest priority, there is
        /// sometimes the need to push one to the top, making it the current Live camera if
        /// it shares the highest priority in the queue with its peers.
        ///
        /// This happens automatically when a
        /// new CmCamera is enabled: the most recent one goes to the top of the priority subqueue.
        /// Use this method to push a camera to the top of its priority peers.
        /// If it and its peers share the highest priority, then this vcam will become Live.</summary>
        public void Prioritize()
        {
            UpdateVcamPoolStatus(); // Force a re-sort
        }
        
        /// <summary>This is called to notify the component that a target got warped,
        /// so that the component can update its internal state to make the camera
        /// also warp seamlessly.</summary>
        /// <param name="target">The object that was warped</param>
        /// <param name="positionDelta">The amount the target's position changed</param>
        public virtual void OnTargetObjectWarped(Transform target, Vector3 positionDelta)
        {
            // inform the extensions
            if (Extensions != null)
            {
                for (int i = 0; i < Extensions.Count; ++i)
                    Extensions[i].OnTargetObjectWarped(target, positionDelta);
            }
        }

        /// <summary>
        /// Force the virtual camera to assume a given position and orientation
        /// </summary>
        /// <param name="pos">Worldspace position to take</param>
        /// <param name="rot">Worldspace orientation to take</param>
        public virtual void ForceCameraPosition(Vector3 pos, Quaternion rot)
        {
            // inform the extensions
            if (Extensions != null)
            {
                for (int i = 0; i < Extensions.Count; ++i)
                    Extensions[i].ForceCameraPosition(pos, rot);
            }
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
            state.RawPosition = TargetPositionCache.GetTargetPosition(transform);
            state.RawOrientation = TargetPositionCache.GetTargetRotation(transform);
            state.ReferenceUp = worldUp;

            CinemachineBrain brain = CinemachineCore.Instance.FindPotentialTargetBrain(this);
            if (brain != null)
                lens.SnapshotCameraReadOnlyProperties(brain.OutputCamera);

            state.Lens = lens;
            return state;
        }

        Transform m_CachedFollowTarget;
        CinemachineVirtualCameraBase m_CachedFollowTargetVcam;
        ICinemachineTargetGroup m_CachedFollowTargetGroup;

        Transform m_CachedLookAtTarget;
        CinemachineVirtualCameraBase m_CachedLookAtTargetVcam;
        ICinemachineTargetGroup m_CachedLookAtTargetGroup;

        void InvalidateCachedTargets()
        {
            m_CachedFollowTarget = null;
            m_CachedFollowTargetVcam = null;
            m_CachedFollowTargetGroup = null;
            m_CachedLookAtTarget = null;
            m_CachedLookAtTargetVcam = null;
            m_CachedLookAtTargetGroup = null;
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoad]
        class OnDomainReload 
        { 
            static OnDomainReload() 
            {
                var vcams = FindObjectsOfType<CinemachineVirtualCameraBase>(true);
                foreach (var vcam in vcams)
                    vcam.InvalidateCachedTargets();
            }
        }
#endif

        /// <summary>
        /// This property is true if the Follow target was changed this frame.
        /// </summary>
        public bool FollowTargetChanged { get; private set; }

        /// <summary>
        /// This property is true if the LookAttarget was changed this frame.
        /// </summary>
        public bool LookAtTargetChanged { get; private set; }

        /// <summary>
        /// Call this from InternalUpdateCameraState() to check for changed 
        /// targets and update the target cache.  This is needed for tracking
        /// when a target object changes.
        /// </summary>
        protected void UpdateTargetCache()
        {
            var target = ResolveFollow(Follow);
            FollowTargetChanged = target != m_CachedFollowTarget;
            if (FollowTargetChanged)
            {
                m_CachedFollowTarget = target;
                m_CachedFollowTargetVcam = null;
                m_CachedFollowTargetGroup = null;
                if (m_CachedFollowTarget != null)
                {
                    target.TryGetComponent(out m_CachedFollowTargetVcam);
                    target.TryGetComponent(out m_CachedFollowTargetGroup);
                }
            }
            target = ResolveLookAt(LookAt);
            LookAtTargetChanged = target != m_CachedLookAtTarget;
            if (LookAtTargetChanged)
            {
                m_CachedLookAtTarget = target;
                m_CachedLookAtTargetVcam = null;
                m_CachedLookAtTargetGroup = null;
                if (target != null)
                {
                    target.TryGetComponent(out m_CachedLookAtTargetVcam);
                    target.TryGetComponent(out m_CachedLookAtTargetGroup);
                }
            }
        }

        /// <summary>Get Follow target as ICinemachineTargetGroup, 
        /// or null if target is not a ICinemachineTargetGroup</summary>
        public ICinemachineTargetGroup FollowTargetAsGroup => m_CachedFollowTargetGroup;

        /// <summary>Get Follow target as CinemachineVirtualCameraBase, 
        /// or null if target is not a CinemachineVirtualCameraBase</summary>
        public CinemachineVirtualCameraBase FollowTargetAsVcam => m_CachedFollowTargetVcam;

        /// <summary>Get LookAt target as ICinemachineTargetGroup, 
        /// or null if target is not a ICinemachineTargetGroup</summary>
        public ICinemachineTargetGroup LookAtTargetAsGroup => m_CachedLookAtTargetGroup;

        /// <summary>Get LookAt target as CinemachineVirtualCameraBase, 
        /// or null if target is not a CinemachineVirtualCameraBase</summary>
        public CinemachineVirtualCameraBase LookAtTargetAsVcam => m_CachedLookAtTargetVcam;

        /// <summary>Get the component set for a specific stage in the pipeline.</summary>
        /// <param name="stage">The stage for which we want the component</param>
        /// <returns>The Cinemachine component for that stage, or null if not present.</returns>
        public virtual CinemachineComponentBase GetCinemachineComponent(CinemachineCore.Stage stage) => null;
    }
}
