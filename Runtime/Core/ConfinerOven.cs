using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using ClipperLib;

namespace Cinemachine
{
    /// <summary>
    /// Responsible for baking confiners via BakeConfiner function.
    /// </summary>
    internal class ConfinerOven
    {
        public float SqrPolygonDiagonal { get; private set; }
        public float MaxFrustumHeight { get; private set; }

        private List<List<IntPoint>> clipperInput;
        private List<List<IntPoint>> m_Skeleton;

        const long k_FloatToIntScaler = 10000000; // same as in Physics2D
        const float k_IntToFloatScaler = 1.0f / k_FloatToIntScaler;
        const float k_MinStepSize = 0.005f; 

        private float m_CenterX; // used for aspect ratio scaling
        private float m_AspectRatio;
            
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
        public void BakeConfiner(
            in List<List<Vector2>> inputPath, in float aspectRatio, float maxFrustumHeight)
        {
            // Compute the aspect-adjusted height of the polygon bounding box
            var polygonRect = GetPolygonBoundingBox(inputPath);
            float polygonHeight = polygonRect.height / aspectRatio; // GML todo: why are we adjusting it for aspect?

            // Cache the polygon diagonal 
            SqrPolygonDiagonal = polygonRect.width * polygonRect.width + polygonHeight * polygonHeight;

            // Ensuring that we don't compute further than what is the theoretical max
            float polygonHalfHeight = polygonHeight;
            if (maxFrustumHeight == 0 || maxFrustumHeight > polygonHalfHeight) // exact comparison to 0 is intentional!
            {
                maxFrustumHeight = polygonHalfHeight; 
            }

            m_CenterX = polygonRect.center.x;
            m_AspectRatio = aspectRatio;

            // Initialize clipper
            clipperInput = new List<List<IntPoint>>(inputPath.Count);
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
                clipperInput.Add(path);
            }
            
            var offsetter = new ClipperOffset();
            offsetter.AddPaths(clipperInput, JoinType.jtMiter, EndType.etClosedPolygon);

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
            
            float maxStepSize = polygonHalfHeight / 4f;
            float stepSize = maxStepSize;
            while (solutions.Count < 1000)
            {
#if false
                Debug.Log($"States = {m_Solutions.Count}, "
                    + $"Frustum height = {currentFrustumHeight}, stepSize = {stepSize}");
#endif
                bool stateChangeFound = false;
                var numPaths = leftCandidate.m_Polygons.Count;
                var candidate = new List<List<IntPoint>>(numPaths);

                stepSize = Mathf.Min(stepSize, maxFrustumHeight - leftCandidate.m_FrustumHeight);
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
            m_Skeleton = ComputeSkeleton(in solutions);
        }

        /// <summary>
        /// Converts and returns a prebaked ConfinerState for the input frustumHeight.
        /// </summary>
        public List<List<Vector2>> GetConfinerAtFrustumHeight(float frustumHeight)
        {
            // Get the best solution
            // var solution = new PolygonSolution();
            // for (int i = m_Solutions.Count - 1; i >= 0; --i)
            // {
            //     if (m_Solutions[i].m_FrustumHeight <= frustumHeight)
            //     {
            //         if (i == m_Solutions.Count - 1)
            //             solution = m_Solutions[i];
            //         else if (i % 2 == 0)
            //             solution = ClipperSolutionLerp(m_Solutions[i], m_Solutions[i+1], frustumHeight);
            //         else if (Mathf.Abs(m_Solutions[i].m_FrustumHeight - frustumHeight) < 
            //                  Mathf.Abs(m_Solutions[i + 1].m_FrustumHeight - frustumHeight))
            //             solution = m_Solutions[i];
            //         else
            //             solution = m_Solutions[i + 1];
            //         break;
            //     }
            // }

            // Inflate with clipper to frustumHeight
            var offsetter = new ClipperOffset();
            offsetter.AddPaths(clipperInput, JoinType.jtMiter, EndType.etClosedPolygon);
            List<List<IntPoint>> solution = new List<List<IntPoint>>();
            offsetter.Execute(ref solution, -1f * frustumHeight * k_FloatToIntScaler);
            
            
            // Add in the skeleton
            var skeletonAdded = new List<List<IntPoint>>();
            Clipper c = new Clipper();
            c.AddPaths(solution, PolyType.ptSubject, true);
            c.AddPaths(m_Skeleton, PolyType.ptClip, true);
            c.Execute(ClipType.ctUnion, skeletonAdded, PolyFillType.pftEvenOdd, PolyFillType.pftEvenOdd);

            // Convert to client space
            var numPaths = skeletonAdded.Count;
            var paths = new List<List<Vector2>>(numPaths);
            for (int i = 0; i < numPaths; ++i)
            {
                var srcPoly = skeletonAdded[i];
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

        /// <summary>
        /// Linearly interpolates between ConfinerStates.
        /// </summary>
        private PolygonSolution ClipperSolutionLerp(
            in PolygonSolution left, in PolygonSolution right, float frustumHeight)
        {
            if (left.m_Polygons.Count != right.m_Polygons.Count)
            {
                Assert.IsTrue(false, "Error in ClipperSolutionLerp - Let us know on the Cinemachine forum please!");
                return left;
            }

            var result = new PolygonSolution
            {
                m_Polygons = new List<List<IntPoint>>(left.m_Polygons.Count),
                m_FrustumHeight = frustumHeight,
            };
            
            float lerpValue = Mathf.InverseLerp(left.m_FrustumHeight, right.m_FrustumHeight, frustumHeight);
            for (int i = 0; i < left.m_Polygons.Count; ++i)
            {
                result.m_Polygons.Add(new List<IntPoint>(left.m_Polygons[i].Count));
                for (int j = 0; j < left.m_Polygons[i].Count; ++j)
                {
                    result.m_Polygons[i].Add(new IntPoint(
                        Mathf.Lerp(left.m_Polygons[i][j].X, right.m_Polygons[i][j].X, lerpValue), 
                        Mathf.Lerp(left.m_Polygons[i][j].Y, right.m_Polygons[i][j].Y, lerpValue)));
                }
            }
            return result;
        }

        List<List<IntPoint>> ComputeSkeleton(in List<PolygonSolution> solutions)
        {
            var skeleton = new List<List<IntPoint>>();

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
                skeleton.AddRange(solution);
            }
            return skeleton;
        }
    }
}