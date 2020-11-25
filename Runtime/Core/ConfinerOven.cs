using System.Collections.Generic;
using Cinemachine.Utility;
using UnityEngine;
using ClipperLib;

namespace Cinemachine
{
    /// <summary>
    /// Responsible for baking confiners via BakeConfiner function.
    /// </summary>
    internal class ConfinerOven
    {
        public float MaxFrustumHeight { get; private set; }
        private float m_MinFrustumHeightWithBones;

        private List<List<IntPoint>> m_ClipperInput;
        private List<List<IntPoint>> m_Skeleton = new List<List<IntPoint>>();
        private float m_FrustumHeightOfSolution;
        private List<List<IntPoint>> m_Solution = new List<List<IntPoint>>();
        
        private const long k_FloatToIntScaler = 100000;
        private const float k_IntToFloatScaler = 1.0f / k_FloatToIntScaler;
        private const float k_MinStepSize = 0.005f;
        
        private float m_SqrPolygonDiagonal;
        private Rect m_PolygonRect;
        private float m_CenterX; // used for aspect ratio scaling
        private float m_AspectRatio;
        
        public Vector2 ConfinePoint(in Vector2 pointToConfine)
        {
            Vector2 pInConfinerSpace = pointToConfine;
            pInConfinerSpace.x = (pInConfinerSpace.x - m_CenterX) * (1f / m_AspectRatio) + m_CenterX;
            IntPoint p = new IntPoint(pInConfinerSpace.x * k_FloatToIntScaler, pInConfinerSpace.y * k_FloatToIntScaler);
            foreach (List<IntPoint> sol in m_Solution)
            {
                if (Clipper.PointInPolygon(p, sol) != 0) // 0: outside, +1: inside , -1: point on poly boundary
                {
                    return pointToConfine; // inside, no confinement needed
                } 
            }

            // If the poly has bones and if the position to confine is not outside of the original
            // bounding shape, then it is possible that the bone in a neighbouring section
            // is closer than the bone in the correct section of the polygon, if the current section 
            // is very large and the neighbouring section is small.  In that case, we'll need to 
            // add an extra check when calculating the nearest point.
            bool hasBone = m_MinFrustumHeightWithBones < m_FrustumHeightOfSolution;
            bool checkIntersectOriginal = hasBone && IsInsideOriginal(p);
            // Confine point
            IntPoint closest = p;
            double minDistance = double.MaxValue;
            for (int i = 0; i < m_Solution.Count; ++i)
            {
                int numPoints = m_Solution[i].Count;
                for (int j = 0; j < numPoints; ++j)
                {
                    IntPoint l1 = m_Solution[i][j];
                    IntPoint l2 = m_Solution[i][(j + 1) % numPoints];

                    IntPoint c = IntPointLerp(l1, l2, ClosestPointOnSegment(p, l1, l2));
                    IntPoint difference = new IntPoint
                    {
                        X = p.X - c.X,
                        Y = p.Y - c.Y,
                    };
                    double diffX = difference.X;
                    double diffY = difference.Y;
                    double distance = diffX * diffX + diffY * diffY;
                    
                    if (Mathf.Abs(difference.X) > m_PolygonRect.width * k_FloatToIntScaler || 
                        Mathf.Abs(difference.Y) > m_PolygonRect.height * k_FloatToIntScaler)
                    {
                        // penalty for points from which the target is not visible, prefering visibility over proximity
                        distance += m_SqrPolygonDiagonal; 
                    }

                    if (distance < minDistance 
                        && (!checkIntersectOriginal || !DoesIntersectOriginal(p, c)))
                    {
                        minDistance = distance;
                        closest = c;
                    }
                }
            }
            
            var result = new Vector2(closest.X * k_IntToFloatScaler, closest.Y * k_IntToFloatScaler);
            result.x = (result.x - m_CenterX) * m_AspectRatio + m_CenterX;
            return result; 
        }

        private bool IsInsideOriginal(IntPoint p)
        {
            foreach (List<IntPoint> original in m_ClipperInput)
            {
                if (Clipper.PointInPolygon(p, original) != 0)
                {
                    return true;
                }
            }
            return false;
        }

        private float ClosestPointOnSegment(IntPoint p, IntPoint s0, IntPoint s1)
        {
            double sX = s1.X - s0.X;
            double sY = s1.Y - s0.Y;
            double len2 = sX * sX + sY * sY;
            if (len2 < m_ClipperEpsilon)
                return 0; // degenerate segment
            
            double s0pX = p.X - s0.X;
            double s0pY = p.Y - s0.Y;
            double dot = s0pX * sX + s0pY * sY;
            return Mathf.Clamp01((float) (dot / len2));
        }

        private IntPoint IntPointLerp(IntPoint a, IntPoint b, float lerp)
        {
            return new IntPoint {
                X = Mathf.RoundToInt(a.X + (b.X - a.X) * lerp),
                Y = Mathf.RoundToInt(a.Y + (b.Y - a.Y) * lerp),
            };
        }

        private bool DoesIntersectOriginal(IntPoint l1, IntPoint l2)
        {
            foreach (var original in m_ClipperInput)
            {
                int numPoints = original.Count;
                for (int i = 0; i < numPoints; ++i)
                {
                    if (FindIntersection(l1, l2, 
                        original[i], original[(i + 1) % numPoints]) == 2)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private const double m_ClipperEpsilon = 0.01f * k_FloatToIntScaler;
        private int FindIntersection(in IntPoint p1, in IntPoint p2, in IntPoint p3, in IntPoint p4)
        {
            // Get the segments' parameters.
            double dx12 = p2.X - p1.X;
            double dy12 = p2.Y - p1.Y;
            double dx34 = p4.X - p3.X;
            double dy34 = p4.Y - p3.Y;

            // Solve for t1 and t2
            double denominator = dy12 * dx34 - dx12 * dy34;
            double t1 =
                ((p1.X - p3.X) * dy34 + (p3.Y - p1.Y) * dx34)
                / denominator;
            if (double.IsInfinity(t1) || double.IsNaN(t1))
            {
                // The lines are parallel (or close enough to it).
                if (IntPointDiffSqrMagnitude(p1, p3) < m_ClipperEpsilon || IntPointDiffSqrMagnitude(p1, p4) < m_ClipperEpsilon ||
                    IntPointDiffSqrMagnitude(p2, p3) < m_ClipperEpsilon || IntPointDiffSqrMagnitude(p2, p4) < m_ClipperEpsilon)
                {
                    return 2; // they are the same line, or very close parallels
                }
                return 0; // no intersection
            }
            
            double t2 = ((p3.X - p1.X) * dy12 + (p1.Y - p3.Y) * dx12) / -denominator;
            return (t1 >= 0 && t1 <= 1 && t2 >= 0 && t2 < 1) ? 2 : 1; // 2 = segments intersect, 1 = lines intersect
        }

        private double IntPointDiffSqrMagnitude(IntPoint p1, IntPoint p2)
        {
            double X = p1.X - p2.X;
            double Y = p1.Y - p2.Y;
            return X * X + Y * Y;
        }
        
        /// <summary>
        /// Converts and returns a prebaked ConfinerState for the input frustumHeight.
        /// </summary>
        private List<List<Vector2>> CalculateConfinerAtFrustumHeight(float frustumHeight)
        {
            // Inflate with clipper to frustumHeight
            var offsetter = new ClipperOffset();
            offsetter.AddPaths(m_ClipperInput, JoinType.jtMiter, EndType.etClosedPolygon);
            List<List<IntPoint>> solution = new List<List<IntPoint>>();
            offsetter.Execute(ref solution, -1f * frustumHeight * k_FloatToIntScaler);

            // Add in the skeleton
            m_FrustumHeightOfSolution = frustumHeight;
            m_Solution.Clear();
            Clipper c = new Clipper();
            c.AddPaths(solution, PolyType.ptSubject, true);
            c.AddPaths(m_Skeleton, PolyType.ptClip, true);
            c.Execute(ClipType.ctUnion, m_Solution, PolyFillType.pftEvenOdd, PolyFillType.pftEvenOdd);

            if (Application.isEditor) // #if UNITY_EDITOR
            {
                // Convert to client space
                var numPaths = m_Solution.Count;
                var paths = new List<List<Vector2>>(numPaths);
                for (int i = 0; i < numPaths; ++i)
                {
                    var srcPoly = m_Solution[i];
                    int numPoints = srcPoly.Count;
                    var pathSegment = new List<Vector2>(numPoints);
                    for (int j = 0; j < numPoints; j++)
                    {
                        // Restore the original aspect ratio
                        var x = srcPoly[j].X * k_IntToFloatScaler;
                        x = (x - m_CenterX) * m_AspectRatio + m_CenterX;
                        pathSegment.Add(new Vector2(x, srcPoly[j].Y * k_IntToFloatScaler));
                    }
                    paths.Add(pathSegment);
                }
                return paths;
            } //#endif
            return null;
        }

        internal List<List<Vector2>> m_Path = null;
        public void ValidateCache(in bool confinerStateChanged, in float frustumHeight)
        {
            if (confinerStateChanged || m_Path == null 
                                     || Mathf.Abs(frustumHeight - m_FrustumHeightOfSolution) > k_MinStepSize)
            {
                m_Path = CalculateConfinerAtFrustumHeight(frustumHeight);
            }
        }
        
        private struct PolygonSolution
        {
            public List<List<IntPoint>> m_Polygons;
            public float m_FrustumHeight;

            public bool StateChanged(in List<List<IntPoint>> paths)
            {
                if (paths.Count != m_Polygons.Count)
                    return true;
                for (var i = 0; i < paths.Count; i++)
                {
                    if (paths[i].Count != m_Polygons[i].Count)
                        return true;
                }
                return false;
            }

            public bool IsEmpty => m_Polygons == null;
        }

        /// <summary>
        /// Creates shrinkable polygons from input parameters.
        /// The algorithm is divide and conquer. It iteratively shrinks down the input 
        /// polygon towards its shrink directions. If the polygon intersects with itself, 
        /// then we divide the polygon into two polygons at the intersection point, and 
        /// continue the algorithm on these two polygons separately. We need to keep track of
        /// the connectivity information between sub-polygons.
        /// </summary>
        public void BakeConfiner(in List<List<Vector2>> inputPath, in float aspectRatio, float maxFrustumHeight)
        {
            // Compute the aspect-adjusted height of the polygon bounding box
            m_PolygonRect = GetPolygonBoundingBox(inputPath);

            // Cache the polygon diagonal 
            m_SqrPolygonDiagonal = m_PolygonRect.width * m_PolygonRect.width + 
                                   m_PolygonRect.height * m_PolygonRect.height;

            // Ensuring that we don't compute further than what is the theoretical max
            float polygonHalfHeight = m_PolygonRect.height / 2f;
            if (maxFrustumHeight == 0 || maxFrustumHeight > polygonHalfHeight) // exact comparison to 0 is intentional!
            {
                maxFrustumHeight = polygonHalfHeight; 
            }

            m_CenterX = m_PolygonRect.center.x;
            m_AspectRatio = aspectRatio;

            // Initialize clipper
            m_ClipperInput = new List<List<IntPoint>>(inputPath.Count);
            for (var i = 0; i < inputPath.Count; ++i)
            {
                var xScale = 1 / aspectRatio;

                var srcPath = inputPath[i];
                int numPoints = srcPath.Count;
                var path = new List<IntPoint>(numPoints);
                for (int j = 0; j < numPoints; ++j)
                {
                    // Neutralize the aspect ratio
                    var x = (srcPath[j].x - m_CenterX) * xScale + m_CenterX;
                    path.Add(new IntPoint(x * k_FloatToIntScaler, srcPath[j].y * k_FloatToIntScaler));
                }
                m_ClipperInput.Add(path);
            }
            
            var offsetter = new ClipperOffset();
            offsetter.AddPaths(m_ClipperInput, JoinType.jtMiter, EndType.etClosedPolygon);

            List<List<IntPoint>> solution = new List<List<IntPoint>>();
            offsetter.Execute(ref solution, 0);
            
            List<PolygonSolution> solutions = new List<PolygonSolution>();
            solutions.Add(new PolygonSolution
            {
                m_Polygons = solution,
                m_FrustumHeight = 0,
            });

            // Binary search for next non-lerpable state
            PolygonSolution rightCandidate = new PolygonSolution();
            PolygonSolution leftCandidate = new PolygonSolution
            {
                m_Polygons = solution,
                m_FrustumHeight = 0,
            };
            float currentFrustumHeight = 0;
            
            float maxStepSize = maxFrustumHeight;
            float stepSize = maxStepSize;
            while (solutions.Count < 1000)
            {
                bool stateChangeFound = false;
                var numPaths = leftCandidate.m_Polygons.Count;
                var candidate = new List<List<IntPoint>>(numPaths);

                stepSize = Mathf.Min(stepSize, maxStepSize - leftCandidate.m_FrustumHeight);
#if false
                Debug.Log($"States = {solutions.Count}, "
                          + $"Frustum height = {currentFrustumHeight}, stepSize = {stepSize}");
#endif
                currentFrustumHeight = leftCandidate.m_FrustumHeight + stepSize;
                offsetter.Execute(ref candidate, -1f * currentFrustumHeight * k_FloatToIntScaler);
                stateChangeFound = leftCandidate.StateChanged(in candidate);

                if (stateChangeFound)
                {
                    rightCandidate = new PolygonSolution
                    {
                        m_Polygons = candidate,
                        m_FrustumHeight = currentFrustumHeight,
                    };
                    stepSize = Mathf.Max(stepSize / 2f, k_MinStepSize);
                }
                else
                {
                    leftCandidate = new PolygonSolution
                    {
                        m_Polygons = candidate,
                        m_FrustumHeight = currentFrustumHeight,
                    };

                    // if we have not found right yet, then we don't need to decrease stepsize
                    if (!rightCandidate.IsEmpty)
                    {
                        stepSize = Mathf.Max(stepSize / 2f, k_MinStepSize);
                    }
                }
                
                // if we have a right candidate, and left and right are sufficiently close, 
                // then we have located a state change point
                if (!rightCandidate.IsEmpty && stepSize <= k_MinStepSize)
                {
                    // Add both states: one before the state change and one after
                    solutions.Add(leftCandidate);
                    solutions.Add(rightCandidate);

                    leftCandidate = rightCandidate;
                    rightCandidate = new PolygonSolution();
                    
                    // Back to max step.  GML todo: this can be smaller now
                    stepSize = maxStepSize;
                }
                else if (rightCandidate.IsEmpty && leftCandidate.m_FrustumHeight >= maxFrustumHeight)
                {
                    solutions.Add(leftCandidate);
                    break; // stop shrinking, because we are at the bound
                }
                else
                {
                    continue; // keep searching for a closer left and right or a non-null right
                }
            }

            // Cache the max confinable view size
            MaxFrustumHeight = solutions.Count == 0 ? 0 : solutions[solutions.Count-1].m_FrustumHeight;
            m_MinFrustumHeightWithBones = solutions.Count <= 1 ? 0 : solutions[1].m_FrustumHeight;
            
            ComputeSkeleton(in solutions);
        }
        
        private static Rect GetPolygonBoundingBox(in List<List<Vector2>> polygons)
        {
            float minX = float.PositiveInfinity, maxX = float.NegativeInfinity;
            float minY = float.PositiveInfinity, maxY = float.NegativeInfinity;
            for (int i = 0; i < polygons.Count; ++i)
            {
                var path = polygons[i];
                for (int j = 0; j < path.Count; ++j)
                {
                    var p = path[j];
                    minX = Mathf.Min(minX, p.x);
                    maxX = Mathf.Max(maxX, p.x);
                    minY = Mathf.Min(minY, p.y);
                    maxY = Mathf.Max(maxY, p.y);
                }
            }
            return new Rect(minX, minY, Mathf.Max(0, maxX - minX), Mathf.Max(0, maxY - minY));
        }
        
        private void ComputeSkeleton(in List<PolygonSolution> solutions)
        {
            m_Skeleton.Clear();

            // At each state change point, collect geometry that gets lost over the transition
            Clipper clipper = new Clipper();
            var offsetter = new ClipperOffset();
            for (int i = 1; i < solutions.Count - 1; i += 2)
            {
                var prev = solutions[i];
                var next = solutions[i+1];

                const int padding = 5; // to counteract precision problems - inflates small regions
                double step = padding * k_FloatToIntScaler * (next.m_FrustumHeight - prev.m_FrustumHeight);

                // Grow the larger polygon to inflate marginal regions
                List<List<IntPoint>> expandedPrev = new List<List<IntPoint>>();
                offsetter.Clear();
                offsetter.AddPaths(prev.m_Polygons, JoinType.jtMiter, EndType.etClosedPolygon);
                offsetter.Execute(ref expandedPrev, step);

                // Grow the smaller polygon to be a bit bigger than the expanded larger one
                List<List<IntPoint>> expandedNext = new List<List<IntPoint>>();
                offsetter.Clear();
                offsetter.AddPaths(next.m_Polygons, JoinType.jtMiter, EndType.etClosedPolygon);
                offsetter.Execute(ref expandedNext, step * 2);

                // Compute the difference - this is the lost geometry
                var solution = new List<List<IntPoint>>();
                clipper.Clear();
                clipper.AddPaths(expandedPrev, PolyType.ptSubject, true);
                clipper.AddPaths(expandedNext, PolyType.ptClip, true);
                clipper.Execute(ClipType.ctDifference, solution, PolyFillType.pftEvenOdd, PolyFillType.pftEvenOdd);

                // Add that lost geometry to the skeleton
                m_Skeleton.AddRange(solution);
            }
        }
    }
}