#if CINEMACHINE_PHYSICS
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Cinemachine
{
    /// <summary>
    /// An add-on module for CinemachineCamera that post-processes
    /// the final position of the camera. Based on the supplied settings,
    /// the Decollider will pull the camera out of any objects it is intersecting.
    /// </summary>
    [AddComponentMenu("Cinemachine/Procedural/Extensions/Cinemachine Decollider")]
    [SaveDuringPlay]
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequiredTarget(RequiredTargetAttribute.RequiredTargets.Tracking)]
    [HelpURL(Documentation.BaseURL + "manual/CinemachineDecollider.html")]
    public class CinemachineDecollider : CinemachineExtension
    {
        /// <summary>
        /// Camera will try to maintain this distance from any obstacle or terrain.
        /// </summary>
        [Tooltip("Camera will try to maintain this distance from any obstacle or terrain.  Increase it "
            + "if necessary to keep the camera from clipping the near edge of obsacles.")]
        public float CameraRadius = 0.4f;

        /// <summary>Settings for pushing the camera out of intersecting objects</summary>
        [Serializable]
        public struct DecollisionSettings
        {
            /// <summary>When enabled, will attempt to push the camera out of intersecting objects</summary>
            [Tooltip("When enabled, will attempt to push the camera out of intersecting objects")]
            public bool Enabled;

            /// <summary>Objects on these layers will be detected.</summary>
            [Tooltip("Objects on these layers will be detected")]
            public LayerMask ObstacleLayers;

            /// <summary>Settings for resolving towards Follow target instead of LookAt.</summary>
            [Serializable]
            public struct FollowTargetSettings
            {
                /// <summary>Use the Follow target when resolving occlusions, instead of the LookAt target.</summary>
                [Tooltip("Use the Follow target when resolving occlusions, instead of the LookAt target.")]
                public bool Enabled;

                [Tooltip("Vertical offset from the Follow target's root, in target local space")]
                public float YOffset;
            }

            /// <summary>Use the Follow target when resolving occlusions, instead of the LookAt target.</summary>
            [EnabledProperty]
            public FollowTargetSettings UseFollowTarget;

            /// <summary>
            /// How gradually the camera returns to its normal position after having been corrected.
            /// Higher numbers will move the camera more gradually back to normal.
            /// </summary>
            [Range(0, 10)]
            [Tooltip("How gradually the camera returns to its normal position after having been corrected.  "
                + "Higher numbers will move the camera more gradually back to normal.")]
            public float Damping;

            /// <summary>
            /// Smoothing to apply to obstruction resolution.  Nearest camera point is held for at least this long.
            /// </summary>
            [Range(0, 2)]
            [Tooltip("Smoothing to apply to obstruction resolution.  Nearest camera point is held for at least this long")]
            public float SmoothingTime;
        }

        /// <summary>When enabled, will attempt to push the camera out of intersecting objects</summary>
        [FoldoutWithEnabledButton]
        public DecollisionSettings Decollision;

        /// <summary>Settings for putting the camera on top of the terrain</summary>
        [Serializable]
        public struct TerrainSettings
        {
            /// <summary>When enabled, will attempt to place the camera on top of terrain layers</summary>
            [Tooltip("When enabled, will attempt to place the camera on top of terrain layers")]
            public bool Enabled;

            /// <summary>Colliders on these layers will be detected.</summary>
            [Tooltip("Colliders on these layers will be detected")]
            public LayerMask TerrainLayers;

            /// <summary>Specifies the maximum length of a raycast used to find terrain colliders.</summary>
            [Tooltip("Specifies the maximum length of a raycast used to find terrain colliders")]
            public float MaximumRaycast;

            /// <summary>
            /// How gradually the camera returns to its normal position after having been corrected.
            /// Higher numbers will move the camera more gradually back to normal.
            /// </summary>
            [Range(0, 10)]
            [Tooltip("How gradually the camera returns to its normal position after having been corrected.  "
                + "Higher numbers will move the camera more gradually back to normal.")]
            public float Damping;
        }

        /// <summary>When enabled, will attempt to place the camera on top of terrain layers</summary>
        [FoldoutWithEnabledButton]
        public TerrainSettings TerrainResolution;

        const int kColliderBufferSize = 10;
        static Collider[] s_ColliderBuffer = new Collider[kColliderBufferSize];
        static float[] s_ColliderDistanceBuffer = new float[kColliderBufferSize];
        static int[] s_ColliderOrderBuffer = new int[kColliderBufferSize];

        // Farthest stuff comes first
        static readonly IComparer<int> s_ColliderBufferSorter = Comparer<int>.Create((a, b) =>
        {
            if (s_ColliderDistanceBuffer[a] == s_ColliderDistanceBuffer[b])
                return 0;
            return s_ColliderDistanceBuffer[a] > s_ColliderDistanceBuffer[b] ? -1 : 1;
        });

        void OnValidate()
        {
            CameraRadius = Mathf.Max(0.01f, CameraRadius);
        }

        void Reset()
        {
            CameraRadius = 0.4f;
            TerrainResolution = new () { Enabled = true, TerrainLayers = 1, MaximumRaycast = 10, Damping = 0.5f };
            Decollision = new () { Enabled = false, ObstacleLayers = 1, Damping = 0.5f };
        }

        /// <summary>Cleanup</summary>
        protected override void OnDestroy()
        {
            RuntimeUtility.DestroyScratchCollider();
            base.OnDestroy();
        }

        /// <summary>
        /// Report maximum damping time needed for this component.
        /// </summary>
        /// <returns>Highest damping setting in this component</returns>
        public override float GetMaxDampTime()
        {
            return Mathf.Max(
                Decollision.Enabled ? Decollision.Damping : 0,
                TerrainResolution.Enabled ? TerrainResolution.Damping : 0);
        }

        /// <summary>Per-vcam extra state info</summary>
        class VcamExtraState : VcamExtraStateBase
        {
            public float PreviousTerrainDisplacement;
            public float PreviousDistanceFromTarget;
            public Vector3 PreviouDecollisionDisplacement;
            public Vector3 PreviousCorrectedCameraPosition;

            float m_SmoothedDistance;
            float m_SmoothingStartTime;
            public float UpdateDistanceSmoothing(float distance, float smoothingTime, bool haveDisplacement)
            {
                if (haveDisplacement && (m_SmoothedDistance == 0 || distance <= m_SmoothedDistance))
                {
                    m_SmoothedDistance = distance;
                    m_SmoothingStartTime = CinemachineCore.CurrentTime;
                }

                if (m_SmoothingStartTime != 0 && CinemachineCore.CurrentTime - m_SmoothingStartTime < smoothingTime)
                    distance = Mathf.Min(distance, m_SmoothedDistance);

                if (!haveDisplacement && CinemachineCore.CurrentTime - m_SmoothingStartTime >= smoothingTime)
                    m_SmoothedDistance = m_SmoothingStartTime = 0;

                return distance;
            }
        };

        /// <summary>
        /// Force the virtual camera to assume a given position and orientation
        /// </summary>
        /// <param name="vcam">Virtual camera being warped warp</param>
        /// <param name="pos">World-space position to take</param>
        /// <param name="rot">World-space orientation to take</param>
        public override void ForceCameraPosition(CinemachineVirtualCameraBase vcam, Vector3 pos, Quaternion rot) 
        {
            var extra = GetExtraState<VcamExtraState>(vcam);
            extra.PreviousCorrectedCameraPosition = pos;
        }

        /// <summary>
        /// Callback to do the collision resolution and shot evaluation
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
                var extra = GetExtraState<VcamExtraState>(vcam);
                var up = state.ReferenceUp;
                var initialCamPos = state.GetCorrectedPosition();

                // Capture lookAt screen offset for composition preservation
                var hasLookAt = state.HasLookAt();
                var lookAtPoint = hasLookAt ? state.ReferenceLookAt : state.GetCorrectedPosition();
                var resolutionTargetPoint = GetAvoidanceResolutionTargetPoint(vcam, ref state);
                var lookAtScreenOffset = hasLookAt ? state.RawOrientation.GetCameraRotationToTarget(
                    lookAtPoint - initialCamPos, state.ReferenceUp) : Vector2.zero;

                if (!vcam.PreviousStateIsValid)
                    deltaTime = -1;

                // Resolve terrains
                extra.PreviousTerrainDisplacement = TerrainResolution.Enabled
                    ? ResolveTerrain(extra, state.GetCorrectedPosition(), up, deltaTime) : 0;
                state.PositionCorrection += extra.PreviousTerrainDisplacement * up;

                // Resolve collisions
                if (Decollision.Enabled)
                {
                    var oldCamPos = state.GetCorrectedPosition();
                    var displacement = DecollideCamera(oldCamPos, resolutionTargetPoint);
                    displacement = ApplySmoothingAndDamping(displacement, resolutionTargetPoint, oldCamPos, extra, deltaTime);
                    if (!displacement.AlmostZero())
                    {
                        state.PositionCorrection += displacement;

                        // Resolve terrains again, just in case the decollider messed it up.
                        // No damping this time.
                        var terrainDisplacement = TerrainResolution.Enabled
                            ? ResolveTerrain(extra, state.GetCorrectedPosition(), up, -1) : 0;
                        if (Mathf.Abs(terrainDisplacement) > Epsilon)
                        {
                            state.PositionCorrection += terrainDisplacement * up;
                            extra.PreviousTerrainDisplacement = 0;
                        }
                    }
                }

                // Restore screen composition
                var newCamPos = state.GetCorrectedPosition();
                if (hasLookAt && !(initialCamPos - newCamPos).AlmostZero())
                {
                    var q = Quaternion.LookRotation(lookAtPoint - newCamPos, up);
                    state.RawOrientation = q.ApplyCameraRotation(-lookAtScreenOffset, up);

                    if (deltaTime >= 0)
                    {
                        var dir0 = extra.PreviousCorrectedCameraPosition - lookAtPoint;
                        var dir1 = newCamPos - lookAtPoint;
                        if (dir0.sqrMagnitude > Epsilon && dir1.sqrMagnitude > Epsilon)
                            state.RotationDampingBypass = UnityVectorExtensions.SafeFromToRotation(dir0, dir1, up);
                    }
                }
                extra.PreviousCorrectedCameraPosition = newCamPos;
            }
        }

        Vector3 GetAvoidanceResolutionTargetPoint(
            CinemachineVirtualCameraBase vcam, ref CameraState state)
        {
            var resolutuionTargetPoint = state.HasLookAt() ? state.ReferenceLookAt : state.GetCorrectedPosition();
            if (Decollision.UseFollowTarget.Enabled)
            {
                var target = vcam.Follow;
                if (target != null)
                {
                    resolutuionTargetPoint = TargetPositionCache.GetTargetPosition(target)
                        + TargetPositionCache.GetTargetRotation(target) * Vector3.up * Decollision.UseFollowTarget.YOffset;
                }
            }
            return resolutuionTargetPoint;
        }

        // Returns distance to move the camera in the up directon to stay on top of terrain
        float ResolveTerrain(VcamExtraState extra, Vector3 camPos, Vector3 up, float deltaTime)
        {
            float displacement = 0;
            if (RuntimeUtility.SphereCastIgnoreTag(
                    new Ray(camPos + TerrainResolution.MaximumRaycast * up, -up),
                    CameraRadius + Epsilon, out var hitInfo,
                    TerrainResolution.MaximumRaycast, TerrainResolution.TerrainLayers, string.Empty))
            {
                displacement = TerrainResolution.MaximumRaycast - hitInfo.distance + Epsilon;
            }
            // Apply damping
            if (deltaTime >= 0 && TerrainResolution.Damping > Epsilon)
            {
                if (displacement < extra.PreviousTerrainDisplacement)
                    displacement = extra.PreviousTerrainDisplacement
                        + Damper.Damp(displacement - extra.PreviousTerrainDisplacement,
                            TerrainResolution.Damping, deltaTime);
            }
            return displacement;
        }

        Vector3 DecollideCamera(Vector3 cameraPos, Vector3 lookAtPoint)
        {
            // Don't handle layers already taken care of by terrain resolution
            var layers = Decollision.ObstacleLayers;
            if (TerrainResolution.Enabled)
                layers &= ~TerrainResolution.TerrainLayers;
            if (layers == 0)
                return Vector3.zero;

            // Detect any intersecting obstacles
            var dir = cameraPos - lookAtPoint;
            var capsuleLength = dir.magnitude;
            if (capsuleLength < Epsilon)
                return Vector3.zero;
            int numObstacles = Physics.OverlapCapsuleNonAlloc(
                lookAtPoint, cameraPos, CameraRadius - Epsilon, s_ColliderBuffer,
                layers, QueryTriggerInteraction.Ignore);
            if (numObstacles == 0)
                return Vector3.zero;

            dir /= capsuleLength; // normalize

            // Sort the colliders fartherst-to-nearest
            for (int i = 0; i < numObstacles; ++i)
            {
                var c = s_ColliderBuffer[i];
                s_ColliderOrderBuffer[i] = i;
                s_ColliderDistanceBuffer[i] = 0; // if raycast fails then target is inside collider - we will ignore those colliders
                if (c.Raycast(new Ray(lookAtPoint, dir), out var hitInfo, capsuleLength + CameraRadius))
                {
                    var distance = hitInfo.distance - CameraRadius;
                    if (distance < CameraRadius)
                        distance = Mathf.Max(0.01f, distance + (CameraRadius - distance) * 0.5f);
                    s_ColliderDistanceBuffer[i] = distance;
                }
            }
            Array.Sort(s_ColliderOrderBuffer, 0, numObstacles, s_ColliderBufferSorter);

            // Move camera in front of any overlapping obstacles
            var newCamPos = cameraPos;
            var scratchCollider = RuntimeUtility.GetScratchCollider();
            scratchCollider.radius = CameraRadius - Epsilon;
            for (int i = 0; i < numObstacles; ++i)
            {
                var index = s_ColliderOrderBuffer[i];
                if (s_ColliderDistanceBuffer[index] == 0)
                    continue; // ignore colliders that are on the target
                var c = s_ColliderBuffer[index];
                if (Physics.ComputePenetration(
                    scratchCollider, newCamPos, Quaternion.identity,
                    c, c.transform.position, c.transform.rotation,
                    out var _, out var _))
                {
                    // Camera overlaps - move it in front
                    newCamPos = lookAtPoint + dir * s_ColliderDistanceBuffer[index];
                }
            }
            return newCamPos - cameraPos;
        }

        Vector3 ApplySmoothingAndDamping(
            Vector3 displacement, Vector3 lookAtPoint,
            Vector3 oldCamPos, VcamExtraState extra, float deltaTime)
        {
            var newOffset = oldCamPos + displacement - lookAtPoint;
            var newOffsetMag = float.MaxValue;
            if (deltaTime >= 0)
            {
                newOffsetMag = newOffset.magnitude;
                if (newOffsetMag > CameraRadius)
                {
                    // Apply smoothing
                    var newOffsetDir = newOffset / newOffsetMag;
                    if (Decollision.SmoothingTime > Epsilon)
                    {
                        newOffsetMag = extra.UpdateDistanceSmoothing(newOffsetMag, Decollision.SmoothingTime, !displacement.AlmostZero());
                        displacement = (lookAtPoint + newOffsetDir * newOffsetMag) - oldCamPos;
                    }

                    // Apply damping
                    if (Decollision.Damping > Epsilon && newOffsetMag > extra.PreviousDistanceFromTarget)
                    {
                        // Avoid introducing spurious damping when the camera changed position relative to the target.
                        // We calculate the previous offset from target in two ways, and take the one that's closest
                        // to the current desired offset.
                        var prevOffsetMag = extra.PreviousDistanceFromTarget;
                        var prevOffsetMag2 = (oldCamPos - lookAtPoint).magnitude - extra.PreviouDecollisionDisplacement.magnitude;
                        if (Mathf.Abs(newOffsetMag - prevOffsetMag2) < Mathf.Abs(newOffsetMag - prevOffsetMag))
                            prevOffsetMag = prevOffsetMag2;

                        newOffsetMag = prevOffsetMag + Damper.Damp(newOffsetMag - prevOffsetMag, Decollision.Damping, deltaTime);
                        displacement = (lookAtPoint + newOffsetDir * newOffsetMag) - oldCamPos;
                    }
                }
            }
            extra.PreviousDistanceFromTarget = newOffsetMag;
            extra.PreviouDecollisionDisplacement = displacement;
            return displacement;
        }
    }
}
#endif
