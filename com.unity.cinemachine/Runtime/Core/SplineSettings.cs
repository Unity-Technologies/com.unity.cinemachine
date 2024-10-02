using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace Unity.Cinemachine
{
    /// <summary>Interface for behaviours that reference a Spline</summary>
    public interface ISplineReferencer
    {
        /// <summary>Get a reference to the SplineSettings struct contained in this object.</summary>
        /// <value>A reference to the embedded SplineSettings struct</value>
        public ref SplineSettings SplineSettings { get; }
    }

    /// <summary>
    /// This structure holds the spline reference and the position and position units.
    /// </summary>
    [Serializable]
    public struct SplineSettings
    {
        /// <summary>The Spline container to which the the position will apply.</summary>
        [Tooltip("The Spline container to which the position will apply.")]
        public SplineContainer Spline;

        /// <summary>The position along the spline.  The actual value corresponding to a given point
        /// on the spline will depend on the unity type.</summary>
        [NoSaveDuringPlay]
        [Tooltip("The position along the spline.  The actual value corresponding to a given point "
            + "on the spline will depend on the unity type.")]
        public float Position;
        
        /// <summary>How to interpret the Spline Position:
        /// - Distance: Values range from 0 (start of Spline) to Length of the Spline (end of Spline).
        /// - Normalized: Values range from 0 (start of Spline) to 1 (end of Spline).
        /// - Knot: Values are defined by knot indices and a fractional value representing the normalized
        /// interpolation between the specific knot index and the next knot."</summary>
        [Tooltip("How to interpret the Spline Position:\n"
            + "- <b>Distance</b>: Values range from 0 (start of Spline) to Length of the Spline (end of Spline).\n"
            + "- <b>Normalized</b>: Values range from 0 (start of Spline) to 1 (end of Spline).\n"
            + "- <b>Knot</b>: Values are defined by knot indices and a fractional value representing the normalized " 
            + "interpolation between the specific knot index and the next knot.\n")]
        public PathIndexUnit Units;

        /// <summary>
        /// Change the units of the position, preserving the position on the spline.  
        /// The value of Position may change in order to preserve the position on the spline.
        /// </summary>
        /// <param name="newUnits">The new units to use</param>
        public void ChangeUnitPreservePosition(PathIndexUnit newUnits)
        {
            if (Spline.IsValid() && newUnits != Units)
                Position = GetCachedSpline().ConvertIndexUnit(Position, Units, newUnits);
            Units = newUnits;
        }

        CachedScaledSpline m_CachedSpline;
        int m_CachedFrame;

        /// <summary>
        /// Computing spline length dynamically is costly.  This method computes the length on the first call
        /// and caches it for subsequent calls. 
        ///
        /// While we can auto-detect changes to the transform and some changes to the spline's knots, it would be
        /// too costly to continually check for subtle changes to the spline's control points.  Therefore, if such
        /// subtle changes are made to the spline's control points at runtime, client is responsible 
        /// for calling InvalidateCache().
        /// </summary>
        /// <returns>Cached version of the spline with transform incorporated</returns>
        internal CachedScaledSpline GetCachedSpline()
        {
            if (!Spline.IsValid())
                InvalidateCache();
            else
            {
                // Only check crude validity once per frame, to keep things efficient
                if (m_CachedSpline == null || (Time.frameCount != m_CachedFrame && !m_CachedSpline.IsCrudelyValid(Spline.Spline, Spline.transform)))
                {
                    InvalidateCache();
                    m_CachedSpline = new CachedScaledSpline(Spline.Spline, Spline.transform);
                }
#if UNITY_EDITOR
                // Deep check only in editor and if not playing
                else if (!Application.isPlaying && Time.frameCount != m_CachedFrame && !m_CachedSpline.KnotsAreValid(Spline.Spline, Spline.transform))
                {
                    InvalidateCache();
                    m_CachedSpline = new CachedScaledSpline(Spline.Spline, Spline.transform);
                }
#endif
                m_CachedFrame = Time.frameCount;
            }
            return m_CachedSpline;
        }

        /// <summary>
        /// While we can auto-detect changes to the transform and some changes to the spline's knots, it would be
        /// too costly to continually check for subtle changes to the spline's control points.  Therefore, if such
        /// subtle changes are made to the spline's control points at runtime, client is responsible 
        /// for calling InvalidateCache().
        /// </summary>
        public void InvalidateCache()
        {
            m_CachedSpline?.Dispose();
            m_CachedSpline = null;
        }
    }


    /// <summary>
    /// In order to properly handle the spline scale, we need to cache a spline that incorporates the scale 
    /// natively.  This class does that.
    /// Be sure to call Dispose() before discarding this object, otherwise there will be memory leaks.
    /// </summary> 
    internal class CachedScaledSpline : ISpline, IDisposable
    {
        NativeSpline m_NativeSpline;
        readonly Spline m_CachedSource;
        //readonly float m_CachedRawLength;
        readonly Vector3 m_CachedScale;
        bool m_IsAllocated;

        /// <summary>Construct a CachedScaledSpline</summary>
        /// <param name="spline">The spline to cache</param>
        /// <param name="transform">The transform to use for the scale, or null</param>
        /// <param name="allocator">The allocator to use for the native spline</param>
        public CachedScaledSpline(Spline spline, Transform transform, Allocator allocator = Allocator.Persistent)
        {
            var scale = transform != null ? transform.lossyScale : Vector3.one;
            m_CachedSource = spline;
            m_NativeSpline = new NativeSpline(spline, Matrix4x4.Scale(scale), allocator);
            //m_CachedRawLength = spline.GetLength();
            m_CachedScale = scale;
            m_IsAllocated = true;
        }

        /// <inheritdoc/>
        public void Dispose() 
        { 
            if (m_IsAllocated) 
                m_NativeSpline.Dispose(); 
            m_IsAllocated = false;
        }

        /// <summary>Check if the cached spline is still valid, without doing any costly knot checks.</summary>
        /// <param name="spline">The source spline</param>
        /// <param name="transform">The source spline's transform, or null</param>
        /// <returns>True if the spline is crudely unchanged</returns>
        public bool IsCrudelyValid(Spline spline, Transform transform)
        {
            var scale = transform != null ? transform.lossyScale : Vector3.one;
            return spline == m_CachedSource && (m_CachedScale - scale).AlmostZero() 
                && m_NativeSpline.Count == m_CachedSource.Count
                //&& Mathf.Abs(m_CachedRawLength - spline.GetLength()) < 0.001f; // this would catch almost everything but is it too expensive?
                ;
        }

        /// <summary>Performs costly knot check to see if the spline's knots have changed.</summary>
        /// <param name="spline">The source spline</param>
        /// <param name="transform">The source spline's transform, or null</param>
        /// <returns>True if the knots have not changed</returns>
        public bool KnotsAreValid(Spline spline, Transform transform)
        {
            if (m_NativeSpline.Count != spline.Count)
                return false;

            var m = Matrix4x4.Scale(transform != null ? transform.lossyScale : Vector3.one);
            var ita = GetEnumerator();
            var itb = spline.GetEnumerator();
            while (ita.MoveNext() && itb.MoveNext())
                if (!ita.Current.Equals(itb.Current.Transform(m)))
                    return false;
            return true;
        }

        /// <inheritdoc/>
        public BezierKnot this[int index] => m_NativeSpline[index];
        /// <inheritdoc/>
        public bool Closed => m_NativeSpline.Closed;
        /// <inheritdoc/>
        public int Count => m_NativeSpline.Count;
        /// <inheritdoc/>
        public BezierCurve GetCurve(int index) => m_NativeSpline.GetCurve(index);
        /// <inheritdoc/>
        public float GetCurveInterpolation(int curveIndex, float curveDistance) => m_NativeSpline.GetCurveInterpolation(curveIndex, curveDistance);
        /// <inheritdoc/>
        public float GetCurveLength(int index) => m_NativeSpline.GetCurveLength(index);
#if CINEMACHINE_SPLINES_2_4
        /// <inheritdoc/>
        public float3 GetCurveUpVector(int index, float t) => m_NativeSpline.GetCurveUpVector(index, t);
#endif
        /// <inheritdoc/>
        public IEnumerator<BezierKnot> GetEnumerator() => m_NativeSpline.GetEnumerator();
        /// <inheritdoc/>
        public float GetLength() => m_NativeSpline.GetLength();
        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => m_NativeSpline.GetEnumerator();
    }
}
