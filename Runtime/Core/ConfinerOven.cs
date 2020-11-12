#define CINEMACHINE_EXPERIMENTAL_CONFINER2D
#define ASPECT_RATIO_EXPERIMENT

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using ClipperLib;
using Cinemachine.Utility;

namespace Cinemachine
{
    /// <summary>
    /// Responsible for baking confiners via BakeConfiner function.
    /// </summary>
    internal class ConfinerOven
    {
        public class ConfinerState
        {
            public List<ShrinkablePolygon> m_Polygons;
            public float m_FrustumHeight;
            public float m_State;

            public float m_AspectRatio;
            public float m_CenterX; // used for aspect ratio scaling

            private const int FloatToIntScaler = 10000000; // same as in Physics2D
            private const float Epsilon = UnityVectorExtensions.Epsilon;

            /// <summary>
            /// Converts shrinkable polygons into a simple path made of 2D points.
            /// </summary>
            /// <param name="maxCachedFrustumHeight">Maximum cached frustum height.</param>
            /// <param name="path">Resulting path.</param>
            /// <param name="hasIntersections">True, if the polygon has intersections. False, otherwise.</param>
            public void ConvertToPath(
                in float maxCachedFrustumHeight,
                out List<List<Vector2>> path, out bool hasIntersections)
            {
                hasIntersections = maxCachedFrustumHeight < m_FrustumHeight;
                // convert shrinkable polygons points to int based points for Clipper
                List<List<IntPoint>> clip = new List<List<IntPoint>>(m_Polygons.Count);
                int index = 0;
                for (var polyIndex = 0; polyIndex < m_Polygons.Count; polyIndex++)
                {
                    var polygon = m_Polygons[polyIndex];
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
                        AddConnectingSegment(clip, intersectionPoint, polygon.ClosestPolygonPoint(intersectionPoint));
                        ++index;
                    }
                
                    // Add twigs to ensure that all original points can be seen
                    foreach (var point in polygon.m_Points)
                    {
                        if (!point.m_OriginalPosition.IsNaN())
                        {
#if ASPECT_RATIO_EXPERIMENT
                            Vector2 direction = point.m_Position - point.m_OriginalPosition;
                            if (Mathf.Abs(direction.x) > m_FrustumHeight || Mathf.Abs(direction.y) > m_FrustumHeight)
                            {
                                direction *= Mathf.Sign(direction.x) / direction.x;
                                if (Mathf.Abs(direction.y) > 1)
                                {
                                    direction *= Mathf.Sign(direction.y) / direction.y;
                                }
                                direction *= m_FrustumHeight;
                                Vector2 cornerTouchingPoint = point.m_OriginalPosition + direction;
                                AddConnectingSegment(clip, point.m_Position, cornerTouchingPoint);
                                ++index;
                            }
#else
                            Vector2 direction = point.m_Position - point.m_OriginalPosition;
                            float sqrCornerDistance = direction.sqrMagnitude;
                            if (direction.x > 0)
                            {
                                direction *= (m_AspectRatio / direction.x);
                            }
                            else if (direction.x < 0)
                            {
                                direction *= -(m_AspectRatio / direction.x);
                            }
                            if (direction.y > 1)
                            {
                                direction *= (1f / direction.y);
                            }
                            else if (direction.y < -1)
                            {
                                direction *= -(1f / direction.y);
                            }

                            direction *= m_FrustumHeight;
                            if (direction.sqrMagnitude >= sqrCornerDistance)
                            {
                                continue; // camera is already touching this point
                            }
                            Vector2 cornerTouchingPoint = point.m_OriginalPosition + direction;
                            if (Vector2.Distance(cornerTouchingPoint, point.m_Position) < Epsilon)
                            {
                                continue;
                            }
                            AddConnectingSegment(clip, point.m_Position, cornerTouchingPoint);
     
                            index++;
#endif
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
#if ASPECT_RATIO_EXPERIMENT
                        // Restore the original aspect ratio
                        p.x = ((p.x - m_CenterX) * m_AspectRatio) + m_CenterX;
#endif
                        pathSegment.Add(p);
                    }

                    path.Add(pathSegment);
                }
            }

            private void AddConnectingSegment(List<List<IntPoint>> clip, Vector2 p1, Vector2 p2)
            {
                Vector2 direction = (p2 - p1).normalized * Epsilon;
                Vector2 normal = new Vector2(direction.y, -direction.x);

                Vector2 pA = p2 + normal + direction;
                Vector2 pB = p1 + normal - direction;
                Vector3 pC = p1 - normal - direction;
                Vector3 pD = p2 - normal + direction;

                var path = new List<IntPoint>(4);
                path.Add(new IntPoint(pA.x * FloatToIntScaler, pA.y * FloatToIntScaler));
                path.Add(new IntPoint(pB.x * FloatToIntScaler, pB.y * FloatToIntScaler));
                path.Add(new IntPoint(pC.x * FloatToIntScaler, pC.y * FloatToIntScaler));
                path.Add(new IntPoint(pD.x * FloatToIntScaler, pD.y * FloatToIntScaler));
                clip.Add(path);
            }
        }

        private List<ConfinerState> m_confinerStates = new List<ConfinerState>();

        internal const float k_MinStepSize = 0.005f; // internal, because Tests access it

        public float SqrPolygonDiagonal { get; private set; }
        public float MaxFrustumHeight { get; private set; }

        /// <summary>
        /// Creates shrinkable polygons from input parameters.
        /// The algorithm is divide and conquer. It iteratively shrinks down the input 
        /// polygon towards its shrink directions. If the polygon intersects with itself, 
        /// then we divide the polygon into two polygons at the intersection point, and 
        /// continue the algorithm on these two polygons separately. We need to keep track of
        /// the connectivity information between sub-polygons.
        /// </summary>
        public List<ConfinerState> BakeConfiner(
            in List<List<Vector2>> inputPath, in float aspectRatio, float maxFrustumHeight)
        {
#if CINEMACHINE_EXPERIMENTAL_CONFINER2D
            bool shrinkToPoint = true;
            bool stopAtFirstIntersection = false;
#else
            bool shrinkToPoint = false;
            bool stopAtFirstIntersection = true;
#endif

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

            // GML todo: get rid of ShrinkablePolygon.AspectData
#if ASPECT_RATIO_EXPERIMENT
            var aspectData = new ShrinkablePolygon.AspectData(1);

            // Scale the polygon's X values to neutralize aspect ratio
            {
                var c = polygonRect.center.x;
                var s = 1 / aspectRatio;
                for (int i = 0; i < inputPath.Count; ++i)
                {
                    var path = inputPath[i];
                    for (int j = 0; j < path.Count; ++j)
                    {
                        var p = path[j];
                        path[j] = new Vector2((p.x - c) * s + c, p.y);
                    }
                }
            }
#else
            var aspectData = new ShrinkablePolygon.AspectData(aspectRatio);
#endif

            // Initial polygon
            List<List<ShrinkablePolygon>> shrinkablePolygons = new List<List<ShrinkablePolygon>>(100);
            shrinkablePolygons.Add(CreateShrinkablePolygons(inputPath));

#if ASPECT_RATIO_EXPERIMENT
            // Restore the aspect ratio
            {
                var c = polygonRect.center.x;
                var s = aspectRatio;
                for (int i = 0; i < inputPath.Count; ++i)
                {
                    var path = inputPath[i];
                    for (int j = 0; j < path.Count; ++j)
                    {
                        var p = path[j];
                        path[j] = new Vector2((p.x - c) * s + c, p.y);
                    }
                }
            }
#endif

            for (int i = 0; i < shrinkablePolygons[0].Count; ++i)
                shrinkablePolygons[0][i].ComputeAspectBasedShrinkDirections(aspectData);

            // Binary search for next non-lerpable state
            List<ShrinkablePolygon> rightCandidate = null;
            List<ShrinkablePolygon> leftCandidate = shrinkablePolygons[0];
            float maxStepSize = polygonHalfHeight / 4f;
            float stepSize = maxStepSize;
            bool shrinking = true;
            while (shrinking)
            {
#if true
                Debug.Log($"States = {shrinkablePolygons.Count}, "
                    + $"Frustum height = {leftCandidate[0].m_FrustumHeight}, stepSize = {stepSize}");
#endif
                if (shrinkablePolygons.Count > 1000)
                {
                    Debug.LogError("Exited with iteration count limit: " + shrinkablePolygons.Count);
                    break;
                }

                bool stateChangeFound = false;
                var numPaths = leftCandidate.Count;
                var candidate = new List<ShrinkablePolygon>(numPaths);
                for (int pathIndex = 0; pathIndex < numPaths; ++pathIndex)
                {
                    ShrinkablePolygon poly = leftCandidate[pathIndex].DeepCopy();

                    stepSize = Mathf.Min(stepSize, maxFrustumHeight - poly.m_FrustumHeight);
                    if (poly.Shrink(stepSize, shrinkToPoint, aspectData.m_AspectRatio))
                    {
                        // don't simplify at small frustumHeight, because some points at 
                        // start may be close together that are important
                        if (poly.m_FrustumHeight > 0.1f) 
                        {
                            if (poly.Simplify(k_MinStepSize))
                            {
                                poly.ComputeAspectBasedShrinkDirections(aspectData);
                                stateChangeFound = true;
                            }
                        }
                        if (!stateChangeFound)
                        {
                            if (poly.DoesSelfIntersect() || 
                                Mathf.Sign(poly.m_Area) != Mathf.Sign(leftCandidate[pathIndex].m_Area))
                            {
                                stateChangeFound = true;
                            }
                        }
                    }
                        
                    candidate.Add(poly);
                }

                if (stateChangeFound)
                {
                    rightCandidate = candidate;
                    stepSize = Mathf.Max(stepSize / 2f, k_MinStepSize);
                }
                else
                {
                    leftCandidate = candidate;
                    if (rightCandidate != null)
                    {
                        // if we have not found right yet, then we don't need to decrease stepsize
                        stepSize = Mathf.Max(stepSize / 2f, k_MinStepSize);
                    }
                }
                
                // if we have a right candidate, and left and right are sufficiently close, 
                // then we have located a state change point
                if (rightCandidate != null && stepSize <= k_MinStepSize)
                {
                    // Add both states: one before the state change and one after
                    shrinkablePolygons.Add(leftCandidate);

                    // Split the state-changed poly where the path self-intersects
                    var splitPoly = new List<ShrinkablePolygon>();
                    for (int i = 0; i < rightCandidate.Count; ++i)
                    {
                        ShrinkablePolygon.DivideAlongIntersections(rightCandidate[i], ref s_subPolygonCache, aspectData);
                        for (int subpoly = 0; subpoly < s_subPolygonCache.Count; ++subpoly)
                        {
                            s_subPolygonCache[subpoly].ComputeAspectBasedShrinkDirections(aspectData);
                        }
                        splitPoly.AddRange(s_subPolygonCache);
                    }

                    // Reduced-functionality version does not shrink beyond first self-intersection
                    if (stopAtFirstIntersection && splitPoly.Count > rightCandidate.Count)
                        break;

                    shrinkablePolygons.Add(splitPoly);
                    leftCandidate = splitPoly;
                    rightCandidate = null;
                    
                    // Back to max step.  GML todo: this can be smaller now
                    stepSize = maxStepSize;
                }
                else if (rightCandidate == null && leftCandidate[0].m_FrustumHeight >= maxFrustumHeight)
                {
                    shrinkablePolygons.Add(leftCandidate);
                    break; // stop shrinking, because we are at the bound
                }
                else
                {
                    continue; // keep searching for a closer left and right or a non-null right
                }

                // TODO: could move this up into the if part
                shrinking = false;
                for (int i = 0; i < leftCandidate.Count; ++i)
                {
                    var polygon = leftCandidate[i];
                    if (polygon.IsShrinkable())
                    {
                        shrinking = true;
                        break;
                    }
                }
            }

            // Cache the max confinable view size
            MaxFrustumHeight = shrinkablePolygons.Count == 0 
                ? 0 : shrinkablePolygons[shrinkablePolygons.Count-1][0].m_FrustumHeight;

            // Generate the confiner states
            m_confinerStates.Clear();
            if (m_confinerStates.Capacity < shrinkablePolygons.Count)
                m_confinerStates.Capacity = shrinkablePolygons.Count;
            for (int i = 0; i < shrinkablePolygons.Count; ++i)
            {
                float stateSum = 0;
                for (int j = 0; j < shrinkablePolygons[i].Count; ++j)
                {
                    stateSum += shrinkablePolygons[i][j].m_State;
                }
                
                m_confinerStates.Add(new ConfinerState
                {
                    m_FrustumHeight = shrinkablePolygons[i][0].m_FrustumHeight,
                    m_Polygons = shrinkablePolygons[i],
                    m_State = stateSum,
                    m_AspectRatio = aspectRatio,
                    m_CenterX = polygonRect.center.x
                });
            }

            return m_confinerStates;
        }

        private static List<ShrinkablePolygon> s_subPolygonCache = new List<ShrinkablePolygon>();
        
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
        /// Creates shrinkable polygons from a list of polygons
        /// </summary>
        public static List<ShrinkablePolygon> CreateShrinkablePolygons(in List<List<Vector2>> paths)
        {
            int numPaths = paths == null ? 0 : paths.Count;
            var shrinkablePolygons = new List<ShrinkablePolygon>(numPaths);
            for (int i = 0; i < numPaths; ++i)
            {
                shrinkablePolygons.Add(new ShrinkablePolygon(paths[i]));
            }
            return shrinkablePolygons;
        }

        /// <summary>
        /// Converts and returns a prebaked ConfinerState for the input frustumHeight.
        /// </summary>
        public ConfinerState GetConfinerAtFrustumHeight(float frustumHeight)
        {
            for (int i = m_confinerStates.Count - 1; i >= 0; --i)
            {
                if (m_confinerStates[i].m_FrustumHeight <= frustumHeight)
                {
                    if (i == m_confinerStates.Count - 1)
                    {
                        return m_confinerStates[i];
                    }
                    
                    if (Math.Abs(m_confinerStates[i].m_State - m_confinerStates[i + 1].m_State) < 
                        ShrinkablePolygon.k_NonLerpableStateChangePenalty)
                    {
                        // blend between m_confinerStates with same m_State
                        return ConfinerStateLerp(m_confinerStates[i], m_confinerStates[i+1], frustumHeight);
                    }
                    
                    // choose m_confinerStates with windowSize closer to frustumHeight
                    return
                        Mathf.Abs(m_confinerStates[i].m_FrustumHeight - frustumHeight) < 
                        Mathf.Abs(m_confinerStates[i + 1].m_FrustumHeight - frustumHeight) ? 
                            m_confinerStates[i] : 
                            m_confinerStates[i+1];
                }
            }

            return new ConfinerState();
        }

        /// <summary>
        /// Linearly interpolates between ConfinerStates.
        /// </summary>
        private ConfinerState ConfinerStateLerp(in ConfinerState left, in ConfinerState right, float frustumHeight)
        {
            if (left.m_Polygons.Count != right.m_Polygons.Count)
            {
                Assert.IsTrue(false, "Error in ConfinerStateLerp - Let us know on the Cinemachine forum please!");
                return left;
            }
            ConfinerState result = new ConfinerState
            {
                m_Polygons = new List<ShrinkablePolygon>(left.m_Polygons.Count),
                m_FrustumHeight = frustumHeight,
                m_State = left.m_State,
                m_AspectRatio = left.m_AspectRatio,
                m_CenterX = left.m_CenterX
            };
            
            float lerpValue = Mathf.InverseLerp(left.m_FrustumHeight, right.m_FrustumHeight, frustumHeight);
            for (int i = 0; i < left.m_Polygons.Count; ++i)
            {
                var r = new ShrinkablePolygon
                {
                    m_Points = new List<ShrinkablePolygon.ShrinkablePoint2>(left.m_Polygons[i].m_Points.Count),
                    m_FrustumHeight = frustumHeight,
                };
                for (int j = 0; j < left.m_Polygons[i].m_Points.Count; ++j)
                {
                    r.m_IntersectionPoints = left.m_Polygons[i].m_IntersectionPoints;
                    Vector2 rightPoint = right.m_Polygons[i].ClosestPolygonPoint(left.m_Polygons[i].m_Points[j]);
                    r.m_Points.Add(new ShrinkablePolygon.ShrinkablePoint2
                    {
                        m_Position = Vector2.Lerp(left.m_Polygons[i].m_Points[j].m_Position, rightPoint, lerpValue),
                        m_OriginalPosition = left.m_Polygons[i].m_Points[j].m_OriginalPosition,
                    });
                }
                result.m_Polygons.Add(r);   
            }
            return result;
        }
    }
}