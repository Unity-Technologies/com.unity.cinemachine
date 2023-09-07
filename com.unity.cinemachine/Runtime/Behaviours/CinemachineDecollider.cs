#if CINEMACHINE_PHYSICS
using System;
using UnityEngine;

namespace Unity.Cinemachine
{
    /// <summary>
    /// An add-on module for CinemachineCamera that post-processes
    /// the final position of the camera. Based on the supplied settings,
    /// the Decollider will pull the camera out of any objects it is intersecting.
    /// Camera will be decollided in the direction of the Tracking Target, but otherwise
    /// no attempt will be make to preserve the line of sight.
    /// </summary>
    [AddComponentMenu("Cinemachine/Procedural/Extensions/Cinemachine Decollider")]
    [SaveDuringPlay]
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [HelpURL(Documentation.BaseURL + "manual/CinemachineDecollider.html")]
    public class CinemachineDecollider : CinemachineExtension
    {
        /// <summary>Objects on these layers will be detected.</summary>
        [Tooltip("Objects on these layers will be detected")]
        public LayerMask CollideAgainst = 1;

        /// <summary>Obstacles closer to the target than this will be ignored</summary>
        [Tooltip("Obstacles closer to the target than this will be ignored")]
        public float MinimumDistanceFromTarget = 0.1f;

        /// <summary>
        /// Camera will try to maintain this distance from any obstacle.
        /// Increase this value if you are seeing inside obstacles due to a large
        /// FOV on the camera.
        /// </summary>
        [Tooltip("Camera will try to maintain this distance from any obstacle.  Try to keep this value small.  "
            + "Increase it if you are seeing inside obstacles due to a large FOV on the camera.")]
        public float CameraRadius = 0.1f;

        /// <summary>
        /// Smoothing to apply to obstruction resolution.  Nearest camera point is held for at least this long.
        /// </summary>
        [Range(0, 2)]
        [Tooltip("Smoothing to apply to obstruction resolution.  Nearest camera point is held for at least this long")]
        public float SmoothingTime = 0.2f;

        /// <summary>
        /// How gradually the camera returns to its normal position after having been corrected.
        /// Higher numbers will move the camera more gradually back to normal.
        /// </summary>
        [Range(0, 10)]
        [Tooltip("How gradually the camera returns to its normal position after having been corrected.  "
            + "Higher numbers will move the camera more gradually back to normal.")]
        public float Damping = 0.2f;

        void OnValidate()
        {
            MinimumDistanceFromTarget = Mathf.Max(0.01f, MinimumDistanceFromTarget);
            CameraRadius = Mathf.Max(0.01f, CameraRadius);
            SmoothingTime = Mathf.Max(0, SmoothingTime);
            Damping = Mathf.Max(0, Damping);
        }

        void Reset()
        {
            CollideAgainst = 1;
            MinimumDistanceFromTarget = 0.2f;
            CameraRadius = 0.1f; 
            SmoothingTime = 0.2f;
            Damping = 0.2f;
        }
        
        /// <summary>
        /// Cleanup
        /// </summary>
        protected override void OnDestroy()
        {
            RuntimeUtility.DestroyScratchCollider();
            base.OnDestroy();
        }

        /// This must be small but greater than 0 - reduces false results due to precision
        const float k_PrecisionSlush = 0.001f;

        /// <summary>
        /// Per-vcam extra state info
        /// </summary>
        class VcamExtraState : VcamExtraStateBase
        {
            public Vector3 PreviousDisplacement;
            public Vector3 PreviousCameraOffset;
            public Vector3 PreviousCameraPosition;
            public float OcclusionStartTime;

            float m_SmoothedDistance;
            float m_SmoothedTime;

            public float ApplyDistanceSmoothing(float distance, float smoothingTime)
            {
                if (m_SmoothedTime != 0 && smoothingTime > Epsilon)
                {
                    float now = CinemachineCore.CurrentTime;
                    if (now - m_SmoothedTime < smoothingTime)
                        return Mathf.Min(distance, m_SmoothedDistance);
                }
                return distance;
            }
            public void UpdateDistanceSmoothing(float distance)
            {
                if (m_SmoothedDistance == 0 || distance < m_SmoothedDistance)
                {
                    m_SmoothedDistance = distance;
                    m_SmoothedTime = CinemachineCore.CurrentTime;
                }
            }
            public void ResetDistanceSmoothing(float smoothingTime)
            {
                float now = CinemachineCore.CurrentTime;
                if (now - m_SmoothedTime >= smoothingTime)
                    m_SmoothedDistance = m_SmoothedTime = 0;
            }
        };

        /// <summary>
        /// Report maximum damping time needed for this component.
        /// </summary>
        /// <returns>Highest damping setting in this component</returns>
        public override float GetMaxDampTime() 
        { 
            return Mathf.Max(Damping, SmoothingTime);
        }
        
        /// <inheritdoc />
        public override void OnTargetObjectWarped(
            CinemachineVirtualCameraBase vcam, Transform target, Vector3 positionDelta)
        {
            var extra = GetExtraState<VcamExtraState>(vcam);
            extra.PreviousCameraPosition += positionDelta;
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
                var initialCamPos = state.GetCorrectedPosition();

                // Rotate the previous collision correction along with the camera
                var dampingBypass = state.RotationDampingBypass;
                extra.PreviousDisplacement = dampingBypass * extra.PreviousDisplacement;

                // Resolve collisions
                var cameraPos = initialCamPos;
                var lookAt = state.HasLookAt() ? state.ReferenceLookAt : cameraPos;
                var displacement = RespectCameraRadius(cameraPos, lookAt);

                // Apply distance smoothing - this can artificially hold the camera closer
                // to the target for a while, to reduce popping in and out on bumpy objects
                if (SmoothingTime > Epsilon && state.HasLookAt())
                {
                    var pos = initialCamPos + displacement;
                    var dir = pos - state.ReferenceLookAt;
                    var distance = dir.magnitude;
                    if (distance > Epsilon)
                    {
                        dir /= distance;
                        if (!displacement.AlmostZero())
                            extra.UpdateDistanceSmoothing(distance);
                        distance = extra.ApplyDistanceSmoothing(distance, SmoothingTime);
                        displacement += (state.ReferenceLookAt + dir * distance) - pos;
                    }
                }
                    
                if (displacement.AlmostZero())
                    extra.ResetDistanceSmoothing(SmoothingTime);

                // Apply damping
                if (deltaTime >= 0 && vcam.PreviousStateIsValid && Damping > Epsilon)
                {
                    var dispSqrMag = displacement.sqrMagnitude;
                    if (dispSqrMag < extra.PreviousDisplacement.sqrMagnitude)
                    {
                        bool bodyAfterAim = false;
                        if (vcam is CinemachineCamera cam)
                        {
                            var body = cam.GetCinemachineComponent(CinemachineCore.Stage.Body);
                            bodyAfterAim = body != null && body.BodyAppliesAfterAim;
                        }
                        var prevDisplacement = bodyAfterAim ? extra.PreviousDisplacement
                            : lookAt + dampingBypass * extra.PreviousCameraOffset - initialCamPos;
                        displacement = prevDisplacement + Damper.Damp(displacement - prevDisplacement, Damping, deltaTime);
                    }
                }
                state.PositionCorrection += displacement;
                cameraPos = state.GetCorrectedPosition();

                // Adjust the damping bypass to account for the displacement
                if (state.HasLookAt() && vcam.PreviousStateIsValid)
                {
                    var dir0 = extra.PreviousCameraPosition - state.ReferenceLookAt;
                    var dir1 = cameraPos - state.ReferenceLookAt;
                    if (dir0.sqrMagnitude > Epsilon && dir1.sqrMagnitude > Epsilon)
                        state.RotationDampingBypass = UnityVectorExtensions.SafeFromToRotation(
                            dir0, dir1, state.ReferenceUp);
                }
                extra.PreviousDisplacement = displacement;
                extra.PreviousCameraOffset = cameraPos - lookAt;
                extra.PreviousCameraPosition = cameraPos;
            }
        }

        static Collider[] s_ColliderBuffer = new Collider[5];

        Vector3 RespectCameraRadius(Vector3 cameraPos, Vector3 lookAtPos)
        {
            Vector3 result = Vector3.zero;
            Vector3 dir = cameraPos - lookAtPos;
            float distance = dir.magnitude;
            if (distance > Epsilon)
                dir /= distance;

            // Pull it out of any intersecting obstacles
            RaycastHit hitInfo;
            int numObstacles = Physics.OverlapSphereNonAlloc(
                cameraPos, CameraRadius, s_ColliderBuffer,
                CollideAgainst, QueryTriggerInteraction.Ignore);
            if (numObstacles == 0 && distance > MinimumDistanceFromTarget + Epsilon)
            {
                // Make sure the camera position isn't completely inside an obstacle.
                // OverlapSphereNonAlloc won't catch those.
                float d = distance - MinimumDistanceFromTarget;
                Vector3 targetPos = lookAtPos + dir * MinimumDistanceFromTarget;
                if (RuntimeUtility.RaycastIgnoreTag(new Ray(targetPos, dir), 
                    out hitInfo, d, CollideAgainst, string.Empty))
                {
                    // Only count it if there's an incoming collision but not an outgoing one
                    Collider c = hitInfo.collider;
                    if (!c.Raycast(new Ray(cameraPos, -dir), out hitInfo, d))
                        s_ColliderBuffer[numObstacles++] = c;
                }
            }
            if (numObstacles > 0 && distance == 0 || distance > MinimumDistanceFromTarget)
            {
                var scratchCollider = RuntimeUtility.GetScratchCollider();
                scratchCollider.radius = CameraRadius;

                Vector3 newCamPos = cameraPos;
                for (int i = 0; i < numObstacles; ++i)
                {
                    Collider c = s_ColliderBuffer[i];

                    // If we have a lookAt target, move the camera to the nearest edge of obstacle
                    if (distance > MinimumDistanceFromTarget)
                    {
                        dir = newCamPos - lookAtPos;
                        float d = dir.magnitude;
                        if (d > Epsilon)
                        {
                            dir /= d;
                            var ray = new Ray(lookAtPos, dir);
                            if (c.Raycast(ray, out hitInfo, d + CameraRadius))
                                newCamPos = ray.GetPoint(hitInfo.distance) - (dir * k_PrecisionSlush);
                        }
                    }
                    if (Physics.ComputePenetration(
                        scratchCollider, newCamPos, Quaternion.identity,
                        c, c.transform.position, c.transform.rotation,
                        out var offsetDir, out var offsetDistance))
                    {
                        newCamPos += offsetDir * offsetDistance;
                    }
                }
                result = newCamPos - cameraPos;
            }

            // Respect the minimum distance from target - push camera back if we have to
            if (distance > Epsilon && MinimumDistanceFromTarget > Epsilon)
            {
                float minDistance = Mathf.Max(MinimumDistanceFromTarget, CameraRadius) + k_PrecisionSlush;
                Vector3 newOffset = cameraPos + result - lookAtPos;
                if (newOffset.magnitude < minDistance)
                    result = lookAtPos - cameraPos + dir * minDistance;
            }

            return result;
        }
    }
}
#endif
