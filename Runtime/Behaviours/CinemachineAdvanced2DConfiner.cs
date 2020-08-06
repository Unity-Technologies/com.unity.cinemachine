using System;
using System.Collections.Generic;
using Cinemachine.Utility;
using UnityEditor;
using UnityEngine;

namespace Cinemachine
{ 
    [SaveDuringPlay]
// #if UNITY_2018_3_OR_NEWER
//     [ExecuteAlways]
// #else
//     [ExecuteInEditMode]
// #endif
    public class CinemachineAdvanced2DConfiner : CinemachineExtension
    {
        // TODO: OnValidate parameters (e.g. m_bakedConfinerResolution)
        
        
        /// <summary>The 2D shape within which the camera is to be contained.</summary>
        [Tooltip("The 2D shape within which the camera is to be contained")]
        public Collider2D m_BoundingShape2D;

        [Tooltip("TODO: is it needed? -= Defines the prebaked confiner step resolution. Decrease this, if you feel the confiner is does not change smoothly enough.")]
        [Range(0.005f, 1f)]
        private float m_bakedConfinerResolution = 0.03f;

        public bool DrawGizmosDebug = false;

        private Collider2D m_BoundingCompositeShape2D;
        
        private List<List<Vector2>> m_originalPath;
        private int m_originalPathTotalPointCount;
        
        private float windowSizeCache;
        private List<List<Vector2>> m_currentPathCache;

        private List<List<Graph>> graphs;
        private List<ConfinerState> confinerStates;
        private ConfinerOven _confinerBaker = null;
        private ConfinerStateToPath _confinerStateConverter = null;

        /// <summary>How gradually to return the camera to the bounding volume if it goes beyond the borders</summary>
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
        
        class VcamExtraState
        {
            public Vector3 m_previousDisplacement;
            public float confinerDisplacement;
            public bool applyAfterAim;
        };

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
                    // TODO: what to do?
                    return; // invalid path
                }

                float frustumHeight;
                if (state.Lens.Orthographic)
                {
                    frustumHeight = Mathf.Abs(state.Lens.OrthographicSize);
                }
                else
                {
                    Vector3 objectOfInterest = vcam.Follow != null ? vcam.Follow.position :
                        vcam.LookAt != null ? vcam.LookAt.position :
                        vcam.transform.position + vcam.transform.forward * 10;

                    float distance = (objectOfInterest - vcam.transform.position).magnitude;
                    frustumHeight = 2.0f * distance * Mathf.Tan(state.Lens.FieldOfView * 0.25f * Mathf.Deg2Rad);
                }

                if (m_currentPathCache == null || 
                    m_BoundingCompositeShape2D == null || 
                    pathChanged ||
                    Math.Abs(frustumHeight - windowSizeCache) > m_bakedConfinerResolution)
                {
                    // TODO: Use polygon union operation, once polygon union operation is exposed by unity core
                    windowSizeCache = frustumHeight;
                    confinerCache = confinerOven().GetConfinerAtOrthoSize(windowSizeCache);
                    confinerStateToPath().Convert(confinerCache, m_BoundingShape2D.transform.position,
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

        private float sensorRatioCache;
        private Collider2D m_BoundingShape2DCache;
        private float bakedConfinerResolutionCache;

        private void InvalidatePathCache()
        {
            m_originalPath = null;
            m_BoundingShape2DCache = null;
            sensorRatioCache = 0;
        }

        private bool ValidatePathCache(float sensorRatio, out bool pathChanged)
        {
            if (m_originalPath != null && 
                m_BoundingShape2DCache == m_BoundingShape2D &&
                Math.Abs(sensorRatioCache - sensorRatio) < UnityVectorExtensions.Epsilon &&
                Math.Abs(m_bakedConfinerResolution - bakedConfinerResolutionCache) < UnityVectorExtensions.Epsilon)
            {
                pathChanged = false;
                return true;
            }
            pathChanged = true;


            if (m_originalPath == null || m_BoundingShape2DCache != m_BoundingShape2D)
            {
                Type colliderType = m_BoundingShape2D == null ? null:  m_BoundingShape2D.GetType();
                if (colliderType == typeof(PolygonCollider2D))
                {
                    PolygonCollider2D poly = m_BoundingShape2D as PolygonCollider2D;
                    if (m_originalPath == null || m_originalPath.Count != poly.pathCount || m_originalPathTotalPointCount != poly.GetTotalPointCount())
                    { 
                        m_originalPath = new List<List<Vector2>>();
                        for (int i = 0; i < poly.pathCount; ++i)
                        {
                            Vector2[] path = poly.GetPath(i);
                            List<Vector2> dst = new List<Vector2>();
                            for (int j = 0; j < path.Length; ++j)
                                dst.Add(path[j]);
                            m_originalPath.Add(dst);
                        }
                        m_originalPathTotalPointCount = poly.GetTotalPointCount();
                    }
                }
                else if (colliderType == typeof(CompositeCollider2D))
                {
                    CompositeCollider2D poly = m_BoundingShape2D as CompositeCollider2D;
                    if (m_originalPath == null || m_originalPath.Count != poly.pathCount || m_originalPathTotalPointCount != poly.pointCount)
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
                                dst.Add(path[j] * revertCompositeColliderScale);
                            m_originalPath.Add(dst);
                        }
                        m_originalPathTotalPointCount = poly.pointCount;
                    }
                }
                else
                {
                    InvalidatePathCache();
                    return false;
                }
            }

            bakedConfinerResolutionCache = m_bakedConfinerResolution;
            sensorRatioCache = sensorRatio;
            confinerOven().BakeConfiner(m_originalPath, sensorRatioCache, bakedConfinerResolutionCache);
            confinerStates = confinerOven().TrimGraphs();
            
            m_BoundingShape2DCache = m_BoundingShape2D;

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
            if (m_currentPathCache == null) return;
            
            Gizmos.color = Color.cyan;
            foreach (var path in m_currentPathCache)
            {
                for (var index = 0; index < path.Count; index++)
                {
                    Gizmos.DrawLine(
                        m_BoundingCompositeShape2D.transform.TransformPoint(path[index]), 
                        m_BoundingCompositeShape2D.transform.TransformPoint(path[(index + 1) % path.Count]));
                }
            }

            Vector2 offset = m_BoundingShape2D.transform.position;
            {
                var index = 0;
                var confinerState = confinerStates[index];
                for (var index1 = 0; index1 < confinerState.graphs.Count; index1++)
                {
                    Gizmos.color = new Color((float) index / (float) confinerStates.Count,
                        (float) index1 / (float) confinerState.graphs.Count, 0.2f);
                    var g = confinerState.graphs[index1];
                    Handles.Label(offset + g.points[0].position, "A=" + g.ComputeSignedArea());
                    for (int i = 0; i < g.points.Count; ++i)
                    {
                        Gizmos.DrawLine(offset + g.points[i].position,
                            offset + g.points[(i + 1) % g.points.Count].position);
                    }
                }
            }
        }
        
        private void OnDrawGizmos()
        {
            if (!DrawGizmosDebug) return;
            if (confinerStates != null && m_BoundingShape2D != null)
            {
                Vector2 offset = m_BoundingShape2D.transform.position;
                for (var index = 0; index < confinerStates.Count; index++)
                {
                    var confinerState = confinerStates[index];
                    for (var index1 = 0; index1 < confinerState.graphs.Count; index1++)
                    {
                        Gizmos.color = new Color((float) index / (float) confinerStates.Count, (float) index1 / (float) confinerState.graphs.Count, 0.2f);
                        var g = confinerState.graphs[index1];
                        //Handles.Label(offset + g.points[0].position, "A="+g.ComputeSignedArea());
                        Handles.Label(offset + g.points[0].position, "W="+g.windowDiagonal);
                        for (int i = 0; i < g.points.Count; ++i)
                        {
                            Gizmos.DrawLine(offset + g.points[i].position, offset + g.points[(i + 1) % g.points.Count].position);
                        }
                    }
                }

                Gizmos.color = Color.white;
                foreach (var confinerState in confinerStates)
                {
                    foreach (var g in confinerState.graphs)
                    {
                        for (int i = 0; i < g.points.Count; ++i)
                        {
                            Gizmos.DrawLine(offset + g.points[i].position, offset + g.points[i].position + g.points[i].normal);
                        }
                    }
                }
            }
        }
    }
}