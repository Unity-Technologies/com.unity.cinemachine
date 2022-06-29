using UnityEngine;
using System;
using System.Linq;
using Cinemachine.Utility;
using UnityEngine.Splines;

namespace Cinemachine
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
    [AddComponentMenu("")] // Don't display in add component menu
    [SaveDuringPlay]
    [CameraPipeline(CinemachineCore.Stage.Body)]
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

        /// <summary>Where to put the camera relative to the spline postion.  X is perpendicular 
        /// to the spline, Y is up, and Z is parallel to the spline.</summary>
        [Tooltip("Where to put the camera relative to the spline position.  X is perpendicular "
            + "to the spline, Y is up, and Z is parallel to the spline.")]
        public Vector3 SplineOffset = Vector3.zero;

        /// <summary>How to set the virtual camera's Up vector.  This will affect the screen composition.</summary>
        [Tooltip("How to set the virtual camera's Up vector.  This will affect the screen composition, because "
            + "the camera Aim behaviours will always try to respect the Up direction.")]
        public CameraUpMode CameraUp = CameraUpMode.Default;
        
        /// <summary>Different ways to set the camera's up vector</summary>
        public enum CameraUpMode
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
            /// This is only used if the Camera Up is not Default.</summary>
            [RangeSlider(0f, 20f)]
            [Tooltip("How aggressively the camera tries to maintain the desired rotation.  "
                + "This is only used if the Camera Up is not Default.")]
            public float Angular;
        }

        /// <summary>Settings for controlling damping, which causes the camera to 
        /// move gradually towards the desired spline position</summary>
        [FoldoutWithEnabledButton]
        [Tooltip("Settings for controlling damping, which causes the camera to "
            + "move gradually towards the desired spline position")]
        public DampingSettings Damping;
        
        /// <summary>Controls how automatic dollying occurs</summary>
        [Serializable]
        public struct AutoDolly
        {
            /// <summary>If checked, will enable automatic dolly, which chooses a spline position
            /// that is as close as possible to the Follow target.</summary>
            [Tooltip("If checked, will enable automatic dolly, which chooses a spline position that is as "
                + "close as possible to the Follow target.  Note: this can have significant performance impact")]
            public bool Enabled;

            /// <summary>
            /// Offset, in current position units, from the closest point on the spline to the follow target.
            /// </summary>
            [Tooltip("Offset, in current position units, from the closest point on the spline to the follow target")]
            public float PositionOffset;

            /// <summary>
            /// Affects how many segments to split a spline into when calculating the nearest point.
            /// Higher values mean smaller and more segments, which increases accuracy at the cost of 
            /// processing time.  In most cases, the default resolution is appropriate. Use 
            /// with <seealso cref="SearchIteration"/> to fine-tune point accuracy.
            /// For more information, see SplineUtility.GetNearestPoint.
            /// </summary>
            [HideInInspector]
            [Tooltip("Affects how many segments to split a spline into when calculating the nearest point.  " 
                + "Higher values mean smaller and more segments, which increases accuracy at the cost of "
                + "processing time.  In most cases, the default value (4) is appropriate. Use with SearchIteration "
                + "to fine-tune point accuracy.")]
            public int SearchResolution;

            /// <summary>
            /// The nearest point is calculated by finding the nearest point on the entire length
            /// of the spline using <seealso cref="SearchResolution"/> to divide into equally spaced line segments. 
            /// Successive iterations will then subdivide further the nearest segment, producing more 
            /// accurate results. In most cases, the default value is sufficient.
            /// For more information, see SplineUtility.GetNearestPoint.
            /// </summary>
            [HideInInspector]
            [Tooltip("The nearest point is calculated by finding the nearest point on the entire "
                + "length of the spline using SearchResolution to divide into equally spaced line segments. "
                + "Successive iterations will then subdivide further the nearest segment, producing more "
                + "accurate results. In most cases, the default value (2) is sufficient.")]
            public int SearchIteration;

            internal static AutoDolly Default => new AutoDolly
            {
                Enabled = false, 
                PositionOffset = 0,
                SearchIteration = 4, 
                SearchResolution = 2,
            };
        }

        /// <summary>Controls how automatic dollying occurs</summary>
        [FoldoutWithEnabledButton]
        [Tooltip("Controls how automatic dollying occurs.  A tracking target is necessary to use this feature.")]
        public AutoDolly AutomaticDolly = AutoDolly.Default;

        // State info for damping
        float m_PreviousNormalizedSplinePosition = 0;
        Quaternion m_PreviousOrientation = Quaternion.identity;
        Vector3 m_PreviousCameraPosition = Vector3.zero;

        CinemachineSplineRoll m_RollCache; // don't use this directly - use SplineRoll

        SplineContainer m_SplineCache;
        float m_SplineLengthCache;

        void OnValidate()
        {
            Damping.Position.x = Mathf.Clamp(Damping.Position.x, 0, 20);
            Damping.Position.y = Mathf.Clamp(Damping.Position.y, 0, 20);
            Damping.Position.z = Mathf.Clamp(Damping.Position.z, 0, 20);
            Damping.Angular = Mathf.Clamp(Damping.Angular, 0, 20);
        }

        void Reset()
        {
            Spline = null;
            CameraPosition = 0;
            PositionUnits = PathIndexUnit.Normalized;
            SplineOffset = Vector3.zero;
            CameraUp = CameraUpMode.Default;
            Damping = default;
            AutomaticDolly = AutoDolly.Default;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            RefreshRollCache();
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

            UpdateSplineCache();
            CameraPosition = splinePath.StandardizeSplinePosition(CameraPosition, PositionUnits, m_SplineLengthCache);

            // splines work with normalized position by default, so we convert m_SplinePosition to normalized at the start
            var normalizedSplinePosition = 
                splinePath.ConvertIndexUnit(CameraPosition, PositionUnits, PathIndexUnit.Normalized);
            
            // Init previous frame state info
            if (deltaTime < 0 || !VirtualCamera.PreviousStateIsValid)
            {
                m_PreviousNormalizedSplinePosition = normalizedSplinePosition;
                m_PreviousCameraPosition = curState.RawPosition;
                m_PreviousOrientation = curState.RawOrientation;
                RefreshRollCache();
            }

            // Get the new ideal spline base position
            if (AutomaticDolly.Enabled && FollowTarget != null)
            {
                // convert follow target into spline local space, because SplineUtility works in spline local space
                SplineUtility.GetNearestPoint(splinePath, 
                    Spline.transform.InverseTransformPoint(FollowTargetPosition), out _, out normalizedSplinePosition, 
                    AutomaticDolly.SearchResolution, AutomaticDolly.SearchIteration);

                // GML hack because SplineUtility.GetNearestPoint is buggy
                normalizedSplinePosition = Mathf.Clamp01(normalizedSplinePosition);

                CameraPosition = splinePath.ConvertIndexUnit(normalizedSplinePosition, 
                    PathIndexUnit.Normalized, PositionUnits);
                
                // Apply the spline position offset
                normalizedSplinePosition += splinePath.ConvertIndexUnit(AutomaticDolly.PositionOffset, 
                    PositionUnits, PathIndexUnit.Normalized);
            }

            if (deltaTime >= 0 && VirtualCamera.PreviousStateIsValid)
            {
                const float maxNormalizedValue = 1f;
                float prev = m_PreviousNormalizedSplinePosition;
                float next = normalizedSplinePosition;

                if (splinePath.Closed && Mathf.Abs(next - prev) > maxNormalizedValue / 2f)
                {
                    if (next > prev)
                        prev += maxNormalizedValue;
                    else
                        prev -= maxNormalizedValue;
                }

                m_PreviousNormalizedSplinePosition = prev;
                normalizedSplinePosition = next;

                // Apply damping in the spline direction
                if (Damping.Enabled && deltaTime >= 0)
                {
                    float offset = m_PreviousNormalizedSplinePosition - normalizedSplinePosition;
                    offset = Damper.Damp(offset, Damping.Position.z, deltaTime);
                    normalizedSplinePosition = m_PreviousNormalizedSplinePosition - offset;
                }
            }
            m_PreviousNormalizedSplinePosition = normalizedSplinePosition;

            Spline.EvaluateSplineWithRoll(
                SplineRoll, normalizedSplinePosition, out var newCameraPos, out var newSplineOrientation);

            // Apply the offset to get the new camera position
            var offsetX = newSplineOrientation * Vector3.right;
            var offsetY = newSplineOrientation * Vector3.up;
            var offsetZ = newSplineOrientation * Vector3.forward;
            newCameraPos += SplineOffset.x * offsetX;
            newCameraPos += SplineOffset.y * offsetY;
            newCameraPos += SplineOffset.z * offsetZ;

            // Apply damping to the remaining directions
            if (Damping.Enabled && deltaTime >= 0 && VirtualCamera.PreviousStateIsValid)
            {
                Vector3 currentCameraPos = m_PreviousCameraPosition;
                Vector3 delta = (currentCameraPos - newCameraPos);
                Vector3 delta1 = Vector3.Dot(delta, offsetY) * offsetY;
                Vector3 delta0 = delta - delta1;

                delta0 = Damper.Damp(delta0, Damping.Position.x, deltaTime);
                delta1 = Damper.Damp(delta1, Damping.Position.y, deltaTime);
                newCameraPos = currentCameraPos - (delta0 + delta1);
            }
            curState.RawPosition = m_PreviousCameraPosition = newCameraPos;

            // Set the orientation and up
            Quaternion newOrientation = GetCameraOrientationAtSplinePoint(newSplineOrientation, curState.ReferenceUp);
            if (Damping.Enabled && deltaTime >= 0 && VirtualCamera.PreviousStateIsValid)
            {
                float t = VirtualCamera.DetachedFollowTargetDamp(1, Damping.Angular, deltaTime);
                newOrientation = Quaternion.Slerp(m_PreviousOrientation, newOrientation, t);
            }
            m_PreviousOrientation = newOrientation;
            curState.RawOrientation = newOrientation;

            if (CameraUp != CameraUpMode.Default)
                curState.ReferenceUp = curState.RawOrientation * Vector3.up;
        }

        Quaternion GetCameraOrientationAtSplinePoint(Quaternion splineOrientation, Vector3 up)
        {
            switch (CameraUp)
            {
                default:
                case CameraUpMode.Default: break;
                case CameraUpMode.Spline: return splineOrientation;
                case CameraUpMode.SplineNoRoll:
                    return Quaternion.LookRotation(splineOrientation * Vector3.forward, up);
                case CameraUpMode.FollowTarget:
                    if (FollowTarget != null)
                        return FollowTargetRotation;
                    break;
                case CameraUpMode.FollowTargetNoRoll:
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
                m_RollCache.SplineContainer = Spline; 
#endif
            // check if our spline has CinemachineSplineRoll
            if (Spline != null && m_RollCache == null)
                Spline.TryGetComponent(out m_RollCache);
        }

        void UpdateSplineCache()
        {
            if (m_SplineCache != Spline)
            {
                if (m_SplineCache != null && m_SplineCache.Spline != null)
                    m_SplineCache.Spline.changed -= UpdateLengthCache;

                m_SplineLengthCache = 0;
                if (Spline != null && Spline.Spline != null)
                {
                    Spline.Spline.changed -= UpdateLengthCache;
                    Spline.Spline.changed += UpdateLengthCache;
                    UpdateLengthCache();
                }
                m_SplineCache = Spline;
            }
            // Local funtion to cache length
            void UpdateLengthCache() => m_SplineLengthCache = Spline.Spline.GetLength(); 
        }
    }
}
