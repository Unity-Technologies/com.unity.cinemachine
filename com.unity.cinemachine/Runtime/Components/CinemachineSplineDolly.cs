using UnityEngine;
using System;
using UnityEngine.Splines;
using UnityEngine.Serialization;

namespace Unity.Cinemachine
{
    /// <summary>
    /// A Cinemachine Camera Body component that constrains camera motion to a Spline.
    /// The camera can move along the spline.
    ///
    /// This behaviour can operate in two modes: manual positioning, and Auto-Dolly positioning.
    /// In Manual mode, the camera's position is specified by animating the Spline Position field.
    /// In Auto-Dolly mode, the Spline Position field is animated automatically every frame by finding
    /// the position on the spline that's closest to the camera's tracking target.
    /// </summary>
    [AddComponentMenu("Cinemachine/Procedural/Position Control/Cinemachine Spline Dolly")]
    [SaveDuringPlay]
    [CameraPipeline(CinemachineCore.Stage.Body)]
    [HelpURL(Documentation.BaseURL + "manual/CinemachineSplineDolly.html")]
    public class CinemachineSplineDolly : CinemachineComponentBase
    {
        /// <summary>The Spline container to which the camera will be constrained.</summary>
        [Tooltip("The Spline container to which the camera will be constrained.")]
        public SplineContainer Spline;

        /// <summary>The position along the spline at which the camera will be placed. This can be animated directly,
        /// or set automatically by the Auto-Dolly feature to get as close as possible to the Follow target.
        /// The value is interpreted according to the Position Units setting.</summary>
        [Tooltip("The position along the spline at which the camera will be placed.  "
            + "This can be animated directly, or set automatically by the Auto-Dolly feature to "
            + "get as close as possible to the Follow target.  The value is interpreted "
            + "according to the Position Units setting.")]
        public float CameraPosition;

        /// <summary>How to interpret the Spline Position:
        /// - Distance: Values range from 0 (start of Spline) to Length of the Spline (end of Spline).
        /// - Normalized: Values range from 0 (start of Spline) to 1 (end of Spline).
        /// - Knot: Values are defined by knot indices and a fractional value representing the normalized
        /// interpolation between the specific knot index and the next knot."</summary>
        [Tooltip("How to interpret the Spline Position:\n"
            + "- Distance: Values range from 0 (start of Spline) to Length of the Spline (end of Spline).\n"
            + "- Normalized: Values range from 0 (start of Spline) to 1 (end of Spline).\n"
            + "- Knot: Values are defined by knot indices and a fractional value representing the normalized " 
            + "interpolation between the specific knot index and the next knot.\n")]
        public PathIndexUnit PositionUnits = PathIndexUnit.Normalized;

        /// <summary>Where to put the camera relative to the spline position.  X is perpendicular 
        /// to the spline, Y is up, and Z is parallel to the spline.</summary>
        [Tooltip("Where to put the camera relative to the spline position.  X is perpendicular "
            + "to the spline, Y is up, and Z is parallel to the spline.")]
        public Vector3 SplineOffset = Vector3.zero;

        /// <summary>How to set the camera's rotation and Up.  This will affect the screen composition.</summary>
        [Tooltip("How to set the camera's rotation and Up.  This will affect the screen composition, because "
            + "the camera Aim behaviours will always try to respect the Up direction.")]
        [FormerlySerializedAs("CameraUp")]
        public RotationMode CameraRotation = RotationMode.Default;
        
        /// <summary>Different ways to set the camera's up vector</summary>
        public enum RotationMode
        {
            /// <summary>Leave the camera's up vector alone.  It will be set according to the Brain's WorldUp.</summary>
            Default,
            /// <summary>Take the up vector from the spline's up vector at the current point</summary>
            Spline,
            /// <summary>Take the up vector from the spline's up vector at the current point, but with the roll zeroed out</summary>
            SplineNoRoll,
            /// <summary>Take the up vector from the Follow target's up vector</summary>
            FollowTarget,
            /// <summary>Take the up vector from the Follow target's up vector, but with the roll zeroed out</summary>
            FollowTargetNoRoll,
        };

        /// <summary>Settings for controlling damping</summary>
        [Serializable]
        public struct DampingSettings
        {
            /// <summary>Enables damping, which causes the camera to move gradually towards 
            /// the desired spline position.</summary>
            [Tooltip("Enables damping, which causes the camera to move gradually towards the desired spline position")]
            public bool Enabled;

            /// <summary>How aggressively the camera tries to maintain the offset along
            /// the x, y, or z directions in spline local space.
            /// Meaning:
            /// - x represents the axis that is perpendicular to the spline. Use this to smooth out
            /// imperfections in the path. This may move the camera off the spline.
            /// - y represents the axis that is defined by the spline-local up direction. Use this to smooth out
            /// imperfections in the path. This may move the camera off the spline.
            /// - z represents the axis that is parallel to the spline. This won't move the camera off the spline.
            /// Smaller numbers are more responsive. Larger numbers give a heavier more slowly responding camera.
            /// Using different settings per axis can yield a wide range of camera behaviors.</summary>
            [Tooltip("How aggressively the camera tries to maintain the offset along the " 
                + "x, y, or z directions in spline local space. \n" 
                + "- x represents the axis that is perpendicular to the spline. Use this to smooth out " 
                + "imperfections in the path. This may move the camera off the spline.\n" 
                + "- y represents the axis that is defined by the spline-local up direction. Use this to smooth out " 
                + "imperfections in the path. This may move the camera off the spline.\n" 
                + "- z represents the axis that is parallel to the spline. This won't move the camera off the spline.\n\n" 
                + "Smaller numbers are more responsive, larger numbers give a heavier more slowly responding camera. " 
                + "Using different settings per axis can yield a wide range of camera behaviors.")]
            public Vector3 Position;

            /// <summary>How aggressively the camera tries to maintain the desired rotation.
            /// This is only used if Camera Rotation is not Default.</summary>
            [RangeSlider(0f, 20f)]
            [Tooltip("How aggressively the camera tries to maintain the desired rotation.  "
                + "This is only used if Camera Rotation is not Default.")]
            public float Angular;
        }

        /// <summary>Settings for controlling damping, which causes the camera to 
        /// move gradually towards the desired spline position</summary>
        [FoldoutWithEnabledButton]
        [Tooltip("Settings for controlling damping, which causes the camera to "
            + "move gradually towards the desired spline position")]
        public DampingSettings Damping;
        
        /// <summary>Controls how automatic dolly occurs</summary>
        [NoSaveDuringPlay]
        [FoldoutWithEnabledButton]
        [Tooltip("Controls how automatic dolly occurs.  A tracking target may be necessary to use this feature.")]
        public SplineAutoDolly AutomaticDolly;

        // State info for damping
        float m_PreviousSplinePosition;
        Quaternion m_PreviousRotation;
        Vector3 m_PreviousPosition;

        CinemachineSplineRoll m_RollCache; // don't use this directly - use SplineRoll

        void OnValidate()
        {
            Damping.Position.x = Mathf.Clamp(Damping.Position.x, 0, 20);
            Damping.Position.y = Mathf.Clamp(Damping.Position.y, 0, 20);
            Damping.Position.z = Mathf.Clamp(Damping.Position.z, 0, 20);
            Damping.Angular = Mathf.Clamp(Damping.Angular, 0, 20);
            if (AutomaticDolly.Method != null)
                AutomaticDolly.Method.Validate();
        }

        void Reset()
        {
            Spline = null;
            CameraPosition = 0;
            PositionUnits = PathIndexUnit.Normalized;
            SplineOffset = Vector3.zero;
            CameraRotation = RotationMode.Default;
            Damping = default;
            AutomaticDolly.Method = null;
        }

        /// <summary>Called when the behaviour is enabled.</summary>
        protected override void OnEnable()
        {
            base.OnEnable();
            RefreshRollCache();
            if (AutomaticDolly.Method != null)
                AutomaticDolly.Method.Reset();
        }

        /// <summary>True if component is enabled and has a spline</summary>
        public override bool IsValid => enabled && Spline != null;

        /// <summary>Get the Cinemachine Pipeline stage that this component implements.
        /// Always returns the Body stage</summary>
        public override CinemachineCore.Stage Stage => CinemachineCore.Stage.Body;

        /// <summary>
        /// Report maximum damping time needed for this component.
        /// </summary>
        /// <returns>Highest damping setting in this component</returns>
        public override float GetMaxDampTime() => !Damping.Enabled ? 0 :
            Mathf.Max(Mathf.Max(Damping.Position.x, Mathf.Max(Damping.Position.y, Damping.Position.z)), Damping.Angular);

        /// <summary>Positions the virtual camera according to the transposer rules.</summary>
        /// <param name="curState">The current camera state</param>
        /// <param name="deltaTime">Used for damping.  If less that 0, no damping is done.</param>
        public override void MutateCameraState(ref CameraState curState, float deltaTime)
        {
            if (!IsValid)
                return;

            var splinePath = Spline.Spline;
            if (splinePath == null || splinePath.Count == 0)
                return;
            
            var pathLength = splinePath.GetLength();
            var splinePos = splinePath.StandardizePosition(CameraPosition, PositionUnits, pathLength);

            // Init previous frame state info
            if (deltaTime < 0 || !VirtualCamera.PreviousStateIsValid)
            {
                m_PreviousSplinePosition = splinePos;
                m_PreviousPosition = curState.RawPosition;
                m_PreviousRotation = curState.RawOrientation;
                RefreshRollCache();
            }

            // Invoke AutoDolly algorithm to get new desired spline position
            if (AutomaticDolly.Enabled && AutomaticDolly.Method != null)
                splinePos = AutomaticDolly.Method.GetSplinePosition(
                    this, FollowTarget, Spline, splinePos, PositionUnits, deltaTime);

            // Apply damping in the spline direction
            if (Damping.Enabled && deltaTime >= 0 && VirtualCamera.PreviousStateIsValid)
            {
                // If spline is closed, we choose shortest path for damping
                var max = splinePath.GetMaxPosition(PositionUnits, pathLength);
                var prev = m_PreviousSplinePosition;
                if (splinePath.Closed && Mathf.Abs(splinePos - prev) > max * 0.5f)
                    prev += (splinePos > prev) ? max : -max;

                // Do the damping
                splinePos = prev + Damper.Damp(splinePos - prev, Damping.Position.z, deltaTime);
            }
            m_PreviousSplinePosition = CameraPosition = splinePos;

            Spline.EvaluateSplineWithRoll(
                SplineRoll, m_PreviousRotation, 
                splinePath.ConvertIndexUnit(splinePos, PositionUnits, PathIndexUnit.Normalized), 
                out var newPos, out var newSplineRotation);

            // Apply the offset to get the new camera position
            var offsetX = newSplineRotation * Vector3.right;
            var offsetY = newSplineRotation * Vector3.up;
            var offsetZ = newSplineRotation * Vector3.forward;
            newPos += SplineOffset.x * offsetX;
            newPos += SplineOffset.y * offsetY;
            newPos += SplineOffset.z * offsetZ;

            // Apply damping to the remaining directions
            if (Damping.Enabled && deltaTime >= 0 && VirtualCamera.PreviousStateIsValid)
            {
                var currentCameraPos = m_PreviousPosition;
                var delta = (currentCameraPos - newPos);
                var delta1 = Vector3.Dot(delta, offsetY) * offsetY;
                var delta0 = delta - delta1;

                delta0 = Damper.Damp(delta0, Damping.Position.x, deltaTime);
                delta1 = Damper.Damp(delta1, Damping.Position.y, deltaTime);
                newPos = currentCameraPos - (delta0 + delta1);
            }
            curState.RawPosition = m_PreviousPosition = newPos;

            // Set the orientation and up
            var newRot = GetCameraRotationAtSplinePoint(newSplineRotation, curState.ReferenceUp);
            if (Damping.Enabled && deltaTime >= 0 && VirtualCamera.PreviousStateIsValid)
            {
                float t = VirtualCamera.DetachedFollowTargetDamp(1, Damping.Angular, deltaTime);
                newRot = Quaternion.Slerp(m_PreviousRotation, newRot, t);
            }
            m_PreviousRotation = newRot;
            curState.RawOrientation = newRot;

            if (CameraRotation != RotationMode.Default)
                curState.ReferenceUp = curState.RawOrientation * Vector3.up;
        }

        Quaternion GetCameraRotationAtSplinePoint(Quaternion splineOrientation, Vector3 up)
        {
            switch (CameraRotation)
            {
                default:
                case RotationMode.Default: break;
                case RotationMode.Spline: return splineOrientation;
                case RotationMode.SplineNoRoll:
                    return Quaternion.LookRotation(splineOrientation * Vector3.forward, up);
                case RotationMode.FollowTarget:
                    if (FollowTarget != null)
                        return FollowTargetRotation;
                    break;
                case RotationMode.FollowTargetNoRoll:
                    if (FollowTarget != null)
                        return Quaternion.LookRotation(FollowTargetRotation * Vector3.forward, up);
                    break;
            }
            return Quaternion.LookRotation(VirtualCamera.transform.rotation * Vector3.forward, up);
        }

        CinemachineSplineRoll SplineRoll
        {
            get
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    RefreshRollCache();
#endif
                return m_RollCache;
            }
        }
        
        void RefreshRollCache()
        {
            // check if we have CinemachineSplineRoll
            TryGetComponent(out m_RollCache);
#if UNITY_EDITOR
            // need to tell CinemachineSplineRoll about its spline for gizmo drawing purposes
            if (m_RollCache != null)
                m_RollCache.Container = Spline; 
#endif
            // check if our spline has CinemachineSplineRoll
            if (Spline != null && m_RollCache == null)
                Spline.TryGetComponent(out m_RollCache);
        }
    }
}
