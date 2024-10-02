#if !CINEMACHINE_NO_CM2_SUPPORT
using UnityEngine;
using UnityEngine.Serialization;
using System;
using System.Collections.Generic;

namespace Unity.Cinemachine
{
    /// <summary>
    /// This is a deprecated component.  Use CinemachineCamera instead.
    /// </summary>
    [Obsolete("This is deprecated. Use Create -> Cinemachine -> FreeLook camera, or " +
        "create a CinemachineCamera with appropriate components")]
    [DisallowMultipleComponent]
    [ExecuteAlways]
    [ExcludeFromPreset]
    [AddComponentMenu("")] // Don't display in add component menu
    public class CinemachineFreeLook : CinemachineVirtualCameraBase, AxisState.IRequiresInput, ICinemachineMixer
    {
        /// <summary>Object for the camera children to look at (the aim target)</summary>
        [Tooltip("Object for the camera children to look at (the aim target).")]
        [NoSaveDuringPlay]
        [VcamTargetProperty]
        public Transform m_LookAt = null;

        /// <summary>Object for the camera children wants to move with (the body target)</summary>
        [Tooltip("Object for the camera children wants to move with (the body target).")]
        [NoSaveDuringPlay]
        [VcamTargetProperty]
        public Transform m_Follow = null;

        /// <summary>If enabled, this lens setting will apply to all three child rigs, 
        /// otherwise the child rig lens settings will be used</summary>
        [Tooltip("If enabled, this lens setting will apply to all three child rigs, "
            + "otherwise the child rig lens settings will be used")]
        [FormerlySerializedAs("m_UseCommonLensSetting")]
        public bool m_CommonLens = true;

        /// <summary>Specifies the lens properties of this Virtual Camera.
        /// This generally mirrors the Unity Camera's lens settings, and will be used to drive
        /// the Unity camera when the vcam is active</summary>
        [Tooltip("Specifies the lens properties of this Virtual Camera.  This generally "
            + "mirrors the Unity Camera's lens settings, and will be used to drive the "
            + "Unity camera when the vcam is active")]
        [FormerlySerializedAs("m_LensAttributes")]
        public LegacyLensSettings m_Lens = LegacyLensSettings.Default;

        /// <summary>Hint for transitioning to and from this CinemachineCamera.  Hints can be combined, although 
        /// not all combinations make sense.  In the case of conflicting hints, Cinemachine will 
        /// make an arbitrary choice.</summary>
        [Tooltip("Hint for transitioning to and from this CinemachineCamera.  Hints can be combined, although "
            + "not all combinations make sense.  In the case of conflicting hints, Cinemachine will "
            + "make an arbitrary choice.")]
        public CinemachineCore.BlendHints BlendHint;

        /// <summary>This event fires when a transition occurs.</summary>
        [Tooltip("This event fires when a transition occurs")]
        public CinemachineLegacyCameraEvents.OnCameraLiveEvent m_OnCameraLiveEvent = new();

        /// <summary>The Vertical axis.  Value is 0..1.  Chooses how to blend the child rigs</summary>
        [Header("Axis Control")]
        [Tooltip("The Vertical axis.  Value is 0..1.  Chooses how to blend the child rigs")]
        public AxisState m_YAxis = new AxisState(0, 1, false, true, 2f, 0.2f, 0.1f, "Mouse Y", false);

        /// <summary>Controls how automatic recentering of the Y axis is accomplished</summary>
        [Tooltip("Controls how automatic recentering of the Y axis is accomplished")]
        public AxisState.Recentering m_YAxisRecentering = new AxisState.Recentering(false, 1, 2);

        /// <summary>The Horizontal axis.  Value is -180...180.  This is passed on to 
        /// the rigs' OrbitalTransposer component</summary>
        [Tooltip("The Horizontal axis.  Value is -180...180.  "
            + "This is passed on to the rigs' OrbitalTransposer component")]
        public AxisState m_XAxis = new AxisState(-180, 180, true, false, 300f, 0.1f, 0.1f, "Mouse X", true);

        /// <summary>The definition of Forward.  Camera will follow behind</summary>
        [Tooltip("The definition of Forward.  Camera will follow behind.")]
        public CinemachineOrbitalTransposer.Heading m_Heading
            = new CinemachineOrbitalTransposer.Heading(
                CinemachineOrbitalTransposer.Heading.HeadingDefinition.TargetForward, 4, 0);

        /// <summary>Controls how automatic recentering of the X axis is accomplished</summary>
        [Tooltip("Controls how automatic recentering of the X axis is accomplished")]
        public AxisState.Recentering m_RecenterToTargetHeading = new AxisState.Recentering(false, 1, 2);

        /// <summary>The coordinate space to use when interpreting the offset from the target</summary>
        [Header("Orbits")]
        [Tooltip("The coordinate space to use when interpreting the offset from the target.  "
            + "This is also used to set the camera's Up vector, which will be maintained "
            + "when aiming the camera.")]
        public TargetTracking.BindingMode m_BindingMode
            = TargetTracking.BindingMode.LazyFollow;

        /// <summary>Controls how taut is the line that connects the rigs' orbits, which 
        /// determines final placement on the Y axis</summary>
        [Tooltip("Controls how taut is the line that connects the rigs' orbits, which "
            + "determines final placement on the Y axis")]
        [Range(0f, 1f)]
        [FormerlySerializedAs("m_SplineTension")]
        public float m_SplineCurvature = 0.2f;

        /// <summary>Defines the height and radius of the Rig orbit</summary>
        [Serializable]
        public struct Orbit
        {
            /// <summary>Height relative to target</summary>
            public float m_Height;
            /// <summary>Radius of orbit</summary>
            public float m_Radius;
            /// <summary>Constructor with specific values</summary>
            /// <param name="h">Orbit height</param>
            /// <param name="r">Orbit radius</param>
            public Orbit(float h, float r) { m_Height = h; m_Radius = r; }
        }

        /// <summary>The radius and height of the three orbiting rigs</summary>
        [Tooltip("The radius and height of the three orbiting rigs.")]
        public Orbit[] m_Orbits = new Orbit[3]
        {
            // These are the default orbits
            new Orbit(4.5f, 1.75f),
            new Orbit(2.5f, 3f),
            new Orbit(0.4f, 1.3f)
        };

        // Legacy support
        [SerializeField] [HideInInspector] [FormerlySerializedAs("m_HeadingBias")]
        private float m_LegacyHeadingBias = float.MaxValue;
        bool mUseLegacyRigDefinitions = false;

        [Serializable]
        struct LegacyTransitionParams
        {
            [FormerlySerializedAs("m_PositionBlending")]
            public int m_BlendHint;
            public bool m_InheritPosition;
            public CinemachineLegacyCameraEvents.OnCameraLiveEvent m_OnCameraLive;
        }
        [SerializeField, HideInInspector] LegacyTransitionParams m_LegacyTransitions;

        /// <inheritdoc/>
        internal protected override void PerformLegacyUpgrade(int streamedVersion)
        {
            base.PerformLegacyUpgrade(streamedVersion);
            if (streamedVersion < 20221011)
            {
                if (m_LegacyHeadingBias != float.MaxValue)
                {
                    m_Heading.m_Bias= m_LegacyHeadingBias;
                    m_LegacyHeadingBias = float.MaxValue;
                    int heading = (int)m_Heading.m_Definition;
                    if (m_RecenterToTargetHeading.LegacyUpgrade(ref heading, ref m_Heading.m_VelocityFilterStrength))
                        m_Heading.m_Definition = (CinemachineOrbitalTransposer.Heading.HeadingDefinition)heading;
                    mUseLegacyRigDefinitions = true;
                }
                if (m_LegacyTransitions.m_BlendHint != 0)
                {
                    if (m_LegacyTransitions.m_BlendHint == 3)
                        BlendHint = CinemachineCore.BlendHints.ScreenSpaceAimWhenTargetsDiffer;
                    else
                        BlendHint = (CinemachineCore.BlendHints)m_LegacyTransitions.m_BlendHint;
                    m_LegacyTransitions.m_BlendHint = 0;
                }
                if (m_LegacyTransitions.m_InheritPosition)
                {
                    BlendHint |= CinemachineCore.BlendHints.InheritPosition;
                    m_LegacyTransitions.m_InheritPosition = false;
                }
                if (m_LegacyTransitions.m_OnCameraLive != null)
                {
                    m_OnCameraLiveEvent = m_LegacyTransitions.m_OnCameraLive;
                    m_LegacyTransitions.m_OnCameraLive = null;
                }
            }
        }
        
        /// <summary>Helper for upgrading from CM2</summary>
        internal protected override bool IsDprecated => true;

        /// <summary>Enforce bounds for fields, when changed in inspector.</summary>
        void OnValidate()
        {
            m_YAxis.Validate();
            m_XAxis.Validate();
            m_RecenterToTargetHeading.Validate();
            m_YAxisRecentering.Validate();
            m_Lens.Validate();

            InvalidateRigCache();
            
#if UNITY_EDITOR
            for (int i = 0; m_Rigs != null && i < m_Rigs.Length; ++i)
                if (m_Rigs[i] != null)
                    CinemachineVirtualCamera.SetFlagsForHiddenChild(m_Rigs[i].gameObject);
#endif
        }

        /// <summary>Get a child rig</summary>
        /// <param name="i">Rig index.  Can be 0, 1, or 2</param>
        /// <returns>The rig, or null if index is bad.</returns>
        public CinemachineVirtualCamera GetRig(int i)
        {
            return (UpdateRigCache() && i >= 0 && i < 3) ? m_Rigs[i] : null;
        }

        internal bool RigsAreCreated { get => m_Rigs != null && m_Rigs.Length == 3; }

        /// <summary>Names of the 3 child rigs</summary>
        public static string[] RigNames { get { return new string[] { "TopRig", "MiddleRig", "BottomRig" }; } }

        bool mIsDestroyed = false;

        /// <summary>Updates the child rig cache</summary>
        protected override void OnEnable()
        {
            mIsDestroyed = false;
            base.OnEnable();
            InvalidateRigCache();
            UpdateInputAxisProvider();
        }

        /// <summary>
        /// API for the inspector.  Internal use only
        /// </summary>
        internal void UpdateInputAxisProvider()
        {
            m_XAxis.SetInputAxisProvider(0, null);
            m_YAxis.SetInputAxisProvider(1, null);
            var provider = GetComponent<AxisState.IInputAxisProvider>();
            if (provider != null)
            {
                m_XAxis.SetInputAxisProvider(0, provider);
                m_YAxis.SetInputAxisProvider(1, provider);
            }
        }

        /// <summary>Makes sure that the child rigs get destroyed in an undo-firndly manner.
        /// Invalidates the rig cache.</summary>
        protected override void OnDestroy()
        {
            // Make the rigs visible instead of destroying - this is to keep Undo happy
            if (m_Rigs != null)
                foreach (var rig in m_Rigs)
                    if (rig != null && rig.gameObject != null)
                        rig.gameObject.hideFlags
                            &= ~(HideFlags.HideInHierarchy | HideFlags.HideInInspector);

            mIsDestroyed = true;
            base.OnDestroy();
        }

        /// <summary>Invalidates the rig cache</summary>
        void OnTransformChildrenChanged()
        {
            InvalidateRigCache();
        }

        void Reset()
        {
            DestroyRigs();
            UpdateRigCache();
            Priority = new ();
            OutputChannel = OutputChannels.Default;
        }

        /// <summary>Set this to force the next update to ignore deltaTime and reset itself</summary>
        public override bool PreviousStateIsValid
        {
            get { return base.PreviousStateIsValid; }
            set
            {
                if (value == false)
                    for (int i = 0; m_Rigs != null && i < m_Rigs.Length; ++i)
                        if (m_Rigs[i] != null)
                            m_Rigs[i].PreviousStateIsValid = value;
                base.PreviousStateIsValid = value;
            }
        }

        /// <summary>The cacmera state, which will be a blend of the child rig states</summary>
        override public CameraState State { get { return m_State; } }

        /// <summary>Get the current LookAt target.  Returns parent's LookAt if parent
        /// is non-null and no specific LookAt defined for this camera</summary>
        override public Transform LookAt
        {
            get { return ResolveLookAt(m_LookAt); }
            set { m_LookAt = value; }
        }

        /// <summary>Get the current Follow target.  Returns parent's Follow if parent
        /// is non-null and no specific Follow defined for this camera</summary>
        override public Transform Follow
        {
            get { return ResolveFollow(m_Follow); }
            set { m_Follow = value; }
        }

        /// <summary>Check whether the vcam a live child of this camera.
        /// Returns true if the child is currently contributing actively to the camera state.</summary>
        /// <param name="vcam">The Virtual Camera to check</param>
        /// <param name="dominantChildOnly">If truw, will only return true if this vcam is the dominant live child</param>
        /// <returns>True if the vcam is currently actively influencing the state of this vcam</returns>
        public bool IsLiveChild(ICinemachineCamera vcam, bool dominantChildOnly = false)
        {
            // Do not update the rig cache here or there will be infinite loop at creation time
            if (!RigsAreCreated)
                return false;
            var y = GetYAxisValue();
            if (dominantChildOnly)
            {
                if (vcam == (ICinemachineCamera)m_Rigs[0])
                    return y > 0.666f;
                if (vcam == (ICinemachineCamera)m_Rigs[2])
                    return y < 0.333;
                if (vcam == (ICinemachineCamera)m_Rigs[1])
                    return y >= 0.333f && y <= 0.666f;
                return false;
            }
            if (vcam == (ICinemachineCamera)m_Rigs[1])
                return true;
            if (y < 0.5f)
                return vcam == (ICinemachineCamera)m_Rigs[2];
            return vcam == (ICinemachineCamera)m_Rigs[0];
        }

        /// <summary>This is called to notify the vcam that a target got warped,
        /// so that the vcam can update its internal state to make the camera
        /// also warp seamlessly.</summary>
        /// <param name="target">The object that was warped</param>
        /// <param name="positionDelta">The amount the target's position changed</param>
        public override void OnTargetObjectWarped(Transform target, Vector3 positionDelta)
        {
            UpdateRigCache();
            if (RigsAreCreated)
                foreach (var vcam in m_Rigs)
                    vcam.OnTargetObjectWarped(target, positionDelta);
            base.OnTargetObjectWarped(target, positionDelta);
        }

        /// <summary>
        /// Force the virtual camera to assume a given position and orientation.  
        /// Procedural placement then takes over
        /// </summary>
        /// <param name="pos">Worldspace position to take</param>
        /// <param name="rot">Worldspace orientation to take</param>
        public override void ForceCameraPosition(Vector3 pos, Quaternion rot)
        {
            var up = State.ReferenceUp;
            m_YAxis.Value = GetYAxisClosestValue(pos, up);

            PreviousStateIsValid = true;
            transform.position = pos;
            transform.rotation = rot;
            m_State.RawPosition = pos;
            m_State.RawOrientation = rot;

            if (UpdateRigCache())
            {
                if (m_BindingMode != TargetTracking.BindingMode.LazyFollow)
                    m_XAxis.Value = mOrbitals[1].GetAxisClosestValue(pos, up);

                PushSettingsToRigs();
                for (int i = 0; i < 3; ++i)
                    m_Rigs[i].ForceCameraPosition(pos, rot);

                InternalUpdateCameraState(up, -1);
            }
            base.ForceCameraPosition(pos, rot);
        }
        
        /// <summary>Internal use only.  Called by CinemachineCore at designated update time
        /// so the vcam can position itself and track its targets.  All 3 child rigs are updated,
        /// and a blend calculated, depending on the value of the Y axis.</summary>
        /// <param name="worldUp">Default world Up, set by the CinemachineBrain</param>
        /// <param name="deltaTime">Delta time for time-based effects (ignore if less than 0)</param>
        override public void InternalUpdateCameraState(Vector3 worldUp, float deltaTime)
        {
            UpdateTargetCache();
            UpdateRigCache();
            if (!RigsAreCreated)
                return;

            if (deltaTime < 0)
                PreviousStateIsValid = false;

            // Update the current state by invoking the component pipeline
            m_State = CalculateNewState(worldUp, deltaTime);
            m_State.BlendHint = (CameraState.BlendHints)BlendHint;

            // Push the raw position back to the game object's transform, so it
            // moves along with the camera.  Leave the orientation alone, because it
            // screws up camera dragging when there is a LookAt behaviour.
            if (Follow != null)
            {
                Vector3 delta = State.RawPosition - transform.position;
                transform.position = State.RawPosition;
                m_Rigs[0].transform.position -= delta;
                m_Rigs[1].transform.position -= delta;
                m_Rigs[2].transform.position -= delta;
            }

            InvokePostPipelineStageCallback(this, CinemachineCore.Stage.Finalize, ref m_State, deltaTime);
            PreviousStateIsValid = true;

            // Set up for next frame
            bool activeCam = PreviousStateIsValid && CinemachineCore.IsLive(this);
            if (activeCam && deltaTime >= 0)
            {
                if (m_YAxis.Update(deltaTime))
                    m_YAxisRecentering.CancelRecentering();
            }
            PushSettingsToRigs();
            if (m_BindingMode == TargetTracking.BindingMode.LazyFollow)
                m_XAxis.Value = 0;
        }

        /// <summary>If we are transitioning from another FreeLook, grab the axis values from it.</summary>
        /// <param name="fromCam">The camera being deactivated.  May be null.</param>
        /// <param name="worldUp">Default world Up, set by the CinemachineBrain</param>
        /// <param name="deltaTime">Delta time for time-based effects (ignore if less than or equal to 0)</param>
        public override void OnTransitionFromCamera(
            ICinemachineCamera fromCam, Vector3 worldUp, float deltaTime)
        {
            base.OnTransitionFromCamera(fromCam, worldUp, deltaTime);
            if (!RigsAreCreated)
                return;
            InvokeOnTransitionInExtensions(fromCam, worldUp, deltaTime);
            bool forceUpdate = false;
//            m_RecenterToTargetHeading.DoRecentering(ref m_XAxis, -1, 0);
//              m_YAxis.m_Recentering.DoRecentering(ref m_YAxis, -1, 0.5f);
//            m_RecenterToTargetHeading.CancelRecentering();
//            m_YAxis.m_Recentering.CancelRecentering();
            if (fromCam != null 
                && (State.BlendHint & CameraState.BlendHints.InheritPosition) != 0 
                && !CinemachineCore.IsLiveInBlend(this))
            {
                var cameraPos = fromCam.State.RawPosition;

                // Special handling for FreeLook: get an undamped outgoing position
                if (fromCam is CinemachineFreeLook)
                {
                    var flFrom = (fromCam as CinemachineFreeLook);
                    var orbital = flFrom.mOrbitals != null ? flFrom.mOrbitals[1] : null;
                    if (orbital != null)
                        cameraPos = orbital.GetTargetCameraPosition(worldUp);
                }
                ForceCameraPosition(cameraPos, fromCam.State.GetFinalOrientation());
            }
            if (forceUpdate)
            {
                for (int i = 0; i < 3; ++i)
                    m_Rigs[i].InternalUpdateCameraState(worldUp, deltaTime);
                InternalUpdateCameraState(worldUp, deltaTime);
            }
            else
                UpdateCameraState(worldUp, deltaTime);
            m_OnCameraLiveEvent?.Invoke(this, fromCam);
        }
        
        /// <summary>Returns true if this object requires user input from a IInputAxisProvider.</summary>
        /// <returns>Returns true when input is required.</returns>
        bool AxisState.IRequiresInput.RequiresInput() => true;

        float GetYAxisClosestValue(Vector3 cameraPos, Vector3 up)
        {
            if (Follow != null)
            {
                // Rotate the camera pos to the back
                Quaternion q = Quaternion.FromToRotation(up, Vector3.up);
                Vector3 dir = q * (cameraPos - Follow.position);
                Vector3 flatDir = dir; flatDir.y = 0;
                if (!flatDir.AlmostZero())
                {
                    float angle = UnityVectorExtensions.SignedAngle(flatDir, Vector3.back, Vector3.up);
                    dir = Quaternion.AngleAxis(angle, Vector3.up) * dir;
                }
                dir.x = 0;

                // We need to find the minimum of the angle of function using steepest descent
                return SteepestDescent(dir.normalized * (cameraPos - Follow.position).magnitude);
            }
            return m_YAxis.Value; // stay conservative
        }

        float SteepestDescent(Vector3 cameraOffset)
        {
            const int maxIteration = 5;
            const float epsilon = 0.005f;
            var x = InitialGuess();
            for (var i = 0; i < maxIteration; ++i)
            {
                var angle = AngleFunction(x);
                var slope = SlopeOfAngleFunction(x);
                if (Mathf.Abs(slope) < epsilon || Mathf.Abs(angle) < epsilon)
                    break; // found best
                x = Mathf.Clamp01(x - (angle / slope)); // clamping is needed so we don't overshoot
            }
            return x;

            // localFunctions
            float AngleFunction(float input)
            {
                var point = GetLocalPositionForCameraFromInput(input);
                return Mathf.Abs(UnityVectorExtensions.SignedAngle(cameraOffset, point, Vector3.right));
            }

            // approximating derivative using symmetric difference quotient (finite diff)
            float SlopeOfAngleFunction(float input)
            {
                var angleBehind = AngleFunction(input - epsilon);
                var angleAfter = AngleFunction(input + epsilon);
                return (angleAfter - angleBehind) / (2f * epsilon);
            }

            float InitialGuess()
            {
                UpdateCachedSpline();

                const float step = 1.0f / 10;
                float best = 0.5f;
                float bestAngle = AngleFunction(best);
                for (int j = 0; j <= 5; ++j)
                {
                    var t = j * step;
                    ChooseBestAngle(0.5f + t);
                    ChooseBestAngle(0.5f - t);
                    void ChooseBestAngle(float x)
                    {
                        var a = AngleFunction(x);
                        if (a < bestAngle)
                        {
                            bestAngle = a;
                            best = x;
                        }
                    }
                }
                return best;
            }
        }

        CameraState m_State = CameraState.Default;          // Current state this frame

        // Serialized only to implement copy/paste of rigs.
        // Note however that this strategy has its limitations: the rigs
        // won't be pasted onto a prefab asset outside the scene unless the prefab
        // is opened in Prefab edit mode.
        [SerializeField][HideInInspector][NoSaveDuringPlay]
        CinemachineVirtualCamera[] m_Rigs = new CinemachineVirtualCamera[3];

        void InvalidateRigCache() { mOrbitals = null; }
        CinemachineOrbitalTransposer[] mOrbitals = null;
        CinemachineBlend mBlendA;
        CinemachineBlend mBlendB;

        /// <summary>
        /// Override component pipeline creation.
        /// This needs to be done by the editor to support Undo.
        /// The override must do exactly the same thing as the CreatePipeline method in this class.
        /// </summary>
        public static CreateRigDelegate CreateRigOverride;

        /// <summary>
        /// Override component pipeline creation.
        /// This needs to be done by the editor to support Undo.
        /// The override must do exactly the same thing as the CreatePipeline method in this class.
        /// </summary>
        /// <param name="copyFrom">The rig template.</param>
        /// <param name="name">The rig name.</param>
        /// <param name="vcam">The FreeLook owner of the rig.</param>
        /// <returns>The created rig instance.</returns>
        public delegate CinemachineVirtualCamera CreateRigDelegate(
            CinemachineFreeLook vcam, string name, CinemachineVirtualCamera copyFrom);

        /// <summary>
        /// Override component pipeline destruction.
        /// This needs to be done by the editor to support Undo.
        /// </summary>
        public static DestroyRigDelegate DestroyRigOverride;

        /// <summary>
        /// Override component pipeline destruction.
        /// This needs to be done by the editor to support Undo.
        /// </summary>
        /// <param name="rig">The rig to destroy.</param>
        public delegate void DestroyRigDelegate(GameObject rig);

        private void DestroyRigs()
        {
            // First collect rigs because we will destroy as we go
            var rigs = new List<CinemachineVirtualCamera>(3);
            for (int i = 0; i < RigNames.Length; ++i)
                foreach (Transform child in transform)
                    if (child.gameObject.name == RigNames[i])
                        rigs.Add(child.GetComponent<CinemachineVirtualCamera>());

            foreach (var rig in rigs)
            {
                if (rig != null)
                {
                    if (DestroyRigOverride != null)
                        DestroyRigOverride(rig.gameObject);
                    else
                    {
                        rig.DestroyPipeline();
                        Destroy(rig);
                        if (!RuntimeUtility.IsPrefab(gameObject))
                            Destroy(rig.gameObject);
                    }
                }
            }
            mOrbitals = null;
            m_Rigs = null;
        }

        private CinemachineVirtualCamera[] CreateRigs(CinemachineVirtualCamera[] copyFrom)
        {
            float[] softCenterDefaultsV = new float[] { 0.5f, 0.55f, 0.6f };

            // Invalidate the cache
            mOrbitals = null;
            m_Rigs = null;

            // Create the rig contents
            CinemachineVirtualCamera[] newRigs = new CinemachineVirtualCamera[3];
            for (int i = 0; i < newRigs.Length; ++i)
            {
                var src = (copyFrom != null && copyFrom.Length > i) ? copyFrom[i] : null;
                if (CreateRigOverride != null)
                    newRigs[i] = CreateRigOverride(this, RigNames[i], src);
                else
                {
                    // Recycle the game object if it exists
                    GameObject go = null;
                    foreach (Transform child in transform)
                    {
                        if (child.gameObject.name == RigNames[i])
                        {
                            go = child.gameObject;
                            break;
                        }
                    }

                    // Create a new rig with default components
                    // Note: copyFrom only supported in Editor, not build
                    if (go == null)
                    {
                        if (!RuntimeUtility.IsPrefab(gameObject))
                        {
                            go = new GameObject(RigNames[i]);
                            go.transform.parent = transform;
                        }
                    }
                    if (go == null)
                        newRigs[i] = null;
                    else
                    {
                        newRigs[i] = go.AddComponent<CinemachineVirtualCamera>();
                        newRigs[i].AddCinemachineComponent<CinemachineOrbitalTransposer>();
                        newRigs[i].AddCinemachineComponent<CinemachineComposer>();
                    }
                }

                // Set up the defaults
                if (newRigs[i] != null)
                {
                    newRigs[i].InvalidateComponentPipeline();
                    CinemachineOrbitalTransposer orbital = newRigs[i].GetCinemachineComponent<CinemachineOrbitalTransposer>();
                    if (orbital == null)
                        orbital = newRigs[i].AddCinemachineComponent<CinemachineOrbitalTransposer>(); // should not happen
                    if (src == null)
                    {
                        // Only set defaults if not copying
                        orbital.m_YawDamping = 0;
                        var composer = newRigs[i].GetCinemachineComponent<CinemachineComposer>();
                        if (composer != null)
                        {
                            composer.m_HorizontalDamping = composer.m_VerticalDamping = 0;
                            composer.m_ScreenX = 0.5f; composer.m_ScreenY = softCenterDefaultsV[i];
                            composer.m_DeadZoneWidth = composer.m_DeadZoneHeight = 0;
                            composer.m_SoftZoneWidth = composer.m_SoftZoneHeight = 0.6f;
                            composer.m_BiasX = composer.m_BiasY = 0;
                        }
                    }
                }
            }
            return newRigs;
        }

        private bool UpdateRigCache()
        {
            if (mIsDestroyed)
                return false;

#if UNITY_EDITOR
            // Special condition: Did we just get copy/pasted?
            if (m_Rigs != null && m_Rigs.Length == 3
                && m_Rigs[0] != null && m_Rigs[0].transform.parent != transform)
            {
                var copyFrom = m_Rigs;
                DestroyRigs();
                CreateRigs(copyFrom);
            }
#endif

            // Early out if we're up to date
            if (mOrbitals != null && mOrbitals.Length == 3)
                return true;

            // Locate existing rigs, and recreate them if any are missing
            m_CachedXAxisHeading = 0;
            m_Rigs = null;
            mOrbitals = null;
            var rigs = LocateExistingRigs(false);
            if (rigs == null || rigs.Count != 3)
            {
                DestroyRigs();
                CreateRigs(null);
                rigs = LocateExistingRigs(true);
            }
            if (rigs != null && rigs.Count == 3)
                m_Rigs = rigs.ToArray();

            if (RigsAreCreated)
            {
                mOrbitals = new CinemachineOrbitalTransposer[m_Rigs.Length];
                for (int i = 0; i < m_Rigs.Length; ++i)
                     mOrbitals[i] = m_Rigs[i].GetCinemachineComponent<CinemachineOrbitalTransposer>();

                foreach (var rig in m_Rigs)
                {
                    // Configure the UI
                    if (rig == null)
                        continue;
                    rig.m_ExcludedPropertiesInInspector = m_CommonLens
                        ? new string[] { "m_Script", "Header", "Extensions", "Priority", "OutputChannel", "m_Transitions", "m_Follow", "m_StandbyUpdate", "m_Lens" }
                        : new string[] { "m_Script", "Header", "Extensions", "Priority", "OutputChannel", "m_Transitions", "m_Follow", "m_StandbyUpdate" };
                    rig.m_LockStageInInspector = new CinemachineCore.Stage[] { CinemachineCore.Stage.Body };
                }

                // Create the blend objects
                mBlendA = new CinemachineBlend { CamA = m_Rigs[1], CamB = m_Rigs[0], BlendCurve = AnimationCurve.Linear(0, 0, 1, 1), Duration = 1 };
                mBlendB = new CinemachineBlend { CamA = m_Rigs[2], CamB = m_Rigs[1], BlendCurve = AnimationCurve.Linear(0, 0, 1, 1), Duration = 1 };

                return true;
            }
            return false;
        }

        private List<CinemachineVirtualCamera> LocateExistingRigs(bool forceOrbital)
        {
            m_CachedXAxisHeading = m_XAxis.Value;
            m_LastHeadingUpdateFrame = -1;
            var rigs = new List<CinemachineVirtualCamera>(3);
            foreach (Transform child in transform)
            {
                CinemachineVirtualCamera vcam = child.GetComponent<CinemachineVirtualCamera>();
                if (vcam != null)
                {
                    GameObject go = child.gameObject;
                    for (int i = 0; i < RigNames.Length; ++i)
                    {
                        if (go.name != RigNames[i])
                            continue;

                        // Must have an orbital transposer or it's no good
                        var orbital = vcam.GetCinemachineComponent<CinemachineOrbitalTransposer>();
                        if (orbital == null && forceOrbital)
                            orbital = vcam.AddCinemachineComponent<CinemachineOrbitalTransposer>();
                        if (orbital != null)
                        {
                            orbital.m_HeadingIsDriven = true;
                            orbital.HideOffsetInInspector = true;
                            orbital.m_XAxis.m_InputAxisName = string.Empty;
                            orbital.HeadingUpdater = UpdateXAxisHeading;
                            orbital.m_RecenterToTargetHeading.m_enabled = false;

                            vcam.StandbyUpdate = StandbyUpdate;
                            rigs.Add(vcam);
                        }
                    }
                }
            }
            return rigs;
        }

        float m_CachedXAxisHeading;
        float m_LastHeadingUpdateFrame;

        float UpdateXAxisHeading(CinemachineOrbitalTransposer orbital, float deltaTime, Vector3 up)
        {
            if (this == null)   
                return 0; // deleted

            // Update the axis only once per frame
            if (m_LastHeadingUpdateFrame != Time.frameCount)
            {
                m_LastHeadingUpdateFrame = Time.frameCount;
                var oldValue = m_XAxis.Value;
                m_CachedXAxisHeading = orbital.UpdateHeading(
                    PreviousStateIsValid ? deltaTime : -1, up,
                    ref m_XAxis, ref m_RecenterToTargetHeading,
                    CinemachineCore.IsLive(this));
                // Allow externally-driven values to work in this mode
                if (m_BindingMode == TargetTracking.BindingMode.LazyFollow)
                    m_XAxis.Value = oldValue;
            }
            return m_CachedXAxisHeading;
        }

        void PushSettingsToRigs()
        {
            for (int i = 0; i < m_Rigs.Length; ++i)
            {
                if (m_CommonLens)
                    m_Rigs[i].m_Lens = m_Lens;

                // If we just deserialized from a legacy version,
                // pull the orbits and targets from the rigs
                if (mUseLegacyRigDefinitions)
                {
                    mUseLegacyRigDefinitions = false;
                    m_Orbits[i].m_Height = mOrbitals[i].m_FollowOffset.y;
                    m_Orbits[i].m_Radius = -mOrbitals[i].m_FollowOffset.z;
                    if (m_Rigs[i].Follow != null)
                        Follow = m_Rigs[i].Follow;
                }
                m_Rigs[i].Follow = null;
                m_Rigs[i].StandbyUpdate = StandbyUpdate;
                m_Rigs[i].FollowTargetAttachment = FollowTargetAttachment;
                m_Rigs[i].LookAtTargetAttachment = LookAtTargetAttachment;
                if (!PreviousStateIsValid)
                {
                    m_Rigs[i].PreviousStateIsValid = false;
                    m_Rigs[i].transform.position = transform.position;
                    m_Rigs[i].transform.rotation = transform.rotation;
                }
#if UNITY_EDITOR
                // Hide the rigs from prying eyes
                CinemachineVirtualCamera.SetFlagsForHiddenChild(m_Rigs[i].gameObject);
#endif
                mOrbitals[i].m_FollowOffset = GetLocalPositionForCameraFromInput(GetYAxisValue());
                mOrbitals[i].m_BindingMode = m_BindingMode;
                mOrbitals[i].m_Heading = m_Heading;
                mOrbitals[i].m_XAxis.Value = m_XAxis.Value;

                // Hack to get LazyFollow with heterogeneous dampings to work
                if (m_BindingMode == TargetTracking.BindingMode.LazyFollow)
                    m_Rigs[i].SetStateRawPosition(State.RawPosition);
            }
        }

        private float GetYAxisValue()
        {
            float range = m_YAxis.m_MaxValue - m_YAxis.m_MinValue;
            return (range > UnityVectorExtensions.Epsilon) ? m_YAxis.Value / range : 0.5f;
        }

        private LensSettings m_LensSettings;

        private CameraState CalculateNewState(Vector3 worldUp, float deltaTime)
        {
            m_LensSettings = m_Lens.ToLensSettings();
            CameraState state = PullStateFromVirtualCamera(worldUp, ref m_LensSettings);
            m_YAxisRecentering.DoRecentering(ref m_YAxis, deltaTime, 0.5f);

            // Blend from the appropriate rigs
            float t = GetYAxisValue();
            if (t > 0.5f)
            {
                if (mBlendA != null)
                {
                    mBlendA.TimeInBlend = (t - 0.5f) * 2f;
                    mBlendA.UpdateCameraState(worldUp, deltaTime);
                    state = mBlendA.State;
                }
            }
            else
            {
                if (mBlendB != null)
                {
                    mBlendB.TimeInBlend = t * 2f;
                    mBlendB.UpdateCameraState(worldUp, deltaTime);
                    state = mBlendB.State;
                }
            }
            return state;
        }

        /// <summary>
        /// Returns the local position of the camera along the spline used to connect the
        /// three camera rigs. Does not take into account the current heading of the
        /// camera (or its target)
        /// </summary>
        /// <param name="t">The t-value for the camera on its spline. Internally clamped to
        /// the value [0,1]</param>
        /// <returns>The local offset (back + up) of the camera WRT its target based on the
        /// supplied t-value</returns>
        public Vector3 GetLocalPositionForCameraFromInput(float t)
        {
            if (mOrbitals == null)
                return Vector3.zero;
            UpdateCachedSpline();
            int n = 1;
            if (t > 0.5f)
            {
                t -= 0.5f;
                n = 2;
            }
            return SplineHelpers.Bezier3(
                t * 2f, m_CachedKnots[n], m_CachedCtrl1[n], m_CachedCtrl2[n], m_CachedKnots[n+1]);
        }

        Orbit[] m_CachedOrbits;
        float m_CachedTension;
        Vector4[] m_CachedKnots;
        Vector4[] m_CachedCtrl1;
        Vector4[] m_CachedCtrl2;
        void UpdateCachedSpline()
        {
            bool cacheIsValid = (m_CachedOrbits != null && m_CachedOrbits.Length == 3 
                && m_CachedTension == m_SplineCurvature);
            for (int i = 0; i < 3 && cacheIsValid; ++i)
                cacheIsValid = (m_CachedOrbits[i].m_Height == m_Orbits[i].m_Height
                    && m_CachedOrbits[i].m_Radius == m_Orbits[i].m_Radius);
            if (!cacheIsValid)
            {
                float t = m_SplineCurvature;
                m_CachedKnots = new Vector4[5];
                m_CachedCtrl1 = new Vector4[5];
                m_CachedCtrl2 = new Vector4[5];
                m_CachedKnots[1] = new Vector4(0, m_Orbits[2].m_Height, -m_Orbits[2].m_Radius, 0);
                m_CachedKnots[2] = new Vector4(0, m_Orbits[1].m_Height, -m_Orbits[1].m_Radius, 0);
                m_CachedKnots[3] = new Vector4(0, m_Orbits[0].m_Height, -m_Orbits[0].m_Radius, 0);
                m_CachedKnots[0] = Vector4.Lerp(m_CachedKnots[1], Vector4.zero, t);
                m_CachedKnots[4] = Vector4.Lerp(m_CachedKnots[3], Vector4.zero, t);
                SplineHelpers.ComputeSmoothControlPoints(
                    ref m_CachedKnots, ref m_CachedCtrl1, ref m_CachedCtrl2);
                m_CachedOrbits = new Orbit[3];
                for (int i = 0; i < 3; ++i)
                    m_CachedOrbits[i] = m_Orbits[i];
                m_CachedTension = m_SplineCurvature;
            }
        }
    }
}
#endif
