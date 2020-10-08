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
        private bool m_CornerDampingIsOn = false;
        private float m_CornerDampingSpeedup = 1f;
        private float m_CornerAngleTreshold = 10f;

        // advanced features
        public bool m_DrawGizmos = false;
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
                
                float frustumHeight = CalculateFrustumHeight(state, vcam);
                ValidatePathCache(confinerStateChanged, frustumHeight);

                var extra = GetExtraState<VcamExtraState>(vcam);
                Vector3 displacement = ConfinePoint(state.CorrectedPosition);
                if (VirtualCamera.PreviousStateIsValid && deltaTime >= 0)
                { 
                    float displacementAngle = Vector2.Angle(extra.m_previousDisplacement, displacement);
                    if (m_CornerDampingIsOn || m_Damping > 0 && displacementAngle > m_CornerAngleTreshold)
                    {
                        Vector3 delta = displacement - extra.m_previousDisplacement;
                        var deltaDamped = 
                            Damper.Damp(delta, m_Damping / m_CornerDampingSpeedup, deltaTime);
                        displacement = extra.m_previousDisplacement + deltaDamped;

                        m_CornerDampingSpeedup = displacementAngle < 1f ? 2f : 1f;
                        m_CornerDampingIsOn = displacementAngle > UnityVectorExtensions.Epsilon ||
                                              delta.sqrMagnitude > UnityVectorExtensions.Epsilon;
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
        
        private ConfinerOven.ConfinerState m_confinerCache;
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
        
        private void OnDrawGizmos()
        {
            if (!m_DrawGizmos) return;
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
        }
    }
#endif
}