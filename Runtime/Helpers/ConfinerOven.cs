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
        private List<List<Graph>> graphs;
        private ConfinerStateToPath _confinerStateToPath;
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="inputPath"></param>
        /// <param name="sensorRatio"></param>
        /// <param name="shrinkAmount"></param>
        internal void BakeConfiner(in List<List<Vector2>> inputPath, in float sensorRatio, in float shrinkAmount)
        {
            graphs = CreateGraphs(inputPath, sensorRatio);
            int graphs_index = 0;

            bool shrinking = true;
            while (shrinking)
            {
                List<Graph> nextGraphsIteration = new List<Graph>();
                for (var g = 0; g < graphs[graphs_index].Count; ++g)
                {
                    graphs[graphs_index][g].ComputeRectangulizedNormals();
                    var graph = graphs[graphs_index][g].DeepCopy();
                    if (graph.Shrink(shrinkAmount))
                    {
                        if (graph.windowDiagonal > 0.1f) graph.Simplify();
                        /// 2. DO until Graph G has intersections
                        /// 2.a.: Found 1 intersection, divide G into g1, g2. Then, G=g2, continue from 2.
                        /// Result of 2 is G in subgraphs without intersections: g1, g2, ..., gn.
                        Graph.DivideAlongIntersections(graph, out List<Graph> subgraphs);
                        nextGraphsIteration.AddRange(subgraphs);
                    }
                    else
                    {
                        nextGraphsIteration.Add(graph);
                    }
                }

                graphs.Add(nextGraphsIteration);
                ++graphs_index;

                shrinking = false;
                for (var index = 0; index < graphs[graphs_index].Count; index++)
                {
                    var graph = graphs[graphs_index][index];
                    if (graph.IsShrinkable())
                    {
                        shrinking = true;
                        break;
                    }
                }
            }
        }
     
        /// <summary>
        /// Converts and returns List<List<Graph>> from a List<List<Vector2>
        /// </summary>
        private List<List<Graph>> CreateGraphs(in List<List<Vector2>> paths, in float sensorRatio)
        {
            if (paths == null)
            {
                return new List<List<Graph>>();
            }

            List<List<Point2>> pathPoints = new List<List<Point2>>();
            foreach (var path in paths)
            {
                var points = new List<Point2>();
                foreach (var point in path)
                {
                    points.Add(new Point2
                    {
                        position = point,
                    });
                }
                pathPoints.Add(Graph.RotateListToLeftmost(points));
            }

            List<List<Graph>> newGraphs = new List<List<Graph>>();
            foreach (var points in pathPoints)
            {
                Graph newGraph = new Graph { points = points };
                newGraph.sensorRatio = sensorRatio;
                newGraph.ComputeNormals(true);
                newGraph.ComputeSignedArea();
                if (!newGraph.ClockwiseOrientation)
                {
                    newGraph.FlipNormals();
                    newGraph.ComputeSignedArea();
                }
                newGraphs.Add(new List<Graph> { newGraph });
            }

            return newGraphs;
        }
        
        /// <summary>
        /// Converts and returns a prebaked ConfinerState for the input orthographicSize.
        /// </summary>
        internal ConfinerState GetConfinerAtOrthoSize(float orthographicSize)
        {
            // TODO: no need to lerp, at i % 2 == 0 - remove this part
            ConfinerState result = new ConfinerState();
            for (int i = confinerStates.Count - 1; i >= 0; --i)
            {
                if (confinerStates[i].windowSize <= orthographicSize)
                {
                    if (i == confinerStates.Count - 1)
                    {
                        result = confinerStates[i];
                    }
                    else if (Math.Abs(confinerStates[i].state - confinerStates[i + 1].state) < 1e-6f)
                    {
                        // blend between confinerStates with same state
                        result = ConfinerStateLerp(confinerStates[i], confinerStates[i+1], Mathf.InverseLerp(
                            confinerStates[i].windowSize, confinerStates[i + 1].windowSize, orthographicSize));
                    }
                    else
                    {
                        // choose confinerStates with windowSize closer to orthographicSize
                        result = 
                            Mathf.Abs(confinerStates[i].windowSize - orthographicSize) < 
                            Mathf.Abs(confinerStates[i + 1].windowSize - orthographicSize) ? 
                                confinerStates[i] : 
                                confinerStates[i+1];
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
                graphs = new List<Graph>(left.graphs.Count),
            };
            for (int i = 0; i < left.graphs.Count; ++i)
            {
                var r = new Graph
                {
                    points = new List<Point2>(left.graphs[i].points.Count),
                };
                for (int j = 0; j < left.graphs[i].points.Count; ++j)
                {
                    r.intersectionPoints = left.graphs[i].intersectionPoints;
                    var rightPoint = right.graphs[i].ClosestGraphPoint(left.graphs[i].points[j]);
                    r.points.Add(new Point2
                    {
                        position = Vector2.Lerp(left.graphs[i].points[j].position, rightPoint, lerp),
                    });
                }
                result.graphs.Add(r);   
            }
            return result;
        }
        
        private List<ConfinerState> confinerStates;
        /// <summary>
        /// Converts and returns graphs into List<ConfinerState>
        /// </summary>
        internal List<ConfinerState> GetGraphsAsConfinerStates()
        {
            TrimGraphs();

            confinerStates = new List<ConfinerState>();
            for (int i = 0; i < graphs.Count; ++i)
            {
                float stateAverage = graphs[i].Count;
                for (var index = 0; index < graphs[i].Count; index++)
                {
                    stateAverage += graphs[i][index].state;
                }
                stateAverage /= graphs[i].Count + 1;

                var maxWindowDiagonal = graphs[i][0].windowDiagonal;
                for (var index = 1; index < graphs[i].Count; index++)
                {
                    maxWindowDiagonal = Mathf.Max(graphs[i][index].windowDiagonal, maxWindowDiagonal);
                }
                
                confinerStates.Add(new ConfinerState
                {
                    windowSize = maxWindowDiagonal,
                    graphs = graphs[i],
                    state = stateAverage,
                });
            }

            return confinerStates;
        }

        /// <summary>
        /// Removes graphs from the precalculated graphs that are redundant,
        /// because they are lerpable between two other graphs.
        /// </summary>
        private void TrimGraphs()
        {
            int stateStart = graphs.Count - 1;
            // going backwards, so we can remove without problems
            for (int i = graphs.Count - 2; i >= 0; --i)
            {
                bool stateChanged = graphs[stateStart].Count != graphs[i].Count;
                if (graphs[stateStart].Count == graphs[i].Count)
                {
                    for (int j = 0; j < graphs[stateStart].Count; ++j)
                    {
                        if (graphs[stateStart][j].state != graphs[i][j].state)
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
                    // when graphs[i].Count != graphs[j].Count, then we are at state0_max
                    // so remove all between state0_max + 2, to state1_max - 1.
                    var stateEnd = i != 0 ? i + 2 : 1;
                    if (stateEnd < stateStart)
                    {
                        graphs.RemoveRange(stateEnd, stateStart - stateEnd);
                    }

                    stateStart = i;
                }
            }
        }
    }
}