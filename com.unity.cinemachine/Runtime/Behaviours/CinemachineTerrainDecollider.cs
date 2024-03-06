#if CINEMACHINE_PHYSICS
using System;
using UnityEngine;

namespace Unity.Cinemachine
{
    /// <summary>
    /// An add-on module for CinemachineCamera that post-processes
    /// the final position of the camera. Based on the supplied settings,
    /// the Terrain Decollider will prevent the camera from going below the specified terrain
    /// collision layers, by projecting down from above the current camera location.
    /// </summary>
    [AddComponentMenu("Cinemachine/Procedural/Extensions/Cinemachine Terrain Decollider")]
    [SaveDuringPlay]
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [HelpURL(Documentation.BaseURL + "manual/CinemachineTerrainDecollider.html")]
    public class CinemachineTerrainDecollider : CinemachineExtension
    {
        /// <summary>Objects on these layers will be detected.</summary>
        [Tooltip("Objects on these layers will be detected")]
        public LayerMask TerrainLayers = 1;

        /// <summary>Specifies the maximum length of a raycast.</summary>
        [Tooltip("Specifies the maximum length of a raycast")]
        public float MaximumRaycast = 10;

        /// <summary>
        /// Camera will try to maintain this distance from any obstacle.
        /// Increase this value if you are seeing inside obstacles due to a large
        /// FOV on the camera.
        /// </summary>
        [Tooltip("Camera will try to maintain this distance from any obstacle.  Try to keep this value small.  "
            + "Increase it if you are seeing inside obstacles due to a large FOV on the camera.")]
        public float CameraRadius = 0.4f;

        /// <summary>
        /// Smoothing to apply to camera movement.  Largest correction is held for at least this long.
        /// </summary>
        [Range(0, 2)]
        [Tooltip("Smoothing to apply to camera movement.  Largest correction is held for at least this long")]
        public float SmoothingTime = 0.5f;

        /// <summary>
        /// How gradually the camera returns to its normal position after having been corrected.
        /// Higher numbers will move the camera more gradually back to normal.
        /// </summary>
        [Range(0, 10)]
        [Tooltip("How gradually the camera returns to its normal position after having been corrected.  "
            + "Higher numbers will move the camera more gradually back to normal.")]
        public float Damping = 0.5f;

        /// <summary>
        /// Re-adjust the aim to preserve the screen position
        /// of the LookAt target as much as possible
        /// </summary>
        [Tooltip("Re-adjust the aim to preserve the screen position of the LookAt target as much as possible")]
        public bool PreserveComposition = true;


        /// This must be small but greater than 0 - reduces false results due to precision
        const float k_PrecisionSlush = 0.001f;

        void OnValidate()
        {
            MaximumRaycast = Mathf.Max(1, MaximumRaycast);
            CameraRadius = Mathf.Max(0.01f, CameraRadius);
            SmoothingTime = Mathf.Max(0, SmoothingTime);
            Damping = Mathf.Max(0, Damping);
        }

        void Reset()
        {
            TerrainLayers = 1;
            MaximumRaycast = 10;
            CameraRadius = 0.4f; 
            SmoothingTime = 0.5f;
            Damping = 0.5f;
            PreserveComposition = true;
        }
        
        /// <summary>
        /// Per-vcam extra state info
        /// </summary>
        class VcamExtraState : VcamExtraStateBase
        {
            public float PreviousTerrainDisplacement;
            public Vector3 PreviousCorrectedCameraPosition;

            float m_TerrainSmoothedDistance;
            float m_TerrainSmoothedTime;

            public float ApplyTerrainDistanceSmoothing(float distance, float smoothingTime)
            {
                float now = CinemachineCore.CurrentTime;
                if (m_TerrainSmoothedDistance == 0 || distance > m_TerrainSmoothedDistance)
                {
                    m_TerrainSmoothedDistance = distance;
                    m_TerrainSmoothedTime = now;
                }
                if (now - m_TerrainSmoothedTime >= smoothingTime)
                {
                    m_TerrainSmoothedDistance = m_TerrainSmoothedTime = 0;
                    return distance;
                }
                return Mathf.Max(distance, m_TerrainSmoothedDistance);
            }

            public void ResetTerrainDistanceSmoothing() => m_TerrainSmoothedDistance = m_TerrainSmoothedTime = 0;
        };

        /// <summary>
        /// Report maximum damping time needed for this component.
        /// </summary>
        /// <returns>Highest damping setting in this component</returns>
        public override float GetMaxDampTime() => Mathf.Max(Damping, SmoothingTime);
        
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
                var lookAtPoint = preserveLookAt ? state.ReferenceLookAt : initialCamPos + state.RawOrientation * Vector3.forward;
                var lookAtScreenOffset = preserveLookAt ? state.RawOrientation.GetCameraRotationToTarget(
                    lookAtPoint - initialCamPos, state.ReferenceUp) : Vector2.zero;

                if (!vcam.PreviousStateIsValid)
                    deltaTime = -1;

                // Resolve terrains
                extra.PreviousTerrainDisplacement = ResolveTerrain(extra, initialCamPos, up, deltaTime);
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
                    new Ray(camPos + MaximumRaycast * up, -up), 
                    CameraRadius + k_PrecisionSlush,
                    out var hitInfo, MaximumRaycast, TerrainLayers, string.Empty))
            {
                displacement = MaximumRaycast - hitInfo.distance;
            }

            // Apply distance smoothing - this can artificially hold the camera away
            // from the terrain for a while, to reduce popping in and out on bumpy objects
            if (deltaTime < 0)
                extra.ResetTerrainDistanceSmoothing();
            else if (SmoothingTime > Epsilon)
                displacement = extra.ApplyTerrainDistanceSmoothing(displacement, SmoothingTime);

            // Apply damping
            if (deltaTime >= 0 && Damping > Epsilon)
            {
                if (displacement < extra.PreviousTerrainDisplacement)
                    displacement = extra.PreviousTerrainDisplacement 
                        + Damper.Damp(displacement - extra.PreviousTerrainDisplacement, Damping, deltaTime);
            }
            return displacement;
        }
    }
}
#endif
