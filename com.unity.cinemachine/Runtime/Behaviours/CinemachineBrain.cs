//#define RESET_PROJECTION_MATRIX // GML todo: decide on the correct solution

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

#if CINEMACHINE_HDRP
    using UnityEngine.Rendering.HighDefinition;
#elif CINEMACHINE_URP
    using UnityEngine.Rendering.Universal;
#endif

namespace Unity.Cinemachine
{
    /// <summary>
    /// This interface is specifically for Timeline.  Do not use it.
    /// </summary>
    public interface ICameraOverrideStack
    {
        /// <summary>
        /// Override the current camera and current blend.  This setting will trump
        /// any in-game logic that sets virtual camera priorities and Enabled states.
        /// This is the main API for the timeline.
        /// </summary>
        /// <param name="overrideId">Id to represent a specific client.  An internal
        /// stack is maintained, with the most recent non-empty override taking precedence.
        /// This id must be > 0.  If you pass -1, a new id will be created, and returned.
        /// Use that id for subsequent calls.  Don't forget to
        /// call ReleaseCameraOverride after all overriding is finished, to
        /// free the OverrideStack resources.</param>
        /// <param name="camA">The camera to set, corresponding to weight=0.</param>
        /// <param name="camB">The camera to set, corresponding to weight=1.</param>
        /// <param name="weightB">The blend weight.  0=camA, 1=camB.</param>
        /// <param name="deltaTime">Override for deltaTime.  Should be Time.FixedDelta for
        /// time-based calculations to be included, -1 otherwise.</param>
        /// <returns>The override ID.  Don't forget to call ReleaseCameraOverride
        /// after all overriding is finished, to free the OverrideStack resources.</returns>
        int SetCameraOverride(
            int overrideId,
            ICinemachineCamera camA, ICinemachineCamera camB,
            float weightB, float deltaTime);

        /// <summary>
        /// See SetCameraOverride.  Call ReleaseCameraOverride after all overriding
        /// is finished, to free the OverrideStack resources.
        /// </summary>
        /// <param name="overrideId">The ID to released.  This is the value that
        /// was returned by SetCameraOverride</param>
        void ReleaseCameraOverride(int overrideId);

        /// <summary>
        /// Get the current definition of Up.  May be different from Vector3.up.
        /// </summary>
        Vector3 DefaultWorldUp { get; }
    }


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
//    [RequireComponent(typeof(Camera))] // strange but true: we can live without it
    [DisallowMultipleComponent]
    [ExecuteAlways]
    [AddComponentMenu("Cinemachine/Cinemachine Brain")]
    [SaveDuringPlay]
    [HelpURL(Documentation.BaseURL + "manual/CinemachineBrain.html")]
    public class CinemachineBrain : MonoBehaviour, ICameraOverrideStack
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
            = new LensModeOverrideSettings { DefaultMode = LensSettings.OverrideModes.Perspective };

        /// <summary>
        /// The blend which is used if you don't explicitly define a blend between two Virtual Cameras.
        /// </summary>
        [Tooltip("The blend that is used in cases where you haven't explicitly defined a "
            + "blend between two Virtual Cameras")]
        [FormerlySerializedAs("m_DefaultBlend")]
        public CinemachineBlendDefinition DefaultBlend
            = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, 2f);

        /// <summary>
        /// This is the asset which contains custom settings for specific blends.
        /// </summary>
        [Tooltip("This is the asset that contains custom settings for blends between "
            + "specific virtual cameras in your scene")]
        [FormerlySerializedAs("m_CustomBlends")]
        public CinemachineBlenderSettings CustomBlends = null;

        /// <summary>Event with a CinemachineBrain parameter</summary>
        [Serializable] public class BrainEvent : UnityEvent<CinemachineBrain> {}

        /// <summary>
        /// Event that is fired when a virtual camera is activated.
        /// The parameters are (incoming_vcam, outgoing_vcam), in that order.
        /// </summary>
        [Serializable] public class VcamActivatedEvent : UnityEvent<ICinemachineCamera, ICinemachineCamera> {}

        /// <summary>This event will fire whenever a virtual camera goes live and there is no blend</summary>
        [Tooltip("This event will fire whenever a virtual camera goes live and there is no blend")]
        [FormerlySerializedAs("m_CameraCutEvent")]
        public BrainEvent CameraCutEvent = new BrainEvent();

        /// <summary>This event will fire whenever a virtual camera goes live.  If a blend is involved,
        /// then the event will fire on the first frame of the blend.
        /// 
        /// The Parameters are (incoming_vcam, outgoing_vcam), in that order.</summary>
        [Tooltip("This event will fire whenever a virtual camera goes live.  If a blend is "
            + "involved, then the event will fire on the first frame of the blend.")]
        [FormerlySerializedAs("m_CameraActivatedEvent")]
        public VcamActivatedEvent CameraActivatedEvent = new VcamActivatedEvent();

        Camera m_OutputCamera = null; // never use directly - use accessor
        GameObject m_TargetOverride = null; // never use directly - use accessor
        Coroutine m_PhysicsCoroutine;
        int m_LastFrameUpdated;

        static CinemachineVirtualCameraBase s_SoloCamera;

        class BrainFrame
        {
            public int id;
            public CinemachineBlend blend = new CinemachineBlend(null, null, null, 0, 0);
            public bool Active { get { return blend.IsValid; } }

            // Working data - updated every frame
            public CinemachineBlend workingBlend = new CinemachineBlend(null, null, null, 0, 0);
            public BlendSourceVirtualCamera workingBlendSource = new BlendSourceVirtualCamera(null);

            // Used by Timeline Preview for overriding the current value of deltaTime
            public float deltaTimeOverride;

            // Used for blend reversal.  Range is 0...1,
            // representing where the blend started when reversed mid-blend
            public float blendStartPosition;
        }

        // Current game state is always frame 0, overrides are subsequent frames
        List<BrainFrame> m_FrameStack = new List<BrainFrame>();
        int m_NextFrameId = 1;

        // Current Brain State - result of all frames.  Blend camB is "current" camera always
        CinemachineBlend m_CurrentLiveCameras = new CinemachineBlend(null, null, null, 0, 0);
        
        // To avoid GC memory alloc every frame
        static readonly AnimationCurve s_DefaultLinearAnimationCurve = AnimationCurve.Linear(0, 0, 1, 1);

        WaitForFixedUpdate m_WaitForFixedUpdate = new WaitForFixedUpdate();

        ICinemachineCamera m_ActiveCameraPreviousFrame;
        CinemachineVirtualCameraBase m_ActiveCameraPreviousFrameGameObject;

        void OnValidate()
        {
            DefaultBlend.Time = Mathf.Max(0, DefaultBlend.Time);
#if UNITY_EDITOR
            EditorApplication.delayCall -= SetupRuntimeUIToolKit;
            EditorApplication.delayCall += SetupRuntimeUIToolKit;
#endif
        }

        void Reset()
        {
            ShowDebugText = false;
            ShowCameraFrustum = true;
            IgnoreTimeScale = false;
            WorldUpOverride = null;
            ChannelMask = OutputChannel.Channels.Default;
            UpdateMethod = UpdateMethods.SmartUpdate;
            BlendUpdateMethod = BrainUpdateMethods.LateUpdate;
            LensModeOverride = new LensModeOverrideSettings { DefaultMode = LensSettings.OverrideModes.Perspective };
            DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, 2f);
            CustomBlends = null;
            CameraCutEvent = new BrainEvent();
            CameraActivatedEvent = new VcamActivatedEvent();
        }

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
        public static CinemachineVirtualCameraBase SoloCamera
        {
            get => s_SoloCamera;
            set
            {
                if (value != null && !CinemachineCore.Instance.IsLive(value))
                    value.OnTransitionFromCamera(null, Vector3.up, CinemachineCore.DeltaTime);
                s_SoloCamera = value;
            }
        }

        /// <summary>API for the Unity Editor.</summary>
        /// <returns>Color used to indicate that a camera is in Solo mode.</returns>
        internal static Color GetSoloGUIColor() => Color.Lerp(Color.red, Color.yellow, 0.8f);

        /// <summary>Get the default world up for the virtual cameras.</summary>
        public Vector3 DefaultWorldUp => (WorldUpOverride != null) ? WorldUpOverride.transform.up : Vector3.up;

        void OnEnable()
        {
            // Make sure there is a first stack frame
            if (m_FrameStack.Count == 0)
                m_FrameStack.Add(new BrainFrame());

            CinemachineCore.Instance.AddActiveBrain(this);
            CinemachineDebug.OnGUIHandlers -= DebugTextHandler;
            CinemachineDebug.OnGUIHandlers += DebugTextHandler;

            // We check in after the physics system has had a chance to move things
            m_PhysicsCoroutine = StartCoroutine(AfterPhysics());

            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;

            CinemachineDebug.OnGUIHandlers -= DebugTextHandler;
            CinemachineCore.Instance.RemoveActiveBrain(this);
            m_FrameStack.Clear();
            StopCoroutine(m_PhysicsCoroutine);
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode) 
        { 
            if (Time.frameCount == m_LastFrameUpdated && m_FrameStack.Count > 0)
                ManualUpdate();
        }

        void OnSceneUnloaded(Scene scene)
        {
            if (Time.frameCount == m_LastFrameUpdated && m_FrameStack.Count > 0)
                ManualUpdate();
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
        
#if UNITY_EDITOR
        GameObject m_UIDocumentGo;
        UIDocument m_UIDocument;
        Label m_DebugLabel;
        void SetupRuntimeUIToolKit()
        {
            if (!ShowDebugText)
            {
                if (m_UIDocumentGo != null)
                    RuntimeUtility.DestroyObject(m_UIDocumentGo); // clean-up
                return;
            }
            
            if (m_UIDocumentGo == null)
            {
                m_UIDocumentGo = new GameObject("CinemachineRuntimeUI")
                {
                    transform = { parent = transform },
                    hideFlags = HideFlags.NotEditable | HideFlags.DontSaveInEditor,
                    tag = "EditorOnly"
                };
                m_UIDocument = m_UIDocumentGo.AddComponent<UIDocument>();
                const string path = "Packages/com.unity.cinemachine/Runtime/UI/";
                m_UIDocument.panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(path + "CinemachinePanelSettings.asset");
                m_UIDocument.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path + "CinemachineDebugText.uxml");
                
                m_DebugLabel = m_UIDocument.rootVisualElement.Q("DebugLabel") as Label;
            }
        }
        
        void OnGUI()
        {
            if (CinemachineDebug.OnGUIHandlers != null && Event.current.type != EventType.Layout)
                    CinemachineDebug.OnGUIHandlers(this);
        }
#endif
        
        void DebugTextHandler(CinemachineBrain brain)
        {
            if (!ShowDebugText || brain != this || m_DebugLabel == null) 
                return;

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

            if (IsBlending)
                sb.Append(ActiveBlend.Description);
            else
            {
                ICinemachineCamera vcam = ActiveVirtualCamera;
                if (vcam == null)
                    sb.Append("(none)");
                else
                {
                    sb.Append(vcam.Name);
                    var desc = vcam.Description;
                    if (!string.IsNullOrEmpty(desc))
                    {
                        sb.Append(" ");
                        sb.Append(desc);
                    }
                }
            }
            m_DebugLabel.text = sb.ToString();
            CinemachineDebug.ReturnToPool(sb);
        }

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
                    UpdateFrame0(Time.fixedDeltaTime);
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
                UpdateFrame0(deltaTime);

            ComputeCurrentBlend(ref m_CurrentLiveCameras, 0);

            if (UpdateMethod == UpdateMethods.FixedUpdate)
            {
                // Special handling for fixed update: cameras that have been enabled
                // since the last physics frame must be updated now
                if (BlendUpdateMethod != BrainUpdateMethods.FixedUpdate)
                {
                    CinemachineCore.Instance.m_CurrentUpdateFilter = CinemachineCore.UpdateFilter.Fixed;
                    if (SoloCamera == null)
                        m_CurrentLiveCameras.UpdateCameraState(
                            DefaultWorldUp, GetEffectiveDeltaTime(true));
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

        float GetEffectiveDeltaTime(bool fixedDelta)
        {
            if (CinemachineCore.UniformDeltaTimeOverride >= 0)
                return CinemachineCore.UniformDeltaTimeOverride;

            if (SoloCamera != null)
                return Time.unscaledDeltaTime;

            if (!Application.isPlaying)
            {
                for (int i = m_FrameStack.Count - 1; i > 0; --i)
                {
                    var frame = m_FrameStack[i];
                    if (frame.Active)
                        return frame.deltaTimeOverride;
                }
                return -1;
            }
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
            m_CurrentLiveCameras.UpdateCameraState(DefaultWorldUp, deltaTime);

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
        public ICinemachineCamera ActiveVirtualCamera
        {
            get
            {
                if (SoloCamera != null)
                    return SoloCamera;
                return DeepCamBFromBlend(m_CurrentLiveCameras);
            }
        }

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
        /// Checks if the vcam is live as part of an outgoing blend.  
        /// Does not check whether the vcam is also the current active vcam.
        /// </summary>
        /// <param name="vcam">The virtual camera to check</param>
        /// <returns>True if the virtual camera is part of a live outgoing blend, false otherwise</returns>
        public bool IsLiveInBlend(ICinemachineCamera vcam)
        {
            // Ignore m_CurrentLiveCameras.CamB
            if (vcam == m_CurrentLiveCameras.CamA)
                return true;
            if (m_CurrentLiveCameras.CamA is BlendSourceVirtualCamera b && b.Blend.Uses(vcam))
                return true;
            ICinemachineCamera parent = vcam.ParentCamera;
            if (parent != null && parent.IsLiveChild(vcam, false))
                return IsLiveInBlend(parent);
            return false;
        }

        /// <summary>
        /// Is there a blend in progress?
        /// </summary>
        public bool IsBlending { get { return ActiveBlend != null; } }

        /// <summary>
        /// Get the current blend in progress.  Returns null if none.
        /// It is also possible to set the current blend, but this is not a recommended usage.
        /// </summary>
        public CinemachineBlend ActiveBlend
        {
            get
            {
                if (SoloCamera != null)
                    return null;
                if (m_CurrentLiveCameras.CamA == null || m_CurrentLiveCameras.Equals(null) || m_CurrentLiveCameras.IsComplete)
                    return null;
                return m_CurrentLiveCameras;
            }
            set
            {
                if (value == null)
                    m_FrameStack[0].blend.Duration = 0;
                else
                    m_FrameStack[0].blend = value;
            }
        }

        /// Get the frame index corresponding to the ID
        int GetBrainFrame(int withId)
        {
            int count = m_FrameStack.Count;
            for (int i = count - 1; i > 0; --i)
                if (m_FrameStack[i].id == withId)
                    return i;
            // Not found - add it
            m_FrameStack.Add(new BrainFrame() { id = withId });
            return m_FrameStack.Count - 1;
        }

        
        /// <summary>
        /// This API is specifically for Timeline.  Do not use it.
        /// Override the current camera and current blend.  This setting will trump
        /// any in-game logic that sets virtual camera priorities and Enabled states.
        /// This is the main API for the timeline.
        /// </summary>
        /// <param name="overrideId">Id to represent a specific client.  An internal
        /// stack is maintained, with the most recent non-empty override taking precedence.
        /// This id must be > 0.  If you pass -1, a new id will be created, and returned.
        /// Use that id for subsequent calls.  Don't forget to
        /// call ReleaseCameraOverride after all overriding is finished, to
        /// free the OverrideStack resources.</param>
        /// <param name="camA"> The camera to set, corresponding to weight=0</param>
        /// <param name="camB"> The camera to set, corresponding to weight=1</param>
        /// <param name="weightB">The blend weight.  0=camA, 1=camB</param>
        /// <param name="deltaTime">override for deltaTime.  Should be Time.FixedDelta for
        /// time-based calculations to be included, -1 otherwise</param>
        /// <returns>The override ID.  Don't forget to call ReleaseCameraOverride
        /// after all overriding is finished, to free the OverrideStack resources.</returns>
        public int SetCameraOverride(
            int overrideId,
            ICinemachineCamera camA, ICinemachineCamera camB,
            float weightB, float deltaTime)
        {
            if (overrideId < 0)
                overrideId = m_NextFrameId++;

            BrainFrame frame = m_FrameStack[GetBrainFrame(overrideId)];
            frame.deltaTimeOverride = deltaTime;
            frame.blend.CamA = camA;
            frame.blend.CamB = camB;
            frame.blend.BlendCurve = s_DefaultLinearAnimationCurve;
            frame.blend.Duration = 1;
            frame.blend.TimeInBlend = weightB;

            // In case vcams are inactive game objects, make sure they get initialized properly
            var cam = camA as CinemachineVirtualCameraBase;
            if (cam != null)
                cam.EnsureStarted();
            cam = camB as CinemachineVirtualCameraBase;
            if (cam != null)
                cam.EnsureStarted();

            return overrideId;
        }

        /// <summary>
        /// This API is specifically for Timeline.  Do not use it.
        /// Release the resources used for a camera override client.
        /// See SetCameraOverride.
        /// </summary>
        /// <param name="overrideId">The ID to released.  This is the value that
        /// was returned by SetCameraOverride</param>
        public void ReleaseCameraOverride(int overrideId)
        {
            for (int i = m_FrameStack.Count - 1; i > 0; --i)
            {
                if (m_FrameStack[i].id == overrideId)
                {
                    m_FrameStack.RemoveAt(i);
                    return;
                }
            }
        }

        void ProcessActiveCamera(float deltaTime)
        {
            var activeCamera = ActiveVirtualCamera;
            if (SoloCamera != null)
            {
                var state = SoloCamera.State;
                PushStateToUnityCamera(ref state);
            }
            else if (activeCamera == null)
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
            else
            {
                // Has the current camera changed this frame?
                if (m_ActiveCameraPreviousFrameGameObject == null)
                    m_ActiveCameraPreviousFrame = null; // object was deleted
                if (activeCamera != m_ActiveCameraPreviousFrame)
                {
                    // Notify incoming camera of transition
                    activeCamera.OnTransitionFromCamera(
                        m_ActiveCameraPreviousFrame, DefaultWorldUp, deltaTime);
                    if (CameraActivatedEvent != null)
                        CameraActivatedEvent.Invoke(activeCamera, m_ActiveCameraPreviousFrame);

                    // If we're cutting without a blend, send an event
                    if (!IsBlending || (m_ActiveCameraPreviousFrame != null
                                && !ActiveBlend.Uses(m_ActiveCameraPreviousFrame)))
                    {
                        if (CameraCutEvent != null)
                            CameraCutEvent.Invoke(this);
                        if (CinemachineCore.CameraCutEvent != null)
                            CinemachineCore.CameraCutEvent.Invoke(this);
                    }
                    // Re-update in case it's inactive
                    activeCamera.UpdateCameraState(DefaultWorldUp, deltaTime);
                }
                // Apply the vcam state to the Unity camera
                var state = m_CurrentLiveCameras.State;
                PushStateToUnityCamera(ref state);
            }
            m_ActiveCameraPreviousFrame = activeCamera;
            m_ActiveCameraPreviousFrameGameObject = activeCamera as CinemachineVirtualCameraBase;
        }

        void UpdateFrame0(float deltaTime)
        {
            // Make sure there is a first stack frame
            if (m_FrameStack.Count == 0)
                m_FrameStack.Add(new BrainFrame());

            // Update the in-game frame (frame 0)
            BrainFrame frame = m_FrameStack[0];

            // Are we transitioning cameras?
            var activeCamera = TopCameraFromPriorityQueue();
            var outGoingCamera = frame.blend.CamB;

            if (activeCamera != outGoingCamera)
            {
                // Do we need to create a game-play blend?
                if ((UnityEngine.Object)activeCamera != null
                    && (UnityEngine.Object)outGoingCamera != null && deltaTime >= 0)
                {
                    // Create a blend (curve will be null if a cut)
                    var blendDef = LookupBlend(outGoingCamera, activeCamera);
                    float blendDuration = blendDef.BlendTime;
                    float blendStartPosition = 0;
                    if (blendDef.BlendCurve != null && blendDuration > UnityVectorExtensions.Epsilon)
                    {
                        if (frame.blend.IsComplete)
                            frame.blend.CamA = outGoingCamera;  // new blend
                        else
                        {
                            // Special case: if backing out of a blend-in-progress
                            // with the same blend in reverse, adjust the blend time
                            // to cancel out the progress made in the opposite direction
                            if ((frame.blend.CamA == activeCamera 
                                    || (frame.blend.CamA as BlendSourceVirtualCamera)?.Blend.CamB == activeCamera) 
                                && frame.blend.CamB == outGoingCamera)
                            {
                                // How far have we blended?  That is what we must undo
                                var progress = frame.blendStartPosition 
                                    + (1 - frame.blendStartPosition) * frame.blend.TimeInBlend / frame.blend.Duration;
                                blendDuration *= progress;
                                blendStartPosition = 1 - progress;
                            }
                            // Chain to existing blend
                            frame.blend.CamA = new BlendSourceVirtualCamera(
                                new CinemachineBlend(
                                    frame.blend.CamA, frame.blend.CamB,
                                    frame.blend.BlendCurve, frame.blend.Duration,
                                    frame.blend.TimeInBlend));
                        }
                    }
                    frame.blend.BlendCurve = blendDef.BlendCurve;
                    frame.blend.Duration = blendDuration;
                    frame.blend.TimeInBlend = 0;
                    frame.blendStartPosition = blendStartPosition;
                }
                // Set the current active camera
                frame.blend.CamB = activeCamera;
            }

            // Advance the current blend (if any)
            if (frame.blend.CamA != null)
            {
                frame.blend.TimeInBlend += (deltaTime >= 0) ? deltaTime : frame.blend.Duration;
                if (frame.blend.IsComplete)
                {
                    // No more blend
                    frame.blend.CamA = null;
                    frame.blend.BlendCurve = null;
                    frame.blend.Duration = 0;
                    frame.blend.TimeInBlend = 0;
                }
            }
        }

        /// <summary>
        /// Used internally to compute the current blend, taking into account
        /// the in-game camera and all the active overrides.  Caller may optionally
        /// exclude n topmost overrides.
        /// </summary>
        /// <param name="outputBlend">Receives the nested blend</param>
        /// <param name="numTopLayersToExclude">Optionally exclude the last number 
        /// of overrides from the blend</param>
        public void ComputeCurrentBlend(
            ref CinemachineBlend outputBlend, int numTopLayersToExclude)
        {
            // Make sure there is a first stack frame
            if (m_FrameStack.Count == 0)
                m_FrameStack.Add(new BrainFrame());

            // Resolve the current working frame states in the stack
            int lastActive = 0;
            int topLayer = Mathf.Max(1, m_FrameStack.Count - numTopLayersToExclude);
            for (int i = 0; i < topLayer; ++i)
            {
                BrainFrame frame = m_FrameStack[i];
                if (i == 0 || frame.Active)
                {
                    frame.workingBlend.CamA = frame.blend.CamA;
                    frame.workingBlend.CamB = frame.blend.CamB;
                    frame.workingBlend.BlendCurve = frame.blend.BlendCurve;
                    frame.workingBlend.Duration = frame.blend.Duration;
                    frame.workingBlend.TimeInBlend = frame.blend.TimeInBlend;
                    if (i > 0 && !frame.blend.IsComplete)
                    {
                        if (frame.workingBlend.CamA == null)
                        {
                            if (m_FrameStack[lastActive].blend.IsComplete)
                                frame.workingBlend.CamA = m_FrameStack[lastActive].blend.CamB;
                            else
                            {
                                frame.workingBlendSource.Blend = m_FrameStack[lastActive].workingBlend;
                                frame.workingBlend.CamA = frame.workingBlendSource;
                            }
                        }
                        else if (frame.workingBlend.CamB == null)
                        {
                            if (m_FrameStack[lastActive].blend.IsComplete)
                                frame.workingBlend.CamB = m_FrameStack[lastActive].blend.CamB;
                            else
                            {
                                frame.workingBlendSource.Blend = m_FrameStack[lastActive].workingBlend;
                                frame.workingBlend.CamB = frame.workingBlendSource;
                            }
                        }
                    }
                    lastActive = i;
                }
            }
            var workingBlend = m_FrameStack[lastActive].workingBlend;
            outputBlend.CamA = workingBlend.CamA;
            outputBlend.CamB = workingBlend.CamB;
            outputBlend.BlendCurve = workingBlend.BlendCurve;
            outputBlend.Duration = workingBlend.Duration;
            outputBlend.TimeInBlend = workingBlend.TimeInBlend;
        }

        /// <summary>
        /// True if the ICinemachineCamera the current active camera,
        /// or part of a current blend, either directly or indirectly because its parents are live.
        /// </summary>
        /// <param name="vcam">The camera to test whether it is live</param>
        /// <param name="dominantChildOnly">If true, will only return true if this vcam is the dominant live child</param>
        /// <returns>True if the camera is live (directly or indirectly)
        /// or part of a blend in progress.</returns>
        public bool IsLive(ICinemachineCamera vcam, bool dominantChildOnly = false)
        {
            if ((ICinemachineCamera)SoloCamera == vcam)
                return true;
            if (m_CurrentLiveCameras.Uses(vcam))
                return true;

            ICinemachineCamera parent = vcam.ParentCamera;
            while (parent != null && parent.IsLiveChild(vcam, dominantChildOnly))
            {
                if ((ICinemachineCamera)SoloCamera == parent || m_CurrentLiveCameras.Uses(parent))
                    return true;
                vcam = parent;
                parent = vcam.ParentCamera;
            }
            return false;
        }

        /// <summary>
        /// The current state applied to the unity camera (may be the result of a blend)
        /// </summary>
        public CameraState CurrentCameraState { get; private set; }

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

        /// <summary>
        /// Create a blend curve for blending from one ICinemachineCamera to another.
        /// If there is a specific blend defined for these cameras it will be used, otherwise
        /// a default blend will be created, which could be a cut.
        /// </summary>
        CinemachineBlendDefinition LookupBlend(
            ICinemachineCamera fromKey, ICinemachineCamera toKey)
        {
            // Get the blend curve that's most appropriate for these cameras
            CinemachineBlendDefinition blend = DefaultBlend;
            if (CustomBlends != null)
            {
                string fromCameraName = (fromKey != null) ? fromKey.Name : string.Empty;
                string toCameraName = (toKey != null) ? toKey.Name : string.Empty;
                blend = CustomBlends.GetBlendForVirtualCameras(
                        fromCameraName, toCameraName, blend);
            }
            if (CinemachineCore.GetBlendOverride != null)
                blend = CinemachineCore.GetBlendOverride(fromKey, toKey, blend, this);
            return blend;
        }

        /// <summary> Apply a cref="CameraState"/> to the game object</summary>
        void PushStateToUnityCamera(ref CameraState state)
        {
            CurrentCameraState = state;
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
