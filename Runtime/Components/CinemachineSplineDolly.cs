#if CINEMACHINE_UNITY_SPLINES
using UnityEngine;
using System;
using Cinemachine.Utility;
using UnityEngine.Splines;

namespace Cinemachine
{
    /// <summary>
    /// A Cinemachine Virtual Camera Body component that constrains camera motion
    /// to a Spline.  The camera can move along the path.
    ///
    /// This behaviour can operate in two modes: manual positioning, and Auto-Dolly positioning.
    /// In Manual mode, the camera's position is specified by animating the Path Position field.
    /// In Auto-Dolly mode, the Path Position field is animated automatically every frame by finding
    /// the position on the path that's closest to the virtual camera's Follow target.
    /// </summary>
    [DocumentationSorting(DocumentationSortingAttribute.Level.UserRef)]
    [AddComponentMenu("")] // Don't display in add component menu
    [SaveDuringPlay]
    public class CinemachineSplineDolly : CinemachineComponentBase
    {
        /// <summary>The Spline container to which the camera will be constrained.  This must be non-null.</summary>
        [Tooltip("The Spline container to which the camera will be constrained.  This must be non-null.")]
        public SplineContainer m_Track;

        /// <summary>The position along the path at which the camera will be placed. This can be animated directly,
        /// or set automatically by the Auto-Dolly feature to get as close as possible to the Follow target.
        /// The value is interpreted according to the Position Units setting.</summary>
        [Tooltip("The position along the path at which the camera will be placed.  "
           + "This can be animated directly, or set automatically by the Auto-Dolly feature to "
            + "get as close as possible to the Follow target.  The value is interpreted "
            + "according to the Position Units setting.")]
        public float m_PathPosition;

        /// <summary>How to interpret the Path Position</summary>
        [Tooltip("How to interpret the Path Position:\n"+
            "- Distance: Values range from 0 (start of Spline) to Length of the Spline (end of Spline).\n"+
            "- Normalized: Values range from 0 (start of Spline) to 1 (end of Spline).\n"+
            "- Knot: Values are defined by knot indices and a fractional value representing the"+
            "normalized interpolation between the specific knot index and the next knot.\n")]
        public PathIndexUnit m_PositionUnits = PathIndexUnit.Normalized;

        /// <summary>Where to put the camera realtive to the path postion.  X is perpendicular 
        /// to the path, Y is up, and Z is parallel to the path.</summary>
        [Tooltip("Where to put the camera relative to the path position.  X is perpendicular "
            + "to the path, Y is up, and Z is parallel to the path.  This allows the camera to "
            + "be offset from the path itself (as if on a tripod, for example).")]
        public Vector3 m_PathOffset = Vector3.zero;

        /// <summary>How aggressively the camera tries to maintain the offset perpendicular to the path.
        /// Small numbers are more responsive, rapidly translating the camera to keep the target's
        /// x-axis offset.  Larger numbers give a more heavy slowly responding camera.
        /// Using different settings per axis can yield a wide range of camera behaviors</summary>
        [Range(0f, 20f)]
        [Tooltip("How aggressively the camera tries to maintain its position in a direction "
            + "perpendicular to the path.  Small numbers are more responsive, rapidly translating "
            + "the camera to keep the target's x-axis offset.  Larger numbers give a more heavy "
            + "slowly responding camera. Using different settings per axis can yield a wide range "
            + "of camera behaviors.")]
        public float m_XDamping = 0f;

        /// <summary>How aggressively the camera tries to maintain the offset in the path-local up direction.
        /// Small numbers are more responsive, rapidly translating the camera to keep the target's
        /// y-axis offset.  Larger numbers give a more heavy slowly responding camera.
        /// Using different settings per axis can yield a wide range of camera behaviors</summary>
        [Range(0f, 20f)]
        [Tooltip("How aggressively the camera tries to maintain its position in the path-local up direction.  "
            + "Small numbers are more responsive, rapidly translating the camera to keep the target's "
            + "y-axis offset.  Larger numbers give a more heavy slowly responding camera. Using different "
            + "settings per axis can yield a wide range of camera behaviors.")]
        public float m_YDamping = 0f;

        /// <summary>How aggressively the camera tries to maintain the offset parallel to the path.
        /// Small numbers are more responsive, rapidly translating the camera to keep the
        /// target's z-axis offset.  Larger numbers give a more heavy slowly responding camera.
        /// Using different settings per axis can yield a wide range of camera behaviors</summary>
        [Range(0f, 20f)]
        [Tooltip("How aggressively the camera tries to maintain its position in a direction parallel to the path.  "
            + "Small numbers are more responsive, rapidly translating the camera to keep the target's z-axis offset.  "
            + "Larger numbers give a more heavy slowly responding camera. Using different settings per axis "
            + "can yield a wide range of camera behaviors.")]
        public float m_ZDamping = 1f;

        /// <summary>Different ways to set the camera's up vector</summary>
        [DocumentationSorting(DocumentationSortingAttribute.Level.UserRef)]
        public enum CameraUpMode
        {
            /// <summary>Leave the camera's up vector alone.  It will be set according to the Brain's WorldUp.</summary>
            Default,
            /// <summary>Take the up vector from the path's up vector at the current point</summary>
            Path,
            /// <summary>Take the up vector from the path's up vector at the current point, but with the roll zeroed out</summary>
            PathNoRoll,
            /// <summary>Take the up vector from the Follow target's up vector</summary>
            FollowTarget,
            /// <summary>Take the up vector from the Follow target's up vector, but with the roll zeroed out</summary>
            FollowTargetNoRoll,
        };

        /// <summary>How to set the virtual camera's Up vector.  This will affect the screen composition.</summary>
        [Tooltip("How to set the virtual camera's Up vector.  This will affect the screen composition, because "
            + "the camera Aim behaviours will always try to respect the Up direction.")]
        public CameraUpMode m_CameraUp = CameraUpMode.Default;

        /// <summary>"How aggressively the camera tries to track the target rotation's X angle.
        /// Small numbers are more responsive.  Larger numbers give a more heavy slowly responding camera.</summary>
        [Range(0f, 20f)]
        [Tooltip("How aggressively the camera tries to track the target rotation's X angle.  Small numbers are "
            + "more responsive.  Larger numbers give a more heavy slowly responding camera.")]
        public float m_PitchDamping = 0;

        /// <summary>How aggressively the camera tries to track the target rotation's Y angle.
        /// Small numbers are more responsive.  Larger numbers give a more heavy slowly responding camera.</summary>
        [Range(0f, 20f)]
        [Tooltip("How aggressively the camera tries to track the target rotation's Y angle.  Small numbers are "
            + "more responsive.  Larger numbers give a more heavy slowly responding camera.")]
        public float m_YawDamping = 0;

        /// <summary>How aggressively the camera tries to track the target rotation's Z angle.
        /// Small numbers are more responsive.  Larger numbers give a more heavy slowly responding camera.</summary>
        [Range(0f, 20f)]
        [Tooltip("How aggressively the camera tries to track the target rotation's Z angle.  Small numbers "
            + "are more responsive.  Larger numbers give a more heavy slowly responding camera.")]
        public float m_RollDamping = 0f;

        /// <summary>Controls how automatic dollying occurs</summary>
        [DocumentationSorting(DocumentationSortingAttribute.Level.UserRef)]
        [Serializable]
        public struct AutoDolly
        {
            /// <summary>If checked, will enable automatic dolly, which chooses a path position
            /// that is as close as possible to the Follow target.</summary>
            [Tooltip("If checked, will enable automatic dolly, which chooses a path position that is as "
                + "close as possible to the Follow target.  Note: this can have significant performance impact")]
            public bool m_Enabled;

            /// <summary>Offset, in current position units, from the closest point on the path to the follow target.</summary>
            [Tooltip("Offset, in current position units, from the closest point on the path to the follow target")]
            public float m_PositionOffset;

            /// <summary>
            /// Affects how many segments to split a spline (Track) into when calculating the nearest point.
            /// Higher values mean smaller and more segments, which increases accuracy at the cost of processing time.
            /// In most cases, the default resolution is appropriate. Use with <seealso cref="m_SearchIteration"/> to fine tune point accuracy.
            /// For more information, see SplineUtility.GetNearestPoint.
            /// </summary>
            [Tooltip("Affects how many segments to split a spline (Track) into when calculating the nearest point." +
                "Higher values mean smaller and more segments, which increases accuracy at the cost of processing time. " +
                "In most cases, the default value (4) is appropriate. Use with SearchIteration to fine tune point accuracy.")]
            public int m_SearchResolution;

            /// <summary>
            /// The nearest point is calculated by finding the nearest point on the entire length
            /// of the spline using <seealso cref="m_SearchResolution"/> to divide into equally spaced line segments. Successive
            /// iterations will then subdivide further the nearest segment, producing more accurate results. In most cases,
            /// the default value is sufficient.
            /// For more information, see SplineUtility.GetNearestPoint.
            /// </summary>
            [Tooltip("The nearest point is calculated by finding the nearest point on the entire length of the spline " +
                "using SearchResolution to divide into equally spaced line segments. Successive iterations will then " +
                "subdivide further the nearest segment, producing more accurate results. In most cases, the default value (2) is sufficient.")]
            public int m_SearchIteration;

            /// <summary>Constructor with specific field values</summary>
            /// <param name="enabled">Whether to enable automatic dolly</param>
            /// <param name="positionOffset">Offset, in current position units, from the closest point on the path to the follow target</param>
            /// <param name="searchResolution">We search between waypoints by dividing the segment into this many straight pieces</param>
            /// <param name="searchIteration">Straight pieces defined by searchResolution are further subdivided.</param>
            public AutoDolly(bool enabled, float positionOffset, int searchResolution, int searchIteration)
            {
                m_Enabled = enabled;
                m_PositionOffset = positionOffset;
                m_SearchResolution = searchResolution;
                m_SearchIteration = searchIteration;
            }
        };

        /// <summary>Controls how automatic dollying occurs</summary>
        [Tooltip("Controls how automatic dollying occurs.  A Follow target is necessary to use this feature.")]
        public AutoDolly m_AutoDolly = new AutoDolly(false, 0, 4, 2);

        /// <summary>True if component is enabled and has a path</summary>
        public override bool IsValid { get { return enabled && m_Track != null; } }

        /// <summary>Get the Cinemachine Pipeline stage that this component implements.
        /// Always returns the Body stage</summary>
        public override CinemachineCore.Stage Stage { get { return CinemachineCore.Stage.Body; } }

        /// <summary>
        /// Report maximum damping time needed for this component.
        /// </summary>
        /// <returns>Highest damping setting in this component</returns>
        public override float GetMaxDampTime() 
        { 
            var d2 = AngularDamping;
            var a = Mathf.Max(m_XDamping, Mathf.Max(m_YDamping, m_ZDamping)); 
            var b = Mathf.Max(d2.x, Mathf.Max(d2.y, d2.z)); 
            return Mathf.Max(a, b); 
        }
        
        /// <summary>
        /// Subscribe to onSplineChanged if you'd like to react to changes to the Spline attached to this vcam.
        /// This action is invoked by the Spline's changed event when a spline property is modified.
        /// </summary>
        public event Action onSplineChanged;

        bool m_Registered = false;
        SplineContainer m_TrackCache;
        void OnValidate()
        {
            if (m_TrackCache != null)
            {
                m_TrackCache.Spline.changed -= onSplineChanged;
                m_TrackCache = m_Track;
                m_Registered = false;
            }
            if (!m_Registered && m_Track != null && m_Track.Spline != null)
            {
                m_Registered = true;
                m_TrackCache = m_Track;
                m_Track.Spline.changed += onSplineChanged;
            }
        }

        void UpdateOverrideCache()
        {
            m_RollOverrideExtension = null;
            // if vcam has an override, use that
            if (transform.parent.TryGetComponent(out m_RollOverrideExtension))
            {
#if UNITY_EDITOR
                m_RollOverrideExtension.splineContainer = m_Track; // this is needed by the RollHandle to work in the scene view
#endif
            }
            // else if the spline has an override, use that
            else if (m_Track.TryGetComponent(out m_RollOverrideExtension)) {}
        }

        CinemachineSplineRollOverrideExtension m_RollOverrideExtension; // don't use this directly
        CinemachineSplineRollOverrideExtension RollOverrideExtension
        {
            get
            {
                if(m_RollOverrideExtension == null)
                    UpdateOverrideCache();

                return m_RollOverrideExtension;
            }
        }
        /// <summary>Positions the virtual camera according to the transposer rules.</summary>
        /// <param name="curState">The current camera state</param>
        /// <param name="deltaTime">Used for damping.  If less that 0, no damping is done.</param>
        public override void MutateCameraState(ref CameraState curState, float deltaTime)
        {
            if (!IsValid)
            {
                m_PathPosition = 0;
                return;
            }

            // splines work with normalized position by default, so we convert m_PathPosition to normalized at the start
            var pathSpline = m_Track.Spline;
            if (pathSpline == null || pathSpline.Count == 0)
            {
                return;
            }
            if (pathSpline.Count == 1)
            {
                curState.RawPosition = m_PreviousCameraPosition = pathSpline[0].Position;
                curState.RawOrientation = m_PreviousOrientation = pathSpline[0].Rotation;
                return;
            }
            
            m_PathPosition = 
                SplineUtility.ConvertIndexUnit(pathSpline, m_PathPosition, m_PositionUnits, PathIndexUnit.Normalized);
            
            // Init previous frame state info
            if (deltaTime < 0 || !VirtualCamera.PreviousStateIsValid)
            {
                m_PreviousNormalizedPathPosition = m_PathPosition;
                m_PreviousCameraPosition = curState.RawPosition;
                m_PreviousOrientation = curState.RawOrientation;
            }

            // Get the new ideal path base position
            if (m_AutoDolly.m_Enabled && FollowTarget != null)
            {
                // convert follow target into spline local space, because SplineUtility works in spline local space
                SplineUtility.GetNearestPoint(pathSpline, 
                    m_Track.transform.InverseTransformPoint(FollowTargetPosition), out _, out m_PathPosition, 
                    m_AutoDolly.m_SearchResolution, m_AutoDolly.m_SearchIteration);
                // Apply the path position offset
                m_PathPosition += m_AutoDolly.m_PositionOffset;
            }
            float newPathPosition = m_PathPosition;

            if (deltaTime >= 0 && VirtualCamera.PreviousStateIsValid)
            {
                float maxUnit = 1; // we are always using normalized unit [0-1]
                {
                    float prev = m_PreviousNormalizedPathPosition;
                    float next = newPathPosition;
                    if (pathSpline.Closed && Mathf.Abs(next - prev) > maxUnit / 2)
                    {
                        if (next > prev)
                            prev += maxUnit;
                        else
                            prev -= maxUnit;
                    }
                    m_PreviousNormalizedPathPosition = prev;
                    newPathPosition = next;
                }

                // Apply damping along the path direction
                float offset = m_PreviousNormalizedPathPosition - newPathPosition;
                offset = Damper.Damp(offset, m_ZDamping, deltaTime);
                newPathPosition = m_PreviousNormalizedPathPosition - offset;
            }
            m_PreviousNormalizedPathPosition = newPathPosition;
            m_Track.Evaluate(newPathPosition, 
                out var localPosition, out var localTangent, out var localUp);
            Vector3 newCameraPos = localPosition;
            var newPathOrientation = 
                Vector3.SqrMagnitude(localTangent) == 0 || Vector3.SqrMagnitude(localUp) == 0 ? 
                    Quaternion.identity : Quaternion.LookRotation(localTangent, localUp);
            
            if (RollOverrideExtension != null && RollOverrideExtension.enabled)
            {
                float roll = RollOverrideExtension.RollOverride.Evaluate(pathSpline, newPathPosition, 
                    PathIndexUnit.Normalized, new UnityEngine.Splines.Interpolators.LerpFloat());
                var rollRotation = Quaternion.AngleAxis(-roll, localTangent);
                newPathOrientation = Quaternion.LookRotation(localTangent, rollRotation * localUp);
                //Debug.Log("Quaternion"+rollRotation+" Quaternion"+newPathOrientation);
            }

            // Apply the offset to get the new camera position
            var offsetX = newPathOrientation * Vector3.right;
            var offsetY = newPathOrientation * Vector3.up;
            var offsetZ = newPathOrientation * Vector3.forward;
            newCameraPos += m_PathOffset.x * offsetX;
            newCameraPos += m_PathOffset.y * offsetY;
            newCameraPos += m_PathOffset.z * offsetZ;

            // Apply damping to the remaining directions
            if (deltaTime >= 0 && VirtualCamera.PreviousStateIsValid)
            {
                Vector3 currentCameraPos = m_PreviousCameraPosition;
                Vector3 delta = (currentCameraPos - newCameraPos);
                Vector3 delta1 = Vector3.Dot(delta, offsetY) * offsetY;
                Vector3 delta0 = delta - delta1;
                delta0 = Damper.Damp(delta0, m_XDamping, deltaTime);
                delta1 = Damper.Damp(delta1, m_YDamping, deltaTime);
                newCameraPos = currentCameraPos - (delta0 + delta1);
            }
            curState.RawPosition = m_PreviousCameraPosition = newCameraPos;

            // Set the orientation and up
            Quaternion newOrientation
                = GetCameraOrientationAtPathPoint(newPathOrientation, curState.ReferenceUp);
            if (deltaTime >= 0 && VirtualCamera.PreviousStateIsValid)
            {
                Vector3 relative = (Quaternion.Inverse(m_PreviousOrientation)
                    * newOrientation).eulerAngles;
                for (int i = 0; i < 3; ++i)
                    if (relative[i] > 180)
                        relative[i] -= 360;
                relative = Damper.Damp(relative, AngularDamping, deltaTime);
                newOrientation = m_PreviousOrientation * Quaternion.Euler(relative);
            }
            m_PreviousOrientation = newOrientation;

            curState.RawOrientation = newOrientation;
            if (m_CameraUp != CameraUpMode.Default)
                curState.ReferenceUp = curState.RawOrientation * Vector3.up;
            
            // convert unit back to user's preference
            m_PathPosition = 
                SplineUtility.ConvertIndexUnit(pathSpline, m_PathPosition, PathIndexUnit.Normalized, m_PositionUnits);
        }

        Quaternion GetCameraOrientationAtPathPoint(Quaternion pathOrientation, Vector3 up)
        {
            switch (m_CameraUp)
            {
                default:
                case CameraUpMode.Default: break;
                case CameraUpMode.Path: return pathOrientation;
                case CameraUpMode.PathNoRoll:
                    return Quaternion.LookRotation(pathOrientation * Vector3.forward, up);
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

        Vector3 AngularDamping
        {
            get
            {
                switch (m_CameraUp)
                {
                    case CameraUpMode.PathNoRoll:
                    case CameraUpMode.FollowTargetNoRoll:
                        return new Vector3(m_PitchDamping, m_YawDamping, 0);
                    case CameraUpMode.Default:
                        return Vector3.zero;
                    default:
                        return new Vector3(m_PitchDamping, m_YawDamping, m_RollDamping);
                }
            }
        }

        float m_PreviousNormalizedPathPosition = 0;
        Quaternion m_PreviousOrientation = Quaternion.identity;
        Vector3 m_PreviousCameraPosition = Vector3.zero;
    }
}
#endif