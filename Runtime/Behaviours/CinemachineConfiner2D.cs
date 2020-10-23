#if !UNITY_2019_3_OR_NEWER
#define CINEMACHINE_PHYSICS_2D
#endif

using System;
using System.Collections.Generic;
using Cinemachine.Utility;
using UnityEngine;
using Matrix4x4 = UnityEngine.Matrix4x4;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Cinemachine
{
#if CINEMACHINE_PHYSICS_2D
    /// <summary>
    /// <para>
    /// An add-on module for Cinemachine Virtual Camera that post-processes the final position of the virtual camera.
    /// It will confine the virtual camera view window to the area specified in the Bounding Shape 2D field based on
    /// the camera's window size and ratio. The confining area is baked and cached at start.
    /// </para>
    /// 
    ///<para>
    /// CinemachineConfiner2D uses a cache to avoid recalculating the confiner unnecessarily.
    /// If the cache is invalid, it will be automatically recomputed at the next usage (lazy evaluation).
    /// The cache is automatically invalidated in some well-defined circumstances:
    /// <list type="bullet">
    /// <item><description> MaxOrthoSize parameter changes.</description></item>
    /// <item><description> Aspect ratio of the parent vcam changes.</description></item>
    /// <item><description> Input collider object changes (i.e. use another Collider2D).</description></item>
    /// </list>
    /// </para>
    /// 
    /// <para>
    /// The cache is <strong>NOT</strong> automatically invalidated (due to high computation cost every frame) if the
    /// contents of the confining shape change (e.g. points get moved dynamically). In that case, the client must call
    /// the InvalidatePathCache() function or the user must click the Invalidate Cache button in the component's
    /// inspector.
    /// </para>
    /// 
    /// <para>
    /// Collider's Transform changes are supported, but after changing the Scale or Rotation components the cache is
    /// going to be invalid. If the users would like to have a valid cache, then they must call the
    /// InvalidatePathCache() function or the user must click the Invalidate Cache button in the component's inspector.
    /// </para>
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
        public float m_Damping;

        /// <summary>
        /// The confiner will correctly confine up to this maximum orthographic size. If set to 0, then this
        /// parameter is ignored and all camera sizes are supported. Use it to optimize computation and memory costs.
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

        /// <summary>Validates cache</summary>
        /// <param name="cameraAspectRatio">Aspect ratio of camera.</param>
        /// <returns>Returns true if the cache could be validated. False, otherwise.</returns>
        public bool ValidatePathCache(float cameraAspectRatio)
        {
            return m_shapeCache.ValidateCache(
                m_BoundingShape2D, m_MaxOrthoSize, m_confinerBaker, cameraAspectRatio, out _);
        }
        
        private readonly ConfinerOven m_confinerBaker = new ConfinerOven();
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
                
                // TODO: use this for frustum height too
                Vector3 cameraPosLocal = m_shapeCache.TransformPointToConfinerSpace(state.CorrectedPosition);
                
                float frustumHeight = CalculateHalfFrustumHeight(state, vcam);
                var extra = GetExtraState<VcamExtraState>(vcam);
                extra.m_vcam = vcam;
                extra.m_vcamShapeCache.ValidateCache(m_confinerBaker, confinerStateChanged, frustumHeight);
                
                Vector3 displacement = ConfinePoint(cameraPosLocal, extra.m_vcamShapeCache.m_path);
                displacement = m_shapeCache.TransformConfinerSpacePointToWorld(displacement);

                // Remember the desired displacement for next frame
                var prev = extra.m_previousDisplacement;
                extra.m_previousDisplacement = displacement;

                if (!VirtualCamera.PreviousStateIsValid || deltaTime < 0 || m_Damping <= 0)
                    extra.m_dampedDisplacement = Vector3.zero;
                else
                {
                    // If a big change from previous frame's desired displacement is detected, 
                    // assume we are going around a corner and extract that difference for damping
                    if (Vector2.Angle(prev, displacement) > m_cornerAngleTreshold)
                        extra.m_dampedDisplacement += displacement - prev;

                    extra.m_dampedDisplacement -= Damper.Damp(extra.m_dampedDisplacement, m_Damping, deltaTime);
                    displacement -= extra.m_dampedDisplacement;
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
                // TODO: move camera to collider local space 
                // distance between the collider's plane and the camera
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
        private Vector2 ConfinePoint(Vector2 positionToConfine, in List<List<Vector2>> pathCache)
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
        
        internal static readonly float m_bakedConfinerResolution = 0.005f; // internal, because Tests access it

        private List<List<Vector2>> m_gizmoPaths = new List<List<Vector2>>(); // TODO: editor and out it
        internal List<List<Vector2>> GetCurrentPath()
        {
            m_gizmoPaths.Clear();
            var allExtraStates = GetAllExtraStates<VcamExtraState>();
            for (int i = 0; i < allExtraStates.Count; ++i)
            {
                if (!CinemachineCore.Instance.IsLive(allExtraStates[i].m_vcam)) continue;

                for (int p = 0; p < allExtraStates[i].m_vcamShapeCache.m_path.Count; ++p)
                {
                    m_gizmoPaths.Add(allExtraStates[i].m_vcamShapeCache.m_path[p]);
                }
            }
            return m_gizmoPaths;
        }
        private class VcamExtraState
        {
            internal CinemachineVirtualCameraBase m_vcam;
            public Vector3 m_previousDisplacement;
            public Vector3 m_dampedDisplacement;
            public VcamShapeCache m_vcamShapeCache;
            
            /// <summary> Contains all the cache items that are dependent on something in the vcam. </summary>
            internal struct VcamShapeCache
            {
                public List<List<Vector2>> m_path;
                
                private float m_frustumHeight;
                
                /// <summary>
                /// Check that the path cache was converted from the current confiner cache, or
                /// converts it if the frustum height was changed.
                /// </summary>
                public void ValidateCache(
                    in ConfinerOven confinerBaker, in bool confinerStateChanged, 
                    in float frustumHeight)
                {
                    if (!confinerStateChanged && IsValid(frustumHeight))
                    {
                        return;
                    }
            
                    var confinerCache = confinerBaker.GetConfinerAtFrustumHeight(frustumHeight);
                    ShrinkablePolygon.ConvertToPath(confinerCache.polygons, frustumHeight, 
                        out m_path);
                
                    m_frustumHeight = frustumHeight;
                }

                
                private bool IsValid(in float frustumHeight)
                {
                    return m_path != null &&
                           Math.Abs(frustumHeight - m_frustumHeight) < m_bakedConfinerResolution;
                }
            }
        };
        
        internal ShapeCache m_shapeCache; // internal, because Editor Gizmos access it
        /// <summary>
        /// ShapeCache: contains all state that's dependent only on the settings in the confiner.
        /// </summary>
        internal struct ShapeCache  // internal, because Editor Gizmos access it
        {
            public List<List<Vector2>> m_originalPath;

            public Vector3 m_positionDelta;
            public Vector3 m_offset;
            public Vector3 m_scaleDelta;
            public Quaternion m_rotationDelta;
            
            private float m_aspectRatio;
            private float m_maxOrthoSize;
            private Vector3 m_boundingShapeBakedScale;
            private Quaternion m_boundingShapeBakedRotation;

            private Collider2D m_boundingShape2D;
            private List<ConfinerOven.ConfinerState> m_confinerStates;

            /// <summary>
            /// Invalidates shapeCache
            /// </summary>
            public void Invalidate()
            {
                m_aspectRatio = 0;
                m_maxOrthoSize = 0;
                
                m_boundingShapeBakedScale = Vector3.one;
                m_boundingShapeBakedRotation = Quaternion.identity;
                
                m_boundingShape2D = null;
                m_originalPath = null;

                m_confinerStates = null;
            }

            /// <summary>
            /// Checks if we have a valid confiner state cache. Calculates cache if it is invalid (outdated or empty).
            /// </summary>
            /// <param name="aspectRatio">Camera window ratio (width / height)</param>
            /// <param name="confinerStateChanged">True, if the baked confiner state has changed.
            /// False, otherwise.</param>
            /// <returns>True, if path is baked and valid. False, otherwise.</returns>
            public bool ValidateCache(Collider2D boundingShape2D, float maxOrthoSize, ConfinerOven confinerBaker,
                 float aspectRatio, out bool confinerStateChanged)
            {
                confinerStateChanged = false;
                if (IsValid(boundingShape2D, 
                    aspectRatio, maxOrthoSize))
                {
                    m_boundingShape2D = boundingShape2D;
                    CalculateDeltaTransformationMatrix();
                    CalculateOffset();
                    return true;
                }
                
                Invalidate();
                confinerStateChanged = true;
                
                Type colliderType = boundingShape2D == null ? null:  boundingShape2D.GetType();
                if (colliderType == typeof(PolygonCollider2D))
                {
                    PolygonCollider2D poly = boundingShape2D as PolygonCollider2D;
                    m_originalPath = new List<List<Vector2>>();
                    var localToWorld = boundingShape2D.transform.localToWorldMatrix;
                    localToWorld.m03 = 0; localToWorld.m13 = 0; localToWorld.m23 = 0; // set translation part to 0
                    
                    for (int i = 0; i < poly.pathCount; ++i)
                    {
                        Vector2[] path = poly.GetPath(i);
                        List<Vector2> dst = new List<Vector2>();
                        for (int j = 0; j < path.Length; ++j)
                        {
                            dst.Add(localToWorld.MultiplyPoint3x4(path[j]));
                        }
                        m_originalPath.Add(dst);
                    }
                }
                else if (colliderType == typeof(CompositeCollider2D))
                {
                    CompositeCollider2D poly = boundingShape2D as CompositeCollider2D;
                    
                    m_originalPath = new List<List<Vector2>>();
                    var localToWorld = boundingShape2D.transform.localToWorldMatrix;
                    localToWorld.m03 = 0; localToWorld.m13 = 0; localToWorld.m23 = 0; // set translation part to 0
                    Vector2[] path = new Vector2[poly.pointCount];
                    for (int i = 0; i < poly.pathCount; ++i)
                    {
                        int numPoints = poly.GetPath(i, path);
                        List<Vector2> dst = new List<Vector2>();
                        for (int j = 0; j < numPoints; ++j)
                        {
                            dst.Add(localToWorld.MultiplyPoint3x4(path[j]));
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
                CalculateDeltaTransformationMatrix();
                CalculateOffset();

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
                m_boundingShapeBakedScale = boundingShapeTransform.lossyScale;
                m_boundingShapeBakedRotation = boundingShapeTransform.rotation;
            }
            
            /// <summary>
            /// Transforms point to confiner local space
            /// </summary>
            /// <param name="point">Point to transform</param>
            /// <returns>Point in confiner's local space</returns>
            public Vector3 TransformPointToConfinerSpace(in Vector3 point)
            {
                Vector3 pointInConfinerSpace = point - m_positionDelta - m_offset;
                pointInConfinerSpace = R.MultiplyPoint3x4(pointInConfinerSpace);
                pointInConfinerSpace = S.MultiplyPoint3x4(pointInConfinerSpace);
                return pointInConfinerSpace;
            }

            /// <summary>
            /// Transforms point in confiner local space to world space.
            /// </summary>
            /// <param name="point">Point to transform</param>
            /// <returns>Point in world space</returns>
            public Vector3 TransformConfinerSpacePointToWorld(in Vector3 point)
            {
                Vector3 displacementCamera = S_inverse.MultiplyPoint3x4(point);
                displacementCamera = R_inverse.MultiplyPoint3x4(displacementCamera);
                return displacementCamera;
            }

            private Matrix4x4 S, R; // Scale, Rotation matrices converting camera to confiner space
            private Matrix4x4 S_inverse, R_inverse; // Scale, Rotation matrices converting displacement to camera space
            private void CalculateDeltaTransformationMatrix()
            {
                if (m_boundingShape2D.transform.hasChanged)
                {
                    Transform boundingShapeTransform = m_boundingShape2D.transform;
                
                    m_positionDelta = boundingShapeTransform.position;
                
                    m_rotationDelta = Quaternion.Inverse(m_boundingShapeBakedRotation) * boundingShapeTransform.rotation;
                
                    Vector3 lossyScale = boundingShapeTransform.lossyScale;
                    m_scaleDelta.x = Math.Abs(m_boundingShapeBakedScale.x) < UnityVectorExtensions.Epsilon
                        ? 0
                        : lossyScale.x / m_boundingShapeBakedScale.x;
                    m_scaleDelta.y = Math.Abs(m_boundingShapeBakedScale.y) < UnityVectorExtensions.Epsilon
                        ? 0
                        : lossyScale.y / m_boundingShapeBakedScale.y;
                    m_scaleDelta.z = Math.Abs(m_boundingShapeBakedScale.z) < UnityVectorExtensions.Epsilon
                        ? 0
                        : lossyScale.z / m_boundingShapeBakedScale.z;
                
                    S = Matrix4x4.identity;
                    S.m00 = Math.Abs(m_scaleDelta.x) < UnityVectorExtensions.Epsilon ? 0 : 1f / m_scaleDelta.x;
                    S.m11 = Math.Abs(m_scaleDelta.y) < UnityVectorExtensions.Epsilon ? 0 : 1f / m_scaleDelta.y;
                    S.m22 = Math.Abs(m_scaleDelta.z) < UnityVectorExtensions.Epsilon ? 0 : 1f / m_scaleDelta.z;
                    S_inverse = S.inverse;

                    R = Matrix4x4.Rotate(Quaternion.Inverse(m_rotationDelta));
                    R_inverse = Matrix4x4.Rotate(m_rotationDelta);
                }
            }

            private void CalculateOffset()
            {
                var offset = m_boundingShape2D.offset;
                var boundingShapeTransform = m_boundingShape2D.transform;
                m_offset = new Vector3(
                    offset.x * boundingShapeTransform.lossyScale.x,
                    offset.y * boundingShapeTransform.lossyScale.y,
                    0
                );
                m_offset = boundingShapeTransform.rotation * m_offset;
            }
        }

        private void OnValidate()
        {
            m_Damping = Mathf.Max(0, m_Damping);
            m_MaxOrthoSize = Mathf.Max(0, m_MaxOrthoSize);
        }

        private void Reset()
        {
            m_Damping = 0;
            m_MaxOrthoSize = 0;
        }
    }
#endif
}