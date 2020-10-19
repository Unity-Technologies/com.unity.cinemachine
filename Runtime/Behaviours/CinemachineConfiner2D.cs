#if !UNITY_2019_3_OR_NEWER
#define CINEMACHINE_PHYSICS_2D
#endif

using System;
using System.Collections.Generic;
using Cinemachine.Utility;
using UnityEditor;
using UnityEngine;

namespace Cinemachine
{
#if CINEMACHINE_PHYSICS_2D
    /// <summary>
    /// An add-on module for Cinemachine Virtual Camera that post-processes
    /// the final position of the virtual camera. It will confine the virtual
    /// camera view window to the area specified in the Bounding Shape 2D field
    /// based on the camera's window size and ratio.
    /// The confining area is baked and cached at start.
    /// </summary>
    [SaveDuringPlay, ExecuteAlways]
    public class CinemachineConfiner2D : CinemachineExtension
    {
        /// <summary>The 2D shape within which the camera is to be contained.</summary>
        [Tooltip("The 2D shape within which the camera is to be contained.  " +
                 "Can be a 2D polygon or 2D composite collider.")]
        public Collider2D m_BoundingShape2D;

        /// <summary>Damping applied automatically around corners to avoid jumps.</summary>
        [Tooltip("Damping applied around corners to avoid jumps.  Higher numbers are more gradual.")]
        [Range(0, 5)]
        public float m_Damping = 0;
        private float m_CornerDampingSpeedup = 1f;
        private float m_CornerAngleTreshold = 10f;

        /// <summary>Draws Gizmos for easier fine-tuning.</summary>
        [Tooltip("Draws Input Bounding Shape (black) and Confiner (cyan) for easier fine-tuning.")]
        public bool m_DrawGizmos = true;
        private List<List<Vector2>> m_ConfinerGizmos;
        
        
        [HideInInspector, SerializeField] internal bool m_AutoBake = true; // TODO: remove
                                                                           // reason: if user wants to
                                                                           // switch between cameras, it is better
                                                                           // to just have a different 2D confiner
                                                                           // for each setup
        [HideInInspector, SerializeField] internal bool m_TriggerBake = false;
        [HideInInspector, SerializeField] internal bool m_TriggerClearCache = false;
        [HideInInspector, SerializeField] internal float m_MaxOrthoSize;
        [HideInInspector, SerializeField] internal bool m_ShrinkToPointsExperimental;
        
        private static readonly float m_bakedConfinerResolution = 0.005f;
        
        internal enum BakeProgressEnum { EMPTY, BAKING, BAKED, INVALID_CACHE } // TODO: remove states after
                                                                               // fist pass cleanup!
        [HideInInspector, SerializeField] internal BakeProgressEnum BakeProgress = BakeProgressEnum.INVALID_CACHE;

        

        private List<ConfinerOven.ConfinerState> m_confinerStates;
        private ConfinerOven m_confinerBaker = null;

        /// <summary>Force rebake manually. This function invalidates the cache and rebakes the confiner.</summary>
        public void ForceBake()
        {
            InvalidatePathCache();
            Bake();
        }
        
        /// <summary>
        /// Trigger rebake process manually.
        /// The confiner rebakes iff an input parameters affecting the outcome of the baked result change.
        /// </summary>
        private void Bake()
        {
            m_TriggerBake = true;
        }

        protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam, 
            CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            if (stage == CinemachineCore.Stage.Body)
            {
                if (!ValidateConfinerStateCache(state.Lens.Aspect, out bool confinerStateChanged))
                {
                    return; // invalid path
                }
                
                var extra = GetExtraState<VcamExtraState>(vcam);
                
                float frustumHeight = CalculateFrustumHeight(state, vcam);
                ValidatePathCache(confinerStateChanged, frustumHeight, extra);

                Vector3 displacement = ConfinePoint(state.CorrectedPosition, extra.m_VcamShapeCache.m_path);
                if (VirtualCamera.PreviousStateIsValid && deltaTime >= 0)
                { 
                    float displacementAngle = Vector2.Angle(extra.m_previousDisplacement, displacement);
                    if (extra.m_CornerDampingIsOn || 
                        (m_Damping > 0 && displacementAngle > m_CornerAngleTreshold))
                    {
                        Vector3 delta = displacement - extra.m_previousDisplacement;
                        var deltaDamped = 
                            Damper.Damp(delta, m_Damping / m_CornerDampingSpeedup, deltaTime);
                        displacement = extra.m_previousDisplacement + deltaDamped;

                        m_CornerDampingSpeedup = displacementAngle < 1f ? 2f : 1f;
                        extra.m_CornerDampingIsOn = displacementAngle > UnityVectorExtensions.Epsilon ||
                                                    delta.sqrMagnitude > UnityVectorExtensions.Epsilon;
                    }
                }
                extra.m_previousDisplacement = displacement;
                state.PositionCorrection += displacement;
            }
        }

        // <summary>
        /// Calculates Frustum Height for orthographic or perspective camera.
        /// Ascii illustration of Frustum Height:
        ///  |----+----+  -\
        ///  |    |    |    } m_frustumHeight = cameraWindowHeight / 2
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
        
        /// <summary>
        /// Confines input 2D point within the confined area.
        /// </summary>
        /// <param name="positionToConfine">2D point to confine</param>
        /// <returns>Confined position</returns>
        private Vector2 ConfinePoint(in Vector2 positionToConfine, in List<List<Vector2>> pathCache)
        {
            if (ShrinkablePolygon.IsInside(pathCache, positionToConfine))
            {
                return Vector2.zero;
            }

            Vector2 closest = positionToConfine;
            float minDistance = float.MaxValue;
            for (int i = 0; i < pathCache.Count; ++i)
            {
                int numPoints = pathCache[i].Count;
                if (numPoints > 0)
                {
                    Vector2 v0 = pathCache[i][numPoints - 1];
                    for (int j = 0; j < numPoints; ++j)
                    {
                        Vector2 v = pathCache[i][j];
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
            public bool m_CornerDampingIsOn;
            public VcamShapeCache m_VcamShapeCache;
        };

        /// <summary>
        /// ShapeCache: contains all state that's dependent only on the settings in the confiner: bounding shape,
        /// shrinkToPoint, maxOrthoSize. Contains nothing that is dependent on anything in the vcam itself
        /// (except maybe aspect ratio, which we can assume to be constant among vcam children).
        /// </summary>
        private struct ShapeCache
        {
            public float m_aspectRatio;
            public Vector3 m_boundingShapePosition;
            public Vector3 m_boundingShapeScale;
            public Quaternion m_boundingShapeRotation;
            
            public Collider2D m_boundingShape2D;
            public List<List<Vector2>> m_originalPath;
            public int m_originalPathTotalPointCount;

            public void Invalidate()
            {
                m_aspectRatio = 0;
                m_boundingShapePosition = Vector3.negativeInfinity;
                m_boundingShapeScale = Vector3.negativeInfinity;
                m_boundingShapeRotation = new Quaternion(0,0,0,0);
                
                m_boundingShape2D = null;
                m_originalPath = null;
                m_originalPathTotalPointCount = 0;
            }

            public void SetTransformCache(in Transform boundingShapeTransform)
            {
                m_boundingShapePosition = boundingShapeTransform.position;
                m_boundingShapeScale = boundingShapeTransform.localScale;
                m_boundingShapeRotation = boundingShapeTransform.rotation;
            }
            
            public bool BoundingShapeTransformChanged(in Transform boundingShapeTransform)
            {
                return m_boundingShape2D != null && 
                       (m_boundingShapePosition != boundingShapeTransform.position ||
                        m_boundingShapeScale != boundingShapeTransform.localScale ||
                        m_boundingShapeRotation != boundingShapeTransform.rotation);
            }
        }
        private ShapeCache m_shapeCache;

        /// <summary>
        /// VcamShapeCache (lives inside VcamExtraState): contains all the cache items that are dependent on
        /// something in the vcam (e.g. orthoSize).
        /// </summary>
        private struct VcamShapeCache
        {
            public float m_frustumHeight;
            public List<List<Vector2>> m_path;
        }

        /// <summary>
        /// Invalidates path cache.
        /// </summary>
        private void InvalidatePathCache()
        {
            m_shapeCache.Invalidate();
        }
        
        /// <summary>
        /// Checks if we have a valid confiner state cache. Calculates it if cache is invalid, and bake was requested.
        /// </summary>
        /// <param name="aspectRatio">Camera window ratio (width / height)</param>
        /// <param name="confinerStateChanged">True, if the baked confiner state has changed. False, otherwise.</param>
        /// <returns>True, if path is baked and valid. False, if path is invalid or non-existent.</returns>
        private bool ValidateConfinerStateCache(float aspectRatio, out bool confinerStateChanged)
        {
            if (m_TriggerClearCache)
            {
                InvalidatePathCache();
                m_TriggerClearCache = false;
            }
            
            confinerStateChanged = false;
            bool cacheIsEmpty = m_confinerStates == null;
            bool cacheIsValid =
                m_shapeCache.m_originalPath != null && // first time?
                !cacheIsEmpty && // has a prev. baked result?
                !m_shapeCache.BoundingShapeTransformChanged(m_BoundingShape2D.transform) && // confiner was moved or rotated or scaled?
                Math.Abs(m_shapeCache.m_aspectRatio - aspectRatio) < UnityVectorExtensions.Epsilon; // sensor ratio changed?
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

            bool boundingShapeTransformChanged = m_shapeCache.BoundingShapeTransformChanged(m_BoundingShape2D.transform);
            if (boundingShapeTransformChanged || m_shapeCache.m_originalPath == null)
            {
                Type colliderType = m_BoundingShape2D == null ? null:  m_BoundingShape2D.GetType();
                if (colliderType == typeof(PolygonCollider2D))
                {
                    PolygonCollider2D poly = m_BoundingShape2D as PolygonCollider2D;
                    if (boundingShapeTransformChanged || m_shapeCache.m_originalPath == null || 
                        m_shapeCache.m_originalPath.Count != poly.pathCount || 
                        m_shapeCache.m_originalPathTotalPointCount != poly.GetTotalPointCount())
                    { 
                        m_shapeCache.m_originalPath = new List<List<Vector2>>();
                        for (int i = 0; i < poly.pathCount; ++i)
                        {
                            Vector2[] path = poly.GetPath(i);
                            List<Vector2> dst = new List<Vector2>();
                            for (int j = 0; j < path.Length; ++j)
                            {
                                dst.Add(m_BoundingShape2D.transform.TransformPoint(path[j]));
                            }
                            m_shapeCache.m_originalPath.Add(dst);
                        }
                        m_shapeCache.m_originalPathTotalPointCount = poly.GetTotalPointCount();
                    }
                }
                else if (colliderType == typeof(CompositeCollider2D))
                {
                    CompositeCollider2D poly = m_BoundingShape2D as CompositeCollider2D;
                    if (boundingShapeTransformChanged || m_shapeCache.m_originalPath == null || 
                        m_shapeCache.m_originalPath.Count != poly.pathCount || m_shapeCache.m_originalPathTotalPointCount != poly.pointCount)
                    {
                        m_shapeCache.m_originalPath = new List<List<Vector2>>();
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
                            m_shapeCache.m_originalPath.Add(dst);
                        }
                        m_shapeCache.m_originalPathTotalPointCount = poly.pointCount;
                    }
                }
                else
                {
                    BakeProgress = BakeProgressEnum.INVALID_CACHE;
                    InvalidatePathCache();
                    return false; // input collider is invalid
                }
            }

            GetConfinerOven().BakeConfiner(m_shapeCache.m_originalPath, aspectRatio, m_bakedConfinerResolution, 
                m_MaxOrthoSize, m_ShrinkToPointsExperimental);
            m_confinerStates = GetConfinerOven().GetShrinkablePolygonsAsConfinerStates();

            m_shapeCache.m_aspectRatio = aspectRatio;
            m_shapeCache.m_boundingShape2D = m_BoundingShape2D;
            m_shapeCache.SetTransformCache(m_BoundingShape2D.transform);

            BakeProgress = BakeProgressEnum.BAKED;
            return true;
        }

        
        private ConfinerOven.ConfinerState m_confinerCache;
        /// <summary>
        /// Check that the path cache was converted from the current confiner cache, or
        /// converts it if the frustum height was changed.
        /// </summary>
        /// <param name="confinerStateChanged">Confiner cache was changed</param>
        /// <param name="frustumHeight">Camera frustum height</param>
        private void ValidatePathCache(in bool confinerStateChanged, in float frustumHeight, in VcamExtraState extra)
        {
            if (confinerStateChanged ||
                extra.m_VcamShapeCache.m_path == null || 
                Math.Abs(frustumHeight - extra.m_VcamShapeCache.m_frustumHeight) > m_bakedConfinerResolution)
            {
                m_confinerCache = GetConfinerOven().GetConfinerAtFrustumHeight(frustumHeight);
                ShrinkablePolygon.ConvertToPath(m_confinerCache.polygons, frustumHeight, 
                    out extra.m_VcamShapeCache.m_path);
                
                extra.m_VcamShapeCache.m_frustumHeight = frustumHeight;
                
                if (m_DrawGizmos)
                {
                    m_ConfinerGizmos = extra.m_VcamShapeCache.m_path;
                }
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
        
        private void OnDrawGizmos()
        {
            if (!m_DrawGizmos) return;
            if (m_ConfinerGizmos == null || m_shapeCache.m_originalPath == null) return;
            
            // Draw confiner for current camera size
            Gizmos.color = Color.cyan;
            foreach (var path in m_ConfinerGizmos)
            {
                for (var index = 0; index < path.Count; index++)
                {
                    Gizmos.DrawLine(
                        path[index], 
                        path[(index + 1) % path.Count]);
                }
            }
            
            // Draw input confiner
            Gizmos.color = Color.black;
            foreach (var path in m_shapeCache.m_originalPath )
            {
                for (var index = 0; index < path.Count; index++)
                {
                    Gizmos.DrawLine(
                        path[index], 
                        path[(index + 1) % path.Count]);
                }
            }
        }
    }
#endif
}