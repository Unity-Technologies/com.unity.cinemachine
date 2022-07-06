using Cinemachine.Utility;
using UnityEngine;
using UnityEngine.Splines;

namespace Cinemachine
{
    /// <summary>
    /// A collection of helpers for UnityEngine Spline.
    /// </summary>
    public static class SplineContainerExtensions
    {
        /// <summary>
        /// Apply to a <see cref="SplineContainer"/>additional roll from <see cref="CinemachineSplineRoll"/>
        /// </summary>
        /// <param name="spline">The spline in question</param>
        /// <param name="roll">The additional roll to apply</param>
        /// <param name="tNormalized">The normalized position on the spline</param>
        /// <param name="position">returned point on the spline</param>
        /// <param name="rotation">returned rotation at the point on the spline</param>
        /// <returns>True if the spline position is valid</returns>
        public static bool EvaluateSplineWithRoll(
            this SplineContainer spline,
            CinemachineSplineRoll roll,
            Quaternion defaultRotation,
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
        /// Clamp spline position to min and max values, respecting loop wraparound for closed paths.
        /// </summary>
        /// <param name="spline">The spline in question</param>
        /// <param name="t">Spline position to sanitize</param>
        /// <param name="unit">The spline position is expressed in these units</param>
        /// <param name="splineLength">The length of the spline, in distance units.  
        /// Passed as parameter for efficienacy because length calculation is slow.
        /// If a negative value is passed, length will be calculated.</param>
        /// <returns></returns>
        public static float StandardizeSplinePosition(
            this Spline spline, float t, PathIndexUnit unit, float splineLength = -1)
        {
            switch (unit)
            {
                case PathIndexUnit.Distance:
                {
                    if (splineLength < 0)
                        splineLength = spline.GetLength();
                    if (!spline.Closed)
                        t = Mathf.Clamp(t, 0, splineLength);
                    else 
                    {
                        t %= splineLength;
                        if (t < 0)
                            t += splineLength;
                    }
                    break;
                }
                case PathIndexUnit.Knot:
                {
                    var knotCount = spline.Count;
                    if (!spline.Closed || knotCount < 2)
                        t = Mathf.Clamp(t, 0, knotCount);
                    else 
                    {
                        t %= knotCount + 1;
                        if (t < 0)
                            t += knotCount;
                    }
                    break;
                }
                default:
                case PathIndexUnit.Normalized:
                {
                    if (!spline.Closed)
                        t = Mathf.Clamp01(t);
                    else
                    {
                        t -= Mathf.Floor(t);
                        if (t < 0)
                            t += 1f;
                    }
                    break;
                }
            }
            return t;
        }
    }
}