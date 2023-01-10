using UnityEngine;
using System;
using Cinemachine.Utility;

namespace Cinemachine
{
    /// <summary>Defines a world-space path, consisting of an array of waypoints,
    /// each of which has position and roll settings.  Bezier interpolation
    /// is performed between the waypoints, to get a smooth and continuous path.
    /// The path will pass through all waypoints, and (unlike CinemachinePath) first 
    /// and second order continuity is guaranteed</summary>
    [DocumentationSorting(DocumentationSortingAttribute.Level.UserRef)]
    [AddComponentMenu("Cinemachine/CinemachineSmoothPath")]
    [SaveDuringPlay]
    [DisallowMultipleComponent]
    [HelpURL(Documentation.BaseURL + "manual/CinemachineSmoothPath.html")]
    public class CinemachineSmoothPath : CinemachinePathBase
    {
        /// <summary>If checked, then the path ends are joined to form a continuous loop</summary>
        [Tooltip("If checked, then the path ends are joined to form a continuous loop.")]
        public bool m_Looped;

        /// <summary>A waypoint along the path</summary>
        [DocumentationSorting(DocumentationSortingAttribute.Level.UserRef)]
        [Serializable] public struct Waypoint
        {
            /// <summary>Position in path-local space</summary>
            [Tooltip("Position in path-local space")]
            public Vector3 position;

            /// <summary>Defines the roll of the path at this waypoint.  
            /// The other orientation axes are inferred from the tangent and world up.</summary>
            [Tooltip("Defines the roll of the path at this waypoint.  The other orientation axes are inferred from the tangent and world up.")]
            public float roll;

            /// <summary>Representation as Vector4</summary>
            internal Vector4 AsVector4
            {
                get { return new Vector4(position.x, position.y, position.z, roll); }
            }

            internal static Waypoint FromVector4(Vector4 v)
            {
                Waypoint wp = new Waypoint();
                wp.position = new Vector3(v[0], v[1], v[2]);
                wp.roll = v[3];
                return wp;
            }
        }

        /// <summary>The waypoints that define the path.
        /// They will be interpolated using a bezier curve</summary>
        [Tooltip("The waypoints that define the path.  They will be interpolated using a bezier curve.")]
        public Waypoint[] m_Waypoints = Array.Empty<Waypoint>();

        /// <summary>The minimum value for the path position</summary>
        public override float MinPos => 0;

        /// <summary>The maximum value for the path position</summary>
        public override float MaxPos
        {
            get
            {
                int count = m_Waypoints.Length - 1;
                if (count < 1)
                    return 0;
                return m_Looped ? count + 1 : count;
            }
        }
        /// <summary>True if the path ends are joined to form a continuous loop</summary>
        public override bool Looped => m_Looped;

        /// <summary>When calculating the distance cache, sample the path this many 
        /// times between points</summary>
        public override int DistanceCacheSampleStepsPerSegment => m_Resolution;

        private void OnValidate() { InvalidateDistanceCache(); }

        private void Reset()
        {
            m_Looped = false;
            m_Waypoints = new Waypoint[2]
            {
                new Waypoint { position = new Vector3(0, 0, -5) },
                new Waypoint { position = new Vector3(0, 0, 5) }
            };
            m_Appearance = new Appearance();
            InvalidateDistanceCache();
        }

        /// <summary>Call this if the path changes in such a way as to affect distances
        /// or other cached path elements</summary>
        public override void InvalidateDistanceCache()
        {
            base.InvalidateDistanceCache();
            m_ControlPoints1 = null;
            m_ControlPoints2 = null;
        }
        
        internal Waypoint[] m_ControlPoints1;
        internal Waypoint[] m_ControlPoints2;
        bool m_IsLoopedCache;

        internal void UpdateControlPoints()
        {
            int numPoints = (m_Waypoints == null) ? 0 : m_Waypoints.Length;
            if (numPoints > 1 
                && (Looped != m_IsLoopedCache
                    || m_ControlPoints1 == null || m_ControlPoints1.Length != numPoints
                    || m_ControlPoints2 == null || m_ControlPoints2.Length != numPoints))
            {
                Vector4[] p1 = new Vector4[numPoints];
                Vector4[] p2 = new Vector4[numPoints];
                Vector4[] K = new Vector4[numPoints];
                for (int i = 0; i < numPoints; ++i)
                    K[i] = m_Waypoints[i].AsVector4;
                if (Looped)
                    SplineHelpers.ComputeSmoothControlPointsLooped(ref K, ref p1, ref p2);
                else
                    SplineHelpers.ComputeSmoothControlPoints(ref K, ref p1, ref p2);

                m_ControlPoints1 = new Waypoint[numPoints];
                m_ControlPoints2 = new Waypoint[numPoints];
                for (int i = 0; i < numPoints; ++i)
                {
                    m_ControlPoints1[i] = Waypoint.FromVector4(p1[i]);
                    m_ControlPoints2[i] = Waypoint.FromVector4(p2[i]);
                }
                m_IsLoopedCache = Looped;
            }
        }

        /// <summary>Returns standardized position</summary>
        float GetBoundingIndices(float pos, out int indexA, out int indexB)
        {
            pos = StandardizePos(pos);
            int numWaypoints = m_Waypoints.Length;
            if (numWaypoints < 2)
                indexA = indexB = 0;
            else
            {
                indexA = Mathf.FloorToInt(pos);
                if (indexA >= numWaypoints)
                {
                    // Only true if looped
                    pos -= MaxPos;
                    indexA = 0;
                }
                indexB = indexA + 1;
                if (indexB == numWaypoints)
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
        public override Vector3 EvaluateLocalPosition(float pos)
        {
            var result = Vector3.zero;
            if (m_Waypoints.Length > 0)
            {
                UpdateControlPoints();
                pos = GetBoundingIndices(pos, out var indexA, out var indexB);
                if (indexA == indexB)
                    result = m_Waypoints[indexA].position;
                else
                    result = SplineHelpers.Bezier3(pos - indexA, 
                        m_Waypoints[indexA].position, m_ControlPoints1[indexA].position,
                        m_ControlPoints2[indexA].position, m_Waypoints[indexB].position);
            }
            return result;
        }

        /// <summary>Get the tangent of the curve at a point along the path.</summary>
        /// <param name="pos">Position along the path.  Need not be normalized.</param>
        /// <returns>World-space direction of the path tangent.
        /// Length of the vector represents the tangent strength</returns>
        public override Vector3 EvaluateLocalTangent(float pos)
        {
            var result = Vector3.forward;
            if (m_Waypoints.Length > 1)
            {
                UpdateControlPoints();
                pos = GetBoundingIndices(pos, out var indexA, out var indexB);
                if (!Looped && indexA == m_Waypoints.Length - 1)
                    --indexA;
                result = SplineHelpers.BezierTangent3(pos - indexA,
                    m_Waypoints[indexA].position, m_ControlPoints1[indexA].position,
                    m_ControlPoints2[indexA].position, m_Waypoints[indexB].position);
            }
            return result;
        }

        /// <summary>Get the orientation the curve at a point along the path.</summary>
        /// <param name="pos">Position along the path.  Need not be normalized.</param>
        /// <returns>World-space orientation of the path, as defined by tangent, up, and roll.</returns>
        public override Quaternion EvaluateLocalOrientation(float pos)
        {
            var result = Quaternion.identity;
            if (m_Waypoints.Length > 0)
            {
                float roll;
                pos = GetBoundingIndices(pos, out var indexA, out var indexB);
                if (indexA == indexB)
                    roll = m_Waypoints[indexA].roll;
                else
                {
                    UpdateControlPoints();
                    roll = SplineHelpers.Bezier1(pos - indexA,
                        m_Waypoints[indexA].roll, m_ControlPoints1[indexA].roll,
                        m_ControlPoints2[indexA].roll, m_Waypoints[indexB].roll);
                }

                Vector3 fwd = EvaluateTangent(pos);
                if (!fwd.AlmostZero())
                    result = Quaternion.LookRotation(fwd) * RollAroundForward(roll);
            }
            return result;
        }
        
        // same as Quaternion.AngleAxis(roll, Vector3.forward), just simplified
        static Quaternion RollAroundForward(float angle)
        {
            float halfAngle = angle * 0.5F * Mathf.Deg2Rad;
            return new Quaternion(0, 0, Mathf.Sin(halfAngle), Mathf.Cos(halfAngle));
        }
    }
}
