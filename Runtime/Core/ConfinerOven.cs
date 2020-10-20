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
            public List<ShrinkablePolygon> polygons;
            public float windowSize;
            public float state;
        }
        
        private List<List<ShrinkablePolygon>> m_shrinkablePolygons;
        
        /// <summary>
        /// Creates shrinkable polygons from input parameters.
        /// The algorithm is divide and conquer. It iteratively shrinks down the input polygon towards its shrink
        /// directions. If the polygon intersects with itself, then we divide the polygon into two polygons at the
        /// intersection point, and continue the algorithm on these two polygons separately. We need to keep track of
        /// the connectivity information between sub-polygons.
        /// </summary>
        public void BakeConfiner(in List<List<Vector2>> inputPath, in float sensorRatio, in float shrinkAmount, 
            in float maxOrthosize, in bool shrinkToPoint)
        {
            m_shrinkablePolygons = CreateShrinkablePolygons(inputPath, sensorRatio);
            var polyIndex = 0;
            var shrinking = true;
            while (shrinking)
            {
                List<ShrinkablePolygon> nextPolygonIteration = new List<ShrinkablePolygon>();
                for (int g = 0; g < m_shrinkablePolygons[polyIndex].Count; ++g)
                {
                    m_shrinkablePolygons[polyIndex][g].ComputeAspectBasedShrinkDirections();
                    ShrinkablePolygon shrinkablePolygon = m_shrinkablePolygons[polyIndex][g].DeepCopy();
                    if (shrinkablePolygon.Shrink(shrinkAmount, shrinkToPoint))
                    {
                        if (shrinkablePolygon.m_windowDiagonal > shrinkAmount * 100f)
                        {
                            shrinkablePolygon.Simplify(shrinkAmount);
                        }
                        
                        ShrinkablePolygon.DivideAlongIntersections(shrinkablePolygon, 
                            out List<ShrinkablePolygon> subPolygons);
                        nextPolygonIteration.AddRange(subPolygons);
                    }
                    else
                    {
                        nextPolygonIteration.Add(shrinkablePolygon);
                    }
                }

                m_shrinkablePolygons.Add(nextPolygonIteration);
                if (maxOrthosize != 0 && maxOrthosize < m_shrinkablePolygons[polyIndex][0].m_windowDiagonal)
                {
                    break;
                }
                ++polyIndex;

                shrinking = false;
                for (int i = 0; i < m_shrinkablePolygons[polyIndex].Count; ++i)
                {
                    ShrinkablePolygon polygon = m_shrinkablePolygons[polyIndex][i];
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
        public List<List<ShrinkablePolygon>> CreateShrinkablePolygons(
            in List<List<Vector2>> paths, in float aspectRatio)
        {
            if (paths == null)
            {
                return new List<List<ShrinkablePolygon>>();
            }

            List<List<ShrinkablePolygon>> shrinkablePolygons = new List<List<ShrinkablePolygon>>();
            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;
            for (int i = 0; i < paths.Count; ++i)
            {
                var newShrinkablePolygon = new ShrinkablePolygon(paths[i], aspectRatio);
                for (int j = 0; j < newShrinkablePolygon.m_points.Count; ++j)
                {
                    var p = newShrinkablePolygon.m_points[j];
                    minX = Mathf.Min(minX, p.m_position.x);
                    minY = Mathf.Min(minY, p.m_position.y);
                    maxX = Mathf.Max(maxX, p.m_position.x);
                    maxY = Mathf.Max(maxY, p.m_position.y);
                }

                shrinkablePolygons.Add(new List<ShrinkablePolygon> {newShrinkablePolygon});
            }

            float squareSize = Mathf.Min(maxX - minX, maxY - minY);
            for (int i = 0; i < shrinkablePolygons.Count; ++i)
            {
                shrinkablePolygons[i][0].m_minArea = squareSize / 100f;
            }

            return shrinkablePolygons;
        }
        
        /// <summary>
        /// Converts and returns a prebaked ConfinerState for the input frustumHeight.
        /// </summary>
        public ConfinerState GetConfinerAtFrustumHeight(float frustumHeight)
        {
            ConfinerState result = new ConfinerState();
            for (int i = m_confinerStates.Count - 1; i >= 0; --i)
            {
                if (m_confinerStates[i].windowSize <= frustumHeight)
                {
                    if (i == m_confinerStates.Count - 1)
                    {
                        result = m_confinerStates[i];
                    }
                    else if (Math.Abs(m_confinerStates[i].state - m_confinerStates[i + 1].state) < 1e-6f)
                    {
                        // blend between m_confinerStates with same m_state
                        result = ConfinerStateLerp(m_confinerStates[i], m_confinerStates[i+1], frustumHeight);
                    }
                    else
                    {
                        // choose m_confinerStates with windowSize closer to frustumHeight
                        result = 
                            Mathf.Abs(m_confinerStates[i].windowSize - frustumHeight) < 
                            Mathf.Abs(m_confinerStates[i + 1].windowSize - frustumHeight) ? 
                                m_confinerStates[i] : 
                                m_confinerStates[i+1];
                    }
                    break;
                }
            }

            return result;
        }
        
        /// <summary>
        /// Linearly interpolates between ConfinerStates.
        /// </summary>
        private ConfinerState ConfinerStateLerp(in ConfinerState left, in ConfinerState right, float frustumHeight)
        {
            if (left.polygons.Count != right.polygons.Count)
            {
                Assert.IsTrue(false, "Error in ConfinerStateLerp - Let us know on the Cinemachine forum please!");
                return left;
            }
            ConfinerState result = new ConfinerState
            {
                polygons = new List<ShrinkablePolygon>(left.polygons.Count),
            };
            
            float lerpValue = Mathf.InverseLerp(left.windowSize, right.windowSize, frustumHeight);
            for (int i = 0; i < left.polygons.Count; ++i)
            {
                var r = new ShrinkablePolygon(
                    left.polygons[i].m_aspectRatio,
                    left.polygons[i].m_aspectRatioBasedDiagonal,
                    left.polygons[i].m_normalDirections)
                {
                    m_points = new List<ShrinkablePolygon.ShrinkablePoint2>(left.polygons[i].m_points.Count),
                    m_windowDiagonal = frustumHeight,
                };
                for (int j = 0; j < left.polygons[i].m_points.Count; ++j)
                {
                    r.m_intersectionPoints = left.polygons[i].m_intersectionPoints;
                    Vector2 rightPoint = right.polygons[i].ClosestPolygonPoint(left.polygons[i].m_points[j]);
                    r.m_points.Add(new ShrinkablePolygon.ShrinkablePoint2
                    {
                        m_position = Vector2.Lerp(left.polygons[i].m_points[j].m_position, rightPoint, lerpValue),
                        m_originalPosition = left.polygons[i].m_points[j].m_originalPosition,
                    });
                }
                result.polygons.Add(r);   
            }
            return result;
        }
        
        private List<ConfinerState> m_confinerStates;
        /// <summary>
        /// Converts and returns m_shrinkablePolygons into List<ConfinerState>
        /// </summary>
        public List<ConfinerState> GetShrinkablePolygonsAsConfinerStates()
        {
            TrimShrinkablePolygons();

            m_confinerStates = new List<ConfinerState>();
            for (int i = 0; i < m_shrinkablePolygons.Count; ++i)
            {
                float stateAverage = m_shrinkablePolygons[i].Count;
                for (int j = 0; j < m_shrinkablePolygons[i].Count; ++j)
                {
                    stateAverage += m_shrinkablePolygons[i][j].m_state;
                }
                stateAverage /= m_shrinkablePolygons[i].Count + 1;

                float maxWindowDiagonal = m_shrinkablePolygons[i][0].m_windowDiagonal;
                for (int j = 1; j < m_shrinkablePolygons[i].Count; ++j)
                {
                    maxWindowDiagonal = Mathf.Max(m_shrinkablePolygons[i][j].m_windowDiagonal, maxWindowDiagonal);
                }
                
                m_confinerStates.Add(new ConfinerState
                {
                    windowSize = maxWindowDiagonal,
                    polygons = m_shrinkablePolygons[i],
                    state = stateAverage,
                });
            }

            return m_confinerStates;
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
                if (m_shrinkablePolygons[stateStart].Count == m_shrinkablePolygons[i].Count)
                {
                    for (int j = 0; j < m_shrinkablePolygons[stateStart].Count; ++j)
                    {
                        if (m_shrinkablePolygons[stateStart][j].m_state != m_shrinkablePolygons[i][j].m_state)
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