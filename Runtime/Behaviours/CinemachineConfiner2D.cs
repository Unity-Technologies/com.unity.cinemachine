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
    /// An add-on module for Cinemachine Virtual Camera that post-processes the final position of the virtual camera.
    /// It will confine the virtual camera view window to the area specified in the Bounding Shape 2D field based on
    /// the camera's window size and ratio. The confining area is baked and cached at start.
    ///
    /// 
    /// CinemachineConfiner2D uses a cache to avoid rebaking the confiner unnecessarily.
    /// If the cache is invalid, it will be automatically recomputed at the next usage (lazy evaluation).
    /// The cache is automatically invalidated in some well-defined circumstances:
    /// - Aspect ratio of the parent vcam changes.
    /// - Relevant parameters in the Confiner change (MaxOrthoSize, ShrinkToPointsExperimental).
    /// - Transform of the bounding shape 2D changes.
    /// 
    /// The cache is NOT automatically invalidated (due to high computation cost every frame) if the contents
    /// of the confining shape change (e.g. points get moved dynamically). In that case, we expose an API to
    /// forcibly invalidate the cache so that it gets auto-recomputed next time it's needed.
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
        
        [HideInInspector, SerializeField] internal float m_MaxOrthoSize; // TODO: in editor change name to
                                                                         // maxFrustumHeight and convert between
                                                                         // ortho and perspective bull
        [HideInInspector, SerializeField] internal bool m_ShrinkToPointsExperimental;
        
        private static readonly float m_bakedConfinerResolution = 0.005f;

        private ConfinerOven m_confinerBaker = null;

        /// <summary>Forces rebake at next iteration.</summary>
        public void ForceBake()
        {
            m_shapeCache.Invalidate();
        }

        protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam, 
            CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            if (stage == CinemachineCore.Stage.Body)
            {
                if (!ValidatePathCache(state.Lens.Aspect, out bool confinerStateChanged))
                {
                    return; // invalid path
                }
                
                float frustumHeight = CalculateFrustumHeight(state, vcam);
                var extra = GetExtraState<VcamExtraState>(vcam);
                ValidateVcamPathCache(confinerStateChanged, frustumHeight, state.Lens.Orthographic, extra);

                Vector3 displacement = ConfinePoint(state.CorrectedPosition, extra.m_vcamShapeCache.m_path);
                if (VirtualCamera.PreviousStateIsValid && deltaTime >= 0)
                { 
                    float displacementAngle = Vector2.Angle(extra.m_previousDisplacement, displacement);
                    if (extra.m_cornerDampingIsOn || 
                        (m_Damping > 0 && displacementAngle > m_CornerAngleTreshold))
                    {
                        Vector3 delta = displacement - extra.m_previousDisplacement;
                        var deltaDamped = 
                            Damper.Damp(delta, m_Damping / m_CornerDampingSpeedup, deltaTime);
                        displacement = extra.m_previousDisplacement + deltaDamped;

                        m_CornerDampingSpeedup = displacementAngle < 1f ? 2f : 1f;
                        extra.m_cornerDampingIsOn = displacementAngle > UnityVectorExtensions.Epsilon ||
                                                    delta.sqrMagnitude > UnityVectorExtensions.Epsilon;
                    }
                }
                extra.m_previousDisplacement = displacement;
                state.PositionCorrection += displacement;
            }
        }

        /// <summary>
        /// Calculates Frustum Height for orthographic or perspective camera.
        /// Ascii illustration of Frustum Height:
        ///  |----+----+  -\
        ///  |    |    |    } m_frustumHeight = cameraWindowHeight / 2
        ///  |---------+  -/
        ///  |    |    |
        ///  |---------|
        /// </summary>
        /// <param name="state">CameraState to check if Orthographic or Perspective</param>
        /// <param name="vcam">Vi</param>
        /// <returns>Frustum height of the camera</returns>
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
            public bool m_cornerDampingIsOn;
            public VcamShapeCache m_vcamShapeCache;
        };

        /// <summary>
        /// ShapeCache: contains all state that's dependent only on the settings in the confiner.
        /// </summary>
        private struct ShapeCache
        {
            public float m_aspectRatio;
            public float m_maxOrthoSize;
            public bool m_shrinkToPoints;

            private Vector3 m_boundingShapePosition;
            private Vector3 m_boundingShapeScale;
            private Quaternion m_boundingShapeRotation;
            
            public Collider2D m_boundingShape2D;
            public Vector2 m_boundingShapeOffset;
            public List<List<Vector2>> m_originalPath;
            public List<ConfinerOven.ConfinerState> m_confinerStates;

            public void Invalidate()
            {
                m_aspectRatio = 0;
                m_maxOrthoSize = 0;
                m_shrinkToPoints = false;
                
                m_boundingShapePosition = Vector3.negativeInfinity;
                m_boundingShapeScale = Vector3.negativeInfinity;
                m_boundingShapeRotation = new Quaternion(0,0,0,0);
                
                m_boundingShape2D = null;
                m_originalPath = null;

                m_confinerStates = null;
            }

            public bool IsValid(in Collider2D boundingShape2D, 
                in float aspectRatio, in float maxOrthoSize, in bool shrinkToPoint)
            {
                return m_boundingShape2D == boundingShape2D && // same boundingShape?
                       !BoundingShapeTransformChanged(boundingShape2D.transform) && // input shape changed?
                       m_boundingShapeOffset == boundingShape2D.offset && // same offset on boundingShape?
                       m_originalPath != null && // first time?
                       m_confinerStates != null && // cache not empty? 
                       Mathf.Abs(m_aspectRatio - aspectRatio) < UnityVectorExtensions.Epsilon && // aspect changed?
                       Mathf.Abs(m_maxOrthoSize - maxOrthoSize) < UnityVectorExtensions.Epsilon && // max ortho changed?
                       m_shrinkToPoints == shrinkToPoint; // shrink to point option changed
            }

            public void SetTransformCache(in Transform boundingShapeTransform)
            {
                m_boundingShapePosition = boundingShapeTransform.position;
                m_boundingShapeScale = boundingShapeTransform.localScale;
                m_boundingShapeRotation = boundingShapeTransform.rotation;
            }
            
            private bool BoundingShapeTransformChanged(in Transform boundingShapeTransform)
            {
                return m_boundingShape2D != null && 
                       (m_boundingShapePosition != boundingShapeTransform.position ||
                        m_boundingShapeScale != boundingShapeTransform.localScale ||
                        m_boundingShapeRotation != boundingShapeTransform.rotation);
            }
        }
        private ShapeCache m_shapeCache;

        /// <summary>
        /// VcamShapeCache (lives inside VcamExtraState): contains all the cache items
        /// that are dependent on something in the vcam.
        /// </summary>
        private struct VcamShapeCache
        {
            public float m_frustumHeight;
            public bool m_orthographic;
            public List<List<Vector2>> m_path;

            public bool IsValid(in float frustumHeight, in bool isOrthographic)
            {
                return m_path != null && m_orthographic == isOrthographic &&
                       Math.Abs(frustumHeight - m_frustumHeight) < m_bakedConfinerResolution;
            }
        }

        /// <summary>
        /// Checks if we have a valid confiner state cache. Calculates it if cache is invalid.
        /// </summary>
        /// <param name="aspectRatio">Camera window ratio (width / height)</param>
        /// <param name="confinerStateChanged">True, if the baked confiner state has changed. False, otherwise.</param>
        /// <returns>True, if path is baked and valid. False, otherwise.</returns>
        private bool ValidatePathCache(float aspectRatio, out bool confinerStateChanged)
        {
            confinerStateChanged = false;
            if (m_shapeCache.IsValid(m_BoundingShape2D, 
                aspectRatio, m_MaxOrthoSize, m_ShrinkToPointsExperimental))
            {
                return true;
            }
            
            m_shapeCache.Invalidate();
            confinerStateChanged = true;
            
            Type colliderType = m_BoundingShape2D == null ? null:  m_BoundingShape2D.GetType();
            if (colliderType == typeof(PolygonCollider2D))
            {
                PolygonCollider2D poly = m_BoundingShape2D as PolygonCollider2D;
                Vector2 offset = m_BoundingShape2D.offset * m_BoundingShape2D.transform.localScale;
                
                m_shapeCache.m_originalPath = new List<List<Vector2>>();
                for (int i = 0; i < poly.pathCount; ++i)
                {
                    Vector2[] path = poly.GetPath(i);
                    List<Vector2> dst = new List<Vector2>();
                    for (int j = 0; j < path.Length; ++j)
                    {
                        dst.Add(m_BoundingShape2D.transform.TransformPoint(path[j] + offset));
                    }
                    m_shapeCache.m_originalPath.Add(dst);
                }
            }
            else if (colliderType == typeof(CompositeCollider2D))
            {
                CompositeCollider2D poly = m_BoundingShape2D as CompositeCollider2D;
                Vector2 offset = m_BoundingShape2D.offset * m_BoundingShape2D.transform.localScale;
                
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
                            (path[j] + offset) * revertCompositeColliderScale));
                    }
                    m_shapeCache.m_originalPath.Add(dst);
                }
            }
            else
            {
                m_shapeCache.Invalidate();
                return false; // input collider is invalid
            }

            GetConfinerOven().BakeConfiner(m_shapeCache.m_originalPath, aspectRatio, m_bakedConfinerResolution, 
                m_MaxOrthoSize, m_ShrinkToPointsExperimental);
            m_shapeCache.m_confinerStates = GetConfinerOven().GetShrinkablePolygonsAsConfinerStates();

            m_shapeCache.m_aspectRatio = aspectRatio;
            m_shapeCache.m_boundingShape2D = m_BoundingShape2D;
            m_shapeCache.m_boundingShapeOffset = m_BoundingShape2D.offset;
            m_shapeCache.SetTransformCache(m_BoundingShape2D.transform);
            m_shapeCache.m_maxOrthoSize = m_MaxOrthoSize;
            m_shapeCache.m_shrinkToPoints = m_ShrinkToPointsExperimental;

            return true;
        }

        
        private ConfinerOven.ConfinerState m_confinerCache;
        /// <summary>
        /// Check that the path cache was converted from the current confiner cache, or
        /// converts it if the frustum height was changed.
        /// </summary>
        /// <param name="confinerStateChanged">Confiner cache was changed</param>
        /// <param name="frustumHeight">Camera frustum height</param>
        private void ValidateVcamPathCache(
            in bool confinerStateChanged, in float frustumHeight, in bool orthographic, in VcamExtraState extra)
        {
            if (!confinerStateChanged && extra.m_vcamShapeCache.IsValid(frustumHeight, orthographic))
            {
                return;
            }
            
            m_confinerCache = GetConfinerOven().GetConfinerAtFrustumHeight(frustumHeight);
            ShrinkablePolygon.ConvertToPath(m_confinerCache.polygons, frustumHeight, 
                out extra.m_vcamShapeCache.m_path);
                
            extra.m_vcamShapeCache.m_frustumHeight = frustumHeight;
            extra.m_vcamShapeCache.m_orthographic = orthographic;
                
            if (m_DrawGizmos)
            {
                m_ConfinerGizmos = extra.m_vcamShapeCache.m_path;
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