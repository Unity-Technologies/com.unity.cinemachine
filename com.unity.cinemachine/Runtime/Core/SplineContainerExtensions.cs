using Unity.Mathematics;
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
        public static bool IsValid(this ISplineContainer spline) => spline != null && spline.Splines != null && spline.Splines.Count > 0;

        /// <summary>
        /// Apply to a <see cref="ISpline"/>additional roll from <see cref="CinemachineSplineRoll"/>
        /// </summary>
        /// <param name="spline">The spline in question</param>
        /// <param name="tNormalized">The normalized position on the spline</param>
        /// <param name="roll">The additional roll to apply, or null</param>
        /// <param name="position">returned point on the spline, in spline-local coords</param>
        /// <param name="rotation">returned rotation at the point on the spline, in spline-local coords</param>
        /// <returns>True if the spline position is valid</returns>
        public static bool LocalEvaluateSplineWithRoll(
            this ISpline spline,
            float tNormalized, 
            CinemachineSplineRoll roll,
            out Vector3 position, out Quaternion rotation)
        {
            if (spline == null || !SplineUtility.Evaluate(
                spline, tNormalized, out var splinePosition, out var fwd, out var up))
            {
                position = Vector3.zero;
                rotation = Quaternion.identity;
                return false;
            }

            // Use defaults if spline rotation is undefined
            var cross = Vector3.Cross(fwd, up);
            if (cross.AlmostZero() || cross.IsNaN())
            {
                fwd = Vector3.forward;
                up = Vector3.up;
            }

            // Apply extra roll if present
            if (roll == null || !roll.enabled)
                rotation = Quaternion.LookRotation(fwd, up);
            else
            {
                float rollValue = roll.Roll.Evaluate(spline, tNormalized, PathIndexUnit.Normalized, roll.GetInterpolator());
                rotation = Quaternion.LookRotation(fwd, up) * RollAroundForward(rollValue);

                // same as Quaternion.AngleAxis(roll, Vector3.forward), just simplified
                static Quaternion RollAroundForward(float angle)
                {
                    float halfAngle = angle * 0.5F * Mathf.Deg2Rad;
                    return new Quaternion(0, 0, Mathf.Sin(halfAngle), Mathf.Cos(halfAngle));
                }
            }
        
            position = splinePosition;
            return true;
        }

        /// <summary>
        /// Apply to a <see cref="ISpline"/>additional roll from <see cref="CinemachineSplineRoll"/>
        /// </summary>
        /// <param name="spline">The spline in question</param>
        /// <param name="transform">The transform of the spline</param>
        /// <param name="tNormalized">The normalized position on the spline</param>
        /// <param name="roll">The additional roll to apply, or null</param>
        /// <param name="position">returned point on the spline, in world coords</param>
        /// <param name="rotation">returned rotation at the point on the spline, in world coords</param>
        /// <returns>True if the spline position is valid</returns>
        public static bool EvaluateSplineWithRoll(
            this ISpline spline,
            Transform transform,
            float tNormalized, 
            CinemachineSplineRoll roll,
            out Vector3 position, out Quaternion rotation)
        {
            var result = LocalEvaluateSplineWithRoll(spline, tNormalized, roll, out position, out rotation);
            position = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one).MultiplyPoint3x4(position);
            rotation = transform.rotation * rotation;
            return result;
        }
        
        /// <summary>Evaluate a spline's world position at a normalized spline index</summary>
        /// <param name="spline">The spline in question</param>
        /// <param name="transform">The transform of the spline, or null</param>
        /// <param name="tNormalized">The normalized position on the spline</param>
        /// <returns>True if the spline position is valid</returns>
        public static Vector3 EvaluateSplinePosition(
            this ISpline spline, Transform transform, float tNormalized)
        {
            float3 position = spline == null ? default : SplineUtility.EvaluatePosition(spline, tNormalized);
            return transform == null ? position : Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one).MultiplyPoint3x4(position);
        }
        
        /// <summary>
        /// Get the maximum value for the spline position.  Minimum value is always 0.
        /// </summary>
        /// <param name="spline">The spline in question</param>
        /// <param name="unit">The spline position is expressed in these units</param>
        /// This is needed because we don't have access to the spline's scale.</param>
        /// <returns></returns>
        public static float GetMaxPosition(this ISpline spline, PathIndexUnit unit)
        {
            switch (unit)
            {
                case PathIndexUnit.Distance: 
                    return spline.GetLength();
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
        /// This is needed because we don't have access to the spline's scale.</param>
        /// <returns>The clamped position value, respecting the specified units</returns>
        public static float StandardizePosition(this ISpline spline, float t, PathIndexUnit unit, out float maxPos)
        {
            maxPos = spline.GetMaxPosition(unit);
            if (float.IsNaN(t))
                return 0;
            if (!spline.Closed)
                return Mathf.Clamp(t, 0, maxPos);
            t %= maxPos;
            if (t < 0)
                t += maxPos;
            return t;
        }
    }
}