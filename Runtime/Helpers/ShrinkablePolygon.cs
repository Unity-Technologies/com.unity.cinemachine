using System;
using System.Collections.Generic;
using System.Linq;
using Cinemachine.Utility;
using ClipperLib;
using UnityEngine;
using UnityEngine.Assertions;

namespace Cinemachine
{
    /// <summary>
    /// ShrinkablePolygon represent a m_points with normals that can shrink down to it's skeleton.  
    /// </summary>
    internal class ShrinkablePolygon
    {
        internal class ShrinkablePoint2
        {
            public Vector2 m_position;
            public Vector2 m_shrinkDirection;
            public bool m_cantIntersect;

            internal ShrinkablePoint2()
            {
                m_position = Vector2.zero;
                m_shrinkDirection = Vector2.zero;
            }

            internal ShrinkablePoint2(Vector2 mPosition, Vector2 mShrinkDirection, bool mCantIntersect)
            {
                m_position = mPosition;
                m_shrinkDirection = mShrinkDirection;
                m_cantIntersect = mCantIntersect;
            }
        }

        internal List<ShrinkablePoint2> m_points;
        internal float m_windowDiagonal;
        internal int m_state;
        
        private bool m_clockwiseOrientation;
        private readonly float m_aspectRatio;
        private float m_aspectRatioBasedDiagonal;
        private readonly Vector2[] m_normalDirections;
        private float m_area;

        internal List<Vector2> m_intersectionPoints;

        public ShrinkablePolygon()
        {
            m_points = new List<ShrinkablePoint2>();
            m_intersectionPoints = new List<Vector2>();
        }

        public ShrinkablePolygon(List<Vector2> points, float aspectRatio) : this()
        {
            m_points = new List<ShrinkablePoint2>(points.Count);
            for (int i = 0; i < points.Count; ++i)
            {
                m_points.Add(new ShrinkablePoint2 { m_position = points[i] });
            }
            
            m_aspectRatio = aspectRatio;
            m_aspectRatioBasedDiagonal = Mathf.Sqrt(m_aspectRatio*m_aspectRatio + 1);
            m_normalDirections = new[]
            {
                Vector2.up,
                new Vector2(m_aspectRatio, 1),
                new Vector2(m_aspectRatio, 0),
                new Vector2(m_aspectRatio, -1),
                Vector2.down,
                new Vector2(-m_aspectRatio, -1),
                new Vector2(-m_aspectRatio, 0),
                new Vector2(-m_aspectRatio, 1),
            };
            
            ComputeNormals(true);
            ComputeSignedArea();
            if (!m_clockwiseOrientation)
            {
                FlipNormals();
                ComputeSignedArea();
            }
        }

        /// <summary>
        /// Creates and returns a deep copy of this shrinkablePolygon.
        /// </summary>
        /// <returns>Deep copy of this shrinkablePolygon</returns>
        public ShrinkablePolygon DeepCopy()
        {
            // this can be shallow
            return new ShrinkablePolygon(m_aspectRatio, m_aspectRatioBasedDiagonal, m_normalDirections)
            {
                // shallow
                m_clockwiseOrientation = m_clockwiseOrientation,
                m_area = m_area,
                m_windowDiagonal = m_windowDiagonal,
                m_state = m_state,
                
                // deep
                m_points = m_points.ConvertAll(point =>
                    new ShrinkablePoint2(point.m_position, point.m_shrinkDirection, point.m_cantIntersect)),
                m_intersectionPoints =
                    m_intersectionPoints.ConvertAll(intersection => new Vector2(intersection.x, intersection.y))
            };
        }
        
        private ShrinkablePolygon(float aspectRatio, float aspectRatioBasedDiagonal, Vector2[] normalDirections) : this()
        {
            m_aspectRatio = aspectRatio;
            m_aspectRatioBasedDiagonal = aspectRatioBasedDiagonal;
            m_normalDirections = normalDirections; // shallow copy is enough here
        }

        /// <summary>
        /// Computes signed m_area and determines whether a shrinkablePolygon is oriented clockwise or counter-clockwise.
        /// </summary>
        /// <returns>Area of the shrinkablePolygon</returns>
        private float ComputeSignedArea()
        {
            m_area = 0;
            for (int i = 0; i < m_points.Count; ++i)
            {
                var p1 = m_points[i];
                var p2 = m_points[(i + 1) % m_points.Count];

                m_area += (p2.m_position.x - p1.m_position.x) * (p2.m_position.y + p1.m_position.y);
            }

            m_clockwiseOrientation = m_area > 0;
            return m_area;
        }

        private static float m_oneOverSquarerootOfTwo = 0.70710678f;
        /// <summary>
        /// Computes normalized normals for all m_points. If fixBigCornerAngles is true, then adds additional m_points for corners
        /// with reflex angles to ensure correct offset
        /// </summary>
        /// <param name="fixBigCornerAngles"></param>
        private void ComputeNormals(bool fixBigCornerAngles)
        {
            // TODO: this is wrong - 1 place shift
            var edgeNormals = new List<Vector2>(m_points.Count);
            for (int i = 0; i < m_points.Count; ++i)
            {
                Vector2 edge = m_points[(i + 1) % m_points.Count].m_position - m_points[i].m_position;
                Vector2 normal = m_clockwiseOrientation ? new Vector2(edge.y, -edge.x) : new Vector2(-edge.y, edge.x); 
                edgeNormals.Add(normal.normalized);
            }

            // calculating normals
            for (int i = 0; i < m_points.Count; ++i)
            {
                int prevEdgeIndex = i == 0 ? edgeNormals.Count - 1 : i - 1;
                m_points[i].m_shrinkDirection = edgeNormals[i] + edgeNormals[prevEdgeIndex];
                m_points[i].m_shrinkDirection.Normalize();
            }

            if (fixBigCornerAngles)
            {
                // we need to fix corner angles, that are negative (bigger than 180 degrees)
                // fixing means that we add more shrink directions, because at this corners the offset from the
                // camera middle point can be different depending on which way the camera comes from
                // worst case: every point has negative angle (not possible in practise, but makes the algorithm simpler)
                // so all in all we will have 3 times as many points as before (1 + 2 extra for each point)
                // original points are placed with padding _ 0 _ _ 1 _ _ 2 _ _ ... 
                List<ShrinkablePoint2> extended_points = new ShrinkablePoint2[m_points.Count * 3].ToList();
                for (int i = 0; i < m_points.Count; ++i)
                {
                    extended_points[i * 3 + 1] = m_points[i];
                    
                    int prevEdgeIndex = i == 0 ? edgeNormals.Count - 1 : i - 1;
                    var angle = Vector2.SignedAngle(edgeNormals[i], edgeNormals[prevEdgeIndex]);
                    if (angle < 0)
                    {
                        int prevIndex = (i == 0 ? m_points.Count - 1 : i - 1);
                        extended_points[i * 3 + 0] = new ShrinkablePoint2
                        {
                            m_position = Vector2.Lerp(m_points[i].m_position, m_points[prevIndex].m_position, 0.01f),
                            m_shrinkDirection = m_points[i].m_shrinkDirection,
                            m_cantIntersect = true,
                        };
                        
                        int nextIndex = (i == m_points.Count - 1 ? 0 : i + 1);
                        extended_points[i * 3 + 2] = new ShrinkablePoint2
                        {
                            m_position = Vector2.Lerp(m_points[i].m_position, m_points[nextIndex].m_position, 0.01f),
                            m_shrinkDirection = m_points[i].m_shrinkDirection,
                            m_cantIntersect = true,
                        };
                    }
                }
                
                for (var index = extended_points.Count - 1; index >= 0; index--)
                {
                    if (extended_points[index] == null)
                    {
                        extended_points.RemoveAt(index);
                    }
                }

                m_points = extended_points;
            }
        }
    
        /// <summary>
        /// Computes normals that respect the aspect ratio of the camera. If the camera window is a square,
        /// then the normals will be the usual normals.
        /// This normals will determine the shrink direction of their m_points.
        /// </summary>
        internal void ComputeAspectBasedNormals()
        {
            var normalsBefore = new List<Vector2>(m_points.Count);
            for (int i = 0; i < m_points.Count; ++i)
            {
                normalsBefore.Add(m_points[i].m_shrinkDirection);
            }

            ComputeNormals(false);
            for (int i = 0; i < m_points.Count; ++i)
            {
                int prevIndex = i == 0 ? m_points.Count - 1 : i - 1;
                int nextIndex = i == m_points.Count - 1 ? 0 : i + 1;

                m_points[i].m_shrinkDirection = CalculateShrinkDirection(m_points[i].m_shrinkDirection, 
                    m_points[prevIndex].m_position, m_points[i].m_position, m_points[nextIndex].m_position);
            }

            if (normalsBefore.Count != m_points.Count)
            {
                m_state++; // m_state change if more m_points where added
            }
            else
            {
                for (var index = 0; index < normalsBefore.Count; index++)
                {
                    if (normalsBefore[index] != m_points[index].m_shrinkDirection)
                    {
                        m_state++; // m_state change when even one m_shrinkDirection has been changed
                        break;
                    }
                }
            }
        }
        
        private static readonly int FloatToIntScaler = 10000000; // same as in Physics2D
        internal static void ConvertToPath(in List<ShrinkablePolygon> shrinkablePolygons, out List<List<Vector2>> path)
        {
            // convert shrinkable polygons points to int based points for Clipper
            List<List<IntPoint>> clip = new List<List<IntPoint>>(shrinkablePolygons.Count);
            int index = 0;
            for (var polyIndex = 0; polyIndex < shrinkablePolygons.Count; polyIndex++)
            {
                var polygon = shrinkablePolygons[polyIndex];
                clip.Add(new List<IntPoint>(polygon.m_points.Count));
                foreach (var point in polygon.m_points)
                {
                    clip[index].Add(new IntPoint(point.m_position.x * FloatToIntScaler,
                        point.m_position.y * FloatToIntScaler));
                }
                index++;

                // add a thin line to each intersection point, thus connecting disconnected polygons
                foreach (var intersectionPoint in polygon.m_intersectionPoints)
                {
                    Vector2 closestPoint = polygon.ClosestGraphPoint(intersectionPoint);
                    Vector2 direction = (closestPoint - intersectionPoint).normalized;
                    Vector2 epsilonNormal = new Vector2(direction.y, -direction.x) * 0.01f;

                    clip.Add(new List<IntPoint>(4));
                    Vector2 p1 = closestPoint + epsilonNormal;
                    Vector2 p2 = intersectionPoint + epsilonNormal;
                    Vector3 p3 = intersectionPoint - epsilonNormal;
                    Vector3 p4 = closestPoint - epsilonNormal;

                    clip[index].Add(new IntPoint(p1.x * FloatToIntScaler, p1.y * FloatToIntScaler));
                    clip[index].Add(new IntPoint(p2.x * FloatToIntScaler, p2.y * FloatToIntScaler));
                    clip[index].Add(new IntPoint(p3.x * FloatToIntScaler, p3.y * FloatToIntScaler));
                    clip[index].Add(new IntPoint(p4.x * FloatToIntScaler, p4.y * FloatToIntScaler));

                    index++;
                }
            }

            // Merge polygons with Clipper
            List<List<IntPoint>> solution = new List<List<IntPoint>>();
            Clipper c = new Clipper();
            c.AddPaths(clip, PolyType.ptClip, true);
            c.Execute(ClipType.ctUnion, solution, 
                PolyFillType.pftEvenOdd, PolyFillType.pftEvenOdd);
            
            // Convert result to float points
            path = new List<List<Vector2>>(solution.Count);
            foreach (var polySegment in solution)
            {
                var pathSegment = new List<Vector2>(polySegment.Count);
                for (index = 0; index < polySegment.Count; index++)
                {
                    var p_int = polySegment[index];
                    var p = new Vector2(p_int.X / (float) FloatToIntScaler, p_int.Y / (float) FloatToIntScaler);
                    pathSegment.Add(p);
                }
                path.Add(pathSegment);
            }
        }

        // TODO: this could also be in UnityVectorExtensions?
        /// <summary>
        /// Checks whether p is inside or outside the polygons. The algorithm determines if a point is inside based
        /// on a horizontal raycast from p. If the ray intersects the polygon odd number of times, then p is inside.
        /// </summary>
        /// <param name="polygons"></param>
        /// <param name="p"></param>
        /// <returns>True, if inside. False, otherwise./returns>
        internal static bool IsInside(in List<List<Vector2>> polygons, in Vector2 p)
        {
            float minX = Single.PositiveInfinity;
            float maxX = Single.NegativeInfinity;
            foreach (var path in polygons)
            {
                minX = path.Aggregate(minX, (current, point) => Mathf.Min(current, point.x));
                maxX = path.Aggregate(maxX, (current, point) => Mathf.Max(current, point.x));
            }
            var polygonXWidth = maxX - minX;
            
            int intersectionCount = 0;
            var camRayEndFromCamPos2D = p + Vector2.right * polygonXWidth;
            foreach (var polygon in polygons)
            {
                for (var index = 0; index < polygon.Count; index++)
                {
                    var p1 = polygon[index];
                    var p2 = polygon[(index + 1) % polygon.Count];
                    UnityVectorExtensions.FindIntersection(p, camRayEndFromCamPos2D, p1, p2,
                        out bool lines_intersect, out bool segments_intersect, out Vector2 intersection);
                    if (segments_intersect)
                    {
                        intersectionCount++;
                    }
                }
            }

            return intersectionCount % 2 != 0; // inside polygon when odd num of intersections
        }
        

        // TODO: this could also be in UnityVectorExtensions?
        /// <summary>
        /// Finds midpoint of a rectangle's side touching CA and CB.
        /// D1 - D2 defines the side or diagonal of a rectangle touching CA and CB.
        /// </summary>
        /// <returns></returns>
        private Vector2 FindMidPoint(Vector2 A, Vector2 B, Vector2 C, Vector2 D1, Vector2 D2)
        {
            Vector2 CA = (A - C);
            Vector2 CB = (B - C);

            var gamma = UnityVectorExtensions.Angle(CA, CB);
            if (gamma <= 0.05f || 179.95f <= gamma) 
            { 
                return (A + B) / 2; // too narrow angle, so just return the mid point
            }
            var D1D2 = D1 - D2;
            var D1C = C - B;
            var beta = UnityVectorExtensions.Angle(D1C, D1D2);
            var D2D1 = D2 - D1;
            var D2C = C - A;
            var alpha = UnityVectorExtensions.Angle(D2C, D2D1);
            if (Math.Abs(gamma + beta + alpha - 180) > 0.5f)
            {
                D1D2 = D2 - D1;
                D1C = C - B;
                beta = UnityVectorExtensions.Angle(D1C, D1D2);
                D2D1 = D1 - D2;
                D2C = C - A;
                alpha = UnityVectorExtensions.Angle(D2C, D2D1);
            }
            if (alpha <= 0.05f || 179.95f <= alpha || 
                beta <= 0.05f || 179.95f <= beta)
            {
                return (A + B) / 2; // too narrow angle, so just return the mid point
            }

            var c = D1D2.magnitude;
            var a = (c / Mathf.Sin(gamma * Mathf.Deg2Rad)) * Mathf.Sin(alpha * Mathf.Deg2Rad);
            var b = (c / Mathf.Sin(gamma * Mathf.Deg2Rad)) * Mathf.Sin(beta * Mathf.Deg2Rad);

            var M1 = C + CB.normalized * Mathf.Abs(a);
            var M2 = C + CA.normalized * Mathf.Abs(b);

            var dist1 = (A + B) / 2 - C;
            var dist2 = (M1 + M2) / 2 - C;
            if (dist1.sqrMagnitude < dist2.sqrMagnitude)
            {
                return (A + B) / 2;
            }
            
            return (M1 + M2) / 2; // midpoint
        }
        
        /// <summary>
        /// Calculates shrink direction for thisPoint, based on it's normal and neighbouring points.
        /// </summary>
        /// <param name="normal">normal of thisPoint</param>
        /// <param name="prevPoint">previous neighbouring of thisPoint</param>
        /// <param name="thisPoint"></param>
        /// <param name="nextPoint">next neighbouring of thisPoint</param>
        /// <returns>Returns direction for thisPoint</returns>
        private Vector2 CalculateShrinkDirection(Vector2 normal, Vector2 prevPoint, Vector2 thisPoint, Vector2 nextPoint)
        {
            var A = prevPoint;
            var B = nextPoint;
            var C = thisPoint;

            Vector2 CA = (A - C);
            Vector2 CB = (B - C);
            
            var angle1_abs = Vector2.Angle(CA, normal);
            var angle2_abs = Vector2.Angle(CB, normal);
            
            Vector2 R = normal.normalized * m_aspectRatioBasedDiagonal;
            float angle = Vector2.SignedAngle(R, m_normalDirections[0]);
            if (0 < angle && angle < 90)
            {
                if (angle - angle1_abs <= 1f && 89 <= angle + angle2_abs)
                {
                    // case 0 - 1 point intersection with camera window
                    R = m_normalDirections[1];
                }
                else if (angle - angle1_abs <= 0 && angle + angle2_abs < 90)
                {
                    // case 1a - 2 point intersection with camera window's bottom
                    var M = FindMidPoint(A, B, C, m_normalDirections[3], m_normalDirections[5]); // bottom side's midpoint
                    var rectangleMidPoint = M + m_normalDirections[0]; // rectangle's midpoint
                    R = rectangleMidPoint - C;
                }
                else if (0 < angle - angle1_abs && 90 <= angle + angle2_abs)
                {
                    // case 1b - 2 point intersection with camera window's left side
                    var M = FindMidPoint(A, B, C, m_normalDirections[7], m_normalDirections[5]); // left side's midpoint
                    var rectangleMidPoint = M + m_normalDirections[2]; // rectangle's midpoint
                    R = rectangleMidPoint - C;
                }
                else if (0 < angle - angle1_abs && angle + angle2_abs < 90)
                {
                    // case 2 - 2 point intersection with camera window's diagonal (top-left to bottom-right)
                    var rectangleMidPoint = FindMidPoint(A, B, C, m_normalDirections[3], m_normalDirections[7]); // rectangle's midpoint
                    R = rectangleMidPoint - C;
                }
                else
                {
                    Debug.Log("Error in CalculateShrinkDirection - Let us know on the Cinemachine forum please!"); // should never happen
                }
            }
            else if (90 < angle && angle < 180)
            {
                if (angle - angle1_abs <= 91 && 179 <= angle + angle2_abs)
                {
                    // case 0 - 1 point intersection with camera window
                    R = m_normalDirections[3];
                }
                else if (angle - angle1_abs <= 90 && angle + angle2_abs < 180)
                {
                    // case 1a - 2 point intersection with camera window's left
                    var M = FindMidPoint(A, B, C, m_normalDirections[0], m_normalDirections[4]); // left side's midpoint
                    var rectangleMidPoint = M + m_normalDirections[2]; // rectangle's midpoint
                    R = rectangleMidPoint - C;
                }
                else if (90 < angle - angle1_abs && 180 <= angle + angle2_abs)
                {
                    // case 1b - 2 point intersection with camera window's top side
                    var M = FindMidPoint(A, B, C, m_normalDirections[1], m_normalDirections[7]); // top side's midpoint
                    var rectangleMidPoint = M + m_normalDirections[4]; // rectangle's midpoint
                    R = rectangleMidPoint - C;
                }
                else if (90 < angle - angle1_abs && angle + angle2_abs < 180)
                {
                    // case 2 - 2 point intersection with camera window's diagonal (top-right to bottom-left)
                    var rectangleMidPoint = FindMidPoint(A, B, C, m_normalDirections[1], m_normalDirections[5]); // rectangle's midpoint
                    R = rectangleMidPoint - C;
                }
                else
                {
                    Debug.Log("Error in CalculateShrinkDirection - Let us know on the Cinemachine forum please!"); // should never happen
                }
            }
            else if (-180 < angle && angle < -90)
            {
                if (angle - angle1_abs <= -179 && -91 <= angle + angle2_abs)
                {
                    // case 0 - 1 point intersection with camera window
                    R = m_normalDirections[5];
                }
                else if (angle - angle1_abs <= -180 && angle + angle2_abs < -90)
                {
                    // case 1a - 2 point intersection with camera window's top
                    var M = FindMidPoint(A, B, C, m_normalDirections[7], m_normalDirections[1]); // top side's midpoint
                    var rectangleMidPoint = M + m_normalDirections[4]; // rectangle's midpoint
                    R = rectangleMidPoint - C;
                }
                else if (-180 < angle - angle1_abs && -90 <= angle + angle2_abs)
                {
                    // case 1b - 2 point intersection with camera window's right side
                    var M = FindMidPoint(A, B, C, m_normalDirections[1], m_normalDirections[3]); // right side's midpoint
                    var rectangleMidPoint = M + m_normalDirections[6]; // rectangle's midpoint
                    R = rectangleMidPoint - C;
                }
                else if (-180 < angle - angle1_abs && angle + angle2_abs < -90)
                {
                    // case 2 - 2 point intersection with camera window's diagonal (top-left to bottom-right)
                    var rectangleMidPoint = FindMidPoint(A, B, C, m_normalDirections[3], m_normalDirections[7]); // rectangle's midpoint
                    R = rectangleMidPoint - C;
                }
                else
                {
                    Debug.Log("Error in CalculateShrinkDirection - Let us know on the Cinemachine forum please!"); // should never happen
                }
            }
            else if (-90 < angle && angle < 0)
            {
                if (angle - angle1_abs <= -89 && -1 <= angle + angle2_abs)
                {
                    // case 0 - 1 point intersection with camera window
                    R = m_normalDirections[7];
                }
                else if (angle - angle1_abs <= -90 && angle + angle2_abs < 0)
                {
                    // case 1a - 2 point intersection with camera window's right side
                    var M = FindMidPoint(A, B, C, m_normalDirections[7], m_normalDirections[5]); // right side's midpoint
                    var rectangleMidPoint = M + m_normalDirections[6]; // rectangle's midpoint
                    R = rectangleMidPoint - C;
                }
                else if (-90 < angle - angle1_abs && 0 <= angle + angle2_abs)
                {
                    // case 1b - 2 point intersection with camera window's bottom side
                    var M = FindMidPoint(A, B, C, m_normalDirections[5], m_normalDirections[3]); // bottom side's mid point
                    var rectangleMidPoint = M + m_normalDirections[0]; // rectangle's midpoint
                    R = rectangleMidPoint - C;
                }
                else if (-90 < angle - angle1_abs && angle + angle2_abs < 0)
                {
                    // case 2 - 2 point intersection with camera window's diagonal (top-right to bottom-left)
                    var rectangleMidPoint = FindMidPoint(A, B, C, m_normalDirections[1], m_normalDirections[5]); // rectangle's midpoint
                    R = rectangleMidPoint - C;
                }
                else
                {
                    Debug.Log("Error in CalculateShrinkDirection - Let us know on the Cinemachine forum please!"); // should never happen
                }
            }
            else
            {
                R.x = Mathf.Clamp(R.x, -m_aspectRatio, m_aspectRatio);
                R.y = Mathf.Clamp(R.y, -1, 1);
            }

            return R;
        }


        /// <summary>
        /// Flips normals in the shrinkablePolygon.
        /// </summary>
        private void FlipNormals()
        {
            for (int i = 0; i < m_points.Count; ++i)
            {
                m_points[i].m_shrinkDirection = -m_points[i].m_shrinkDirection;
            }
        }

        /// <summary>
        /// ShrinkablePolygon is shrinkable if it has at least one non-zero m_shrinkDirection.
        /// </summary>
        /// <returns>True, if shrinkablePolygon is shrinkable. False, otherwise.</returns>
        internal bool IsShrinkable()
        {
            for (int i = 0; i < m_points.Count; ++i)
            {
                if (m_points[i].m_shrinkDirection != Vector2.zero)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Shrink graphs m_points towards their m_shrinkDirection by shrinkAmount.
        /// </summary>
        internal bool Shrink(float shrinkAmount)
        {
            //if (shrinkBonesToPoint)
            // {
            //     var minX = float.PositiveInfinity;
            //     var minY = float.PositiveInfinity;
            //     var maxX = float.NegativeInfinity;
            //     var maxY = float.NegativeInfinity;
            //     for (int i = 0; i < m_points.Count; ++i)
            //     {
            //         minX = Mathf.Min(m_points[i].m_position.x, minX);
            //         minY = Mathf.Min(m_points[i].m_position.y, minY);
            //         maxX = Mathf.Max(m_points[i].m_position.x, maxX);
            //         maxY = Mathf.Max(m_points[i].m_position.y, maxY);
            //     }
            //
            //     bool normalsTowardsCenter = false;
            //     bool normalsXZero = false;
            //     bool normalsYZero = false;
            //     if (Math.Abs(maxX - minX) < 1f)
            //     {
            //         for (int i = 0; i < m_points.Count; ++i)
            //         {
            //             m_points[i].m_shrinkDirection.x = 0;
            //             normalsXZero = true;
            //         }
            //     }
            //
            //     if (Math.Abs(maxY - minY) < 1f)
            //     {
            //         for (int i = 0; i < m_points.Count; ++i)
            //         {
            //             m_points[i].m_shrinkDirection.y = 0;
            //             normalsYZero = true;
            //         }
            //     }
            //
            //     if (normalsXZero && SetZeroNormalsXdirection())
            //     {
            //         return false;
            //     }
            //
            //     if (normalsYZero && SetZeroNormalsYdirection())
            //     {
            //         return false;
            //     }
            //
            //     bool allNormalsAreNonZero = false;
            //     for (int i = 0; i < m_points.Count; ++i)
            //     {
            //         if (m_points[i].m_shrinkDirection.sqrMagnitude > UnityVectorExtensions.Epsilon)
            //         {
            //             allNormalsAreNonZero = true;
            //         }
            //         else
            //         {
            //             m_points[i].m_shrinkDirection = Vector2.zero;
            //         }
            //     }
            //
            //     if (!allNormalsAreNonZero)
            //     {
            //         if (!normalsXZero)
            //         {
            //             normalsXZero = true;
            //             SetZeroNormalsXdirection();
            //         }
            //
            //         if (!normalsYZero)
            //         {
            //             normalsYZero = true;
            //             SetZeroNormalsYdirection();
            //         }
            //     }
            //
            //     if (normalsXZero && normalsYZero)
            //     {
            //         return false;
            //     }
            //
            //     ComputeSignedArea();
            //     if (!normalsXZero && !normalsYZero && Mathf.Abs(m_area) > 0.5f && Mathf.Abs(m_area) < 2f)
            //     {
            //         normalsTowardsCenter = true;
            //         Vector2 center = new Vector2((minX + maxX) / 2f, (minY + maxY) / 2f);
            //         for (int i = 0; i < m_points.Count; ++i)
            //         {
            //             m_points[i].m_shrinkDirection = CalculateShrinkDirection(center - m_points[i].m_position);
            //         }
            //         Simplify();
            //     }
            //     if (normalsTowardsCenter && SetNormalDirectionTowardsCenter())
            //     {
            //         return false;
            //     }
            // }
             m_windowDiagonal += shrinkAmount;
            // TODO: optimize shrink - shrink until intersection instead of steps
            float area1 = Mathf.Abs(ComputeSignedArea());
            if (area1 < 1.3f)
            {
                for (int i = 0; i < m_points.Count; ++i)
                {
                    m_points[i].m_shrinkDirection = Vector2.zero;
                }

                return false;
            }
            for (int i = 0; i < m_points.Count; ++i)
            {
                m_points[i].m_position += m_points[i].m_shrinkDirection * shrinkAmount;
            }
            float area2 = Mathf.Abs(ComputeSignedArea());
            if (area2 > area1)
            {
                FlipNormals();
                for (int i = 0; i < m_points.Count; ++i)
                {
                    m_points[i].m_position += m_points[i].m_shrinkDirection * (shrinkAmount * 2f); // why 2?
                }
            }
            float area3 = Mathf.Abs(ComputeSignedArea());
            if (area3 > area2 || area1 < 0.02f ||
                area1 < area2 && area1 < area3)
            {
                FlipNormals();
                for (int i = 0; i < m_points.Count; ++i)
                {
                    m_points[i].m_position += m_points[i].m_shrinkDirection * (shrinkAmount);
                    m_points[i].m_shrinkDirection = Vector2.zero;
                }
            }
            return true;
        }

        /// <summary>
        /// Calculates quared distance to 'P' from closest point to 'P' in the shrinkablePolygon
        /// </summary>
        /// <param name="p">Point in space.</param>
        /// <returns>Squared distance to 'P' from closest point to 'P' in the shrinkablePolygon</returns>
        private float SqrDistanceTo(Vector2 p)
        {
            float minDistance = float.MaxValue;
            for (int i = 0; i < m_points.Count; ++i)
            {
                minDistance = Mathf.Min(minDistance, (m_points[i].m_position - p).sqrMagnitude);
            }

            return minDistance;
        }

        /// <summary>
        /// Calculates the closest point to the shrinkablePolygon from P.
        /// The point returned is going to be one of the m_points of the shrinkablePolygon.
        /// </summary>
        /// <param name="p">Point from which the distance is calculated.</param>
        /// <returns>A point that is part of the shrinkablePolygon m_points and is closest to P.</returns>
        internal Vector2 ClosestGraphPoint(Vector2 p)
        {
            float minDistance = float.MaxValue;
            Vector2 closestPoint = Vector2.zero;
            for (int i = 0; i < m_points.Count; ++i)
            {
                float sqrDistance = (m_points[i].m_position - p).sqrMagnitude;
                if (minDistance > sqrDistance)
                {
                    minDistance = sqrDistance;
                    closestPoint = m_points[i].m_position;
                }
            }

            return closestPoint;
        }
        
        /// <summary>
        /// Returns point closest to p that is a point of the shrinkablePolygon.
        /// </summary>
        /// <param name="p"></param>
        /// <returns>Closest point to p in ShrinkablePolygon</returns>
        internal Vector2 ClosestGraphPoint(ShrinkablePoint2 p)
        {
            bool foundWithNormal = false;
            float minDistance = float.MaxValue;
            Vector2 closestPoint = Vector2.zero;
            for (int i = 0; i < m_points.Count; ++i)
            {
                var diff = m_points[i].m_position - p.m_position;
                var angle = Vector2.Angle(p.m_shrinkDirection, diff);
                if (angle < 5 || 175 < angle)
                {
                    foundWithNormal = true;
                    float sqrDistance = diff.sqrMagnitude;
                    if (minDistance > sqrDistance)
                    {
                        minDistance = sqrDistance;
                        closestPoint = m_points[i].m_position;
                    }
                }
            }
            if (foundWithNormal)
            {
                return closestPoint;
            }

            for (int i = 0; i < m_points.Count; ++i)
            {
                var diff = m_points[i].m_position - p.m_position;
                float sqrDistance = diff.sqrMagnitude;
                if (minDistance > sqrDistance)
                {
                    minDistance = sqrDistance;
                    closestPoint = m_points[i].m_position;
                }
            }
            return closestPoint;
        }

        /// <summary>
        /// Removes m_points that are the same or very close.
        /// </summary>
        internal void Simplify()
        {
            // TODO: remove goto with other function 
            if (m_points.Count <= 4)
            {
                return;
            }

            var canSimplify = true;
            while (canSimplify)
            {
                canSimplify = false;
                for (int i = 0; i < m_points.Count; ++i)
                {
                    // for (int j = i + 1; j < m_points.Count; ++j)
                    // just check adjacent points
                    int j = (i + 1) % m_points.Count;
                    {
                        if (!m_points[i].m_cantIntersect && !m_points[j].m_cantIntersect) continue;
                        if (m_points[i].m_cantIntersect && m_points[j].m_cantIntersect) continue;
                        if ((m_points[i].m_position - m_points[j].m_position).sqrMagnitude <= 0.01f)
                        {
                            if (m_points[i].m_cantIntersect)
                            {
                                m_points.RemoveAt(i);
                            }
                            else if (m_points[j].m_cantIntersect)
                            {
                                m_points.RemoveAt(j);
                            }
                            else
                            {
                                m_points.RemoveAt(j);
                                m_points.RemoveAt(i);
                            }

                            canSimplify = true;
                            goto CONTINUE_WHILE;
                        }
                    }
                }

                CONTINUE_WHILE: ;
            }
        }

        // TODO: refine summary outside and within this function - DivideGraph and also DivideAlongIntersections
        /// <summary>Divides shrinkablePolygon into subgraphs if there are intersections.</summary>
        /// <param name="shrinkablePolygon">ShrinkablePolygon to divide. ShrinkablePolygon will be overwritten by a shrinkablePolygon with possible intersections,
        /// after cutting off the shrinkablePolygon part 'left' of the intersection.</param>
        /// <param name="subgraphs">Resulting subgraphs from dividing shrinkablePolygon.</param>
        /// <returns>True, if found intersection. False, otherwise.</returns>
        private static bool DivideGraph(ref ShrinkablePolygon shrinkablePolygon, ref List<ShrinkablePolygon> subgraphs)
        {
            // for each edge in shrinkablePolygon, but not edges that directly connect (e.P. 0-1, 1-2) check for intersections.
            // if we intersect, we need to divide the shrinkablePolygon into two graphs (g1,g2) to remove the intersection within a shrinkablePolygon.
            // g1 will be 'left' of the intersection, g2 will be 'right' of the intersection.
            // g2 may contain additional intersections.
            for (int i = 0; i < shrinkablePolygon.m_points.Count; ++i)
            {
                int nextI = (i + 1) % shrinkablePolygon.m_points.Count;
                
                for (int j = i + 2; j < shrinkablePolygon.m_points.Count; ++j)
                {
                    int nextJ = (j + 1) % shrinkablePolygon.m_points.Count;
                    if (i == nextJ) continue;

                    UnityVectorExtensions.FindIntersection(shrinkablePolygon.m_points[i].m_position,
                        shrinkablePolygon.m_points[nextI].m_position,
                        shrinkablePolygon.m_points[j].m_position, shrinkablePolygon.m_points[nextJ].m_position,
                        out bool linesIntersect, out bool segmentsIntersect,
                        out Vector2 intersection);
                    
                    if (segmentsIntersect)
                    {
                        // g1 will be left from the intersection, g2 will be right of the intersection.
                        ShrinkablePolygon g1 = new ShrinkablePolygon(shrinkablePolygon.m_aspectRatio, shrinkablePolygon.m_aspectRatioBasedDiagonal, shrinkablePolygon.m_normalDirections);
                        {
                            g1.m_windowDiagonal = shrinkablePolygon.m_windowDiagonal;
                            g1.m_intersectionPoints.Add(intersection);
                            g1.m_state = shrinkablePolygon.m_state + 1;

                            // g1 -> intersection j+1 ... i
                            var points = new List<ShrinkablePoint2>
                            {
                                new ShrinkablePoint2 {m_position = intersection, m_shrinkDirection = Vector2.zero,}
                            };
                            for (int k = (j + 1) % shrinkablePolygon.m_points.Count;
                                k != (i + 1) % shrinkablePolygon.m_points.Count;
                                k = (k + 1) % shrinkablePolygon.m_points.Count)
                            {
                                points.Add(shrinkablePolygon.m_points[k]);
                            }
                            
                            g1.m_points = RotateListToLeftmost(points);
                        }
                        subgraphs.Add(g1);

                        ShrinkablePolygon g2 = new ShrinkablePolygon(shrinkablePolygon.m_aspectRatio, shrinkablePolygon.m_aspectRatioBasedDiagonal, shrinkablePolygon.m_normalDirections);
                        {
                            g2.m_windowDiagonal = shrinkablePolygon.m_windowDiagonal;
                            g2.m_intersectionPoints.Add(intersection);
                            g2.m_state = shrinkablePolygon.m_state + 1;

                            // g2 -> intersection i+1 ... j
                            var points = new List<ShrinkablePoint2>
                            {
                                new ShrinkablePoint2 {m_position = intersection, m_shrinkDirection = Vector2.zero,}
                            };
                            for (int k = (i + 1) % shrinkablePolygon.m_points.Count;
                                k != (j + 1) % shrinkablePolygon.m_points.Count;
                                k = (k + 1) % shrinkablePolygon.m_points.Count)
                            {
                                points.Add(shrinkablePolygon.m_points[k]);
                            }

                            g2.m_points = RollListToStartClosestToPoint(points, intersection);
                        }

                        // we need to move the intersection m_points from the parent shrinkablePolygon
                        // to g1 and g2 graphs, depending on which is closer to the intersection point.
                        for (int k = 0; k < shrinkablePolygon.m_intersectionPoints.Count; ++k)
                        {
                            float g1Dist = g1.SqrDistanceTo(shrinkablePolygon.m_intersectionPoints[k]);
                            float g2Dist = g2.SqrDistanceTo(shrinkablePolygon.m_intersectionPoints[k]);
                            if (g1Dist < g2Dist)
                            {
                                g1.m_intersectionPoints.Add(shrinkablePolygon.m_intersectionPoints[k]);
                            }
                            else
                            {
                                g2.m_intersectionPoints.Add(shrinkablePolygon.m_intersectionPoints[k]);
                            }
                        }

                        shrinkablePolygon = g2;
                        return true; // shrinkablePolygon has nice intersections
                    }
                }
            }

            return false; // shrinkablePolygon does not have nice intersections
        }

        internal static void DivideAlongIntersections(ShrinkablePolygon shrinkablePolygon, out List<ShrinkablePolygon> subgraphs)
        {
            /// 2. DO until ShrinkablePolygon G has intersections
            /// 2.a.: Found 1 intersection, divide G into g1, g2. Then, G=g2, continue from 2.
            /// Result of 2 is G in subgraphs without intersections: g1, g2, ..., gn.
            /// done.
            subgraphs = new List<ShrinkablePolygon>();
            int maxIteration = 10;
            while (maxIteration > 0 && DivideGraph(ref shrinkablePolygon, ref subgraphs))
            {
                maxIteration--;
            };
            if (maxIteration <= 0)
            {
                Debug.Log("Exited with max iteration safety! - Let us know on the Cinemachine forums please!"); // never happened to me in my tests
            }
            subgraphs.Add(shrinkablePolygon); // add remaining shrinkablePolygon
        }
        
        /// <summary>
        /// Rotates input List to start closest to point in 2D space.
        /// </summary>
        /// <param name="point">List will rotate so it's 0th element is as close to point as possible.</param>
        /// <param name="points">List to rotate</param>
        /// <returns>List, in which the 0 element is the closest point in the List to point in 2D space.
        /// Order of m_points of the original List is preserved</returns>
        private static List<ShrinkablePoint2> RollListToStartClosestToPoint(in List<ShrinkablePoint2> points, in Vector2 point)
        {
            int closestIndex = 0;
            Vector2 closestPoint = points[0].m_position;
            for (int i = 1; i < points.Count; ++i)
            {
                if ((closestPoint - point).sqrMagnitude > (closestPoint - points[i].m_position).sqrMagnitude)
                {
                    closestIndex = i;
                    closestPoint = points[i].m_position;
                }
            }

            var point_rolledToStartAtClosestPoint = new List<ShrinkablePoint2>(points.Count);
            for (int i = closestIndex; i < points.Count; ++i)
            {
                point_rolledToStartAtClosestPoint.Add(points[i]);
            }

            for (int i = 0; i < closestIndex; ++i)
            {
                point_rolledToStartAtClosestPoint.Add(points[i]);
            }

            return point_rolledToStartAtClosestPoint;
        }

        /// <summary>
        /// Rotates input List to start from the left-most element in 2D space.
        /// </summary>
        /// <param name="points">List to rotate</param>
        /// <returns>List, in which the 0 element is the left-most in 2D space.
        /// Order of m_points of the original List is preserved</returns>
        private static List<ShrinkablePoint2> RotateListToLeftmost(List<ShrinkablePoint2> points)
        {
            int leftMostPointIndex = 0;
            Vector2 leftMostPoint = points[0].m_position;
            for (int i = 1; i < points.Count; ++i)
            {
                if (leftMostPoint.x > points[i].m_position.x)
                {
                    leftMostPointIndex = i;
                    leftMostPoint = points[i].m_position;
                }
            }

            var pointsRolledToStartAtLeftMostPoint = new List<ShrinkablePoint2>(points.Count);
            for (int i = leftMostPointIndex; i < points.Count; ++i)
            {
                pointsRolledToStartAtLeftMostPoint.Add(points[i]);
            }

            for (int i = 0; i < leftMostPointIndex; ++i)
            {
                pointsRolledToStartAtLeftMostPoint.Add(points[i]);
            }

            return pointsRolledToStartAtLeftMostPoint;
        }
    }
}
