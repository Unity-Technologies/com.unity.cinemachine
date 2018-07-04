using UnityEngine;
using Cinemachine.Utility;
using System;

namespace Cinemachine
{
    /// <summary>
    /// A Cinemachine Camera geared towards a 3rd person camera experience.
    /// The camera orbits around its subject with three separate camera rigs defining
    /// rings around the target. Each rig has its own radius, height offset, composer,
    /// and lens settings.
    /// Depending on the camera's position along the spline connecting these three rigs,
    /// these settings are interpolated to give the final camera position and state.
    /// </summary>
    [DocumentationSorting(DocumentationSortingAttribute.Level.UserRef)]
    [ExecuteInEditMode, DisallowMultipleComponent]
    [AddComponentMenu("Cinemachine/CinemachineNewFreeLook")]
    public class CinemachineNewFreeLook : CinemachineVirtualCameraBase
    {
        /// <summary>Object for the camera children to look at (the aim target)</summary>
        [Tooltip("Object for the camera children to look at (the aim target).")]
        [NoSaveDuringPlay]
        public Transform m_LookAt = null;

        /// <summary>Object for the camera children wants to move with (the body target)</summary>
        [Tooltip("Object for the camera children wants to move with (the body target).")]
        [NoSaveDuringPlay]
        public Transform m_Follow = null;

        /// <summary>Specifies the LensSettings of this Virtual Camera.
        /// These settings will be transferred to the Unity camera when the vcam is live.</summary>
        [Tooltip("Specifies the lens properties of this Virtual Camera.  This generally mirrors the Unity Camera's lens settings, and will be used to drive the Unity camera when the vcam is active.")]
        [LensSettingsProperty]
        public LensSettings m_Lens = LensSettings.Default;

        /// <summary> Collection of parameters that influence how this virtual camera transitions from 
        /// other virtual cameras </summary>
        public TransitionParams m_Transitions;

        /// <summary>The Vertical axis.  Value is 0..1.  Chooses how to blend the child rigs</summary>
        [Header("Axis Control")]
        [Tooltip("The Vertical axis.  Value is 0..1.  0.5 is the middle rig.  Chooses how to blend the child rigs")]
        [AxisStateProperty]
        public AxisState m_VerticalAxis = new AxisState(0, 1, false, true, 2f, 0.2f, 0.1f, "Mouse Y", false);

        /// <summary>Controls how automatic recentering of the Vertical axis is accomplished</summary>
        [Tooltip("Controls how automatic recentering of the Vertical axis is accomplished")]
        public AxisState.Recentering m_VerticalRecentering = new AxisState.Recentering(false, 1, 2);

#if false
        [Tooltip("The Radial axis.  Value is the base radius of the orbits")]
        [AxisStateProperty]
        public AxisState m_RadialAxis = new AxisState(1, 10, false, true, 2f, 0.2f, 0.1f, "Mouse ScrollWheel", false);

        /// <summary>Controls how automatic recentering of the Radial axis is accomplished</summary>
        [Tooltip("Controls how automatic recentering of the Radial axis is accomplished")]
        public AxisState.Recentering m_RadialRecentering = new AxisState.Recentering(false, 1, 2);
#endif
        /// <summary>Defines the height and radius for an orbit</summary>
        [Serializable]
        public struct Orbit 
        { 
            /// <summary>Height relative to target</summary>
            public float m_Height; 

            /// <summary>Radius of orbit</summary>
            public float m_Radius; 
        }

        /// <summary>Order is Top, Middle, Bottom</summary>
        [HideInInspector]
        public Orbit[] m_Orbits = new Orbit[3];

        /// <summary></summary>
        [Tooltip("Controls how taut is the line that connects the rigs' orbits, which determines final placement on the Y axis")]
        [Range(0f, 1f)]
        [HideInInspector]
        public float m_SplineCurvature;

        /// <summary>Identifiers for accessing override settings for top and bottom rigs</summary>
        public enum RigID { Top, Bottom };
        
        /// <summary>Override settings for top and bottom rigs</summary>
        [Serializable]
        public class Rig
        {
            [SerializeField]
            public bool m_CustomLens;

            [LensSettingsProperty]
            public LensSettings m_Lens;

            [SerializeField]
            public bool m_CustomBody;
            public CinemachineTransposer.BlendableSettings m_Body;

            [SerializeField]
            public bool m_CustomAim;
            public CinemachineComposer.BlendableSettings m_Aim;

            [SerializeField]
            public bool m_CustomNoise;
            public CinemachineBasicMultiChannelPerlin.BlendableSettings m_Noise;

            internal void Validate()
            {
                if (m_Lens.FieldOfView == 0)
                    m_Lens = LensSettings.Default;
                m_Lens.Validate();
            }
        }

        [SerializeField, HideInInspector]
        internal Rig[] m_Rigs = new Rig[2] { new Rig(), new Rig() };

        /// <summary>Accessor for rig override settings</summary>
        public Rig GetRigSettings(RigID rig) { return m_Rigs[(int)rig]; }

        /// <summary>Enforce bounds for fields, when changed in inspector.</summary>
        protected override void OnValidate()
        {
            base.OnValidate();
            for (int i = 0; i < m_Rigs.Length; ++i)
                m_Rigs[i].Validate();
        }

        /// <summary>Updates the child rig cache</summary>
        protected override void OnEnable()
        {
            base.OnEnable();
            InvalidateComponentCache();
        }

        void Reset()
        {
            SetDefault();
        }

        /// <summary>The cacmera state, which will be a blend of the child rig states</summary>
        override public CameraState State { get { return m_State; } }
        CameraState m_State = CameraState.Default; // Current state this frame

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

        /// <summary>This is called to notify the vcam that a target got warped,
        /// so that the vcam can update its internal state to make the camera 
        /// also warp seamlessy.</summary>
        /// <param name="target">The object that was warped</param>
        /// <param name="positionDelta">The amount the target's position changed</param>
        public override void OnTargetObjectWarped(Transform target, Vector3 positionDelta)
        {
            UpdateComponentCache();
            for (int i = 0; i < m_Components.Length; ++i)
            {
                if (m_Components[i] != null)
                    m_Components[i].OnTargetObjectWarped(target, positionDelta);
            }
            base.OnTargetObjectWarped(target, positionDelta);
        }

        /// <summary>Internal use only.  Called by CinemachineCore at designated update time
        /// so the vcam can position itself and track its targets.  All 3 child rigs are updated,
        /// and a blend calculated, depending on the value of the Y axis.</summary>
        /// <param name="worldUp">Default world Up, set by the CinemachineBrain</param>
        /// <param name="deltaTime">Delta time for time-based effects (ignore if less than 0)</param>
        override public void InternalUpdateCameraState(Vector3 worldUp, float deltaTime)
        {
            if (!PreviousStateIsValid)
                deltaTime = -1;

            UpdateComponentCache();

            // Update the current state by invoking the component pipeline
            m_State = CalculateNewState(worldUp, deltaTime);
            ApplyPositionBlendMethod(ref m_State, m_Transitions.m_BlendHint);

            // Push the raw position back to the game object's transform, so it
            // moves along with the camera.  Leave the orientation alone, because it
            // screws up camera dragging when there is a LookAt behaviour.
            if (Follow != null)
                transform.position = State.RawPosition;

            InvokePostPipelineStageCallback(this, CinemachineCore.Stage.Finalize, ref m_State, deltaTime);
            PreviousStateIsValid = true;
        }

        /// <summary>If we are transitioning from another FreeLook, grab the axis values from it.</summary>
        /// <param name="fromCam">The camera being deactivated.  May be null.</param>
        /// <param name="worldUp">Default world Up, set by the CinemachineBrain</param>
        /// <param name="deltaTime">Delta time for time-based effects (ignore if less than or equal to 0)</param>
        public override void OnTransitionFromCamera(
            ICinemachineCamera fromCam, Vector3 worldUp, float deltaTime) 
        {
            base.OnTransitionFromCamera(fromCam, worldUp, deltaTime);
            bool forceUpdate = false;
            if (fromCam != null)
            {
                CinemachineNewFreeLook freeLookFrom = fromCam as CinemachineNewFreeLook;
                if (freeLookFrom != null && freeLookFrom.Follow == Follow)
                {
                    if (Orbital.m_BindingMode != CinemachineTransposer.BindingMode.SimpleFollowWithWorldUp)
                        Orbital.m_XAxis.Value = freeLookFrom.Orbital.m_XAxis.Value;
                    m_VerticalAxis.Value = freeLookFrom.m_VerticalAxis.Value;
                    //m_RadialAxis.Value = freeLookFrom.m_RadialAxis.Value;
                    forceUpdate = true;
                }
            }
            if (m_Transitions.m_InheritPosition)
            {
                var brain = CinemachineCore.Instance.FindPotentialTargetBrain(this);
                if (brain != null)
                {
                    transform.position = brain.transform.position;
                    transform.rotation = brain.transform.rotation;
                    m_State = PullStateFromVirtualCamera(worldUp, ref m_Lens);
                    m_Rigs[(int)RigID.Top].m_Lens.SnapshotCameraReadOnlyProperties(ref m_Lens);
                    m_Rigs[(int)RigID.Bottom].m_Lens.SnapshotCameraReadOnlyProperties(ref m_Lens);
                    PreviousStateIsValid = false;
                    forceUpdate = true;
                    InternalUpdateCameraState(worldUp, deltaTime);
                }
            }
            if (forceUpdate)
                InternalUpdateCameraState(worldUp, deltaTime);
            else
                UpdateCameraState(worldUp, deltaTime);
            if (m_Transitions.m_OnCameraLive != null)
                m_Transitions.m_OnCameraLive.Invoke(this, fromCam);
        }

        // Component Cache - serialized only for copy/paste
        [SerializeField, HideInInspector, NoSaveDuringPlay]
        CinemachineComponentBase[] m_Components;

        /// For inspector
        internal CinemachineComponentBase[] ComponentCache { get { UpdateComponentCache(); return m_Components; } }

        /// Must always have an Orbital Transposer
        CinemachineOrbitalTransposer Orbital 
        { 
            get 
            {
                UpdateComponentCache();
                return m_Components[(int)CinemachineCore.Stage.Body] as CinemachineOrbitalTransposer;
            }
        }

        public void InvalidateComponentCache()
        {
            m_Components = null;
        }

        void UpdateComponentCache()
        {
            if (m_Components != null)
                return;

            m_Components = new CinemachineComponentBase[(int)CinemachineCore.Stage.Finalize];
            m_Components[(int)CinemachineCore.Stage.Body] = GetComponent<CinemachineOrbitalTransposer>();
            m_Components[(int)CinemachineCore.Stage.Aim] = GetComponent<CinemachineComposer>();
            m_Components[(int)CinemachineCore.Stage.Noise] = GetComponent<CinemachineBasicMultiChannelPerlin>();

            if (Orbital != null)
                Orbital.HideOffsetInInspector = true;

            for (int i = 0; i < m_Components.Length; ++i)
                if (m_Components[i] != null)
                    m_Components[i].hideFlags |= HideFlags.HideInInspector;
        }
        
        void SetDefault()
        {
            var existing = GetComponents<CinemachineComponentBase>();
            for (int i = 0; i < existing.Length; ++i)
            {
#if UNITY_EDITOR
                UnityEditor.Undo.DestroyObjectImmediate(existing[i]);
#else
                UnityEngine.Object.Destroy(existing[i]);
#endif
            }

#if UNITY_EDITOR
            var orbital = UnityEditor.Undo.AddComponent<CinemachineOrbitalTransposer>(gameObject);
            UnityEditor.Undo.RecordObject(orbital, "creating rig");
            orbital.m_BindingMode = CinemachineTransposer.BindingMode.SimpleFollowWithWorldUp;
            UnityEditor.Undo.RecordObject(
                UnityEditor.Undo.AddComponent<CinemachineComposer>(gameObject), 
                "creating rig");
#else
            gameObject.AddComponent<CinemachineOrbitalTransposer>().m_BindingMode 
                    = CinemachineTransposer.BindingMode.SimpleFollowWithWorldUp;
            gameObject.AddComponent<CinemachineComposer>();
#endif
            InvalidateComponentCache();
            m_Rigs = new Rig[2] { new Rig(), new Rig() };

            // Default orbits
            m_Orbits = new Orbit[3];
            m_Orbits[0].m_Height = 10;  m_Orbits[0].m_Radius = 4;
            m_Orbits[1].m_Height = 2.5f;  m_Orbits[1].m_Radius = 8;
            m_Orbits[2].m_Height = -0.5f; m_Orbits[2].m_Radius = 5;

            m_SplineCurvature = 0.5f;
        }

        private float GetVerticalAxisValue()
        {
            float range = m_VerticalAxis.m_MaxValue - m_VerticalAxis.m_MinValue;
            return (range > UnityVectorExtensions.Epsilon) ? m_VerticalAxis.Value / range : 0.5f;
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
                
        Vector2[] m_CachedOrbits;
        float m_CachedTension;
        Vector4[] m_CachedKnots;
        Vector4[] m_CachedCtrl1;
        Vector4[] m_CachedCtrl2;
        void UpdateCachedSpline()
        {
            bool cacheIsValid = (m_CachedOrbits != null && m_CachedTension == m_SplineCurvature);
            for (int i = 0; i < 3 && cacheIsValid; ++i)
                cacheIsValid = (m_CachedOrbits[i].y == m_Orbits[i].m_Height 
                    && m_CachedOrbits[i].x == m_Orbits[i].m_Radius);
            if (!cacheIsValid)
            {
                float t = m_SplineCurvature;
                m_CachedKnots = new Vector4[5];
                m_CachedCtrl1 = new Vector4[5];
                m_CachedCtrl2 = new Vector4[5];
                m_CachedKnots[1] = new Vector4(0, m_Orbits[2].m_Height, -m_Orbits[2].m_Radius, 0);
                m_CachedKnots[2] = new Vector4(0, m_Orbits[1].m_Height, -m_Orbits[1].m_Radius, 0);
                m_CachedKnots[3] = new Vector4(0, m_Orbits[0].m_Height, -m_Orbits[0].m_Radius, 0);
                m_CachedKnots[0] = Vector4.Lerp(m_CachedKnots[0], Vector4.zero, t);
                m_CachedKnots[4] = Vector4.Lerp(m_CachedKnots[3], Vector4.zero, t);
                SplineHelpers.ComputeSmoothControlPoints(
                    ref m_CachedKnots, ref m_CachedCtrl1, ref m_CachedCtrl2);
                m_CachedOrbits = new Vector2[3];
                for (int i = 0; i < 3; ++i)
                    m_CachedOrbits[i] = new Vector2(m_Orbits[i].m_Radius, m_Orbits[i].m_Height);
                m_CachedTension = m_SplineCurvature;
            }
        }

        ComponentBlender mBlender;

        private CameraState CalculateNewState(Vector3 worldUp, float deltaTime)
        {
            if (mBlender == null)
                mBlender = new ComponentBlender(this);
            CameraState state = PullStateFromVirtualCamera(worldUp, ref m_Lens);
            m_Rigs[(int)RigID.Top].m_Lens.SnapshotCameraReadOnlyProperties(ref m_Lens);
            m_Rigs[(int)RigID.Bottom].m_Lens.SnapshotCameraReadOnlyProperties(ref m_Lens);

            // Update our axes
            bool activeCam = (deltaTime >= 0) || CinemachineCore.Instance.IsLive(this);
            if (activeCam)
            {
                if (m_VerticalAxis.Update(deltaTime))
                    m_VerticalRecentering.CancelRecentering();
                //if (m_RadialAxis.Update(deltaTime))
                //    m_RadialRecentering.CancelRecentering();
            }
            m_VerticalRecentering.DoRecentering(ref m_VerticalAxis, deltaTime, 0.5f);
            //m_RadialRecentering.DoRecentering(ref m_RadialAxis, deltaTime, 0.5f);

            // Set the Reference LookAt
            Transform lookAtTarget = LookAt;
            if (lookAtTarget != null)
                state.ReferenceLookAt = lookAtTarget.position;

            // Blend from the appropriate rigs
            mBlender.Blend(GetVerticalAxisValue());

            // Blend the lens
            if (m_Rigs[mBlender.OtherRig].m_CustomLens)
                state.Lens = LensSettings.Lerp(
                    state.Lens, m_Rigs[mBlender.OtherRig].m_Lens, mBlender.BlendAmount);

            // Apply the component pipeline
            for (CinemachineCore.Stage stage = CinemachineCore.Stage.Body; 
                stage < CinemachineCore.Stage.Finalize; ++stage)
            {
                var c = m_Components[(int)stage];
                if (c != null)
                    c.PrePipelineMutateCameraState(ref state);
            }
            for (CinemachineCore.Stage stage = CinemachineCore.Stage.Body; 
                stage < CinemachineCore.Stage.Finalize; ++stage)
            {
                var c = m_Components[(int)stage];
                if (c != null)
                    c.MutateCameraState(ref state, deltaTime);
                else if (stage == CinemachineCore.Stage.Aim)
                    state.BlendHint |= CameraState.BlendHintValue.IgnoreLookAtTarget;
                InvokePostPipelineStageCallback(this, stage, ref state, deltaTime);
            }

            // Restore the components
            mBlender.Restore();

            return state;
        }

        internal class ComponentBlender
        {
            CinemachineTransposer.BlendableSettings orbitalSaved = new CinemachineTransposer.BlendableSettings();
            CinemachineComposer.BlendableSettings composerSaved = new CinemachineComposer.BlendableSettings();
            CinemachineBasicMultiChannelPerlin.BlendableSettings noiseSaved = new CinemachineBasicMultiChannelPerlin.BlendableSettings();

            CinemachineTransposer.BlendableSettings orbitalWorking = new CinemachineTransposer.BlendableSettings();
            CinemachineComposer.BlendableSettings composerWorking = new CinemachineComposer.BlendableSettings();
            CinemachineBasicMultiChannelPerlin.BlendableSettings noiseWorking = new CinemachineBasicMultiChannelPerlin.BlendableSettings();

            public int OtherRig { get; set; }
            public float BlendAmount { get; set; }
            CinemachineNewFreeLook mFreeLook;

            public ComponentBlender(CinemachineNewFreeLook freeLook) { mFreeLook = freeLook; }

            public void Blend(float y)
            {
                Vector3 followOffset = mFreeLook.GetLocalPositionForCameraFromInput(y);

                if (y < 0.5f)
                {
                    BlendAmount = 1 - (y * 2);
                    OtherRig = (int)RigID.Bottom;
                }
                else
                {
                    BlendAmount = (y - 0.5f) * 2f;
                    OtherRig = (int)RigID.Top;
                }

                var orbital = mFreeLook.Orbital;
                if (orbital != null && mFreeLook.m_Rigs[OtherRig].m_CustomBody)
                {
                    orbital.GetBlendableSettings(orbitalSaved);
                    orbital.GetBlendableSettings(orbitalWorking);
                    orbitalWorking.LerpTo(mFreeLook.m_Rigs[OtherRig].m_Body, BlendAmount);
                    orbital.SetBlendableSettings(orbitalWorking);
                }
                if (orbital != null)
                    orbital.m_FollowOffset = followOffset;

                var composer = mFreeLook.m_Components[(int)CinemachineCore.Stage.Aim] as CinemachineComposer;
                if (composer != null && mFreeLook.m_Rigs[OtherRig].m_CustomAim)
                {
                    composer.GetBlendableSettings(composerSaved);
                    composer.GetBlendableSettings(composerWorking);
                    composerWorking.LerpTo(mFreeLook.m_Rigs[OtherRig].m_Aim, BlendAmount);
                    composer.SetBlendableSettings(composerWorking);
                }

                var noise = mFreeLook.m_Components[(int)CinemachineCore.Stage.Noise] as CinemachineBasicMultiChannelPerlin;
                if (noise != null && mFreeLook.m_Rigs[OtherRig].m_CustomNoise)
                {
                    noise.GetBlendableSettings(noiseSaved);
                    noise.GetBlendableSettings(noiseWorking);
                    noiseWorking.LerpTo(mFreeLook.m_Rigs[OtherRig].m_Noise, BlendAmount);
                    noise.SetBlendableSettings(noiseWorking);
                }
            }

            public void Restore()
            {
                var orbital = mFreeLook.Orbital;
                if (orbital != null && mFreeLook.m_Rigs[OtherRig].m_CustomBody)
                    orbital.SetBlendableSettings(orbitalSaved);

                var composer = mFreeLook.m_Components[(int)CinemachineCore.Stage.Aim] as CinemachineComposer;
                if (composer != null && mFreeLook.m_Rigs[OtherRig].m_CustomAim)
                    composer.SetBlendableSettings(composerSaved);

                var noise = mFreeLook.m_Components[(int)CinemachineCore.Stage.Noise] as CinemachineBasicMultiChannelPerlin;
                if (noise != null && mFreeLook.m_Rigs[OtherRig].m_CustomNoise)
                    noise.SetBlendableSettings(noiseSaved);
            }
        }
    }
}
