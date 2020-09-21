using System;
using System.Collections.Generic;
using Cinemachine.Utility;
using UnityEngine;
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
        /// 
        /// </summary>
        /// <param name="inputPath"></param>
        /// <param name="sensorRatio"></param>
        /// <param name="shrinkAmount"></param>
        internal void BakeConfiner(in List<List<Vector2>> inputPath, in float sensorRatio, in float shrinkAmount, 
            in float maxOrthosize, in bool shrinkToPoint)
        {
            m_shrinkablePolygons = CreateShrinkablePolygons(inputPath, sensorRatio);
            int graphs_index = 0;

            bool shrinking = true;
            while (shrinking)
            {
                List<ShrinkablePolygon> nextGraphsIteration = new List<ShrinkablePolygon>();
                for (var g = 0; g < m_shrinkablePolygons[graphs_index].Count; ++g)
                {
                    m_shrinkablePolygons[graphs_index][g].ComputeAspectBasedNormals();
                    var graph = m_shrinkablePolygons[graphs_index][g].DeepCopy();
                    if (graph.Shrink(shrinkAmount, shrinkToPoint))
                    {
                        if (graph.m_windowDiagonal > 0.1f) graph.Simplify();
                        /// 2. DO until Graph G has intersections
                        /// 2.a.: Found 1 intersection, divide G into g1, g2. Then, G=g2, continue from 2.
                        /// Result of 2 is G in subgraphs without intersections: g1, g2, ..., gn.
                        ShrinkablePolygon.DivideAlongIntersections(graph, out List<ShrinkablePolygon> subgraphs);
                        nextGraphsIteration.AddRange(subgraphs);
                    }
                    else
                    {
                        nextGraphsIteration.Add(graph);
                    }
                }

                m_shrinkablePolygons.Add(nextGraphsIteration);
                if (maxOrthosize < m_shrinkablePolygons[graphs_index][0].m_windowDiagonal)
                {
                    break;
                }
                ++graphs_index;

                shrinking = false;
                for (var index = 0; index < m_shrinkablePolygons[graphs_index].Count; index++)
                {
                    var graph = m_shrinkablePolygons[graphs_index][index];
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
            bool first = true;
            foreach (var points in paths)
            {
                float squareSize = 0;
                var newShrinkablePolygon = new ShrinkablePolygon(points, aspectRatio);
                if (first)
                {
                    float minX = float.MaxValue, maxX = float.MinValue;
                    float minY = float.MaxValue, maxY = float.MinValue;
                    foreach (var p in newShrinkablePolygon.m_points)
                    {
                        minX = Mathf.Min(minX, p.m_position.x);
                        minY = Mathf.Min(minY, p.m_position.y);
                        maxX = Mathf.Max(maxX, p.m_position.x);
                        maxY = Mathf.Max(maxY, p.m_position.y);
                    }

                    squareSize = Mathf.Min(maxX - minX, maxY - minY);
                    first = false;
                }

                newShrinkablePolygon.m_minArea = squareSize / 100f; 
                shrinkablePolygons.Add(new List<ShrinkablePolygon> { newShrinkablePolygon });
            }

            return shrinkablePolygons;
        }
        
        /// <summary>
        /// Converts and returns a prebaked ConfinerState for the input orthographicSize.
        /// </summary>
        internal ConfinerState GetConfinerAtOrthoSize(float orthographicSize)
        {
            ConfinerState result = new ConfinerState();
            for (int i = m_confinerStates.Count - 1; i >= 0; --i)
            {
                if (m_confinerStates[i].windowSize <= orthographicSize)
                {
                    if (i == m_confinerStates.Count - 1)
                    {
                        result = m_confinerStates[i];
                    }
                    else if (Math.Abs(m_confinerStates[i].state - m_confinerStates[i + 1].state) < 1e-6f)
                    {
                        // blend between m_confinerStates with same m_state
                        result = ConfinerStateLerp(m_confinerStates[i], m_confinerStates[i+1], Mathf.InverseLerp(
                            m_confinerStates[i].windowSize, m_confinerStates[i + 1].windowSize, orthographicSize));
                    }
                    else
                    {
                        // choose m_confinerStates with windowSize closer to orthographicSize
                        result = 
                            Mathf.Abs(m_confinerStates[i].windowSize - orthographicSize) < 
                            Mathf.Abs(m_confinerStates[i + 1].windowSize - orthographicSize) ? 
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
                Debug.Log("Error in ConfinerStateLerp - Let us know on the Cinemachine forum please!"); // should never happen
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
                    var rightPoint = right.graphs[i].ClosestGraphPoint(left.graphs[i].m_points[j]);
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
                for (var index = 0; index < m_shrinkablePolygons[i].Count; index++)
                {
                    stateAverage += m_shrinkablePolygons[i][index].m_state;
                }
                stateAverage /= m_shrinkablePolygons[i].Count + 1;

                var maxWindowDiagonal = m_shrinkablePolygons[i][0].m_windowDiagonal;
                for (var index = 1; index < m_shrinkablePolygons[i].Count; index++)
                {
                    maxWindowDiagonal = Mathf.Max(m_shrinkablePolygons[i][index].m_windowDiagonal, maxWindowDiagonal);
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
        /// Removes m_shrinkablePolygons from the precalculated m_shrinkablePolygons that are redundant,
        /// because they are lerpable between two other m_shrinkablePolygons.
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
                    // state0_min, ..., state0_max, state1_min, ... state1_max
                    // ... parts need to be removed
                    // when m_shrinkablePolygons[i].Count != m_shrinkablePolygons[j].Count, then we are at state0_max
                    // so remove all between state0_max + 2, to state1_max - 1.
                    var stateEnd = i != 0 ? i + 2 : 1;
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