#if CINEMACHINE_PHYSICS_2D

using System;
using System.Collections.Generic;
using Cinemachine.Utility;
using UnityEngine;
using UnityEngine.Serialization;

namespace Cinemachine
{
    /// <summary>
    /// An add-on module for Cinemachine Camera that post-processes the final position 
    /// of the virtual camera.  It will confine the camera's position such that the screen edges stay 
    /// within a shape defined by a 2D polygon.  This will work for orthographic or perspective cameras, 
    /// provided that the camera's forward vector remains parallel to the bounding shape's normal, 
    /// i.e. that the camera is looking straight at the polygon, and not obliquely at it.
    /// 
    /// When confining the camera, the camera's view size at the polygon plane is considered, and 
    /// also its aspect ratio. Based on this information and the input polygon, a second (smaller) 
    /// polygon is computed to which the camera's transform is constrained. Computation of this secondary 
    /// polygon is nontrivial and expensive, so it should be done only when absolutely necessary.
    ///
    /// When the Orthographic Size or Field of View of the Cinemachine Camera's lens changes, Cinemachine will not
    /// automatically adjust the Confiner for efficiency reasons. To adjust the Confiner, call InvalidateComputedConfiner().
    /// An inspector button is also provided for this purpose.
    ///
    /// Confiner2D pre-calculates a cache to speed up calculation.
    /// The cache needs to be recomputed in the following circumstances:
    ///  - when the input polygon's points change
    ///  - when the input polygon is non-uniformly scaled
    ///  - when the input polygon is rotated
    ///
    /// For efficiency reasons, Cinemachine will not automatically regenerate the cache.
    /// It is the responsibility of the client to call the InvalidateBoundingShapeCache() method to trigger
    /// a recalculation. An inspector button is also provided for this purpose.
    ///
    /// If the input polygon scales uniformly or translates, the cache remains valid. If the 
    /// polygon rotates, then the cache degrades in quality (more or less depending on the aspect 
    /// ratio - it's better if the ratio is close to 1:1) but can still be used. 
    /// Regenerating it will eliminate the imperfections.
    ///
    /// When the Oversize Window is enabled an additional pre-calculation step is added to the caching process.
    /// This cache is not a single polygon, but rather a family of polygons. The number of 
    /// polygons in this family will depend on the complexity of the input polygon, and the maximum 
    /// expected camera view size. The MaxWindowSize property is provided to give a hint to the 
    /// algorithm to stop generating polygons for camera view sizes larger than the one specified. 
    /// This can represent a substantial cost saving when regenerating the cache, so it is a good 
    /// idea to set it carefully. Leaving it at 0 will cause the maximum number of polygons to be generated.
    /// </summary>
    [AddComponentMenu("Cinemachine/Procedural/Extensions/Cinemachine Confiner 2D")]
    [SaveDuringPlay]
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [HelpURL(Documentation.BaseURL + "manual/CinemachineConfiner2D.html")]
    public class CinemachineConfiner2D : CinemachineExtension
    {
        /// <summary>The 2D shape within which the camera is to be contained.</summary>
        [Tooltip("The 2D shape within which the camera is to be contained.  " +
                 "Can be polygon-, box-, or composite collider 2D.")]
        [FormerlySerializedAs("m_BoundingShape2D")]
        public Collider2D BoundingShape2D;

        /// <summary>Damping applied automatically around corners to avoid jumps.</summary>
        [Tooltip("Damping applied around corners to avoid jumps.  Higher numbers are more gradual.")]
        [RangeSlider(0, 5)]
        [FormerlySerializedAs("m_Damping")]
        public float Damping;

        /// <summary>Size of the slow-down zone at the edge of the bounding shape.</summary>
        [Tooltip("Size of the slow-down zone at the edge of the bounding shape.")]
        public float SlowingDistance = 0;
        
        /// <summary>
        /// Settings to optimize computation and memory costs in the event that the
        /// window size is expected to be larger than will fit inside the confining shape.
        /// </summary>
        [Serializable]
        public struct OversizeWindowSettings
        {
            /// <summary>
            /// Enable optimizing of computation and memory costs in the event that the
            /// window size is expected to be larger than will fit inside the confining shape.
            /// Enable only if needed, because it's costly.
            /// </summary>
            [Tooltip("Enable optimizing of computation and memory costs in the event that the "
                + "window size is expected to be larger than will fit inside the confining shape.\n"
                + "Enable only if needed, because it's costly")]
            public bool Enabled;

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
            public float MaxWindowSize;
        }

        /// <summary>
        /// Settings to optimize computation and memory costs in the event that the
        /// window size is expected to be larger than will fit inside the confining shape.
        /// </summary>
        [FoldoutWithEnabledButton]
        public OversizeWindowSettings OversizeWindow;

        [SerializeField, HideInInspector, FormerlySerializedAs("m_MaxWindowSize")]
        float m_LegacyMaxWindowSize = -2; // -2 means there's no legacy upgrade to do

        void OnValidate()
        {
            const float maxComputationTimePerFrameInSeconds = 1f / 120f;
            Damping = Mathf.Max(0, Damping);
            SlowingDistance = Mathf.Max(0, SlowingDistance);
            m_ShapeCache.maxComputationTimePerFrameInSeconds = maxComputationTimePerFrameInSeconds;
            OversizeWindow.MaxWindowSize = Mathf.Max(0, OversizeWindow.MaxWindowSize);

            // Legacy upgrade
            if (m_LegacyMaxWindowSize != -2)
            {
                OversizeWindow = new ()
                {
                    Enabled = m_LegacyMaxWindowSize >= 0,
                    MaxWindowSize = Mathf.Max(0, m_LegacyMaxWindowSize)
                };
                m_LegacyMaxWindowSize = -2;
            }
        }

        void Reset()
        {
            Damping = 0.5f;
            SlowingDistance = 0;
            OversizeWindow = new ();
        }

        /// <summary>
        /// Report maximum damping time needed for this component.
        /// </summary>
        /// <returns>Highest damping setting in this component</returns>
        public override float GetMaxDampTime() => Mathf.Max(Damping, SlowingDistance * 0.2f); // just an approximation - we don't know the time
        
        /// <summary>This is called to notify the extension that a target got warped,
        /// so that the extension can update its internal state to make the camera
        /// also warp seamlessly.  Base class implementation does nothing.</summary>
        /// <param name="target">The object that was warped</param>
        /// <param name="positionDelta">The amount the target's position changed</param>
        public override void OnTargetObjectWarped(Transform target, Vector3 positionDelta) 
            => GetExtraState<VcamExtraState>(VirtualCamera).PreviousCameraPosition += positionDelta;
            
        /// <summary>
        /// Invalidates the lens cache for the Cinemachine Camera that ownes this Confiner.
        /// Call this when when the Field of View or Orthographic Size changes.
        /// Calculating the lens cache is fast, but causes allocations.
        /// </summary>
        /// <remarks>
        /// - When the owner is a CinemachineCameraManagerBase then this will do nothing.
        /// Call InvalidateLensCache for each child virtual camera of the manager instead.
        /// - It is often more efficient to have more Cinemachine Cameras with different lens settings
        /// that have their own confiners and blend between them instead of changing
        /// one Cinemachine Camera's lens and calling this over and over.
        /// </remarks>
        public void InvalidateLensCache() => InvalidateLensCache(VirtualCamera);

        /// <summary>
        /// Invalidates the lens cache of the input vcam, so a new one is computed next frame.
        /// Call this when when the Field of View or Orthographic Size changes.
        /// Calculating the lens cache is fast, but causes allocations.
        /// </summary>
        /// <remarks>
        /// - When the owner is a CinemachineCameraManagerBase then this will do nothing.
        /// Call InvalidateLensCache for each child virtual camera of the manager instead.
        /// - It is often more efficient to have more Cinemachine Cameras with different lens settings
        /// that have their own confiners and blend between them instead of changing
        /// one Cinemachine Camera's lens and calling this over and over.
        /// </remarks>
        /// <param name="vcam"></param>
        public void InvalidateLensCache(CinemachineVirtualCameraBase vcam)
        {
            var extra = GetExtraState<VcamExtraState>(vcam);
            extra.BakedSolution = null;
            extra.AspectRatio = 0;
            extra.FrustumHeight = 0;
        }

        /// <summary>
        /// Invalidates Bounding Shape Cache, so a new one is computed next frame.
        /// The re-computation is costly.  This recomputes the bounding shape cache, and
        /// the computed confiner cache.
        /// Call this when the input bounding shape changes (non-uniform scale, rotation, or
        /// points are moved, added or deleted).
        /// </summary>
        /// <remarks>
        /// It is much more efficient to have more Cinemachine Cameras with different input bounding shapes and
        /// blend between them instead of changing one Confiner2D's input bounding shape and calling this over and over.
        /// </remarks>
        public void InvalidateBoundingShapeCache() => m_ShapeCache.Invalidate();

        [Obsolete("Call InvalidateBoundingShapeCache() instead.", false)]
        public void InvalidateCache() => InvalidateBoundingShapeCache();

        const float k_CornerAngleThreshold = 10f;
        
        /// <summary>
        /// Callback to do the camera confining
        /// </summary>
        /// <param name="vcam">The virtual camera being processed</param>
        /// <param name="stage">The current pipeline stage</param>
        /// <param name="state">The current virtual camera state</param>
        /// <param name="deltaTime">The current applicable deltaTime</param>
        protected override void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam, 
            CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            if (stage == CinemachineCore.Stage.Body)
            {
                var aspectRatio = state.Lens.Aspect;
                if (!m_ShapeCache.ValidateCache(BoundingShape2D, OversizeWindow, aspectRatio, out bool confinerStateChanged))
                    return; // invalid path

                var extra = GetExtraState<VcamExtraState>(vcam);
                var camPos = state.GetCorrectedPosition();

                // Make sure we have a solution for our current frustum size
                if (confinerStateChanged || extra.BakedSolution == null || !extra.BakedSolution.IsValid()) 
                {
                    // convert frustum height from world to baked space. deltaWorldToBaked.lossyScale is always uniform.
                    var deltaW = m_ShapeCache.DeltaWorldToBaked;
                    extra.AspectRatio = aspectRatio;
                    extra.FrustumHeight = 
                        CalculateHalfFrustumHeight(state.Lens, deltaW.MultiplyPoint3x4(camPos).z) * deltaW.lossyScale.x;
                    extra.BakedSolution = m_ShapeCache.ConfinerOven.GetBakedSolution(extra.FrustumHeight);
                }
                var fwd = state.GetCorrectedOrientation() * Vector3.forward;
                var newPos = ConfinePoint(camPos, extra, fwd);

                if (SlowingDistance > Epsilon && deltaTime >= 0 && VirtualCamera.PreviousStateIsValid)
                {
                    // Reduce speed if moving towards the edge and close enough to it
                    var prevPos = extra.PreviousCameraPosition;
                    var dir = newPos - prevPos;
                    var speed = dir.magnitude;
                    if (speed > Epsilon)
                    {
                        var t = GetDistanceFromEdge(prevPos, dir / speed, SlowingDistance, extra, fwd) / SlowingDistance;

                        // This formula is found to give a smooth slowing curve while ensuring
                        // that it comes to a full stop in a reasonable time
                        newPos = Vector3.Lerp(prevPos, newPos, t * t * t + 0.05f);
                    }
                }

                // Remember the desired displacement for next frame
                var prev = extra.PreviousDisplacement;
                var displacement = newPos - camPos;
                extra.PreviousDisplacement = displacement;

                if (!VirtualCamera.PreviousStateIsValid || deltaTime < 0 || Damping <= 0)
                    extra.DampedDisplacement = Vector3.zero;
                else
                {
                    // If a big change from previous frame's desired displacement is detected, 
                    // assume we are going around a corner and extract that difference for damping
                    if (prev.sqrMagnitude > 0.01f && Vector2.Angle(prev, displacement) > k_CornerAngleThreshold)
                        extra.DampedDisplacement += displacement - prev;

                    extra.DampedDisplacement -= Damper.Damp(extra.DampedDisplacement, Damping, deltaTime);
                    displacement -= extra.DampedDisplacement;
                }
                state.PositionCorrection += displacement;
                extra.PreviousCameraPosition = state.GetCorrectedPosition();
            }
        }

        Vector3 ConfinePoint(Vector3 pos, VcamExtraState extra, Vector3 fwd)
        {
            var posLocal = m_ShapeCache.DeltaWorldToBaked.MultiplyPoint3x4(pos);
            var newPos = m_ShapeCache.DeltaBakedToWorld.MultiplyPoint3x4(
                extra.BakedSolution.ConfinePoint(posLocal));

            // Don't move the point alog the fwd axis
            return newPos - fwd * Vector3.Dot(fwd, newPos - pos);
        }

        // Returns distance from edge in direction of motion, or max if distance is greater than max.
        // dirUnit must be unit length.
        float GetDistanceFromEdge(Vector3 p, Vector3 dirUnit, float max, VcamExtraState extra, Vector3 fwd)
        {
            p += dirUnit * max;
            return max - (ConfinePoint(p, extra, fwd) - p).magnitude;
        }
        
        /// <summary>
        /// Calculates half frustum height for orthographic or perspective camera.
        /// For more info on frustum height, see <see cref="docs.unity3d.com/Manual/FrustumSizeAtDistance.html"/>.
        /// 
        /// Internal for editor. 
        /// </summary>
        /// <param name="lens">Camera Lens for checking if Orthographic or Perspective</param>
        /// <param name="cameraPosLocalZ">camera's z pos in local space</param>
        /// <returns>Frustum height of the camera</returns>
        internal static float CalculateHalfFrustumHeight(in LensSettings lens, in float cameraPosLocalZ)
        {
            float frustumHeight;
            if (lens.Orthographic)
                frustumHeight = lens.OrthographicSize;
            else
            {
                // distance between the collider's plane and the camera
                float distance = cameraPosLocalZ;
                frustumHeight = distance * Mathf.Tan(lens.FieldOfView * 0.5f * Mathf.Deg2Rad);
            }

            return Mathf.Abs(frustumHeight);
        }

        class VcamExtraState
        {
            public ConfinerOven.BakedSolution BakedSolution;
            
            public Vector3 PreviousDisplacement;
            public Vector3 DampedDisplacement;
            public Vector3 PreviousCameraPosition;
            
            public float FrustumHeight;
            public float AspectRatio;
        };
        
        ShapeCache m_ShapeCache; // internal for editor

        /// <summary>
        /// ShapeCache: contains all states that dependent only on the settings in the confiner.
        /// </summary>
        struct ShapeCache
        {
            public ConfinerOven ConfinerOven;
            public List<List<Vector2>> OriginalPath;  // in baked space, not including offset

            // These account for offset and transform change since baking
            public Matrix4x4 DeltaWorldToBaked; 
            public Matrix4x4 DeltaBakedToWorld;
            
            OversizeWindowSettings m_OversizeWindowSettings;
            internal float maxComputationTimePerFrameInSeconds;

            Matrix4x4 m_BakedToWorld; // defines baked space
            Collider2D m_BoundingShape2D;

            /// <summary>
            /// Invalidates shapeCache
            /// </summary>
            public void Invalidate()
            {
                m_OversizeWindowSettings = new ();
                DeltaBakedToWorld = DeltaWorldToBaked = Matrix4x4.identity;

                m_BoundingShape2D = null;
                OriginalPath = null;
                ConfinerOven = null;
            }

            /// <summary>
            /// Checks if we have a valid confiner state cache. Calculates cache if it is invalid (outdated or empty).
            /// </summary>
            /// <param name="boundingShape2D">Bounding shape</param>
            /// <param name="maxWindowSize">Max Window size (calculation upper bound)</param>
            /// <param name="aspectRatio">Aspect ratio</param>
            /// <param name="confinerStateChanged">True, if the baked confiner state has changed.
            /// False, otherwise.</param>
            /// <returns>True, if input is valid. False, otherwise.</returns>
            public bool ValidateCache(Collider2D boundingShape2D, OversizeWindowSettings oversize, float aspectRatio, 
                out bool confinerStateChanged)
            {
                confinerStateChanged = false;
                
                if (IsValid(boundingShape2D, oversize))
                {
                    // Advance confiner baking
                    if (ConfinerOven.State == ConfinerOven.BakingState.BAKING)
                    {
                        ConfinerOven.BakeConfiner(maxComputationTimePerFrameInSeconds);

                        // If no longer baking, then confinerStateChanged
                        confinerStateChanged = ConfinerOven.State != ConfinerOven.BakingState.BAKING;
                    }
                    
                    // Update in case the polygon's transform changed
                    CalculateDeltaTransformationMatrix();
                    
                    // If delta world to baked scale is uniform, cache is valid.
                    Vector2 lossyScaleXY = DeltaWorldToBaked.lossyScale;
                    if (lossyScaleXY.IsUniform())
                        return true;
                }
                
                Invalidate();
                if (boundingShape2D == null)
                    return false;
                
                confinerStateChanged = true;
                switch (boundingShape2D)
                {
                    case PolygonCollider2D polygonCollider2D:
                    {
                        OriginalPath = new List<List<Vector2>>();
                        
                        // Cache the current worldspace shape
                        m_BakedToWorld = boundingShape2D.transform.localToWorldMatrix;
                        for (var i = 0; i < polygonCollider2D.pathCount; ++i)
                        {
                            var path = polygonCollider2D.GetPath(i);
                            var dst = new List<Vector2>();
                            for (var j = 0; j < path.Length; ++j)
                                dst.Add(m_BakedToWorld.MultiplyPoint3x4(path[j]));
                            OriginalPath.Add(dst);
                        }
                    }
                        break;
                    case BoxCollider2D boxCollider2D:
                    {
                        // Cache the current worldspace shape
                        m_BakedToWorld = boundingShape2D.transform.localToWorldMatrix;
                        var size = boxCollider2D.size;
                        var halfY = size.y / 2f;
                        var halfX = size.x / 2f;
                        var topLeft = m_BakedToWorld.MultiplyPoint3x4(new Vector3(-halfX, halfY));
                        var topRight = m_BakedToWorld.MultiplyPoint3x4(new Vector3(halfX, halfY));
                        var btmRight = m_BakedToWorld.MultiplyPoint3x4(new Vector3(halfX, -halfY));
                        var btmLeft = m_BakedToWorld.MultiplyPoint3x4(new Vector3(-halfX, -halfY));

                        OriginalPath = new List<List<Vector2>>
                        {
                            new() { topLeft, topRight, btmRight, btmLeft }
                        };
                    }
                        break;
                    case CompositeCollider2D compositeCollider2D:
                    {
                        OriginalPath = new List<List<Vector2>>();

                        // Cache the current worldspace shape
                        m_BakedToWorld = boundingShape2D.transform.localToWorldMatrix;
                        var path = new Vector2[compositeCollider2D.pointCount];
                        for (var i = 0; i < compositeCollider2D.pathCount; ++i)
                        {
                            var numPoints = compositeCollider2D.GetPath(i, path);
                            var dst = new List<Vector2>();
                            for (var j = 0; j < numPoints; ++j)
                                dst.Add(m_BakedToWorld.MultiplyPoint3x4(path[j]));
                            OriginalPath.Add(dst);
                        }
                    }
                        break;
                    default:
                        return false;
                }

                ConfinerOven = new ConfinerOven(OriginalPath, aspectRatio, oversize.Enabled ? oversize.MaxWindowSize : -1);
                m_BoundingShape2D = boundingShape2D;
                m_OversizeWindowSettings = oversize;

                CalculateDeltaTransformationMatrix();

                return true;
            }

            bool IsValid(in Collider2D boundingShape2D, in OversizeWindowSettings oversize)
            {
                return boundingShape2D != null && m_BoundingShape2D != null && 
                       m_BoundingShape2D == boundingShape2D && // same boundingShape?
                       OriginalPath != null && // first time?
                       ConfinerOven != null && // cache not empty? 
                       m_OversizeWindowSettings.Enabled == oversize.Enabled && // max ortho changed?
                       Mathf.Abs(m_OversizeWindowSettings.MaxWindowSize - oversize.MaxWindowSize) < UnityVectorExtensions.Epsilon;
            }

            void CalculateDeltaTransformationMatrix()
            {
                // Account for current collider offset (in local space) and 
                // incorporate the worldspace delta that the confiner has moved since baking
                var m = Matrix4x4.Translate(-m_BoundingShape2D.offset) * 
                        m_BoundingShape2D.transform.worldToLocalMatrix;
                DeltaWorldToBaked = m_BakedToWorld * m;
                DeltaBakedToWorld = DeltaWorldToBaked.inverse;
            }
        }

#if UNITY_EDITOR
        // Used by editor gizmo drawer
        internal bool GetGizmoPaths(
            out List<List<Vector2>> originalPath,
            ref List<List<Vector2>> currentPath,
            out Matrix4x4 pathLocalToWorld)
        {
            originalPath = m_ShapeCache.OriginalPath;
            pathLocalToWorld = m_ShapeCache.DeltaBakedToWorld;
            currentPath.Clear();
            var allExtraStates = GetAllExtraStates<VcamExtraState>();
            for (var i = 0; i < allExtraStates.Count; ++i)
            {
                var e = allExtraStates[i];
                if (e.BakedSolution != null)
                {
                    currentPath.AddRange(e.BakedSolution.GetBakedPath());
                }
            }
            return originalPath != null;
        }

        // Used by editor script to notify user that the confiner cannot fit the camera
        internal bool IsCameraTooBigForTheConfiner(CinemachineVirtualCameraBase vcam)
        {
            InvalidateLensCacheIfNeeded(vcam);
            
            if (BoundingShape2D == null)
                return false;
            
            if (m_ShapeCache.ConfinerOven != null && m_ShapeCache.ConfinerOven.m_Skeleton.Count > 0)
                return true; // there is a skeleton, that means some parts are collapsed -> oversized
            
            if (vcam is CinemachineCameraManagerBase manager)
            {
                foreach (var childCamera in manager.ChildCameras)
                    if (CheckForOversized(GetExtraState<VcamExtraState>(childCamera), m_ShapeCache))
                        return true; // at least one child is oversized
            }
            else if (CheckForOversized(GetExtraState<VcamExtraState>(vcam), m_ShapeCache))
                return true; // oversized
            
            return false;
            
            // local function
            static bool CheckForOversized(VcamExtraState extra, ShapeCache shapeCache)
            {
                if (extra.BakedSolution != null)
                {
                    var solution = extra.BakedSolution.m_Solution;
                    if (solution.Count == 1 && solution[0].Count == 1)
                        return true; // shrank down to mid point -> oversized
                    if (shapeCache.OriginalPath != null && solution.Count != shapeCache.OriginalPath.Count)
                        return true; // polygon count of the input and solution differs -> oversized
                }
                return false;
            }
        }
        
        void InvalidateLensCacheIfNeeded(CinemachineVirtualCameraBase vcam)
        {
            if (vcam is CinemachineCameraManagerBase manager)
            {
                foreach (var childCamera in manager.ChildCameras)
                    if (IsLensCacheInvalid(childCamera.State, GetExtraState<VcamExtraState>(childCamera), m_ShapeCache))
                        InvalidateLensCache(childCamera);
            }
            else if (IsLensCacheInvalid(vcam.State, GetExtraState<VcamExtraState>(vcam), m_ShapeCache))
                InvalidateLensCache(vcam);
            
            // local function
            static bool IsLensCacheInvalid(CameraState state, VcamExtraState extra, ShapeCache shapeCache)
            {
                var lens = state.Lens;
                var deltaW = shapeCache.DeltaWorldToBaked;
                var frustum = 
                    CalculateHalfFrustumHeight(lens, deltaW.MultiplyPoint3x4(state.GetCorrectedPosition()).z);
                return Mathf.Abs(extra.AspectRatio - lens.Aspect) > UnityVectorExtensions.Epsilon ||
                    Mathf.Abs(extra.FrustumHeight - frustum * deltaW.lossyScale.x) > UnityVectorExtensions.Epsilon;
            }
        }

        internal float BakeProgress() => m_ShapeCache.ConfinerOven != null ? m_ShapeCache.ConfinerOven.bakeProgress : 0f;
        internal bool ConfinerOvenTimedOut() => m_ShapeCache.ConfinerOven != null && 
            m_ShapeCache.ConfinerOven.State == ConfinerOven.BakingState.TIMEOUT;
#endif
    }
}
#endif
