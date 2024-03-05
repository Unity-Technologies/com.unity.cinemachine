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
        public float CameraRadius = 0.1f;

        /// <summary>
        /// Smoothing to apply to camera movement.  Largest correction is held for at least this long.
        /// </summary>
        [Range(0, 2)]
        [Tooltip("Smoothing to apply to camera movement.  Largest correction is held for at least this long")]
        public float SmoothingTime = 0f;

        /// <summary>
        /// How gradually the camera returns to its normal position after having been corrected.
        /// Higher numbers will move the camera more gradually back to normal.
        /// </summary>
        [Range(0, 10)]
        [Tooltip("How gradually the camera returns to its normal position after having been corrected.  "
            + "Higher numbers will move the camera more gradually back to normal.")]
        public float Damping = 0.2f;

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
            CameraRadius = 0.1f; 
            SmoothingTime = 0f;
            Damping = 0.2f;
        }
        
        /// <summary>
        /// Per-vcam extra state info
        /// </summary>
        class VcamExtraState : VcamExtraStateBase
        {
            public float PreviousDisplacement;

            float m_SmoothedDistance;
            float m_SmoothedTime;

            public float ApplyDistanceSmoothing(float distance, float smoothingTime)
            {
                float now = CinemachineCore.CurrentTime;
                if (m_SmoothedDistance == 0 || distance > m_SmoothedDistance)
                {
                    m_SmoothedDistance = distance;
                    m_SmoothedTime = now;
                }
                if (now - m_SmoothedTime >= smoothingTime)
                {
                    m_SmoothedDistance = m_SmoothedTime = 0;
                    return distance;
                }
                return Mathf.Min(distance, m_SmoothedDistance);
            }

            public void ResetDistanceSmoothing() => m_SmoothedDistance = m_SmoothedTime = 0;
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
                var hasLookAt = state.HasLookAt();
                var up = state.ReferenceUp;
                var initialCamPos = state.GetCorrectedPosition();
                var lookAtScreenOffset = hasLookAt ? state.RawOrientation.GetCameraRotationToTarget(
                    state.ReferenceLookAt - initialCamPos, up) : Vector2.zero;

                float displacement = 0;
                if (RuntimeUtility.SphereCastIgnoreTag(
                        new Ray(initialCamPos + MaximumRaycast * up, -up), 
                        CameraRadius + k_PrecisionSlush,
                        out var hitInfo, MaximumRaycast, TerrainLayers, string.Empty))
                {
                    displacement = MaximumRaycast - hitInfo.distance;
                }

                // Apply distance smoothing - this can artificially hold the camera away
                // from the terrain for a while, to reduce popping in and out on bumpy objects
                if (!vcam.PreviousStateIsValid || deltaTime < 0)
                    extra.ResetDistanceSmoothing();
                else if (SmoothingTime > Epsilon)
                    displacement = extra.ApplyDistanceSmoothing(displacement, SmoothingTime);

                // Apply damping
                if (deltaTime >= 0 && vcam.PreviousStateIsValid && Damping > Epsilon)
                {
                    if (displacement < extra.PreviousDisplacement)
                        displacement = extra.PreviousDisplacement 
                            + Damper.Damp(displacement - extra.PreviousDisplacement, Damping, deltaTime);
                }

                state.PositionCorrection += displacement * up;
                extra.PreviousDisplacement = displacement;

                // Restore the lookAt offset
                if (hasLookAt && displacement > Epsilon)
                {
                    var q = Quaternion.LookRotation(state.ReferenceLookAt - state.GetCorrectedPosition(), up);
                    state.RawOrientation = q.ApplyCameraRotation(-lookAtScreenOffset, up);
                }
            }
        }
    }
}
#endif
