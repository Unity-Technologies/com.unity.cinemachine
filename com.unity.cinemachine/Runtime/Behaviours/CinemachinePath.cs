using UnityEngine;
using System;
using Cinemachine.Utility;

namespace Cinemachine
{
    /// <summary>Defines a world-space path, consisting of an array of waypoints,
    /// each of which has position, tangent, and roll settings.  Bezier interpolation
    /// is performed between the waypoints, to get a smooth and continuous path.</summary>
    [DocumentationSorting(DocumentationSortingAttribute.Level.UserRef)]
    [AddComponentMenu("Cinemachine/CinemachinePath")]
    [SaveDuringPlay]
    [DisallowMultipleComponent]
    [HelpURL(Documentation.BaseURL + "manual/CinemachinePath.html")]
    public class CinemachinePath : CinemachinePathBase
    {
        /// <summary>A waypoint along the path</summary>
        [DocumentationSorting(DocumentationSortingAttribute.Level.UserRef)]
        [Serializable] public struct Waypoint
        {
            /// <summary>Position in path-local space</summary>
            [Tooltip("Position in path-local space")]
            public Vector3 position;

            /// <summary>Offset from the position, which defines the tangent of the curve at the waypoint.  
            /// The length of the tangent encodes the strength of the bezier handle.  
            /// The same handle is used symmetrically on both sides of the waypoint, to ensure smoothness.</summary>
            [Tooltip("Offset from the position, which defines the tangent of the curve at the waypoint.  "
                + "The length of the tangent encodes the strength of the bezier handle.  The same handle "
                + "is used symmetrically on both sides of the waypoint, to ensure smoothness.")]
            public Vector3 tangent;

            /// <summary>Defines the roll of the path at this waypoint.  
            /// The other orientation axes are inferred from the tangent and world up.</summary>
            [Tooltip("Defines the roll of the path at this waypoint.  The other orientation axes are inferred from the tangent and world up.")]
            public float roll;
        }

        /// <summary>If checked, then the path ends are joined to form a continuous loop</summary>
        [Tooltip("If checked, then the path ends are joined to form a continuous loop.")]
        public bool m_Looped;

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

        private void Reset()
        {
            m_Looped = false;
            m_Waypoints = new Waypoint[2]
            {
                new Waypoint { position = new Vector3(0, 0, -5), tangent = new Vector3(1, 0, 0) },
                new Waypoint { position = new Vector3(0, 0, 5), tangent = new Vector3(1, 0, 0) }
            };
            m_Appearance = new Appearance();
            InvalidateDistanceCache();
        }

        private void OnValidate() { InvalidateDistanceCache(); }

        /// <summary>When calculating the distance cache, sample the path this many 
        /// times between points</summary>
        public override int DistanceCacheSampleStepsPerSegment => m_Resolution; 

        /// <summary>Returns normalized position</summary>
        float GetBoundingIndices(float pos, out int indexA, out int indexB)
        {
            pos = StandardizePos(pos);
            int rounded = Mathf.RoundToInt(pos);
            if (Mathf.Abs(pos - rounded) < UnityVectorExtensions.Epsilon)
                indexA = indexB = (rounded == m_Waypoints.Length) ? 0 : rounded;
            else
            {
                indexA = Mathf.FloorToInt(pos);
                if (indexA >= m_Waypoints.Length)
                {
                    pos -= MaxPos;
                    indexA = 0;
                }
                indexB = Mathf.CeilToInt(pos);
                if (indexB >= m_Waypoints.Length)
                    indexB = 0;
            }
            return pos;
        }

        /// <summary>Get a worldspace position of a point along the path</summary>
        /// <param name="pos">Postion along the path.  Need not be normalized.</param>
        /// <returns>Local-space position of the point along at path at pos</returns>
        public override Vector3 EvaluateLocalPosition(float pos)
        {
            var result = Vector3.zero;
            if (m_Waypoints.Length > 0)
            {
                pos = GetBoundingIndices(pos, out var indexA, out var indexB);
                if (indexA == indexB)
                    result = m_Waypoints[indexA].position;
                else
                {
                    // interpolate
                    var wpA = m_Waypoints[indexA];
                    var wpB = m_Waypoints[indexB];
                    result = SplineHelpers.Bezier3(pos - indexA,
                        m_Waypoints[indexA].position, wpA.position + wpA.tangent,
                        wpB.position - wpB.tangent, wpB.position);
                }
            }
            return result;
        }

        /// <summary>Get the tangent of the curve at a point along the path.</summary>
        /// <param name="pos">Postion along the path.  Need not be normalized.</param>
        /// <returns>Local-space direction of the path tangent.
        /// Length of the vector represents the tangent strength</returns>
        public override Vector3 EvaluateLocalTangent(float pos)
        {
            var result = Vector3.forward;
            if (m_Waypoints.Length > 0)
            {
                pos = GetBoundingIndices(pos, out var indexA, out var indexB);
                if (indexA == indexB)
                    result = m_Waypoints[indexA].tangent;
                else
                {
                    var wpA = m_Waypoints[indexA];
                    var wpB = m_Waypoints[indexB];
                    result = SplineHelpers.BezierTangent3(pos - indexA,
                        m_Waypoints[indexA].position, wpA.position + wpA.tangent,
                        wpB.position - wpB.tangent, wpB.position);
                }
            }
            return result;
        }

        /// <summary>Get the orientation the curve at a point along the path.</summary>
        /// <param name="pos">Postion along the path.  Need not be normalized.</param>
        /// <returns>Local-space orientation of the path, as defined by tangent, up, and roll.</returns>
        public override Quaternion EvaluateLocalOrientation(float pos)
        {
            var result = Quaternion.identity;
            if (m_Waypoints.Length > 0)
            {
                pos = GetBoundingIndices(pos, out var indexA, out var indexB);
                var fwd = EvaluateLocalTangent(pos);
                if (!fwd.AlmostZero())
                    result = Quaternion.LookRotation(fwd) * RollAroundForward(GetRoll(indexA, indexB, pos));
            }
            return result;
        }

        internal float GetRoll(int indexA, int indexB, float standardizedPos)
        {
            if (indexA == indexB)
                return m_Waypoints[indexA].roll;
            float rollA = m_Waypoints[indexA].roll;
            float rollB = m_Waypoints[indexB].roll;
            if (indexB == 0)
            {
                // Special handling at the wraparound - cancel the spins
                rollA %= 360;
                rollB %= 360;
            }
            return Mathf.Lerp(rollA, rollB, standardizedPos - indexA);
        }

        // same as Quaternion.AngleAxis(roll, Vector3.forward), just simplified
        static Quaternion RollAroundForward(float angle)
        {
            float halfAngle = angle * 0.5F * Mathf.Deg2Rad;
            return new Quaternion(0, 0, Mathf.Sin(halfAngle), Mathf.Cos(halfAngle));
        }
    }
}
