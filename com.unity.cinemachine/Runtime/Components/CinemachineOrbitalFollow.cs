using UnityEngine;
using System.Collections.Generic;
using System;
using Unity.Cinemachine.TargetTracking;

namespace Unity.Cinemachine
{
    /// <summary>
    /// This is a CinemachineComponent in the the Body section of the component pipeline.
    /// Its job is to position the camera somewhere on a spheroid centered at the Follow target.
    ///
    /// The position on the sphere and the radius of the sphere can be controlled by user input.
    /// </summary>
    [AddComponentMenu("Cinemachine/Procedural/Position Control/Cinemachine Orbital Follow")]
    [SaveDuringPlay]
    [DisallowMultipleComponent]
    [CameraPipeline(CinemachineCore.Stage.Body)]
    [RequiredTarget(RequiredTargetAttribute.RequiredTargets.Tracking)]
    [HelpURL(Documentation.BaseURL + "manual/CinemachineOrbitalFollow.html")]
    public class CinemachineOrbitalFollow
        : CinemachineComponentBase, IInputAxisOwner, IInputAxisResetSource
        , CinemachineFreeLookModifier.IModifierValueSource
        , CinemachineFreeLookModifier.IModifiablePositionDamping
        , CinemachineFreeLookModifier.IModifiableDistance
    {
        /// <summary>Offset from the object's center in local space.
        /// Use this to fine-tune the orbit when the desired focus of the orbit is not
        /// the tracked object's center</summary>
        [Tooltip("Offset from the target object's center in target-local space. Use this to fine-tune the "
            + "orbit when the desired focus of the orbit is not the tracked object's center.")]
        public Vector3 TargetOffset;

        /// <summary>Settings to control damping for target tracking.</summary>
        public TrackerSettings TrackerSettings = TrackerSettings.Default;

        /// <summary>How to construct the surface on which the camera will travel</summary>
        public enum OrbitStyles
        {
            /// <summary>Camera is at a fixed distance from the target,
            /// defining a sphere</summary>
            Sphere,
            /// <summary>Camera surface is built by extruding a line connecting 3 circular
            /// orbits around the target</summary>
            ThreeRing
        }

        /// <summary>How to construct the surface on which the camera will travel</summary>
        [Tooltip("Defines the manner in which the orbit surface is constructed." )]
        public OrbitStyles OrbitStyle = OrbitStyles.Sphere;

        /// <summary>The camera will be placed at this distance from the Follow target</summary>
        [Tooltip("The camera will be placed at this distance from the Follow target.")]
        public float Radius = 10;

        /// <summary>Defines a complex surface rig from 3 horizontal rings.</summary>
        [Tooltip("Defines a complex surface rig from 3 horizontal rings.")]
        [HideFoldout]
        public Cinemachine3OrbitRig.Settings Orbits = Cinemachine3OrbitRig.Settings.Default;

        /// <summary>Defines the reference frame in which horizontal recentering is done.</summary>
        public enum ReferenceFrames
        {
            /// <summary>Static reference frame.  Axis center value is not dynamically updated.</summary>
            AxisCenter,

            /// <summary>Axis center is dynamically adjusted to be behind the parent
            /// object's forward.</summary>
            ParentObject,

            /// <summary>Axis center is dynamically adjusted to be behind the
            /// Tracking Target's forward.</summary>
            TrackingTarget,

            /// <summary>Axis center is dynamically adjusted to be behind the
            /// LookAt Target's forward.</summary>
            LookAtTarget
        }

        /// <summary>Defines the reference frame for horizontal recentering.  The axis center
        /// will be dynamically updated to be behind the selected object.</summary>
        [Tooltip("Defines the reference frame for horizontal recentering.  The axis center "
            + "will be dynamically updated to be behind the selected object.")]
        public ReferenceFrames RecenteringTarget = ReferenceFrames.TrackingTarget;

        /// <summary>Axis representing the current horizontal rotation.  Value is in degrees
        /// and represents a rotation about the up vector</summary>
        [Tooltip("Axis representing the current horizontal rotation.  Value is in degrees "
            + "and represents a rotation about the up vector.")]
        public InputAxis HorizontalAxis = DefaultHorizontal;

        /// <summary>Axis representing the current vertical rotation.  Value is in degrees
        /// and represents a rotation about the right vector</summary>
        [Tooltip("Axis representing the current vertical rotation.  Value is in degrees "
            + "and represents a rotation about the right vector.")]
        public InputAxis VerticalAxis = DefaultVertical;

        /// <summary>Axis controlling the scale of the current distance.  Value is a scalar
        /// multiplier and is applied to the specified camera distance</summary>
        [Tooltip("Axis controlling the scale of the current distance.  Value is a scalar "
            + "multiplier and is applied to the specified camera distance.")]
        public InputAxis RadialAxis = DefaultRadial;

        // State information
        Vector3 m_PreviousOffset;

        // Helper object to track the Follow target
        Tracker m_TargetTracker;

        // 3-rig orbit implementation
        Cinemachine3OrbitRig.OrbitSplineCache m_OrbitCache;

        /// <summary>
        /// Input axis controller registers here a delegate to call when the camera is reset
        /// </summary>
        Action m_ResetHandler;

        /// <summary>Internal API for inspector</summary>
        internal Vector3 TrackedPoint { get; private set; }

        void OnValidate()
        {
            Radius = Mathf.Max(0, Radius);
            TrackerSettings.Validate();
            HorizontalAxis.Validate();
            VerticalAxis.Validate();
            RadialAxis.Validate();
            RadialAxis.Range.x = Mathf.Max(RadialAxis.Range.x, Epsilon);

            HorizontalAxis.Restrictions &= ~(InputAxis.RestrictionFlags.NoRecentering | InputAxis.RestrictionFlags.RangeIsDriven);
        }

        void Reset()
        {
            TargetOffset = Vector3.zero;
            TrackerSettings = TrackerSettings.Default;
            OrbitStyle = OrbitStyles.Sphere;
            Radius = 10;
            Orbits = Cinemachine3OrbitRig.Settings.Default;
            HorizontalAxis = DefaultHorizontal;
            VerticalAxis = DefaultVertical;
            RadialAxis = DefaultRadial;
        }

        static InputAxis DefaultHorizontal => new () { Value = 0, Range = new Vector2(-180, 180), Wrap = true, Center = 0, Recentering = InputAxis.RecenteringSettings.Default };
        static InputAxis DefaultVertical => new () { Value = 17.5f, Range = new Vector2(-10, 45), Wrap = false, Center = 17.5f, Recentering = InputAxis.RecenteringSettings.Default };
        static InputAxis DefaultRadial => new () { Value = 1, Range = new Vector2(1, 1), Wrap = false, Center = 1, Recentering = InputAxis.RecenteringSettings.Default };

        /// <summary>True if component is enabled and has a valid Follow target</summary>
        public override bool IsValid => enabled && FollowTarget != null;

        /// <summary>Get the Cinemachine Pipeline stage that this component implements.
        /// Always returns the Body stage</summary>
        public override CinemachineCore.Stage Stage => CinemachineCore.Stage.Body;

        /// <summary>
        /// Report maximum damping time needed for this component.
        /// </summary>
        /// <returns>Highest damping setting in this component</returns>
        public override float GetMaxDampTime() => TrackerSettings.GetMaxDampTime();

        /// <summary>Report the available input axes</summary>
        /// <param name="axes">Output list to which the axes will be added</param>
        void IInputAxisOwner.GetInputAxes(List<IInputAxisOwner.AxisDescriptor> axes)
        {
            axes.Add(new () { DrivenAxis = () => ref HorizontalAxis, Name = "Look Orbit X", Hint = IInputAxisOwner.AxisDescriptor.Hints.X });
            axes.Add(new () { DrivenAxis = () => ref VerticalAxis, Name = "Look Orbit Y", Hint = IInputAxisOwner.AxisDescriptor.Hints.Y });
            axes.Add(new () { DrivenAxis = () => ref RadialAxis, Name = "Orbit Scale", Hint = IInputAxisOwner.AxisDescriptor.Hints.Y });
        }

        /// <summary>Register a handler that will be called when input needs to be reset</summary>
        /// <param name="handler">The handler to register</param>
        void IInputAxisResetSource.RegisterResetHandler(Action handler) => m_ResetHandler += handler;

        /// <summary>Unregister a handler that will be called when input needs to be reset</summary>
        /// <param name="handler">The handler to unregister</param>
        void IInputAxisResetSource.UnregisterResetHandler(Action handler) => m_ResetHandler -= handler;

        /// <summary>Inspector checks this and displays warning if no handler</summary>
        bool IInputAxisResetSource.HasResetHandler => m_ResetHandler != null;

        float CinemachineFreeLookModifier.IModifierValueSource.NormalizedModifierValue 
            => GetCameraPoint().w / Mathf.Max(Epsilon, RadialAxis.Value);

        Vector3 CinemachineFreeLookModifier.IModifiablePositionDamping.PositionDamping
        {
            get => TrackerSettings.PositionDamping;
            set => TrackerSettings.PositionDamping = value;
        }

        float CinemachineFreeLookModifier.IModifiableDistance.Distance
        {
            get => Radius;
            set => Radius = value;
        }

        /// <summary>
        /// For inspector.
        /// Get the camera offset corresponding to the normalized position, which ranges from -1...1.
        /// </summary>
        /// <returns>Camera position in target local space</returns>
        internal Vector3 GetCameraOffsetForNormalizedAxisValue(float t)
            => m_OrbitCache.SplineValue(Mathf.Clamp01((t + 1) * 0.5f));

        Vector4 GetCameraPoint()
        {
            Vector3 pos;
            float t;
            if (OrbitStyle == OrbitStyles.ThreeRing)
            {
                if (m_OrbitCache.SettingsChanged(Orbits))
                    m_OrbitCache.UpdateOrbitCache(Orbits);
                var v = m_OrbitCache.SplineValue(VerticalAxis.GetNormalizedValue());
                v *= RadialAxis.Value;
                pos = Quaternion.AngleAxis(HorizontalAxis.Value, Vector3.up) * v;
                t = v.w;
            }
            else
            {
                var rot = Quaternion.Euler(VerticalAxis.Value, HorizontalAxis.Value, 0);
                pos = rot * new Vector3(0, 0, -Radius * RadialAxis.Value);
                t = VerticalAxis.GetNormalizedValue() * 2 - 1;
            }
            if (TrackerSettings.BindingMode == BindingMode.LazyFollow)
                pos.z = -Mathf.Abs(pos.z);

            return new Vector4(pos.x, pos.y, pos.z, t);
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
            m_ResetHandler?.Invoke(); // cancel re-centering
            if (fromCam != null
                && (VirtualCamera.State.BlendHint & CameraState.BlendHints.InheritPosition) != 0
                && !CinemachineCore.IsLiveInBlend(VirtualCamera))
            {
                var state = fromCam.State;
                ForceCameraPosition(state.GetFinalPosition(), state.GetFinalOrientation());
                return true;
            }
            return false;
        }

        /// <summary>
        /// Force the virtual camera to assume a given position and orientation
        /// </summary>
        /// <param name="pos">World-space position to take</param>
        /// <param name="rot">World-space orientation to take</param>
        public override void ForceCameraPosition(Vector3 pos, Quaternion rot)
        {
            m_ResetHandler?.Invoke(); // cancel re-centering
            if (FollowTarget != null)
            {
                var state = VcamState;
                m_PreviousOffset = (rot * Quaternion.Inverse(state.GetFinalOrientation())) * m_PreviousOffset;

                state.RawPosition = pos;
                state.RawOrientation = rot;
                state.PositionCorrection = Vector3.zero;
                state.OrientationCorrection = Quaternion.identity;

                var dir = pos - FollowTarget.TransformPoint(TargetOffset);
                var distance = dir.magnitude;
                if (distance > 0.001f)
                {
                    dir /= distance;
                    if (OrbitStyle == OrbitStyles.ThreeRing)
                        InferAxesFromPosition_ThreeRing(dir, distance, ref state);
                    else
                        InferAxesFromPosition_Sphere(dir, distance, ref state);
                }
                m_TargetTracker.OnForceCameraPosition(this, TrackerSettings.BindingMode, ref state);
            }
        }

        void InferAxesFromPosition_Sphere(Vector3 dir, float distance, ref CameraState state)
        {
            var up = state.ReferenceUp;
            var orient = m_TargetTracker.GetReferenceOrientation(this, TrackerSettings.BindingMode, up, ref state);
            var localDir = Quaternion.Inverse(orient) * dir;
            var r = UnityVectorExtensions.SafeFromToRotation(Vector3.back, localDir, up).eulerAngles;
            VerticalAxis.Value = VerticalAxis.ClampValue(UnityVectorExtensions.NormalizeAngle(r.x));
            HorizontalAxis.Value = HorizontalAxis.ClampValue(UnityVectorExtensions.NormalizeAngle(r.y));
            RadialAxis.Value = RadialAxis.ClampValue(distance / Radius);
        }

        void InferAxesFromPosition_ThreeRing(Vector3 dir, float distance, ref CameraState state)
        {
            var up = state.ReferenceUp;
            var orient = m_TargetTracker.GetReferenceOrientation(this, TrackerSettings.BindingMode, up, ref state);
            HorizontalAxis.Value = GetHorizontalAxis();
            VerticalAxis.Value = GetVerticalAxisClosestValue(out var splinePoint);
            RadialAxis.Value = RadialAxis.ClampValue(distance / splinePoint.magnitude);

            // local functions
            float GetHorizontalAxis()
            {
                var fwd = (orient * Vector3.back).ProjectOntoPlane(up);
                if (!fwd.AlmostZero())
                    return UnityVectorExtensions.SignedAngle(fwd, dir.ProjectOntoPlane(up), up);
                return HorizontalAxis.Value; // Can't calculate, stay conservative
            }

            float GetVerticalAxisClosestValue(out Vector3 splinePoint)
            {
                // Rotate the camera pos to the back
                var q = UnityVectorExtensions.SafeFromToRotation(up, Vector3.up, up);
                var localDir = q * dir;
                var flatDir = localDir; flatDir.y = 0;
                if (!flatDir.AlmostZero())
                {
                    var angle = UnityVectorExtensions.SignedAngle(flatDir, Vector3.back, Vector3.up);
                    localDir = Quaternion.AngleAxis(angle, Vector3.up) * localDir;
                }
                localDir.x = 0;
                localDir.Normalize();

                // We need to find the minimum of the angle function using steepest descent
                var t = SteepestDescent(localDir * distance);
                splinePoint = m_OrbitCache.SplineValue(t);
                return t <= 0.5f
                    ? Mathf.Lerp(VerticalAxis.Range.x, VerticalAxis.Center, MapTo01(t, 0f, 0.5f))  // [0, 0.5] -> [0, 1] -> [Range.x, Center]
                    : Mathf.Lerp(VerticalAxis.Center, VerticalAxis.Range.y, MapTo01(t, 0.5f, 1f)); // [0.5, 1] -> [0, 1] -> [Center, Range.Y]

                // local functions
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
                        var point = m_OrbitCache.SplineValue(input);
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
                        if (m_OrbitCache.SettingsChanged(Orbits))
                            m_OrbitCache.UpdateOrbitCache(Orbits);

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

                static float MapTo01(float valueToMap, float fMin, float fMax) => (valueToMap - fMin) / (fMax - fMin);
            }
        }

        /// <summary>This is called to notify the user that a target got warped,
        /// so that we can update its internal state to make the camera
        /// also warp seamlessly.</summary>
        /// <param name="target">The object that was warped</param>
        /// <param name="positionDelta">The amount the target's position changed</param>
        public override void OnTargetObjectWarped(Transform target, Vector3 positionDelta)
        {
            base.OnTargetObjectWarped(target, positionDelta);
            if (target == FollowTarget)
                m_TargetTracker.OnTargetObjectWarped(positionDelta);
        }

        /// <summary>Positions the virtual camera according to the transposer rules.</summary>
        /// <param name="curState">The current camera state</param>
        /// <param name="deltaTime">Used for damping.  If less than 0, no damping is done.</param>
        public override void MutateCameraState(ref CameraState curState, float deltaTime)
        {
            m_TargetTracker.InitStateInfo(this, deltaTime, TrackerSettings.BindingMode, curState.ReferenceUp);
            if (!IsValid)
                return;

            // Force a reset if enabled, but don't be too aggressive about it,
            // because maybe we've just inherited a position
            if (deltaTime < 0)// || !VirtualCamera.PreviousStateIsValid || !CinemachineCore.IsLive(VirtualCamera)
                m_ResetHandler?.Invoke();

            Vector3 offset = GetCameraPoint();

            var gotInputX = HorizontalAxis.TrackValueChange();
            var gotInputY = VerticalAxis.TrackValueChange();
            var gotInputZ = RadialAxis.TrackValueChange();
            if (TrackerSettings.BindingMode == BindingMode.LazyFollow)
                HorizontalAxis.SetValueAndLastValue(0);

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
            pos += orient * TargetOffset;
            TrackedPoint = pos;
            curState.RawPosition = pos + offset;

            // Compute the rotation bypass for the lookat target
            if (curState.HasLookAt())
            {
                // Handle the common case where lookAt and follow targets are not the same point.
                // If we don't do this, we can get inappropriate vertical damping when offset changes.
                var lookAtOfset = orient
                    * (curState.ReferenceLookAt - (FollowTargetPosition + FollowTargetRotation * TargetOffset));
                offset = curState.RawPosition - (pos + lookAtOfset);
            }
            if (deltaTime >= 0 && VirtualCamera.PreviousStateIsValid
                && m_PreviousOffset.sqrMagnitude > Epsilon && offset.sqrMagnitude > Epsilon)
            {
                curState.RotationDampingBypass = UnityVectorExtensions.SafeFromToRotation(
                    m_PreviousOffset, offset, curState.ReferenceUp);
            }
            m_PreviousOffset = offset;

            if (HorizontalAxis.Recentering.Enabled)
                UpdateHorizontalCenter(orient);

            // Sync recentering if the recenter times match
            gotInputX |= gotInputY && (HorizontalAxis.Recentering.Time == VerticalAxis.Recentering.Time);
            gotInputX |= gotInputZ && (HorizontalAxis.Recentering.Time == RadialAxis.Recentering.Time);
            gotInputY |= gotInputX && (VerticalAxis.Recentering.Time == HorizontalAxis.Recentering.Time);
            gotInputY |= gotInputZ && (VerticalAxis.Recentering.Time == RadialAxis.Recentering.Time);
            gotInputZ |= gotInputX && (RadialAxis.Recentering.Time == HorizontalAxis.Recentering.Time);
            gotInputZ |= gotInputY && (RadialAxis.Recentering.Time == VerticalAxis.Recentering.Time);

            HorizontalAxis.UpdateRecentering(deltaTime, gotInputX);
            VerticalAxis.UpdateRecentering(deltaTime, gotInputY);
            RadialAxis.UpdateRecentering(deltaTime, gotInputZ);
        }

        void UpdateHorizontalCenter(Quaternion referenceOrientation)
        {
            // Get the recentering target's forward vector
            var fwd = Vector3.forward;
            switch (RecenteringTarget)
            {
                case ReferenceFrames.AxisCenter:
                    if (TrackerSettings.BindingMode == BindingMode.LazyFollow)
                        HorizontalAxis.Center = 0;
                    return;
                case ReferenceFrames.ParentObject:
                    if (transform.parent != null)
                        fwd = transform.parent.forward;
                    break;
                case ReferenceFrames.TrackingTarget:
                    if (FollowTarget != null)
                        fwd = FollowTarget.forward;
                    break;
                case ReferenceFrames.LookAtTarget:
                    if (LookAtTarget != null)
                        fwd = LookAtTarget.forward;
                    break;
            }
            // Align the axis center to be behind fwd
            var up = referenceOrientation * Vector3.up;
            fwd.ProjectOntoPlane(up);
            HorizontalAxis.Center = -Vector3.SignedAngle(fwd, referenceOrientation * Vector3.forward, up);
        }

        /// For the inspector
        internal Quaternion GetReferenceOrientation() => m_TargetTracker.PreviousReferenceOrientation.normalized;
    }

    /// <summary>
    /// Helpers for the 3-Orbit rig for the OrbitalFollow component
    /// </summary>
    public static class Cinemachine3OrbitRig
    {
        /// <summary>Defines the height and radius for an orbit</summary>
        [Serializable]
        public struct Orbit
        {
            /// <summary>Radius of orbit</summary>
            [Tooltip("Horizontal radius of the orbit")]
            public float Radius;

            /// <summary>Height relative to target</summary>
            [Tooltip("Height of the horizontal orbit circle, relative to the target position")]
            public float Height;
        }

        /// <summary>
        /// Settings to define the 3-orbit FreeLook rig using OrbitalFollow.
        /// </summary>
        [Serializable]
        public struct Settings
        {
            /// <summary>Value to take at the top of the axis range</summary>
            [Tooltip("Value to take at the top of the axis range")]
            public Orbit Top;

            /// <summary>Value to take at the center of the axis range</summary>
            [Tooltip("Value to take at the center of the axis range")]
            public Orbit Center;

            /// <summary>Value to take at the bottom of the axis range</summary>
            [Tooltip("Value to take at the bottom of the axis range")]
            public Orbit Bottom;

            /// <summary>Controls how taut is the line that connects the rigs' orbits, which
            /// determines final placement on the Y axis</summary>
            [Tooltip("Controls how taut is the line that connects the rigs' orbits, "
                + "which determines final placement on the Y axis")]
            [Range(0f, 1f)]
            public float SplineCurvature;

            /// <summary>Default orbit rig</summary>
            public static Settings Default => new Settings
            {
                SplineCurvature = 0.5f,
                Top = new Orbit { Height = 5, Radius = 2 },
                Center = new Orbit { Height = 2.25f, Radius = 4 },
                Bottom= new Orbit { Height = 0.1f, Radius = 2.5f }
            };
        }

        /// <summary>
        /// Calculates and caches data necessary to implement the 3-orbit rig surface
        /// </summary>
        internal struct OrbitSplineCache
        {
            Settings OrbitSettings;

            Vector4[] CachedKnots;
            Vector4[] CachedCtrl1;
            Vector4[] CachedCtrl2;

            /// <summary>Test to see if the cache needs to be updated because the orbit settings have changed</summary>
            /// <param name="other">The current orbit settings, will be compared against the cached settings</param>
            /// <returns>True if UpdateCache() needs to be called</returns>
            public bool SettingsChanged(in Settings other)
            {
                return OrbitSettings.SplineCurvature != other.SplineCurvature
                    || OrbitSettings.Top.Height != other.Top.Height || OrbitSettings.Top.Radius != other.Top.Radius
                    || OrbitSettings.Center.Height != other.Center.Height || OrbitSettings.Center.Radius != other.Center.Radius
                    || OrbitSettings.Bottom.Height != other.Bottom.Height || OrbitSettings.Bottom.Radius != other.Bottom.Radius;
            }

            /// <summary>
            /// Update the cache according to the new orbit settings.
            /// This does a bunch of expensive calculations so should only be called when necessary.
            /// </summary>
            /// <param name="orbits"></param>
            public void UpdateOrbitCache(in Settings orbits)
            {
                OrbitSettings = orbits;

                float t = orbits.SplineCurvature;
                CachedKnots = new Vector4[5];
                CachedCtrl1 = new Vector4[5];
                CachedCtrl2 = new Vector4[5];
                CachedKnots[1] = new Vector4(0, orbits.Bottom.Height, -orbits.Bottom.Radius, -1);
                CachedKnots[2] = new Vector4(0, orbits.Center.Height, -orbits.Center.Radius, 0);
                CachedKnots[3] = new Vector4(0, orbits.Top.Height, -orbits.Top.Radius, 1);
                CachedKnots[0] = Vector4.Lerp(CachedKnots[1] + (CachedKnots[1] - CachedKnots[2]) * 0.5f, Vector4.zero, t);
                CachedKnots[4] = Vector4.Lerp(CachedKnots[3] + (CachedKnots[3] - CachedKnots[2]) * 0.5f, Vector4.zero, t);
                SplineHelpers.ComputeSmoothControlPoints(ref CachedKnots, ref CachedCtrl1, ref CachedCtrl2);
            }

            /// <summary>Get the value of a point on the spline curve</summary>
            /// <param name="t">Where on the spline arc, with 0...1 t==0.5 being the center orbit.</param>
            /// <returns>Point on the spline along the surface defined by the orbits.
            /// XYZ is the point itself, and W ranges from 0 on the bottom to 2 on the top,
            /// with 1 being the center.</returns>
            public Vector4 SplineValue(float t)
            {
                if (CachedKnots == null)
                    return Vector4.zero;

                int n = 1;
                if (t > 0.5f)
                {
                    t -= 0.5f;
                    n = 2;
                }
                Vector4 pos = SplineHelpers.Bezier3(
                    t * 2f, CachedKnots[n], CachedCtrl1[n], CachedCtrl2[n], CachedKnots[n+1]);

                // -1 <= w <= 1, where 0 == center
                pos.w = SplineHelpers.Bezier1(
                    t * 2f, CachedKnots[n].w, CachedCtrl1[n].w, CachedCtrl2[n].w, CachedKnots[n+1].w);

                return pos;
            }
        }
    }
}
