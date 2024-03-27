using UnityEngine;
using UnityEngine.Splines;

namespace Unity.Cinemachine
{
    /// <summary>
    /// A collection of helpers for UnityEngine Spline.
    /// </summary>
    static class SplineContainerExtensions
    {
        /// <summary>Check spline container and child spline for null</summary>
        /// <param name="spline">spline container to check</param>
        /// <returns>true if container holds a non-null spline</returns>
        public static bool IsValid(this SplineContainer spline) => spline != null && spline.Spline != null;

        /// <summary>
        /// Apply to a <see cref="SplineContainer"/>additional roll from <see cref="CinemachineSplineRoll"/>
        /// </summary>
        /// <param name="spline">The spline in question</param>
        /// <param name="roll">The additional roll to apply</param>
        /// <param name="tNormalized">The normalized position on the spline</param>
        /// <param name="position">returned point on the spline, in spline-local coords</param>
        /// <param name="rotation">returned rotation at the point on the spline, in spline-local coords</param>
        /// <returns>True if the spline position is valid</returns>
        public static bool LocalEvaluateSplineWithRoll(
            this SplineContainer spline,
            CinemachineSplineRoll roll,
            Quaternion defaultRotation,
            float tNormalized, 
            out Vector3 position, out Quaternion rotation)
        {
            if (spline.Spline == null || !SplineUtility.Evaluate(
                spline.Spline, tNormalized, out var localPosition, out var localTangent, out var localUp))
            {
                position = Vector3.zero;
                rotation = Quaternion.identity;
                return false;
            }

            position = localPosition;
            Vector3 fwd = localTangent;
            Vector3 up = localUp;

            // Try to fix tangent when 0
            if (fwd.AlmostZero())
            {
                const float delta = 0.01f;
                var atEnd = tNormalized > 1.0f - delta;
                var t1 = atEnd ? tNormalized - delta : tNormalized + delta;
                var p = spline.EvaluatePosition(t1);
                fwd = atEnd ? localPosition - p : p - localPosition;
            }

            // Use supplied defaults if spline rotation is still undefined
            var cross = Vector3.Cross(fwd, up);
            if (cross.AlmostZero() || cross.IsNaN())
            {
                fwd = defaultRotation * Vector3.forward;
                up = defaultRotation * Vector3.up;
            }
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

        /// <summary>
        /// Apply to a <see cref="SplineContainer"/>additional roll from <see cref="CinemachineSplineRoll"/>
        /// </summary>
        /// <param name="spline">The spline in question</param>
        /// <param name="roll">The additional roll to apply</param>
        /// <param name="tNormalized">The normalized position on the spline</param>
        /// <param name="position">returned point on the spline, in world coords</param>
        /// <param name="rotation">returned rotation at the point on the spline, in world coords</param>
        /// <returns>True if the spline position is valid</returns>
        public static bool EvaluateSplineWithRoll(
            this SplineContainer spline,
            CinemachineSplineRoll roll,
            Quaternion defaultRotation,
            float tNormalized, 
            out Vector3 position, out Quaternion rotation)
        {
            var result = LocalEvaluateSplineWithRoll(spline, roll, defaultRotation, tNormalized, out position, out rotation);
            position = spline.transform.TransformPoint(position);
            rotation = spline.transform.rotation * rotation;
            return result;
        }
        
        /// <summary>
        /// Get the maximum value for the spline position.  Minimum value is always 0.
        /// </summary>
        /// <param name="spline">The spline in question</param>
        /// <param name="unit">The spline position is expressed in these units</param>
        /// <param name="splineLength">The length of the spline, in distance units.  
        /// Passed as parameter for efficiency because length calculation is slow.
        /// If a negative value is passed, length will be calculated.</param>
        /// <returns></returns>
        public static float GetMaxPosition(
            this Spline spline, PathIndexUnit unit, float splineLength = -1)
        {
            switch (unit)
            {
                case PathIndexUnit.Distance: 
                    return splineLength < 0 ? spline.GetLength() : splineLength;
                case PathIndexUnit.Knot: 
                {
                    var knotCount = spline.Count;
                    return (!spline.Closed || knotCount < 2) ? Mathf.Max(0, knotCount - 1) : knotCount;
                }
            }
            return 1;
        }
        
        /// <summary>
        /// Clamp spline position to min and max values, respecting loop wraparound for closed paths.
        /// </summary>
        /// <param name="spline">The spline in question</param>
        /// <param name="t">Spline position to sanitize</param>
        /// <param name="unit">The spline position is expressed in these units</param>
        /// <param name="splineLength">The length of the spline, in distance units.  
        /// Passed as parameter for efficiency because length calculation is slow.
        /// If a negative value is passed, length will be calculated.</param>
        /// <returns>The clamped position value, respecting the specified units</returns>
        public static float StandardizePosition(
            this Spline spline, float t, PathIndexUnit unit, float splineLength = -1)
        {
            var max = spline.GetMaxPosition(unit, splineLength);
            if (!spline.Closed)
                return Mathf.Clamp(t, 0, max);
            t %= max;
            if (t < 0)
                t += max;
            return t;
        }
    }
}