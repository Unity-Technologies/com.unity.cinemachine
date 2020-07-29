using System;
using System.Collections.Generic;
using Cinemachine.Utility;
using UnityEditor.Graphs;
using UnityEngine;

namespace Cinemachine
{
    public class CinemachineAdvanced2DConfiner : CinemachineExtension
    {
        /// <summary>The 2D shape within which the camera is to be contained.</summary>
        [Tooltip("The 2D shape within which the camera is to be contained")]
        public Collider2D m_BoundingShape2D;
        private Collider2D m_BoundingShape2DCache;
        private List<List<Vector2>> m_originalPathCache;
        private int m_originalPathTotalPointCount;
        
        private List<GraphToCompositeCollider.FovBakedConfiners> fovConfiners;
        private float currentFOV;
        private List<List<Vector2>> m_currentPathCache;

        private List<List<Graph>> graphs;
        private List<ConfinerState> confinerStates;
        private ConfinerOven confinerBaker = null;
        private GraphToCompositeCollider graphToCompositeCollider = null;
        
        /// <summary>How gradually to return the camera to the bounding volume if it goes beyond the borders</summary>
        [Tooltip("How gradually to return the camera to the bounding volume if it goes beyond the borders.  "
                 + "Higher numbers are more gradual.")]
        [Range(0, 10)]
        public float m_Damping = 0;

        [Tooltip("Damping applied automatically around corners to avoid jumps.  "
                 + "Higher numbers produce more smooth cornering.")]
        [Range(0, 10)]
        public float m_CornerDamping = 0;

        [Tooltip("After going through the corner should the camera return smoothly or snap?")]
        public bool m_SnapFromCorner = true;
        private float m_CornerAngleTreshold = 10f;
        private bool m_Cornerring = false;
        
        class VcamExtraState
        {
            public Vector3 m_previousDisplacement;
            public float confinerDisplacement;
            public bool applyAfterAim;
        };

        protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam, 
            CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            var extra = GetExtraState<VcamExtraState>(vcam);
            if ((extra.applyAfterAim && stage == CinemachineCore.Stage.Finalize)
                ||
                (!extra.applyAfterAim && stage == CinemachineCore.Stage.Body))
            {
                ValidatePathCache();
                
                var stateLensFieldOfView = Mathf.Abs(state.Lens.FieldOfView);
                // TODO: float comparison granulity
                if (Math.Abs(stateLensFieldOfView - currentFOV) > UnityVectorExtensions.Epsilon)
                {
                    currentFOV = stateLensFieldOfView;
                    for (int i = 0; i < fovConfiners.Count; ++i)
                    {
                        if (fovConfiners[i].fov <= currentFOV)
                        {
                            if (i == fovConfiners.Count)
                            {
                                m_currentPathCache = fovConfiners[i].path;
                            }
                            else if (i % 2 == 0)
                            {
                                m_currentPathCache = 
                                    PathLerp(fovConfiners[i].path, fovConfiners[i+1].path, 
                                    Mathf.InverseLerp(fovConfiners[i].fov, fovConfiners[i + 1].fov, currentFOV));
                            }
                            else
                            {
                                m_currentPathCache = 
                                    Mathf.Abs(fovConfiners[i].fov - currentFOV) < 
                                    Mathf.Abs(fovConfiners[i + 1].fov - currentFOV) ? 
                                    fovConfiners[i].path : 
                                    fovConfiners[i+1].path;
                            }
                            
                            break;
                        }
                    }
                    
                }
                Vector3 displacement = ConfinePoint(state.CorrectedPosition);
                
                if (VirtualCamera.PreviousStateIsValid && deltaTime >= 0)
                { 
                    var displacementAngle = Vector2.Angle(extra.m_previousDisplacement, displacement);
                    if (m_CornerDamping > 0 && (m_Cornerring || displacementAngle > m_CornerAngleTreshold))
                    {
                        if (!m_SnapFromCorner) {
                            m_Cornerring = displacementAngle > 1f;
                        }
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
                }
                
                extra.m_previousDisplacement = displacement;
                state.PositionCorrection += displacement;
                extra.confinerDisplacement = displacement.magnitude;
            }
        }

        private List<List<Vector2>> PathLerp(in List<List<Vector2>> left, in List<List<Vector2>> right, float lerp)
        {
            if (left.Count != right.Count)
            {
                Debug.Log("SOMETHINGS NOT RIGHT 1 - PathLerp");
                return left;
            }
            for (int i = 0; i < left.Count; ++i)
            {
                if (left[i].Count != right[i].Count)
                {
                    Debug.Log("SOMETHINGS NOT RIGHT 2 - PathLerp");
                    return left;
                }
            }

            List<List<Vector2>> result = new List<List<Vector2>>(left.Count);
            for (int i = 0; i < left.Count; ++i)
            {
                var r = new List<Vector2>(left[i].Count);
                for (int j = 0; j < left[i].Count; ++j)
                {
                    r.Add(Vector2.Lerp(left[i][j], right[i][j], lerp));
                }
                result.Add(r);
            }
            return result;
        }
        
         
        private void Start()
        {
            graphToCompositeCollider = new GraphToCompositeCollider(this.transform);
        }
        
        private Vector3 ConfinePoint(Vector3 camPos)
        {
            // 2D version
            Vector2 p = camPos;
            Vector2 closest = p;
            if (m_BoundingShape2D.OverlapPoint(camPos))
                return Vector3.zero;
            // Find the nearest point on the shape's boundary
            if (!ValidatePathCache())
                return Vector3.zero;

            float bestDistance = float.MaxValue;
            for (int i = 0; i < m_currentPathCache.Count; ++i)
            {
                int numPoints = m_currentPathCache[i].Count;
                if (numPoints > 0)
                {
                    Vector2 v0 = m_BoundingShape2D.transform.TransformPoint(m_currentPathCache[i][numPoints - 1]);
                    for (int j = 0; j < numPoints; ++j)
                    {
                        Vector2 v = m_BoundingShape2D.transform.TransformPoint(m_currentPathCache[i][j]);
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
        
        public void InvalidatePathCache()
        {
            m_originalPathCache = null;
            m_BoundingShape2DCache = null;
        }

        bool ValidatePathCache()
        {
            // if (m_BoundingShape2DCache == m_BoundingShape2D)
            // {
            //     return true;
            // }
            
            InvalidatePathCache();
            m_BoundingShape2DCache = m_BoundingShape2D;
            
            Type colliderType = m_BoundingShape2D == null ? null:  m_BoundingShape2D.GetType();
            if (colliderType == typeof(PolygonCollider2D))
            {
                PolygonCollider2D poly = m_BoundingShape2D as PolygonCollider2D;
                if (m_originalPathCache == null || m_originalPathCache.Count != poly.pathCount || m_originalPathTotalPointCount != poly.GetTotalPointCount())
                {
                    m_originalPathCache = new List<List<Vector2>>();
                    for (int i = 0; i < poly.pathCount; ++i)
                    {
                        Vector2[] path = poly.GetPath(i);
                        List<Vector2> dst = new List<Vector2>();
                        for (int j = 0; j < path.Length; ++j)
                            dst.Add(path[j]);
                        m_originalPathCache.Add(dst);
                    }
                    m_originalPathTotalPointCount = poly.GetTotalPointCount();
                }
            }
            else if (colliderType == typeof(CompositeCollider2D))
            {
                CompositeCollider2D poly = m_BoundingShape2D as CompositeCollider2D;
                if (m_originalPathCache == null || m_originalPathCache.Count != poly.pathCount || m_originalPathTotalPointCount != poly.pointCount)
                {
                    m_originalPathCache = new List<List<Vector2>>();
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
                        m_originalPathCache.Add(dst);
                    }
                    m_originalPathTotalPointCount = poly.pointCount;
                }
            }
            else
            {
                InvalidatePathCache();
                return false;
            }

            
            var sensorRatio = 1f; // TODO:
            if (confinerBaker == null)
            {
                confinerBaker = new ConfinerOven();
            }
            confinerBaker.BakeConfiner(m_originalPathCache, sensorRatio);
            if (graphToCompositeCollider == null)
            {
                graphToCompositeCollider = new GraphToCompositeCollider(this.transform);
            }
            graphToCompositeCollider.Convert(confinerBaker.GetStateGraphs(), Vector2.zero);
            fovConfiners = graphToCompositeCollider.GetBakedConfiners();
            m_currentPathCache = new List<List<Vector2>>();
            return true;
        }
    }
}