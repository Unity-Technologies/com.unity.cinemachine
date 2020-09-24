using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Vector2 = UnityEngine.Vector2;

namespace Cinemachine
{
    /// <summary>
    /// Responsible for baking confiners via BakeConfiner function.
    /// </summary>
    public class ConfinerOven
    {
        internal class ConfinerState
        {
            public List<ShrinkablePolygon> graphs;
            public float windowSize;
            public float state;
        }
        
        private List<List<ShrinkablePolygon>> m_shrinkablePolygons;
        
        /// <summary>
        /// Precalculates from input parameters the shrinked down polygons.
        /// </summary>
        /// <param name="inputPath"></param>
        /// <param name="sensorRatio"></param>
        /// <param name="shrinkAmount"></param>
        /// <param name="maxOrthosize"></param>
        /// <param name="shrinkToPoint"></param>
        internal void BakeConfiner(in List<List<Vector2>> inputPath, in float sensorRatio, in float shrinkAmount, 
            in float maxOrthosize, in bool shrinkToPoint)
        {
            m_shrinkablePolygons = CreateShrinkablePolygons(inputPath, sensorRatio);
            var graphsIndex = 0;
            var shrinking = true;
            while (shrinking)
            {
                List<ShrinkablePolygon> nextGraphsIteration = new List<ShrinkablePolygon>();
                for (int g = 0; g < m_shrinkablePolygons[graphsIndex].Count; ++g)
                {
                    m_shrinkablePolygons[graphsIndex][g].ComputeAspectBasedShrinkDirections();
                    ShrinkablePolygon graph = m_shrinkablePolygons[graphsIndex][g].DeepCopy();
                    if (graph.Shrink(shrinkAmount, shrinkToPoint))
                    {
                        if (graph.m_windowDiagonal > shrinkAmount * 100f)
                        {
                            graph.Simplify(shrinkAmount);
                        }
                        
                        ShrinkablePolygon.DivideAlongIntersections(graph, out List<ShrinkablePolygon> subgraphs);
                        nextGraphsIteration.AddRange(subgraphs);
                    }
                    else
                    {
                        nextGraphsIteration.Add(graph);
                    }
                }

                m_shrinkablePolygons.Add(nextGraphsIteration);
                if (maxOrthosize != 0 && maxOrthosize < m_shrinkablePolygons[graphsIndex][0].m_windowDiagonal)
                {
                    break;
                }
                ++graphsIndex;

                shrinking = false;
                for (int i = 0; i < m_shrinkablePolygons[graphsIndex].Count; ++i)
                {
                    ShrinkablePolygon graph = m_shrinkablePolygons[graphsIndex][i];
                    if (graph.IsShrinkable())
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
        private List<List<ShrinkablePolygon>> CreateShrinkablePolygons(in List<List<Vector2>> paths, in float aspectRatio)
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
        internal ConfinerState GetConfinerAtFrustumHeight(float frustumHeight)
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
                        result = ConfinerStateLerp(m_confinerStates[i], m_confinerStates[i+1], Mathf.InverseLerp(
                            m_confinerStates[i].windowSize, m_confinerStates[i + 1].windowSize, frustumHeight));
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
        private ConfinerState ConfinerStateLerp(in ConfinerState left, in ConfinerState right, float lerp)
        {
            if (left.graphs.Count != right.graphs.Count)
            {
                Assert.IsTrue(false, "Error in ConfinerStateLerp - Let us know on the Cinemachine forum please!");
                return left;
            }

            ConfinerState result = new ConfinerState
            {
                graphs = new List<ShrinkablePolygon>(left.graphs.Count),
            };
            for (int i = 0; i < left.graphs.Count; ++i)
            {
                var r = new ShrinkablePolygon
                {
                    m_points = new List<ShrinkablePolygon.ShrinkablePoint2>(left.graphs[i].m_points.Count),
                };
                for (int j = 0; j < left.graphs[i].m_points.Count; ++j)
                {
                    r.m_intersectionPoints = left.graphs[i].m_intersectionPoints;
                    Vector2 rightPoint = right.graphs[i].ClosestGraphPoint(left.graphs[i].m_points[j]);
                    r.m_points.Add(new ShrinkablePolygon.ShrinkablePoint2
                    {
                        m_position = Vector2.Lerp(left.graphs[i].m_points[j].m_position, rightPoint, lerp),
                    });
                }
                result.graphs.Add(r);   
            }
            return result;
        }
        
        private List<ConfinerState> m_confinerStates;
        /// <summary>
        /// Converts and returns m_shrinkablePolygons into List<ConfinerState>
        /// </summary>
        internal List<ConfinerState> GetGraphsAsConfinerStates()
        {
            TrimGraphs();

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
                    graphs = m_shrinkablePolygons[i],
                    state = stateAverage,
                });
            }

            return m_confinerStates;
        }

        /// <summary>
        /// Removes redundant shrinkable polygons from the baked shrinkable polygons. A shrinkable polygon is
        /// redundant, if they are lerpable between two other shrinkable polygons.
        /// </summary>
        private void TrimGraphs()
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