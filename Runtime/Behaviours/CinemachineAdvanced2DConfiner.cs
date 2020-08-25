using System;
using System.Collections.Generic;
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

    /// <summary>
    /// Advanced 2D confiner prebakes a confiner for ...
    ///
    /// todo...
    /// If you change the input collider's points (without changing the number of points or ...)
    /// </summary>
    public class CinemachineAdvanced2DConfiner : CinemachineExtension
    {
        // TODO: OnValidate parameters (e.g. m_bakedConfinerResolution)
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

        [Tooltip("Stops any kind of damping when the camera gets back inside the confiner area.  ")]
        public bool m_StopDampingWithinConfiner = false;
        
        // advanced features
        public bool DrawGizmosDebug = false;
        [HideInInspector, SerializeField] internal bool AutoBake = true;
        [HideInInspector, SerializeField] internal bool TriggerBake = false;
        [HideInInspector, SerializeField] internal bool TriggerClearCache = false;
        
        private static readonly float m_bakedConfinerResolution = 0.005f;
        
        internal enum BakeProgressEnum { EMPTY, BAKING, BAKED, INVALID_CACHE }
        [HideInInspector, SerializeField] internal BakeProgressEnum BakeProgress = BakeProgressEnum.INVALID_CACHE;

        private Collider2D m_BoundingCompositeShape2D; // result from converting from m_BoundingShape2D
        
        private List<List<Vector2>> m_originalPath;
        private List<List<Vector2>> m_originalPathCache;
        private int m_originalPathTotalPointCount;
        
        private float frustumHeightCache;
        private List<List<Vector2>> m_currentPathCache;

        private List<List<Graph>> graphs;
        private List<ConfinerState> confinerStates;
        private ConfinerOven _confinerBaker = null;
        private ConfinerStateToPath _confinerStateConverter = null;

        /// <summary>
        /// Trigger rebake process manually.
        /// The confiner rebakes iff an input parameter affecting the outcome of the baked result.
        /// </summary>
        public void Bake()
        {
            TriggerBake = true;
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

        private ConfinerState confinerCache;
        protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam, 
            CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            var extra = GetExtraState<VcamExtraState>(vcam);
            if ((extra.applyAfterAim && stage == CinemachineCore.Stage.Finalize)
                ||
                (!extra.applyAfterAim && stage == CinemachineCore.Stage.Body))
            {
                if (!ValidatePathCache(state.Lens.SensorSize.x / state.Lens.SensorSize.y, out bool pathChanged))
                {
                    return; // invalid path
                }

                if (BakeProgress == BakeProgressEnum.EMPTY || BakeProgress == BakeProgressEnum.BAKING)
                {
                    return; // need to wait until we have a baked cache
                }

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

                if (pathChanged ||
                    m_currentPathCache == null || 
                    m_BoundingCompositeShape2D == null ||
                    Math.Abs(frustumHeight - frustumHeightCache) > m_bakedConfinerResolution)
                {
                    // TODO: performance optimization
                    // TODO: Use polygon union operation, once polygon union operation is exposed by unity core
                    frustumHeightCache = frustumHeight;
                    confinerCache = confinerOven().GetConfinerAtOrthoSize(frustumHeightCache);
                    confinerStateToPath().Convert(confinerCache, 
                        out m_currentPathCache, out m_BoundingCompositeShape2D);
                }
                
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
 
        private Vector3 ConfinePoint(Vector3 camPos)
        {
            // 2D version
            Vector2 p = camPos;
            Vector2 closest = p;
            if (m_BoundingCompositeShape2D.OverlapPoint(camPos))
                return Vector3.zero;

            float bestDistance = float.MaxValue;
            for (int i = 0; i < m_currentPathCache.Count; ++i)
            {
                int numPoints = m_currentPathCache[i].Count;
                if (numPoints > 0)
                {
                    Vector2 v0 = m_BoundingCompositeShape2D.transform.TransformPoint(m_currentPathCache[i][numPoints - 1]);
                    for (int j = 0; j < numPoints; ++j)
                    {
                        Vector2 v = m_BoundingCompositeShape2D.transform.TransformPoint(m_currentPathCache[i][j]);
                        Vector2 c = Vector2.Lerp(v0, v, p.ClosestPointOnSegment(v0, v));
                        float d = Vector2.SqrMagnitude(p - c);
                        if (d < bestDistance)
                        {
                            bestDistance = d;
                            closest = c;
                        }
                        v0 = v;
                    }
                }
            }
            return closest - p;
        }
        
        private class VcamExtraState
        {
            public Vector3 m_previousDisplacement;
            public float confinerDisplacement;
            public bool applyAfterAim;
        };

        private float sensorRatioCache;
        private float bakedConfinerResolutionCache;
        private Vector3 boundingShapePositionCache;
        private Vector3 boundingShapeScaleCache;
        private Quaternion boundingShapeRotationCache;
        private void InvalidatePathCache()
        {
            m_originalPath = null;
            m_originalPathCache = null;
            sensorRatioCache = 0;
            boundingShapePositionCache = Vector3.negativeInfinity;
            boundingShapeScaleCache = Vector3.negativeInfinity;
            boundingShapeRotationCache = new Quaternion(0,0,0,0);
        }

        bool DidBoundingShapeTransformChange()
        {
            return boundingShapePositionCache != m_BoundingShape2D.transform.position ||
                   boundingShapeScaleCache != m_BoundingShape2D.transform.localScale ||
                   boundingShapeRotationCache != m_BoundingShape2D.transform.rotation;
        }

        /// <summary>
        /// Checks if we have a valid path cache. Calculates it if needed.
        /// </summary>
        /// <param name="sensorRatio">CameraWindow ratio (width / height)</param>
        /// <param name="pathChanged">True, if the baked path has changed. False, otherwise.</param>
        /// <returns>True, if path is baked and valid. False, if path is invalid or non-existent.</returns>
        private bool ValidatePathCache(float sensorRatio, out bool pathChanged)
        {
            // TODO: turn this and subsequent functions calls into courotines, to not block ui
            // TODO: before calling this make sure to stop any running courotine 
            // runningCoroutine = StartCoroutine(MyCoroutine());
            // StopCoroutine(runningCoroutine);
            // or async? naw, just set return values as members - this couritne runs alone always

            if (TriggerClearCache)
            {
                InvalidatePathCache();
                TriggerClearCache = false;
            }
            
            pathChanged = false;
            var cacheIsEmpty = confinerStates == null;
            var cacheIsValid = 
                m_originalPath != null && // first time?
                !cacheIsEmpty && // has a prev. baked result?
                !DidBoundingShapeTransformChange() && // confiner was moved or rotated or scaled?
                Math.Abs(sensorRatioCache - sensorRatio) < UnityVectorExtensions.Epsilon && // sensor ratio changed?
                Math.Abs(m_bakedConfinerResolution - bakedConfinerResolutionCache) < UnityVectorExtensions.Epsilon; // resolution changed?
            if (!AutoBake && !TriggerBake)
            {
                if (cacheIsEmpty)
                {
                    BakeProgress = BakeProgressEnum.EMPTY;
                    return false; // if confinerStates is null, then we don't have path -> false
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
                TriggerBake = false;
                BakeProgress = BakeProgressEnum.BAKED;
                return true;
            }
            
            TriggerBake = false;
            BakeProgress = BakeProgressEnum.BAKING;
            pathChanged = true;

            bool boundingShapeTransformChanged = DidBoundingShapeTransformChange();
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

            bakedConfinerResolutionCache = m_bakedConfinerResolution;
            sensorRatioCache = sensorRatio;
            confinerOven().BakeConfiner(m_originalPath, sensorRatioCache, bakedConfinerResolutionCache);
            confinerStates = confinerOven().GetGraphsAsConfinerStates();

            boundingShapePositionCache = m_BoundingShape2D.transform.position;
            boundingShapeRotationCache = m_BoundingShape2D.transform.rotation;
            boundingShapeScaleCache = m_BoundingShape2D.transform.localScale;

            BakeProgress = BakeProgressEnum.BAKED;
            return true;
        }

        private ConfinerStateToPath confinerStateToPath()
        {
            if (_confinerStateConverter == null)
            {
                _confinerStateConverter = new ConfinerStateToPath(gameObject.name);
            }

            return _confinerStateConverter;
        }

        private ConfinerOven confinerOven()
        {
            if (_confinerBaker == null)
            {
                _confinerBaker = new ConfinerOven();
            }

            return _confinerBaker;
        }
        
        void OnDrawGizmosSelected()
        {
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

            if (confinerStates != null && m_BoundingShape2D != null) {
                var index = 0;
                var confinerState = confinerStates[index];
                for (var index1 = 0; index1 < confinerState.graphs.Count; index1++)
                {
                    Gizmos.color = new Color((float) index / (float) confinerStates.Count,
                        (float) index1 / (float) confinerState.graphs.Count, 0.2f);
                    var g = confinerState.graphs[index1];
                    Handles.Label(g.points[0].position, "A=" + g.ComputeSignedArea());
                    for (int i = 0; i < g.points.Count; ++i)
                    {
                        Gizmos.DrawLine(
                            g.points[i].position,
                            g.points[(i + 1) % g.points.Count].position);
                    }
                }
            }
        }
        
        private void OnDrawGizmos()
        {
            if (!DrawGizmosDebug) return;
            if (confinerStates != null && m_BoundingShape2D != null)
            {
                Vector2 offset = Vector2.zero;// m_BoundingShape2D.transform.position;
                for (var index = 0; index < confinerStates.Count; index++)
                {
                    var confinerState = confinerStates[index];
                    for (var index1 = 0; index1 < confinerState.graphs.Count; index1++)
                    {
                        Gizmos.color = new Color((float) index / (float) confinerStates.Count, (float) index1 / (float) confinerState.graphs.Count, 0.2f);
                        var g = confinerState.graphs[index1];
                        //Handles.Label(offset + g.points[0].position, "A="+g.ComputeSignedArea());
                        //Handles.Label(offset + g.points[0].position, "W="+g.windowDiagonal);
                        for (int i = 0; i < g.points.Count; ++i)
                        {
                            Gizmos.DrawLine(offset + g.points[i].position, offset + g.points[(i + 1) % g.points.Count].position);
                        }
                    }
                }

                Gizmos.color = Color.white;
                // for (var index = 0; index < confinerStates.Count; index++)
                {
                    // var confinerState = confinerStates[index];
                    var confinerState = confinerStates[0];
                    foreach (var g in confinerState.graphs)
                    {
                        for (int i = 0; i < g.points.Count; ++i)
                        {
                            Gizmos.DrawLine(offset + g.points[i].position,
                                offset + g.points[i].position + g.points[i].normal);
                        }
                    }
                }
            }
        }
    }
}