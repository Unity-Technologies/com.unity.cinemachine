using System;
using System.Collections.Generic;
using Cinemachine.Utility;
using UnityEditor;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;

namespace Cinemachine
{
    public class ConfinerOven
    {
        /// <summary>Inputs represent areas within the virtual camera can operate the camera.
        /// Distance from the border depends the camera view window size.</summary>

        private List<List<Graph>> graphs;

        public bool ConvertToCompositeCollider;
        private ConfinerStateToPath _confinerStateToPath;
        
        public CinemachineVirtualCamera VcamToBakeFor;

        private List<Graph> GIZMOS_currentGraphs;
        private List<Graph> GIZMOS_subGraphs;
        private List<Vector2> GIZMOS_input;
        public bool GIZMOS_drawIntersection;
        
        private bool IsCacheValid(in List<List<Vector2>> inputPath, in float sensorRatio)
        {
            if (Math.Abs(sensorRatio - sensorRatioCache) > UnityVectorExtensions.Epsilon)
            {
                inputPathCache = inputPath;
                sensorRatioCache = sensorRatio;
                return false;
            }
            
            if (inputPathCache == null)
            {
                inputPathCache = inputPath;
                sensorRatioCache = sensorRatio;
                return false;
            }
            if (inputPathCache.Count == inputPath.Count)
            {
                for (int i = 0; i < inputPath.Count; ++i)
                {
                    if (inputPath[i].Count == inputPathCache[i].Count)
                    {
                        for (int j = 0; j < inputPath[i].Count; ++j)
                        {
                            if (inputPath[i][j] != inputPathCache[i][j])
                            {
                                inputPathCache = inputPath;
                                sensorRatioCache = sensorRatio;
                                return false;
                            }
                        }
                    }
                    else
                    {
                        inputPathCache = inputPath;
                        sensorRatioCache = sensorRatio;
                        return false;
                    }
                }
            }
            else
            {
                inputPathCache = inputPath;
                sensorRatioCache = sensorRatio;
                return false;
            }

            return true;
        }

        private List<List<Vector2>> inputPathCache = null;
        private float sensorRatioCache = 0;
        internal bool BakeConfiner(in List<List<Vector2>> inputPath, in float sensorRatio, in float shrinkAmount)
        {
            if (IsCacheValid(inputPath, sensorRatio))
            {
                //return false;
            }
            
            graphs = CreateGraphs(inputPath, sensorRatio);
            int graphs_index = 0;

            int counter = 0;
            bool shrinking = true;
            while (shrinking)
            {
                GIZMOS_currentGraphs = graphs[graphs_index];
                List<Graph> nextGraphsIteration = new List<Graph>();
                for (var g = 0; g < graphs[graphs_index].Count; ++g)
                {
                    var graph = graphs[graphs_index][g].DeepCopy();
                    var area = graph.ComputeArea();
                    if (area < UnityVectorExtensions.Epsilon)
                    {
                        for (int i = 0; i < graph.points.Count; ++i)
                        {
                            // TODO: check that points closest to graph.intersectionPoints not to be considered
                            if (graph.IsClosestPointToAnyIntersection(i) ||
                                graph.IsClosestPointToAnyIntersection(i + 1))
                            {
                                continue;
                            }
                        }
                        for (int i = 0; i < graph.points.Count; ++i)
                        {
                            // TODO: find end point of line, and dont set the normal to zero there, so the line can get shorter
                            // todo: or flipping normals back and forth should also work 
                            // or center of mass -> does not wokr
                            graph.points[i].normal = Vector2.zero; // stops shrinking when area is very small -> line
                        }
                        // graph.Simplify(); // TODO: need to explore this option more
                    }
                    
                    graph.Shrink(shrinkAmount);

                    /// 2. DO until Graph G has intersections
                    /// 2.a.: Found 1 intersection, divide G into g1, g2. Then, G=g2, continue from 2.
                    /// Result of 2 is G in subgraphs without intersections: g1, g2, ..., gn.
                    Graph.DivideAlongIntersections(graph, out List<Graph> subgraphs);
                    nextGraphsIteration.AddRange(subgraphs);
                }

                graphs.Add(nextGraphsIteration);
                ++graphs_index;
                GIZMOS_subGraphs = nextGraphsIteration;

                shrinking = false;
                foreach (var graph in graphs[graphs_index])
                {
                    if (graph.IsShrinkable())
                    {
                        shrinking = true;
                        break;
                    }
                }
            }
            
            return true;
        }
        
        private List<Graph> CreateGraph(in Vector2[] path, in float sensorRatio)
        {
            if (path == null || path.Length == 0)
            {
                return new List<Graph>();
            }

            List<Point2> pathPoints = new List<Point2>();
            foreach (var p in path)
            {
                pathPoints.Add(new Point2
                {
                    position = p,
                });
            }

            List<Point2> points = Graph.RotateListToLeftmost(pathPoints);
            Graph graph = new Graph
            {
                points = points,
            };
            graph.sensorRatio = sensorRatio;
            graph.ComputeNormals();
            graph.FlipNormals();
            graph.ComputeArea();
            if (!graph.ClockwiseOrientation)
            {
                graph.FlipNormals();
                graph.ComputeArea();
            }
            return new List<Graph> {graph};
        }

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
                newGraph.ComputeNormals();
                newGraph.FlipNormals();
                newGraph.ComputeArea();
                if (!newGraph.ClockwiseOrientation)
                {
                    newGraph.FlipNormals();
                    newGraph.ComputeArea();
                }
                newGraphs.Add(new List<Graph> { newGraph });
            }

            return newGraphs;
        }


        internal ConfinerState GetConfinerAtOrthoSize(float orthographicSize)
        {
            ConfinerState result = new ConfinerState();
            for (int i = confinerStates.Count - 1; i >= 0; --i)
            {
                if (confinerStates[i].windowSize <= orthographicSize)
                {
                    if (i == confinerStates.Count - 1)
                    {
                        result = confinerStates[i];
                    }
                    else if (i % 2 == 0)
                    {
                        result = 
                            ConfinerStateLerp(confinerStates[i], confinerStates[i+1], 
                                Mathf.InverseLerp(confinerStates[i].windowSize, confinerStates[i + 1].windowSize, orthographicSize));
                    }
                    else
                    {
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

        private ConfinerState ConfinerStateLerp(in ConfinerState left, in ConfinerState right, float lerp)
        {
            if (left.graphs.Count != right.graphs.Count)
            {
                Debug.Log("SOMETHINGS NOT RIGHT 1 - PathLerp");
                return left;
            }
            for (int i = 0; i < left.graphs.Count; ++i)
            {
                if (left.graphs[i].points.Count != right.graphs[i].points.Count)
                {
                    Debug.Log("SOMETHINGS NOT RIGHT 2 - PathLerp");
                    return left;
                }
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
                    r.points.Add(new Point2
                    {
                        position = Vector2.Lerp(left.graphs[i].points[j].position, right.graphs[i].points[j].position, lerp),
                    });
                }
                result.graphs.Add(r);
            }
            return result;
        }


        
        private List<ConfinerState> confinerStates;
        internal void TrimGraphs()
        {
            // TODO: List<Graph> should hace a state marker -> managed by the graph division -> incerement state when state change happens
            // todo: statechange (intersection, or skeleton skrinking)
            
            int stateStart = graphs.Count - 1;
            // going backwards, so we can remove without problems
            for (int i = graphs.Count - 2; i >= 0; --i)
            {
                if (graphs[stateStart].Count != graphs[i].Count || i == 0)
                {
                    // state0_min, ..., state0_max, state1_min, ... state1_max
                    // ... parts need to be removed
                    // when graphs[i].Count != graphs[j].Count, then we are at state0_max
                    // so remove all between state0_max + 2, to state1_max - 1.
                    var stateEnd = i != 0 ? i + 2 : 1;
                    if (stateEnd < stateStart) {
                        graphs.RemoveRange(stateEnd, stateStart - stateEnd);
                    }
                    stateStart = i;
                }
            }

            confinerStates = new List<ConfinerState>();
            for (int i = 0; i < graphs.Count; ++i)
            {
                confinerStates.Add(new ConfinerState
                {
                    windowSize = graphs[i][0].windowDiagonal,
                    graphs = graphs[i],
                    state = graphs[i].Count,
                });
            }
            
            for (int i = 0; i < confinerStates.Count; i += 2)
            {
                if (i + 1 == confinerStates.Count || confinerStates[i + 1].state != confinerStates[i].state)
                {
                    confinerStates.Insert(i + 1, confinerStates[i]);
                }
            }
        }
        
        void OnDrawGizmosSelected()
        {
            if (GIZMOS_currentGraphs == null)
            {
                return;
            }

            Vector2 offset = Vector2.zero;
        
            // original input
            for (int i = 0; i < GIZMOS_input.Count - 1; ++i)
            {
                Gizmos.color = Color.black;
                Gizmos.DrawLine(offset + GIZMOS_input[i], offset + GIZMOS_input[i + 1]);
            }
        
            // graphs
            for (int i = 0; i < GIZMOS_subGraphs.Count; ++i)
            {
                float color = ((float) i / (float) GIZMOS_subGraphs.Count) * 0.8f;
        
                Gizmos.color = new Color(i % 2, 0.9f - color, 0.25f);
                for (int j = 0; j < GIZMOS_subGraphs[i].points.Count; ++j)
                {
                    if (j == 0)
                    {
                        Handles.Label(offset + GIZMOS_subGraphs[i].points[j].position, i.ToString());
                    }
        
                    Gizmos.DrawLine(offset + GIZMOS_subGraphs[i].points[j].position,
                        offset + GIZMOS_subGraphs[i].points[(j + 1) % GIZMOS_subGraphs[i].points.Count].position);
                }
        
                if (GIZMOS_drawIntersection)
                    foreach (var t in GIZMOS_subGraphs[i].intersectionPoints)
                    {
                        Gizmos.DrawSphere(offset + t, 1f);
                    }
        
                // graph connections
                foreach (var t in GIZMOS_subGraphs[i].intersectionPoints)
                {
                    Vector2 closestPoint = GIZMOS_subGraphs[i].ClosestPoint(t);
                    Gizmos.DrawLine(offset + t, offset + closestPoint);
                }
            }
        
            // graph normals
            for (int i = 0; i < GIZMOS_subGraphs.Count; ++i)
            {
                Gizmos.color = Color.magenta;
                for (int j = 0; j < GIZMOS_subGraphs[i].points.Count; ++j)
                {
                    Gizmos.DrawLine(offset + GIZMOS_subGraphs[i].points[j].position,
                        offset + GIZMOS_subGraphs[i].points[j].position + GIZMOS_subGraphs[i].points[j].normal);
                }
            }
        }
        
        // private void OnValidate()
        // {
        //     Bake = true;
        // }
    }
}