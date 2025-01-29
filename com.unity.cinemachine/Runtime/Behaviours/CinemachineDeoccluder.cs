#if CINEMACHINE_PHYSICS

using UnityEngine;
using System.Collections.Generic;
using System;

namespace Unity.Cinemachine
{
    /// <summary>
    /// An add-on module for CinemachineCamera that post-processes
    /// the final position of the camera. Based on the supplied settings,
    /// the Deoccluder will attempt to preserve the line of sight
    /// with the LookAt target of the camera by moving
    /// away from objects that will obstruct the view.
    ///
    /// Additionally, the Deoccluder can be used to assess the shot quality and
    /// report this as a field in the camera State.
    /// </summary>
    [AddComponentMenu("Cinemachine/Procedural/Extensions/Cinemachine Deoccluder")]
    [SaveDuringPlay]
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequiredTarget(RequiredTargetAttribute.RequiredTargets.Tracking)]
    [HelpURL(Documentation.BaseURL + "manual/CinemachineDeoccluder.html")]
    public class CinemachineDeoccluder : CinemachineExtension, IShotQualityEvaluator
    {
        /// <summary>Objects on these layers will be detected.</summary>
        [Tooltip("Objects on these layers will be detected")]
        public LayerMask CollideAgainst = 1;

        /// <summary>Obstacles with this tag will be ignored.  It is a good idea to set this field to the target's tag</summary>
        [TagField]
        [Tooltip("Obstacles with this tag will be ignored.  It is a good idea to set this field to the target's tag")]
        public string IgnoreTag = string.Empty;

        /// <summary>Objects on these layers will never obstruct view of the target.</summary>
        [Tooltip("Objects on these layers will never obstruct view of the target")]
        public LayerMask TransparentLayers = 0;

        /// <summary>Obstacles closer to the target than this will be ignored</summary>
        [Tooltip("Obstacles closer to the target than this will be ignored")]
        public float MinimumDistanceFromTarget = 0.3f;

        /// <summary>Settings for deoccluding the camera when obstacles are present</summary>
        [Serializable]
        public struct ObstacleAvoidance
        {
            /// <summary>
            /// When enabled, will attempt to resolve situations where the line of sight to the
            /// target is blocked by an obstacle
            /// </summary>
            [Tooltip("When enabled, will attempt to resolve situations where the line of sight "
                + "to the target is blocked by an obstacle")]
            public bool Enabled;

            /// <summary>
            /// The raycast distance to test for when checking if the line of sight to this camera's target is clear.
            /// </summary>
            [Tooltip("The maximum raycast distance when checking if the line of sight to this camera's target is clear.  "
                + "If the setting is 0 or less, the current actual distance to target will be used.")]
            public float DistanceLimit;

            /// <summary>
            /// Don't take action unless occlusion has lasted at least this long.
            /// </summary>
            [Tooltip("Don't take action unless occlusion has lasted at least this long.")]
            public float MinimumOcclusionTime;

            /// <summary>
            /// Camera will try to maintain this distance from any obstacle.
            /// Increase this value if you are seeing inside obstacles due to a large
            /// FOV on the camera.
            /// </summary>
            [Tooltip("Camera will try to maintain this distance from any obstacle.  Try to keep this value small.  "
                + "Increase it if you are seeing inside obstacles due to a large FOV on the camera.")]
            public float CameraRadius;

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
            
            /// <summary>The way in which the Deoccluder will attempt to preserve sight of the target.</summary>
            public enum ResolutionStrategy
            {
                /// <summary>Camera will be pulled forward along its Z axis until it is in front of
                /// the nearest obstacle</summary>
                PullCameraForward,
                /// <summary>In addition to pulling the camera forward, an effort will be made to
                /// return the camera to its original height</summary>
                PreserveCameraHeight,
                /// <summary>In addition to pulling the camera forward, an effort will be made to
                /// return the camera to its original distance from the target</summary>
                PreserveCameraDistance
            };
            /// <summary>The way in which the Deoccluder will attempt to preserve sight of the target.</summary>
            [Tooltip("The way in which the Deoccluder will attempt to preserve sight of the target.")]
            public ResolutionStrategy Strategy;

            /// <summary>
            /// Upper limit on how many obstacle hits to process.  Higher numbers may impact performance.
            /// In most environments, 4 is enough.
            /// </summary>
            [Range(1, 10)]
            [Tooltip("Upper limit on how many obstacle hits to process.  Higher numbers may impact performance.  "
                + "In most environments, 4 is enough.")]
            public int MaximumEffort;

            /// <summary>
            /// Smoothing to apply to obstruction resolution.  Nearest camera point is held for at least this long.
            /// </summary>
            [Range(0, 2)]
            [Tooltip("Smoothing to apply to obstruction resolution.  Nearest camera point is held for at least this long")]
            public float SmoothingTime;

            /// <summary>
            /// How gradually the camera returns to its normal position after having been corrected.
            /// Higher numbers will move the camera more gradually back to normal.
            /// </summary>
            [Range(0, 10)]
            [Tooltip("How gradually the camera returns to its normal position after having been corrected.  "
                + "Higher numbers will move the camera more gradually back to normal.")]
            public float Damping;

            /// <summary>
            /// How gradually the camera moves to resolve an occlusion.
            /// Higher numbers will move the camera more gradually.
            /// </summary>
            [Range(0, 10)]
            [Tooltip("How gradually the camera moves to resolve an occlusion.  "
                + "Higher numbers will move the camera more gradually.")]
            public float DampingWhenOccluded;

            internal static ObstacleAvoidance Default => new () 
            { 
                Enabled = true,
                DistanceLimit = 0,
                MinimumOcclusionTime = 0,
                CameraRadius = 0.4f,
                Strategy = ResolutionStrategy.PullCameraForward,
                MaximumEffort = 4,
                SmoothingTime = 0,
                Damping = 0.4f,
                DampingWhenOccluded = 0.2f
            };
        }

        /// <summary>Settings for deoccluding the camera when obstacles are present</summary>
        [FoldoutWithEnabledButton]
        public ObstacleAvoidance AvoidObstacles;

        /// <summary>Settings for shot quality evaluation</summary>
        [Serializable]
        public struct QualityEvaluation
        {
            /// <summary>If enabled, will evaluate shot quality based on target distance and occlusion</summary>
            [Tooltip("If enabled, will evaluate shot quality based on target distance and occlusion")]
            public bool Enabled;

            /// <summary>If greater than zero, maximum quality boost will occur when target is this far from the camera</summary>
            [Tooltip("If greater than zero, maximum quality boost will occur when target is this far from the camera")]
            public float OptimalDistance;

            /// <summary>Shots with targets closer to the camera than this will not get a quality boost</summary>
            [Tooltip("Shots with targets closer to the camera than this will not get a quality boost")]
            public float NearLimit;

            /// <summary>Shots with targets farther from the camera than this will not get a quality boost</summary>
            [Tooltip("Shots with targets farther from the camera than this will not get a quality boost")]
            public float FarLimit;

            /// <summary>High quality shots will be boosted by this fraction of their normal quality</summary>
            [Tooltip("High quality shots will be boosted by this fraction of their normal quality")]
            public float MaxQualityBoost;

            internal static QualityEvaluation Default => new () { NearLimit = 5, FarLimit = 30, OptimalDistance = 10, MaxQualityBoost = 0.2f };
        }
        /// <summary>If enabled, will evaluate shot quality based on target distance and occlusion</summary>
        [FoldoutWithEnabledButton]
        public QualityEvaluation ShotQualityEvaluation = QualityEvaluation.Default;

        List<VcamExtraState> m_extraStateCache;

        /// <summary>See whether an object is blocking the camera's view of the target</summary>
        /// <param name="vcam">The virtual camera in question.  This might be different from the
        /// virtual camera that owns the deoccluder, in the event that the camera has children</param>
        /// <returns>True if something is blocking the view</returns>
        public bool IsTargetObscured(CinemachineVirtualCameraBase vcam)
        {
            return GetExtraState<VcamExtraState>(vcam).TargetObscured;
        }

        /// <summary>See whether the virtual camera has been moved nby the collider</summary>
        /// <param name="vcam">The virtual camera in question.  This might be different from the
        /// virtual camera that owns the deoccluder, in the event that the camera has children</param>
        /// <returns>True if the virtual camera has been displaced due to collision or
        /// target obstruction</returns>
        public bool CameraWasDisplaced(CinemachineVirtualCameraBase vcam)
        {
            return GetCameraDisplacementDistance(vcam) > 0;
        }

        /// <summary>See how far the virtual camera wa moved nby the collider</summary>
        /// <param name="vcam">The virtual camera in question.  This might be different from the
        /// virtual camera that owns the deoccluder, in the event that the camera has children</param>
        /// <returns>True if the virtual camera has been displaced due to collision or
        /// target obstruction</returns>
        public float GetCameraDisplacementDistance(CinemachineVirtualCameraBase vcam)
        {
            return GetExtraState<VcamExtraState>(vcam).PreviousDisplacement.magnitude;
        }

        void OnValidate()
        {
            AvoidObstacles.DistanceLimit = Mathf.Max(0, AvoidObstacles.DistanceLimit);
            AvoidObstacles.MinimumOcclusionTime = Mathf.Max(0, AvoidObstacles.MinimumOcclusionTime);
            AvoidObstacles.CameraRadius = Mathf.Max(0, AvoidObstacles.CameraRadius);
            MinimumDistanceFromTarget = Mathf.Max(0.01f, MinimumDistanceFromTarget);
            ShotQualityEvaluation.NearLimit = Mathf.Max(0.1f, ShotQualityEvaluation.NearLimit);
            ShotQualityEvaluation.FarLimit = Mathf.Max(ShotQualityEvaluation.NearLimit, ShotQualityEvaluation.FarLimit);
            ShotQualityEvaluation.OptimalDistance = Mathf.Clamp(
                ShotQualityEvaluation.OptimalDistance, ShotQualityEvaluation.NearLimit, ShotQualityEvaluation.FarLimit);
        }

        private void Reset()
        {
            CollideAgainst = 1;
            IgnoreTag = string.Empty;
            TransparentLayers = 0;
            MinimumDistanceFromTarget = 0.3f;
            AvoidObstacles = ObstacleAvoidance.Default;
            ShotQualityEvaluation = QualityEvaluation.Default;
        }

        /// <summary>Cleanup</summary>
        protected override void OnDestroy()
        {
            RuntimeUtility.DestroyScratchCollider();
            base.OnDestroy();
        }

        /// <summary>Called ehn the behaviour is enabled</summary>
        protected override void OnEnable() 
        {
            base.OnEnable();
            var states = new List<VcamExtraState>();
            GetAllExtraStates(states);
            for (int i = 0; i < states.Count; ++i)
                states[i].StateIsValid = false;
        }

        /// This must be small but greater than 0 - reduces false results due to precision
        const float k_PrecisionSlush = 0.001f;

        /// <summary>
        /// Per-vcam extra state info
        /// </summary>
        class VcamExtraState : VcamExtraStateBase
        {
            public Vector3 PreviousDisplacement;
            public bool TargetObscured;
            public float OcclusionStartTime;
            public List<Vector3> DebugResolutionPath;
            public List<Collider> OccludingObjects;
            public Vector3 PreviousCameraOffset;
            public Vector3 PreviousCameraPosition;
            public float PreviousDampTime;
            public bool StateIsValid;

            public void AddPointToDebugPath(Vector3 p, Collider c)
            {
#if UNITY_EDITOR
                DebugResolutionPath ??= new ();
                DebugResolutionPath.Add(p);
                OccludingObjects ??= new ();
                OccludingObjects.Add(c);
#endif
            }

            // Thanks to Sebastien LeTouze from Exiin Studio for the smoothing idea
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

        /// <summary>Debug API for discovering which objects are occluding the camera,
        /// and the path taken by the camera to ist deoccluded position.  Note that
        /// this information is only collected while running in the editor.  In the build, the
        /// return values will always be empty.  This is for performance reasons.</summary>
        /// <param name="paths">A container to hold lists of points representing the camera path.  
        /// There will be one path per CinemachineCamera influenced by this deoccluder.
        /// This parameter may be null.</param>
        /// <param name="obstacles">A container to hold lists of Colliders representing the obstacles encountered.  
        /// There will be one list per CinemachineCamera influenced by this deoccluder.
        /// This parameter may be null.</param>
        public void DebugCollisionPaths(List<List<Vector3>> paths, List<List<Collider>> obstacles)
        {
            paths?.Clear();
            obstacles?.Clear();
            m_extraStateCache ??= new();
            GetAllExtraStates(m_extraStateCache);
            for (int i = 0; i < m_extraStateCache.Count; ++i)
            {
                var e = m_extraStateCache[i];
                if (e.DebugResolutionPath != null && e.DebugResolutionPath.Count > 0)
                {
                    paths?.Add(e.DebugResolutionPath);
                    obstacles?.Add(e.OccludingObjects);
                }
            }
        }

        /// <summary>
        /// Report maximum damping time needed for this component.
        /// </summary>
        /// <returns>Highest damping setting in this component</returns>
        public override float GetMaxDampTime() 
        { 
            return AvoidObstacles.Enabled 
                ? Mathf.Max(AvoidObstacles.Damping, Mathf.Max(AvoidObstacles.DampingWhenOccluded, AvoidObstacles.SmoothingTime)) 
                : 0; 
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
                extra.TargetObscured = false;
                extra.DebugResolutionPath?.Clear();
                extra.OccludingObjects?.Clear();
            
                if (!AvoidObstacles.Enabled)
                    extra.StateIsValid = false;
                else
                {
                    var initialCamPos = state.GetCorrectedPosition();
                    var up = state.ReferenceUp;
                    var hasLookAt = state.HasLookAt();
                    var lookAtPoint = hasLookAt ? state.ReferenceLookAt : state.GetCorrectedPosition();
                    var hasResolutionTarget = GetAvoidanceResolutionTargetPoint(vcam, ref state, out var resolutionTargetPoint);
                    var lookAtScreenOffset = hasLookAt ? state.RawOrientation.GetCameraRotationToTarget(
                        lookAtPoint - initialCamPos, up) : Vector2.zero;

                    // Rotate the previous collision correction along with the camera
                    var dampingBypass = state.RotationDampingBypass;
                    if (extra.StateIsValid)
                        extra.PreviousDisplacement = dampingBypass * extra.PreviousDisplacement;

                    // Calculate the desired collision correction
                    var displacement = hasResolutionTarget 
                        ? PreserveLineOfSight(ref state, ref extra, resolutionTargetPoint) : Vector3.zero;
                    if (AvoidObstacles.MinimumOcclusionTime > Epsilon)
                    {
                        // If minimum occlusion time set, ignore new occlusions until they've lasted long enough
                        var now = CinemachineCore.CurrentTime;
                        if (displacement.AlmostZero())
                            extra.OcclusionStartTime = 0; // no occlusion
                        else
                        {
                            if (extra.OcclusionStartTime <= 0)
                                extra.OcclusionStartTime = now; // occlusion timer starts now
                            if (extra.StateIsValid && now - extra.OcclusionStartTime < AvoidObstacles.MinimumOcclusionTime)
                                displacement = extra.PreviousDisplacement;
                        }
                    }

                    // Apply distance smoothing - this can artificially hold the camera closer
                    // to the target for a while, to reduce popping in and out on bumpy objects
                    if (hasResolutionTarget && AvoidObstacles.SmoothingTime > Epsilon)
                    {
                        var pos = initialCamPos + displacement;
                        var dir = pos - resolutionTargetPoint;
                        var distance = dir.magnitude;
                        if (distance > Epsilon)
                        {
                            dir /= distance;
                            if (!displacement.AlmostZero())
                                extra.UpdateDistanceSmoothing(distance);
                            distance = extra.ApplyDistanceSmoothing(distance, AvoidObstacles.SmoothingTime);
                            displacement += (resolutionTargetPoint + dir * distance) - pos;
                        }
                    }
                    
                    if (displacement.AlmostZero())
                        extra.ResetDistanceSmoothing(AvoidObstacles.SmoothingTime);

                    // Apply additional correction due to camera radius
                    var newCamPos = initialCamPos + displacement;
                    if (AvoidObstacles.Strategy != ObstacleAvoidance.ResolutionStrategy.PullCameraForward)
                        displacement += RespectCameraRadius(newCamPos, resolutionTargetPoint);

                    // Apply damping
                    float dampTime = AvoidObstacles.DampingWhenOccluded;
                    if (deltaTime >= 0 && vcam.PreviousStateIsValid && extra.StateIsValid
                        && AvoidObstacles.DampingWhenOccluded + AvoidObstacles.Damping > Epsilon)
                    {
                        // To ease the transition between damped and undamped regions, we damp the damp time
                        var dispSqrMag = displacement.sqrMagnitude;
                        dampTime = dispSqrMag > extra.PreviousDisplacement.sqrMagnitude 
                            ? AvoidObstacles.DampingWhenOccluded : AvoidObstacles.Damping;
                        if (dispSqrMag < Epsilon)
                            dampTime = extra.PreviousDampTime - Damper.Damp(extra.PreviousDampTime, dampTime, deltaTime);

                        var prevDisplacement = resolutionTargetPoint + dampingBypass * extra.PreviousCameraOffset - initialCamPos;
                        displacement = prevDisplacement + Damper.Damp(displacement - prevDisplacement, dampTime, deltaTime);
                    }
                    
                    state.PositionCorrection += displacement;
                    newCamPos = state.GetCorrectedPosition();

                    // Adjust the damping bypass to account for the displacement
                    if (hasLookAt && displacement.sqrMagnitude > Epsilon)
                    {
                        // Restore the lookAt offset
                        var q = Quaternion.LookRotation(lookAtPoint - newCamPos, up);
                        state.RawOrientation = q.ApplyCameraRotation(-lookAtScreenOffset, up);
                        if (vcam.PreviousStateIsValid && extra.StateIsValid)
                        {
                            var dir0 = extra.PreviousCameraPosition - lookAtPoint;
                            var dir1 = newCamPos - lookAtPoint;
                            if (dir0.sqrMagnitude > Epsilon && dir1.sqrMagnitude > Epsilon)
                                state.RotationDampingBypass = UnityVectorExtensions.SafeFromToRotation(dir0, dir1, up);
                        }
                    }

                    extra.PreviousDisplacement = displacement;
                    extra.PreviousCameraOffset = newCamPos - resolutionTargetPoint;
                    extra.PreviousCameraPosition = newCamPos;
                    extra.PreviousDampTime = dampTime;
                    extra.StateIsValid = true;
                }
            }
            // Rate the shot after the aim was set
            if (stage == CinemachineCore.Stage.Finalize && ShotQualityEvaluation.Enabled && state.HasLookAt())
            {
                var extra = GetExtraState<VcamExtraState>(vcam);
                extra.TargetObscured = state.IsTargetOffscreen() || IsTargetObscured(state);

                if (extra.TargetObscured)
                    state.ShotQuality *= 0.2f;
                if (extra.StateIsValid && !extra.PreviousDisplacement.AlmostZero())
                    state.ShotQuality *= 0.8f;

                float nearnessBoost = 0;
                if (ShotQualityEvaluation.OptimalDistance > 0)
                {
                    var distance = Vector3.Magnitude(state.ReferenceLookAt - state.GetFinalPosition());
                    if (distance <= ShotQualityEvaluation.OptimalDistance)
                    {
                        if (distance >= ShotQualityEvaluation.NearLimit)
                            nearnessBoost = ShotQualityEvaluation.MaxQualityBoost * (distance - ShotQualityEvaluation.NearLimit)
                                / (ShotQualityEvaluation.OptimalDistance - ShotQualityEvaluation.NearLimit);
                    }
                    else
                    {
                        distance -= ShotQualityEvaluation.OptimalDistance;
                        if (distance < ShotQualityEvaluation.FarLimit)
                            nearnessBoost = ShotQualityEvaluation.MaxQualityBoost * (1f - (distance / ShotQualityEvaluation.FarLimit));
                    }
                    state.ShotQuality *= (1f + nearnessBoost);
                }
            }
        }
        
        bool GetAvoidanceResolutionTargetPoint(
            CinemachineVirtualCameraBase vcam, ref CameraState state, out Vector3 resolutuionTargetPoint)
        {
            var hasResolutionPoint = state.HasLookAt();
            resolutuionTargetPoint = hasResolutionPoint ? state.ReferenceLookAt : state.GetCorrectedPosition();
            if (AvoidObstacles.UseFollowTarget.Enabled)
            {
                var target = vcam.Follow;
                if (target != null)
                {
                    hasResolutionPoint = true;
                    resolutuionTargetPoint = TargetPositionCache.GetTargetPosition(target)
                        + TargetPositionCache.GetTargetRotation(target) * Vector3.up * AvoidObstacles.UseFollowTarget.YOffset;
                }
            }
            return hasResolutionPoint;
        }
        
        Vector3 PreserveLineOfSight(ref CameraState state, ref VcamExtraState extra, Vector3 lookAtPoint)
        {
            if (CollideAgainst != 0 && CollideAgainst != TransparentLayers)
            {
                var cameraPos = state.GetCorrectedPosition();
                var hitInfo = new RaycastHit();
                var newPos = PullCameraInFrontOfNearestObstacle(
                    cameraPos, lookAtPoint, CollideAgainst & ~TransparentLayers, ref hitInfo);
                if (hitInfo.collider != null)
                {
                    extra.AddPointToDebugPath(newPos, hitInfo.collider);
                    if (AvoidObstacles.Strategy != ObstacleAvoidance.ResolutionStrategy.PullCameraForward)
                    {
                        Vector3 targetToCamera = cameraPos - lookAtPoint;
                        newPos = PushCameraBack(
                            newPos, targetToCamera, hitInfo, lookAtPoint,
                            new Plane(state.ReferenceUp, cameraPos),
                            targetToCamera.magnitude, AvoidObstacles.MaximumEffort, ref extra);
                    }
                }
                return newPos - cameraPos;
            }
            return Vector3.zero;
        }

        Vector3 PullCameraInFrontOfNearestObstacle(
            Vector3 cameraPos, Vector3 lookAtPos, int layerMask, ref RaycastHit hitInfo)
        {
            var newPos = cameraPos;
            var dir = cameraPos - lookAtPos;
            var targetDistance = dir.magnitude;
            if (targetDistance > Epsilon)
            {
                dir /= targetDistance;
                var minDistance = MinimumDistanceFromTarget + AvoidObstacles.CameraRadius + k_PrecisionSlush;
                if (targetDistance > minDistance)
                {
                    // Make a ray that looks towards the camera, to get the obstacle closest to target
                    var rayLength = Mathf.Max(targetDistance - minDistance - AvoidObstacles.CameraRadius, k_PrecisionSlush);
                    if (AvoidObstacles.DistanceLimit > Epsilon)
                        rayLength = Mathf.Min(AvoidObstacles.DistanceLimit, rayLength);
                    if (RuntimeUtility.SphereCastIgnoreTag(
                        new Ray(lookAtPos + dir * minDistance, dir), 
                        AvoidObstacles.CameraRadius, out hitInfo, rayLength, layerMask, IgnoreTag))
                    {
                        newPos = hitInfo.point + hitInfo.normal * (AvoidObstacles.CameraRadius + k_PrecisionSlush);
                    }

                    // Respect the minimum distance from target - push camera back if we have to
                    if ((lookAtPos - newPos).sqrMagnitude < minDistance * minDistance)
                        newPos = lookAtPos + dir * minDistance;
                }
            }
            return newPos;
        }

        Vector3 PushCameraBack(
            Vector3 currentPos, Vector3 pushDir, RaycastHit obstacle,
            Vector3 lookAtPos, Plane startPlane, float targetDistance, int iterations,
            ref VcamExtraState extra)
        {
            // Take a step along the wall.
            var pos = currentPos;
            var dir = Vector3.zero;
            if (obstacle.collider == null || !GetWalkingDirection(pos, pushDir, obstacle, ref dir))
                return pos;

            Ray ray = new Ray(pos, dir);
            float distance = GetPushBackDistance(ray, startPlane, targetDistance, lookAtPos);
            if (distance <= Epsilon)
                return pos;

            // Check only as far as the obstacle bounds
            float clampedDistance = ClampRayToBounds(ray, distance, obstacle.collider.bounds);
            distance = Mathf.Min(distance, clampedDistance + k_PrecisionSlush);

            if (RuntimeUtility.SphereCastIgnoreTag(
                ray, AvoidObstacles.CameraRadius, out var hitInfo, distance, 
                CollideAgainst & ~TransparentLayers, IgnoreTag))
            {
                // We hit something.  Stop there and take a step along that wall.
                var adjustment = hitInfo.distance - k_PrecisionSlush;
                pos = ray.GetPoint(adjustment);
                extra.AddPointToDebugPath(pos, hitInfo.collider);
                if (iterations > 1)
                    pos = PushCameraBack(
                        pos, dir, hitInfo,
                        lookAtPos, startPlane,
                        targetDistance, iterations-1, ref extra);
                return pos;
            }

            // Didn't hit anything.  Can we push back all the way now?
            pos = ray.GetPoint(distance);

            // First check if we can still see the target.  If not, abort
            dir = pos - lookAtPos;
            var d = dir.magnitude;
            if (d < Epsilon || RuntimeUtility.SphereCastIgnoreTag(
                    new Ray(lookAtPos, dir), AvoidObstacles.CameraRadius, out _, d - k_PrecisionSlush, 
                        CollideAgainst & ~TransparentLayers, IgnoreTag))
                return currentPos;

            // All clear
            ray = new Ray(pos, dir);
            extra.AddPointToDebugPath(pos, null);
            distance = GetPushBackDistance(ray, startPlane, targetDistance, lookAtPos);
            if (distance > Epsilon)
            {
                if (!RuntimeUtility.SphereCastIgnoreTag(
                    ray, AvoidObstacles.CameraRadius, out hitInfo, distance, 
                    CollideAgainst & ~TransparentLayers, IgnoreTag))
                {
                    pos = ray.GetPoint(distance); // no obstacles - all good
                    extra.AddPointToDebugPath(pos, null);
                }
                else
                {
                    // We hit something.  Stop there and maybe take a step along that wall
                    float adjustment = hitInfo.distance - k_PrecisionSlush;
                    pos = ray.GetPoint(adjustment);
                    extra.AddPointToDebugPath(pos, hitInfo.collider);
                    if (iterations > 1)
                        pos = PushCameraBack(
                            pos, dir, hitInfo, lookAtPos, startPlane,
                            targetDistance, iterations-1, ref extra);
                }
            }
            return pos;
        }

        RaycastHit[] m_CornerBuffer = new RaycastHit[4];

        bool GetWalkingDirection(
            Vector3 pos, Vector3 pushDir, RaycastHit obstacle, ref Vector3 outDir)
        {
            var normal2 = obstacle.normal;

            // Check for nearby obstacles.  Are we in a corner?
            var nearbyDistance = k_PrecisionSlush * 5;
            int numFound = Physics.SphereCastNonAlloc(
                pos, nearbyDistance, pushDir.normalized, m_CornerBuffer, 0,
                CollideAgainst & ~TransparentLayers, QueryTriggerInteraction.Ignore);
            if (numFound > 1)
            {
                // Calculate the second normal
                for (int i = 0; i < numFound; ++i)
                {
                    if (m_CornerBuffer[i].collider == null)
                        continue;
                    if (IgnoreTag.Length > 0 && m_CornerBuffer[i].collider.CompareTag(IgnoreTag))
                        continue;
                    Type type = m_CornerBuffer[i].collider.GetType();
                    if (type == typeof(BoxCollider)
                        || type == typeof(SphereCollider)
                        || type == typeof(CapsuleCollider))
                    {
                        var p = m_CornerBuffer[i].collider.ClosestPoint(pos);
                        var d = p - pos;
                        if (d.magnitude > Vector3.kEpsilon)
                        {
                            if (m_CornerBuffer[i].collider.Raycast(
                                new Ray(pos, d), out m_CornerBuffer[i], nearbyDistance))
                            {
                                if (!(m_CornerBuffer[i].normal - obstacle.normal).AlmostZero())
                                    normal2 = m_CornerBuffer[i].normal;
                                break;
                            }
                        }
                    }
                }
            }

            // Walk along the wall.  If we're in a corner, walk their intersecting line
            var dir = Vector3.Cross(obstacle.normal, normal2);
            if (dir.AlmostZero())
                dir = Vector3.ProjectOnPlane(pushDir, obstacle.normal);
            else
            {
                var dot = Vector3.Dot(dir, pushDir);
                if (Mathf.Abs(dot) < Epsilon)
                    return false;
                if (dot < 0)
                    dir = -dir;
            }
            if (dir.AlmostZero())
                return false;

            outDir = dir.normalized;
            return true;
        }

        const float k_AngleThreshold = 0.1f;
        float GetPushBackDistance(Ray ray, Plane startPlane, float targetDistance, Vector3 lookAtPos)
        {
            var maxDistance = targetDistance - (ray.origin - lookAtPos).magnitude;
            if (maxDistance < Epsilon)
                return 0;
            if (AvoidObstacles.Strategy == ObstacleAvoidance.ResolutionStrategy.PreserveCameraDistance)
                return maxDistance;

            if (!startPlane.Raycast(ray, out var distance))
                distance = 0;
            distance = Mathf.Min(maxDistance, distance);
            if (distance < Epsilon)
                return 0;

            // If we are close to parallel to the plane, we have to take special action
            var angle = Mathf.Abs(UnityVectorExtensions.Angle(startPlane.normal, ray.direction) - 90);
            distance = Mathf.Lerp(0, distance, angle / k_AngleThreshold);
            return distance;
        }

        static float ClampRayToBounds(Ray ray, float distance, Bounds bounds)
        {
            float d;
            if (Vector3.Dot(ray.direction, Vector3.up) > 0)
            {
                if (new Plane(Vector3.down, bounds.max).Raycast(ray, out d) && d > Epsilon)
                    distance = Mathf.Min(distance, d);
            }
            else if (Vector3.Dot(ray.direction, Vector3.down) > 0)
            {
                if (new Plane(Vector3.up, bounds.min).Raycast(ray, out d) && d > Epsilon)
                    distance = Mathf.Min(distance, d);
            }

            if (Vector3.Dot(ray.direction, Vector3.right) > 0)
            {
                if (new Plane(Vector3.left, bounds.max).Raycast(ray, out d) && d > Epsilon)
                    distance = Mathf.Min(distance, d);
            }
            else if (Vector3.Dot(ray.direction, Vector3.left) > 0)
            {
                if (new Plane(Vector3.right, bounds.min).Raycast(ray, out d) && d > Epsilon)
                    distance = Mathf.Min(distance, d);
            }

            if (Vector3.Dot(ray.direction, Vector3.forward) > 0)
            {
                if (new Plane(Vector3.back, bounds.max).Raycast(ray, out d) && d > Epsilon)
                    distance = Mathf.Min(distance, d);
            }
            else if (Vector3.Dot(ray.direction, Vector3.back) > 0)
            {
                if (new Plane(Vector3.forward, bounds.min).Raycast(ray, out d) && d > Epsilon)
                    distance = Mathf.Min(distance, d);
            }
            return distance;
        }

        static Collider[] s_ColliderBuffer = new Collider[5];

        Vector3 RespectCameraRadius(Vector3 cameraPos, Vector3 lookAtPos)
        {
            var result = Vector3.zero;
            if (AvoidObstacles.CameraRadius < Epsilon || CollideAgainst == 0)
                return result;

            var dir = cameraPos - lookAtPos;
            var distance = dir.magnitude;
            if (distance > Epsilon)
                dir /= distance;

            // Pull it out of any intersecting obstacles
            RaycastHit hitInfo;
            int numObstacles = Physics.OverlapSphereNonAlloc(
                cameraPos, AvoidObstacles.CameraRadius, s_ColliderBuffer,
                CollideAgainst, QueryTriggerInteraction.Ignore);
            if (numObstacles == 0 && TransparentLayers != 0
                && distance > MinimumDistanceFromTarget + Epsilon)
            {
                // Make sure the camera position isn't completely inside an obstacle.
                // OverlapSphereNonAlloc won't catch those.
                float d = distance - MinimumDistanceFromTarget;
                Vector3 targetPos = lookAtPos + dir * MinimumDistanceFromTarget;
                if (RuntimeUtility.SphereCastIgnoreTag(
                    new Ray(targetPos, dir), AvoidObstacles.CameraRadius,
                    out hitInfo, d, CollideAgainst, IgnoreTag))
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
                scratchCollider.radius = AvoidObstacles.CameraRadius;

                var newCamPos = cameraPos;
                for (int i = 0; i < numObstacles; ++i)
                {
                    var c = s_ColliderBuffer[i];
                    if (IgnoreTag.Length > 0 && c.CompareTag(IgnoreTag))
                        continue;

                    // If we have a lookAt target, move the camera to the nearest edge of obstacle
                    if (distance > MinimumDistanceFromTarget)
                    {
                        dir = newCamPos - lookAtPos;
                        var d = dir.magnitude;
                        if (d > Epsilon)
                        {
                            dir /= d;
                            var ray = new Ray(lookAtPos, dir);
                            if (c.Raycast(ray, out hitInfo, d + AvoidObstacles.CameraRadius))
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
                var minDistance = Mathf.Max(MinimumDistanceFromTarget, AvoidObstacles.CameraRadius) + k_PrecisionSlush;
                var newOffset = cameraPos + result - lookAtPos;
                if (newOffset.magnitude < minDistance)
                    result = lookAtPos - cameraPos + dir * minDistance;
            }

            return result;
        }

        bool IsTargetObscured(CameraState state)
        {
            if (state.HasLookAt())
            {
                var lookAtPos = state.ReferenceLookAt;
                var pos = state.GetCorrectedPosition();
                var dir = lookAtPos - pos;
                var distance = dir.magnitude;
                if (distance < Mathf.Max(MinimumDistanceFromTarget, Epsilon))
                    return true;
                var ray = new Ray(pos, dir.normalized);
                if (RuntimeUtility.SphereCastIgnoreTag(
                        ray, AvoidObstacles.CameraRadius, out _,
                        distance - MinimumDistanceFromTarget,
                        CollideAgainst & ~TransparentLayers, IgnoreTag))
                    return true;
            }
            return false;
        }
    }
}
#endif
