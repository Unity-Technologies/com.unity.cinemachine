using System;
using UnityEngine;
using Cinemachine.Utility;
using UnityEngine.Serialization;

namespace Cinemachine
{
    /// <summary>
    /// This is a CinemachineComponent in the the Body section of the component pipeline.
    /// Its job is to position the camera in a variable relationship to a the vcam's
    /// Follow target object, with offsets and damping.
    ///
    /// This component is typically used to implement a camera that follows its target.
    /// It can accept player input from an input device, which allows the player to
    /// dynamically control the relationship between the camera and the target,
    /// for example with a joystick.
    ///
    /// The OrbitalTransposer introduces the concept of __Heading__, which is the direction
    /// in which the target is moving, and the OrbitalTransposer will attempt to position
    /// the camera in relationship to the heading, which is by default directly behind the target.
    /// You can control the default relationship by adjusting the Heading Bias setting.
    ///
    /// If you attach an input controller to the OrbitalTransposer, then the player can also
    /// control the way the camera positions itself in relation to the target heading.  This allows
    /// the camera to move to any spot on an orbit around the target.
    /// </summary>
    [DocumentationSorting(DocumentationSortingAttribute.Level.UserRef)]
    [AddComponentMenu("")] // Don't display in add component menu
    [SaveDuringPlay]
    public class CinemachineOrbitalTransposer : CinemachineTransposer
    {
        /// <summary>
        /// How the "forward" direction is defined.  Orbital offset is in relation to the forward
        /// direction.
        /// </summary>
        [DocumentationSorting(DocumentationSortingAttribute.Level.UserRef)]
        [Serializable]
        public struct Heading
        {
            /// <summary>
            /// Sets the algorithm for determining the target's heading for purposes
            /// of re-centering the camera
            /// </summary>
            [DocumentationSorting(DocumentationSortingAttribute.Level.UserRef)]
            public enum HeadingDefinition
            {
                /// <summary>
                /// Target heading calculated from the difference between its position on
                /// the last update and current frame.
                /// </summary>
                PositionDelta,
                /// <summary>
                /// Target heading calculated from its <b>Rigidbody</b>'s velocity.
                /// If no <b>Rigidbody</b> exists, it will fall back
                /// to HeadingDerivationMode.PositionDelta
                /// </summary>
                Velocity,
                /// <summary>
                /// Target heading calculated from the Target <b>Transform</b>'s euler Y angle
                /// </summary>
                TargetForward,
                /// <summary>
                /// Default heading is a constant world space heading.
                /// </summary>
                WorldForward,
            }
            /// <summary>The method by which the 'default heading' is calculated if
            /// recentering to target heading is enabled</summary>
            [FormerlySerializedAs("m_HeadingDefinition")]
            [Tooltip("How 'forward' is defined.  The camera will be placed by default behind the target.  PositionDelta will consider 'forward' to be the direction in which the target is moving.")]
            public HeadingDefinition m_Definition;

            /// <summary>Size of the velocity sampling window for target heading filter.
            /// Used only if deriving heading from target's movement</summary>
            [Range(0, 10)]
            [Tooltip("Size of the velocity sampling window for target heading filter.  This filters out irregularities in the target's movement.  Used only if deriving heading from target's movement (PositionDelta or Velocity)")]
            public int m_VelocityFilterStrength;

            /// <summary>Additional Y rotation applied to the target heading.
            /// When this value is 0, the camera will be placed behind the target</summary>
            [Range(-180f, 180f)]
            [FormerlySerializedAs("m_HeadingBias")]
            [Tooltip("Where the camera is placed when the X-axis value is zero.  This is a rotation in degrees around the Y axis.  When this value is 0, the camera will be placed behind the target.  Nonzero offsets will rotate the zero position around the target.")]
            public float m_Bias;

            /// <summary>Constructor</summary>
            public Heading(HeadingDefinition def, int filterStrength, float bias)
            {
                m_Definition = def;
                m_VelocityFilterStrength = filterStrength;
                m_Bias = bias;
            }
        };

        /// <summary>The definition of Forward.  Camera will follow behind.</summary>
        [Space]
        [OrbitalTransposerHeadingProperty]
        [Tooltip("The definition of Forward.  Camera will follow behind.")]
        public Heading m_Heading = new Heading(Heading.HeadingDefinition.TargetForward, 4, 0);

        /// <summary>Parameters that control Automating Heading Recentering</summary>
        [Tooltip("Automatic heading recentering.  The settings here defines how the camera will reposition itself in the absence of player input.")]
        public AxisState.Recentering m_RecenterToTargetHeading = new AxisState.Recentering(true, 1, 2);

        /// <summary>Axis representing the current heading.  Value is in degrees
        /// and represents a rotation about the up vector</summary>
        [Tooltip("Heading Control.  The settings here control the behaviour of the camera in response to the player's input.")]
        [AxisStateProperty]
        public AxisState m_XAxis = new AxisState(-180, 180, true, false, 300f, 0.1f, 0.1f, "Mouse X", true);

        /// <summary>Legacy support</summary>
        [SerializeField] [HideInInspector] [FormerlySerializedAs("m_Radius")] private float m_LegacyRadius = float.MaxValue;
        [SerializeField] [HideInInspector] [FormerlySerializedAs("m_HeightOffset")] private float m_LegacyHeightOffset = float.MaxValue;
        [SerializeField] [HideInInspector] [FormerlySerializedAs("m_HeadingBias")] private float m_LegacyHeadingBias = float.MaxValue;
        protected override void OnValidate()
        {
            // Upgrade after a legacy deserialize
            if (m_LegacyRadius != float.MaxValue
                && m_LegacyHeightOffset != float.MaxValue
                && m_LegacyHeadingBias != float.MaxValue)
            {
                m_FollowOffset = new Vector3(0, m_LegacyHeightOffset, -m_LegacyRadius);
                m_LegacyHeightOffset = m_LegacyRadius = float.MaxValue;

                m_Heading.m_Bias = m_LegacyHeadingBias;
                m_XAxis.m_MaxSpeed /= 10;
                m_XAxis.m_AccelTime /= 10;
                m_XAxis.m_DecelTime /= 10;
                m_LegacyHeadingBias = float.MaxValue;
                int heading = (int)m_Heading.m_Definition;
                if (m_RecenterToTargetHeading.LegacyUpgrade(ref heading, ref m_Heading.m_VelocityFilterStrength))
                    m_Heading.m_Definition = (Heading.HeadingDefinition)heading;
            }
            m_XAxis.Validate();
            m_RecenterToTargetHeading.Validate();

            base.OnValidate();
        }

        /// <summary>
        /// Drive the x-axis setting programmatically.
        /// Automatic heading updating will be disabled.
        /// </summary>
        [HideInInspector, NoSaveDuringPlay]
        public bool m_HeadingIsSlave = false;

        /// <summary>
        /// Delegate that allows the the m_XAxis object to be replaced with another one.
        /// </summary>
        internal delegate float UpdateHeadingDelegate(
            CinemachineOrbitalTransposer orbital, float deltaTime, Vector3 up);

        /// <summary>
        /// Delegate that allows the the XAxis object to be replaced with another one.
        /// To use it, just call orbital.UpdateHeading() with a reference to a
        /// private AxisState object, and that AxisState object will be updated and
        /// used to calculate the heading.
        /// </summary>
        internal UpdateHeadingDelegate HeadingUpdater
            = (CinemachineOrbitalTransposer orbital, float deltaTime, Vector3 up)
                => { return orbital.UpdateHeading(deltaTime, up, ref orbital.m_XAxis); };

        /// <summary>
        /// Update the X axis and calculate the heading.  This can be called by a delegate
        /// with a custom axis.
        /// <param name="deltaTime">Used for damping.  If less than 0, no damping is done.</param>
        /// <param name="up">World Up, set by the CinemachineBrain</param>
        /// <param name="axis"></param>
        /// <returns>Axis value</returns>
        /// </summary>
        public float UpdateHeading(float deltaTime, Vector3 up, ref AxisState axis)
        {
            // Only read joystick when game is playing
            if (deltaTime < 0 || !CinemachineCore.Instance.IsLive(VirtualCamera))
            {
                axis.Reset();
                m_RecenterToTargetHeading.CancelRecentering();
            }
            else if (axis.Update(deltaTime))
                m_RecenterToTargetHeading.CancelRecentering();

            float targetHeading = GetTargetHeading(axis.Value, GetReferenceOrientation(up), deltaTime);
            if (m_BindingMode != BindingMode.SimpleFollowWithWorldUp)
                m_RecenterToTargetHeading.DoRecentering(ref axis, deltaTime, targetHeading);

            float finalHeading = axis.Value;
            if (m_BindingMode == BindingMode.SimpleFollowWithWorldUp)
                axis.Value = 0;
            return finalHeading;
        }

        private void OnEnable()
        {
            // GML todo: do we really need this?
            PreviousTarget = null;
            mLastTargetPosition = Vector3.zero;
        }

        private Vector3 mLastTargetPosition = Vector3.zero;
        private HeadingTracker mHeadingTracker;
        private Rigidbody mTargetRigidBody = null;
        private Transform PreviousTarget { get; set; }
        private Quaternion mHeadingPrevFrame = Quaternion.identity;
        private Vector3 mOffsetPrevFrame = Vector3.zero;

        /// <summary>This is called to notify the us that a target got warped,
        /// so that we can update its internal state to make the camera
        /// also warp seamlessy.</summary>
        /// <param name="target">The object that was warped</param>
        /// <param name="positionDelta">The amount the target's position changed</param>
        public override void OnTargetObjectWarped(Transform target, Vector3 positionDelta)
        {
            base.OnTargetObjectWarped(target, positionDelta);
            if (target == FollowTarget)
            {
                mLastTargetPosition += positionDelta;
            }
        }

        /// <summary>Notification that this virtual camera is going live.
        /// Base class implementation does nothing.</summary>
        /// <param name="fromCam">The camera being deactivated.  May be null.</param>
        /// <param name="worldUp">Default world Up, set by the CinemachineBrain</param>
        /// <param name="deltaTime">Delta time for time-based effects (ignore if less than or equal to 0)</param>
        /// <returns>True if the vcam should do an internal update as a result of this call</returns>
        public override bool OnTransitionFromCamera(
            ICinemachineCamera fromCam, Vector3 worldUp, float deltaTime,
            ref CinemachineVirtualCameraBase.TransitionParams transitionParams)
        {
            if (fromCam != null //&& fromCam.Follow == FollowTarget
                && m_BindingMode != CinemachineTransposer.BindingMode.SimpleFollowWithWorldUp
                && transitionParams.m_InheritPosition)
            {
                m_XAxis.Value = GetAxisClosestValue(fromCam.State.RawPosition, worldUp);
                return true;
            }
            return false;
        }

        /// <summary>
        /// What axis value would we need to get as close as possible to the desired cameraPos?
        /// </summary>
        /// <param name="cameraPos">camera position we would like to approximate</param>
        /// <param name="up">world up</param>
        /// <returns>The best value to put into the X axis, to approximate the desired camera pos</returns>
        public float GetAxisClosestValue(Vector3 cameraPos, Vector3 up)
        {
            Quaternion orient = GetReferenceOrientation(up);
            Vector3 fwd = (orient * Vector3.forward).ProjectOntoPlane(up);
            if (!fwd.AlmostZero() && FollowTarget != null)
            {
                // Get the base camera placement
                float heading = 0;
                if (m_BindingMode != BindingMode.SimpleFollowWithWorldUp)
                    heading += m_Heading.m_Bias;
                orient = orient *  Quaternion.AngleAxis(heading, up);
                Vector3 targetPos = FollowTargetPosition;
                Vector3 pos = targetPos + orient * EffectiveOffset;

                Vector3 a = (pos - targetPos).ProjectOntoPlane(up);
                Vector3 b = (cameraPos - targetPos).ProjectOntoPlane(up);
                return Vector3.SignedAngle(a, b, up);
            }
            return LastHeading; // Can't calculate, stay conservative
        }

        float LastHeading { get; set; }

        /// <summary>Positions the virtual camera according to the transposer rules.</summary>
        /// <param name="curState">The current camera state</param>
        /// <param name="deltaTime">Used for damping.  If less than 0, no damping is done.</param>
        public override void MutateCameraState(ref CameraState curState, float deltaTime)
        {
            InitPrevFrameStateInfo(ref curState, deltaTime);

            // Update the heading
            if (FollowTarget != PreviousTarget)
            {
                PreviousTarget = FollowTarget;
                mTargetRigidBody = (PreviousTarget == null) ? null : PreviousTarget.GetComponent<Rigidbody>();
                mLastTargetPosition = (PreviousTarget == null) ? Vector3.zero : PreviousTarget.position;
                mHeadingTracker = null;
            }
            LastHeading = HeadingUpdater(this, deltaTime, curState.ReferenceUp);
            float heading = LastHeading;

            if (IsValid)
            {
                mLastTargetPosition = FollowTargetPosition;

                // Calculate the heading
                if (m_BindingMode != BindingMode.SimpleFollowWithWorldUp)
                    heading += m_Heading.m_Bias;
                Quaternion headingRot = Quaternion.AngleAxis(heading, Vector3.up);

                // Track the target, with damping
                Vector3 offset = EffectiveOffset;
                Vector3 pos;
                Quaternion orient;
                TrackTarget(deltaTime, curState.ReferenceUp, headingRot * offset, out pos, out orient);

                // Place the camera
                curState.ReferenceUp = orient * Vector3.up;
                if (deltaTime >= 0)
                {
                    Vector3 bypass = (headingRot * offset) - mHeadingPrevFrame * mOffsetPrevFrame;
                    bypass = orient * bypass;
                    curState.PositionDampingBypass = bypass;
                }
                orient = orient * headingRot;
                curState.RawPosition = pos + orient * offset;

                mHeadingPrevFrame = (m_BindingMode == BindingMode.SimpleFollowWithWorldUp)
                    ? Quaternion.identity : headingRot;
                mOffsetPrevFrame = offset;
            }
        }

        /// <summary>Internal API for the Inspector Editor, so it can draw a marker at the target</summary>
        public override Vector3 GetTargetCameraPosition(Vector3 worldUp)
        {
            if (!IsValid)
                return Vector3.zero;
            float heading = LastHeading;
            if (m_BindingMode != BindingMode.SimpleFollowWithWorldUp)
                heading += m_Heading.m_Bias;
            Quaternion orient = Quaternion.AngleAxis(heading, Vector3.up);
            orient = GetReferenceOrientation(worldUp) * orient;
            var pos = orient * EffectiveOffset;
            pos += mLastTargetPosition;
            return pos;
        }

        static string GetFullName(GameObject current)
        {
            if (current == null)
                return "";
            if (current.transform.parent == null)
                return "/" + current.name;
            return GetFullName(current.transform.parent.gameObject) + "/" + current.name;
        }

        // Make sure this is calld only once per frame
        private float GetTargetHeading(
            float currentHeading, Quaternion targetOrientation, float deltaTime)
        {
            if (m_BindingMode == BindingMode.SimpleFollowWithWorldUp)
                return 0;
            if (FollowTarget == null)
                return currentHeading;

            var headingDef = m_Heading.m_Definition;
            if (headingDef == Heading.HeadingDefinition.Velocity && mTargetRigidBody == null)
                headingDef = Heading.HeadingDefinition.PositionDelta;

            Vector3 velocity = Vector3.zero;
            switch (headingDef)
            {
                case Heading.HeadingDefinition.PositionDelta:
                    velocity = FollowTargetPosition - mLastTargetPosition;
                    break;
                case Heading.HeadingDefinition.Velocity:
                    velocity = mTargetRigidBody.velocity;
                    break;
                case Heading.HeadingDefinition.TargetForward:
                    velocity = FollowTargetRotation * Vector3.forward;
                    break;
                default:
                case Heading.HeadingDefinition.WorldForward:
                    return 0;
            }

            // Process the velocity and derive the heading from it.
            int filterSize = m_Heading.m_VelocityFilterStrength * 5;
            if (mHeadingTracker == null || mHeadingTracker.FilterSize != filterSize)
                mHeadingTracker = new HeadingTracker(filterSize);
            mHeadingTracker.DecayHistory();
            Vector3 up = targetOrientation * Vector3.up;
            velocity = velocity.ProjectOntoPlane(up);
            if (!velocity.AlmostZero())
                mHeadingTracker.Add(velocity);

            velocity = mHeadingTracker.GetReliableHeading();
            if (!velocity.AlmostZero())
                return UnityVectorExtensions.SignedAngle(targetOrientation * Vector3.forward, velocity, up);

            // If no reliable heading, then stay where we are.
            return currentHeading;
        }
    }
}
