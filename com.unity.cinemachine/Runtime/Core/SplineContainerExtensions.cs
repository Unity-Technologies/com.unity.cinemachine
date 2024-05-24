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
        /// Apply to a <see cref="CachedScaledSpline"/>additional roll from <see cref="CinemachineSplineRoll"/>
        /// </summary>
        /// <param name="spline">The spline in question</param>
        /// <param name="roll">The additional roll to apply</param>
        /// <param name="tNormalized">The normalized position on the spline</param>
        /// <param name="defaultRotation">Used to resolve cases where spline tangent is undefined</param>
        /// <param name="position">returned point on the spline, in spline-local coords</param>
        /// <param name="rotation">returned rotation at the point on the spline, in spline-local coords</param>
        /// <returns>True if the spline position is valid</returns>
        public static bool LocalEvaluateSplineWithRoll(
            this ISpline spline,
            float tNormalized, 
            Quaternion defaultRotation,
            CinemachineSplineRoll roll,
            out Vector3 position, out Quaternion rotation)
        {
            if (spline == null || !SplineUtility.Evaluate(
                spline, tNormalized, out var localPosition, out var localTangent, out var localUp))
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
                float rollValue = roll.Roll.Evaluate(spline, tNormalized, 
                    PathIndexUnit.Normalized, new CinemachineSplineRoll.LerpRollData());
                rotation = Quaternion.AngleAxis(-rollValue, fwd) * rotation;
            }
            return true;
        }

        /// <summary>
        /// Apply to a <see cref="CachedScaledSpline"/>additional roll from <see cref="CinemachineSplineRoll"/>
        /// </summary>
        /// <param name="spline">The spline in question</param>
        /// <param name="tNormalized">The normalized position on the spline</param>
        /// <param name="defaultRotation">Used to resolve cases where spline tangent is undefined</param>
        /// <param name="roll">The additional roll to apply</param>
        /// <param name="position">returned point on the spline, in world coords</param>
        /// <param name="rotation">returned rotation at the point on the spline, in world coords</param>
        /// <returns>True if the spline position is valid</returns>
        public static bool EvaluateSplineWithRoll(
            this CachedScaledSpline spline,
            Transform transform,
            float tNormalized, 
            Quaternion defaultRotation,
            CinemachineSplineRoll roll,
            out Vector3 position, out Quaternion rotation)
        {
            var result = LocalEvaluateSplineWithRoll(spline, tNormalized, defaultRotation, roll, out position, out rotation);
            position = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one).MultiplyPoint3x4(position);
            rotation = transform.rotation * rotation;
            return result;
        }

        /// <summary>Evaluate a spline's world position and rotation at a normalized spline index</summary>
        /// <param name="spline">The spline in question</param>
        /// <param name="tNormalized">The normalized position on the spline</param>
        /// <param name="position">returned point on the spline, in world coords</param>
        /// <returns>True if the spline position is valid</returns>
        public static bool EvaluateSplinePosition(
            this CachedScaledSpline spline, Transform transform, float tNormalized, out Vector3 position)
        {
            if (spline == null || !SplineUtility.Evaluate(spline, tNormalized, out var localPosition, out _, out _))
            {
                position = Vector3.zero;
                return false;
            }
            position = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one).MultiplyPoint3x4(localPosition);
            return true;
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