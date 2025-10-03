using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Unity.Cinemachine
{
    /// <summary>
    /// Base class for a Monobehaviour that represents a Virtual Camera within the Unity scene.
    ///
    /// This is intended to be attached to an empty Transform GameObject.
    /// Inherited classes can be either standalone virtual cameras such
    /// as CinemachineCamera, or meta-cameras such as
    /// CinemachineClearShot or CinemachineBlendListCamera.
    ///
    /// A CinemachineVirtualCameraBase exposes an OutputChannel property.  When the behaviour is
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
    public abstract class CinemachineVirtualCameraBase : MonoBehaviour, ICinemachineCamera
    {
        /// <summary>
        /// Priority can be used to control which Cm Camera is live when multiple CM Cameras are
        /// active simultaneously.  The most-recently-activated CinemachineCamera will take control, unless there
        /// is another Cm Camera active with a higher priority.  In general, the most-recently-activated
        /// highest-priority CinemachineCamera will control the main camera.
        ///
        /// The default priority value is 0. Often it is sufficient to leave the default setting.
        /// In special cases where you want a CinemachineCamera to have a higher or lower priority value than 0, you can set it here.
        /// </summary>
        [NoSaveDuringPlay]
        [Tooltip("Priority can be used to control which Cm Camera is live when multiple CM Cameras are "
            + "active simultaneously.  The most-recently-activated CinemachineCamera will take control, unless there "
            + "is another Cm Camera active with a higher priority.  In general, the most-recently-activated "
            + "highest-priority CinemachineCamera will control the main camera. \n\n"
            + "The default priority is value 0.  Often it is sufficient to leave the default setting.  "
            + "In special cases where you want a CinemachineCamera to have a higher or lower priority value than 0, you can set it here.")]
        [EnabledProperty(toggleText: "(using default)")]
        public PrioritySettings Priority = new ();

        /// <summary>
        /// The output channel functions like Unity layers.  Use it to filter the output of CinemachineCameras
        /// to different CinemachineBrains, for instance in a multi-screen environemnt.
        /// </summary>
        [NoSaveDuringPlay]
        [Tooltip("The output channel functions like Unity layers.  Use it to filter the output of CinemachineCameras "
            + "to different CinemachineBrains, for instance in a multi-screen environemnt.")]
        public OutputChannels OutputChannel = OutputChannels.Default;

        /// <summary>Helper for upgrading from CM2</summary>
        internal protected virtual bool IsDprecated => false;

        /// <summary>A sequence number that represents object activation order of vcams.
        /// Used for priority sorting.</summary>
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

        // Cache for GameObject name, to avoid GC allocs
        [NonSerialized] string m_CachedName;
        [NonSerialized] bool m_WasStarted;
        [NonSerialized] bool m_ChildStatusUpdated = false;
        [NonSerialized] CinemachineVirtualCameraBase m_ParentVcam = null;

        [NonSerialized] Transform m_CachedFollowTarget;
        [NonSerialized] CinemachineVirtualCameraBase m_CachedFollowTargetVcam;
        [NonSerialized] ICinemachineTargetGroup m_CachedFollowTargetGroup;

        [NonSerialized] Transform m_CachedLookAtTarget;
        [NonSerialized] CinemachineVirtualCameraBase m_CachedLookAtTargetVcam;
        [NonSerialized] ICinemachineTargetGroup m_CachedLookAtTargetGroup;


        //============================================================================
        // Legacy streaming support

        [HideInInspector, SerializeField, NoSaveDuringPlay]
        int m_StreamingVersion;

        /// <summary>
        /// Override this to handle any upgrades necessitated by a streaming version change.
        /// Note that since this method is not called from the main thread, there are many things
        /// it cannot do, including checking a unity object for null.
        /// </summary>
        /// <param name="streamedVersion">The version that was streamed</param>
        internal protected virtual void PerformLegacyUpgrade(int streamedVersion)
        {
            if (streamedVersion < 20220601)
            {
                if (m_LegacyPriority != 0)
                {
                    Priority.Value = m_LegacyPriority;
                    m_LegacyPriority = 0;
                }
            }
        }

        [HideInInspector, SerializeField, NoSaveDuringPlay, FormerlySerializedAs("m_Priority")]
        int m_LegacyPriority = 0;

        //============================================================================

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
        /// Implementation must be sure to call this after each pipeline stage, to allow
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
            if (ParentCamera is CinemachineVirtualCameraBase vcamParent)
                vcamParent.InvokePostPipelineStageCallback(vcam, stage, ref newState, deltaTime);
        }

        /// <summary>
        /// Invokes the PrePipelineMutateCameraStateCallback for this camera,
        /// and up the hierarchy for all parent cameras (if any).
        /// Implementation must be sure to call this after each pipeline stage, to allow
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
            if (ParentCamera is CinemachineVirtualCameraBase vcamParent)
                vcamParent.InvokePrePipelineMutateCameraStateCallback(vcam, ref newState, deltaTime);
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
        /// returns a cache of the owner GameObject's name.</summary>
        public string Name
        {
            get
            {
#if UNITY_EDITOR
                // Allow vcam name changes when not playing
                if (!Application.isPlaying)
                    m_CachedName = null;
#endif
                m_CachedName ??= IsValid ? name : "(deleted)";
                return m_CachedName;
            }
        }

        /// <summary>Gets a brief debug description of this virtual camera, for use when displaying debug info</summary>
        public virtual string Description => "";

        /// <summary>Returns false if the object has been deleted</summary>
        public bool IsValid => !(this == null);

        /// <summary>The CameraState object holds all of the information
        /// necessary to position the Unity camera.  It is the output of this class.</summary>
        public abstract CameraState State { get; }

        /// <summary>Support for meta-virtual-cameras.  This is the situation where a
        /// virtual camera is in fact the public face of a private army of virtual cameras, which
        /// it manages on its own.  This method gets the VirtualCamera owner, if any.
        /// Private armies are implemented as Transform children of the parent vcam.</summary>
        public ICinemachineMixer ParentCamera
        {
            get
            {
                if (!m_ChildStatusUpdated || !Application.isPlaying)
                    UpdateStatusAsChild();
                return m_ParentVcam as ICinemachineMixer;
            }
        }

        /// <summary>Get the LookAt target for the Aim component in the Cinemachine pipeline.</summary>
        public abstract Transform LookAt { get; set; }

        /// <summary>Get the Follow target for the Body component in the Cinemachine pipeline.</summary>
        public abstract Transform Follow { get; set; }

        /// <summary>Set this to force the next update to ignore state from the previous frame.
        /// This is useful, for example, if you want to cancel damping or other time-based processing.</summary>
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
            CameraUpdateManager.UpdateVirtualCamera(this, worldUp, deltaTime);
        }

        /// <summary>Internal use only.
        /// Called by CinemachineCore at designated update time
        /// so the vcam can position itself and track its targets.
        /// Do not call this method.  Let the framework do it at the appropriate time</summary>
        /// <param name="worldUp">Default world Up, set by the CinemachineBrain</param>
        /// <param name="deltaTime">Delta time for time-based effects (ignore if less than 0)</param>
        public abstract void InternalUpdateCameraState(Vector3 worldUp, float deltaTime);

        /// <inheritdoc />
        public virtual void OnCameraActivated(ICinemachineCamera.ActivationEventParams evt)
        {
            if (evt.IncomingCamera == (ICinemachineCamera)this)
                OnTransitionFromCamera(evt.OutgoingCamera, evt.WorldUp, evt.DeltaTime);
        }

        // GML todo: get rid of OnTransitionFromCamera
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

        /// <summary>
        /// Called on inactive object when being artificially activated by timeline.
        /// This is necessary because Awake() isn't called on inactive gameObjects.
        /// </summary>
        internal void EnsureStarted()
        {
            if (!m_WasStarted)
            {
                m_WasStarted = true;

                // Perform legacy upgrade if necessary
                if (m_StreamingVersion < CinemachineCore.kStreamingVersion)
                    PerformLegacyUpgrade(m_StreamingVersion);
                m_StreamingVersion = CinemachineCore.kStreamingVersion;

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
            for (int i = 0; i < vcams.Length; ++i)
                vcams[i].LookAtTargetChanged = vcams[i].FollowTargetChanged = true;
        }
#endif

        /// <summary>Base class implementation makes sure the priority queue remains up-to-date.</summary>
        protected virtual void OnTransformParentChanged()
        {
            CameraUpdateManager.CameraDisabled(this);
            CameraUpdateManager.CameraEnabled(this);
            UpdateStatusAsChild();
            UpdateVcamPoolStatus();
        }

        /// <summary>Maintains the global vcam registry.  Always call the base class implementation.</summary>
        protected virtual void OnDestroy()
        {
            CameraUpdateManager.CameraDestroyed(this);
        }

        /// <summary>Derived classes should call base class implementation.</summary>
        protected virtual void Start()
        {
            m_WasStarted = true;

            // Perform legacy upgrade if necessary
            if (m_StreamingVersion < CinemachineCore.kStreamingVersion)
                PerformLegacyUpgrade(m_StreamingVersion);
            m_StreamingVersion = CinemachineCore.kStreamingVersion;
        }

        /// <summary>Base class implementation adds the virtual camera from the priority queue.</summary>
        protected virtual void OnEnable()
        {
            UpdateStatusAsChild();
            UpdateVcamPoolStatus();    // Add to queue
            if (!CinemachineCore.IsLive(this))
                PreviousStateIsValid = false;
            CameraUpdateManager.CameraEnabled(this);
            InvalidateCachedTargets();

            // Sanity check - if another vcam component is enabled, shut down
            var vcamComponents = GetComponents<CinemachineVirtualCameraBase>();
            for (int i = 0; i < vcamComponents.Length; ++i)
            {
                if (vcamComponents[i].enabled && vcamComponents[i] != this)
                {
                    var toDeprecate = vcamComponents[i].IsDprecated ? vcamComponents[i] : this;
                    if (!toDeprecate.IsDprecated)
                        Debug.LogWarning(Name
                            + " has multiple CinemachineVirtualCameraBase-derived components.  Disabling "
                            + toDeprecate.GetType().Name);
                    toDeprecate.enabled = false;
                }
            }
        }

        /// <summary>Base class implementation makes sure the priority queue remains up-to-date.</summary>
        protected virtual void OnDisable()
        {
            UpdateVcamPoolStatus();    // Remove from queue
            CameraUpdateManager.CameraDisabled(this);
        }

        /// <summary>Base class implementation makes sure the priority queue remains up-to-date.</summary>
        protected virtual void Update()
        {
            if (Priority.Value != m_QueuePriority)
                UpdateVcamPoolStatus(); // Force a re-sort
        }

        void UpdateStatusAsChild()
        {
            m_ChildStatusUpdated = true;
            m_ParentVcam = null;
            Transform p = transform.parent;
            if (p != null)
                p.TryGetComponent(out m_ParentVcam);
        }

        /// <summary>Returns this vcam's LookAt target, or if that is null, will return
        /// the parent vcam's LookAt target.</summary>
        /// <param name="localLookAt">This vcam's LookAt value.</param>
        /// <returns>The same value, or the parent's if null and a parent exists.</returns>
        public Transform ResolveLookAt(Transform localLookAt)
        {
            Transform lookAt = localLookAt;
            if (lookAt == null && ParentCamera is CinemachineVirtualCameraBase vcamParent)
                lookAt = vcamParent.LookAt; // Parent provides default
            return lookAt;
        }

        /// <summary>Returns this vcam's Follow target, or if that is null, will retrun
        /// the parent vcam's Follow target.</summary>
        /// <param name="localFollow">This vcam's Follow value.</param>
        /// <returns>The same value, or the parent's if null and a parent exists.</returns>
        public Transform ResolveFollow(Transform localFollow)
        {
            Transform follow = localFollow;
            if (follow == null && ParentCamera is CinemachineVirtualCameraBase vcamParent)
                follow = vcamParent.Follow; // Parent provides default
            return follow;
        }

        void UpdateVcamPoolStatus()
        {
            CameraUpdateManager.RemoveActiveCamera(this);
            if (m_ParentVcam == null && isActiveAndEnabled)
                CameraUpdateManager.AddActiveCamera(this);
            m_QueuePriority = Priority.Value;
        }

        /// <summary>When multiple virtual cameras have the highest priority, there is
        /// sometimes the need to push one to the top, making it the current Live camera if
        /// it shares the highest priority in the queue with its peers.
        ///
        /// This happens automatically when a
        /// new vcam is enabled: the most recent one goes to the top of the priority sub-queue.
        /// Use this method to push a vcam to the top of its priority peers.
        /// If it and its peers share the highest priority, then this vcam will become Live.</summary>
        [Obsolete("Please use Prioritize()")]
        public void MoveToTopOfPrioritySubqueue() => Prioritize();

        /// <summary>When multiple Cm Cameras have the highest priority, there is
        /// sometimes the need to push one to the top, making it the current Live camera if
        /// it shares the highest priority in the queue with its peers.
        ///
        /// This happens automatically when a
        /// new CinemachineCamera is enabled: the most recent one goes to the top of the priority sub-queue.
        /// Use this method to push a camera to the top of its priority peers.
        /// If it and its peers share the highest priority, then this vcam will become Live.</summary>
        public void Prioritize() => UpdateVcamPoolStatus(); // Force a re-sort

        /// <summary>This is called to notify the component that a target got warped,
        /// so that the component can update its internal state to make the camera
        /// also warp seamlessly.</summary>
        /// <param name="target">The object that was warped</param>
        /// <param name="positionDelta">The amount the target's position changed</param>
        public virtual void OnTargetObjectWarped(Transform target, Vector3 positionDelta)
            => OnTargetObjectWarped(this, target, positionDelta);

        void OnTargetObjectWarped(CinemachineVirtualCameraBase vcam, Transform target, Vector3 positionDelta)
        {
            // inform the extensions
            var count = Extensions?.Count;
            for (int i = 0; i < count; ++i)
                Extensions[i].OnTargetObjectWarped(vcam, target, positionDelta);
            if (ParentCamera is CinemachineVirtualCameraBase vcamParent)
                vcamParent.OnTargetObjectWarped(vcam, target, positionDelta);
        }

        /// <summary>
        /// Force the virtual camera to assume a given position and orientation
        /// </summary>
        /// <param name="pos">World-space position to take</param>
        /// <param name="rot">World-space orientation to take</param>
        public virtual void ForceCameraPosition(Vector3 pos, Quaternion rot) => ForceCameraPosition(this, pos, rot);

        void ForceCameraPosition(CinemachineVirtualCameraBase vcam, Vector3 pos, Quaternion rot)
        {
            // inform the extensions
            var count = Extensions?.Count;
            for (int i = 0; i < count; ++i)
            {
                Extensions[i].ForceCameraPosition(vcam, pos, rot);
                Extensions[i].ForceCameraPosition(pos, rot); // call the obsolete API in order to not break old client code
            }
            if (ParentCamera is CinemachineVirtualCameraBase vcamParent)
                vcamParent.ForceCameraPosition(vcam, pos, rot);
            PreviousStateIsValid = true;
        }

        /// <summary>
        /// Create a camera state based on the current transform of this vcam
        /// </summary>
        /// <param name="worldUp">Current World Up direction, as provided by the brain</param>
        /// <param name="lens">Lens settings to serve as base, will be combined with lens from brain, if any</param>
        /// <returns>A CameraState based on the current transform of this vcam.</returns>
        protected CameraState PullStateFromVirtualCamera(Vector3 worldUp, ref LensSettings lens)
        {
            CameraState state = CameraState.Default;
            state.RawPosition = TargetPositionCache.GetTargetPosition(transform);
            state.RawOrientation = TargetPositionCache.GetTargetRotation(transform);
            state.ReferenceUp = worldUp;

            CinemachineBrain brain = CinemachineCore.FindPotentialTargetBrain(this);
            if (brain != null && brain.OutputCamera != null)
                lens.PullInheritedPropertiesFromCamera(brain.OutputCamera);

            state.Lens = lens;
            return state;
        }

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
#if UNITY_2023_1_OR_NEWER
                var vcams = FindObjectsByType<CinemachineVirtualCameraBase>
                    (FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
                var vcams = FindObjectsOfType<CinemachineVirtualCameraBase>(true);
#endif
                for (int i = 0; i < vcams.Length; ++i)
                    vcams[i].InvalidateCachedTargets();
            }
        }
#endif

        /// <summary>
        /// This property is true if the Follow target was changed this frame.
        /// </summary>
        public bool FollowTargetChanged { get; private set; }

        /// <summary>
        /// This property is true if the LookAt was changed this frame.
        /// </summary>
        public bool LookAtTargetChanged { get; private set; }

        /// <summary>
        /// Call this from InternalUpdateCameraState() to check for changed
        /// targets and update the target cache.  This is needed for tracking
        /// when a target object changes.
        /// </summary>
        public void UpdateTargetCache()
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

        /// <summary>Returns true if this camera is currently live for some CinemachineBrain.</summary>
        public bool IsLive => CinemachineCore.IsLive(this);

        /// <summary>Check to see whether this camera is currently participating in a blend
        /// within its parent manager or in a CinemacineBrain</summary>
        /// <returns>True if the camera is participating in a blend</returns>
        public bool IsParticipatingInBlend()
        {
            if (IsLive)
            {
                var parent = ParentCamera as CinemachineCameraManagerBase;
                if (parent != null)
                    return (parent.ActiveBlend != null && parent.ActiveBlend.Uses(this)) || parent.IsParticipatingInBlend();
                var brain = CinemachineCore.FindPotentialTargetBrain(this);
                if (brain != null)
                    return brain.ActiveBlend != null && brain.ActiveBlend.Uses(this);
            }
            return false;
        }

        /// <summary>
        /// Temporarily cancel damping for this frame.  The camera will sanp to its target
        /// position when it is updated.
        /// </summary>
        /// <param name="updateNow">If true, snap the camera to its target immediately, otherwise wait
        /// until the end of the frame when cameras are normally updated.</param>
        public void CancelDamping(bool updateNow = false)
        {
            PreviousStateIsValid = false;
            if (updateNow)
            {
                var up = State.ReferenceUp;
                var brain = CinemachineCore.FindPotentialTargetBrain(this);
                if (brain != null)
                    up = brain.DefaultWorldUp;
                InternalUpdateCameraState(up, -1);
            }
        }
    }
}
