#if CINEMACHINE_UNITY_SPLINES
using UnityEngine;
using System;
using System.Linq;
using Cinemachine.Utility;
using UnityEngine.Splines;

namespace Cinemachine
{
    /// <summary>
    /// A Cinemachine Virtual Camera Body component that constrains camera motion to a Spline.
    /// The camera can move along the spline.
    ///
    /// This behaviour can operate in two modes: manual positioning, and Auto-Dolly positioning.
    /// In Manual mode, the camera's position is specified by animating the Spline Position field.
    /// In Auto-Dolly mode, the Spline Position field is animated automatically every frame by finding
    /// the position on the spline that's closest to the virtual camera's Follow target.
    /// </summary>
    [AddComponentMenu("")] // Don't display in add component menu
    [SaveDuringPlay]
    [CameraPipelineAttribute(CinemachineCore.Stage.Body)]
    public class CinemachineSplineDolly : CinemachineComponentBase
    {
        /// <summary>The Spline container to which the camera will be constrained.  This must be non-null.</summary>
        [Tooltip("The Spline container to which the camera will be constrained.  This must be non-null."), SerializeField]
        internal SplineContainer m_Spline;

        /// <summary>The position along the spline at which the camera will be placed. This can be animated directly,
        /// or set automatically by the Auto-Dolly feature to get as close as possible to the Follow target.
        /// The value is interpreted according to the Position Units setting.</summary>
        [Tooltip("The position along the spline at which the camera will be placed.  "
           + "This can be animated directly, or set automatically by the Auto-Dolly feature to "
            + "get as close as possible to the Follow target.  The value is interpreted "
            + "according to the Position Units setting.")]
        public float m_CameraPosition;

        /// <summary>How to interpret the Spline Position:
        /// - Distance: Values range from 0 (start of Spline) to Length of the Spline (end of Spline).
        /// - Normalized: Values range from 0 (start of Spline) to 1 (end of Spline).
        /// - Knot: Values are defined by knot indices and a fractional value representing the normalized
        /// interpolation between the specific knot index and the next knot."</summary>
        [Tooltip("How to interpret the Spline Position:\n"+
            "- Distance: Values range from 0 (start of Spline) to Length of the Spline (end of Spline).\n"+
            "- Normalized: Values range from 0 (start of Spline) to 1 (end of Spline).\n"+
            "- Knot: Values are defined by knot indices and a fractional value representing the normalized " +
            "interpolation between the specific knot index and the next knot.\n")]
        public PathIndexUnit m_PositionUnits = PathIndexUnit.Normalized;

        /// <summary>Where to put the camera relative to the spline postion.  X is perpendicular 
        /// to the spline, Y is up, and Z is parallel to the spline.</summary>
        [Tooltip("Where to put the camera relative to the spline position.  X is perpendicular "
            + "to the spline, Y is up, and Z is parallel to the spline.")]
        public Vector3 m_SplineOffset = Vector3.zero;

        /// <summary>How to set the virtual camera's Up vector.  This will affect the screen composition.</summary>
        [Tooltip("How to set the virtual camera's Up vector.  This will affect the screen composition, because "
            + "the camera Aim behaviours will always try to respect the Up direction.")]
        public CameraUpMode m_CameraUp = CameraUpMode.Default;
        
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

        /// <summary>If checked, will enable damping.</summary>
        [Tooltip("If checked, will enable damping.")]
        public bool m_DampingEnabled;
        
        /// <summary>How aggressively the camera tries to maintain the offset along
        /// the x, y, or z directions in spline local space.
        /// Meaning:
        /// - x represents the axis that is perpendicular to the spline. Use this to smooth out
        /// imperfections in the path. This may move the camera off the spline.
        /// - y represents the axis that is defined by the spline-local up direction. Use this to smooth out
        /// imperfections in the path. This may move the camera off the spline.
        /// - z represents the axis that is parallel to the spline. This won't move the camera off the spline.
        /// Smaller numbers are more responsive. Larger numbers give a more heavy, slowly responding camera.
        /// Using different settings per axis can yield a wide range of camera behaviors.</summary>
        [Tooltip("How aggressively the camera tries to maintain the offset along the " +
            "x, y, or z directions in spline local space. \n" +
            "- x represents the axis that is perpendicular to the spline. Use this to smooth out " +
            "imperfections in the path. This may move the camera off the spline.\n" +
            "- y represents the axis that is defined by the spline-local up direction. Use this to smooth out " +
            "imperfections in the path. This may move the camera off the spline.\n" +
            "- z represents the axis that is parallel to the spline. This won't move the camera off the spline.\n\n" +
            "Smaller numbers are more responsive, larger numbers give a more heavy, slowly responding camera. " +
            "Using different settings per axis can yield a wide range of camera behaviors.")]
        public Vector3 m_Damping = Vector3.zero;

        /// <summary>How aggressively the camera tries to track the target's orientation.
        /// Small numbers are more responsive.  Larger numbers give a more heavy slowly responding camera.</summary>
        [Range(0f, 20f)]
        [Tooltip("How aggressively the camera tries to track the target's orientation.  "
            + "Small numbers are more responsive.  Larger numbers give a more heavy slowly responding camera.")]
        public float m_AngularDamping = 0f;

        /// <summary>Controls how automatic dollying occurs</summary>
        [Serializable]
        public struct AutoDolly
        {
            /// <summary>If checked, will enable automatic dolly, which chooses a spline position
            /// that is as close as possible to the Follow target.</summary>
            [Tooltip("If checked, will enable automatic dolly, which chooses a spline position that is as "
                + "close as possible to the Follow target.  Note: this can have significant performance impact")]
            public bool m_Enabled;

            /// <summary>Offset, in current position units, from the closest point on the spline to the follow target.</summary>
            [Tooltip("Offset, in current position units, from the closest point on the spline to the follow target")]
            public float m_PositionOffset;

            /// <summary>
            /// Affects how many segments to split a spline into when calculating the nearest point.
            /// Higher values mean smaller and more segments, which increases accuracy at the cost of processing time.
            /// In most cases, the default resolution is appropriate. Use with <seealso cref="m_SearchIteration"/> to fine-tune point accuracy.
            /// For more information, see SplineUtility.GetNearestPoint.
            /// </summary>
            [HideInInspector]
            [Tooltip("Affects how many segments to split a spline into when calculating the nearest point." +
                "Higher values mean smaller and more segments, which increases accuracy at the cost of processing time. " +
                "In most cases, the default value (4) is appropriate. Use with SearchIteration to fine-tune point accuracy.")]
            public int m_SearchResolution;

            /// <summary>
            /// The nearest point is calculated by finding the nearest point on the entire length
            /// of the spline using <seealso cref="m_SearchResolution"/> to divide into equally spaced line segments. Successive
            /// iterations will then subdivide further the nearest segment, producing more accurate results. In most cases,
            /// the default value is sufficient.
            /// For more information, see SplineUtility.GetNearestPoint.
            /// </summary>
            [HideInInspector]
            [Tooltip("The nearest point is calculated by finding the nearest point on the entire length of the spline " +
                "using SearchResolution to divide into equally spaced line segments. Successive iterations will then " +
                "subdivide further the nearest segment, producing more accurate results. In most cases, the default value (2) is sufficient.")]
            public int m_SearchIteration;
        }

        /// <summary>Controls how automatic dollying occurs</summary>
        [Tooltip("Controls how automatic dollying occurs.  A Follow target is necessary to use this feature.")]
        public AutoDolly m_AutoDolly = new AutoDolly 
        { 
            m_Enabled = false, 
            m_PositionOffset = 0,
            m_SearchIteration = 4, 
            m_SearchResolution = 2,
        };

        /// <summary>True if component is enabled and has a spline</summary>
        public override bool IsValid => enabled && m_Spline != null;

        /// <summary>Get the Cinemachine Pipeline stage that this component implements.
        /// Always returns the Body stage</summary>
        public override CinemachineCore.Stage Stage => CinemachineCore.Stage.Body;

        /// <summary>
        /// Report maximum damping time needed for this component.
        /// </summary>
        /// <returns>Highest damping setting in this component</returns>
        public override float GetMaxDampTime() => !m_DampingEnabled ? 0 :
            Mathf.Max(Mathf.Max(m_Damping.x, Mathf.Max(m_Damping.y, m_Damping.z)), m_AngularDamping);

        /// <summary>
        /// Adds spline to SplineDolly.
        /// </summary>
        /// <param name="spline">The spline to add. If null, then any spline on SplineDolly will be removed.</param>
        public void AddSplineContainer(SplineContainer spline)
        {
            m_Spline = spline;
            UpdateSplineData();
        }

        /// <summary>
        /// Subscribe to onSplineChanged if you'd like to react to changes to the Spline attached to this vcam.
        /// This action is invoked by the Spline's changed event when a spline property is modified. Available in editor only.
        /// </summary>
        public event Action onSplineChanged;

        /// <summary>Positions the virtual camera according to the transposer rules.</summary>
        /// <param name="curState">The current camera state</param>
        /// <param name="deltaTime">Used for damping.  If less that 0, no damping is done.</param>
        public override void MutateCameraState(ref CameraState curState, float deltaTime)
        {
            if (!IsValid)
            {
                return;
            }

            var spline = m_Spline.Spline;
            if (spline == null || spline.Count == 0)
            {
                return;
            }
            
            m_CameraPosition = SanitizeInterpolationValue(spline, m_CameraPosition, m_PositionUnits);
            // splines work with normalized position by default, so we convert m_SplinePosition to normalized at the start
            var normalizedSplinePosition = 
                spline.ConvertIndexUnit(m_CameraPosition, m_PositionUnits, PathIndexUnit.Normalized);
            
            // Init previous frame state info
            if (deltaTime < 0 || !VirtualCamera.PreviousStateIsValid)
            {
                m_PreviousNormalizedSplinePosition = normalizedSplinePosition;
                m_PreviousCameraPosition = curState.RawPosition;
                m_PreviousOrientation = curState.RawOrientation;
                RefreshRollCache();
            }

            // Get the new ideal spline base position
            if (m_AutoDolly.m_Enabled && FollowTarget != null)
            {
                // convert follow target into spline local space, because SplineUtility works in spline local space
                SplineUtility.GetNearestPoint(spline, 
                    m_Spline.transform.InverseTransformPoint(FollowTargetPosition), out _, out normalizedSplinePosition, 
                    m_AutoDolly.m_SearchResolution, m_AutoDolly.m_SearchIteration);
                m_CameraPosition = spline.ConvertIndexUnit(normalizedSplinePosition, 
                    PathIndexUnit.Normalized, m_PositionUnits);
                
                // Apply the spline position offset
                normalizedSplinePosition += spline.ConvertIndexUnit(m_AutoDolly.m_PositionOffset, 
                    m_PositionUnits, PathIndexUnit.Normalized);
            }

            if (deltaTime >= 0 && VirtualCamera.PreviousStateIsValid)
            {
                const float maxNormalizedValue = 1f;
                float prev = m_PreviousNormalizedSplinePosition;
                float next = normalizedSplinePosition;
                if (spline.Closed && Mathf.Abs(next - prev) > maxNormalizedValue / 2f)
                {
                    if (next > prev)
                        prev += maxNormalizedValue;
                    else
                        prev -= maxNormalizedValue;
                }

                m_PreviousNormalizedSplinePosition = prev;
                normalizedSplinePosition = next;

                // Apply damping in the spline direction
                if (m_DampingEnabled && deltaTime >= 0)
                {
                    float offset = m_PreviousNormalizedSplinePosition - normalizedSplinePosition;
                    offset = Damper.Damp(offset, m_Damping.z, deltaTime);
                    normalizedSplinePosition = m_PreviousNormalizedSplinePosition - offset;
                }
            }
            m_PreviousNormalizedSplinePosition = normalizedSplinePosition;

            m_Spline.EvaluateSplineWithRoll(
                SplineRoll, normalizedSplinePosition, out var newCameraPos, out var newSplineOrientation);

            // Apply the offset to get the new camera position
            var offsetX = newSplineOrientation * Vector3.right;
            var offsetY = newSplineOrientation * Vector3.up;
            var offsetZ = newSplineOrientation * Vector3.forward;
            newCameraPos += m_SplineOffset.x * offsetX;
            newCameraPos += m_SplineOffset.y * offsetY;
            newCameraPos += m_SplineOffset.z * offsetZ;

            // Apply damping to the remaining directions
            if (m_DampingEnabled && deltaTime >= 0 && VirtualCamera.PreviousStateIsValid)
            {
                Vector3 currentCameraPos = m_PreviousCameraPosition;
                Vector3 delta = (currentCameraPos - newCameraPos);
                Vector3 delta1 = Vector3.Dot(delta, offsetY) * offsetY;
                Vector3 delta0 = delta - delta1;
                delta0 = Damper.Damp(delta0, m_Damping.x, deltaTime);
                delta1 = Damper.Damp(delta1, m_Damping.y, deltaTime);
                newCameraPos = currentCameraPos - (delta0 + delta1);
            }
            curState.RawPosition = m_PreviousCameraPosition = newCameraPos;

            // Set the orientation and up
            Quaternion newOrientation = GetCameraOrientationAtSplinePoint(newSplineOrientation, curState.ReferenceUp);
            if (m_DampingEnabled && deltaTime >= 0 && VirtualCamera.PreviousStateIsValid)
            {
                float t = VirtualCamera.DetachedFollowTargetDamp(1, m_AngularDamping, deltaTime);
                newOrientation = Quaternion.Slerp(m_PreviousOrientation, newOrientation, t);
            }
            m_PreviousOrientation = newOrientation;

            curState.RawOrientation = newOrientation;
            if (m_CameraUp != CameraUpMode.Default)
                curState.ReferenceUp = curState.RawOrientation * Vector3.up;
        }


        CinemachineSplineRoll m_RollCache; // don't use this directly

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
        
        float SanitizeInterpolationValue(Spline spline, float t, PathIndexUnit unit)
        {
            var sanitized = t;
            var splineClosed = spline.Closed;
            switch (unit)
            {
                case PathIndexUnit.Distance:
                {
                    var splineLength = m_SplineLength;
                    if (splineClosed)
                    {
                        sanitized %= splineLength;
                        if (sanitized < 0)
                        {
                            sanitized += splineLength;
                        }
                    }
                    else sanitized = Mathf.Clamp(sanitized, 0, splineLength);

                    break;
                }
                case PathIndexUnit.Knot:
                {
                    var knotCount = m_SplineKnotCount;
                    if (splineClosed)
                    {
                        sanitized %= knotCount;
                        if (sanitized < 0)
                        {
                            sanitized += knotCount;
                        }
                    }
                    else sanitized = Mathf.Clamp(sanitized, 0, knotCount);

                    break;
                }
                default:
                case PathIndexUnit.Normalized:
                {
                    if (splineClosed)
                    {
                        sanitized -= Mathf.Floor(sanitized);
                        if (sanitized < 0)
                        {
                            sanitized += 1f;
                        }
                    }
                    else sanitized = Mathf.Clamp01(sanitized);
                    break;
                }
            }
            return sanitized;
        }

        Quaternion GetCameraOrientationAtSplinePoint(Quaternion splineOrientation, Vector3 up)
        {
            switch (m_CameraUp)
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

        float m_PreviousNormalizedSplinePosition = 0;
        Quaternion m_PreviousOrientation = Quaternion.identity;
        Vector3 m_PreviousCameraPosition = Vector3.zero;
        
        bool m_Registered = false;
        SplineContainer m_SplineCache;
        void OnValidate()
        {
            if (m_SplineCache != null)
            {
                m_SplineCache.Spline.changed -= onSplineChanged;
                m_SplineCache.Spline.changed -= UpdateSplineData;
                m_SplineCache = m_Spline;
                m_Registered = false;
            }
            if (!m_Registered && m_Spline != null && m_Spline.Spline != null)
            {
                m_Registered = true;
                m_SplineCache = m_Spline;
                m_Spline.Spline.changed += onSplineChanged;
                m_Spline.Spline.changed += UpdateSplineData;
            }

            m_Damping.x = Mathf.Clamp(m_Damping.x, 0, 20);
            m_Damping.y = Mathf.Clamp(m_Damping.y, 0, 20);
            m_Damping.z = Mathf.Clamp(m_Damping.z, 0, 20);
            m_AngularDamping = Mathf.Clamp(m_AngularDamping, 0, 20);
            UpdateSplineData();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            RefreshRollCache();
        }

        float m_SplineLength;
        int m_SplineKnotCount;
        void UpdateSplineData()
        {
            if (m_Spline != null && m_Spline.Spline != null)
            {
                m_SplineLength = m_Spline.Spline.GetLength();
                m_SplineKnotCount = m_Spline.Spline.Knots.Count();
            }
            else
            {
                m_SplineLength = m_SplineKnotCount = 0;
            }
        }

        void RefreshRollCache()
        {
            VirtualCamera.TryGetComponent(out m_RollCache); // check if vcam has CinemachineSplineRoll
#if UNITY_EDITOR
            if (m_RollCache != null)
                m_RollCache.SplineContainer = m_Spline; // need to tell CinemachineSplineRoll about its spline for drawing purposes
#endif
            if (m_Spline != null && m_RollCache == null)
                m_Spline.TryGetComponent(out m_RollCache); // check if our spline has CinemachineSplineRoll
        }
    }

    static class SplineContainerExtensions
    {
        public static bool EvaluateSplineWithRoll(
            this SplineContainer spline,
            CinemachineSplineRoll roll,
            float tNormalized, 
            out Vector3 position, out Quaternion rotation)
        {
            if (!spline.Evaluate(tNormalized, out var localPosition, out var localTangent, out var localUp))
            {
                position = localPosition;
                rotation = Quaternion.identity;
                return false;
            }

            position = localPosition;
            Vector3 fwd = localTangent;
            Vector3 up = localUp;

            // fix tangent when 0
            if (fwd.Equals(Vector3.zero))
            {
                const float delta = 0.001f;
                var atEnd = tNormalized > 1.0f - delta;
                var t1 = atEnd ? tNormalized - delta : tNormalized + delta;
                var p = spline.EvaluatePosition(t1);
                fwd = atEnd ? localPosition - p : p - localPosition;
            }
            // GML todo: what if fwd and up are parallel?
            rotation = Quaternion.LookRotation(fwd, up);

            // Apply extra roll
            if (roll != null && roll.enabled)
            {
                float rollValue = roll.Roll.Evaluate(spline.Spline, tNormalized, 
                    PathIndexUnit.Normalized, new UnityEngine.Splines.Interpolators.LerpFloat());
                rotation = Quaternion.AngleAxis(-rollValue, fwd) * rotation;
            }
            return true;
        }
    }
}
#endif
