using System;
using System.Collections.Generic;
using Cinemachine.Utility;
using ClipperLib;
using UnityEngine;
using UnityEngine.Assertions;

namespace Cinemachine
{
    /// <summary>
    /// ShrinkablePolygon represents points with shrink directions.
    /// </summary>
    internal class ShrinkablePolygon
    {
        /// <summary>
        /// 2D point with shrink direction.
        /// </summary>
        public struct ShrinkablePoint2
        {
            public Vector2 m_Position;
            public Vector2 m_OriginalPosition;
            public Vector2 m_ShrinkDirection;
            public bool m_CantIntersect;
            public static readonly Vector2 m_Vector2NaN = new Vector2(float.NaN, float.NaN);
        }

        /// <summary>
        /// Info relating to current aspect ratio
        /// </summary>
        public struct AspectData
        {
            public float m_AspectRatio;
            public float m_AspectRatioBasedDiagonal;
            public Vector2[] m_NormalDirections;

            public AspectData(float aspectRatio)
            {
                m_AspectRatio = aspectRatio;
                m_AspectRatioBasedDiagonal = Mathf.Sqrt(aspectRatio*aspectRatio + 1);
                m_NormalDirections = new[]
                {
                    Vector2.up,
                    new Vector2(aspectRatio, 1),
                    new Vector2(aspectRatio, 0),
                    new Vector2(aspectRatio, -1),
                    Vector2.down,
                    new Vector2(-aspectRatio, -1),
                    new Vector2(-aspectRatio, 0),
                    new Vector2(-aspectRatio, 1),
                };
            }
        }

        public List<ShrinkablePoint2> m_Points;
        public float m_FrustumHeight;
        public int m_State;
        public List<Vector2> m_IntersectionPoints;
        
        public float m_Area;
        private bool ClockwiseOrientation => m_Area > 0;

        /// <summary>
        /// Default constructor initializing points and intersection points.
        /// </summary>
        public ShrinkablePolygon() {}

        /// <summary>
        /// Parameterized constructor for points - computes normals and signed area.
        /// </summary>
        public ShrinkablePolygon(List<Vector2> points) : this()
        {
            int numPoints = points.Count;
            m_Points = new List<ShrinkablePoint2>(numPoints);
            for (int i = 0; i < numPoints; ++i)
            {
                m_Points.Add(new ShrinkablePoint2
                {
                    m_Position = points[i],
                    m_OriginalPosition = points[i],
                });
            }
            m_IntersectionPoints = new List<Vector2>();
            
            m_Area = ComputeSignedArea();
            ComputeNormals(true);
        }

        /// <summary>
        /// Creates and returns a deep copy of this subPolygons.
        /// </summary>
        /// <returns>Deep copy of this subPolygons</returns>
        public ShrinkablePolygon DeepCopy()
        {
            return new ShrinkablePolygon
            {
                m_Area = m_Area,
                m_FrustumHeight = m_FrustumHeight,
                m_State = m_State,
                
                // deep
                m_Points = m_Points.ConvertAll(point => point),
                m_IntersectionPoints = m_IntersectionPoints.ConvertAll(intersection => intersection),
            };
        }
        
        /// <summary>
        /// Computes signed area. Sign determines whether the polygon is oriented clockwise or counter-clockwise.
        /// Does not work if polygon has self intersections.
        /// </summary>
        /// <returns>Area of the subPolygons</returns>
        public float ComputeSignedArea()
        {
            float area = 0f;
            int numPoints = m_Points.Count;
            for (int i = 0; i < numPoints; ++i)
            {
                var p1 = m_Points[i].m_Position;
                var p2 = m_Points[(i + 1) % numPoints].m_Position;
                area += (p2.x - p1.x) * (p2.y + p1.y);
            }
            return area / 2f;
        }

        /// <summary>
        /// Checks whether the polygon intersects itself.
        /// </summary>
        /// <returns>True, if yes. False, otherwise.</returns>
        public bool DoesSelfIntersect()
        {
            // for each edge in subPolygons, but not for edges that are neighbours (e.g. 0-1, 1-2),
            // check for intersections.
            int numPoints = m_Points.Count;
            for (int i = 0; i < numPoints; ++i)
            {
                int nextI = (i + 1) % numPoints;
                for (int j = i + 2; j < numPoints; ++j)
                {
                    int nextJ = (j + 1) % numPoints;
                    if (i == nextJ) continue;

                    int intersectionType = UnityVectorExtensions.FindIntersection(
                        m_Points[i].m_Position, m_Points[nextI].m_Position,
                        m_Points[j].m_Position, m_Points[nextJ].m_Position,
                        out _);
                    
                    if (intersectionType == 2)
                    {
                        return true; // self intersects
                    }
                }
            }

            return false; // does not self intersect
        }

        private static readonly List<Vector2> s_edgeNormalsCache = new List<Vector2>(); 
        private static readonly List<ShrinkablePoint2> s_extendedPointsCache = new List<ShrinkablePoint2>();

        /// <summary>
        /// Computes normalized normals for all points. If fixBigCornerAngles is true, then adds additional points for
        /// corners with reflex angles to ensure correct offsets.
        /// If fixBigCornerAngles is true, number of points may change.
        /// </summary>
        private void ComputeNormals(bool fixBigCornerAngles)
        {
            int numPoints = m_Points.Count;
            s_edgeNormalsCache.Clear();
            if (s_edgeNormalsCache.Capacity < numPoints)
                s_edgeNormalsCache.Capacity = numPoints;
            for (int i = 0; i < numPoints; ++i)
            {
                Vector2 edge = m_Points[(i + 1) % numPoints].m_Position - m_Points[i].m_Position;
                Vector2 normal = ClockwiseOrientation ? new Vector2(edge.y, -edge.x) : new Vector2(-edge.y, edge.x); 
                s_edgeNormalsCache.Add(normal.normalized);
            }

            // calculating normals
            for (int i = 0; i < numPoints; ++i)
            {
                int prevEdgeIndex = i == 0 ? s_edgeNormalsCache.Count - 1 : i - 1;
                var mPoint = m_Points[i];
                mPoint.m_ShrinkDirection = (s_edgeNormalsCache[i] + s_edgeNormalsCache[prevEdgeIndex]).normalized;
                m_Points[i] = mPoint;
            }

            if (fixBigCornerAngles)
            {
                // we need to fix corners with reflex angle (negative for polygons oriented clockwise,
                // positive for polygons oriented counterclockwise)
                // fixing means that we add more shrink directions, because at this corners the offset from the
                // camera middle point can be different depending on which way the camera comes from
                // worst case: every point has negative angle
                // (not possible in practise, but makes the algorithm simpler)
                s_extendedPointsCache.Clear();
                if (s_extendedPointsCache.Capacity < numPoints * 3)
                    s_extendedPointsCache.Capacity = numPoints * 3;
                for (int i = 0; i < numPoints; ++i)
                {
                    int prevEdgeIndex = i == 0 ? s_edgeNormalsCache.Count - 1 : i - 1;
                    var negativeAngle = Vector3.Cross(s_edgeNormalsCache[i], s_edgeNormalsCache[prevEdgeIndex]).z < 0;
                    var sourcePoint = m_Points[i];
                    if (ClockwiseOrientation != negativeAngle)
                        s_extendedPointsCache.Add(sourcePoint);
                    else
                    {
                        int prevIndex = (i == 0 ? numPoints - 1 : i - 1);
                        s_extendedPointsCache.Add(new ShrinkablePoint2
                        {
                            m_Position = Vector2.Lerp(sourcePoint.m_Position, m_Points[prevIndex].m_Position, 0.01f),
                            m_ShrinkDirection = sourcePoint.m_ShrinkDirection,
                            m_CantIntersect = true,
                            m_OriginalPosition = ShrinkablePoint2.m_Vector2NaN,
                        });
                        
                        var shrinkablePoint2 = sourcePoint;
                        shrinkablePoint2.m_OriginalPosition = ShrinkablePoint2.m_Vector2NaN;
                        s_extendedPointsCache.Add(shrinkablePoint2);

                        int nextIndex = (i == numPoints - 1 ? 0 : i + 1);
                        s_extendedPointsCache.Add(new ShrinkablePoint2
                        {
                            m_Position = Vector2.Lerp(sourcePoint.m_Position, m_Points[nextIndex].m_Position, 0.01f),
                            m_ShrinkDirection = m_Points[i].m_ShrinkDirection,
                            m_CantIntersect = true,
                            m_OriginalPosition = ShrinkablePoint2.m_Vector2NaN,
                        });
                    }
                }
                if (s_extendedPointsCache.Count != numPoints)
                    m_Points = s_extendedPointsCache;
            }
        }
    
        /// <summary>
        /// Computes shrink directions that respect the aspect ratio of the camera. If the camera window is a square,
        /// then the shrink directions will be equivalent to the normals.
        /// </summary>
        public void ComputeAspectBasedShrinkDirections(in AspectData aspectData)
        {
            ComputeNormals(false);
            int numPoints = m_Points.Count;
            for (int i = 0; i < numPoints; ++i)
            {
                int prevIndex = i == 0 ? numPoints - 1 : i - 1;
                int nextIndex = i == numPoints - 1 ? 0 : i + 1;

                var p = m_Points[i];
                p.m_ShrinkDirection = CalculateShrinkDirection(p.m_ShrinkDirection, 
                    m_Points[prevIndex].m_Position, p.m_Position, m_Points[nextIndex].m_Position, aspectData);
                m_Points[i] = p;
            }
        }
        
        private static readonly int FloatToIntScaler = 10000000; // same as in Physics2D

        /// <summary>
        /// Converts shrinkable polygons into a simple path made of 2D points.
        /// </summary>
        /// <param name="shrinkablePolygons">input shrinkable polygons</param>
        /// <param name="aspectRatio">Current camera aspect ratio</param>
        /// <param name="frustumHeight">Frustum height requested by the user.
        /// For the path touching the corners this may be relevant.</param>
        /// <param name="maxCachedFrustumHeight">Maximum cached frustum height.</param>
        /// <param name="path">Resulting path.</param>
        /// <param name="hasIntersections">True, if the polygon has intersections. False, otherwise.</param>
        public static void ConvertToPath(in List<ShrinkablePolygon> shrinkablePolygons, 
            in float aspectRatio, in float frustumHeight, in float maxCachedFrustumHeight,
            out List<List<Vector2>> path, out bool hasIntersections)
        {
            hasIntersections = maxCachedFrustumHeight < frustumHeight;
            // convert shrinkable polygons points to int based points for Clipper
            List<List<IntPoint>> clip = new List<List<IntPoint>>(shrinkablePolygons.Count);
            int index = 0;
            for (var polyIndex = 0; polyIndex < shrinkablePolygons.Count; polyIndex++)
            {
                var polygon = shrinkablePolygons[polyIndex];
                clip.Add(new List<IntPoint>(polygon.m_Points.Count));
                foreach (var point in polygon.m_Points)
                {
                    clip[index].Add(new IntPoint(point.m_Position.x * FloatToIntScaler,
                        point.m_Position.y * FloatToIntScaler));
                }
                index++;

                // add a thin line to each intersection point, thus connecting disconnected polygons
                foreach (var intersectionPoint in polygon.m_IntersectionPoints)
                {
                    hasIntersections = true;
                    
                    Vector2 closestPoint = polygon.ClosestPolygonPoint(intersectionPoint);
                    Vector2 direction = (closestPoint - intersectionPoint).normalized;
                    Vector2 epsilonNormal = new Vector2(direction.y, -direction.x).normalized * 
                                            UnityVectorExtensions.Epsilon;

                    clip.Add(new List<IntPoint>(4));
                    Vector2 p1 = closestPoint + epsilonNormal + direction * UnityVectorExtensions.Epsilon;
                    Vector2 p2 = intersectionPoint + epsilonNormal;
                    Vector3 p3 = intersectionPoint - epsilonNormal;
                    Vector3 p4 = closestPoint - epsilonNormal + direction * UnityVectorExtensions.Epsilon;

                    clip[index].Add(new IntPoint(p1.x * FloatToIntScaler, p1.y * FloatToIntScaler));
                    clip[index].Add(new IntPoint(p2.x * FloatToIntScaler, p2.y * FloatToIntScaler));
                    clip[index].Add(new IntPoint(p3.x * FloatToIntScaler, p3.y * FloatToIntScaler));
                    clip[index].Add(new IntPoint(p4.x * FloatToIntScaler, p4.y * FloatToIntScaler));

                    index++;
                }
                
                foreach (var point in polygon.m_Points)
                {
                    if (!point.m_OriginalPosition.IsNaN())
                    {
                        Vector2 corner = point.m_OriginalPosition;
                        Vector2 shrinkDirection = point.m_Position - corner;
                        float sqrCornerDistance = shrinkDirection.sqrMagnitude;
                        if (shrinkDirection.x > 0)
                        {
                            shrinkDirection *= (aspectRatio / shrinkDirection.x);
                        }
                        else if (shrinkDirection.x < 0)
                        {
                            shrinkDirection *= -(aspectRatio / shrinkDirection.x);
                        }
                        if (shrinkDirection.y > 1)
                        {
                            shrinkDirection *= (1f / shrinkDirection.y);
                        }
                        else if (shrinkDirection.y < -1)
                        {
                            shrinkDirection *= -(1f / shrinkDirection.y);
                        }

                        shrinkDirection *= frustumHeight;
                        if (shrinkDirection.sqrMagnitude >= sqrCornerDistance)
                        {
                            continue; // camera is already touching this point
                        }
                        Vector2 cornerTouchingPoint = corner + shrinkDirection;
                        if (Vector2.Distance(cornerTouchingPoint, point.m_Position) < UnityVectorExtensions.Epsilon)
                        {
                            continue;
                        }
                        Vector2 epsilonNormal = new Vector2(shrinkDirection.y, -shrinkDirection.x).normalized * 
                                                UnityVectorExtensions.Epsilon;
                        clip.Add(new List<IntPoint>(4));
                        Vector2 p1 = point.m_Position + epsilonNormal + 
                                     shrinkDirection.normalized * UnityVectorExtensions.Epsilon;
                        Vector2 p2 = cornerTouchingPoint + epsilonNormal;
                        Vector2 p3 = cornerTouchingPoint - epsilonNormal;
                        Vector2 p4 = point.m_Position - epsilonNormal + 
                                     shrinkDirection.normalized * UnityVectorExtensions.Epsilon;
                        clip[index].Add(new IntPoint(p1.x * FloatToIntScaler, p1.y * FloatToIntScaler));
                        clip[index].Add(new IntPoint(p2.x * FloatToIntScaler, p2.y * FloatToIntScaler));
                        clip[index].Add(new IntPoint(p3.x * FloatToIntScaler, p3.y * FloatToIntScaler));
                        clip[index].Add(new IntPoint(p4.x * FloatToIntScaler, p4.y * FloatToIntScaler));
     
                        index++;
                    }
    
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

        /// <summary>
        /// Checks whether p is inside or outside the polygons. The algorithm determines if a point is inside based
        /// on a horizontal raycast from p. If the ray intersects the polygon odd number of times, then p is inside.
        /// Otherwise, p is outside.
        /// </summary>
        /// <param name="polygons">Input polygons</param>
        /// <param name="p">Input point.</param>
        /// <returns>True, if inside. False, otherwise.</returns>
        public static bool IsInside(in List<List<Vector2>> polygons, in Vector2 p)
        {
            float minX = Single.PositiveInfinity;
            float maxX = Single.NegativeInfinity;
            foreach (var path in polygons)
            {
                foreach (var point in path)
                {
                    var pointInWorldCoordinates = point;
                    minX = Mathf.Min(minX, pointInWorldCoordinates.x);
                    maxX = Mathf.Max(maxX, pointInWorldCoordinates.x);
                }
            }
            float polygonXWidth = maxX - minX;
            if (!(minX <= p.x && p.x <= maxX))
            {
                return false; // p is outside to the left or to the right
            }
            
            int intersectionCount = 0;
            Vector2 camRayEndFromCamPos2D = p + Vector2.right * polygonXWidth;
            foreach (var polygon in polygons)
            {
                for (int index = 0; index < polygon.Count; ++index)
                {
                    Vector2 p1 = polygon[index];
                    Vector2 p2 = polygon[(index + 1) % polygon.Count];
                    int intersectionType = UnityVectorExtensions.FindIntersection(p, camRayEndFromCamPos2D, p1, p2, 
                        out _);
                    if (intersectionType == 2)
                    {
                        intersectionCount++;
                    }
                }
            }

            return intersectionCount % 2 != 0; // inside polygon when odd number of intersections
        }

        
        /// <summary>
        /// Finds midpoint of a rectangle's side touching CA and CB.
        /// D1 - D2 defines the side or diagonal of a rectangle touching CA and CB.
        /// </summary>
        /// <returns>Midpoint of a rectangle's side touching CA and CB.</returns>
        private static Vector2 FindMidPoint(in Vector2 A, in Vector2 B, in Vector2 C, in Vector2 D1, in Vector2 D2)
        {
            Vector2 CA = (A - C);
            Vector2 CB = (B - C);

            float gamma = Vector2.Angle(CA, CB);
            if (gamma <= 0.05f || 179.95f <= gamma) 
            { 
                return (A + B) / 2; // too narrow angle, so just return the mid point
            }
            Vector2 D1D2 = D1 - D2;
            Vector2 D1C = C - B;
            float beta = Vector2.Angle(D1C, D1D2);
            Vector2 D2D1 = D2 - D1;
            Vector2 D2C = C - A;
            float alpha = Vector2.Angle(D2C, D2D1);
            if (Math.Abs(gamma + beta + alpha - 180) > 0.5f)
            {
                D1D2 = D2 - D1;
                D1C = C - B;
                beta = Vector2.Angle(D1C, D1D2);
                D2D1 = D1 - D2;
                D2C = C - A;
                alpha = Vector2.Angle(D2C, D2D1);
            }
            if (alpha <= 0.05f || 179.95f <= alpha || 
                beta <= 0.05f || 179.95f <= beta)
            {
                return (A + B) / 2; // too narrow angle, so just return the mid point
            }

            float c = D1D2.magnitude;
            float a = (c / Mathf.Sin(gamma * Mathf.Deg2Rad)) * Mathf.Sin(alpha * Mathf.Deg2Rad);
            float b = (c / Mathf.Sin(gamma * Mathf.Deg2Rad)) * Mathf.Sin(beta * Mathf.Deg2Rad);

            Vector2 M1 = C + CB.normalized * Mathf.Abs(a);
            Vector2 M2 = C + CA.normalized * Mathf.Abs(b);

            Vector2 dist1 = (A + B) / 2 - C;
            Vector2 dist2 = (M1 + M2) / 2 - C;
            
            return dist1.sqrMagnitude < dist2.sqrMagnitude ? (A + B) / 2 : (M1 + M2) / 2;
        }
        
        /// <summary>
        /// Calculates shrink direction for thisPoint, based on it's normal and neighbouring points.
        /// </summary>
        /// <param name="normal">normal of thisPoint</param>
        /// <param name="prevPoint">previous neighbouring of thisPoint</param>
        /// <param name="thisPoint">point for which the normal is calculated.</param>
        /// <param name="nextPoint">next neighbouring of thisPoint</param>
        /// <returns>Returns shrink direction for thisPoint</returns>
        private Vector2 CalculateShrinkDirection(in Vector2 normal, 
            in Vector2 prevPoint, in Vector2 thisPoint, in Vector2 nextPoint, 
            in AspectData aspectData)
        {
            Vector2 A = prevPoint;
            Vector2 B = nextPoint;
            Vector2 C = thisPoint;

            Vector2 CA = (A - C);
            Vector2 CB = (B - C);
            
            float angle1_abs = Vector2.Angle(CA, normal);
            float angle2_abs = Vector2.Angle(CB, normal);
            
            Vector2 R = normal.normalized * aspectData.m_AspectRatioBasedDiagonal;
            float angle = Vector2.SignedAngle(R, aspectData.m_NormalDirections[0]);
            if (0 < angle && angle < 90)
            {
                if (angle - angle1_abs <= 1f && 89 <= angle + angle2_abs)
                {
                    // case 0 - 1 point intersection with camera window
                    R = aspectData.m_NormalDirections[1];
                }
                else if (angle - angle1_abs <= 0 && angle + angle2_abs < 90)
                {
                    // case 1a - 2 point intersection with camera window's bottom
                    Vector2 M = FindMidPoint(A, B, C, aspectData.m_NormalDirections[3], aspectData.m_NormalDirections[5]); // bottom side's midpoint
                    Vector2 rectangleMidPoint = M + aspectData.m_NormalDirections[0]; // rectangle's midpoint
                    R = rectangleMidPoint - C;
                }
                else if (0 < angle - angle1_abs && 90 <= angle + angle2_abs)
                {
                    // case 1b - 2 point intersection with camera window's left side
                    Vector2 M = FindMidPoint(A, B, C, aspectData.m_NormalDirections[7], aspectData.m_NormalDirections[5]); // left side's midpoint
                    Vector2 rectangleMidPoint = M + aspectData.m_NormalDirections[2]; // rectangle's midpoint
                    R = rectangleMidPoint - C;
                }
                else if (0 < angle - angle1_abs && angle + angle2_abs < 90)
                {
                    // case 2 - 2 point intersection with camera window's diagonal (top-left to bottom-right)
                    Vector2 rectangleMidPoint = FindMidPoint(A, B, C, aspectData.m_NormalDirections[3], aspectData.m_NormalDirections[7]); // rectangle's midpoint
                    R = rectangleMidPoint - C;
                }
                else
                {
                    Assert.IsTrue(false, "Error in CalculateShrinkDirection - " +
                                         "Let us know on the Cinemachine forum please!");
                }
            }
            else if (90 < angle && angle < 180)
            {
                if (angle - angle1_abs <= 91 && 179 <= angle + angle2_abs)
                {
                    // case 0 - 1 point intersection with camera window
                    R = aspectData.m_NormalDirections[3];
                }
                else if (angle - angle1_abs <= 90 && angle + angle2_abs < 180)
                {
                    // case 1a - 2 point intersection with camera window's left
                    Vector2 M = FindMidPoint(A, B, C, aspectData.m_NormalDirections[0], aspectData.m_NormalDirections[4]); // left side's midpoint
                    Vector2 rectangleMidPoint = M + aspectData.m_NormalDirections[2]; // rectangle's midpoint
                    R = rectangleMidPoint - C;
                }
                else if (90 < angle - angle1_abs && 180 <= angle + angle2_abs)
                {
                    // case 1b - 2 point intersection with camera window's top side
                    Vector2 M = FindMidPoint(A, B, C, aspectData.m_NormalDirections[1], aspectData.m_NormalDirections[7]); // top side's midpoint
                    Vector2 rectangleMidPoint = M + aspectData.m_NormalDirections[4]; // rectangle's midpoint
                    R = rectangleMidPoint - C;
                }
                else if (90 < angle - angle1_abs && angle + angle2_abs < 180)
                {
                    // case 2 - 2 point intersection with camera window's diagonal (top-right to bottom-left)
                    Vector2 rectangleMidPoint = FindMidPoint(A, B, C, aspectData.m_NormalDirections[1], aspectData.m_NormalDirections[5]); // rectangle's midpoint
                    R = rectangleMidPoint - C;
                }
                else
                {
                    Assert.IsTrue(false, "Error in CalculateShrinkDirection - " +
                                         "Let us know on the Cinemachine forum please!");
                }
            }
            else if (-180 < angle && angle < -90)
            {
                if (angle - angle1_abs <= -179 && -91 <= angle + angle2_abs)
                {
                    // case 0 - 1 point intersection with camera window
                    R = aspectData.m_NormalDirections[5];
                }
                else if (angle - angle1_abs <= -180 && angle + angle2_abs < -90)
                {
                    // case 1a - 2 point intersection with camera window's top
                    Vector2 M = FindMidPoint(A, B, C, aspectData.m_NormalDirections[7], aspectData.m_NormalDirections[1]); // top side's midpoint
                    Vector2 rectangleMidPoint = M + aspectData.m_NormalDirections[4]; // rectangle's midpoint
                    R = rectangleMidPoint - C;
                }
                else if (-180 < angle - angle1_abs && -90 <= angle + angle2_abs)
                {
                    // case 1b - 2 point intersection with camera window's right side
                    Vector2 M = FindMidPoint(A, B, C, aspectData.m_NormalDirections[1], aspectData.m_NormalDirections[3]); // right side's midpoint
                    Vector2 rectangleMidPoint = M + aspectData.m_NormalDirections[6]; // rectangle's midpoint
                    R = rectangleMidPoint - C;
                }
                else if (-180 < angle - angle1_abs && angle + angle2_abs < -90)
                {
                    // case 2 - 2 point intersection with camera window's diagonal (top-left to bottom-right)
                    Vector2 rectangleMidPoint = FindMidPoint(A, B, C, aspectData.m_NormalDirections[3], aspectData.m_NormalDirections[7]); // rectangle's midpoint
                    R = rectangleMidPoint - C;
                }
                else
                {
                    Assert.IsTrue(false, "Error in CalculateShrinkDirection - " +
                                         "Let us know on the Cinemachine forum please!");
                }
            }
            else if (-90 < angle && angle < 0)
            {
                if (angle - angle1_abs <= -89 && -1 <= angle + angle2_abs)
                {
                    // case 0 - 1 point intersection with camera window
                    R = aspectData.m_NormalDirections[7];
                }
                else if (angle - angle1_abs <= -90 && angle + angle2_abs < 0)
                {
                    // case 1a - 2 point intersection with camera window's right side
                    Vector2 M = FindMidPoint(A, B, C, aspectData.m_NormalDirections[7], aspectData.m_NormalDirections[5]); // right side's midpoint
                    Vector2 rectangleMidPoint = M + aspectData.m_NormalDirections[6]; // rectangle's midpoint
                    R = rectangleMidPoint - C;
                }
                else if (-90 < angle - angle1_abs && 0 <= angle + angle2_abs)
                {
                    // case 1b - 2 point intersection with camera window's bottom side
                    Vector2 M = FindMidPoint(A, B, C, aspectData.m_NormalDirections[5], aspectData.m_NormalDirections[3]); // bottom side's mid point
                    Vector2 rectangleMidPoint = M + aspectData.m_NormalDirections[0]; // rectangle's midpoint
                    R = rectangleMidPoint - C;
                }
                else if (-90 < angle - angle1_abs && angle + angle2_abs < 0)
                {
                    // case 2 - 2 point intersection with camera window's diagonal (top-right to bottom-left)
                    Vector2 rectangleMidPoint = FindMidPoint(A, B, C, aspectData.m_NormalDirections[1], aspectData.m_NormalDirections[5]); // rectangle's midpoint
                    R = rectangleMidPoint - C;
                }
                else
                {
                    Assert.IsTrue(false, "Error in CalculateShrinkDirection - " +
                                         "Let us know on the Cinemachine forum please!");
                }
            }
            else
            {
                R.x = Mathf.Clamp(R.x, -aspectData.m_AspectRatio, aspectData.m_AspectRatio);
                R.y = Mathf.Clamp(R.y, -1, 1);
            }
            
            return R;
        }

        /// <summary>
        /// ShrinkablePolygon is shrinkable if it has at least one non-zero shrink direction.
        /// </summary>
        /// <returns>True, if subPolygons is shrinkable. False, otherwise.</returns>
        public bool IsShrinkable()
        {
            for (int i = 0; i < m_Points.Count; ++i)
            {
                if (m_Points[i].m_ShrinkDirection != Vector2.zero)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Shrink shrinkablePolygon points towards their shrink direction by stepSize.
        /// </summary>
        public bool Shrink(float stepSize, bool shrinkToPoint, in float aspectRatio)
        {
            m_FrustumHeight += stepSize;
            if (Mathf.Abs(m_Area) < 0.1f) // TODO: what's a good value
            {
                // Polygon is a skeleton
                if (shrinkToPoint)
                {
                    Vector2 center = CenterOfMass();
                    for (int i = 0; i < m_Points.Count; ++i)
                    {
                        var mPoint = m_Points[i];
                        Vector2 direction = center - mPoint.m_Position;
                        // normalize direction so it is within the aspectRatio x 1 rectangle.
                        if (Math.Abs(direction.x) > aspectRatio ||
                            Math.Abs(direction.y) > 1)
                        {
                            if (direction.x > aspectRatio)
                            {
                                direction *= (aspectRatio / direction.x);
                            }
                            else if (direction.x < -aspectRatio)
                            {
                                direction *= -(aspectRatio / direction.x);
                            }
                            if (direction.y > 1)
                            {
                                direction *= (1f / direction.y);
                            }
                            else if (direction.y < -1)
                            {
                                direction *= -(1f / direction.y);
                            }

                            mPoint.m_ShrinkDirection = direction;
                        }
                        else
                        {
                            mPoint.m_ShrinkDirection = Vector2.zero;
                        }

                        m_Points[i] = mPoint;
                    }
                }
                else
                {
                    for (int i = 0; i < m_Points.Count; ++i)
                    {
                        var mPoint = m_Points[i];
                        mPoint.m_ShrinkDirection = Vector2.zero;
                        m_Points[i] = mPoint;
                    }

                    return false;
                }
            }

            // move each point in its shrink direction
            for (int i = 0; i < m_Points.Count; ++i)
            {
                var mPoint = m_Points[i];
                mPoint.m_Position += mPoint.m_ShrinkDirection * stepSize;
                m_Points[i] = mPoint;
            }

            m_Area = ComputeSignedArea();
            return true;
        }

        /// <summary>
        /// Simple center of mass of this shrinkable polygon.
        /// </summary>
        /// <returns>Center of Mass</returns>
        private Vector2 CenterOfMass()
        {
            Vector2 center = Vector2.zero;
            for (var i = 0; i < m_Points.Count; ++i)
            {
                center += m_Points[i].m_Position;
            }

            return center / m_Points.Count;
        }

        /// <summary>
        /// Calculates squared distance to 'P' from closest point to 'P' in the subPolygons
        /// </summary>
        /// <param name="p">Point in space.</param>
        /// <returns>Squared distance to 'P' from closest point to 'P' in the subPolygons</returns>
        private float SqrDistanceTo(Vector2 p)
        {
            float minDistance = float.MaxValue;
            for (int i = 0; i < m_Points.Count; ++i)
            {
                minDistance = Mathf.Min(minDistance, (m_Points[i].m_Position - p).sqrMagnitude);
            }

            return minDistance;
        }

        /// <summary>
        /// Calculates the closest point to the subPolygons from P.
        /// The point returned is going to be one of the points of the subPolygons.
        /// </summary>
        /// <param name="p">Point from which the distance is calculated.</param>
        /// <returns>A point that is part of the subPolygons points and is closest to P.</returns>
        private Vector2 ClosestPolygonPoint(Vector2 p)
        {
            float minDistance = float.MaxValue;
            Vector2 closestPoint = Vector2.zero;
            for (int i = 0; i < m_Points.Count; ++i)
            {
                float sqrDistance = (m_Points[i].m_Position - p).sqrMagnitude;
                if (minDistance > sqrDistance)
                {
                    minDistance = sqrDistance;
                    closestPoint = m_Points[i].m_Position;
                }
            }

            return closestPoint;
        }
  
        /// <summary>
        /// Returns point closest to p that is a point of the subPolygons.
        /// </summary>
        /// <param name="p"></param>
        /// <returns>Closest point to p in ShrinkablePolygon</returns>
        public Vector2 ClosestPolygonPoint(ShrinkablePoint2 p)
        {
            var foundWithNormal = false;
            var minDistance = float.MaxValue;
            var closestPoint = Vector2.zero;
            for (int i = 0; i < m_Points.Count; ++i)
            {
                Vector2 diff = m_Points[i].m_Position - p.m_Position;
                float angle = Vector2.Angle(p.m_ShrinkDirection, diff);
                if (angle < 5 || 175 < angle)
                {
                    foundWithNormal = true;
                    float sqrDistance = diff.sqrMagnitude;
                    if (minDistance > sqrDistance)
                    {
                        minDistance = sqrDistance;
                        closestPoint = m_Points[i].m_Position;
                    }
                }
            }
            if (foundWithNormal)
            {
                return closestPoint;
            }

            for (int i = 0; i < m_Points.Count; ++i)
            {
                Vector2 diff = m_Points[i].m_Position - p.m_Position;
                float sqrDistance = diff.sqrMagnitude;
                if (minDistance > sqrDistance)
                {
                    minDistance = sqrDistance;
                    closestPoint = m_Points[i].m_Position;
                }
            }
            return closestPoint;
        }

        internal const int k_NonLerpableStateChangePenalty = 10; // penalty for non-lerpable state changes
      
        /// <summary>
        /// Removes points that are the same or very close.
        /// </summary>
        /// <param name="delta">Radius / 2f within which points are removed</param>
        /// <returns>True, if simplified. False, otherwise.</returns>
        public bool Simplify(float delta)
        {
            if (m_Points.Count <= 4)
            {
                return false;
            }

            bool changeState = false;
            float distanceLimit = delta * 2f;
            var canSimplify = true;
            while (canSimplify)
            {
                canSimplify = false;
                for (int i = 0; i < m_Points.Count; ++i)
                {
                    int j = (i + 1) % m_Points.Count;
                    
                    if (!m_Points[i].m_CantIntersect && !m_Points[j].m_CantIntersect) continue;
                    
                    if ((m_Points[i].m_Position - m_Points[j].m_Position).sqrMagnitude <= distanceLimit)
                    {
                        if (m_Points[i].m_CantIntersect)
                        {
                            m_Points.RemoveAt(i);
                        }
                        else if (m_Points[j].m_CantIntersect)
                        {
                            m_Points.RemoveAt(j);
                        }
                        else
                        {
                            m_Points.RemoveAt(j);
                            m_Points.RemoveAt(i);
                        }

                        changeState = true;
                        canSimplify = true;
                        break;
                    }
                }
            }

            if (changeState)
            {
                m_State += k_NonLerpableStateChangePenalty; // simplify is a state change that cannot be lerped
                return true;
            }

            return false;
        }

        /// <summary>Divides subPolygons into subPolygons if there are intersections.</summary>
        /// <param name="shrinkablePolygon">ShrinkablePolygon to divide. 
        /// ShrinkablePolygon will be overwritten by a subPolygons with possible intersections,
        /// after cutting off the subPolygons part 'left' of the intersection.</param>
        /// <param name="subPolygons">Resulting subPolygons from dividing subPolygons.</param>
        /// <returns>True, if found intersection. False, otherwise.</returns>
        private static bool DivideShrinkablePolygon(ref ShrinkablePolygon shrinkablePolygon, 
            ref List<ShrinkablePolygon> subPolygons)
        {
            // for each edge in subPolygons, but not for edges that are neighbours (e.g. 0-1, 1-2),
            // check for intersections.
            // if we intersect, we need to divide the subPolygons into two shrinkablePolygons (g1,g2) to remove the
            // intersection within a subPolygons.
            // g1 will be 'left' of the intersection, g2 will be 'right' of the intersection.
            // g2 may contain additional intersections.
            int numPoints = shrinkablePolygon.m_Points.Count;
            for (int i = 0; i < numPoints; ++i)
            {
                int nextI = (i + 1) % numPoints;
                for (int j = i + 2; j < numPoints; ++j)
                {
                    int nextJ = (j + 1) % numPoints;
                    if (i == nextJ) continue;

                    int intersectionType = UnityVectorExtensions.FindIntersection(
                        shrinkablePolygon.m_Points[i].m_Position, shrinkablePolygon.m_Points[nextI].m_Position,
                        shrinkablePolygon.m_Points[j].m_Position, shrinkablePolygon.m_Points[nextJ].m_Position,
                        out Vector2 intersection);

                    if (intersectionType == 2 && intersection.sqrMagnitude >= Vector2.positiveInfinity.sqrMagnitude)
                    {
                        // parallel lines so no need to divide.
                        shrinkablePolygon.m_State += k_NonLerpableStateChangePenalty;
                        return true; // subPolygons has nice intersections
                    }
                    if (intersectionType == 2) // so we divide g into g1 and g2.
                    {
                        var g1 = new ShrinkablePolygon();
                        {
                            g1.m_IntersectionPoints = new List<Vector2>();
                            g1.m_FrustumHeight = shrinkablePolygon.m_FrustumHeight;
                            g1.m_IntersectionPoints.Add(intersection);
                            g1.m_State = shrinkablePolygon.m_State + k_NonLerpableStateChangePenalty;

                            // g1 -> intersection j+1 ... i
                            var points = new List<ShrinkablePoint2>
                            {
                                new ShrinkablePoint2
                                {
                                    m_Position = intersection, 
                                    m_OriginalPosition = ShrinkablePoint2.m_Vector2NaN, 
                                    m_ShrinkDirection = Vector2.zero,
                                }
                            };
                            for (int k = (j + 1) % numPoints; k != (i + 1) % numPoints; k = (k + 1) % numPoints)
                            {
                                points.Add(shrinkablePolygon.m_Points[k]);
                            }
                            
                            RollListToLeftmost(points);
                            g1.m_Points = points;
                        }
                        subPolygons.Add(g1);

                        var g2 = new ShrinkablePolygon();
                        {
                            g2.m_IntersectionPoints = new List<Vector2>();
                            g2.m_FrustumHeight = shrinkablePolygon.m_FrustumHeight;
                            g2.m_IntersectionPoints.Add(intersection);
                            g2.m_State = shrinkablePolygon.m_State + k_NonLerpableStateChangePenalty;

                            // g2 -> intersection i+1 ... j
                            var points = new List<ShrinkablePoint2>
                            {
                                new ShrinkablePoint2
                                {
                                    m_Position = intersection,
                                    m_OriginalPosition = ShrinkablePoint2.m_Vector2NaN, 
                                    m_ShrinkDirection = Vector2.zero,
                                }
                            };
                            for (int k = (i + 1) % numPoints; k != (j + 1) % numPoints; k = (k + 1) % numPoints)
                            {
                                points.Add(shrinkablePolygon.m_Points[k]);
                            }

                            RollListToClosest(points, intersection);
                            g2.m_Points = points;
                        }

                        // we need to move the intersection points from the parent subPolygons
                        // to g1 and g2 subPolygons, depending on which is closer to the intersection point.
                        for (int k = 0; k < shrinkablePolygon.m_IntersectionPoints.Count; ++k)
                        {
                            float g1Dist = g1.SqrDistanceTo(shrinkablePolygon.m_IntersectionPoints[k]);
                            float g2Dist = g2.SqrDistanceTo(shrinkablePolygon.m_IntersectionPoints[k]);
                            if (g1Dist < g2Dist)
                            {
                                g1.m_IntersectionPoints.Add(shrinkablePolygon.m_IntersectionPoints[k]);
                            }
                            else
                            {
                                g2.m_IntersectionPoints.Add(shrinkablePolygon.m_IntersectionPoints[k]);
                            }
                        }

                        shrinkablePolygon = g2; // need to continue dividing g2 as it may contain more intersections
                        return true; // subPolygons has nice intersections
                    }
                }
            }

            return false; // subPolygons does not have nice intersections
        }

        /// <summary>
        /// Divides input shrinkable polygon into subpolygons until it has no more intersections.
        /// Input polygon gets reduced and added to the list.
        /// </summary>
        public static void DivideAlongIntersections(
            ShrinkablePolygon poly, ref List<ShrinkablePolygon> divided, in AspectData aspectData)
        {
            // In practice max 1-3 intersections at the same time in the same frame.
            divided.Clear();
            int maxIteration = 10; 
            for (; maxIteration > 0; --maxIteration)
            {
                if (!DivideShrinkablePolygon(ref poly, ref divided))
                    break;
            }
            divided.Add(poly); // add remaining points
            for (int i = 0; i < divided.Count - 1; ++i)
                divided[i].ComputeAspectBasedShrinkDirections(aspectData);
        }
        
        /// <summary>
        /// Rotates input List of shrinkable points to start closest to input point in 2D space.
        /// This is important to ensure order independence in algorithm.
        /// </summary>
        /// <param name="point">List will rotate so its 0th element is as close to point as possible.</param>
        /// <param name="points">List to rotate</param>
        private static void RollListToClosest(List<ShrinkablePoint2> points, in Vector2 point)
        {
            int closestIndex = 0;
            float closestDistance = float.MaxValue;
            int numPoints = points.Count;
            for (int i = 0; i < numPoints; ++i)
            {
                var d = (points[i].m_Position - point).sqrMagnitude;
                if (d < closestDistance)
                {
                    closestIndex = i;
                    closestDistance = d;
                }
            }
            if (closestIndex > 0)
                RollList(points, closestIndex);
        }


        /// <summary>
        /// Rotates input List to start from the left-most element in 2D space.
        /// This is important to ensure order independence in algorithm.
        /// </summary>
        /// <param name="points">List to rotate</param>
        private static void RollListToLeftmost(List<ShrinkablePoint2> points)
        {
            int leftMostPointIndex = 0;
            float leftMostPoint = points[0].m_Position.x;
            int numPoints = points.Count;
            for (int i = 1; i < numPoints; ++i)
            {
                if (leftMostPoint > points[i].m_Position.x)
                {
                    leftMostPointIndex = i;
                    leftMostPoint = points[i].m_Position.x;
                }
            }
            if (leftMostPointIndex > 0)
                RollList(points, leftMostPointIndex);
        }

        private static List<ShrinkablePoint2> s_rollPointsCache = new List<ShrinkablePoint2>();
        private static void RollList(List<ShrinkablePoint2> points, int newFirstElement)
        {
            s_rollPointsCache.Clear();
            if (s_rollPointsCache.Capacity < newFirstElement)
                s_rollPointsCache.Capacity = newFirstElement;

            // GML TODO: this would be faster with memcpy
            for (int i = 0; i < newFirstElement; ++i)
                s_rollPointsCache.Add(points[i]);
            int next = 0;
            int numPoints = points.Count;
            for (int i = newFirstElement; i < numPoints; ++i)
                points[next++] = points[i];
            for (int i = 0; next < numPoints; ++i)
                points[next++] = s_rollPointsCache[i];
        }
    }
}
