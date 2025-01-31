using System;
using UnityEngine;
using UnityEngine.Splines;

namespace Unity.Cinemachine
{
    /// <summary>
    /// This structure holds the object that implements AutoDolly on a spline.
    /// </summary>
    [Serializable]
    public struct SplineAutoDolly
    {
        /// <summary>If set, will enable AutoDolly on a spline</summary>
        [Tooltip("If set, will enable the selected automatic dolly along the spline")]
        public bool Enabled;

        /// <summary>This is the object that actually implements the AutoDolly</summary>
        [SerializeReference]
        public ISplineAutoDolly Method;

        /// <summary>
        /// Interface for procedural spline dolly.
        /// Implement this to provide a custom algorithm for choosing a point on the path.
        /// </summary>
        public interface ISplineAutoDolly
        {
            /// <summary>Called from OnValidate() to validate the settings.</summary>
            void Validate();

            /// <summary>Call this to reset any state information contained in the implementation.</summary>
            void Reset();

            /// <summary>Returns true if this implementation requires a tracking target.</summary>
            bool RequiresTrackingTarget { get; }

            /// <summary>
            /// Compute the desired position on the spline.
            /// </summary>
            /// <param name="sender">The MonoBehaviour that is asking.</param>
            /// <param name="target">The target object (may be null for algorithms that don't require it).</param>
            /// <param name="spline">The spline on which the location must be found.</param>
            /// <param name="currentPosition">The current position on the spline.</param>
            /// <param name="positionUnits">The units in which spline positions are expressed.</param>
            /// <param name="deltaTime">Current deltaTime.  If smaller than 0, then previous frame data should be ignored.</param>
            /// <returns>The desired position on the spline, expressed in positionUnits.</returns>
            float GetSplinePosition(
                MonoBehaviour sender, Transform target, SplineContainer spline,
                float currentPosition, PathIndexUnit positionUnits, float deltaTime);
        }

        /// <summary>
        /// ISplineAutoDolly implementation that moves the object at a constant speed align the spline.
        /// </summary>
        [Serializable]
        public class FixedSpeed : ISplineAutoDolly
        {
            /// <summary>Speed of travel, in current position units per second.</summary>
            [Tooltip("Speed of travel, in current position units per second.")]
            public float Speed;

            /// <summary>Called from OnValidate() to validate the settings.</summary>
            void ISplineAutoDolly.Validate() {}

            /// <summary>This implementation does nothing.</summary>
            void ISplineAutoDolly.Reset() {}

            /// <summary>Returns true if this implementation requires a tracking target.</summary>
            bool ISplineAutoDolly.RequiresTrackingTarget => false;

            /// <summary>
            /// Compute the desired position on the spline.
            /// </summary>
            /// <param name="sender">The MonoBehaviour that is asking.</param>
            /// <param name="target">The target object (may be null for algorithms that don't require it).</param>
            /// <param name="spline">The spline on which the location must be found.</param>
            /// <param name="currentPosition">The current position on the spline.</param>
            /// <param name="positionUnits">The units in which spline positions are expressed.</param>
            /// <param name="deltaTime">Current deltaTime.  If smaller than 0, then previous frame data should be ignored.</param>
            /// <returns>The desired position on the spline, expressed in positionUnits.</returns>
            float ISplineAutoDolly.GetSplinePosition(
                MonoBehaviour sender, Transform target, SplineContainer spline,
                float currentPosition, PathIndexUnit positionUnits, float deltaTime)
            {
                // Only works if playing
                if (Application.isPlaying && spline.IsValid() && deltaTime > 0)
                    return currentPosition + Speed * deltaTime;
                return currentPosition;
            }
        }

        /// <summary>
        /// ISplineAutoDolly implementation that finds the point on th spline closest to the target.
        /// Note that this is a simple stateless algorithm, and is not appropriate for all spline shapes.
        /// For example, if the spline is forming an arc and the target is inside the arc, then the closest
        /// point can be noisy or undefined.  Consider for example a spline that is perfectly circular
        /// with the target at the center.  Where is the closest point?
        /// </summary>
        [Serializable]
        public class NearestPointToTarget : ISplineAutoDolly
        {
            /// <summary>
            /// Offset, in current position units, from the closest point on the spline to the follow target.
            /// </summary>
            [Tooltip("Offset, in current position units, from the closest point on the spline to the follow target")]
            public float PositionOffset = 0;

            /// <summary>
            /// Affects how many segments to split a spline into when calculating the nearest point.
            /// Higher values mean smaller and more segments, which increases accuracy at the cost of
            /// processing time.  In most cases, the default resolution is appropriate. Use
            /// with <see cref="SearchIteration"/> to fine-tune point accuracy.
            /// For more information, see SplineUtility.GetNearestPoint.
            /// </summary>
            [Tooltip("Affects how many segments to split a spline into when calculating the nearest point.  "
                + "Higher values mean smaller and more segments, which increases accuracy at the cost of "
                + "processing time.  In most cases, the default value (4) is appropriate. Use with SearchIteration "
                + "to fine-tune point accuracy.")]
            public int SearchResolution = 4;

            /// <summary>
            /// The nearest point is calculated by finding the nearest point on the entire length
            /// of the spline using <see cref="SearchResolution"/> to divide into equally spaced line segments.
            /// Successive iterations will then subdivide further the nearest segment, producing more
            /// accurate results. In most cases, the default value is sufficient.
            /// For more information, see SplineUtility.GetNearestPoint.
            /// </summary>
            [Tooltip("The nearest point is calculated by finding the nearest point on the entire "
                + "length of the spline using SearchResolution to divide into equally spaced line segments. "
                + "Successive iterations will then subdivide further the nearest segment, producing more "
                + "accurate results. In most cases, the default value (2) is sufficient.")]
            public int SearchIteration = 2;

            /// <summary>Called from OnValidate() to validate the settings.</summary>
            void ISplineAutoDolly.Validate()
            {
                SearchResolution = Mathf.Max(SearchResolution, 1);
                SearchIteration = Mathf.Max(SearchIteration, 1);
            }

            /// <summary>This implementation does nothing.</summary>
            void ISplineAutoDolly.Reset() {}

            /// <summary>Returns true if this implementation requires a tracking target.</summary>
            bool ISplineAutoDolly.RequiresTrackingTarget => true;

            /// <summary>
            /// Compute the desired position on the spline.
            /// </summary>
            /// <param name="sender">The MonoBehaviour that is asking.</param>
            /// <param name="target">The target object (may be null for algorithms that don't require it).</param>
            /// <param name="spline">The spline on which the location must be found.</param>
            /// <param name="currentPosition">The current position on the spline.</param>
            /// <param name="positionUnits">The units in which spline positions are expressed.</param>
            /// <param name="deltaTime">Current deltaTime.  If smaller than 0, then previous frame data should be ignored.</param>
            /// <returns>The desired position on the spline, expressed in positionUnits.</returns>
            float ISplineAutoDolly.GetSplinePosition(
                MonoBehaviour sender, Transform target, SplineContainer spline,
                float currentPosition, PathIndexUnit positionUnits, float deltaTime)
            {
                if (target == null || !spline.IsValid())
                    return currentPosition;

                // Convert target into spline local space, because SplineUtility works in spline local space
                SplineUtility.GetNearestPoint(spline.Spline,
                    spline.transform.InverseTransformPoint(target.position), out _, out var normalizedPos,
                    SearchResolution, SearchIteration);

                // GML hack because SplineUtility.GetNearestPoint is buggy
                normalizedPos = Mathf.Clamp01(normalizedPos);

                var pos = spline.Spline.ConvertIndexUnit(normalizedPos, PathIndexUnit.Normalized, positionUnits);
                return pos + PositionOffset;
            }
        }
    }
}
