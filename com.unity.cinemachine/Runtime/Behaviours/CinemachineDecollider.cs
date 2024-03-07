#if CINEMACHINE_PHYSICS
using System;
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
    [HelpURL(Documentation.BaseURL + "manual/CinemachineDecollider.html")]
    public class CinemachineDecollider : CinemachineExtension
    {
        /// <summary>
        /// Camera will try to maintain this distance from any obstacle or terrain.
        /// </summary>
        [Tooltip("Camera will try to maintain this distance from any obstacle or terrain.  Increase it "
            + "if necessary to keep the camera from clipping the near edge of obsacles.")]
        public float CameraRadius = 0.1f;

        /// <summary>
        /// Re-adjust the aim to preserve the screen position
        /// of the LookAt target as much as possible
        /// </summary>
        [Tooltip("Re-adjust the aim to preserve the screen position of the LookAt target as much as possible")]
        public bool PreserveComposition = true;

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

        void OnValidate()
        {
            CameraRadius = Mathf.Max(0.01f, CameraRadius);
        }

        void Reset()
        {
            CameraRadius = 0.4f; 
            PreserveComposition = true;
            TerrainResolution = new () { Enabled = true, TerrainLayers = 1, MaximumRaycast = 10, Damping = 0.5f };
            Decollision = new () { Enabled = false, ObstacleLayers = 0 };
        }
        
        /// <summary>Cleanup</summary>
        protected override void OnDestroy()
        {
            RuntimeUtility.DestroyScratchCollider();
            base.OnDestroy();
        }

        /// <summary>Per-vcam extra state info</summary>
        class VcamExtraState : VcamExtraStateBase
        {
            public float PreviousTerrainDisplacement;
            public Vector3 PreviousCorrectedCameraPosition;
        };

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
                var preserveLookAt = PreserveComposition && state.HasLookAt();
                var lookAtPoint = state.HasLookAt() ? state.ReferenceLookAt : initialCamPos;
                var lookAtScreenOffset = preserveLookAt ? state.RawOrientation.GetCameraRotationToTarget(
                    lookAtPoint - initialCamPos, state.ReferenceUp) : Vector2.zero;

                if (!vcam.PreviousStateIsValid)
                    deltaTime = -1;

                // Resolve collisions
                if (Decollision.Enabled && vcam.PreviousStateIsValid)
                    state.PositionCorrection += DecollideCamera(initialCamPos, extra.PreviousCorrectedCameraPosition);
                
                // Resolve terrains
                extra.PreviousTerrainDisplacement = TerrainResolution.Enabled 
                    ? ResolveTerrain(extra, state.GetCorrectedPosition(), up, deltaTime) : 0;
                state.PositionCorrection += extra.PreviousTerrainDisplacement * up;

                // Restore screen composition
                var newCamPos = state.GetCorrectedPosition();
                if (preserveLookAt && !(initialCamPos - newCamPos).AlmostZero())
                {
                    var q = Quaternion.LookRotation(lookAtPoint - newCamPos, up);
                    state.RawOrientation = q.ApplyCameraRotation(-lookAtScreenOffset, up);

                    if (vcam.PreviousStateIsValid)
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
 

        static Collider[] s_ColliderBuffer = new Collider[5];

        Vector3 DecollideCamera(Vector3 cameraPos, Vector3 previousCamPos)
        {
            // Don't handle layers already taken care of by terrain resolution
            var layers = Decollision.ObstacleLayers;
            if (TerrainResolution.Enabled)
                layers &= ~TerrainResolution.TerrainLayers;
            if (layers == 0)
                return Vector3.zero;

            Vector3 newCamPos = cameraPos;

            // Detect any intersecting obstacles
            int numObstacles = Physics.OverlapSphereNonAlloc(
                cameraPos, CameraRadius - Epsilon, s_ColliderBuffer,
                Decollision.ObstacleLayers, QueryTriggerInteraction.Ignore);

            // Make sure the camera position isn't completely inside an obstacle.
            // OverlapSphereNonAlloc won't catch those.
            if (numObstacles == 0)
            {
                Vector3 dir = cameraPos - previousCamPos;
                float distance = dir.magnitude;
                if (distance > Epsilon)
                {
                    dir /= distance;
                    distance += CameraRadius;
                    if (Physics.Raycast(
                        new Ray(previousCamPos, dir), out var hitInfo, distance, 
                        Decollision.ObstacleLayers, QueryTriggerInteraction.Ignore))
                    {
                        // Only count it if there's an incoming collision but not an outgoing one
                        Collider c = hitInfo.collider;
                        if (!c.Raycast(new Ray(cameraPos, -dir), out hitInfo, distance))
                        {
                            s_ColliderBuffer[numObstacles++] = c;
                            //newCamPos = previousCamPos + dir * Mathf.Max(0, hitInfo.distance - CameraRadius - Epsilon);
                        }
                    }
                }
            }
#if true
            if (numObstacles > 0)
            {
                Vector3 dir = cameraPos - previousCamPos;
                float distance = dir.magnitude;
                if (distance > Epsilon)
                {
                    dir /= distance;
                    if (RuntimeUtility.SphereCastIgnoreTag(
                        new Ray(previousCamPos, dir), CameraRadius, out var hitInfo, distance, 
                        Decollision.ObstacleLayers, string.Empty))
                    {
                        newCamPos = previousCamPos + dir * Mathf.Max(0, hitInfo.distance - Epsilon);
                    }
                }                
            }
#else
            if (numObstacles > 0)
            {
                var scratchCollider = RuntimeUtility.GetScratchCollider();
                scratchCollider.radius = CameraRadius;
                for (int i = 0; i < numObstacles; ++i)
                {
                    var c = s_ColliderBuffer[i];
                    if (Physics.ComputePenetration(
                        scratchCollider, newCamPos, Quaternion.identity,
                        c, c.transform.position, c.transform.rotation,
                        out var offsetDir, out var offsetDistance))
                    {
                        newCamPos += offsetDir * (offsetDistance + Epsilon);
                    }
                }
            }

            // In case we pulled it out of one obstacle and into another, check again.
            // If it's still intersecting something, move it back where it came from.
            if (newCamPos != cameraPos)
            {
                var dir = newCamPos - previousCamPos;
                var distance = dir.magnitude + CameraRadius;
                if (RuntimeUtility.RaycastIgnoreTag(new Ray(previousCamPos, dir), 
                    out var hitInfo, distance, Decollision.ObstacleLayers, string.Empty))
                {
                    newCamPos = previousCamPos + dir * Mathf.Max(0, hitInfo.distance - CameraRadius - Epsilon);
                }
            }
#endif
            return newCamPos - cameraPos;
        }
    }
}
#endif
