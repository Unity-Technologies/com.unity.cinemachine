#if !UNITY_2019_3_OR_NEWER
#define CINEMACHINE_PHYSICS_2D
#endif

using System;
using System.Collections.Generic;
using Cinemachine.Utility;
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
    /// - MaxOrthoSize parameter in the Confiner changes.
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

        /// <summary>Draws Gizmos for easier fine-tuning.</summary>
        [Tooltip("Draws Input Bounding Shape (black) and Confiner (cyan) for easier fine-tuning.")]
        public bool m_DrawGizmos = true;
        private List<List<Vector2>> m_ConfinerGizmos;
        
        /// <summary>
        /// The confiner will correctly confine up to this maximum orthographic size. If set to 0, then this parameter is ignored and all camera sizes are supported. Use it to optimize computation and memory costs.
        /// </summary>
        [Tooltip("The confiner will correctly confine up to this maximum orthographic size. " +
                 "If set to 0, then this parameter is ignored and all camera sizes are supported. " +
                 "Use it to optimize computation and memory costs.")]
        public float m_MaxOrthoSize;

        internal static readonly float m_bakedConfinerResolution = 0.005f;

        private ConfinerOven m_confinerBaker = null;

        /// <summary>Invalidates cache and consequently trigger a rebake at next iteration.</summary>
        public void InvalidatePathCache()
        {
            m_shapeCache.Invalidate();
        }
        
        private const float CornerAngleTreshold = 10f; // still unsure about the value of this constant
        protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam, 
            CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            if (stage == CinemachineCore.Stage.Body)
            {
                if (!ValidatePathCache(state.Lens.Aspect, out bool confinerStateChanged))
                {
                    return; // invalid path
                }
                
                float frustumHeight = CalculateHalfFrustumHeight(state, vcam);
                var extra = GetExtraState<VcamExtraState>(vcam);
                ValidateVcamPathCache(confinerStateChanged, frustumHeight, state.Lens.Orthographic, extra);

                Vector3 displacement = ConfinePoint(state.CorrectedPosition, extra.m_vcamShapeCache.m_path, 
                    m_shapeCache.m_boundingShape2D);
                // Remember the desired displacement for next frame
                var prev = extra.m_previousDisplacement;
                extra.m_previousDisplacement = displacement;

                if (!VirtualCamera.PreviousStateIsValid || deltaTime < 0 || m_Damping <= 0)
                    extra.m_dampedDisplacement = Vector3.zero;
                else
                {
                    // If a big change from previous frame's desired displacement is detected, 
                    // assume we are going around a corner and extract that difference for damping
                    if (Vector2.Angle(prev, displacement) > CornerAngleTreshold)
                        extra.m_dampedDisplacement += displacement - prev;

                    extra.m_dampedDisplacement -= Damper.Damp(extra.m_dampedDisplacement, m_Damping, deltaTime);
                    displacement -= extra.m_dampedDisplacement;
                }
                state.PositionCorrection += displacement;
            }
        }
        
        private void OnValidate()
        {
            m_Damping = Mathf.Max(0, m_Damping);
            m_MaxOrthoSize = Mathf.Max(0, m_MaxOrthoSize);
        }

        /// <summary>
        /// Calculates half frustum height for orthographic or perspective camera.
        /// For more info on frustum height, see <see cref="docs.unity3d.com/Manual/FrustumSizeAtDistance.html"/> 
        /// </summary>
        /// <param name="state">CameraState for checking if Orthographic or Perspective</param>
        /// <param name="vcam">vcam, to check its position</param>
        /// <returns>Frustum height of the camera</returns>
        private float CalculateHalfFrustumHeight(in CameraState state, in CinemachineVirtualCameraBase vcam)
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
        private Vector2 ConfinePoint(Vector2 positionToConfine, in List<List<Vector2>> pathCache,
            in Collider2D collider2D)
        {
            if (ShrinkablePolygon.IsInside(pathCache, positionToConfine, collider2D))
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
                    Vector2 v0 = collider2D.transform.TransformPoint(pathCache[i][numPoints - 1] + collider2D.offset);
                    for (int j = 0; j < numPoints; ++j)
                    {
                        Vector2 v = collider2D.transform.TransformPoint(pathCache[i][j] + collider2D.offset);
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
            public Vector3 m_dampedDisplacement;
            public VcamShapeCache m_vcamShapeCache;
            
            /// <summary> Contains all the cache items that are dependent on something in the vcam. </summary>
            internal struct VcamShapeCache
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
        };

        /// <summary>
        /// ShapeCache: contains all state that's dependent only on the settings in the confiner.
        /// </summary>
        private struct ShapeCache
        {
            public float m_aspectRatio;
            public float m_maxOrthoSize;

            private Vector3 m_boundingShapeScale;
            private Quaternion m_boundingShapeRotation;
            
            public Collider2D m_boundingShape2D;
            public List<List<Vector2>> m_originalPath;
            public List<ConfinerOven.ConfinerState> m_confinerStates;

            public void Invalidate()
            {
                m_aspectRatio = 0;
                m_maxOrthoSize = 0;
                
                m_boundingShapeScale = Vector3.negativeInfinity;
                m_boundingShapeRotation = new Quaternion(0,0,0,0);
                
                m_boundingShape2D = null;
                m_originalPath = null;

                m_confinerStates = null;
            }

            public bool IsValid(in Collider2D boundingShape2D, 
                in float aspectRatio, in float maxOrthoSize)
            {
                return m_boundingShape2D != null && m_boundingShape2D == boundingShape2D && // same boundingShape?
                       !BoundingShapeTransformChanged(boundingShape2D.transform) && // input shape changed?
                       m_originalPath != null && // first time?
                       m_confinerStates != null && // cache not empty? 
                       Mathf.Abs(m_aspectRatio - aspectRatio) < UnityVectorExtensions.Epsilon && // aspect changed?
                       Mathf.Abs(m_maxOrthoSize - maxOrthoSize) < UnityVectorExtensions.Epsilon; // max ortho changed?
            }

            public void SetTransformCache(in Transform boundingShapeTransform)
            {
                m_boundingShapeScale = boundingShapeTransform.localScale;
                m_boundingShapeRotation = boundingShapeTransform.rotation;
            }
            
            private bool BoundingShapeTransformChanged(in Transform boundingShapeTransform)
            {
                return m_boundingShape2D != null && 
                       (m_boundingShapeScale != boundingShapeTransform.localScale || 
                        m_boundingShapeRotation != boundingShapeTransform.rotation);
            }
        }
        private ShapeCache m_shapeCache;

        /// <summary>
        /// Checks if we have a valid confiner state cache. Calculates cache if it is invalid (outdated or empty).
        /// </summary>
        /// <param name="aspectRatio">Camera window ratio (width / height)</param>
        /// <param name="confinerStateChanged">True, if the baked confiner state has changed. False, otherwise.</param>
        /// <returns>True, if path is baked and valid. False, otherwise.</returns>
        public bool ValidatePathCache(float aspectRatio, out bool confinerStateChanged)
        {
            confinerStateChanged = false;
            if (m_shapeCache.IsValid(m_BoundingShape2D, 
                aspectRatio, m_MaxOrthoSize))
            {
                return true;
            }
            
            m_shapeCache.Invalidate();
            confinerStateChanged = true;
            
            Type colliderType = m_BoundingShape2D == null ? null:  m_BoundingShape2D.GetType();
            if (colliderType == typeof(PolygonCollider2D))
            {
                PolygonCollider2D poly = m_BoundingShape2D as PolygonCollider2D;
                m_shapeCache.m_originalPath = new List<List<Vector2>>();
                for (int i = 0; i < poly.pathCount; ++i)
                {
                    Vector2[] path = poly.GetPath(i);
                    List<Vector2> dst = new List<Vector2>();
                    for (int j = 0; j < path.Length; ++j)
                    {
                        var point = new Vector3(path[j].x, path[j].y, 0);
                        dst.Add(m_BoundingShape2D.transform.rotation * Vector3.Cross(point, m_BoundingShape2D.transform.localScale));
                    }
                    m_shapeCache.m_originalPath.Add(dst);
                }
            }
            else if (colliderType == typeof(CompositeCollider2D))
            {
                CompositeCollider2D poly = m_BoundingShape2D as CompositeCollider2D;
                
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
                        dst.Add(path[j] * revertCompositeColliderScale);
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
                m_MaxOrthoSize, true);
            m_shapeCache.m_confinerStates = GetConfinerOven().GetShrinkablePolygonsAsConfinerStates();

            m_shapeCache.m_aspectRatio = aspectRatio;
            m_shapeCache.m_boundingShape2D = m_BoundingShape2D;
            m_shapeCache.SetTransformCache(m_BoundingShape2D.transform);
            m_shapeCache.m_maxOrthoSize = m_MaxOrthoSize;

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
            InvalidatePathCache();
        }
        
        private void OnDrawGizmos()
        {
            if (!m_DrawGizmos) return;
            if (m_ConfinerGizmos == null || m_shapeCache.m_originalPath == null) return;
            
            // Draw confiner for current camera size
            Gizmos.color = Color.yellow;
            foreach (var path in m_ConfinerGizmos)
            {
                for (var index = 0; index < path.Count; index++)
                {
                    Gizmos.DrawLine(
                        m_shapeCache.m_boundingShape2D.transform.TransformPoint(
                            path[index] + m_shapeCache.m_boundingShape2D.offset), 
                        m_shapeCache.m_boundingShape2D.transform.TransformPoint(
                            path[(index + 1) % path.Count]+ m_shapeCache.m_boundingShape2D.offset));
                }
            }
            
            // Draw input confiner
            Gizmos.color = new Color(1f, 0.92156863f, 0.015686275f, 0.5f); // dimmed yellow
            foreach (var path in m_shapeCache.m_originalPath )
            {
                for (var index = 0; index < path.Count; index++)
                {
                    Gizmos.DrawLine(
                        m_shapeCache.m_boundingShape2D.transform.TransformPoint(
                            path[index] + m_shapeCache.m_boundingShape2D.offset),
                        m_shapeCache.m_boundingShape2D.transform.TransformPoint(
                            path[(index + 1) % path.Count]+ m_shapeCache.m_boundingShape2D.offset));
                }
            }
        }
    }
#endif
}