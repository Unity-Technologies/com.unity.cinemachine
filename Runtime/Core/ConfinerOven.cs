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
        private List<PolygonSolution> m_Solutions = new List<PolygonSolution>();

        const int k_FloatToIntScaler = 10000000; // same as in Physics2D
        const float k_MinStepSize = 0.005f; 

        public float SqrPolygonDiagonal { get; private set; }
        public float MaxFrustumHeight { get; private set; }

        public struct PolygonSolution
        {
            public List<List<IntPoint>> m_Solution;
            public float m_FrustumHeight;

            public float m_CenterX; // used for aspect ratio scaling
            public float m_AspectRatio;
            
            public bool StateChanged(in List<List<IntPoint>> paths)
            {
                if (paths.Count != m_Solution.Count)
                    return true;
                for (var i = 0; i < paths.Count; i++)
                {
                    if (paths[i].Count != m_Solution[i].Count)
                        return true;
                }
                return false;
            }

            public List<List<Vector2>> GetConfinerPath()
            {
                var numPaths = m_Solution.Count;
                var paths = new List<List<Vector2>>(numPaths);
                const float IntToFloatScaler = 1.0f / k_FloatToIntScaler;
                for (int i = 0; i < numPaths; ++i)
                {
                    var srcPoly = m_Solution[i];
                    int numPoints = srcPoly.Count;
                    var pathSegment = new List<Vector2>(numPoints);
                    for (int j = 0; j < numPoints; j++)
                    {
                        // Restore the original aspect ratio
                        var x = srcPoly[j].X * IntToFloatScaler;
                        x = (x - m_CenterX) * m_AspectRatio + m_CenterX;
                        pathSegment.Add(new Vector2(x, srcPoly[j].Y * IntToFloatScaler));
                    }
                    paths.Add(pathSegment);
                }
                return paths;
            }

            public bool IsEmpty => m_Solution == null;
        }


        /// <summary>
        /// Creates shrinkable polygons from input parameters.
        /// The algorithm is divide and conquer. It iteratively shrinks down the input 
        /// polygon towards its shrink directions. If the polygon intersects with itself, 
        /// then we divide the polygon into two polygons at the intersection point, and 
        /// continue the algorithm on these two polygons separately. We need to keep track of
        /// the connectivity information between sub-polygons.
        /// </summary>
        public List<PolygonSolution> BakeConfiner(
            in List<List<Vector2>> inputPath, in float aspectRatio, float maxFrustumHeight)
        {
            // Compute the aspect-adjusted height of the polygon bounding box
            var polygonRect = GetPolygonBoundingBox(inputPath);
            float polygonHeight = polygonRect.height / aspectRatio; // GML todo: why are we adjusting it for aspect?

            // Cache the polygon diagonal 
            SqrPolygonDiagonal = polygonRect.width * polygonRect.width + polygonHeight * polygonHeight;

            // Ensuring that we don't compute further than what is the theoretical max
            float polygonHalfHeight = polygonHeight * 0.5f;
            if (maxFrustumHeight == 0 || maxFrustumHeight > polygonHalfHeight) // exact comparison to 0 is intentional!
            {
                maxFrustumHeight = polygonHalfHeight; 
            }

            // Initialize clipper
            List<List<IntPoint>> clipperInput = new List<List<IntPoint>>(inputPath.Count);
            for (var i = 0; i < inputPath.Count; ++i)
            {
                var xScale = 1 / aspectRatio;

                var srcPath = inputPath[i];
                int numPoints = srcPath.Count;
                var path = new List<IntPoint>(numPoints);
                for (int j = 0; j < numPoints; ++j)
                {
                    var x = (srcPath[j].x - polygonRect.center.x) * xScale + polygonRect.center.x;
                    path.Add(new IntPoint(x * k_FloatToIntScaler, srcPath[j].y * k_FloatToIntScaler));
                }
                clipperInput.Add(path);
            }
            
            var offsetter = new ClipperOffset();
            offsetter.AddPaths(clipperInput, JoinType.jtMiter, EndType.etClosedPolygon);

            List<List<IntPoint>> solution = new List<List<IntPoint>>();
            offsetter.Execute(ref solution, 0);
            m_Solutions.Clear();
            m_Solutions.Add(new PolygonSolution
            {
                m_Solution = solution,
                m_FrustumHeight = 0,
                m_CenterX = polygonRect.center.x,
                m_AspectRatio = aspectRatio
            });

            // Binary search for next non-lerpable state
            PolygonSolution rightCandidate = new PolygonSolution();
            PolygonSolution leftCandidate = new PolygonSolution
            {
                m_Solution = solution,
                m_FrustumHeight = 0,
                m_CenterX = polygonRect.center.x,
                m_AspectRatio = aspectRatio
            };
            float currentFrustumHeight = 0;
            
            float maxStepSize = polygonHalfHeight / 4f;
            float stepSize = maxStepSize;
            bool shrinking = true;
            while (shrinking)
            {
#if false
                Debug.Log($"States = {m_Solutions.Count}, "
                    + $"Frustum height = {currentFrustumHeight}, stepSize = {stepSize}");
#endif
                if (m_Solutions.Count > 1000)
                {
                    Debug.LogError("Exited with iteration count limit: " + m_Solutions.Count);
                    break;
                }

                bool stateChangeFound = false;
                var numPaths = leftCandidate.m_Solution.Count;
                var candidate = new List<List<IntPoint>>(numPaths);

                stepSize = Mathf.Min(stepSize, maxFrustumHeight - leftCandidate.m_FrustumHeight);
                currentFrustumHeight = leftCandidate.m_FrustumHeight + stepSize;
                offsetter.Execute(ref candidate, -1f * currentFrustumHeight * k_FloatToIntScaler);
                stateChangeFound = leftCandidate.StateChanged(in candidate);

                if (stateChangeFound)
                {
                    rightCandidate = new PolygonSolution
                    {
                        m_Solution = candidate,
                        m_FrustumHeight = currentFrustumHeight,
                        m_CenterX = polygonRect.center.x,
                        m_AspectRatio = aspectRatio
                    };
                    stepSize = Mathf.Max(stepSize / 2f, k_MinStepSize);
                }
                else
                {
                    leftCandidate = new PolygonSolution
                    {
                        m_Solution = candidate,
                        m_FrustumHeight = currentFrustumHeight,
                        m_CenterX = polygonRect.center.x,
                        m_AspectRatio = aspectRatio
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
                    m_Solutions.Add(leftCandidate);
                    m_Solutions.Add(rightCandidate);

                    leftCandidate = rightCandidate;
                    rightCandidate = new PolygonSolution();
                    
                    // Back to max step.  GML todo: this can be smaller now
                    stepSize = maxStepSize;
                }
                else if (rightCandidate.IsEmpty && leftCandidate.m_FrustumHeight >= maxFrustumHeight)
                {
                    m_Solutions.Add(leftCandidate);
                    break; // stop shrinking, because we are at the bound
                }
                else
                {
                    continue; // keep searching for a closer left and right or a non-null right
                }

                // GML todo: think about this:
                // no need to do this. We'll go until theoritical max
                // // TODO: could move this up into the if part
                // shrinking = false;
                // for (int i = 0; i < leftCandidate.Count; ++i)
                // {
                //     var polygon = leftCandidate[i];
                //     if (polygon.IsShrinkable())
                //     {
                //         shrinking = true;
                //         break;
                //     }
                // }
            }

            // Cache the max confinable view size
            MaxFrustumHeight = m_Solutions.Count == 0 ? 0 : m_Solutions[m_Solutions.Count-1].m_FrustumHeight;

            return m_Solutions;
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
        /// Converts and returns a prebaked ConfinerState for the input frustumHeight.
        /// </summary>
        public PolygonSolution GetConfinerAtFrustumHeight(float frustumHeight)
        {
            for (int i = m_Solutions.Count - 1; i >= 0; --i)
            {
                if (m_Solutions[i].m_FrustumHeight <= frustumHeight)
                {
                    if (i == m_Solutions.Count - 1)
                        return m_Solutions[i];

                    if (i % 2 == 0)
                        return ClipperSolutionLerp(m_Solutions[i], m_Solutions[i+1], frustumHeight);

                    return
                        Mathf.Abs(m_Solutions[i].m_FrustumHeight - frustumHeight) < 
                        Mathf.Abs(m_Solutions[i + 1].m_FrustumHeight - frustumHeight) 
                            ? m_Solutions[i] 
                            : m_Solutions[i + 1];
                }
            }

            return new PolygonSolution();
        }

        /// <summary>
        /// Linearly interpolates between ConfinerStates.
        /// </summary>
        private PolygonSolution ClipperSolutionLerp(
            in PolygonSolution left, in PolygonSolution right, float frustumHeight)
        {
            if (left.m_Solution.Count != right.m_Solution.Count)
            {
                Assert.IsTrue(false, "Error in ClipperSolutionLerp - Let us know on the Cinemachine forum please!");
                return left;
            }

            var result = new PolygonSolution
            {
                m_Solution = new List<List<IntPoint>>(left.m_Solution.Count),
                m_FrustumHeight = frustumHeight,
                m_CenterX = left.m_CenterX,
                m_AspectRatio = left.m_AspectRatio
            };
            
            float lerpValue = Mathf.InverseLerp(left.m_FrustumHeight, right.m_FrustumHeight, frustumHeight);
            for (int i = 0; i < left.m_Solution.Count; ++i)
            {
                result.m_Solution.Add(new List<IntPoint>(left.m_Solution[i].Count));
                for (int j = 0; j < left.m_Solution[i].Count; ++j)
                {
                    result.m_Solution[i].Add(new IntPoint(
                        Mathf.Lerp(left.m_Solution[i][j].X, right.m_Solution[i][j].X, lerpValue), 
                        Mathf.Lerp(left.m_Solution[i][j].Y, right.m_Solution[i][j].Y, lerpValue)));
                }
            }
            return result;
        }
    }
}