using System;
using System.Collections.Generic;
using Cinemachine.Utility;
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

        [Tooltip("TODO: is it needed? -= Defines the prebaked confiner step resolution. Decrease this, if you feel the confiner is does not change smoothly enough.")]
        public float m_bakedConfinerResolution = 0.03f;

        private Collider2D m_BoundingCompositeShape2D;
        
        private List<List<Vector2>> m_originalPath;
        private int m_originalPathTotalPointCount;
        
        private float currentOrthographicSize;
        private List<List<Vector2>> m_currentPathCache;

        private List<List<Graph>> graphs;
        private List<ConfinerState> confinerStates;
        private ConfinerOven _confinerBaker = null;
        private ConfinerStateToPath _confinerStateConverter = null;

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
        private float m_DampingStopper;
        
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

                if (!ValidatePathCache(state.Lens.SensorSize.x / state.Lens.SensorSize.y))
                {
                    return; // invalid path
                }
                
                var stateLensOrthographicSize = Mathf.Abs(state.Lens.OrthographicSize);
                if (Math.Abs(stateLensOrthographicSize - currentOrthographicSize) >
                    m_bakedConfinerResolution)
                {
                    currentOrthographicSize = stateLensOrthographicSize;
                    confinerCache = confinerOven().GetConfinerAtOrthoSize(currentOrthographicSize);
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
        public void InvalidatePathCache()
        {
            m_originalPath = null;
            m_BoundingShape2DCache = null;
            sensorRatioCache = 0;
        }

        bool ValidatePathCache(float sensorRatio)
        {
            if (m_originalPath != null && m_BoundingShape2DCache == m_BoundingShape2D &&
                Math.Abs(sensorRatioCache - sensorRatio) < UnityVectorExtensions.Epsilon &&
                Math.Abs(m_bakedConfinerResolution - bakedConfinerResolutionCache) < UnityVectorExtensions.Epsilon)
            {
                return true;
            }

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
            
            confinerOven().BakeConfiner(m_originalPath, sensorRatio, m_bakedConfinerResolution);
            confinerOven().TrimGraphs();
            
            sensorRatioCache = sensorRatio;
            m_BoundingShape2DCache = m_BoundingShape2D;

            return true;
        }

        void OnDrawGizmosSelected()
        {
            if (m_currentPathCache == null) return;
            
            Gizmos.color = Color.red;
            foreach (var path in m_currentPathCache)
            {
                for (var index = 0; index < path.Count; index++)
                {
                    Gizmos.DrawLine(
                        m_BoundingCompositeShape2D.transform.TransformPoint(path[index]), 
                        m_BoundingCompositeShape2D.transform.TransformPoint(path[(index + 1) % path.Count]));
                }
            }
        }
    }
}