using System.Collections.Generic;
using Cinemachine.Utility;
using UnityEngine;

namespace Cinemachine
{
    public class ConfinerState
    {
        public List<Graph> graphs;
        public float windowSize;
        public int state;
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

        internal List<Vector2> intersectionPoints;

        public Graph()
        {
            points = new List<Point2>();
            intersectionPoints = new List<Vector2>();
            area = 0;
            windowDiagonal = 0;
        }

        /// <summary>
        /// Creates and returns a deep copy of this graph.
        /// </summary>
        /// <returns>Deep copy of this graph</returns>
        public Graph DeepCopy()
        {
            Graph deepCopy = new Graph();
            deepCopy.points = this.points.ConvertAll(point => new Point2(point.position, point.normal));
            deepCopy.ClockwiseOrientation = this.ClockwiseOrientation;
            deepCopy.area = this.area;
            deepCopy.intersectionPoints = this.intersectionPoints.ConvertAll(intersection => 
                new Vector2(intersection.x, intersection.y));
            deepCopy.windowDiagonal = windowDiagonal;
            deepCopy.sensorRatio = sensorRatio;
            return deepCopy;
        }

        internal float ComputeArea()
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
        /// <summary> Computes square-normalized normals for all points,
        /// which means the normals are clamped to the square defined by the 4 normalized corner-diagonals.
        /// </summary>
        internal void ComputeNormals()
        {
            var edgeNormals = new List<Vector2>(points.Count);
            for (int i = 0; i < points.Count; ++i)
            {
                Vector2 edge = points[(i + 1) % points.Count].position - points[i].position;
                Vector2 normal = ClockwiseOrientation ? new Vector2(edge.y, -edge.x) : new Vector2(-edge.y, edge.x);
                edgeNormals.Add(normal.normalized);
            }

            for (int i = 0; i < points.Count; ++i)
            {
                int prevIndex = i == 0 ? points.Count - 1 : i - 1;
                Vector2 normal = (edgeNormals[i] + edgeNormals[prevIndex]) / 2f;
                points[i].normal = SquareNormalize(normal);
                // points[i].normal = normal.normalized;
                // points[i].normal.x =
                //     Mathf.Clamp(points[i].normal.x, -oneOverSquarerootOfTwo, oneOverSquarerootOfTwo);
                // points[i].normal.y =
                //     Mathf.Clamp(points[i].normal.y, -oneOverSquarerootOfTwo, oneOverSquarerootOfTwo);
            }
        }

        /// <summary>
        /// Instead of normalizing a vector in a circle with a set a radius, this function normalizes the vector to be
        /// within a rectangle with sides (a, 1). Meaning, the maximum length is a and 1 for the x and y components of the
        /// vector respectively.
        /// </summary>
        /// <param name="normal">Normal to SquareNormalize</param>
        /// <returns>SquareNormalized normal</returns>
        internal Vector2 SquareNormalize(Vector2 normal)
        {
            Vector2 n = normal.normalized * 10 * sensorRatio;
            n.x = Mathf.Clamp(n.x, -sensorRatio, sensorRatio);
            n.y = Mathf.Clamp(n.y, -1, 1);
            return n;
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

        internal void Shrink(float shrinkAmount)
        {
            windowDiagonal += shrinkAmount;
            // TODO: optimize shrink - shrink until intersection instead of steps
            for (int i = 0; i < points.Count; ++i)
            {
                points[i].position += points[i].normal * shrinkAmount;
            }
        }

        /// <summary></summary>
        /// <param name="p">Point in space.</param>
        /// <returns>Squared distance to 'p' from closest point to 'p' in the graph</returns>
        internal float SqrDistanceTo(Vector2 p)
        {
            float minDistance = float.MaxValue;
            for (int i = 0; i < points.Count; ++i)
            {
                minDistance = Mathf.Min(minDistance, (points[i].position - p).sqrMagnitude);
            }

            return minDistance;
        }

        internal Vector2 ClosestPoint(Vector2 p)
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
                    if ((points[i].position - points[j].position).sqrMagnitude <= 0.1f)
                    {
                        points.RemoveAt(j);
                    }
                }
            }
        }


        // internal bool SetOrientationClockwise()
        // {
        //     // NOTE: invalidates normals!
        //     if (!ComputeArea(points.ToArray()))
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
        private static bool DivideGraph(ref Graph graph, ref List<Graph> subgraphs)
        {
            // for each edge in graph, but not edges that directly connect (e.g. 0-1, 1-2) check for intersections.
            // if we intersect, we need to divide the graph into two graphs (g1,g2) to remove the intersection within a graph.
            // g1 will be 'left' of the intersection, g2 will be 'right' of the intersection.
            // g2 may contain additional intersections.
            for (int i = 0; i < graph.points.Count; ++i)
            {
                for (int j = i + 2; j < graph.points.Count; ++j)
                {
                    if (i == (j + 1) % graph.points.Count) continue;

                    UnityVectorExtensions.FindIntersection(
                        graph.points[i].position, graph.points[(i + 1) % graph.points.Count].position,
                        graph.points[j].position, graph.points[(j + 1) % graph.points.Count].position,
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

                            // TODO: instead of Roll To LeftMost we need to roll to closest point to prev graph, see TestComplex (1) why
                            // points[0].normal = (points[1].normal + points[points.Count - 1].normal) / 2f; // normal at intersection
                            //Graph.ComputeNormalAt(0, points);
                            g1.points = RotateListToLeftmost(points);
                            g1.ComputeNormals();
                            g1.FlipNormals();
                        }
                        subgraphs.Add(g1);

                        Graph g2 = new Graph();
                        {
                            g2.sensorRatio = graph.sensorRatio;
                            g2.windowDiagonal = graph.windowDiagonal;
                            g2.intersectionPoints.Add(intersection);

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
                            g2.ComputeNormals();
                            g2.FlipNormals();
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

        internal static void DivideAlongIntersections(Graph graph, out List<Graph> subgraphs)
        {
            /// 2. DO until Graph G has intersections
            /// 2.a.: Found 1 intersection, divide G into g1, g2. Then, G=g2, continue from 2.
            /// Result of 2 is G in subgraphs without intersections: g1, g2, ..., gn.
            /// done.
            subgraphs = new List<Graph>();
            int maxIteration = 5000;
            while (maxIteration > 0 && DivideGraph(ref graph, ref subgraphs))
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
            // TODO: instead of Roll To LeftMost we need to roll to closest point to prev graph, see TestComplex (1) and (2) for why
            // so RollListTo(List<Vector2> points, Vector2 p); where p will be left intersection point...
            // todo: better connectivity
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

    }
}
