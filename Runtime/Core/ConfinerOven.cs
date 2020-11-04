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

        public ConfinerOven(bool stopAtFirstIntersection)
        {
            m_stopAtFirstIntersection = stopAtFirstIntersection;
        }

        private bool m_stopAtFirstIntersection;
        private List<List<ShrinkablePolygon>> m_shrinkablePolygons;
        public float m_polygonDiagonal;
        public float m_cachedMaxOrthosize;
        
        /// <summary>
        /// Creates shrinkable polygons from input parameters.
        /// The algorithm is divide and conquer. It iteratively shrinks down the input polygon towards its shrink
        /// directions. If the polygon intersects with itself, then we divide the polygon into two polygons at the
        /// intersection point, and continue the algorithm on these two polygons separately. We need to keep track of
        /// the connectivity information between sub-polygons.
        /// </summary>
        public void BakeConfiner(in List<List<Vector2>> inputPath, in float sensorRatio, in float shrinkAmount, 
            float maxOrthosize, in bool shrinkToPoint)
        {
            float polygonHalfHeight = HeightOfAspectBasedBoundingBoxAroundPolygons(inputPath, sensorRatio) / 2f;
            if (maxOrthosize == 0 || maxOrthosize > polygonHalfHeight) // exact comparison to 0 is intentional!
            {
                // ensuring that we don't compute further than what is the theoretical max
                maxOrthosize = polygonHalfHeight; 
            }
            
            m_shrinkablePolygons = CreateShrinkablePolygons(inputPath, sensorRatio);
            var polyIndex = 0;
            var shrinking = true;
            while (shrinking)
            {
                var numPaths = m_shrinkablePolygons[polyIndex].Count;
                var nextPolygonIteration = new List<ShrinkablePolygon>(numPaths);
                for (int g = 0; g < numPaths; ++g)
                {
                    m_shrinkablePolygons[polyIndex][g].ComputeAspectBasedShrinkDirections();
                    ShrinkablePolygon shrinkablePolygon = m_shrinkablePolygons[polyIndex][g].DeepCopy();
                    if (shrinkablePolygon.Shrink(shrinkAmount, shrinkToPoint))
                    {
                        if (shrinkablePolygon.m_FrustumHeight > shrinkAmount * 100f)
                        {
                            shrinkablePolygon.Simplify(shrinkAmount);
                        }
                        if (ShrinkablePolygon.DivideAlongIntersections(shrinkablePolygon,
                            out List<ShrinkablePolygon> subPolygons) &&
                            m_stopAtFirstIntersection)
                        {
                            return; // stop at first intersection
                        }

                        nextPolygonIteration.AddRange(subPolygons);
                    }
                    else
                    {
                        nextPolygonIteration.Add(shrinkablePolygon);
                    }
                }

                m_cachedMaxOrthosize = nextPolygonIteration[0].m_FrustumHeight;

                m_shrinkablePolygons.Add(nextPolygonIteration);
                if (maxOrthosize < m_shrinkablePolygons[polyIndex][0].m_FrustumHeight)
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
            int numPaths = paths == null ? 0 : paths.Count;
            var shrinkablePolygons = new List<List<ShrinkablePolygon>>(numPaths);
            if (numPaths > 0)
            {
                float minX = float.MaxValue, maxX = float.MinValue;
                float minY = float.MaxValue, maxY = float.MinValue;
                for (int i = 0; i < numPaths; ++i)
                {
                    var newShrinkablePolygon = new ShrinkablePolygon(paths[i], aspectRatio);
                    int numPoints = newShrinkablePolygon.m_Points.Count;
                    for (int j = 0; j < numPoints; ++j)
                    {
                        var p = newShrinkablePolygon.m_Points[j].m_Position;
                        minX = Mathf.Min(minX, p.x);
                        minY = Mathf.Min(minY, p.y);
                        maxX = Mathf.Max(maxX, p.x);
                        maxY = Mathf.Max(maxY, p.y);
                    }
                    shrinkablePolygons.Add(new List<ShrinkablePolygon> { newShrinkablePolygon });
                }

                float squareSize = Mathf.Min(maxX - minX, maxY - minY);
                for (int i = 0; i < shrinkablePolygons.Count; ++i)
                {
                    shrinkablePolygons[i][0].m_MinArea = squareSize / 100f;
                }
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
                if (m_confinerStates[i].m_FrustumHeight <= frustumHeight)
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
                            Mathf.Abs(m_confinerStates[i].m_FrustumHeight - frustumHeight) < 
                            Mathf.Abs(m_confinerStates[i + 1].m_FrustumHeight - frustumHeight) ? 
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
            
            float lerpValue = Mathf.InverseLerp(left.m_FrustumHeight, right.m_FrustumHeight, frustumHeight);
            for (int i = 0; i < left.m_Polygons.Count; ++i)
            {
                var r = new ShrinkablePolygon(
                    left.m_Polygons[i].m_AspectRatio,
                    left.m_Polygons[i].m_AspectRatioBasedDiagonal,
                    left.m_Polygons[i].m_NormalDirections)
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
                
                m_confinerStates.Add(new ConfinerState
                {
                    m_FrustumHeight = m_shrinkablePolygons[i][0].m_FrustumHeight,
                    m_Polygons = m_shrinkablePolygons[i],
                    m_State = stateAverage,
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
            m_polygonDiagonal = Mathf.Sqrt(pWidth * pWidth + pHeight * pHeight);
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