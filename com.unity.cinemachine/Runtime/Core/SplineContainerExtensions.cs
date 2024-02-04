using System;
using System.Runtime.CompilerServices;
using static System.Runtime.CompilerServices.MethodImplOptions;

using UnityEngine;
using UnityEngine.Splines;

using static UnityEngine.Splines.PathIndexUnit;

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
            rotation = rotation * spline.transform.rotation;
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
        /// <returns></returns>
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
        
        /// <summary>
        /// Converts the distance from one unit to another.
        /// </summary>
        /// <param name="spline">The spline which length is used for conversion.</param>
        /// <param name="distance">The distance to convert.</param>
        /// <param name="oldUnits">The original units of the distance.</param>
        /// <param name="newUnits">The units to convert the distance to.</param>
        /// <returns>The converted distance.</returns>
        [MethodImpl(methodImplOptions: AggressiveInlining)]
        internal static float ConvertDistance(this Spline spline, float distance, PathIndexUnit oldUnits, PathIndexUnit newUnits)
        {
            if(distance == 0) return 0;
            
            return (oldUnits, newUnits) switch
            {
                (oldUnits: Distance,   newUnits: Normalized) => ConvertDistanceMetresToNormalized(spline, distance),
                (oldUnits: Normalized, newUnits: Distance)   => ConvertDistanceNormalizedToMetres(spline, distance),
                (oldUnits: Distance,   newUnits: Knot)       => ConvertDistanceMetresToKnot(      spline, distance),
                (oldUnits: Knot,       newUnits: Distance)   => ConvertDistanceKnotToMetres(      spline, distance),
                (oldUnits: Normalized, newUnits: Knot)       => ConvertDistanceNormalizedToKnot(  spline, distance),
                (oldUnits: Knot,       newUnits: Normalized) => ConvertDistanceKnotToNormalized(  spline, distance),
                _                                            => distance,
            };
        }
        
        /// <summary>
        /// Converts the distance from metres to normalized units.
        /// </summary>
        /// <param name="distance">The distance in metres to convert.</param>
        /// <param name="spline">The spline which length is used for conversion.</param>
        /// <returns>The converted distance in normalized units.</returns>
        [MethodImpl(methodImplOptions: AggressiveInlining)]
        internal static float ConvertDistanceMetresToNormalized(this Spline spline, float distance)
        {
            float splineLength = spline.GetLength();
            
            if(distance < 0)            throw new ArgumentOutOfRangeException(paramName: nameof(distance), message: "Distance cannot be negative.");
            if(distance > splineLength) throw new ArgumentOutOfRangeException(paramName: nameof(distance), message: "Distance cannot be greater than the length of the spline.");
            
            return distance / splineLength;
        }
        
        [MethodImpl(methodImplOptions: AggressiveInlining)]
        internal static float ConvertDistanceNormalizedToMetres(this Spline spline, float distance)
        {
            float splineLength = spline.GetLength();
            
            if(distance < 0) throw new ArgumentOutOfRangeException(paramName: nameof(distance), message: "Distance cannot be negative.");
            if(distance > 1) throw new ArgumentOutOfRangeException(paramName: nameof(distance), message: "Distance cannot be greater than 1.");
            
            return distance * splineLength;
        }

        
        [MethodImpl(methodImplOptions: AggressiveInlining)]
        internal static float ConvertDistanceMetresToKnot(this Spline spline, float distance)
        {
            float splineLength = spline.GetLength();
            
            if(distance < 0)            throw new ArgumentOutOfRangeException(paramName: nameof(distance), message: "Distance cannot be negative.");
            if(distance > splineLength) throw new ArgumentOutOfRangeException(paramName: nameof(distance), message: "Distance cannot be greater than the length of the spline.");
            
            return ConvertDistanceNormalizedToKnot(spline, distance: ConvertDistanceMetresToNormalized(spline, distance), pathLengthReciprocal: 1 / splineLength);
        }
        
        [MethodImpl(methodImplOptions: AggressiveInlining)]
        internal static float ConvertDistanceKnotToMetres(this Spline spline, float distance)
        {
            if (distance < 0)                throw new ArgumentOutOfRangeException(paramName: nameof(distance), message: "Distance cannot be negative.");
            if (distance > spline.Count - 1) throw new ArgumentOutOfRangeException(paramName: nameof(distance), message: "Distance cannot be greater than the number of knots.");
            
            return ConvertDistanceNormalizedToMetres(spline, distance: ConvertDistanceKnotToNormalized(spline, distance, pathLengthReciprocal: 1 / spline.GetLength()));
        }
        
        
        [MethodImpl(methodImplOptions: AggressiveInlining)]
        internal static float ConvertDistanceNormalizedToKnot(this Spline spline, float distance)
        {
            if (distance < 0) throw new ArgumentOutOfRangeException(paramName: nameof(distance), message: "Distance cannot be negative.");
            if (distance > 1) throw new ArgumentOutOfRangeException(paramName: nameof(distance), message: "Distance cannot be greater than 1.");
            
            return ConvertDistanceNormalizedToKnot(spline, distance, pathLengthReciprocal: 1 / spline.GetLength());
        }
        [MethodImpl(methodImplOptions: AggressiveInlining)]
        internal static float ConvertDistanceNormalizedToKnot(this Spline spline, float distance, float pathLengthReciprocal)
        {
            if(distance < 0) throw new ArgumentOutOfRangeException(paramName: nameof(distance), message: "Distance cannot be negative.");
            if(distance > 1) throw new ArgumentOutOfRangeException(paramName: nameof(distance), message: "Distance cannot be greater than 1.");
            
            float accumulatedDistanceNormalized = 0;
            for (int knotIndex = 0; knotIndex < spline.Count; knotIndex++)
            {
                float curveLengthNormalized = spline.GetCurveLength(knotIndex) * pathLengthReciprocal;
                
                if(accumulatedDistanceNormalized + curveLengthNormalized < distance)
                {
                    accumulatedDistanceNormalized += curveLengthNormalized;
                    continue;
                }
                
                float remainingSplineDistanceNormalized = distance - accumulatedDistanceNormalized;
                
                float distanceBetweenKnotsNormalized = remainingSplineDistanceNormalized / curveLengthNormalized;
                
                return knotIndex + distanceBetweenKnotsNormalized;
            }
            return spline.Count - 1;
        }
        
        [MethodImpl(methodImplOptions: AggressiveInlining)]
        internal static float ConvertDistanceKnotToNormalized(this Spline spline, float distance)
        {
            if(distance < 0)                throw new ArgumentOutOfRangeException(paramName: nameof(distance), message: "Distance cannot be negative.");
            if(distance > spline.Count - 1) throw new ArgumentOutOfRangeException(paramName: nameof(distance), message: "Distance cannot be greater than the number of knots.");
            
            return ConvertDistanceKnotToNormalized(spline, distance, pathLengthReciprocal: 1 / spline.GetLength());
        }
        [MethodImpl(methodImplOptions: AggressiveInlining)]
        internal static float ConvertDistanceKnotToNormalized(this Spline spline, float distance, float pathLengthReciprocal)
        {
            if(distance < 0)                throw new ArgumentOutOfRangeException(paramName: nameof(distance), message: "Distance cannot be negative.");
            if(distance > spline.Count - 1) throw new ArgumentOutOfRangeException(paramName: nameof(distance), message: "Distance cannot be greater than the number of knots.");
            
            int knotIndex = Mathf.FloorToInt(distance);
            
            float distanceToKnotIndexNormalized = 0;
            for (int i = 0; i < knotIndex; i++)
            {
                distanceToKnotIndexNormalized += spline.GetCurveLength(i) * pathLengthReciprocal;
            }
            
            float distanceDecimal   = distance - knotIndex;
            float remainingDistance = spline.GetCurveLength(knotIndex) * distanceDecimal;
            
            float remainingDistanceNormalized = remainingDistance * pathLengthReciprocal;

            float normalizedDistance = distanceToKnotIndexNormalized + remainingDistanceNormalized;
            
            return normalizedDistance;
        }
    }
}