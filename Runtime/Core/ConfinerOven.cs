using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

// #define CINEMACHINE_EXPERIMENTAL_CONFINER2D

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
        }
        private List<ConfinerState> m_confinerStates = new List<ConfinerState>();

        internal const float k_MinStepSize = 0.005f; // internal, because Tests access it
        private List<List<ShrinkablePolygon>> m_shrinkablePolygons;

        public float SqrPolygonDiagonal { get; private set; }

        public float MaxFrustumHeight
        {
            get
            {
                int last = (m_shrinkablePolygons == null ? 0 : m_shrinkablePolygons.Count) - 1;
                return last < 0 ? 0 : m_shrinkablePolygons[last][0].m_FrustumHeight;
            }
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
#if CINEMACHINE_EXPERIMENTAL_CONFINER2D
            bool shrinkToPoint = true;
            bool stopAtFirstIntersection = false;
#else
            bool shrinkToPoint = false;
            bool stopAtFirstIntersection = true;
#endif

            // Compute the aspect-adjusted height of the polygon bounding box
            var polygonSize = PolygonSize(inputPath);
            float polygonHeight = polygonSize.y / aspectRatio; // GML todo: why are we adjusting it for aspect?

            // Cache the polygon diagonal 
            SqrPolygonDiagonal = polygonSize.x * polygonSize.x + polygonHeight * polygonHeight;

            // Ensuring that we don't compute further than what is the theoretical max
            float polygonHalfHeight = polygonHeight * 0.5f;
            if (maxFrustumHeight == 0 || maxFrustumHeight > polygonHalfHeight) // exact comparison to 0 is intentional!
            {
                maxFrustumHeight = polygonHalfHeight; 
            }

            // GML todo: scale the poly in X to make 1:1 aspect and get rid of this
            var aspectData = new ShrinkablePolygon.AspectData(aspectRatio);
            
            // Initial polygon
            m_shrinkablePolygons = CreateShrinkablePolygons(inputPath);
            for (int i = 0; i < m_shrinkablePolygons[0].Count; ++i)
                m_shrinkablePolygons[0][i].ComputeAspectBasedShrinkDirections(aspectData);

            // Binary search for next non-lerpable state
            List<ShrinkablePolygon> rightCandidate = null;
            List<ShrinkablePolygon> leftCandidate = m_shrinkablePolygons[0];
            float maxStepSize = polygonHalfHeight / 4f;
            float stepSize = maxStepSize;
            bool shrinking = true;
            while (shrinking)
            {
#if false
                Debug.Log($"States = {m_shrinkablePolygons.Count}, "
                    + $"Frustum height = {leftCandidate[0].m_FrustumHeight}, stepSize = {stepSize}");
#endif
                if (m_shrinkablePolygons.Count > 1000)
                {
                    Debug.Log("Error: exited with iteration count limit: " + m_shrinkablePolygons.Count);
                    break;
                }

                bool stateChangeFound = false;
                var numPaths = leftCandidate.Count;
                var candidate = new List<ShrinkablePolygon>(numPaths);
                for (int pathIndex = 0; pathIndex < numPaths; ++pathIndex)
                {
                    ShrinkablePolygon poly = leftCandidate[pathIndex].DeepCopy();

                    stepSize = Mathf.Min(stepSize, maxFrustumHeight - poly.m_FrustumHeight);
                    if (poly.Shrink(stepSize, shrinkToPoint, aspectRatio))
                    {
                        // don't simplify at small frustumHeight, because some points at 
                        // start may be close together that are important
                        if (poly.m_FrustumHeight > 0.1f) 
                        {
                            // |= because we want to keep it true if it is true
                            stateChangeFound |= poly.Simplify(k_MinStepSize); 
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
                    if (stateChangeFound)
                        poly.ComputeAspectBasedShrinkDirections(aspectData);
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
                    m_shrinkablePolygons.Add(leftCandidate);

                    // Split the state-changed poly where the path self-intersects
                    var splitPoly = new List<ShrinkablePolygon>();
                    for (int i = 0; i < rightCandidate.Count; ++i)
                    {
                        ShrinkablePolygon.DivideAlongIntersections(rightCandidate[i], ref s_subPolygonCache, aspectData);
                        splitPoly.AddRange(s_subPolygonCache);
                    }

                    // Reduced-functionality version does not shrink beyond first self-intersection
                    if (stopAtFirstIntersection && splitPoly.Count > rightCandidate.Count)
                        break;

                    m_shrinkablePolygons.Add(splitPoly);
                    leftCandidate = splitPoly;
                    rightCandidate = null;
                    
                    // Back to max step.  GML todo: this can be smaller now
                    stepSize = maxStepSize;
                }
                else if (rightCandidate == null && leftCandidate[0].m_FrustumHeight >= maxFrustumHeight)
                {
                    m_shrinkablePolygons.Add(leftCandidate);
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
        }

        private static List<ShrinkablePolygon> s_subPolygonCache = new List<ShrinkablePolygon>();
        
        private static Vector2 PolygonSize(in List<List<Vector2>> polygons)
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
            return new Vector2(Mathf.Max(0, maxX - minX), Mathf.Max(0, maxY - minY));
        }
        
        /// <summary>
        /// Converts and returns shrinkable polygons from a polygons.
        /// </summary>
        public static List<List<ShrinkablePolygon>> CreateShrinkablePolygons(in List<List<Vector2>> paths)
        {
            int numPaths = paths == null ? 0 : paths.Count;
            var shrinkablePolygons = new List<List<ShrinkablePolygon>>(numPaths);
            for (int i = 0; i < numPaths; ++i)
            {
                shrinkablePolygons.Add(new List<ShrinkablePolygon> { new ShrinkablePolygon(paths[i]) });
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
        
        /// <summary>
        /// Converts and returns m_shrinkablePolygons into List<ConfinerState>
        /// </summary>
        public List<ConfinerState> GetShrinkablePolygonsAsConfinerStates()
        {
            m_confinerStates.Clear();
            if (m_confinerStates.Capacity < m_shrinkablePolygons.Count)
                m_confinerStates.Capacity = m_shrinkablePolygons.Count;
            for (int i = 0; i < m_shrinkablePolygons.Count; ++i)
            {
                float stateSum = 0;
                for (int j = 0; j < m_shrinkablePolygons[i].Count; ++j)
                {
                    stateSum += m_shrinkablePolygons[i][j].m_State;
                }
                
                m_confinerStates.Add(new ConfinerState
                {
                    m_FrustumHeight = m_shrinkablePolygons[i][0].m_FrustumHeight,
                    m_Polygons = m_shrinkablePolygons[i],
                    m_State = stateSum,
                });
            }

            return m_confinerStates;
        }
    }
}