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
    /// CinemachineConfiner2D uses a cache to avoid recalculating the confiner unnecessarily.
    /// If the cache is invalid, it will be automatically recomputed at the next usage (lazy evaluation).
    /// The cache is automatically invalidated in some well-defined circumstances:
    /// - Aspect ratio of the parent vcam changes.
    /// - MaxOrthoSize parameter in the Confiner changes.
    ///
    /// The user can invalidate the cache manually by calling InvalidatePathCache() function or clicking the
    /// InvalidatePathCache button in the editor on the component.
    ///
    /// Collider's Transform changes are supported, but after changing the Rotation component the cache is going to be
    /// invalid, and the user needs to invalidate it if they want to have a correct cache. If the collider is rotated,
    /// non-uniform scale will distort the confiner.
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
        
        /// <summary>
        /// The confiner will correctly confine up to this maximum orthographic size. If set to 0, then this parameter is ignored and all camera sizes are supported. Use it to optimize computation and memory costs.
        /// </summary>
        [Tooltip("The confiner will correctly confine up to this maximum orthographic size. " +
                 "If set to 0, then this parameter is ignored and all camera sizes are supported. " +
                 "Use it to optimize computation and memory costs.")]
        public float m_MaxOrthoSize;
        
        /// <summary>Invalidates cache and consequently trigger a rebake at next iteration.</summary>
        public void InvalidatePathCache()
        {
            m_shapeCache.Invalidate();
        }

        /// <summary>Validates cache if it is invalid.</summary>
        /// <param name="cameraAspectRatio">Aspect ratio of camera.</param>
        /// <returns>Return true if cache is valid. False, otherwise.</returns>
        public bool ValidatePathCache(float cameraAspectRatio)
        {
            return m_shapeCache.ValidateCache(
                m_BoundingShape2D, m_MaxOrthoSize, m_confinerBaker, cameraAspectRatio, out _);
        }
        
        private ConfinerOven m_confinerBaker = new ConfinerOven();
        private VcamExtraState m_extra;
        private const float m_cornerAngleTreshold = 10f; // still unsure about the value of this constant
        protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam, 
            CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            if (stage == CinemachineCore.Stage.Body)
            {
                if (!m_shapeCache.ValidateCache(m_BoundingShape2D, m_MaxOrthoSize, m_confinerBaker, 
                    state.Lens.Aspect, out bool confinerStateChanged))
                {
                    return; // invalid path
                }
                
                float frustumHeight = CalculateHalfFrustumHeight(state, vcam);
                m_extra = GetExtraState<VcamExtraState>(vcam);
                m_extra.m_vcamShapeCache.ValidateCache(m_confinerBaker, confinerStateChanged, frustumHeight, state.Lens.Orthographic, m_extra);

                Vector3 displacement = ConfinePoint(state.CorrectedPosition, m_extra.m_vcamShapeCache.m_path, 
                    m_shapeCache, Vector3.zero);
                // Remember the desired displacement for next frame
                var prev = m_extra.m_previousDisplacement;
                m_extra.m_previousDisplacement = displacement;

                if (!VirtualCamera.PreviousStateIsValid || deltaTime < 0 || m_Damping <= 0)
                    m_extra.m_dampedDisplacement = Vector3.zero;
                else
                {
                    // If a big change from previous frame's desired displacement is detected, 
                    // assume we are going around a corner and extract that difference for damping
                    if (Vector2.Angle(prev, displacement) > m_cornerAngleTreshold)
                        m_extra.m_dampedDisplacement += displacement - prev;

                    m_extra.m_dampedDisplacement -= Damper.Damp(m_extra.m_dampedDisplacement, m_Damping, deltaTime);
                    displacement -= m_extra.m_dampedDisplacement;
                }
                state.PositionCorrection += displacement;
            }
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
            in ShapeCache shapeCache, in Vector2 offset)
        {
            if (ShrinkablePolygon.IsInside(pathCache, positionToConfine, shapeCache.m_scaleDelta, 
                shapeCache.m_rotationDelta, shapeCache.m_positionDelta, offset))
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
                    Vector2 v0 = shapeCache.ApplyTransformationDelta(pathCache[i][numPoints - 1] + offset);
                    for (int j = 0; j < numPoints; ++j)
                    {
                        Vector2 v = shapeCache.ApplyTransformationDelta(pathCache[i][j] + offset);
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
        
        internal static readonly float m_bakedConfinerResolution = 0.005f;
        private class VcamExtraState
        {
            public Vector3 m_previousDisplacement;
            public Vector3 m_dampedDisplacement;
            public VcamShapeCache m_vcamShapeCache;
            
            /// <summary> Contains all the cache items that are dependent on something in the vcam. </summary>
            internal struct VcamShapeCache
            {
                public List<List<Vector2>> m_path;
                
                private float m_frustumHeight;
                private bool m_orthographic;

                /// <summary>
                /// Check that the path cache was converted from the current confiner cache, or
                /// converts it if the frustum height was changed.
                /// </summary>
                /// <param name="confinerStateChanged">Confiner cache was changed</param>
                /// <param name="frustumHeight">Camera frustum height</param>
                public void ValidateCache(
                    in ConfinerOven confinerBaker, in bool confinerStateChanged, 
                    in float frustumHeight, in bool orthographic, 
                    VcamExtraState extra)
                {
                    if (!confinerStateChanged && extra.m_vcamShapeCache.IsValid(frustumHeight, orthographic))
                    {
                        return;
                    }
            
                    var confinerCache = confinerBaker.GetConfinerAtFrustumHeight(frustumHeight);
                    ShrinkablePolygon.ConvertToPath(confinerCache.polygons, frustumHeight, 
                        out extra.m_vcamShapeCache.m_path);
                
                    extra.m_vcamShapeCache.m_frustumHeight = frustumHeight;
                    extra.m_vcamShapeCache.m_orthographic = orthographic;
                }
                
                private bool IsValid(in float frustumHeight, in bool isOrthographic)
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
            public Vector3 m_positionDelta;
            public Vector3 m_scaleDelta;
            public Quaternion m_rotationDelta;
            public List<List<Vector2>> m_originalPath;
            
            private float m_aspectRatio;
            private float m_maxOrthoSize;
            private Vector3 m_boundingShapeScale;
            private Quaternion m_boundingShapeRotation;

            private Collider2D m_boundingShape2D;
            private List<ConfinerOven.ConfinerState> m_confinerStates;

            /// <summary>
            /// Invalidates shapeCache
            /// </summary>
            public void Invalidate()
            {
                m_aspectRatio = 0;
                m_maxOrthoSize = 0;
                
                m_boundingShapeScale = Vector3.one;
                m_boundingShapeRotation = Quaternion.identity;
                
                m_boundingShape2D = null;
                m_originalPath = null;

                m_confinerStates = null;
            }

            public Vector3 ApplyTransformationDelta(in Vector3 point)
            {
                var transformedPoint = new Vector3(point.x * m_scaleDelta.x, point.y * m_scaleDelta.y,
                    point.z * m_scaleDelta.z);
                transformedPoint = m_rotationDelta * transformedPoint;
                transformedPoint += m_positionDelta;
                return transformedPoint;
            }
            
            /// <summary>
            /// Checks if we have a valid confiner state cache. Calculates cache if it is invalid (outdated or empty).
            /// </summary>
            /// <param name="aspectRatio">Camera window ratio (width / height)</param>
            /// <param name="confinerStateChanged">True, if the baked confiner state has changed. False, otherwise.</param>
            /// <returns>True, if path is baked and valid. False, otherwise.</returns>
            public bool ValidateCache(Collider2D boundingShape2D, float maxOrthoSize, ConfinerOven confinerBaker,
                 float aspectRatio, out bool confinerStateChanged)
            {
                confinerStateChanged = false;
                if (IsValid(boundingShape2D, 
                    aspectRatio, maxOrthoSize))
                {
                    m_boundingShape2D = boundingShape2D;
                    SetLocalToWorldDelta();
                    return true;
                }
                
                Invalidate();
                confinerStateChanged = true;
                
                Type colliderType = boundingShape2D == null ? null:  boundingShape2D.GetType();
                if (colliderType == typeof(PolygonCollider2D))
                {
                    PolygonCollider2D poly = boundingShape2D as PolygonCollider2D;
                    m_originalPath = new List<List<Vector2>>();
                    for (int i = 0; i < poly.pathCount; ++i)
                    {
                        Vector2[] path = poly.GetPath(i);
                        List<Vector2> dst = new List<Vector2>();
                        for (int j = 0; j < path.Length; ++j)
                        {
                            var point = new Vector3(
                                path[j].x * boundingShape2D.transform.localScale.x, 
                                path[j].y * boundingShape2D.transform.localScale.y, 
                                0);
                            var pointResult = boundingShape2D.transform.rotation * point;
                            dst.Add(pointResult);
                        }
                        m_originalPath.Add(dst);
                    }
                }
                else if (colliderType == typeof(CompositeCollider2D))
                {
                    CompositeCollider2D poly = boundingShape2D as CompositeCollider2D;
                    
                    m_originalPath = new List<List<Vector2>>();
                    Vector2[] path = new Vector2[poly.pointCount];
                    Vector3 lossyScale = boundingShape2D.transform.lossyScale;
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
                        m_originalPath.Add(dst);
                    }
                }
                else
                {
                    Invalidate();
                    return false; // input collider is invalid
                }

                confinerBaker.BakeConfiner(m_originalPath, aspectRatio, m_bakedConfinerResolution, 
                    maxOrthoSize, true);
                m_confinerStates = confinerBaker.GetShrinkablePolygonsAsConfinerStates();

                m_aspectRatio = aspectRatio;
                m_boundingShape2D = boundingShape2D;
                m_maxOrthoSize = maxOrthoSize;
                SetTransformCache(boundingShape2D.transform);
                SetLocalToWorldDelta();

                return true;
            }

            private bool IsValid(in Collider2D boundingShape2D, 
                in float aspectRatio, in float maxOrthoSize)
            {
                return boundingShape2D != null && 
                       m_boundingShape2D != null && m_boundingShape2D == boundingShape2D && // same boundingShape?
                       //!BoundingShapeTransformChanged(boundingShape2D.transform) && // input shape changed?
                       m_originalPath != null && // first time?
                       m_confinerStates != null && // cache not empty? 
                       Mathf.Abs(m_aspectRatio - aspectRatio) < UnityVectorExtensions.Epsilon && // aspect changed?
                       Mathf.Abs(m_maxOrthoSize - maxOrthoSize) < UnityVectorExtensions.Epsilon; // max ortho changed?
            }

            private void SetTransformCache(in Transform boundingShapeTransform)
            {
                m_boundingShapeScale = boundingShapeTransform.localScale;
                m_boundingShapeRotation = boundingShapeTransform.rotation;
            }

            private void SetLocalToWorldDelta()
            {
                Transform boundingShapeTransform = m_boundingShape2D.transform;
                m_positionDelta = boundingShapeTransform.position;
                m_rotationDelta = Quaternion.Inverse(m_boundingShapeRotation) * 
                                  boundingShapeTransform.rotation;
                
                Vector3 localScale = boundingShapeTransform.localScale;
                localScale.x = Math.Abs(m_boundingShapeScale.x) < UnityVectorExtensions.Epsilon
                    ? 0
                    : localScale.x / m_boundingShapeScale.x;
                localScale.y = Math.Abs(m_boundingShapeScale.y) < UnityVectorExtensions.Epsilon
                    ? 0
                    : localScale.y / m_boundingShapeScale.y;
                localScale.z = Math.Abs(m_boundingShapeScale.z) < UnityVectorExtensions.Epsilon
                    ? 0
                    : localScale.z / m_boundingShapeScale.z;
                m_scaleDelta = localScale; // TODO: directly assign scaleDelta.xyz
            }
        }
        private ShapeCache m_shapeCache;

        private GameObject localToWorldTransformHolder;

        private void OnValidate()
        {
            m_Damping = Mathf.Max(0, m_Damping);
            m_MaxOrthoSize = Mathf.Max(0, m_MaxOrthoSize);
        }

    #if UNITY_EDITOR
        internal Color m_gizmoColor = Color.yellow;
        private void OnDrawGizmos()
        {
            if (!m_DrawGizmos || 
                m_extra == null || m_extra.m_vcamShapeCache.m_path == null || m_shapeCache.m_originalPath == null)
            {
                return;
            }
            
            // Draw confiner for current camera size
            Gizmos.color = m_gizmoColor;
            Vector3 offset3 = Vector3.zero;
                // m_shapeCache.m_localToWorldDelta.transform.TransformPoint(m_shapeCache.m_boundingShape2D.offset);
            foreach (var path in m_extra.m_vcamShapeCache.m_path)
            {
                for (var index = 0; index < path.Count; index++)
                {
                    Gizmos.DrawLine(
                        m_shapeCache.ApplyTransformationDelta(path[index]) + offset3,
                        m_shapeCache.ApplyTransformationDelta(path[(index + 1) % path.Count]) + offset3);
                }
            }

            Vector2 offset2 = Vector2.zero;
            // Draw input confiner
            Gizmos.color = new Color(m_gizmoColor.r, m_gizmoColor.g, m_gizmoColor.b, m_gizmoColor.a / 2f); // dimmed yellow
            foreach (var path in m_shapeCache.m_originalPath )
            {
                for (var index = 0; index < path.Count; index++)
                {
                    Gizmos.DrawLine(
                        m_shapeCache.ApplyTransformationDelta(path[index] + offset2),
                        m_shapeCache.ApplyTransformationDelta(path[(index + 1) % path.Count] + offset2));
                }
            }
        }
    #endif
    }
#endif
}