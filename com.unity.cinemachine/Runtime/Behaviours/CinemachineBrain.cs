//#define RESET_PROJECTION_MATRIX // GML todo: decide on the correct solution

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace Unity.Cinemachine
{
    /// <summary>
    /// CinemachineBrain is the link between the Unity Camera and the Cinemachine Virtual
    /// Cameras in the scene.  It monitors the priority stack to choose the current
    /// Virtual Camera, and blend with another if necessary.  Finally and most importantly,
    /// it applies the Virtual Camera state to the attached Unity Camera.
    ///
    /// The CinemachineBrain is also the place where rules for blending between virtual cameras
    /// are defined.  Camera blending is an interpolation over time of one virtual camera
    /// position and state to another. If you think of virtual cameras as cameramen, then
    /// blending is a little like one cameraman smoothly passing the camera to another cameraman.
    /// You can specify the time over which to blend, as well as the blend curve shape.
    /// Note that a camera cut is just a zero-time blend.
    /// </summary>
    [DisallowMultipleComponent]
    [ExecuteAlways]
    [AddComponentMenu("Cinemachine/Cinemachine Brain")]
    [SaveDuringPlay]
    [HelpURL(Documentation.BaseURL + "manual/CinemachineBrain.html")]
    public class CinemachineBrain : MonoBehaviour, ICameraOverrideStack, ICinemachineMixer
    {
        /// <summary>
        /// When enabled, the current camera and blend will be indicated in the 
        /// game window, for debugging.
        /// </summary>
        [Tooltip("When enabled, the current camera and blend will be indicated in "
            + "the game window, for debugging")]
        [FormerlySerializedAs("m_ShowDebugText")]
        public bool ShowDebugText = false;

        /// <summary>
        /// When enabled, shows the camera's frustum in the scene view.
        /// </summary>
        [Tooltip("When enabled, the camera's frustum will be shown at all times "
            + "in the scene view")]
        [FormerlySerializedAs("m_ShowCameraFrustum")]
        public bool ShowCameraFrustum = true;

        /// <summary>
        /// When enabled, the cameras will always respond in real-time to user input and damping,
        /// even if the game is running in slow motion
        /// </summary>
        [Tooltip("When enabled, the cameras will always respond in real-time to user input "
            + "and damping, even if the game is running in slow motion")]
        [FormerlySerializedAs("m_IgnoreTimeScale")]
        public bool IgnoreTimeScale = false;

        /// <summary>
        /// If set, this object's Y axis will define the world-space Up vector for all the
        /// virtual cameras.  This is useful in top-down game environments.  If not set, Up is world-space Y.
        /// </summary>
        [Tooltip("If set, this object's Y axis will define the world-space Up vector for all the "
            + "virtual cameras.  This is useful for instance in top-down game environments.  "
            + "If not set, Up is world-space Y.  Setting this appropriately is important, "
            + "because Virtual Cameras don't like looking straight up or straight down.")]
        [FormerlySerializedAs("m_WorldUpOverride")]
        public Transform WorldUpOverride;

        /// <summary>The CinemachineBrain will find the highest-priority CinemachineCamera that outputs to any of the channels selected. 
        /// CinemachineCameras that do not output to one of these channels will be ignored.  Use this in situations where multiple
        /// CinemachineBrains are needed (for example, Split-screen).</summary>
        [Tooltip("The CinemachineBrain will find the highest-priority CinemachineCamera that outputs to any of the channels selected. "
            + "CinemachineCameras that do not output to one of these channels will be ignored.  Use this in situations "
            + "where multiple CinemachineBrains are needed (for example, Split-screen).")]
        public OutputChannel.Channels ChannelMask = OutputChannel.Channels.Default;

        /// <summary>This enum defines the options available for the update method.</summary>
        public enum UpdateMethods
        {
            /// <summary>Virtual cameras are updated in sync with the Physics module, in FixedUpdate</summary>
            FixedUpdate,
            /// <summary>Virtual cameras are updated in MonoBehaviour LateUpdate.</summary>
            LateUpdate,
            /// <summary>Virtual cameras are updated according to how the target is updated.</summary>
            SmartUpdate,
            /// <summary>Virtual cameras are not automatically updated, client must explicitly call 
            /// the CinemachineBrain's ManualUpdate() method.</summary>
            ManualUpdate
        };

        /// <summary>Depending on how the target objects are animated, adjust the update method to
        /// minimize the potential jitter.  Use FixedUpdate if all your targets are animated with for RigidBody animation.
        /// SmartUpdate will choose the best method for each virtual camera, depending
        /// on how the target is animated.</summary>
        [Tooltip("The update time for the vcams.  Use FixedUpdate if all your targets are animated "
            + "during FixedUpdate (e.g. RigidBodies), LateUpdate if all your targets are animated "
            + "during the normal Update loop, and SmartUpdate if you want Cinemachine to do the "
            + "appropriate thing on a per-target basis.  SmartUpdate is the recommended setting")]
        [FormerlySerializedAs("m_UpdateMethod")]
        public UpdateMethods UpdateMethod = UpdateMethods.SmartUpdate;

        /// <summary>This enum defines the options available for the update method.</summary>
        public enum BrainUpdateMethods
        {
            /// <summary>Camera is updated in sync with the Physics module, in FixedUpdate</summary>
            FixedUpdate,
            /// <summary>Camera is updated in MonoBehaviour LateUpdate (or when ManualUpdate is called).</summary>
            LateUpdate
        };

        /// <summary>The update time for the Brain, i.e. when the blends are evaluated and the
        /// brain's transform is updated.</summary>
        [Tooltip("The update time for the Brain, i.e. when the blends are evaluated and "
            + "the brain's transform is updated")]
        [FormerlySerializedAs("m_BlendUpdateMethod")]
        public BrainUpdateMethods BlendUpdateMethod = BrainUpdateMethods.LateUpdate;

        /// <summary>Defines the settings for Lens Mode overriding</summary>
        [Serializable] 
        public struct LensModeOverrideSettings
        {
            /// <summary>If set, will enable CM cameras to override the lens mode of the camera</summary>
            [Tooltip("If set, will enable CM cameras to override the lens mode of the camera")]
            public bool Enabled;

            /// <summary>Lens mode to use when no mode override is active</summary>
            [Tooltip("Lens mode to use when no mode override is active")]
            public LensSettings.OverrideModes DefaultMode;
        }

        /// <summary>Controls whether CM cameras can change the lens mode.</summary>
        [FoldoutWithEnabledButton]
        public LensModeOverrideSettings LensModeOverride 
            = new () { DefaultMode = LensSettings.OverrideModes.Perspective };

        /// <summary>
        /// The blend which is used if you don't explicitly define a blend between two Virtual Cameras.
        /// </summary>
        [Tooltip("The blend that is used in cases where you haven't explicitly defined a "
            + "blend between two Virtual Cameras")]
        [FormerlySerializedAs("m_DefaultBlend")]
        public CinemachineBlendDefinition DefaultBlend = new (CinemachineBlendDefinition.Styles.EaseInOut, 2f);

        /// This is the asset that contains custom settings for specific blends.
        /// </summary>
        [Tooltip("This is the asset that contains custom settings for blends between "
            + "specific virtual cameras in your scene")]
        [FormerlySerializedAs("m_CustomBlends")]
        public CinemachineBlenderSettings CustomBlends = null;

        /// <summary>
        /// Event that is fired when a virtual camera is activated.  
        /// If a blend is involved, it will be fired at the start of the blend.
        /// </summary>
        [Tooltip("This event will fire whenever a virtual camera goes live.  If a blend is "
            + "involved, then the event will fire on the first frame of the blend.")]
        public ICinemachineCamera.ActivationEvent CameraActivatedEvent = new ();

        /// <summary>Event with CinemachineBrain as parameter.</summary>
        public class BrainEvent : UnityEvent<CinemachineBrain> {}

        Camera m_OutputCamera = null; // never use directly - use accessor
        GameObject m_TargetOverride = null; // never use directly - use 
        Coroutine m_PhysicsCoroutine;
        int m_LastFrameUpdated;
        readonly WaitForFixedUpdate m_WaitForFixedUpdate = new ();
        readonly BlendManager m_BlendManager = new ();
        CameraState m_CameraState;

        static ICinemachineCamera s_SoloCamera;

        void OnValidate()
        {
            DefaultBlend.Time = Mathf.Max(0, DefaultBlend.Time);
        }

        void Reset()
        {
            DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, 2f);
            CustomBlends = null;
            ShowDebugText = false;
            ShowCameraFrustum = true;
            IgnoreTimeScale = false;
            WorldUpOverride = null;
            ChannelMask = OutputChannel.Channels.Default;
            UpdateMethod = UpdateMethods.SmartUpdate;
            BlendUpdateMethod = BrainUpdateMethods.LateUpdate;
            LensModeOverride = new LensModeOverrideSettings { DefaultMode = LensSettings.OverrideModes.Perspective };
        }
       
        void Awake()
        {
            ControlledObject.TryGetComponent(out m_OutputCamera);
        }

        void Start()
        {
            m_LastFrameUpdated = -1;
            UpdateVirtualCameras(CinemachineCore.UpdateFilter.Late, -1f);
        }

        void OnEnable()
        {
            m_BlendManager.OnEnable(this);

            CinemachineCore.Instance.AddActiveBrain(this);
            CinemachineDebug.OnGUIHandlers -= OnGuiHandler;
            CinemachineDebug.OnGUIHandlers += OnGuiHandler;

            // We check in after the physics system has had a chance to move things
            m_PhysicsCoroutine = StartCoroutine(AfterPhysics());

            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;

            CinemachineDebug.OnGUIHandlers -= OnGuiHandler;
            CinemachineCore.Instance.RemoveActiveBrain(this);

            m_BlendManager.OnDisable();
            StopCoroutine(m_PhysicsCoroutine);
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode) 
        { 
            if (Time.frameCount == m_LastFrameUpdated && m_BlendManager.IsInitialized)
                ManualUpdate();
        }

        void OnSceneUnloaded(Scene scene)
        {
            if (Time.frameCount == m_LastFrameUpdated && m_BlendManager.IsInitialized)
                ManualUpdate();
        }
        
        /// <inheritdoc />
        public int SetCameraOverride(
            int overrideId,
            ICinemachineCamera camA, ICinemachineCamera camB,
            float weightB, float deltaTime) => m_BlendManager.SetCameraOverride(overrideId, camA, camB, weightB, deltaTime);

        /// <inheritdoc />
        public void ReleaseCameraOverride(int overrideId) => m_BlendManager.ReleaseCameraOverride(overrideId);
        
        /// <summary>Get the default world up for the virtual cameras.</summary>
        public Vector3 DefaultWorldUp => (WorldUpOverride != null) ? WorldUpOverride.transform.up : Vector3.up;

        /// <summary>
        /// True if the ICinemachineCamera the current active camera,
        /// or part of a current blend, either directly or indirectly because its parents are live.
        /// </summary>
        /// <param name="vcam">The camera to test whether it is live</param>
        /// <param name="dominantChildOnly">If true, will only return true if this vcam is the dominant live child</param>
        /// <returns>True if the camera is live (directly or indirectly)
        /// or part of a blend in progress.</returns>
        public bool IsLiveChild(ICinemachineCamera cam, bool dominantChildOnly = false)
            => (ICinemachineCamera)SoloCamera == cam || m_BlendManager.IsLive(cam, dominantChildOnly);

        /// <inheritdoc />
        public string Name => name;
       
        /// <inheritdoc />
        public string Description
        {
            get
            {
                if (IsBlending)
                    return ActiveBlend.Description;
                if (ActiveVirtualCamera == null)
                    return "(none)";
                return $"{ActiveVirtualCamera.Name} {ActiveVirtualCamera.Description}";
            }
        }

        /// <inheritdoc />
        public CameraState State => m_CameraState;

        /// <inheritdoc />
        public bool IsValid => this != null;

        /// <summary>Does nothing</summary>
        public void UpdateCameraState(Vector3 up, float deltaTime) {} // GML todo

        /// <summary>Invokes CameraActivatedEvent</summary>
        public void OnCameraActivated(ICinemachineCamera.ActivationEventParams evt) 
            => CameraActivatedEvent.Invoke(evt);
        
        /// <summary>Does nothing</summary>
        public ICinemachineMixer ParentCamera => null; // GML todo

        /// <summary>
        /// Get the Unity Camera that is attached to this GameObject.  This is the camera
        /// that will be controlled by the brain.
        /// </summary>
        public Camera OutputCamera
        {
            get
            {
                if (m_OutputCamera == null && !Application.isPlaying)
                    ControlledObject.TryGetComponent(out m_OutputCamera);
                return m_OutputCamera;
            }
        }
        
        /// <summary>
        /// CinemachineBrain controls this GameObject.  Normally, this is the GameObject to which 
        /// the CinemachineBrain component is attached.  However, it is possible to override this
        /// by setting this property to another GameObject.  If a Camera component is attached to the 
        /// Controlled Object, then that Camera component's lens settings will also be driven 
        /// by the CinemachineBrain.
        /// If this property is set to null, then CinemachineBrain is controlling the GameObject 
        /// to which it is attached.  The value of this property will always report as non-null.
        /// </summary>
        public GameObject ControlledObject
        {
            get => m_TargetOverride == null ? gameObject : m_TargetOverride;
            set
            {
                if (!ReferenceEquals(m_TargetOverride, value))
                {
                    m_TargetOverride = value;
                    ControlledObject.TryGetComponent(out m_OutputCamera); // update output camera when target changes
                }
            }
        }

        /// <summary>
        /// API for the Unity Editor.
        /// Show this camera no matter what.  This is static, and so affects all Cinemachine brains.
        /// </summary>
        public static ICinemachineCamera SoloCamera
        {
            get => s_SoloCamera;
            set
            {
                if (value != null && !CinemachineCore.Instance.IsLive(value))
                    value.OnCameraActivated(new ICinemachineCamera.ActivationEventParams
                    {
                        Origin = null,
                        OutgoingCamera = null,
                        IncomingCamera = value,
                        IsCut = true,
                        WorldUp = Vector3.up,
                        DeltaTime = CinemachineCore.DeltaTime
                    });
                s_SoloCamera = value;
            }
        }

        /// <summary>API for the Unity Editor.</summary>
        /// <returns>Color used to indicate that a camera is in Solo mode.</returns>
        internal static Color GetSoloGUIColor() => Color.Lerp(Color.red, Color.yellow, 0.8f);

        void OnGuiHandler(CinemachineBrain brain)
        {
#if CINEMACHINE_UNITY_IMGUI
            if (ShowDebugText && brain == this)
            {
                // Show the active camera and blend
                var sb = CinemachineDebug.SBFromPool();
                Color color = GUI.color;
                sb.Length = 0;
                sb.Append("CM ");
                sb.Append(gameObject.name);
                sb.Append(": ");
                if (SoloCamera != null)
                {
                    sb.Append("SOLO ");
                    GUI.color = GetSoloGUIColor();
                }
                sb.Append(Description);
                string text = sb.ToString();
                Rect r = CinemachineDebug.GetScreenPos(OutputCamera, text, GUI.skin.box);
                GUI.Label(r, text, GUI.skin.box);
                GUI.color = color;
                CinemachineDebug.ReturnToPool(sb);
            }
#endif
        }

#if UNITY_EDITOR
        void OnGUI()
        {
            if (CinemachineDebug.OnGUIHandlers != null && Event.current.type != EventType.Layout)
                CinemachineDebug.OnGUIHandlers(this);
        }
#endif

        IEnumerator AfterPhysics()
        {
            while (true)
            {
                // FixedUpdate can be called multiple times per frame
                yield return m_WaitForFixedUpdate;
                if (UpdateMethod == UpdateMethods.FixedUpdate
                    || UpdateMethod == UpdateMethods.SmartUpdate)
                {
                    CinemachineCore.UpdateFilter filter = CinemachineCore.UpdateFilter.Fixed;
                    if (UpdateMethod == UpdateMethods.SmartUpdate)
                    {
                        // Track the targets
                        UpdateTracker.OnUpdate(UpdateTracker.UpdateClock.Fixed);
                        filter = CinemachineCore.UpdateFilter.SmartFixed;
                    }
                    UpdateVirtualCameras(filter, GetEffectiveDeltaTime(true));
                }
                // Choose the active vcam and apply it to the Unity camera
                if (BlendUpdateMethod == BrainUpdateMethods.FixedUpdate)
                {
                    m_BlendManager.UpdateRootFrame(TopCameraFromPriorityQueue(), Time.fixedDeltaTime, LookupBlend);
                    ProcessActiveCamera(Time.fixedDeltaTime);
                }
            }
        }

        void LateUpdate()
        {
            if (UpdateMethod != UpdateMethods.ManualUpdate)
                ManualUpdate();
        }

        /// <summary>
        /// Call this method explicitly from an external script to update the virtual cameras
        /// and position the main camera, if the UpdateMode is set to ManualUpdate.
        /// For other update modes, this method is called automatically, and should not be
        /// called from elsewhere.
        /// </summary>
        public void ManualUpdate()
        {
            m_LastFrameUpdated = Time.frameCount;

            float deltaTime = GetEffectiveDeltaTime(false);
            if (!Application.isPlaying || BlendUpdateMethod != BrainUpdateMethods.FixedUpdate)
                m_BlendManager.UpdateRootFrame(TopCameraFromPriorityQueue(), deltaTime, LookupBlend);

            m_BlendManager.ComputeCurrentBlend();

            if (UpdateMethod == UpdateMethods.FixedUpdate)
            {
                // Special handling for fixed update: cameras that have been enabled
                // since the last physics frame must be updated now
                if (BlendUpdateMethod != BrainUpdateMethods.FixedUpdate)
                {
                    CinemachineCore.Instance.m_CurrentUpdateFilter = CinemachineCore.UpdateFilter.Fixed;
                    if (SoloCamera == null)
                        m_BlendManager.RefreshCurrentCameraState(DefaultWorldUp, GetEffectiveDeltaTime(true));
                }
            }
            else
            {
                CinemachineCore.UpdateFilter filter = CinemachineCore.UpdateFilter.Late;
                if (UpdateMethod == UpdateMethods.SmartUpdate)
                {
                    // Track the targets
                    UpdateTracker.OnUpdate(UpdateTracker.UpdateClock.Late);
                    filter = CinemachineCore.UpdateFilter.SmartLate;
                }
                UpdateVirtualCameras(filter, deltaTime);
            }

            // Choose the active vcam and apply it to the Unity camera
            if (!Application.isPlaying || BlendUpdateMethod != BrainUpdateMethods.FixedUpdate)
                ProcessActiveCamera(deltaTime);
        }

#if UNITY_EDITOR
        /// This is only needed in editor mode to force timeline to call OnGUI while
        /// timeline is up and the game is not running, in order to allow dragging
        /// the composer guide in the game view.
        void OnPreCull()
        {
            if (!Application.isPlaying)
            {
                // Note: this call will cause any screen canvas attached to the camera
                // to be painted one frame out of sync.  It will only happen in the editor when not playing.
                ProcessActiveCamera(GetEffectiveDeltaTime(false));
            }
        }
#endif

        void ProcessActiveCamera(float deltaTime)
        {
            if (SoloCamera != null)
            {
                var state = SoloCamera.State;
                PushStateToUnityCamera(ref state);
            }
            else if (m_BlendManager.ProcessActiveCamera(DefaultWorldUp, deltaTime) != null)
            {
                // Apply the vcam state to the Unity camera
                var state = m_BlendManager.CameraState;
                PushStateToUnityCamera(ref state);
            }
            else
            {
                // No active virtual camera.  We create a state representing its position
                // and call the callback, but we don't actively set the transform or lens
                var state = CameraState.Default;
                var target = ControlledObject.transform;
                state.RawPosition = target.position;
                state.RawOrientation = target.rotation;
                state.Lens = LensSettings.FromCamera(m_OutputCamera);
                state.BlendHint |= CameraState.BlendHintValue.NoTransform | CameraState.BlendHintValue.NoLens;
                PushStateToUnityCamera(ref state);
            }
        }
        
        /// <summary>
        /// Create a blend curve for blending from one ICinemachineCamera to another.
        /// If there is a specific blend defined for these cameras it will be used, otherwise
        /// a default blend will be created, which could be a cut.
        /// </summary>
        CinemachineBlendDefinition LookupBlend(ICinemachineCamera fromKey, ICinemachineCamera toKey)
        {
            // Get the blend curve that's most appropriate for these cameras
            CinemachineBlendDefinition blend = DefaultBlend;
            if (CustomBlends != null)
            {
                string fromCameraName = (fromKey != null) ? fromKey.Name : string.Empty;
                string toCameraName = (toKey != null) ? toKey.Name : string.Empty;
                blend = CustomBlends.GetBlendForVirtualCameras(fromCameraName, toCameraName, blend);
            }
            if (CinemachineCore.GetBlendOverride != null)
                blend = CinemachineCore.GetBlendOverride(fromKey, toKey, blend, this);
            return blend;
        }
        
        float GetEffectiveDeltaTime(bool fixedDelta)
        {
            if (CinemachineCore.UniformDeltaTimeOverride >= 0)
                return CinemachineCore.UniformDeltaTimeOverride;

            if (SoloCamera != null)
                return Time.unscaledDeltaTime;

            if (!Application.isPlaying)
                return m_BlendManager.GetDeltaTimeOverride();

            if (IgnoreTimeScale)
                return fixedDelta ? Time.fixedDeltaTime : Time.unscaledDeltaTime;

            return fixedDelta ? Time.fixedDeltaTime : Time.deltaTime;
        }

        void UpdateVirtualCameras(CinemachineCore.UpdateFilter updateFilter, float deltaTime)
        {
            // We always update all active virtual cameras
            CinemachineCore.Instance.m_CurrentUpdateFilter = updateFilter;
            CinemachineCore.Instance.UpdateAllActiveVirtualCameras((uint)ChannelMask, DefaultWorldUp, deltaTime);

            // Make sure all live cameras get updated, in case some of them are deactivated
            if (SoloCamera != null)
                SoloCamera.UpdateCameraState(DefaultWorldUp, deltaTime);
            m_BlendManager.RefreshCurrentCameraState(DefaultWorldUp, deltaTime);

            // Restore the filter for general use
            updateFilter = CinemachineCore.UpdateFilter.Late;
            if (Application.isPlaying)
            {
                if (UpdateMethod == UpdateMethods.SmartUpdate)
                    updateFilter |= CinemachineCore.UpdateFilter.Smart;
                else if (UpdateMethod == UpdateMethods.FixedUpdate)
                    updateFilter = CinemachineCore.UpdateFilter.Fixed;
            }
            CinemachineCore.Instance.m_CurrentUpdateFilter = updateFilter;
        }

        /// <summary>
        /// Get the current active virtual camera.
        /// </summary>
        public ICinemachineCamera ActiveVirtualCamera => SoloCamera ?? m_BlendManager.ActiveVirtualCamera;

        /// <summary>
        /// Checks if the vcam is live as part of an outgoing blend.  
        /// Does not check whether the vcam is also the current active vcam.
        /// </summary>
        /// <param name="vcam">The virtual camera to check</param>
        /// <returns>True if the virtual camera is part of a live outgoing blend, false otherwise</returns>
        public bool IsLiveInBlend(ICinemachineCamera cam) => m_BlendManager.IsLiveInBlend(cam);

        /// <summary>
        /// Is there a blend in progress?
        /// </summary>
        public bool IsBlending => m_BlendManager.IsBlending;

        /// <summary>
        /// Get the current blend in progress.  Returns null if none.
        /// It is also possible to set the current blend, but this is not a recommended usage.
        /// </summary>
        public CinemachineBlend ActiveBlend 
        {
            get => m_BlendManager.ActiveBlend;
            set => m_BlendManager.ActiveBlend = value;
        }

        /// <summary>Returns true if camera is on a channel that is handles by this Brain.</summary>
        /// <param name="vcam">The camera to check</param>
        /// <returns></returns>
        public bool IsValidChannel(CinemachineVirtualCameraBase vcam) 
            => vcam != null && ((uint)vcam.OutputChannel.Value & (uint)ChannelMask) != 0;

        /// <summary>
        /// Get the highest-priority Enabled ICinemachineCamera
        /// that is visible to my camera.  Culling Mask is used to test visibility.
        /// </summary>
        ICinemachineCamera TopCameraFromPriorityQueue()
        {
            CinemachineCore core = CinemachineCore.Instance;
            int numCameras = core.VirtualCameraCount;
            for (int i = 0; i < numCameras; ++i)
            {
                var cam = core.GetVirtualCamera(i);
                if (IsValidChannel( core.GetVirtualCamera(i)))
                    return cam;
            }
            return null;
        }

        /// <summary> Apply a cref="CameraState"/> to the game object</summary>
        void PushStateToUnityCamera(ref CameraState state)
        {
            m_CameraState = state;
            var target = ControlledObject.transform;
            if ((state.BlendHint & CameraState.BlendHintValue.NoPosition) == 0)
                target.position = state.GetFinalPosition();
            if ((state.BlendHint & CameraState.BlendHintValue.NoOrientation) == 0)
                target.rotation = state.GetFinalOrientation();
            if ((state.BlendHint & CameraState.BlendHintValue.NoLens) == 0)
            {
                Camera cam = OutputCamera;
                if (cam != null)
                {
                    bool isPhysical = cam.usePhysicalProperties;
#if RESET_PROJECTION_MATRIX
                    cam.ResetProjectionMatrix();
#endif
                    cam.nearClipPlane = state.Lens.NearClipPlane;
                    cam.farClipPlane = state.Lens.FarClipPlane;
                    cam.orthographicSize = state.Lens.OrthographicSize;
                    cam.fieldOfView = state.Lens.FieldOfView;
                    
#if RESET_PROJECTION_MATRIX
                    if (!LensModeOverride.Enabled)
                        cam.usePhysicalProperties = isPhysical; // because ResetProjectionMatrix resets it
                    else
#else
                    if (LensModeOverride.Enabled)
#endif
                    {
                        if (state.Lens.ModeOverride != LensSettings.OverrideModes.None)
                        {
                            isPhysical = state.Lens.IsPhysicalCamera;
                            cam.orthographic = state.Lens.ModeOverride == LensSettings.OverrideModes.Orthographic;
                        }
                        else if (LensModeOverride.DefaultMode != LensSettings.OverrideModes.None)
                        {
                            isPhysical = LensModeOverride.DefaultMode == LensSettings.OverrideModes.Physical;
                            cam.orthographic = LensModeOverride.DefaultMode == LensSettings.OverrideModes.Orthographic;
                        }
                        cam.usePhysicalProperties = isPhysical;
                    }

                    if (isPhysical)
                    {
                        cam.sensorSize = state.Lens.PhysicalProperties.SensorSize;
                        cam.gateFit = state.Lens.PhysicalProperties.GateFit;
                        cam.focalLength = Camera.FieldOfViewToFocalLength(
                            state.Lens.FieldOfView, state.Lens.PhysicalProperties.SensorSize.y);
                        cam.lensShift = state.Lens.PhysicalProperties.LensShift;
                        cam.focusDistance = state.Lens.PhysicalProperties.FocusDistance;
                        cam.iso = state.Lens.PhysicalProperties.Iso;
                        cam.shutterSpeed = state.Lens.PhysicalProperties.ShutterSpeed;
                        cam.aperture = state.Lens.PhysicalProperties.Aperture;
                        cam.bladeCount = state.Lens.PhysicalProperties.BladeCount;
                        cam.curvature = state.Lens.PhysicalProperties.Curvature;
                        cam.barrelClipping = state.Lens.PhysicalProperties.BarrelClipping;
                        cam.anamorphism = state.Lens.PhysicalProperties.Anamorphism;
                    }
                }
            }
            if (CinemachineCore.CameraUpdatedEvent != null)
                CinemachineCore.CameraUpdatedEvent.Invoke(this);
        }
    }
}
