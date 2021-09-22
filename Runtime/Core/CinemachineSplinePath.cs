using UnityEngine;
using UnityEngine.Splines;
using System.Collections.Generic;
using Unity.Mathematics;

namespace Cinemachine
{
    /// <summary>Defines a world-space path, consisting of an array of knots,
    /// each of which has position setting.  Different kinds of interpolation
    /// can be chose from to perform between the waypoints to get a continuous path. </summary>
    [DocumentationSorting(DocumentationSortingAttribute.Level.UserRef)]
    [AddComponentMenu("Cinemachine/CinemachineSplinePath")]
    [SaveDuringPlay]
    [DisallowMultipleComponent]
    [ExecuteInEditMode]
    public sealed class CinemachineSplinePath : CinemachinePathBase, ISplineProvider
    {
        readonly Spline[] m_SplineArray = new Spline[1];

        [SerializeField]
        Spline m_Spline = new Spline();

        public Spline Spline
        {
            get => m_Spline;
            set => m_Spline = value;
        }

        IEnumerable<Spline> ISplineProvider.Splines
        {
            get
            {
                m_SplineArray[0] = Spline;
                return m_SplineArray;
            }
        }

        /// <summary>The minimum value for the path position</summary>
        public override float MinPos { get { return 0; } }

        /// <summary>The maximum value for the path position</summary>
        public override float MaxPos
        {
            get
            {
                int count = Spline.KnotCount - 1;
                if (count < 1)
                    return 0;
                return Spline.Closed ? count + 1 : count;
            }
        }
        /// <summary>True if the path ends are joined to form a continuous loop</summary>
        public override bool Looped { get { return Spline.Closed; } }

        /// <summary>When calculating the distance cache, sample the path this many 
        /// times between points</summary>
        public override int DistanceCacheSampleStepsPerSegment { get { return m_Resolution; } }

        void OnValidate() { InvalidateDistanceCache(); }

        void Awake()
        {
            m_Spline.changed += InvalidateDistanceCache;
        }

        void Reset()
        {
            m_Spline = new Spline();
            m_Spline.AddKnot(new BezierKnot { Position = new float3(0, 0, -5), Rotation = quaternion.identity });
            m_Spline.AddKnot(new BezierKnot { Position = new float3(0, 0, 5), Rotation = quaternion.identity });
            m_Appearance = new Appearance();
            InvalidateDistanceCache();
        }

        /// <summary>Call this if the path changes in such a way as to affect distances
        /// or other cached path elements</summary>
        public override void InvalidateDistanceCache()
        {
            base.InvalidateDistanceCache();
        }

        /// <summary>Returns standardized position</summary>
        float GetBoundingIndices(float pos, out int indexA, out int indexB)
        {
            pos = StandardizePos(pos);
            var waypointCount = Spline.KnotCount;
            if (waypointCount < 2)
                indexA = indexB = 0;
            else
            {
                indexA = Mathf.FloorToInt(pos);
                if (indexA >= waypointCount)
                {
                    // Only true if looped
                    pos -= MaxPos;
                    indexA = 0;
                }
                indexB = indexA + 1;
                if (indexB == waypointCount)
                {
                    if (Looped)
                        indexB = 0;
                    else
                    {
                        --indexB;
                        --indexA;
                    }
                }
            }
            return pos;
        }

        /// <summary>Get a worldspace position of a point along the path</summary>
        /// <param name="pos">Position along the path.  Need not be normalized.</param>
        /// <returns>World-space position of the point along at path at pos</returns>
        public override Vector3 EvaluatePosition(float pos)
        {
            var result = Vector3.zero;
            if (Spline.KnotCount > 0)
            {
                pos = GetBoundingIndices(pos, out var indexA, out var indexB);
                if (indexA == indexB)
                    result = CurveUtility.EvaluatePosition(Spline.GetCurve(indexA), 0);
                else
                    result = CurveUtility.EvaluatePosition(Spline.GetCurve(indexA), pos - indexA);
            }
            
            return transform.TransformPoint(result);
        }

        /// <summary>Get the tangent of the curve at a point along the path.</summary>
        /// <param name="pos">Position along the path.  Need not be normalized.</param>
        /// <returns>World-space direction of the path tangent.
        /// Length of the vector represents the tangent strength</returns>
        public override Vector3 EvaluateTangent(float pos)
        {
            var result = transform.rotation * Vector3.forward;
            if (Spline.KnotCount > 1)
            {
                pos = GetBoundingIndices(pos, out var indexA, out var indexB);
                if (indexA == indexB)
                    result = CurveUtility.EvaluateTangent(Spline.GetCurve(indexA), 0);
                else
                    result = CurveUtility.EvaluateTangent(Spline.GetCurve(indexA), pos - indexA);
            }
            
            return transform.TransformDirection(result);
        }

        // <summary>Get the orientation of the curve at a point along the path.</summary>
        /// <param name="pos">Position along the path.  Need not be normalized.</param>
        /// <returns>World-space orientation of the path tangent.</returns>
        public override Quaternion EvaluateOrientation(float pos)
        {
            if (Spline.KnotCount > 1)
            {
                pos = GetBoundingIndices(pos, out var indexA, out var indexB);
                return transform.rotation * Quaternion.Slerp(Spline[indexA].Rotation, Spline[indexB].Rotation, pos - indexA);
            }
            return Spline.KnotCount == 1 ? transform.rotation * Spline[0].Rotation : Quaternion.identity;
        }

    }
}
