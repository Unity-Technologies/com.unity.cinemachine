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
            public float m_WindowSize;
            public float m_State;
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
                        if (shrinkablePolygon.m_WindowDiagonal > shrinkAmount * 100f)
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
                if (maxOrthosize != 0 && maxOrthosize < m_shrinkablePolygons[polyIndex][0].m_WindowDiagonal)
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
        public static List<List<ShrinkablePolygon>> CreateShrinkablePolygons(
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
                for (int j = 0; j < newShrinkablePolygon.m_Points.Count; ++j)
                {
                    var p = newShrinkablePolygon.m_Points[j];
                    minX = Mathf.Min(minX, p.m_Position.x);
                    minY = Mathf.Min(minY, p.m_Position.y);
                    maxX = Mathf.Max(maxX, p.m_Position.x);
                    maxY = Mathf.Max(maxY, p.m_Position.y);
                }

                shrinkablePolygons.Add(new List<ShrinkablePolygon> {newShrinkablePolygon});
            }

            float squareSize = Mathf.Min(maxX - minX, maxY - minY);
            for (int i = 0; i < shrinkablePolygons.Count; ++i)
            {
                shrinkablePolygons[i][0].m_MinArea = squareSize / 100f;
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
                if (m_confinerStates[i].m_WindowSize <= frustumHeight)
                {
                    if (i == m_confinerStates.Count - 1)
                    {
                        result = m_confinerStates[i];
                    }
                    else if (Math.Abs(m_confinerStates[i].m_State - m_confinerStates[i + 1].m_State) < 1e-6f)
                    {
                        // blend between m_confinerStates with same m_State
                        result = ConfinerStateLerp(m_confinerStates[i], m_confinerStates[i+1], frustumHeight);
                    }
                    else
                    {
                        // choose m_confinerStates with windowSize closer to frustumHeight
                        result = 
                            Mathf.Abs(m_confinerStates[i].m_WindowSize - frustumHeight) < 
                            Mathf.Abs(m_confinerStates[i + 1].m_WindowSize - frustumHeight) ? 
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
            if (left.m_Polygons.Count != right.m_Polygons.Count)
            {
                Assert.IsTrue(false, "Error in ConfinerStateLerp - Let us know on the Cinemachine forum please!");
                return left;
            }
            ConfinerState result = new ConfinerState
            {
                m_Polygons = new List<ShrinkablePolygon>(left.m_Polygons.Count),
            };
            
            float lerpValue = Mathf.InverseLerp(left.m_WindowSize, right.m_WindowSize, frustumHeight);
            for (int i = 0; i < left.m_Polygons.Count; ++i)
            {
                var r = new ShrinkablePolygon(
                    left.m_Polygons[i].m_AspectRatio,
                    left.m_Polygons[i].m_AspectRatioBasedDiagonal,
                    left.m_Polygons[i].m_NormalDirections)
                {
                    m_Points = new List<ShrinkablePolygon.ShrinkablePoint2>(left.m_Polygons[i].m_Points.Count),
                    m_WindowDiagonal = frustumHeight,
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
                    stateAverage += m_shrinkablePolygons[i][j].m_State;
                }
                stateAverage /= m_shrinkablePolygons[i].Count + 1;

                float maxWindowDiagonal = m_shrinkablePolygons[i][0].m_WindowDiagonal;
               
                for (int j = 1; j < m_shrinkablePolygons[i].Count; ++j)
                {
                    maxWindowDiagonal = Mathf.Max(m_shrinkablePolygons[i][j].m_WindowDiagonal, maxWindowDiagonal);
                }
                
                m_confinerStates.Add(new ConfinerState
                {
                    m_WindowSize = maxWindowDiagonal,
                    m_Polygons = m_shrinkablePolygons[i],
                    m_State = stateAverage,
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