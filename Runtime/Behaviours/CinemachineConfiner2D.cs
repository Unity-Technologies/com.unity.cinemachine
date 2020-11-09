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
    /// <para>
    /// An add-on module for Cinemachine Virtual Camera that post-processes the final position 
    /// of the virtual camera.  It will confine the camera's position such that the screen edges stay 
    /// within a shape defined by a 2D polygon.  This will work for orthographic or perspective cameras, 
    /// provided that the camera's forward vector remains parallel to the bounding shape's normal, 
    /// i.e. that the camera is looking straight at the polygon, and not obliquely at it.
    /// </para>
    /// 
    /// <para>
    /// When confining the camera, the camera's view size at the polygon plane is considered, and 
    /// also its aspect ratio. Based on this information and the input polygon, a second (smaller) 
    /// polygon is computed to which the camera's transform is constrained. Computation of this secondary 
    /// polygon is nontrivial and expensive, so it should be done only when absolutely necessary.
    /// </para>
    ///
    /// <para>
    /// The cached secondary polygon needs to be recomputed in the following circumstances:
    /// <list type="bullet">
    /// <item><description> when the input polygon's points change.</description></item>
    /// <item><description> when the input polygon is non-uniformly scaled, or.</description></item>
    /// <item><description> when the input polygon is rotated.</description></item>
    /// </list>
    /// For efficiency reasons, Cinemachine will not automatically regenerate the inner polygon 
    /// in these cases, and it is the responsibility of the client to call the InvalidateCache() 
    /// method to trigger the recalculation. An inspector button is also provided for this purpose.
    /// </para>
    ///
    /// <para>
    /// If the input polygon scales uniformly or translates, the cache remains valid. If the 
    /// polygon rotates, then the cache degrades in quality (more or less depending on the aspect 
    /// ratio - it's better if the ratio is close to 1:1) but can still be used. 
    /// Regenerating it will eliminate the imperfections.
    /// </para>
    ///
    /// <para>
    /// The cached secondary polygon is not a single polygon, but rather a family of polygons from 
    /// which a member is chosen depending on the current size of the camera view. The number of 
    /// polygons in this family will depend on the complexity of the input polygon, and the maximum 
    /// expected camera view size. The MaxOrthoSize property is provided to give a hint to the 
    /// algorithm to stop generating polygons for camera view sizes larger than the one specified. 
    /// This can represent a substantial cost saving when regenerating the cache, so it is a good 
    /// idea to set it carefully. Leaving it at 0 will cause the maximum number of polygons to be generated.
    /// </para>
    /// </summary>
    [AddComponentMenu("")] // Hide in menu
    [SaveDuringPlay]
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [HelpURL(Documentation.BaseURL + "manual/CinemachineConfiner2D.html")]
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
        /// To optimize computation and memory costs, set this to the largest view size that the camera 
        /// is expected to have.  The confiner will not compute a polygon cache for frustum sizes larger 
        /// than this.  This refers to the size in world units of the frustum at the confiner plane 
        /// (for orthographic cameras, this is just the orthographic size).  If set to 0, then this 
        /// parameter is ignored and a polygon cache will be calculated for all potential window sizes.
        /// </summary>
        [Tooltip("To optimize computation and memory costs, set this to the largest view size that the "
            + "camera is expected to have.  The confiner will not compute a polygon cache for frustum "
            + "sizes larger than this.  This refers to the size in world units of the frustum at the "
            + "confiner plane (for orthographic cameras, this is just the orthographic size).  If set "
            + "to 0, then this parameter is ignored and a polygon cache will be calculated for all "
            + "potential window sizes.")]
        public float m_MaxWindowSize;

        /// <summary>Invalidates cache and consequently trigger a rebake at next iteration.</summary>
        public void InvalidateCache()
        {
            m_shapeCache.Invalidate();
        }

        /// <summary>Validates cache</summary>
        /// <param name="cameraAspectRatio">Aspect ratio of camera.</param>
        /// <returns>Returns true if the cache could be validated. False, otherwise.</returns>
        public bool ValidateCache(float cameraAspectRatio)
        {
            return m_shapeCache.ValidateCache(
                m_BoundingShape2D, m_MaxWindowSize, m_confinerBaker, cameraAspectRatio, out _);
        }

        private readonly ConfinerOven m_confinerBaker = new ConfinerOven();

        private const float m_cornerAngleTreshold = 10f;

        private float m_currentFrustumHeight = 0;
        
        protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam, 
            CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            if (stage == CinemachineCore.Stage.Body)
            {
                var aspectRatio = state.Lens.Aspect;
                if (!m_shapeCache.ValidateCache(
                    m_BoundingShape2D, m_MaxWindowSize, m_confinerBaker,
                    aspectRatio, out bool confinerStateChanged))
                {
                    return; // invalid path
                }
                
                var oldCameraPos = state.CorrectedPosition;
                var cameraPosLocal = m_shapeCache.m_DeltaWorldToBaked.MultiplyPoint3x4(oldCameraPos);
                m_currentFrustumHeight = CalculateHalfFrustumHeight(state, cameraPosLocal.z);
                var extra = GetExtraState<VcamExtraState>(vcam);
                extra.m_vcam = vcam;
                extra.m_VcamShapeCache.ValidateCache(
                    m_confinerBaker, confinerStateChanged, 
                    aspectRatio, m_currentFrustumHeight);
                
                cameraPosLocal = ConfinePoint(cameraPosLocal, 
                    extra.m_VcamShapeCache.m_Path, extra.m_VcamShapeCache.m_PathHasBone,
                    state.Lens.Aspect * m_currentFrustumHeight, m_currentFrustumHeight);
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
                    if (prev.sqrMagnitude > 0.01f && Vector2.Angle(prev, displacement) > m_cornerAngleTreshold)
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
                frustumHeight = state.Lens.OrthographicSize;
            }
            else
            {
                // distance between the collider's plane and the camera
                float distance = cameraPosLocalZ;
                frustumHeight = distance * Mathf.Tan(state.Lens.FieldOfView * 0.5f * Mathf.Deg2Rad);
            }

            return Mathf.Abs(frustumHeight);
        }
        
        /// <summary>
        /// Confines input 2D point within the confined area.
        /// </summary>
        /// <param name="positionToConfine">2D point to confine</param>
        /// <returns>Confined position</returns>
        private Vector2 ConfinePoint(Vector2 positionToConfine, in List<List<Vector2>> pathCache,
            in bool hasBone, in float windowWidth, in float windowHeight)
        {
            if (ShrinkablePolygon.IsInside(pathCache, positionToConfine))
            {
                return positionToConfine;
            }

            // If the poly has bones and if the position to confine is not outside of the original
            // bounding shape, then it is possible that the bone in a neighbouring section of the 
            // is closer than the bone in the correct section of the polygon, if the current section 
            // is very large and the neighbouring section is small.  In that case, we'll need to 
            // add an extra check when calculating the nearest point.
            bool checkIntersectOriginal = hasBone 
                && ShrinkablePolygon.IsInside(m_shapeCache.m_OriginalPath, positionToConfine);

            Vector2 closest = positionToConfine;
            float minDistance = float.MaxValue;
            for (int i = 0; i < pathCache.Count; ++i)
            {
                int numPoints = pathCache[i].Count;
                for (int j = 0; j < numPoints; ++j)
                {
                    Vector2 v0 = pathCache[i][j];
                    Vector2 v = pathCache[i][(j + 1) % numPoints];
                    Vector2 c = Vector2.Lerp(v0, v, positionToConfine.ClosestPointOnSegment(v0, v));
                    Vector2 difference = positionToConfine - c;
                    float distance = Vector2.SqrMagnitude(difference);
                    if (Mathf.Abs(difference.x) > windowWidth || Mathf.Abs(difference.y) > windowHeight)
                    {
                        // penalty for points from which the target is not visible, prefering visibility over proximity
                        distance += m_confinerBaker.SqrPolygonDiagonal; 
                    }

                    if (distance < minDistance 
                        && (!checkIntersectOriginal || !DoesIntersectOriginal(positionToConfine, c)))
                    {
                        minDistance = distance;
                        closest = c;
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
                    in float aspectRatio, in float frustumHeight)
                {
                    if (!confinerStateChanged && m_Path != null && 
                        Math.Abs(frustumHeight - m_frustumHeight) < UnityVectorExtensions.Epsilon)
                    {
                        return;
                    }
            
                    var confinerCache = confinerBaker.GetConfinerAtFrustumHeight(frustumHeight);
                    ShrinkablePolygon.ConvertToPath(confinerCache.m_Polygons, 
                        aspectRatio, frustumHeight, confinerBaker.MaxFrustumHeight, 
                        out m_Path, out m_PathHasBone);
                
                    m_frustumHeight = frustumHeight;
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
                
                confinerBaker.BakeConfiner(m_OriginalPath, aspectRatio, maxOrthoSize);
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

        // Used by editor gizmo drawer
        internal bool IsOverCachedMaxFrustumHeight()
        {
            return m_confinerBaker.MaxFrustumHeight < m_currentFrustumHeight;
        }

        private void OnValidate()
        {
            m_Damping = Mathf.Max(0, m_Damping);
            m_MaxWindowSize = Mathf.Max(0, m_MaxWindowSize);
        }

        private void Reset()
        {
            m_Damping = 0.5f;
            m_MaxWindowSize = 0;
        }
    }
#endif
}