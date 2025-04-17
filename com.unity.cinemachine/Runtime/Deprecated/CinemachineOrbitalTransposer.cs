#if !CINEMACHINE_NO_CM2_SUPPORT
using System;
using UnityEngine;
using UnityEngine.Serialization;
using Unity.Cinemachine.TargetTracking;

namespace Unity.Cinemachine
{
    /// <summary>Tracks an object's velocity with a filter to determine a reasonably
    /// steady direction for the object's current trajectory.</summary>
    [Obsolete]
    public class HeadingTracker
    {
        struct Item
        {
            public Vector3 velocity;
            public float weight;
            public float time;
        };
        Item[] mHistory;
        int mTop;
        int mBottom;
        int mCount;

        Vector3 mHeadingSum;
        float mWeightSum = 0;
        float mWeightTime = 0;

        Vector3 mLastGoodHeading = Vector3.zero;

        /// <summary>Construct a heading tracker with a given filter size</summary>
        /// <param name="filterSize">The size of the filter.  The larger the filter, the
        /// more constanct (and laggy) is the heading.  30 is pretty big.</param>
        public HeadingTracker(int filterSize)
        {
            mHistory = new Item[filterSize];
            float historyHalfLife = filterSize / 5f; // somewhat arbitrarily
            mDecayExponent = -Mathf.Log(2f) / historyHalfLife;
            ClearHistory();
        }

        /// <summary>Get the current filter size</summary>
        public int FilterSize { get { return mHistory.Length; } }

        void ClearHistory()
        {
            mTop = mBottom = mCount = 0;
            mWeightSum = 0;
            mHeadingSum = Vector3.zero;
        }

        static float mDecayExponent;
        static float Decay(float time) { return Mathf.Exp(time * mDecayExponent); }

        /// <summary>Add a new velocity frame.  This should be called once per frame,
        /// unless the velocity is zero</summary>
        /// <param name="velocity">The object's velocity this frame</param>
        public void Add(Vector3 velocity)
        {
            if (FilterSize == 0)
            {
                mLastGoodHeading = velocity;
                return;
            }
            float weight = velocity.magnitude;
            if (weight > UnityVectorExtensions.Epsilon)
            {
                Item item = new Item();
                item.velocity = velocity;
                item.weight = weight;
                item.time = CinemachineCore.CurrentTime;
                if (mCount == FilterSize)
                    PopBottom();
                ++mCount;
                mHistory[mTop] = item;
                if (++mTop == FilterSize)
                    mTop = 0;

                mWeightSum *= Decay(item.time - mWeightTime);
                mWeightTime = item.time;
                mWeightSum += weight;
                mHeadingSum += item.velocity;
            }
        }

        void PopBottom()
        {
            if (mCount > 0)
            {
                float time = CinemachineCore.CurrentTime;
                Item item = mHistory[mBottom];
                if (++mBottom == FilterSize)
                    mBottom = 0;
                --mCount;

                float decay = Decay(time - item.time);
                mWeightSum -= item.weight * decay;
                mHeadingSum -= item.velocity * decay;
                if (mWeightSum <= UnityVectorExtensions.Epsilon || mCount == 0)
                    ClearHistory();
            }
        }

        /// <summary>Decay the history.  This should be called every frame.</summary>
        public void DecayHistory()
        {
            float time = CinemachineCore.CurrentTime;
            float decay = Decay(time - mWeightTime);
            mWeightSum *= decay;
            mWeightTime = time;
            if (mWeightSum < UnityVectorExtensions.Epsilon)
                ClearHistory();
            else
                mHeadingSum = mHeadingSum * decay;
        }

        /// <summary>Get the filtered heading.</summary>
        /// <returns>The filtered direction of motion</returns>
        public Vector3 GetReliableHeading()
        {
            // Update Last Good Heading
            if (mWeightSum > UnityVectorExtensions.Epsilon
                && (mCount == mHistory.Length || mLastGoodHeading.AlmostZero()))
            {
                Vector3  h = mHeadingSum / mWeightSum;
                if (!h.AlmostZero())
                    mLastGoodHeading = h.normalized;
            }
            return mLastGoodHeading;
        }
    }

    /// <summary>
    /// This is a deprecated component.  Use CinemachineOrbitalFollow instead.
    /// </summary>
    [Obsolete("CinemachineOrbitalTransposer has been deprecated. Use CinemachineOrbitalFollow instead")]
    [AddComponentMenu("")] // Don't display in add component menu
    [SaveDuringPlay]
    [CameraPipeline(CinemachineCore.Stage.Body)]
    public class CinemachineOrbitalTransposer : CinemachineTransposer, AxisState.IRequiresInput
    {
        /// <summary>
        /// How the "forward" direction is defined.  Orbital offset is in relation to the forward
        /// direction.
        /// </summary>
        [Serializable]
        public struct Heading
        {
            /// <summary>
            /// Sets the algorithm for determining the target's heading for purposes
            /// of re-centering the camera
            /// </summary>
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
            [Tooltip("How 'forward' is defined.  The camera will be placed by default behind the target.  "
                + "PositionDelta will consider 'forward' to be the direction in which the target is moving.")]
            public HeadingDefinition m_Definition;

            /// <summary>Size of the velocity sampling window for target heading filter.
            /// Used only if deriving heading from target's movement</summary>
            [Range(0, 10)]
            [Tooltip("Size of the velocity sampling window for target heading filter.  This filters out "
                + "irregularities in the target's movement.  Used only if deriving heading from target's "
                + "movement (PositionDelta or Velocity)")]
            public int m_VelocityFilterStrength;

            /// <summary>Additional Y rotation applied to the target heading.
            /// When this value is 0, the camera will be placed behind the target</summary>
            [Range(-180f, 180f)]
            [FormerlySerializedAs("m_HeadingBias")]
            [Tooltip("Where the camera is placed when the X-axis value is zero.  This is a rotation in "
                + "degrees around the Y axis.  When this value is 0, the camera will be placed behind "
                + "the target.  Nonzero offsets will rotate the zero position around the target.")]
            public float m_Bias;

            /// <summary>Constructor</summary>
            /// <param name="def">The heading definition</param>
            /// <param name="filterStrength">The strength of the heading filter</param>
            /// <param name="bias">The heading bias</param>
            public Heading(HeadingDefinition def, int filterStrength, float bias)
            {
                m_Definition = def;
                m_VelocityFilterStrength = filterStrength;
                m_Bias = bias;
            }
        };

        /// <summary>The definition of Forward.  Camera will follow behind.</summary>
        [Space]
        [Tooltip("The definition of Forward.  Camera will follow behind.")]
        public Heading m_Heading = new Heading(Heading.HeadingDefinition.TargetForward, 4, 0);

        /// <summary>Parameters that control Automating Heading Recentering</summary>
        [Tooltip("Automatic heading recentering.  The settings here defines how the camera "
            + "will reposition itself in the absence of player input.")]
        public AxisState.Recentering m_RecenterToTargetHeading = new AxisState.Recentering(true, 1, 2);

        /// <summary>Axis representing the current heading.  Value is in degrees
        /// and represents a rotation about the up vector</summary>
        [Tooltip("Heading Control.  The settings here control the behaviour of the camera "
            + "in response to the player's input.")]
        public AxisState m_XAxis = new AxisState(-180, 180, true, false, 300f, 0.1f, 0.1f, "Mouse X", true);

        /// <summary>
        /// Helper object that tracks the Follow target, with damping
        /// </summary>
        Tracker m_TargetTracker;

        /// <summary>Legacy support</summary>
        [SerializeField] [HideInInspector] [FormerlySerializedAs("m_Radius")] private float m_LegacyRadius = float.MaxValue;
        [SerializeField] [HideInInspector] [FormerlySerializedAs("m_HeightOffset")] private float m_LegacyHeightOffset = float.MaxValue;
        [SerializeField] [HideInInspector] [FormerlySerializedAs("m_HeadingBias")] private float m_LegacyHeadingBias = float.MaxValue;

        /// <summary>Legacy support for old serialized versions</summary>
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
        [FormerlySerializedAs("m_HeadingIsSlave")]
        [HideInInspector, NoSaveDuringPlay]
        public bool m_HeadingIsDriven = false;

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
            = (CinemachineOrbitalTransposer orbital, float deltaTime, Vector3 up) => {
                    return orbital.UpdateHeading(
                        deltaTime, up, ref orbital.m_XAxis,
                        ref orbital.m_RecenterToTargetHeading,
                        CinemachineCore.IsLive(orbital.VirtualCamera));
                };

        /// <summary>
        /// Update the X axis and calculate the heading.  This can be called by a delegate
        /// with a custom axis.  Note that this method is obsolete.
        /// </summary>
        /// <param name="deltaTime">Used for damping.  If less than 0, no damping is done.</param>
        /// <param name="up">World Up, set by the CinemachineBrain</param>
        /// <param name="axis">The axis whose heading to update.</param>
        /// <returns>Axis value</returns>
        public float UpdateHeading(float deltaTime, Vector3 up, ref AxisState axis)
        {
            return UpdateHeading(deltaTime, up, ref axis, ref m_RecenterToTargetHeading, true);
        }

        /// <summary>
        /// Update the X axis and calculate the heading.  This can be called by a delegate
        /// with a custom axis.
        /// </summary>
        /// <param name="deltaTime">Used for damping.  If less than 0, no damping is done.</param>
        /// <param name="up">World Up, set by the CinemachineBrain</param>
        /// <param name="axis">The axis whose heading to update.</param>
        /// <param name="recentering">The recentering state.</param>
        /// <param name="isLive">true if the vcam is live</param>
        /// <returns>Axis value</returns>
        public float UpdateHeading(
            float deltaTime, Vector3 up, ref AxisState axis,
            ref AxisState.Recentering recentering, bool isLive)
        {
            if (m_BindingMode == BindingMode.LazyFollow)
            {
                axis.m_MinValue = -180;
                axis.m_MaxValue = 180;
            }

            // Only read joystick when game is playing
            if (deltaTime < 0 || !VirtualCamera.PreviousStateIsValid || !isLive)
            {
                axis.Reset();
                recentering.CancelRecentering();
            }
            else if (axis.Update(deltaTime))
                recentering.CancelRecentering();

            if (m_BindingMode == BindingMode.LazyFollow)
            {
                float finalHeading = axis.Value;
                axis.Value = 0;
                return finalHeading;
            }

            var state = VcamState;
            float targetHeading = GetTargetHeading(
                axis.Value, m_TargetTracker.GetReferenceOrientation(this, m_BindingMode, up, ref state));
            recentering.DoRecentering(ref axis, deltaTime, targetHeading);
            return axis.Value;
        }

        /// <summary>
        /// Standard OnEnable call.  Updates the input axis provider.
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();

            // GML todo: do we really need this?
            m_PreviousTarget = null;
            m_LastTargetPosition = Vector3.zero;

            UpdateInputAxisProvider();
        }

        /// <summary>Returns true if this object requires user input from a IInputAxisProvider.</summary>
        /// <returns>Returns true when input is required.</returns>
        bool AxisState.IRequiresInput.RequiresInput() => true;

        /// <summary>
        /// API for the inspector.  Internal use only
        /// </summary>
        internal void UpdateInputAxisProvider()
        {
            m_XAxis.SetInputAxisProvider(0, null);
            if (!m_HeadingIsDriven && VirtualCamera != null)
            {
                var provider = VirtualCamera.GetComponent<AxisState.IInputAxisProvider>();
                if (provider != null)
                    m_XAxis.SetInputAxisProvider(0, provider);
            }
        }

        private Vector3 m_LastTargetPosition = Vector3.zero;
        private HeadingTracker mHeadingTracker;
#if CINEMACHINE_PHYSICS
        private Rigidbody m_TargetRigidBody = null;
#endif
        private Transform m_PreviousTarget;
        private Vector3 m_LastCameraPosition;

        /// <summary>This is called to notify the user that a target got warped,
        /// so that we can update its internal state to make the camera
        /// also warp seamlessly.</summary>
        /// <param name="target">The object that was warped</param>
        /// <param name="positionDelta">The amount the target's position changed</param>
        public override void OnTargetObjectWarped(Transform target, Vector3 positionDelta)
        {
            base.OnTargetObjectWarped(target, positionDelta);
            if (target == FollowTarget)
            {
                m_LastTargetPosition += positionDelta;
                m_LastCameraPosition += positionDelta;
            }
        }

        /// <summary>
        /// Force the virtual camera to assume a given position and orientation
        /// </summary>
        /// <param name="pos">Worldspace position to take</param>
        /// <param name="rot">Worldspace orientation to take</param>
        public override void ForceCameraPosition(Vector3 pos, Quaternion rot)
        {
            base.ForceCameraPosition(pos, rot);
            m_LastCameraPosition = pos;
            m_XAxis.Value = GetAxisClosestValue(pos, VirtualCamera.State.ReferenceUp);
        }

        /// <summary>Notification that this virtual camera is going live.
        /// Base class implementation does nothing.</summary>
        /// <param name="fromCam">The camera being deactivated.  May be null.</param>
        /// <param name="worldUp">Default world Up, set by the CinemachineBrain</param>
        /// <param name="deltaTime">Delta time for time-based effects (ignore if less than or equal to 0)</param>
        /// <returns>True if the vcam should do an internal update as a result of this call</returns>
        public override bool OnTransitionFromCamera(
            ICinemachineCamera fromCam, Vector3 worldUp, float deltaTime)
        {
            m_RecenterToTargetHeading.DoRecentering(ref m_XAxis, -1, 0);
            m_RecenterToTargetHeading.CancelRecentering();
            if (fromCam != null //&& fromCam.Follow == FollowTarget
                && m_BindingMode != BindingMode.LazyFollow
                && (VirtualCamera.State.BlendHint & CameraState.BlendHints.InheritPosition) != 0
                && !CinemachineCore.IsLiveInBlend(VirtualCamera))
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
            var state = VcamState;
            Quaternion orient = m_TargetTracker.GetReferenceOrientation(this, m_BindingMode, up, ref state);
            Vector3 fwd = (orient * Vector3.forward).ProjectOntoPlane(up);
            if (!fwd.AlmostZero() && FollowTarget != null)
            {
                // Get the base camera placement
                float heading = 0;
                if (m_BindingMode != BindingMode.LazyFollow)
                    heading += m_Heading.m_Bias;
                orient = orient *  Quaternion.AngleAxis(heading, up);
                Vector3 targetPos = FollowTargetPosition;
                Vector3 pos = targetPos + orient * EffectiveOffset;

                Vector3 a = (pos - targetPos).ProjectOntoPlane(up);
                Vector3 b = (cameraPos - targetPos).ProjectOntoPlane(up);
                return Vector3.SignedAngle(a, b, up);
            }
            return m_LastHeading; // Can't calculate, stay conservative
        }

        float m_LastHeading;

        /// <summary>Positions the virtual camera according to the transposer rules.</summary>
        /// <param name="curState">The current camera state</param>
        /// <param name="deltaTime">Used for damping.  If less than 0, no damping is done.</param>
        public override void MutateCameraState(ref CameraState curState, float deltaTime)
        {
            m_TargetTracker.InitStateInfo(this, deltaTime, m_BindingMode, curState.ReferenceUp);

            // Update the heading
            if (FollowTarget != m_PreviousTarget)
            {
                m_PreviousTarget = FollowTarget;
#if CINEMACHINE_PHYSICS
                m_TargetRigidBody = (m_PreviousTarget == null) ? null : m_PreviousTarget.GetComponent<Rigidbody>();
#endif
                m_LastTargetPosition = (m_PreviousTarget == null) ? Vector3.zero : m_PreviousTarget.position;
                mHeadingTracker = null;
            }
            m_LastHeading = HeadingUpdater(this, deltaTime, curState.ReferenceUp);
            float heading = m_LastHeading;
            if (IsValid)
            {
                // Calculate the heading
                if (m_BindingMode != BindingMode.LazyFollow)
                    heading += m_Heading.m_Bias;
                Quaternion headingRot = Quaternion.AngleAxis(heading, Vector3.up);

                Vector3 rawOffset = EffectiveOffset;
                Vector3 offset = headingRot * rawOffset;

                // Track the target, with damping
                m_TargetTracker.TrackTarget(
                    this, deltaTime, curState.ReferenceUp, offset, TrackerSettings, ref curState,
                    out Vector3 pos, out Quaternion orient);

                // Place the camera
                offset = orient * offset;
                curState.ReferenceUp = orient * Vector3.up;

                // Respect minimum target distance on XZ plane
                var targetPosition = FollowTargetPosition;
                pos += m_TargetTracker.GetOffsetForMinimumTargetDistance(
                    this, pos, offset, curState.RawOrientation * Vector3.forward,
                    curState.ReferenceUp, targetPosition);
                curState.RawPosition = pos + offset;

                if (deltaTime >= 0 && VirtualCamera.PreviousStateIsValid)
                {
                    var lookAt = targetPosition;
                    if (LookAtTarget != null)
                        lookAt = LookAtTargetPosition;
                    var dir0 = m_LastCameraPosition - lookAt;
                    var dir1 = curState.RawPosition - lookAt;
                    if (dir0.sqrMagnitude > 0.01f && dir1.sqrMagnitude > 0.01f)
                        curState.RotationDampingBypass = curState.RotationDampingBypass
                            * UnityVectorExtensions.SafeFromToRotation(dir0, dir1, curState.ReferenceUp);
                }
                m_LastTargetPosition = targetPosition;
                m_LastCameraPosition = curState.RawPosition;
            }
        }

        /// <summary>Internal API for the Inspector Editor, so it can draw a marker at the target</summary>
        /// <param name="worldUp">Current effective world up</param>
        /// <returns>The position of the Follow target</returns>
        internal override Vector3 GetTargetCameraPosition(Vector3 worldUp)
        {
            if (!IsValid)
                return Vector3.zero;
            float heading = m_LastHeading;
            if (m_BindingMode != BindingMode.LazyFollow)
                heading += m_Heading.m_Bias;
            var state = VcamState;
            Quaternion orient = Quaternion.AngleAxis(heading, Vector3.up);
            orient = m_TargetTracker.GetReferenceOrientation(this, m_BindingMode, worldUp, ref state) * orient;
            var pos = orient * EffectiveOffset;
            pos += m_LastTargetPosition;
            return pos;
        }

        // Make sure this is calld only once per frame
        private float GetTargetHeading(float currentHeading, Quaternion targetOrientation)
        {
            if (m_BindingMode == BindingMode.LazyFollow)
                return 0;
            if (FollowTarget == null)
                return currentHeading;

            var headingDef = m_Heading.m_Definition;
#if CINEMACHINE_PHYSICS
            if (headingDef == Heading.HeadingDefinition.Velocity && m_TargetRigidBody == null)
                headingDef = Heading.HeadingDefinition.PositionDelta;
#endif

            Vector3 velocity = Vector3.zero;
            switch (headingDef)
            {
                case Heading.HeadingDefinition.Velocity:
#if CINEMACHINE_PHYSICS
    #if UNITY_6000_0_OR_NEWER
                    velocity = m_TargetRigidBody.linearVelocity;
    #else
                    velocity = m_TargetRigidBody.velocity;
    #endif
                    break;
#endif
                case Heading.HeadingDefinition.PositionDelta:
                    velocity = FollowTargetPosition - m_LastTargetPosition;
                    break;
                case Heading.HeadingDefinition.TargetForward:
                    velocity = FollowTargetRotation * Vector3.forward;
                    break;
                default:
                case Heading.HeadingDefinition.WorldForward:
                    return 0;
            }

            // Process the velocity and derive the heading from it.
            Vector3 up = targetOrientation * Vector3.up;
            velocity = velocity.ProjectOntoPlane(up);
            if (headingDef != Heading.HeadingDefinition.TargetForward)
            {
                int filterSize = m_Heading.m_VelocityFilterStrength * 5;
                if (mHeadingTracker == null || mHeadingTracker.FilterSize != filterSize)
                    mHeadingTracker = new HeadingTracker(filterSize);
                mHeadingTracker.DecayHistory();
                if (!velocity.AlmostZero())
                    mHeadingTracker.Add(velocity);
                velocity = mHeadingTracker.GetReliableHeading();
            }
            if (!velocity.AlmostZero())
                return UnityVectorExtensions.SignedAngle(
                    targetOrientation * Vector3.forward, velocity, up);

            // If no reliable heading, then stay where we are.
            return currentHeading;
        }

        // Helper to upgrade to CM3
        internal void UpgradeToCm3(CinemachineOrbitalFollow c)
        {
            c.TrackerSettings = TrackerSettings;
            c.OrbitStyle = CinemachineOrbitalFollow.OrbitStyles.Sphere;
            c.Radius = -m_FollowOffset.z;

            c.HorizontalAxis.Range = new Vector2(m_XAxis.m_MinValue, m_XAxis.m_MaxValue);
            c.HorizontalAxis.Wrap = m_XAxis.m_Wrap;
            c.HorizontalAxis.Center = c.HorizontalAxis.ClampValue(0);
            c.HorizontalAxis.Value = c.HorizontalAxis.ClampValue(m_XAxis.Value);
            c.HorizontalAxis.Recentering = new ()
            {
                Enabled = m_RecenterToTargetHeading.m_enabled,
                Time = m_RecenterToTargetHeading.m_RecenteringTime,
                Wait = m_RecenterToTargetHeading.m_WaitTime
            };

            c.VerticalAxis.Center = c.VerticalAxis.Value = m_FollowOffset.y;
            c.VerticalAxis.Range = new Vector2(c.VerticalAxis.Center, c.VerticalAxis.Center);

            c.RadialAxis.Range = Vector2.one;
            c.RadialAxis.Center = c.HorizontalAxis.Value = 1;
        }
    }
}
#endif
