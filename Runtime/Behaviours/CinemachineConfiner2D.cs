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
        internal const float m_bakedConfinerResolution = 0.005f; // internal, because Tests access it

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
                var oldCameraPos = state.CorrectedPosition;
                var cameraPosLocal = m_shapeCache.m_DeltaWorldToBaked.MultiplyPoint3x4(oldCameraPos);
                float frustumHeight = CalculateHalfFrustumHeight(state, cameraPosLocal.z);
                var extra = GetExtraState<VcamExtraState>(vcam);
                extra.m_vcam = vcam;
                extra.m_VcamShapeCache.ValidateCache(m_confinerBaker, confinerStateChanged, frustumHeight);
                
                cameraPosLocal = ConfinePoint(cameraPosLocal, 
                    extra.m_VcamShapeCache.m_Path, extra.m_VcamShapeCache.m_PathHasBone, state.Lens.Aspect);
                var newCameraPos = m_shapeCache.m_DeltaBakedToWorld.MultiplyPoint3x4(cameraPosLocal);

                // Don't move the camera along its z-axis
                var fwd = state.CorrectedOrientation * Vector3.forward;
                newCameraPos -= fwd * Vector3.Dot(fwd, newCameraPos - oldCameraPos);

                // Remember the desired displacement for next frame
                var displacement = newCameraPos - oldCameraPos;
                var prev = extra.m_PreviousDisplacement;
                extra.m_PreviousDisplacement = displacement;

                if (!VirtualCamera.PreviousStateIsValid || deltaTime < 0 || m_Damping <= 0)
                    extra.m_DampedDisplacement = Vector3.zero;
                else
                {
                    // If a big change from previous frame's desired displacement is detected, 
                    // assume we are going around a corner and extract that difference for damping
                    if (Vector2.Angle(prev, displacement) > m_cornerAngleTreshold)
                        extra.m_DampedDisplacement += displacement - prev;

                    extra.m_DampedDisplacement -= Damper.Damp(extra.m_DampedDisplacement, m_Damping, deltaTime);
                    displacement -= extra.m_DampedDisplacement;
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
        private float CalculateHalfFrustumHeight(in CameraState state, in float cameraPosLocalZ)
        {
            float frustumHeight;
            if (state.Lens.Orthographic)
            {
                frustumHeight = Mathf.Abs(state.Lens.OrthographicSize);
            }
            else
            {
                // distance between the collider's plane and the camera
                float distance = cameraPosLocalZ;
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
            in bool hasBone, in float aspect)
        {
            if (ShrinkablePolygon.IsInside(pathCache, positionToConfine))
            {
                return positionToConfine;
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
                        Vector2 difference = positionToConfine - c;
                        difference.x /= aspect; // the weight of distance on X axis depends on the aspect ratio. y is 1
                        
                        float distance = Vector2.SqrMagnitude(difference);
                        if (distance < minDistance && (!hasBone || !DoesIntersectOriginal(positionToConfine, c)))
                        {
                            minDistance = distance;
                            closest = c;
                        }
                        v0 = v;
                    }
                }
            }

            return closest;
        }

        private bool DoesIntersectOriginal(Vector2 l1, Vector2 l2)
        {
            foreach (var originalPath in m_shapeCache.m_OriginalPath)
            {
                for (int i = 0; i < originalPath.Count; ++i)
                {
                    if (UnityVectorExtensions.FindIntersection(l1, l2, originalPath[i], 
                        originalPath[(i + 1) % originalPath.Count], out _) == 2)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        
        private class VcamExtraState
        {
            public Vector3 m_PreviousDisplacement;
            public Vector3 m_DampedDisplacement;
            public VcamShapeCache m_VcamShapeCache;
            
            internal CinemachineVirtualCameraBase m_vcam;
            
            /// <summary> Contains all the cache items that are dependent on something in the vcam. </summary>
            internal struct VcamShapeCache
            {
                public List<List<Vector2>> m_Path;
                public bool m_PathHasBone;
                
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
                    ShrinkablePolygon.ConvertToPath(confinerCache.m_Polygons, frustumHeight, out m_Path, out m_PathHasBone);
                
                    m_frustumHeight = frustumHeight;
                }

                private bool IsValid(in float frustumHeight)
                {
                    return m_Path != null && Math.Abs(frustumHeight - m_frustumHeight) < m_bakedConfinerResolution;
                }
            }
        };
        
        private ShapeCache m_shapeCache; 

        /// <summary>
        /// ShapeCache: contains all states that dependent only on the settings in the confiner.
        /// </summary>
        private struct ShapeCache
        {
            public List<List<Vector2>> m_OriginalPath;  // in baked space, not including offset

            // These account for offset and transform change since baking
            public Matrix4x4 m_DeltaWorldToBaked; 
            public Matrix4x4 m_DeltaBakedToWorld;

            private float m_aspectRatio;
            private float m_maxOrthoSize;

            private Matrix4x4 m_bakedToWorld; // defines baked space
            private Collider2D m_boundingShape2D;
            private List<ConfinerOven.ConfinerState> m_confinerStates;

            /// <summary>
            /// Invalidates shapeCache
            /// </summary>
            public void Invalidate()
            {
                m_aspectRatio = 0;
                m_maxOrthoSize = 0;
                m_DeltaBakedToWorld = m_DeltaWorldToBaked = Matrix4x4.identity;

                m_boundingShape2D = null;
                m_OriginalPath = null;

                m_confinerStates = null;
            }
            
            /// <summary>
            /// Checks if we have a valid confiner state cache. Calculates cache if it is invalid (outdated or empty).
            /// </summary>
            /// <param name="aspectRatio">Camera window ratio (width / height)</param>
            /// <param name="confinerStateChanged">True, if the baked confiner state has changed.
            /// False, otherwise.</param>
            /// <returns>True, if path is baked and valid. False, otherwise.</returns>
            public bool ValidateCache(
                Collider2D boundingShape2D, float maxOrthoSize, ConfinerOven confinerBaker,
                 float aspectRatio, out bool confinerStateChanged)
            {
                confinerStateChanged = false;
                if (IsValid(boundingShape2D, aspectRatio, maxOrthoSize))
                {
                    CalculateDeltaTransformationMatrix();
                    return true;
                }
                
                Invalidate();
                confinerStateChanged = true;
                
                Type colliderType = boundingShape2D == null ? null:  boundingShape2D.GetType();
                if (colliderType == typeof(PolygonCollider2D))
                {
                    PolygonCollider2D poly = boundingShape2D as PolygonCollider2D;
                    m_OriginalPath = new List<List<Vector2>>();

                    // Cache the current worldspace shape
                    m_bakedToWorld = boundingShape2D.transform.localToWorldMatrix;
                    for (int i = 0; i < poly.pathCount; ++i)
                    {
                        Vector2[] path = poly.GetPath(i);
                        List<Vector2> dst = new List<Vector2>();
                        for (int j = 0; j < path.Length; ++j)
                            dst.Add(m_bakedToWorld.MultiplyPoint3x4(path[j]));
                        m_OriginalPath.Add(dst);
                    }
                }
                else if (colliderType == typeof(CompositeCollider2D))
                {
                    CompositeCollider2D poly = boundingShape2D as CompositeCollider2D;
                    m_OriginalPath = new List<List<Vector2>>();

                    // Cache the current worldspace shape
                    m_bakedToWorld = boundingShape2D.transform.localToWorldMatrix;
                    Vector2[] path = new Vector2[poly.pointCount];
                    for (int i = 0; i < poly.pathCount; ++i)
                    {
                        int numPoints = poly.GetPath(i, path);
                        List<Vector2> dst = new List<Vector2>();
                        for (int j = 0; j < numPoints; ++j)
                            dst.Add(m_bakedToWorld.MultiplyPoint3x4(path[j]));
                        m_OriginalPath.Add(dst);
                    }
                }
                else
                {
                    return false; // input collider is invalid
                }

                confinerBaker.BakeConfiner(m_OriginalPath, aspectRatio, m_bakedConfinerResolution, maxOrthoSize, true);
                m_confinerStates = confinerBaker.GetShrinkablePolygonsAsConfinerStates();
                m_aspectRatio = aspectRatio;
                m_boundingShape2D = boundingShape2D;
                m_maxOrthoSize = maxOrthoSize;

                CalculateDeltaTransformationMatrix();

                return true;
            }

            private bool IsValid(in Collider2D boundingShape2D, in float aspectRatio, in float maxOrthoSize)
            {
                return boundingShape2D != null && 
                       m_boundingShape2D != null && m_boundingShape2D == boundingShape2D && // same boundingShape?
                       m_OriginalPath != null && // first time?
                       m_confinerStates != null && // cache not empty? 
                       Mathf.Abs(m_aspectRatio - aspectRatio) < UnityVectorExtensions.Epsilon && // aspect changed?
                       Mathf.Abs(m_maxOrthoSize - maxOrthoSize) < UnityVectorExtensions.Epsilon; // max ortho changed?
            }

            private void CalculateDeltaTransformationMatrix()
            {
                // Account for current collider offset (in local space) and 
                // incorporate the worldspace delta that the confiner has moved since baking
                var m = Matrix4x4.Translate(-m_boundingShape2D.offset) * m_boundingShape2D.transform.worldToLocalMatrix;
                m_DeltaWorldToBaked = m_bakedToWorld * m;
                m_DeltaBakedToWorld = m_DeltaWorldToBaked.inverse;
            }
        }
        
        // Used by editor gizmo drawer
        internal bool GetGizmoPaths(
            out List<List<Vector2>> originalPath,
            ref List<List<Vector2>> currentPath,
            out Matrix4x4 pathLocalToWorld)
        {
            originalPath = m_shapeCache.m_OriginalPath;
            pathLocalToWorld = m_shapeCache.m_DeltaBakedToWorld;

            currentPath.Clear();
            var allExtraStates = GetAllExtraStates<VcamExtraState>();
            for (int i = 0; i < allExtraStates.Count; ++i)
            {
                if (CinemachineCore.Instance.IsLive(allExtraStates[i].m_vcam))
                {
                    for (int p = 0; p < allExtraStates[i].m_VcamShapeCache.m_Path.Count; ++p)
                    {
                        currentPath.Add(allExtraStates[i].m_VcamShapeCache.m_Path[p]);
                    }
                }
            }
            return originalPath != null;
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