using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Cinemachine.Utility;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

//TODO: fix disconnectivity - when lines are so thin the composite collider ignores them
//
namespace Cinemachine
{
    public class ConfinerState
    {
        public List<Graph> graphs;
        public float windowSize;
        public float state;
    }

    public class Point2
    {
        public Vector2 position;
        public Vector2 normal;

        internal Point2()
        {
        }

        internal Point2(Vector2 position, Vector2 normal)
        {
            this.position = position;
            this.normal = normal;
        }
        
    }

    /// <summary>
    /// Graph represent a list of <points, and their normals> that can shrink down to it's skeleton.  
    /// </summary>
    public class Graph
    {
        internal List<Point2> points;
        internal bool ClockwiseOrientation;
        internal float area;
        internal float windowDiagonal;
        internal float sensorRatio;
        internal int state;
        private bool normalDirectionTowardsCenter;
        private bool zeroNormalsXdirection;
        private bool zeroNormalsYdirection;

        internal List<Vector2> intersectionPoints;
        public Graph()
        {
            points = new List<Point2>();
            intersectionPoints = new List<Vector2>();
            area = 0;
            windowDiagonal = 0;
            state = 0;
            normalDirectionTowardsCenter = false;
            zeroNormalsXdirection = false;
            zeroNormalsYdirection = false;
        }

        public bool SetNormalDirectionTowardsCenter()
        {
            if (!normalDirectionTowardsCenter)
            {
                normalDirectionTowardsCenter = true;
                state++;
                return true;
            }
            return false;
        }

        public bool SetZeroNormalsXdirection()
        {
            if (!zeroNormalsXdirection)
            {
                zeroNormalsXdirection = true;
                state++;
                return true;
            }
            return false;
        }

        public bool SetZeroNormalsYdirection()
        {
            if (!zeroNormalsYdirection)
            {
                zeroNormalsYdirection = true;
                state++;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Creates and returns a deep copy of this graph.
        /// </summary>
        /// <returns>Deep copy of this graph</returns>
        public Graph DeepCopy()
        {
            Graph deepCopy = new Graph
            {
                points = this.points.ConvertAll(point => new Point2(point.position, point.normal)),
                ClockwiseOrientation = this.ClockwiseOrientation,
                area = this.area,
                intersectionPoints = this.intersectionPoints.ConvertAll(intersection =>
                    new Vector2(intersection.x, intersection.y)),
                windowDiagonal = this.windowDiagonal,
                sensorRatio = this.sensorRatio,
                normalDirectionTowardsCenter = this.normalDirectionTowardsCenter,
                zeroNormalsXdirection = this.zeroNormalsXdirection,
                zeroNormalsYdirection = this.zeroNormalsYdirection,
                state = this.state,
            };
            return deepCopy;
        }

        internal float ComputeSignedArea()
        {
            area = 0;
            for (int i = 0; i < points.Count; ++i)
            {
                var p1 = points[i];
                var p2 = points[(i + 1) % points.Count];

                area += (p2.position.x - p1.position.x) * (p2.position.y + p1.position.y);
            }

            ClockwiseOrientation = area > 0;
            return area;
        }

        internal static void ComputeNormalAt(int index, List<Point2> points)
        {
            Vector2 pointBefore = points[index == 0 ? points.Count - 1 : index - 1].position;
            Vector2 point = points[index].position;
            Vector2 pointAfter = points[(index + 1) % points.Count].position;

            bool ClockwiseOrientation = true;
            Vector2 edgeBefore = point - pointBefore;
            Vector2 edgeBefore_normal = ClockwiseOrientation
                ? new Vector2(edgeBefore.y, -edgeBefore.x)
                : new Vector2(-edgeBefore.y, edgeBefore.x);
            Vector2 edgeAfter = pointAfter - point;
            Vector2 edgeAfter_normal = ClockwiseOrientation
                ? new Vector2(edgeAfter.y, -edgeAfter.x)
                : new Vector2(-edgeAfter.y, edgeAfter.x);

            points[index].normal = (edgeBefore_normal + edgeAfter_normal).normalized;
        }

        private static float oneOverSquarerootOfTwo = 0.70710678f;
        /// <summary> Computes normalized normals for all points, and adds additional points for corners
        /// with reflex angles to ensure correct offset
        /// </summary>
        internal void ComputeNormals(bool fixBigCornerAngles)
        {
            var edgeNormals = new List<Vector2>(points.Count);
            for (int i = 0; i < points.Count; ++i)
            {
                Vector2 edge = points[(i + 1) % points.Count].position - points[i].position;
                Vector2 normal = ClockwiseOrientation ? new Vector2(edge.y, -edge.x) : new Vector2(-edge.y, edge.x); 
                edgeNormals.Add(normal.normalized);
            }
            
            
            for (int i = points.Count - 1; i >= 0; --i)
            {
                int prevEdgeIndex = i == 0 ? edgeNormals.Count - 1 : i - 1;
                points[i].normal = (edgeNormals[i] + edgeNormals[prevEdgeIndex]) / 2f;
                points[i].normal.Normalize();

                if (fixBigCornerAngles)
                {
                    var angle = Vector2.SignedAngle(edgeNormals[i], edgeNormals[prevEdgeIndex]);
                    if (angle < -89.99f) // -89.999999.99999 <------
                    {
                        int prevIndex = i == 0 ? points.Count - 1 : i - 1;
                        int nextIndex = i == points.Count - 1 ? 0 : i + 1;
                        points.Insert(nextIndex, new Point2
                        {
                            position = Vector2.Lerp(points[i].position, points[nextIndex].position, 0.01f),
                            normal = points[i].normal
                        });
                        points.Insert(i, new Point2
                        {
                            position = Vector2.Lerp(points[i].position, points[prevIndex].position, 0.01f),
                            normal = points[i].normal
                        });
                        points.RemoveAt(nextIndex); // remove original
                    }
                }
            }
        }
    
        internal void ComputeRectangulizedNormals()
        {
            ComputeNormals(false);

            for (int i = 0; i < points.Count; ++i)
            {
                int prevIndex = i == 0 ? points.Count - 1 : i - 1;
                int nextIndex = i == points.Count - 1 ? 0 : i + 1;
                points[i].normal = RectangulizeNormal(points[i].normal, 
                    points[prevIndex].position, points[i].position, points[nextIndex].position);
            
            }
        }

        /// <summary>
        /// Instead of normalizing a vector in a circle with a set a radius, this function normalizes the vector to be
        /// within a rectangle with sides (a, 1). Meaning, the maximum length is a and 1 for the x and y components of the
        /// vector respectively.
        /// </summary>
        /// <param name="normal">Normal to RectangulizeNormal</param>
        /// <returns>RectangleNormalized normal</returns>
        private Vector2 RectangulizeNormal(Vector2 normal, Vector2 prevPoint, Vector2 thisPoint, Vector2 nextPoint)
        {
            var A = prevPoint;
            var B = nextPoint;
            var C = thisPoint;
            List<Vector2> normalDirections = new List<Vector2>
            {
                new Vector2(0, 1),
                new Vector2(sensorRatio, 1),
                new Vector2(sensorRatio, 0),
                new Vector2(sensorRatio, -1),
                new Vector2(0, -1),
                new Vector2(-sensorRatio, -1),
                new Vector2(-sensorRatio, 0),
                new Vector2(-sensorRatio, 1),
            };
            
            
            // for (var i = 0; i < normalDirections.Count; ++i)
            // {
            //     if (Vector2.Angle(normal, normalDirections[i]) < 5)
            //     {
            //         return normalDirections[i];
            //     }
            // }

            Vector2 CA = (A - C);
            Vector2 CB = (B - C);
            
            
            var angle1 = Vector2.SignedAngle(CA, normal);
            var angle1_abs = Math.Abs(angle1);
            var angle2 = Vector2.SignedAngle(CB, normal);
            var angle2_abs = Math.Abs(angle2);

            Vector2 R = normal.normalized * Mathf.Sqrt(sensorRatio*sensorRatio + 1);
            float angle = Vector2.SignedAngle(R, normalDirections[0]);
            
            //    | x
            // -------
            //   |
            if (0 < angle && angle < 90)
            {
                if (angle - angle1_abs <= 1f && 89 <= angle + angle2_abs)
                {
                    // case 0 - 1 point intersection with camera window
                    R = normalDirections[1];
                }
                else if (angle - angle1_abs <= 0 && angle + angle2_abs < 90)
                {
                    // case 1a - 2 point intersection with camera window's bottom
                    var gamma = UnityVectorExtensions.Angle(CA, CB);
                    var D1D2 = normalDirections[3] - normalDirections[5];
                    var D1C = C - B;
                    var beta = UnityVectorExtensions.Angle(D1C, D1D2);
                    var D2D1 = normalDirections[5] - normalDirections[3];
                    var D2C = C - A;
                    var alpha = UnityVectorExtensions.Angle(D2C, D2D1);
                    if (Math.Abs(gamma + beta + alpha - 180) > 0.5f)
                    {
                        D1D2 = normalDirections[5] - normalDirections[3];
                        D1C = C - B;
                        beta = UnityVectorExtensions.Angle(D1C, D1D2);
                        D2D1 = normalDirections[3] - normalDirections[5];
                        D2C = C - A;
                        alpha = UnityVectorExtensions.Angle(D2C, D2D1);
                    }

                    var c = D1D2.magnitude; // TODO: shoild this be exact diagonal? windowDiagonal
                    var a = (c / Mathf.Sin(gamma * Mathf.Deg2Rad)) * Mathf.Sin(alpha * Mathf.Deg2Rad);
                    var b = (c / Mathf.Sin(gamma * Mathf.Deg2Rad)) * Mathf.Sin(beta * Mathf.Deg2Rad);

                    var M1 = C + CB.normalized * Mathf.Abs(a);
                    var M2 = C + CA.normalized * Mathf.Abs(b);
                    var M = (M1 + M2) / 2; // // bottom side's mid point

                    var rectangleMidPoint = M + normalDirections[0];
                    R = rectangleMidPoint - C;
                    return R;
                }
                else if (0 < angle - angle1_abs && 90 <= angle + angle2_abs)
                {
                    // case 1b - 2 point intersection with camera window's left side
                    var gamma = UnityVectorExtensions.Angle(CA, CB);
                    var D1D2 = normalDirections[7] - normalDirections[5];
                    var D1C = C - B;
                    var beta = UnityVectorExtensions.Angle(D1C, D1D2);
                    var D2D1 = normalDirections[5] - normalDirections[7];
                    var D2C = C - A;
                    var alpha = UnityVectorExtensions.Angle(D2C, D2D1);
                    if (Math.Abs(gamma + beta + alpha - 180) > 0.5f)
                    {
                        D1D2 = normalDirections[5] - normalDirections[7];
                        D1C = C - B;
                        beta = UnityVectorExtensions.Angle(D1C, D1D2);
                        D2D1 = normalDirections[7] - normalDirections[5];
                        D2C = C - A;
                        alpha = UnityVectorExtensions.Angle(D2C, D2D1);
                    }
                    
                    var c = D1D2.magnitude;
                    var a = (c / Mathf.Sin(gamma * Mathf.Deg2Rad)) * Mathf.Sin(alpha * Mathf.Deg2Rad);
                    var b = (c / Mathf.Sin(gamma * Mathf.Deg2Rad)) * Mathf.Sin(beta * Mathf.Deg2Rad);

                    var M1 = C + CB.normalized * Mathf.Abs(a);
                    var M2 = C + CA.normalized * Mathf.Abs(b);
                    var M = (M1 + M2) / 2; // left side's mid point

                    var rectangleMidPoint = M + normalDirections[2];
                    R = rectangleMidPoint - C;
                    return R;
                }
                else if (0 < angle - angle1_abs && angle + angle2_abs < 90)
                {
                    // case 2 - 2 point intersection with camera window's diagonal (top-left to bottom-right)
                    var gamma = UnityVectorExtensions.Angle(CA, CB);
                    var D1D2 = normalDirections[3] - normalDirections[7];
                    var D1C = C - B;
                    var beta = UnityVectorExtensions.Angle(D1C, D1D2);
                    var D2D1 = normalDirections[7] - normalDirections[3];
                    var D2C = C - A;
                    var alpha = UnityVectorExtensions.Angle(D2C, D2D1);
                    if (Math.Abs(gamma + beta + alpha - 180) > 0.5f)
                    {
                        D1D2 = normalDirections[7] - normalDirections[3];
                        D1C = C - B;
                        beta = UnityVectorExtensions.Angle(D1C, D1D2);
                        D2D1 = normalDirections[3] - normalDirections[7];
                        D2C = C - A;
                        alpha = UnityVectorExtensions.Angle(D2C, D2D1);
                    }

                    var c = D1D2.magnitude;
                    var a = (c / Mathf.Sin(gamma * Mathf.Deg2Rad)) * Mathf.Sin(alpha * Mathf.Deg2Rad);
                    var b = (c / Mathf.Sin(gamma * Mathf.Deg2Rad)) * Mathf.Sin(beta * Mathf.Deg2Rad);

                    var M1 = C + CB.normalized * Mathf.Abs(a);
                    var M2 = C + CA.normalized * Mathf.Abs(b);
                    var rectangleMidPoint = (M1 + M2) / 2;
                    
                    R = rectangleMidPoint - C;
                    return R;
                }
                else
                {
                    Debug.Log("Should never happen!");
                }
            }
            //    | 
            // -------
            //   | x
            else if (90 < angle && angle < 180)
            {
                if (angle - angle1_abs <= 91 && 179 <= angle + angle2_abs)
                {
                    // case 0 - 1 point intersection with camera window
                    R = normalDirections[3];
                }
                else if (angle - angle1_abs <= 90 && angle + angle2_abs < 180)
                {
                    // case 1a - 2 point intersection with camera window's left // fixed from top
                    var gamma = UnityVectorExtensions.Angle(CA, CB);
                    var D1D2 = normalDirections[0] - normalDirections[4]; // fixed from 1-7
                    var D1C = C - B;
                    var beta = UnityVectorExtensions.Angle(D1C, D1D2);
                    var D2D1 = normalDirections[4] - normalDirections[0];
                    var D2C = C - A;
                    var alpha = UnityVectorExtensions.Angle(D2C, D2D1);
                    if (Math.Abs(gamma + beta + alpha - 180) > 0.5f)
                    {
                        D1D2 = normalDirections[4] - normalDirections[0];
                        D1C = C - B;
                        beta = UnityVectorExtensions.Angle(D1C, D1D2);
                        D2D1 = normalDirections[0] - normalDirections[4];
                        D2C = C - A;
                        alpha = UnityVectorExtensions.Angle(D2C, D2D1);
                    }
                    
                    var c = D1D2.magnitude; // TODO: shoild this be exact diagonal? windowDiagonal
                    var a = (c / Mathf.Sin(gamma * Mathf.Deg2Rad)) * Mathf.Sin(alpha * Mathf.Deg2Rad);
                    var b = (c / Mathf.Sin(gamma * Mathf.Deg2Rad)) * Mathf.Sin(beta * Mathf.Deg2Rad);

                    var M1 = C + CB.normalized * Mathf.Abs(a);
                    var M2 = C + CA.normalized * Mathf.Abs(b);
                    var M = (M1 + M2) / 2; // // bottom side's mid point

                    var rectangleMidPoint = M + normalDirections[2]; // fixed from 0
                    R = rectangleMidPoint - C;
                    return R;
                }
                else if (90 < angle - angle1_abs && 180 <= angle + angle2_abs)
                {
                    // case 1b - 2 point intersection with camera window's top side // fixed from left
                    var gamma = UnityVectorExtensions.Angle(CA, CB);
                    var D1D2 = normalDirections[1] - normalDirections[7]; // fixed from 7-5
                    var D1C = C - B;
                    var beta = UnityVectorExtensions.Angle(D1C, D1D2);
                    var D2D1 = normalDirections[7] - normalDirections[1];
                    var D2C = C - A;
                    var alpha = UnityVectorExtensions.Angle(D2C, D2D1);
                    if (Math.Abs(gamma + beta + alpha - 180) > 0.5f)
                    {
                        D1D2 = normalDirections[7] - normalDirections[1]; // fixed from 7-5
                        D1C = C - B;
                        beta = UnityVectorExtensions.Angle(D1C, D1D2);
                        D2D1 = normalDirections[1] - normalDirections[7];
                        D2C = C - A;
                        alpha = UnityVectorExtensions.Angle(D2C, D2D1);
                    }
                    
                    var c = D1D2.magnitude;
                    var a = (c / Mathf.Sin(gamma * Mathf.Deg2Rad)) * Mathf.Sin(alpha * Mathf.Deg2Rad);
                    var b = (c / Mathf.Sin(gamma * Mathf.Deg2Rad)) * Mathf.Sin(beta * Mathf.Deg2Rad);

                    var M1 = C + CB.normalized * Mathf.Abs(a);
                    var M2 = C + CA.normalized * Mathf.Abs(b);
                    var M = (M1 + M2) / 2; // left side's mid point

                    var rectangleMidPoint = M + normalDirections[4];
                    R = rectangleMidPoint - C;
                    return R;
                }
                else if (90 < angle - angle1_abs && angle + angle2_abs < 180)
                {
                    // case 2 - 2 point intersection with camera window's diagonal (top-right to bottom-left)
                    var gamma = UnityVectorExtensions.Angle(CA, CB);
                    var D1D2 = normalDirections[1] - normalDirections[5];
                    var D1C = C - B;
                    var beta = UnityVectorExtensions.Angle(D1C, D1D2);
                    var D2D1 = normalDirections[5] - normalDirections[1];
                    var D2C = C - A;
                    var alpha = UnityVectorExtensions.Angle(D2C, D2D1);
                    if (Math.Abs(gamma + beta + alpha - 180) > 0.5f)
                    {
                        D1D2 = normalDirections[5] - normalDirections[1];
                        D1C = C - B;
                        beta = UnityVectorExtensions.Angle(D1C, D1D2);
                        D2D1 = normalDirections[1] - normalDirections[5];
                        D2C = C - A;
                        alpha = UnityVectorExtensions.Angle(D2C, D2D1);
                    }

                    var c = D1D2.magnitude;
                    var a = (c / Mathf.Sin(gamma * Mathf.Deg2Rad)) * Mathf.Sin(alpha * Mathf.Deg2Rad);
                    var b = (c / Mathf.Sin(gamma * Mathf.Deg2Rad)) * Mathf.Sin(beta * Mathf.Deg2Rad);

                    var M1 = C + CB.normalized * Mathf.Abs(a);
                    var M2 = C + CA.normalized * Mathf.Abs(b);
                    var rectangleMidPoint = (M1 + M2) / 2;
                    
                    R = rectangleMidPoint - C;
                    return R;
                }
                else
                {
                    Debug.Log("Should never happen!");
                }
            }
            else if (-180 < angle && angle < -90)
            {
                if (angle - angle1_abs <= -179 && -91 <= angle + angle2_abs)
                {
                    // case 0 - 1 point intersection with camera window
                    R = normalDirections[5];
                }
                else if (angle - angle1_abs <= -180 && angle + angle2_abs < -90)
                {
                    // case 1a - 2 point intersection with camera window's top
                    var gamma = UnityVectorExtensions.Angle(CA, CB);
                    var D1D2 = normalDirections[7] - normalDirections[1]; // fixed from 1-7
                    var D1C = C - B;
                    var beta = UnityVectorExtensions.Angle(D1C, D1D2);
                    var D2D1 = normalDirections[1] - normalDirections[7];
                    var D2C = C - A;
                    var alpha = UnityVectorExtensions.Angle(D2C, D2D1);
                    if (Math.Abs(gamma + beta + alpha - 180) > 0.5f)
                    {
                        D1D2 = normalDirections[1] - normalDirections[7]; // fixed from 1-7
                        D1C = C - B;
                        beta = UnityVectorExtensions.Angle(D1C, D1D2);
                        D2D1 = normalDirections[7] - normalDirections[1];
                        D2C = C - A;
                        alpha = UnityVectorExtensions.Angle(D2C, D2D1);
                    }
                    
                    var c = D1D2.magnitude; // TODO: shoild this be exact diagonal? windowDiagonal
                    var a = (c / Mathf.Sin(gamma * Mathf.Deg2Rad)) * Mathf.Sin(alpha * Mathf.Deg2Rad);
                    var b = (c / Mathf.Sin(gamma * Mathf.Deg2Rad)) * Mathf.Sin(beta * Mathf.Deg2Rad);

                    var M1 = C + CB.normalized * Mathf.Abs(a);
                    var M2 = C + CA.normalized * Mathf.Abs(b);
                    var M = (M1 + M2) / 2; // // bottom side's mid point

                    var rectangleMidPoint = M + normalDirections[4]; // fixed from 0
                    R = rectangleMidPoint - C;
                    return R;
                }
                else if (-180 < angle - angle1_abs && -90 <= angle + angle2_abs)
                {
                    // case 1b - 2 point intersection with camera window's right side
                    var gamma = UnityVectorExtensions.Angle(CA, CB);
                    var D1D2 = normalDirections[1] - normalDirections[3];
                    var D1C = C - B;
                    var beta = UnityVectorExtensions.Angle(D1C, D1D2);
                    var D2D1 = normalDirections[3] - normalDirections[1];
                    var D2C = C - A;
                    var alpha = UnityVectorExtensions.Angle(D2C, D2D1);
                    if (Math.Abs(gamma + beta + alpha - 180) > 0.5f)
                    {
                        D1D2 = normalDirections[3] - normalDirections[1];
                        D1C = C - B;
                        beta = UnityVectorExtensions.Angle(D1C, D1D2);
                        D2D1 = normalDirections[1] - normalDirections[3];
                        D2C = C - A;
                        alpha = UnityVectorExtensions.Angle(D2C, D2D1);
                    }
                    
                    var c = D1D2.magnitude;
                    var a = (c / Mathf.Sin(gamma * Mathf.Deg2Rad)) * Mathf.Sin(alpha * Mathf.Deg2Rad);
                    var b = (c / Mathf.Sin(gamma * Mathf.Deg2Rad)) * Mathf.Sin(beta * Mathf.Deg2Rad);

                    var M1 = C + CB.normalized * Mathf.Abs(a);
                    var M2 = C + CA.normalized * Mathf.Abs(b);
                    var M = (M1 + M2) / 2; // left side's mid point

                    var rectangleMidPoint = M + normalDirections[6]; // fixed from 2
                    R = rectangleMidPoint - C;
                    return R;
                }
                else if (-180 < angle - angle1_abs && angle + angle2_abs < -90)
                {
                    // case 2 - 2 point intersection with camera window's diagonal (top-left to bottom-right)
                    var gamma = UnityVectorExtensions.Angle(CA, CB);
                    var D1D2 = normalDirections[3] - normalDirections[7];
                    var D1C = C - B;
                    var beta = UnityVectorExtensions.Angle(D1C, D1D2);
                    var D2D1 = normalDirections[7] - normalDirections[3];
                    var D2C = C - A;
                    var alpha = UnityVectorExtensions.Angle(D2C, D2D1);
                    if (Math.Abs(gamma + beta + alpha - 180) > 0.5f)
                    {
                        D1D2 = normalDirections[7] - normalDirections[3];
                        D1C = C - B;
                        beta = UnityVectorExtensions.Angle(D1C, D1D2);
                        D2D1 = normalDirections[3] - normalDirections[7];
                        D2C = C - A;
                        alpha = UnityVectorExtensions.Angle(D2C, D2D1);
                    }

                    var c = D1D2.magnitude;
                    var a = (c / Mathf.Sin(gamma * Mathf.Deg2Rad)) * Mathf.Sin(alpha * Mathf.Deg2Rad);
                    var b = (c / Mathf.Sin(gamma * Mathf.Deg2Rad)) * Mathf.Sin(beta * Mathf.Deg2Rad);

                    var M1 = C + CB.normalized * Mathf.Abs(a);
                    var M2 = C + CA.normalized * Mathf.Abs(b);
                    var rectangleMidPoint = (M1 + M2) / 2;

                    R = rectangleMidPoint - C;
                    return R;
                }
                else
                {
                    Debug.Log("Should never happen!");
                }
            }
            else if (-90 < angle && angle < 0)
            {
                if (angle - angle1_abs <= -89 && -1 <= angle + angle2_abs)
                {
                    // case 0 - 1 point intersection with camera window
                    R = normalDirections[7];
                }
                else if (angle - angle1_abs <= -90 && angle + angle2_abs < 0)
                {
                    // case 1a - 2 point intersection with camera window's right side // fixed from bottom
                    var gamma = UnityVectorExtensions.Angle(CA, CB);
                    var D1D2 = normalDirections[7] - normalDirections[5];
                    var D1C = C - B;
                    var beta = UnityVectorExtensions.Angle(D1C, D1D2);
                    var D2D1 = normalDirections[5] - normalDirections[7];
                    var D2C = C - A;
                    var alpha = UnityVectorExtensions.Angle(D2C, D2D1);
                    if (Math.Abs(gamma + beta + alpha - 180) > 0.5f)
                    {
                        D1D2 = normalDirections[5] - normalDirections[7];
                        D1C = C - B;
                        beta = UnityVectorExtensions.Angle(D1C, D1D2);
                        D2D1 = normalDirections[7] - normalDirections[5];
                        D2C = C - A;
                        alpha = UnityVectorExtensions.Angle(D2C, D2D1);
                    }
                    
                    var c = D1D2.magnitude; // TODO: shoild this be exact diagonal? windowDiagonal
                    var a = (c / Mathf.Sin(gamma * Mathf.Deg2Rad)) * Mathf.Sin(alpha * Mathf.Deg2Rad);
                    var b = (c / Mathf.Sin(gamma * Mathf.Deg2Rad)) * Mathf.Sin(beta * Mathf.Deg2Rad);

                    var M1 = C + CB.normalized * Mathf.Abs(a);
                    var M2 = C + CA.normalized * Mathf.Abs(b);
                    var M = (M1 + M2) / 2; // // bottom side's mid point

                    var rectangleMidPoint = M + normalDirections[6]; // fixed from 0 to 6
                    R = rectangleMidPoint - C;
                    return R;
                }
                else if (-90 < angle - angle1_abs && 0 <= angle + angle2_abs)
                {
                    // case 1b - 2 point intersection with camera window's bottom side // fixed from right
                    var gamma = UnityVectorExtensions.Angle(CA, CB);
                    var D1D2 = normalDirections[3] - normalDirections[5]; // fixed from 1-3
                    var D1C = C - B;
                    var beta = UnityVectorExtensions.Angle(D1C, D1D2);
                    var D2D1 = normalDirections[5] - normalDirections[3];
                    var D2C = C - A;
                    var alpha = UnityVectorExtensions.Angle(D2C, D2D1);
                    if (Math.Abs(gamma + beta + alpha - 180) > 0.5f)
                    {
                        D1D2 = normalDirections[5] - normalDirections[3]; // fixed from 1-3
                        D1C = C - B;
                        beta = UnityVectorExtensions.Angle(D1C, D1D2);
                        D2D1 = normalDirections[3] - normalDirections[5];
                        D2C = C - A;
                        alpha = UnityVectorExtensions.Angle(D2C, D2D1);
                    }
                    
                    var c = D1D2.magnitude;
                    var a = (c / Mathf.Sin(gamma * Mathf.Deg2Rad)) * Mathf.Sin(alpha * Mathf.Deg2Rad);
                    var b = (c / Mathf.Sin(gamma * Mathf.Deg2Rad)) * Mathf.Sin(beta * Mathf.Deg2Rad);

                    var M1 = C + CB.normalized * Mathf.Abs(a);
                    var M2 = C + CA.normalized * Mathf.Abs(b);
                    var M = (M1 + M2) / 2; // left side's mid point

                    var rectangleMidPoint = M + normalDirections[0];
                    R = rectangleMidPoint - C;
                    return R;
                }
                else if (-90 < angle - angle1_abs && angle + angle2_abs < 0)
                {
                    // case 2 - 2 point intersection with camera window's diagonal (top-right to bottom-left)
                    var gamma = UnityVectorExtensions.Angle(CA, CB);
                    var D1D2 = normalDirections[1] - normalDirections[5];
                    var D1C = C - B;
                    var beta = UnityVectorExtensions.Angle(D1C, D1D2);
                    var D2D1 = normalDirections[5] - normalDirections[1];
                    var D2C = C - A;
                    var alpha = UnityVectorExtensions.Angle(D2C, D2D1);
                    if (Math.Abs(gamma + beta + alpha - 180) > 0.5f)
                    {
                        D1D2 = normalDirections[5] - normalDirections[1];
                        D1C = C - B;
                        beta = UnityVectorExtensions.Angle(D1C, D1D2);
                        D2D1 = normalDirections[1] - normalDirections[5];
                        D2C = C - A;
                        alpha = UnityVectorExtensions.Angle(D2C, D2D1);
                    }

                    var c = D1D2.magnitude;
                    var a = (c / Mathf.Sin(gamma * Mathf.Deg2Rad)) * Mathf.Sin(alpha * Mathf.Deg2Rad);
                    var b = (c / Mathf.Sin(gamma * Mathf.Deg2Rad)) * Mathf.Sin(beta * Mathf.Deg2Rad);

                    var M1 = C + CB.normalized * Mathf.Abs(a);
                    var M2 = C + CA.normalized * Mathf.Abs(b);
                    var rectangleMidPoint = (M1 + M2) / 2;
                    
                    R = rectangleMidPoint - C;
                    return R;
                }
                else
                {
                    Debug.Log("Should never happen!");
                }
            }
            else
            {
                R.x = Mathf.Clamp(R.x, -sensorRatio, sensorRatio);
                R.y = Mathf.Clamp(R.y, -1, 1);
            }
            
            return R;
        }

        /// <summary>
        /// Flips normals in the graph.
        /// </summary>
        internal void FlipNormals()
        {
            for (int i = 0; i < points.Count; ++i)
            {
                points[i].normal = -points[i].normal;
            }
        }

        /// <summary>
        /// Graph is shrinkable if it has at least one non-zero normal.
        /// </summary>
        /// <returns>True, if graph is shrinkable. False, otherwise.</returns>
        internal bool IsShrinkable()
        {
            for (int i = 0; i < points.Count; ++i)
            {
                if (points[i].normal != Vector2.zero)
                {
                    return true;
                }
            }
            return false;
        }

        internal bool Shrink(float shrinkAmount, bool dontShrinkToPoint, out bool woobly)
        {
            woobly = false;
            //if (!dontShrinkToPoint)
            // {
            //     var minX = float.PositiveInfinity;
            //     var minY = float.PositiveInfinity;
            //     var maxX = float.NegativeInfinity;
            //     var maxY = float.NegativeInfinity;
            //     for (int i = 0; i < points.Count; ++i)
            //     {
            //         minX = Mathf.Min(points[i].position.x, minX);
            //         minY = Mathf.Min(points[i].position.y, minY);
            //         maxX = Mathf.Max(points[i].position.x, maxX);
            //         maxY = Mathf.Max(points[i].position.y, maxY);
            //     }
            //
            //     bool normalsTowardsCenter = false;
            //     bool normalsXZero = false;
            //     bool normalsYZero = false;
            //     if (Math.Abs(maxX - minX) < 1f)
            //     {
            //         for (int i = 0; i < points.Count; ++i)
            //         {
            //             points[i].normal.x = 0;
            //             normalsXZero = true;
            //         }
            //     }
            //
            //     if (Math.Abs(maxY - minY) < 1f)
            //     {
            //         for (int i = 0; i < points.Count; ++i)
            //         {
            //             points[i].normal.y = 0;
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
            //     for (int i = 0; i < points.Count; ++i)
            //     {
            //         if (points[i].normal.sqrMagnitude > UnityVectorExtensions.Epsilon)
            //         {
            //             allNormalsAreNonZero = true;
            //         }
            //         else
            //         {
            //             points[i].normal = Vector2.zero;
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
            //     if (!normalsXZero && !normalsYZero && Mathf.Abs(area) > 0.5f && Mathf.Abs(area) < 2f)
            //     {
            //         normalsTowardsCenter = true;
            //         Vector2 center = new Vector2((minX + maxX) / 2f, (minY + maxY) / 2f);
            //         for (int i = 0; i < points.Count; ++i)
            //         {
            //             points[i].normal = RectangulizeNormal(center - points[i].position);
            //         }
            //         Simplify();
            //     }
            //     if (normalsTowardsCenter && SetNormalDirectionTowardsCenter())
            //     {
            //         return false;
            //     }
            // }
            windowDiagonal += shrinkAmount;
            // TODO: optimize shrink - shrink until intersection instead of steps
            float areaBefore = Mathf.Abs(ComputeSignedArea());
            for (int i = 0; i < points.Count; ++i)
            {
                points[i].position += points[i].normal * shrinkAmount;
            }
            float areaAfter = Mathf.Abs(ComputeSignedArea());
            if (areaAfter > areaBefore)
            {
                FlipNormals();
                for (int i = 0; i < points.Count; ++i)
                {
                    points[i].position += points[i].normal * (shrinkAmount * 2f);
                }
            }
            float areaAfterAfter = Mathf.Abs(ComputeSignedArea());
            if (areaAfterAfter > areaAfter || areaBefore < 0.02f)
            {
                FlipNormals();
                for (int i = 0; i < points.Count; ++i)
                {
                    points[i].position += points[i].normal * (shrinkAmount);
                    points[i].normal = Vector2.zero;
                }
            }

            woobly = areaBefore < areaAfter && areaBefore < areaAfterAfter;

            if (woobly)
            {
                for (int i = 0; i < points.Count; ++i)
                {
                    points[i].normal = Vector2.zero;
                }

                int a = 3;
            }
            return true;
        }

        /// <summary></summary>
        /// <param name="p">Point in space.</param>
        /// <returns>Squared distance to 'P' from closest point to 'P' in the graph</returns>
        internal float SqrDistanceTo(Vector2 p)
        {
            float minDistance = float.MaxValue;
            for (int i = 0; i < points.Count; ++i)
            {
                minDistance = Mathf.Min(minDistance, (points[i].position - p).sqrMagnitude);
            }

            return minDistance;
        }

        /// <summary>
        /// Returns the closest point to the graph from P. The point returned is going to be one of the points of the graph.
        /// </summary>
        /// <param name="p">Point from which the distance is calculated.</param>
        /// <returns>A point that is part of the graph points and is closest to P.</returns>
        internal Vector2 ClosestGraphPoint(Vector2 p)
        {
            float minDistance = float.MaxValue;
            Vector2 closestPoint = Vector2.zero;
            for (int i = 0; i < points.Count; ++i)
            {
                float sqrDistance = (points[i].position - p).sqrMagnitude;
                if (minDistance > sqrDistance)
                {
                    minDistance = sqrDistance;
                    closestPoint = points[i].position;
                }
            }

            return closestPoint;
        }

        /// <summary>
        /// Returns the closest point to the graph from P. The point returned can be an edge point.
        /// </summary>
        /// <param name="P">Point from which the distance is calculated.</param>
        /// <returns>Point that is closest to P.</returns>
        internal Vector2 ClosestPoint(Vector2 P)
        {
            float minDistance = float.MaxValue;
            int closestPointIndex = 0;
            for (int i = 0; i < points.Count; ++i)
            {
                float sqrDistance = (points[i].position - P).sqrMagnitude;
                if (minDistance > sqrDistance)
                {
                    minDistance = sqrDistance;
                    closestPointIndex = i;
                }
            }

            Vector2 Q = points[closestPointIndex].position;
            Vector2 R;
            
            var P1 = points[closestPointIndex == 0 ? points.Count - 1 : closestPointIndex - 1].position;
            var P2 = points[closestPointIndex == points.Count - 1 ? 0 : closestPointIndex + 1].position;
            // var distToP1 = (P - P1).sqrMagnitude;
            // var distToP2 = (P - P2).sqrMagnitude;
            // R = distToP1 < distToP2 ? P1 : P2;


             var a1= GetIntersection(P, Q, P1, false, minDistance, out bool i1);
             var a2= GetIntersection(P, Q, P1, true, minDistance, out bool i2);
             var b1= GetIntersection(P, Q, P2, false, minDistance, out bool i3);
             var b2= GetIntersection(P, Q, P2, true, minDistance, out bool i4);

             var closestPoint = a1;
             if (i1)
             {
                 closestPoint = a1;
             }
             if (i2)
             {
                 closestPoint = a2;
             }
             if (i3)
             {
                 closestPoint = b1;
             }
             if (i4)
             {
                 closestPoint = b2;
             }
            //
            // var normal_QR = R - Q;
            // normal_QR = new Vector2(-normal_QR.y, normal_QR.x).normalized;
            // normal_QR *= minDistance;
            // UnityVectorExtensions.FindIntersection(Q, R, P, P + normal_QR, false, 
            //     out bool lines_intersect,
            //     out bool segments_intersect, 
            //     out Vector2 intersection);
            //
            // Vector2 closestPoint;
            // if (segments_intersect)
            // {
            //     closestPoint = intersection;
            // }
            // else
            // {
            //     closestPoint = Q;
            // }
            


            return closestPoint;
        }
        
        private Vector2 GetIntersection(in Vector2 P, in Vector2 Q, in Vector2 R, bool normal, in float minDistance, out bool intersect)
        {
            var normal_QR = R - Q;
            if (normal)
            {
                normal_QR = new Vector2(-normal_QR.y, normal_QR.x).normalized;
            }
            else
            {
                normal_QR = new Vector2(normal_QR.y, -normal_QR.x).normalized;
            }
            normal_QR *= minDistance;
            FindIntersection(Q, R, P, P + normal_QR, false, 
                out bool lines_intersect,
                out bool segments_intersect, 
                out Vector2 intersection);

            intersect = segments_intersect;
            
            Vector2 closestPoint;
            if (segments_intersect)
            {
                closestPoint = intersection;
            }
            else
            {
                closestPoint = Q;
            }

            return closestPoint;
        }

        internal bool IsClosestPointToAnyIntersection(int pointIndex)
        {
            for (int i = 0; i < intersectionPoints.Count; ++i)
            {
                int closestIndex = 0;
                float minDistance = float.MaxValue;
                for (int j = 0; j < points.Count; ++j)
                {
                    float distance = (intersectionPoints[i] - points[j].position).sqrMagnitude;
                    if (minDistance > distance)
                    {
                        minDistance = distance;
                        closestIndex = j;
                    }
                }

                if (closestIndex == pointIndex)
                {
                    return true;
                }
            }

            return true;
        }

        internal Vector2 CenterOfMass()
        {
            Vector2 center = Vector2.zero;
            for (int i = 0; i < points.Count; ++i)
            {
                center += points[i].position;
            }

            return center / points.Count;
        }

        // Removes point that are the same or very close
        internal void Simplify()
        {
            for (int i = 0; i < points.Count; ++i)
            {
                for (int j = i + 1; j < points.Count; ++j)
                {
                    if ((points[i].position - points[j].position).sqrMagnitude <= 0.5f)
                    {
                        points.RemoveAt(j);
                    }
                }
            }
        }


        // internal bool SetOrientationClockwise()
        // {
        //     // NOTE: invalidates normals!
        //     if (!ComputeSignedArea(points.ToArray()))
        //     {
        //         //points.Reverse();
        //         return true;
        //     }
        //
        //     return false;
        // }

        
        // TODO: refine summary outside and within this function - DivideGraph and also DivideAlongIntersections
        /// <summary>Divides graph into subgraphs if there are intersections.</summary>
        /// <param name="graph">Graph to divide. Graph will be overwritten by a graph with possible intersections,
        /// after cutting off the graph part 'left' of the intersection.</param>
        /// <param name="subgraphs">Resulting subgraphs from dividing graph.</param>
        /// <returns>True, if found intersection. False, otherwise.</returns>
        private static bool DivideGraph(ref Graph graph, ref List<Graph> subgraphs, bool woobly)
        {
            // for each edge in graph, but not edges that directly connect (e.P. 0-1, 1-2) check for intersections.
            // if we intersect, we need to divide the graph into two graphs (g1,g2) to remove the intersection within a graph.
            // g1 will be 'left' of the intersection, g2 will be 'right' of the intersection.
            // g2 may contain additional intersections.
            for (int i = 0; i < graph.points.Count; ++i)
            {
                for (int j = i + 2; j < graph.points.Count; ++j)
                {
                    if (i == (j + 1) % graph.points.Count) continue;

                    FindIntersection(
                        graph.points[i].position, graph.points[(i + 1) % graph.points.Count].position,
                        graph.points[j].position, graph.points[(j + 1) % graph.points.Count].position,
                        woobly,
                        out bool linesIntersect, out bool segmentsIntersect,
                        out Vector2 intersection);
                    
                    if (segmentsIntersect)
                    {
                        // TODO: check orientation of g1, g2
                        // divide graph into g1, g2. Then graph = g2

                        // TODO: starting index of new graph should be the left-most index
                        
                        // g1 will be left from the intersection, g2 will be right of the intersection.
                        Graph g1 = new Graph();
                        {
                            g1.sensorRatio = graph.sensorRatio;
                            g1.windowDiagonal = graph.windowDiagonal;
                            g1.intersectionPoints.Add(intersection);
                            g1.state = graph.state + 1;

                            // g1 -> intersection j+1 ... i
                            List<Point2> points = new List<Point2>();
                            points.Add(new Point2
                            {
                                position = intersection,
                                normal = Vector2.zero,
                            });
                            for (int k = (j + 1) % graph.points.Count;
                                k != (i + 1) % graph.points.Count;
                                k = (k + 1) % graph.points.Count)
                            {
                                points.Add(graph.points[k]);
                            }
                            
                            g1.points = RotateListToLeftmost(points);
                            // g1.ComputeNormals();
                            // g1.FlipNormals();
                        }
                        subgraphs.Add(g1);

                        Graph g2 = new Graph();
                        {
                            g2.sensorRatio = graph.sensorRatio;
                            g2.windowDiagonal = graph.windowDiagonal;
                            g2.intersectionPoints.Add(intersection);
                            g2.state = graph.state + 1;

                            // g2 -> intersection i+1 ... j
                            List<Point2> points = new List<Point2>();
                            points.Add(new Point2
                            {
                                position = intersection,
                                normal = Vector2.zero,
                            });
                            for (int k = (i + 1) % graph.points.Count;
                                k != (j + 1) % graph.points.Count;
                                k = (k + 1) % graph.points.Count)
                            {
                                points.Add(graph.points[k]);
                            }

                            // points[0].normal = (points[1].normal + points[points.Count - 1].normal) / 2f; // normal at intersection
                            //Graph.ComputeNormalAt(0, points);
                            g2.points = RollListToStartClosestToPoint(points, intersection);
                            // g2.ComputeNormals();
                            // g2.FlipNormals();
                        }

                        // we need to move the intersection points from the parent graph
                        // to g1 and g2 graphs, depending on which is closer to the intersection point.
                        for (int k = 0; k < graph.intersectionPoints.Count; ++k)
                        {
                            float g1Dist = g1.SqrDistanceTo(graph.intersectionPoints[k]);
                            float g2Dist = g2.SqrDistanceTo(graph.intersectionPoints[k]);
                            if (g1Dist < g2Dist)
                            {
                                g1.intersectionPoints.Add(graph.intersectionPoints[k]);
                            }
                            else
                            {
                                g2.intersectionPoints.Add(graph.intersectionPoints[k]);
                            }
                        }

                        graph = g2;
                        return true; // graph has nice intersections
                    }
                }
            }

            return false; // graph does not have nice intersections
        }

        internal static void DivideAlongIntersections(Graph graph, bool woobly, out List<Graph> subgraphs)
        {
            /// 2. DO until Graph G has intersections
            /// 2.a.: Found 1 intersection, divide G into g1, g2. Then, G=g2, continue from 2.
            /// Result of 2 is G in subgraphs without intersections: g1, g2, ..., gn.
            /// done.
            subgraphs = new List<Graph>();
            int maxIteration = 500;
            while (maxIteration > 0 && DivideGraph(ref graph, ref subgraphs, woobly))
            {
                maxIteration--;
            };
            if (maxIteration <= 0)
            {
                Debug.Log("Exited with max iteration safety!");
            }
            subgraphs.Add(graph); // add remaining graph
        }
        
        /// <summary>
        /// Rotates input List to start closest to point in 2D space.
        /// </summary>
        /// <param name="point">List will rotate so it's 0th element is as close to point as possible.</param>
        /// <param name="points">List to rotate</param>
        /// <returns>List, in which the 0 element is the closest point in the List to point in 2D space.
        /// Order of points of the original List is preserved</returns>
        private static List<Point2> RollListToStartClosestToPoint(in List<Point2> points, in Vector2 point)
        {
            int closestIndex = 0;
            Vector2 closestPoint = points[0].position;
            for (int i = 1; i < points.Count; ++i)
            {
                if ((closestPoint - point).sqrMagnitude > (closestPoint - points[i].position).sqrMagnitude)
                {
                    closestIndex = i;
                    closestPoint = points[i].position;
                }
            }

            var point_rolledToStartAtClosestPoint = new List<Point2>(points.Count);
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
        /// Order of points of the original List is preserved</returns>
        public static List<Point2> RotateListToLeftmost(List<Point2> points)
        {
            int leftMostPointIndex = 0;
            Vector2 leftMostPoint = points[0].position;
            for (int i = 1; i < points.Count; ++i)
            {
                if (leftMostPoint.x > points[i].position.x)
                {
                    leftMostPointIndex = i;
                    leftMostPoint = points[i].position;
                }
            }

            var point_rolledToStartAtLeftmostpoint = new List<Point2>(points.Count);
            for (int i = leftMostPointIndex; i < points.Count; ++i)
            {
                point_rolledToStartAtLeftmostpoint.Add(points[i]);
            }

            for (int i = 0; i < leftMostPointIndex; ++i)
            {
                point_rolledToStartAtLeftmostpoint.Add(points[i]);
            }

            return point_rolledToStartAtLeftmostpoint;
        }
        
        private static void FindIntersection(
            in Vector2 p1, in Vector2 p2, in Vector2 p3, in Vector2 p4, in bool woobly,
            out bool lines_intersect, out bool segments_intersect, out Vector2 intersection)
        {
            // Get the segments' parameters.
            float dx12 = p2.x - p1.x;
            float dy12 = p2.y - p1.y;
            float dx34 = p4.x - p3.x;
            float dy34 = p4.y - p3.y;

            // Solve for t1 and t2
            float denominator = (dy12 * dx34 - dx12 * dy34);

            float t1 =
                ((p1.x - p3.x) * dy34 + (p3.y - p1.y) * dx34)
                / denominator;
            if (float.IsInfinity(t1))
            {
                // The lines are parallel (or close enough to it).
                lines_intersect = false;
                segments_intersect = false;
                intersection = Vector2.positiveInfinity;
                return;
            }
            lines_intersect = true;

            float t2 = ((p3.x - p1.x) * dy12 + (p1.y - p3.y) * dx12) / -denominator;

            // Find the point of intersection.
            intersection = new Vector2(p1.x + dx12 * t1, p1.y + dy12 * t1);

            // The segments intersect if t1 and t2 are between 0 and 1.
            if (woobly)
            {
                segments_intersect = t1 >= -0.3f && t1 <= 1.3f && t2 >= -0.3f && t2 <= 1.3f;
            }
            else
            {
                segments_intersect = t1 >= 0 && t1 <= 1 && t2 >= 0 && t2 <= 1;
            }
        }

    }
}
