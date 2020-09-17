using System;
using System.Collections.Generic;
using System.Linq;
using Cinemachine.Utility;
using UnityEditor;
using UnityEditor.Graphs;
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
        [Tooltip("The 2D shape within which the camera is to be contained")]
        public Collider2D m_BoundingShape2D;
        
        [Tooltip("How gradually to return the camera to the bounding volume if it goes beyond the borders.  "
                 + "Higher numbers are more gradual.")]
        [Range(0, 10)]
        public float m_Damping = 0;

        [Tooltip("Damping applied automatically around corners to avoid jumps.  "
                 + "Higher numbers produce more smooth cornering.")]
        [Range(0, 10)]
        public float m_CornerDamping = 0;
        private float m_CornerAngleTreshold = 10f;

        [Tooltip("Stops any kind of damping when the camera gets back inside the confiner m_area.  ")]
        public bool m_StopDampingWithinConfiner = false;
        
        // advanced features
        public bool m_DrawGizmosDebug = false;
        [HideInInspector, SerializeField] internal bool m_AutoBake = true;
        [HideInInspector, SerializeField] internal bool m_TriggerBake = false;
        [HideInInspector, SerializeField] internal bool m_TriggerClearCache = false;
        
        private static readonly float m_bakedConfinerResolution = 0.005f;
        
        internal enum BakeProgressEnum { EMPTY, BAKING, BAKED, INVALID_CACHE }
        [HideInInspector, SerializeField] internal BakeProgressEnum BakeProgress = BakeProgressEnum.INVALID_CACHE;

        private List<List<Vector2>> m_originalPath;
        private List<List<Vector2>> m_originalPathCache;
        private int m_originalPathTotalPointCount;
        
        private float m_frustumHeightCache;
        private List<List<Vector2>> m_currentPathCache;

        private List<List<ShrinkablePolygon>> m_graphs;
        private List<ConfinerOven.ConfinerState> m_confinerStates;
        private ConfinerOven m_confinerBaker = null;
        private ConfinerStateToPath m_confinerStateConverter = null;

        /// <summary>
        /// Trigger rebake process manually.
        /// The confiner rebakes iff an input parameter affecting the outcome of the baked result.
        /// </summary>
        private void Bake()
        {
            m_TriggerBake = true;
        }

        /// <summary>
        /// Trigger force rebake process manually.
        /// The confiner rebakes even if no input was changed.
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
                if (!ValidateConfinerStateCache(state.Lens.Aspect, out bool pathChanged))
                {
                    return; // invalid path
                }
                
                float frustumHeight = CalculateFrustumHeight(state, vcam);
                ValidateCompositeColliderCache(pathChanged, frustumHeight);

                var extra = GetExtraState<VcamExtraState>(vcam);
                Vector3 displacement = ConfinePoint(state.CorrectedPosition);
                if (VirtualCamera.PreviousStateIsValid && deltaTime >= 0)
                { 
                    var originalDisplacement = displacement;
                    var displacementAngle = Vector2.Angle(extra.m_previousDisplacement, displacement);
                    if (m_CornerDamping > 0 && displacementAngle > m_CornerAngleTreshold)
                    {
                        Vector3 delta = displacement - extra.m_previousDisplacement;
                        delta = Damper.Damp(delta, m_CornerDamping, deltaTime);
                        displacement = extra.m_previousDisplacement + delta;
                    }
                    else if (m_Damping > 0)
                    {
                        Vector3 delta = displacement - extra.m_previousDisplacement;
                        delta = Damper.Damp(delta, m_Damping, deltaTime);
                        displacement = extra.m_previousDisplacement + delta;
                    }
                    
                    if (m_StopDampingWithinConfiner && ConfinePoint(state.CorrectedPosition + displacement).sqrMagnitude <= UnityVectorExtensions.Epsilon)
                    {
                        displacement = originalDisplacement;
                    }
                }
                extra.m_previousDisplacement = displacement;
                state.PositionCorrection += displacement;
                extra.confinerDisplacement = displacement.magnitude;
            }
        }

        // <summary>
        /// Calculates Frustum Height
        /// camera window
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
                var R = Quaternion.Inverse(m_BoundingShape2D.transform.rotation);
                var planePosition = R * m_BoundingShape2D.transform.position;
                var cameraPosition = R * vcam.transform.position;
                var distance = Mathf.Abs(planePosition.z - cameraPosition.z);
                frustumHeight = distance * Mathf.Tan(state.Lens.FieldOfView * 0.5f * Mathf.Deg2Rad);
            }

            return frustumHeight;
        }

        private Vector3 ConfinePoint(Vector3 camPos)
        {
            Vector2 camPos2D = camPos;

            if (ShrinkablePolygon.IsInside(m_currentPathCache, camPos2D))
            {
                return Vector3.zero;
            }

            Vector2 closest = camPos2D;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < m_currentPathCache.Count; ++i)
            {
                int numPoints = m_currentPathCache[i].Count;
                if (numPoints > 0)
                {
                    // Vector2 v0 = m_BoundingCompositeShape2D.transform.TransformPoint(m_currentPathCache[i][numPoints - 1]);
                    Vector2 v0 = m_currentPathCache[i][numPoints - 1];
                    for (int j = 0; j < numPoints; ++j)
                    {
                        // Vector2 v = m_BoundingCompositeShape2D.transform.TransformPoint(m_currentPathCache[i][j]);
                        Vector2 v = m_currentPathCache[i][j];
                        Vector2 c = Vector2.Lerp(v0, v, camPos2D.ClosestPointOnSegment(v0, v));
                        float d = Vector2.SqrMagnitude(camPos2D - c);
                        if (d < bestDistance)
                        {
                            bestDistance = d;
                            closest = c;
                        }
                        v0 = v;
                    }
                }
            }
            return closest - camPos2D;
        }
        
        private class VcamExtraState
        {
            public Vector3 m_previousDisplacement;
            public float confinerDisplacement;
            public bool applyAfterAim;
        };

        private float m_sensorRatioCache;
        private float m_bakedConfinerResolutionCache;
        private Vector3 m_boundingShapePositionCache;
        private Vector3 m_boundingShapeScaleCache;
        private Quaternion m_boundingShapeRotationCache;
        private void InvalidatePathCache()
        {
            m_originalPath = null;
            m_originalPathCache = null;
            m_sensorRatioCache = 0;
            m_boundingShapePositionCache = Vector3.negativeInfinity;
            m_boundingShapeScaleCache = Vector3.negativeInfinity;
            m_boundingShapeRotationCache = new Quaternion(0,0,0,0);
        }
        

        /// <summary>
        /// Checks if we have a valid path cache. Calculates it if needed.
        /// </summary>
        /// <param name="sensorRatio">CameraWindow ratio (width / height)</param>
        /// <param name="pathChanged">True, if the baked path has changed. False, otherwise.</param>
        /// <returns>True, if path is baked and valid. False, if path is invalid or non-existent.</returns>
        private bool ValidateConfinerStateCache(float sensorRatio, out bool pathChanged)
        {
            // TODO: turn this and subsequent functions calls into courotines, to not block ui
            // TODO: before calling this make sure to stop any running courotine 
            // runningCoroutine = StartCoroutine(MyCoroutine());
            // StopCoroutine(runningCoroutine);
            // or async? naw, just set return values as members - this couritne runs alone always

            if (m_TriggerClearCache)
            {
                InvalidatePathCache();
                m_TriggerClearCache = false;
            }
            
            pathChanged = false;
            var cacheIsEmpty = m_confinerStates == null;
            var cacheIsValid = 
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
            pathChanged = true;

            bool boundingShapeTransformChanged = BoundingShapeTransformChanged();
            if (boundingShapeTransformChanged || m_originalPath == null)
            {
                Type colliderType = m_BoundingShape2D == null ? null:  m_BoundingShape2D.GetType();
                if (colliderType == typeof(PolygonCollider2D))
                {
                    PolygonCollider2D poly = m_BoundingShape2D as PolygonCollider2D;
                    if (boundingShapeTransformChanged || m_originalPath == null || m_originalPath.Count != poly.pathCount || 
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
                    if (boundingShapeTransformChanged || m_originalPath == null || m_originalPath.Count != poly.pathCount || 
                        m_originalPathTotalPointCount != poly.pointCount)
                    {
                        m_originalPath = new List<List<Vector2>>();
                        Vector2[] path = new Vector2[poly.pointCount];
                        var lossyScale = m_BoundingShape2D.transform.lossyScale;
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
                    return false;
                }
            }

            m_bakedConfinerResolutionCache = m_bakedConfinerResolution;
            m_sensorRatioCache = sensorRatio;
            GetConfinerOven().BakeConfiner(m_originalPath, m_sensorRatioCache, m_bakedConfinerResolutionCache);
            m_confinerStates = GetConfinerOven().GetGraphsAsConfinerStates();

            m_boundingShapePositionCache = m_BoundingShape2D.transform.position;
            m_boundingShapeRotationCache = m_BoundingShape2D.transform.rotation;
            m_boundingShapeScaleCache = m_BoundingShape2D.transform.localScale;

            BakeProgress = BakeProgressEnum.BAKED;
            return true;
        }

        private bool BoundingShapeTransformChanged()
        {
            return m_BoundingShape2D != null && 
                   (m_boundingShapePositionCache != m_BoundingShape2D.transform.position ||
                    m_boundingShapeScaleCache != m_BoundingShape2D.transform.localScale ||
                    m_boundingShapeRotationCache != m_BoundingShape2D.transform.rotation);
        }

        private void ValidateCompositeColliderCache(bool pathChanged, float frustumHeight)
        {
            if (pathChanged ||
                m_currentPathCache == null || 
                Math.Abs(frustumHeight - m_frustumHeightCache) > m_bakedConfinerResolution)
            {
                m_frustumHeightCache = frustumHeight;
                m_confinerCache = GetConfinerOven().GetConfinerAtOrthoSize(m_frustumHeightCache);
                ShrinkablePolygon.ConvertToPath(m_confinerCache.graphs, out m_currentPathCache);
            }
        }
        
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
            InvalidatePathCache();
        }

        private void OnDrawGizmosSelected()
        {
            if (!m_DrawGizmosDebug) return;
            if (m_confinerStates != null && m_BoundingShape2D != null)
            {
                Vector2 offset = Vector2.zero;// m_BoundingShape2D.transform.m_position;
                // for (var index = 0; index < m_confinerStates.Count; index++)
                // {
                //     var confinerState = m_confinerStates[index];
                //     for (var index1 = 0; index1 < confinerState.graphs.Count; index1++)
                //     {
                //         Gizmos.color = new Color((float) index / (float) m_confinerStates.Count, (float) index1 / (float) confinerState.graphs.Count, 0.2f);
                //         var g = confinerState.graphs[index1];
                //         //Handles.Label(offset + g.m_points[0].m_position, "A="+g.ComputeSignedArea());
                //         //Handles.Label(offset + g.m_points[0].m_position, "W="+g.m_windowDiagonal);
                //         for (int i = 0; i < g.m_points.Count; ++i)
                //         {
                //             Gizmos.DrawLine(offset + g.m_points[i].m_position, offset + g.m_points[(i + 1) % g.m_points.Count].m_position);
                //         }
                //     }
                // }

                Gizmos.color = Color.white;
                // for (var index = 0; index < m_confinerStates.Count; index++)
                {
                    // var confinerState = m_confinerStates[index];
                    var confinerState = m_confinerStates[0];
                    foreach (var g in confinerState.graphs)
                    {
                        for (int i = 0; i < g.m_points.Count; ++i)
                        {
                            Gizmos.DrawLine(offset + g.m_points[i].m_position,
                                offset + g.m_points[i].m_position + g.m_points[i].m_shrinkDirection);
                        }
                    }
                }
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
                for (var index1 = 0; index1 < confinerState.graphs.Count; index1++)
                {
                    Gizmos.color = new Color((float) index / (float) m_confinerStates.Count,
                        (float) index1 / (float) confinerState.graphs.Count, 0.2f);
                    var g = confinerState.graphs[index1];
                    //Handles.Label(g.m_points[0].m_position, "A=" + g.ComputeSignedArea());
                    for (int i = 0; i < g.m_points.Count; ++i)
                    {
                        Gizmos.DrawLine(
                            g.m_points[i].m_position,
                            g.m_points[(i + 1) % g.m_points.Count].m_position);
                    }
                }
            }
            
        }
    }
}