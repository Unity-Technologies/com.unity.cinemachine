using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

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

        private List<List<ShrinkablePolygon>> m_shrinkablePolygons;
        public float m_sqrPolygonDiagonal;
        public float m_cachedMaxFrustumHeight;

        /// <summary>
        /// Creates shrinkable polygons from input parameters.
        /// The algorithm is divide and conquer. It iteratively shrinks down the input polygon towards its shrink
        /// directions. If the polygon intersects with itself, then we divide the polygon into two polygons at the
        /// intersection point, and continue the algorithm on these two polygons separately. We need to keep track of
        /// the connectivity information between sub-polygons.
        /// </summary>
        public void BakeConfiner(in List<List<Vector2>> inputPath, in float aspectRatio, in float bakingResolution,
            float maxFrustumHeightBound, in bool shrinkToPoint, in bool stopAtFirstIntersection)
        {
            float polygonHalfHeight = HeightOfAspectBasedBoundingBoxAroundPolygons(inputPath, aspectRatio) / 2f;
            if (maxFrustumHeightBound == 0 || maxFrustumHeightBound > polygonHalfHeight) // exact comparison to 0 is intentional!
            {
                // ensuring that we don't compute further than what is the theoretical max
                maxFrustumHeightBound = polygonHalfHeight; 
            }

            var aspectData = new ShrinkablePolygon.AspectData(aspectRatio);
            
            m_shrinkablePolygons = CreateShrinkablePolygons(inputPath);
            float maxStepSize = polygonHalfHeight / 4f;
            float minStepSize = 0.005f;
            float stepSize = maxStepSize;
            bool shrinking = true;
            List<ShrinkablePolygon> rightCandidatePolygonIteration = null;
            List<ShrinkablePolygon> leftCandidatePolygonIteration = m_shrinkablePolygons[0];
            while (shrinking)
            {
                bool stateChangeFound = false;
                var numPaths = leftCandidatePolygonIteration.Count;
                var candidatePolygonIteration = new List<ShrinkablePolygon>(numPaths);
                for (int g = 0; g < numPaths; ++g)
                {
                    leftCandidatePolygonIteration[g].ComputeAspectBasedShrinkDirections(aspectData); // TODO: probably only need to recalculate after statechange
                    ShrinkablePolygon shrinkablePolygon = leftCandidatePolygonIteration[g].DeepCopy();
                    shrinkablePolygon.m_Area = shrinkablePolygon.ComputeSignedArea(); // TODO: m_Area may be storing this already
                    stepSize = Mathf.Min(stepSize, maxFrustumHeightBound - shrinkablePolygon.m_FrustumHeight); // ensures we don't go over the max frustum height
                    if (shrinkablePolygon.Shrink(stepSize, shrinkToPoint, aspectRatio))
                    {
                        // don't simplify at small frustumHeight, because some points at start may be close together that are important
                        if (shrinkablePolygon.m_FrustumHeight > 0.1f) {
                            stateChangeFound |= shrinkablePolygon.Simplify(minStepSize); // |= because we want to keep it true if it is true
                        }
                        if (!stateChangeFound)
                        {
                            if (shrinkablePolygon.DoesSelfIntersect() || 
                                shrinkablePolygon.IsInverted(shrinkablePolygon.m_Area))
                            {
                                stateChangeFound = true;
                            }
                        }
                    }
                    
                    candidatePolygonIteration.Add(shrinkablePolygon);
                }

                if (stateChangeFound)
                {
                    rightCandidatePolygonIteration = candidatePolygonIteration;
                    stepSize = Mathf.Max(stepSize / 2f, minStepSize);
                }
                else
                {
                    leftCandidatePolygonIteration = candidatePolygonIteration;
                    if (rightCandidatePolygonIteration != null)
                    {
                        // if we have not found right yet, then we don't need to decrease stepsize
                        stepSize = Mathf.Max(stepSize / 2f, minStepSize);
                    }
                }
                
                // TODO: need to catch case when right is null and left frustum is bigger then max frustum!

                // if we have a right candidate, and left and right are sufficiently close
                if (rightCandidatePolygonIteration != null &&
                    stepSize <= minStepSize)
                {
                    m_shrinkablePolygons.Add(leftCandidatePolygonIteration);
                    
                    var rightCandidatePolygonIterationFixed = new List<ShrinkablePolygon>();
                    for (int rp = 0; rp < rightCandidatePolygonIteration.Count; ++rp)
                    {
                        if (ShrinkablePolygon.DivideAlongIntersections(
                                rightCandidatePolygonIteration[rp], out List<ShrinkablePolygon> subPolygons) && 
                            stopAtFirstIntersection)
                        {
                            return; // stop at first intersection
                        }
                        rightCandidatePolygonIterationFixed.AddRange(subPolygons);
                    }
                    m_shrinkablePolygons.Add(rightCandidatePolygonIterationFixed);

                    leftCandidatePolygonIteration = rightCandidatePolygonIteration;
                    rightCandidatePolygonIteration = null;
                    
                    stepSize = maxStepSize;
                }
                else if (rightCandidatePolygonIteration == null &&
                         leftCandidatePolygonIteration[0].m_FrustumHeight >= maxFrustumHeightBound)
                {
                    m_shrinkablePolygons.Add(leftCandidatePolygonIteration);
                    break; // stop shrinking, because we are at the bound
                }
                else
                {
                    continue; // keep searching for a closer left and right or a non-null right
                }

                // TODO: could move this up into the if part
                shrinking = false;
                for (int i = 0; i < leftCandidatePolygonIteration.Count; ++i)
                {
                    var polygon = leftCandidatePolygonIteration[i];
                    if (polygon.IsShrinkable())
                    {
                        shrinking = true;
                        break;
                    }
                }
            }
        }
     
        /// <summary>
        /// Converts and returns shrinkable polygons from a polygons
        /// </summary>
        public static List<List<ShrinkablePolygon>> CreateShrinkablePolygons(
            in List<List<Vector2>> paths)
        {
            int numPaths = paths == null ? 0 : paths.Count;
            var shrinkablePolygons = new List<List<ShrinkablePolygon>>(numPaths);
            if (numPaths > 0)
            {
                for (int i = 0; i < numPaths; ++i)
                {
                    shrinkablePolygons.Add(new List<ShrinkablePolygon> { new ShrinkablePolygon(paths[i]) });
                }

                for (int i = 0; i < shrinkablePolygons.Count; ++i)
                {
                    for (var j = 0; j < shrinkablePolygons[i].Count; j++)
                    {
                        shrinkablePolygons[i][j].m_Area = shrinkablePolygons[i][0].ComputeSignedArea();
                        shrinkablePolygons[i][j].m_MinArea = 0.1f; // TODO: what's a good value
                    }
                }
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
                        ShrinkablePolygon.s_nonLerpableStateChangePenalty)
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
        
        private List<ConfinerState> m_confinerStates = new List<ConfinerState>();
        /// <summary>
        /// Converts and returns m_shrinkablePolygons into List<ConfinerState>
        /// </summary>
        public List<ConfinerState> GetShrinkablePolygonsAsConfinerStates()
        {
            // TrimShrinkablePolygons();

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

        /// <summary>
        /// Calculates the height (y axis length) of the (unrotated) bounding box with the specified aspect ratio
        /// around the input polygons.
        /// </summary>
        /// <param name="polygons">Input polygons</param>
        /// <param name="aspect">Specifies the aspect ratio of the bounding box.</param>
        /// <returns>The height (y axis length) of the (unrotated) bounding box with the specified aspect ratio
        /// around the input polygon.</returns>
        private float HeightOfAspectBasedBoundingBoxAroundPolygons(in List<List<Vector2>> polygons, in float aspect)
        {
            float minX = Single.PositiveInfinity, maxX = Single.NegativeInfinity;
            float minY = Single.PositiveInfinity, maxY = Single.NegativeInfinity;
            foreach (var path in polygons)
            {
                foreach (var point in path)
                {
                    minX = Mathf.Min(minX, point.x);
                    maxX = Mathf.Max(maxX, point.x);
                    minY = Mathf.Min(minY, point.y);
                    maxY = Mathf.Max(maxY, point.y);
                }
            }

            float pWidth = maxX - minX;
            float pHeight = Mathf.Max(maxY - minY, pWidth / aspect);
            m_sqrPolygonDiagonal = pWidth * pWidth + pHeight * pHeight;
            return pHeight;
        }

        /// <summary>
        /// Removes redundant shrinkable polygons from the baked shrinkable polygons. A shrinkable polygon is
        /// redundant, if it is lerpable polygon between two other shrinkable polygons.
        /// </summary>
        private void TrimShrinkablePolygons()
        {
            int stateStart = m_shrinkablePolygons.Count - 1;
            // going backwards, so we can remove without problems
            for (int i = m_shrinkablePolygons.Count - 2; i >= 0; --i)
            {
                bool stateChanged = m_shrinkablePolygons[stateStart].Count != m_shrinkablePolygons[i].Count;
                if (!stateChanged)
                {
                    for (int j = 0; j < m_shrinkablePolygons[stateStart].Count; ++j)
                    {
                        if (m_shrinkablePolygons[stateStart][j].m_State != m_shrinkablePolygons[i][j].m_State)
                        {
                            stateChanged = true;
                            break;
                        }
                    }
                }

                if (stateChanged || i == 0)
                {
                    int stateEnd = i != 0 ? i + 2 : 1;
                    if (stateEnd < stateStart)
                    {
                        m_shrinkablePolygons.RemoveRange(stateEnd, stateStart - stateEnd);
                    }

                    stateStart = i;
                }
            }
        }
    }
}