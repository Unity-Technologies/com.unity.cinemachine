using System;
using System.Collections.Generic;
using Cinemachine.Utility;
using UnityEditor;
using UnityEngine;

namespace Cinemachine
{ 
    [SaveDuringPlay]
#if UNITY_2018_3_OR_NEWER
    [ExecuteAlways]
#else
    [ExecuteInEditMode]
#endif
    public class CinemachineAdvanced2DConfiner : CinemachineExtension
    {
        /// <summary>The 2D shape within which the camera is to be contained.</summary>
        [Tooltip("The 2D shape within which the camera is to be contained.  " +
                 "Can be a 2D polygon or 2D composite collider.")]
        public Collider2D m_BoundingShape2D;
        
        [Tooltip("How gradually to return the camera to the bounding volume if it goes beyond the borders.  "
                 + "Higher numbers are more gradual.")]
        [Range(0, 10)]
        public float m_Damping = 0;

        [Tooltip("Damping applied automatically around corners to avoid jumps.  "
                 + "Higher numbers are more gradual.")]
        [Range(0, 10)]
        public float m_CornerDamping = 0;
        private bool m_CornerDampingIsOn = false;
        private float m_CornerAngleTreshold = 10f;

        [Tooltip("Damping applied automatically when getting close to the sides.")]
        [Range(0, 10)]
        public float m_SideSmoothing = 0;
        private float m_SideSmoothingProximity = 1;
        

        [Tooltip("Stops confiner damping when the camera gets back inside the confined area.")]
        public bool m_StopDampingWithinConfiner = false;
        
        // advanced features
        public bool m_DrawGizmosDebug = false; // TODO: modify gizmos to only draw what's relevant to a user! After
                                               // Patrick's test - it may be useful for Patrick
        [HideInInspector, SerializeField] internal bool m_AutoBake = true; // TODO: remove
                                                                           // reason: if user wants to
                                                                           // switch between cameras, it is better
                                                                           // to just have a different advnaced confiner
                                                                           // for each setup
        [HideInInspector, SerializeField] internal bool m_TriggerBake = false;
        [HideInInspector, SerializeField] internal bool m_TriggerClearCache = false;
        [HideInInspector, SerializeField] internal float m_MaxOrthoSize;
        [HideInInspector, SerializeField] internal bool m_ShrinkToPointsExperimental;
        
        private static readonly float m_bakedConfinerResolution = 0.005f;
        
        internal enum BakeProgressEnum { EMPTY, BAKING, BAKED, INVALID_CACHE } // TODO: remove states after
                                                                               // fist pass cleanup!
        [HideInInspector, SerializeField] internal BakeProgressEnum BakeProgress = BakeProgressEnum.INVALID_CACHE;

        private List<List<Vector2>> m_originalPath;
        private List<List<Vector2>> m_originalPathCache;
        private int m_originalPathTotalPointCount;
        
        private float m_frustumHeightCache;
        private List<List<Vector2>> m_currentPathCache;

        private List<ConfinerOven.ConfinerState> m_confinerStates;
        private ConfinerOven m_confinerBaker = null;

        /// <summary>
        /// Trigger rebake process manually.
        /// The confiner rebakes iff an input parameters affecting the outcome of the baked result change.
        /// </summary>
        private void Bake()
        {
            m_TriggerBake = true;
        }

        /// <summary>
        /// Force rebake process manually. This function invalidates the cache, thus ensuring a rebake.
        /// </summary>
        public void ForceBake()
        {
            InvalidatePathCache();
            Bake();
        }

        private ConfinerOven.ConfinerState m_confinerCache;
        protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam, 
            CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            if (stage == CinemachineCore.Stage.Body)
            {
                if (!ValidateConfinerStateCache(state.Lens.Aspect, out bool confinerStateChanged))
                {
                    return; // invalid path
                }
                
                float frustumHeight = CalculateFrustumHeight(state, vcam);
                ValidatePathCache(confinerStateChanged, frustumHeight);

                var extra = GetExtraState<VcamExtraState>(vcam);
                Vector3 displacement = ConfinePoint(state.CorrectedPosition);
                if (VirtualCamera.PreviousStateIsValid && deltaTime >= 0)
                { 
                    float displacementAngle = Vector2.Angle(extra.m_previousDisplacement, displacement);
                    if (m_CornerDampingIsOn || m_CornerDamping > 0 && displacementAngle > m_CornerAngleTreshold)
                    {
                        Vector3 delta = displacement - extra.m_previousDisplacement;
                        var deltaDamped = Damper.Damp(delta, m_CornerDamping, deltaTime);
                        displacement = extra.m_previousDisplacement + deltaDamped;

                        m_CornerDampingIsOn = displacementAngle > UnityVectorExtensions.Epsilon ||
                                              delta.sqrMagnitude > UnityVectorExtensions.Epsilon;
                    }
                    else if (m_SideSmoothing > 0)
                    {
                        
                        GetClosestEdgeNormal(state.CorrectedPosition, in m_currentPathCache, out float distance, out Vector2 normal);
                        if (distance < m_SideSmoothingProximity)
                        {
                            Vector3 delta = displacement - extra.m_previousDisplacement;
                            
                            Vector3 deltaX = new Vector3(delta.x, 0, 0);
                            if (delta.x * normal.x < 0) // pointing in opposite dir
                            {
                                deltaX = Damper.Damp(deltaX, m_SideSmoothing, deltaTime);
                            }
                            Vector3 deltaY = new Vector3(0, delta.y, 0);
                            if (delta.y * normal.y < 0) // pointing in opposite dir
                            {
                                deltaY = Damper.Damp(deltaY, m_SideSmoothing, deltaTime);
                            }
                            
                            displacement = extra.m_previousDisplacement + new Vector3(deltaX.x, deltaY.y, 0);
                        }
                    }
                    else if (m_Damping > 0)
                    {
                        Vector3 delta = displacement - extra.m_previousDisplacement;
                        delta = Damper.Damp(delta, m_Damping, deltaTime);
                        displacement = extra.m_previousDisplacement + delta;
                    }
                }
                extra.m_previousDisplacement = displacement;
                state.PositionCorrection += displacement;
                extra.confinerDisplacement = displacement.magnitude;
            }
        }

        // <summary>
        /// Calculates Frustum Height for orthographic or perspective camera.
        /// Ascii illustration of Frustum Height:
        ///  |----+----+  -\
        ///  |    |    |    } frustumHeight = cameraWindowHeight / 2
        ///  |---------+  -/
        ///  |    |    |
        ///  |---------|
        /// </summary>
        private float CalculateFrustumHeight(in CameraState state, in CinemachineVirtualCameraBase vcam)
        {
            float frustumHeight;
            if (state.Lens.Orthographic)
            {
                frustumHeight = Mathf.Abs(state.Lens.OrthographicSize);
            }
            else
            {
                Quaternion inverseRotation = Quaternion.Inverse(m_BoundingShape2D.transform.rotation);
                Vector3 planePosition = inverseRotation * m_BoundingShape2D.transform.position;
                Vector3 cameraPosition = inverseRotation * vcam.transform.position;
                float distance = Mathf.Abs(planePosition.z - cameraPosition.z);
                frustumHeight = distance * Mathf.Tan(state.Lens.FieldOfView * 0.5f * Mathf.Deg2Rad);
            }
            return frustumHeight;
        }

        private void GetClosestEdgeNormal(Vector2 position, in List<List<Vector2>> polygons, 
            out float distance, out Vector2 normal)
        {
            normal = Vector2.zero;

            int closestPolygonIndex = 0;
            int closestPointIndex = 0;
            var minDistance = float.MaxValue;
            for (var i = 0; i < polygons.Count; i++)
            {
                for (var p = 0; p < polygons[i].Count; p++)
                {
                    distance = (polygons[i][p] - position).sqrMagnitude;
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        closestPolygonIndex = i;
                        closestPointIndex = p;
                    }
                }
            }

            var d1 = (polygons[closestPolygonIndex][closestPointIndex == 0 ? 
                polygons[closestPolygonIndex].Count - 1 : 
                closestPointIndex - 1] - position).sqrMagnitude;
            var d2 = (polygons[closestPolygonIndex][closestPointIndex == polygons[closestPolygonIndex].Count - 1 ? 
                0 : 
                closestPointIndex + 1] - position).sqrMagnitude;

            if (d1 < d2)
            {
                int secondClosestIndex = closestPointIndex == 0 ? 
                    polygons[closestPolygonIndex].Count - 1 : 
                    closestPointIndex - 1;
                var edge = polygons[closestPolygonIndex][closestPointIndex] -
                           polygons[closestPolygonIndex][secondClosestIndex];
                normal = new Vector2(edge.y, -edge.x);
                distance = UnityVectorExtensions.DistanceBetweenPointAndLine(position, 
                    polygons[closestPolygonIndex][closestPointIndex],
                    polygons[closestPolygonIndex][secondClosestIndex]);
            }
            else
            {
                int secondClosestIndex = closestPointIndex == polygons[closestPolygonIndex].Count - 1 ? 
                    0 : 
                    closestPointIndex + 1;
                
                var edge = polygons[closestPolygonIndex][secondClosestIndex] -
                           polygons[closestPolygonIndex][closestPointIndex];
                normal = new Vector2(edge.y, -edge.x);
                distance = UnityVectorExtensions.DistanceBetweenPointAndLine(position, 
                    polygons[closestPolygonIndex][secondClosestIndex],
                    polygons[closestPolygonIndex][closestPointIndex]);
            }

            smoothingNormalPos = position;
            smoothingNormalDebug = normal.normalized * 2f;
            smoothingDistance = distance;
        }

        /// <summary>
        /// Confines input 2D point within the confined area.
        /// </summary>
        /// <param name="positionToConfine">2D point to confine</param>
        /// <returns>Confined position</returns>
        private Vector2 ConfinePoint(Vector2 positionToConfine)
        {
            if (ShrinkablePolygon.IsInside(m_currentPathCache, positionToConfine))
            {
                return Vector2.zero;
            }

            Vector2 closest = positionToConfine;
            float minDistance = float.MaxValue;
            for (int i = 0; i < m_currentPathCache.Count; ++i)
            {
                int numPoints = m_currentPathCache[i].Count;
                if (numPoints > 0)
                {
                    Vector2 v0 = m_currentPathCache[i][numPoints - 1];
                    for (int j = 0; j < numPoints; ++j)
                    {
                        Vector2 v = m_currentPathCache[i][j];
                        Vector2 c = Vector2.Lerp(v0, v, positionToConfine.ClosestPointOnSegment(v0, v));
                        float distance = Vector2.SqrMagnitude(positionToConfine - c);
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            closest = c;
                        }
                        v0 = v;
                    }
                }
            }
            return closest - positionToConfine;
        }
        
        private class VcamExtraState
        {
            public Vector3 m_previousDisplacement;
            public float confinerDisplacement;
            public bool applyAfterAim;
        };

        private float m_sensorRatioCache;
        private Vector3 m_boundingShapePositionCache;
        private Vector3 m_boundingShapeScaleCache;
        private Quaternion m_boundingShapeRotationCache;
        /// <summary>
        /// Invalidates path cache.
        /// </summary>
        private void InvalidatePathCache()
        {
            m_originalPath = null;
            m_originalPathCache = null;
            m_sensorRatioCache = 0;
            m_boundingShapePositionCache = Vector3.negativeInfinity;
            m_boundingShapeScaleCache = Vector3.negativeInfinity;
            m_boundingShapeRotationCache = new Quaternion(0,0,0,0);
        }
        
        private float m_bakedConfinerResolutionCache;
        /// <summary>
        /// Checks if we have a valid confiner state cache. Calculates it if cache is invalid, and bake was requested.
        /// </summary>
        /// <param name="sensorRatio">Camera window ratio (width / height)</param>
        /// <param name="confinerStateChanged">True, if the baked confiner state has changed. False, otherwise.</param>
        /// <returns>True, if path is baked and valid. False, if path is invalid or non-existent.</returns>
        private bool ValidateConfinerStateCache(float sensorRatio, out bool confinerStateChanged)
        {
            if (m_TriggerClearCache)
            {
                InvalidatePathCache();
                m_TriggerClearCache = false;
            }
            
            confinerStateChanged = false;
            bool cacheIsEmpty = m_confinerStates == null;
            bool cacheIsValid = 
                m_originalPath != null && // first time?
                !cacheIsEmpty && // has a prev. baked result?
                !BoundingShapeTransformChanged() && // confiner was moved or rotated or scaled?
                Math.Abs(m_sensorRatioCache - sensorRatio) < UnityVectorExtensions.Epsilon && // sensor ratio changed?
                Math.Abs(m_bakedConfinerResolution - m_bakedConfinerResolutionCache) < UnityVectorExtensions.Epsilon; // resolution changed?
            if (!m_AutoBake && !m_TriggerBake)
            {
                if (cacheIsEmpty)
                {
                    BakeProgress = BakeProgressEnum.EMPTY;
                    return false; // if m_confinerStates is null, then we don't have path -> false
                }
                else if (!cacheIsValid)
                {
                    BakeProgress = BakeProgressEnum.INVALID_CACHE;
                    return true;
                }
                else
                {
                    BakeProgress = BakeProgressEnum.BAKED;
                    return true;
                }
            }
            else if (!cacheIsEmpty && cacheIsValid)
            {
                m_TriggerBake = false;
                BakeProgress = BakeProgressEnum.BAKED;
                return true;
            }
            
            m_TriggerBake = false;
            BakeProgress = BakeProgressEnum.BAKING;
            confinerStateChanged = true;

            bool boundingShapeTransformChanged = BoundingShapeTransformChanged();
            if (boundingShapeTransformChanged || m_originalPath == null)
            {
                Type colliderType = m_BoundingShape2D == null ? null:  m_BoundingShape2D.GetType();
                if (colliderType == typeof(PolygonCollider2D))
                {
                    PolygonCollider2D poly = m_BoundingShape2D as PolygonCollider2D;
                    if (boundingShapeTransformChanged || m_originalPath == null || 
                        m_originalPath.Count != poly.pathCount || 
                        m_originalPathTotalPointCount != poly.GetTotalPointCount())
                    { 
                        m_originalPath = new List<List<Vector2>>();
                        for (int i = 0; i < poly.pathCount; ++i)
                        {
                            Vector2[] path = poly.GetPath(i);
                            List<Vector2> dst = new List<Vector2>();
                            for (int j = 0; j < path.Length; ++j)
                            {
                                dst.Add(m_BoundingShape2D.transform.TransformPoint(path[j]));
                            }
                            m_originalPath.Add(dst);
                        }
                        m_originalPathTotalPointCount = poly.GetTotalPointCount();
                    }
                }
                else if (colliderType == typeof(CompositeCollider2D))
                {
                    CompositeCollider2D poly = m_BoundingShape2D as CompositeCollider2D;
                    if (boundingShapeTransformChanged || m_originalPath == null || 
                        m_originalPath.Count != poly.pathCount || m_originalPathTotalPointCount != poly.pointCount)
                    {
                        m_originalPath = new List<List<Vector2>>();
                        Vector2[] path = new Vector2[poly.pointCount];
                        Vector3 lossyScale = m_BoundingShape2D.transform.lossyScale;
                        Vector2 revertCompositeColliderScale = new Vector2(
                            1f / lossyScale.x, 
                            1f / lossyScale.y);
                        for (int i = 0; i < poly.pathCount; ++i)
                        {
                            int numPoints = poly.GetPath(i, path);
                            List<Vector2> dst = new List<Vector2>();
                            for (int j = 0; j < numPoints; ++j)
                            {
                                dst.Add(m_BoundingShape2D.transform.TransformPoint(
                                    path[j] * revertCompositeColliderScale));
                            }
                            m_originalPath.Add(dst);
                        }
                        m_originalPathTotalPointCount = poly.pointCount;
                    }
                }
                else
                {
                    BakeProgress = BakeProgressEnum.INVALID_CACHE;
                    InvalidatePathCache();
                    return false; // input collider is invalid
                }
            }

            m_bakedConfinerResolutionCache = m_bakedConfinerResolution;
            m_sensorRatioCache = sensorRatio;
            GetConfinerOven().BakeConfiner(m_originalPath, m_sensorRatioCache, m_bakedConfinerResolutionCache, 
                m_MaxOrthoSize, m_ShrinkToPointsExperimental);
            m_confinerStates = GetConfinerOven().GetShrinkablePolygonsAsConfinerStates();

            m_boundingShapePositionCache = m_BoundingShape2D.transform.position;
            m_boundingShapeRotationCache = m_BoundingShape2D.transform.rotation;
            m_boundingShapeScaleCache = m_BoundingShape2D.transform.localScale;

            BakeProgress = BakeProgressEnum.BAKED;
            return true;
        }

        /// <summary>
        /// Checks if the input bounding shape was moved, rotated, or scaled.
        /// </summary>
        /// <returns></returns>
        private bool BoundingShapeTransformChanged()
        {
            return m_BoundingShape2D != null && 
                   (m_boundingShapePositionCache != m_BoundingShape2D.transform.position ||
                    m_boundingShapeScaleCache != m_BoundingShape2D.transform.localScale ||
                    m_boundingShapeRotationCache != m_BoundingShape2D.transform.rotation);
        }

        /// <summary>
        /// Check that the path cache was converted from the current confiner cache, or
        /// converts it if the frustum height was changed.
        /// </summary>
        /// <param name="confinerStateChanged">Confiner cache was changed</param>
        /// <param name="frustumHeight">Camera frustum height</param>
        private void ValidatePathCache(bool confinerStateChanged, float frustumHeight)
        {
            if (confinerStateChanged ||
                m_currentPathCache == null || 
                Math.Abs(frustumHeight - m_frustumHeightCache) > m_bakedConfinerResolution)
            {
                m_frustumHeightCache = frustumHeight;
                m_confinerCache = GetConfinerOven().GetConfinerAtFrustumHeight(m_frustumHeightCache);
                ShrinkablePolygon.ConvertToPath(m_confinerCache.polygons, m_frustumHeightCache, out m_currentPathCache);
            }
        }
        
        /// <summary>
        /// Singleton for this object's ConfinerOven.
        /// </summary>
        /// <returns>ConfinerOven</returns>
        private ConfinerOven GetConfinerOven()
        {
            if (m_confinerBaker == null)
            {
                m_confinerBaker = new ConfinerOven();
            }

            return m_confinerBaker;
        }
        
        protected override void OnEnable()
        {
            base.OnEnable();
            ForceBake();
        }

        private Vector2 smoothingNormalPos = Vector2.zero;
        private Vector2 smoothingNormalDebug = Vector2.zero;
        private float smoothingDistance = 0;
        private void OnDrawGizmosSelected()
        {
            if (!m_DrawGizmosDebug) return;
            if (m_confinerStates != null && m_BoundingShape2D != null)
            {
                Gizmos.color = Color.red;
                Handles.Label(smoothingNormalPos, smoothingDistance.ToString());
                Gizmos.DrawLine(smoothingNormalPos, smoothingNormalPos + smoothingNormalDebug);
                
                // Vector2 offset = Vector2.zero;// m_BoundingShape2D.transform.m_position;
                // for (var index = 0; index < m_confinerStates.Count; index++)
                // {
                //     var confinerState = m_confinerStates[index];
                //     for (var index1 = 0; index1 < confinerState.polygons.Count; index1++)
                //     {
                //         Gizmos.color = new Color((float) index / (float) m_confinerStates.Count, (float) index1 / (float) confinerState.polygons.Count, 0.2f);
                //         var g = confinerState.polygons[index1];
                //         if (g.m_area < 0.1f)
                //         {
                //             //Handles.Label(offset + g.m_points[0].m_position, "A="+g.m_area);
                //             //Handles.Label(offset + g.m_points[0].m_position, "W="+g.m_windowDiagonal);
                //             for (int i = 0; i < g.m_points.Count; ++i)
                //             {
                //                 Gizmos.DrawLine(offset + g.m_points[i].m_position,
                //                     offset + g.m_points[(i + 1) % g.m_points.Count].m_position);
                //             }
                //         }
                //     }
                // }
                //
                // Gizmos.color = Color.cyan;
                // // for (var index = 0; index < m_confinerStates.Count; index++)
                // {
                //     // var confinerState = m_confinerStates[index];
                //     var confinerState = m_confinerStates[0];
                //     foreach (var g in confinerState.polygons)
                //     {
                //         for (int i = 0; i < g.m_points.Count; ++i)
                //         {
                //             Gizmos.DrawLine(offset + g.m_points[i].m_position,
                //                 offset + g.m_points[i].m_position + g.m_points[i].m_shrinkDirection);
                //         }
                //     }
                // }
            }
            
            if (m_currentPathCache == null || m_BoundingShape2D == null) return;
            
            Gizmos.color = Color.cyan;
            foreach (var path in m_currentPathCache)
            {
                for (var index = 0; index < path.Count; index++)
                {
                    Gizmos.DrawLine(
                        path[index], 
                        path[(index + 1) % path.Count]);
                }
            }

            if (m_confinerStates != null && m_BoundingShape2D != null) 
            {
                var index = 0;
                var confinerState = m_confinerStates[index];
                for (var index1 = 0; index1 < confinerState.polygons.Count; index1++)
                {
                    
                    var g = confinerState.polygons[index1];
                    Handles.Label(g.m_points[0].m_position, "A=" + g.m_area);
                    //Handles.Label(g.m_points[0].m_position, "A=" + g.ComputeSignedArea());
                    for (int i = 0; i < g.m_points.Count; ++i)
                    {
                        Gizmos.color = Color.black;
                        Gizmos.DrawLine(
                            g.m_points[i].m_position,
                            g.m_points[(i + 1) % g.m_points.Count].m_position);
                        Gizmos.color = Color.green;
                        Gizmos.DrawLine(
                            g.m_points[i].m_position,
                            g.m_points[i].m_position + g.m_points[i].m_shrinkDirection);
                    }
                }
            }
            
        }
    }
}