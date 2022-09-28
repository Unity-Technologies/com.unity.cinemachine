using UnityEngine;
using Cinemachine.Utility;
using System.Collections.Generic;
using System;
using System.Numerics;
using Cinemachine.TargetTracking;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using Vector4 = UnityEngine.Vector4;

namespace Cinemachine
{
    /// <summary>
    /// This is a CinemachineComponent in the the Body section of the component pipeline.
    /// Its job is to position the camera somewhere on a spheroid centered at the Follow target.
    ///
    /// The position on the sphere and the radius of the sphere can be controlled by user input.
    /// </summary>
    [AddComponentMenu("")] // Don't display in add component menu
    [SaveDuringPlay]
    [CameraPipeline(CinemachineCore.Stage.Body)]
    public class CinemachineOrbitalFollow 
        : CinemachineComponentBase, IInputAxisSource, IInputAxisResetSource
        , CinemachineFreeLookModifier.IModifierValueSource
        , CinemachineFreeLookModifier.IModifiablePositionDamping
        , CinemachineFreeLookModifier.IModifiableDistance
    {
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

        /// <summary>Axis representing the current horizontal rotation.  Value is in degrees
        /// and represents a rotation about the up vector</summary>
        [Tooltip("Axis representing the current horizontal rotation.  Value is in degrees "
            + "and represents a rotation about the up vector.")]
        [SerializeReference]
        public InputAxis HorizontalAxis = DefaultHorizontal;

        /// <summary>Axis representing the current vertical rotation.  Value is in degrees
        /// and represents a rotation about the right vector</summary>
        [Tooltip("Axis representing the current vertical rotation.  Value is in degrees "
            + "and represents a rotation about the right vector.")]
        [SerializeReference]
        public InputAxis VerticalAxis = DefaultVertical;

        /// <summary>Axis controlling the scale of the current distance.  Value is a scalar
        /// multiplier and is applied to the specified camera distance</summary>
        [Tooltip("Axis controlling the scale of the current distance.  Value is a scalar "
            + "multiplier and is applied to the specified camera distance.")]
        [SerializeReference]
        public InputAxis RadialAxis = DefaultRadial;

        // State information
        Vector3 m_PreviousWorldOffset;

        // Helper object to track the Follow target
        Tracker m_TargetTracker;

        // 3-rig orbit implementation
        Cinemachine3OrbitRig.OrbitSplineCache m_OrbitCache;

        /// <summary>
        /// Input axis controller registers here a delegate to call when the camera is reset
        /// </summary>
        IInputAxisResetSource.ResetHandler m_ResetHandler;
        
        void OnValidate()
        {
            Radius = Mathf.Max(0, Radius);
            TrackerSettings.Validate();
            HorizontalAxis.Validate();
            VerticalAxis.Validate();
            RadialAxis.Validate();
        }

        void Reset()
        {
            TrackerSettings = TrackerSettings.Default;
            OrbitStyle = OrbitStyles.Sphere;
            Radius = 10;
            Orbits = Cinemachine3OrbitRig.Settings.Default;
            HorizontalAxis = DefaultHorizontal;
            VerticalAxis = DefaultVertical;
            RadialAxis = DefaultRadial;
        }
        
        static InputAxis DefaultHorizontal => new () { Value = 0, Range = new Vector2(-180, 180), Wrap = true, Center = 0 };
        static InputAxis DefaultVertical => new () { Value = 17.5f, Range = new Vector2(-10, 45), Wrap = false, Center = 17.5f };
        static InputAxis DefaultRadial => new () { Value = 1, Range = new Vector2(1, 5), Wrap = false, Center = 1 };

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
        void IInputAxisSource.GetInputAxes(List<IInputAxisSource.AxisDescriptor> axes)
        {
            axes.Add(new IInputAxisSource.AxisDescriptor { Axis = HorizontalAxis, Name = "Look Orbit X", AxisIndex = 0 });
            axes.Add(new IInputAxisSource.AxisDescriptor { Axis = VerticalAxis, Name = "Look Orbit Y", AxisIndex = 1 });
            axes.Add(new IInputAxisSource.AxisDescriptor { Axis = RadialAxis, Name = "Orbit Scale", AxisIndex = 2 });
        }

        /// <summary>Register a handler that will be called when input needs to be reset</summary>
        /// <param name="handler">The handler to register</param>
        void IInputAxisResetSource.RegisterResetHandler(IInputAxisResetSource.ResetHandler handler) => m_ResetHandler += handler;

        /// <summary>Unregister a handler that will be called when input needs to be reset</summary>
        /// <param name="handler">The handler to unregister</param>
        void IInputAxisResetSource.UnregisterResetHandler(IInputAxisResetSource.ResetHandler handler) => m_ResetHandler -= handler;

        /// <summary>Inspector checks this and displays warning if no handler</summary>
        internal bool HasInputHandler => m_ResetHandler != null;

        float CinemachineFreeLookModifier.IModifierValueSource.NormalizedModifierValue => GetCameraPoint().w;

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
            if (TrackerSettings.BindingMode == BindingMode.SimpleFollowWithWorldUp)
                pos.z = -Mathf.Abs(pos.z);

            return new Vector4(pos.x, pos.y, pos.z, t);
        }
        
        /// <summary>Notification that this virtual camera is going live.
        /// Base class implementation does nothing.</summary>
        /// <param name="fromCam">The camera being deactivated.  May be null.</param>
        /// <param name="worldUp">Default world Up, set by the CinemachineBrain</param>
        /// <param name="deltaTime">Delta time for time-based effects (ignore if less than or equal to 0)</param>
        /// <param name="transitionParams">Transition settings for this vcam</param>
        /// <returns>True if the vcam should do an internal update as a result of this call</returns>
        public override bool OnTransitionFromCamera(
            ICinemachineCamera fromCam, Vector3 worldUp, float deltaTime,
            ref CinemachineVirtualCameraBase.TransitionParams transitionParams)
        {
            if (fromCam != null
                && transitionParams.InheritPosition
                && !CinemachineCore.Instance.IsLiveInBlend(VirtualCamera))
            {
                var state = fromCam.State;
                ForceCameraPosition(state.RawPosition, state.RawOrientation);
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
            base.ForceCameraPosition(pos, rot);
            m_TargetTracker.ForceCameraPosition(this, TrackerSettings.BindingMode, pos, rot, GetCameraPoint());

            m_ResetHandler?.Invoke();

            var targetPos = FollowTargetPosition;
            var dir = pos - targetPos;
            var distance = dir.magnitude;
            if (distance > 0.001f)
            {
                switch (OrbitStyle)
                {
                    case OrbitStyles.Sphere:
                    {
                        var up = VirtualCamera.State.ReferenceUp;
                        var orient = m_TargetTracker.GetReferenceOrientation(this, TrackerSettings.BindingMode, up);
                        
                        dir /= distance; // normalize
                        var radialScale = distance / Radius;
                        
                        var followTargetForward = orient * Vector3.back; // hAxis 0, vAxis 0
                        var followTargetUp = orient * Vector3.up;
                        var followTargetRight = orient * Vector3.right;
                        
                        // TODO: there is a drift in the vertical, why?
                        var vertical = dir.ProjectOntoPlane(followTargetRight);
                        VerticalAxis.Value = UnityVectorExtensions.SignedAngle(followTargetForward, vertical, followTargetRight);
                        var horizontal = dir.ProjectOntoPlane(followTargetUp);
                        HorizontalAxis.Value = UnityVectorExtensions.SignedAngle(followTargetForward, horizontal, followTargetUp);
                        RadialAxis.Value = radialScale;

                        // // does not work for binding modes that change the orientation of the rig - works for WorldSpace and LockToTargetWithWorldUp
                        // var up = VirtualCamera.State.ReferenceUp;
                        // var orient = m_TargetTracker.GetReferenceOrientation(this, TrackerSettings.BindingMode, up);
                        // dir /= distance;
                        // var localDir = orient * dir;
                        // var r = UnityVectorExtensions.SafeFromToRotation(Vector3.back, localDir, up).eulerAngles;
                        // // VerticalAxis.Value = TrackerSettings.BindingMode == BindingMode.SimpleFollowWithWorldUp ? 0 : r.x;
                        // HorizontalAxis.Value = r.y; // only works for WorldSpace
                    }
                        break;
                    case OrbitStyles.ThreeRing:
                    {
                        var up = VirtualCamera.State.ReferenceUp;
                        HorizontalAxis.Value = GetHorizontalAxis(pos, targetPos, up, HorizontalAxis);
                        VerticalAxis.Value = GetVerticalAxisClosestValue(pos, targetPos, up, VerticalAxis);
                    }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                
            }
            //RadialAxis.Value = distance / Radius;
        }
        
        float GetHorizontalAxis(Vector3 camPos, Vector3 targetPos, Vector3 up, InputAxis horizontalAxis)
        {
            Quaternion orient = m_TargetTracker.GetReferenceOrientation(this, TrackerSettings.BindingMode, up);
            Vector3 fwd = (orient * Vector3.back).ProjectOntoPlane(up);
            if (!fwd.AlmostZero())
            {
                // Find angle delta between current position (a) and new position (b)
                Vector3 b = (camPos - targetPos).ProjectOntoPlane(up);
                return UnityVectorExtensions.SignedAngle(fwd, b, up);
            }
            return horizontalAxis.Value; // Can't calculate, stay conservative
        }
        float GetVerticalAxisClosestValue(Vector3 camPos, Vector3 targetPos, Vector3 up, InputAxis verticalAxis)
        {
            if (FollowTarget != null)
            {
                // Rotate the camera pos to the back
                Quaternion q = UnityVectorExtensions.SafeFromToRotation(up, Vector3.up, up);
                Vector3 normalizedDirection = (q * (camPos - targetPos)).normalized;
                Vector3 flatDir = normalizedDirection; flatDir.y = 0;
                if (!flatDir.AlmostZero())
                {
                    float angle = UnityVectorExtensions.SignedAngle(flatDir, Vector3.back, Vector3.up);
                    normalizedDirection = Quaternion.AngleAxis(angle, Vector3.up) * normalizedDirection;
                }
                normalizedDirection.x = 0;

                // We need to find the minimum of the angle function using steepest descent
                var x = SteepestDescent(normalizedDirection * (camPos - targetPos).magnitude);
                return x <= 0.5f
                    ? Mathf.Lerp(verticalAxis.Range.x, verticalAxis.Center, MapTo01(x, 0f, 0.5f))  // [0, 0.5] -> [0, 1] -> [Range.x, Center]
                    : Mathf.Lerp(verticalAxis.Center, verticalAxis.Range.y, MapTo01(x, 0.5f, 1f)); // [0.5, 1] -> [0, 1] -> [Center, Range.Y]
            }
            return VerticalAxis.Value; // stay conservative
            
            // local functions
            float SteepestDescent(Vector3 cameraOffset)
            {
                const int maxIteration = 10;
                const float epsilon = 0.00005f;
                var x = InitialGuess(cameraOffset);
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
                // initial guess based on closest line (approximating spline) to point 
                float InitialGuess(Vector3 cameraPosInRigSpace)
                {
                    if (m_OrbitCache.SettingsChanged(Orbits))
                        m_OrbitCache.UpdateOrbitCache(Orbits);
                    
                    var pb = m_OrbitCache.SplineValue(0f); // point at the bottom of spline
                    var pm = m_OrbitCache.SplineValue(0.5f); // point in the middle of spline
                    var pt = m_OrbitCache.SplineValue(1f); // point at the top of spline
                    var t1 = cameraPosInRigSpace.ClosestPointOnSegment(pb, pm);
                    var d1 = Vector3.SqrMagnitude(Vector3.Lerp(pb, pm, t1) - cameraPosInRigSpace);
                    var t2 = cameraPosInRigSpace.ClosestPointOnSegment(pm, pt);
                    var d2 = Vector3.SqrMagnitude(Vector3.Lerp(pm, pt, t2) - cameraPosInRigSpace);

                    // [0,0.5] represent bottom to mid, and [0.5,1] represents mid to top
                    return d1 < d2 ? Mathf.Lerp(0f, 0.5f, t1) : Mathf.Lerp(0.5f, 1f, t2); // represents mid to top
                }
            }
            
            static float MapTo01(float valueToMap, float fMin, float fMax) => (valueToMap - fMin) / (fMax - fMin);
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

            if (deltaTime < 0 || !VirtualCamera.PreviousStateIsValid || !CinemachineCore.Instance.IsLive(VirtualCamera))
                m_ResetHandler?.Invoke();

            Vector3 offset = GetCameraPoint();
            if (TrackerSettings.BindingMode != BindingMode.SimpleFollowWithWorldUp)
                HorizontalAxis.Restrictions 
                    &= ~(InputAxis.RestrictionFlags.NoRecentering | InputAxis.RestrictionFlags.RangeIsDriven);
            else
            {
                HorizontalAxis.Value = 0;
                HorizontalAxis.Restrictions 
                    |= InputAxis.RestrictionFlags.NoRecentering | InputAxis.RestrictionFlags.RangeIsDriven;
            }
            m_TargetTracker.TrackTarget(
                this, deltaTime, curState.ReferenceUp, offset, TrackerSettings,
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
                var dir0 = m_TargetTracker.PreviousTargetPosition + m_PreviousWorldOffset - lookAt;
                var dir1 = curState.RawPosition - lookAt;
                if (dir0.sqrMagnitude > 0.01f && dir1.sqrMagnitude > 0.01f)
                    curState.PositionDampingBypass = UnityVectorExtensions.SafeFromToRotation(
                        dir0, dir1, curState.ReferenceUp).eulerAngles;
            }
            m_PreviousWorldOffset = offset;
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
            [RangeSlider(0f, 1f)]
            public float SplineCurvature;

            /// <summary>Default orbit rig</summary>
            public static Settings Default => new Settings
            { 
                SplineCurvature = 0.5f,
                Top = new Orbit { Height = 10, Radius = 4 },
                Center = new Orbit { Height = 2.5f, Radius = 8 },
                Bottom= new Orbit { Height = -0.5f, Radius = 5 }
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
