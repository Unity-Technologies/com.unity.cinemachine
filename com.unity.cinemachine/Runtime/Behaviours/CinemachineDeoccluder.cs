#if CINEMACHINE_PHYSICS

using UnityEngine;
using System.Collections.Generic;
using Cinemachine.Utility;
using UnityEngine.Serialization;
using System;

namespace Cinemachine
{
    /// <summary>
    /// An add-on module for Cinemachine Virtual Camera that post-processes
    /// the final position of the virtual camera. Based on the supplied settings,
    /// the Deoccluder will attempt to preserve the line of sight
    /// with the LookAt target of the virtual camera by moving
    /// away from objects that will obstruct the view.
    ///
    /// Additionally, the Deoccluder can be used to assess the shot quality and
    /// report this as a field in the camera State.
    /// </summary>
    [AddComponentMenu("Cinemachine/Procedural/Extensions/Cinemachine Deoccluder")]
    [SaveDuringPlay]
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [HelpURL(Documentation.BaseURL + "manual/CinemachineDeoccluder.html")]
    public class CinemachineDeoccluder : CinemachineExtension, IShotQualityEvaluator
    {
        /// <summary>Objects on these layers will be detected.</summary>
        [Header("Obstacle Detection")]
        [Tooltip("Objects on these layers will be detected")]
        [FormerlySerializedAs("m_CollideAgainst")]
        public LayerMask CollideAgainst = 1;

        /// <summary>Obstacles with this tag will be ignored.  It is a good idea to set this field to the target's tag</summary>
        [TagField]
        [Tooltip("Obstacles with this tag will be ignored.  It is a good idea to set this field to the target's tag")]
        [FormerlySerializedAs("m_IgnoreTag")]
        public string IgnoreTag = string.Empty;

        /// <summary>Objects on these layers will never obstruct view of the target.</summary>
        [Tooltip("Objects on these layers will never obstruct view of the target")]
        [FormerlySerializedAs("m_TransparentLayers")]
        public LayerMask TransparentLayers = 0;

        /// <summary>Obstacles closer to the target than this will be ignored</summary>
        [Tooltip("Obstacles closer to the target than this will be ignored")]
        [FormerlySerializedAs("m_MinimumDistanceFromTarget")]
        public float MinimumDistanceFromTarget = 0.1f;

        /// <summary>
        /// When enabled, will attempt to resolve situations where the line of sight to the
        /// target is blocked by an obstacle
        /// </summary>
        [Space]
        [Tooltip("When enabled, will attempt to resolve situations where the line of sight "
            + "to the target is blocked by an obstacle")]
        [FormerlySerializedAs("m_PreserveLineOfSight")]
        [FormerlySerializedAs("m_AvoidObstacles")]
        public bool AvoidObstacles = true;

        /// <summary>
        /// The raycast distance to test for when checking if the line of sight to this camera's target is clear.
        /// </summary>
        [Tooltip("The maximum raycast distance when checking if the line of sight to this camera's target is clear.  "
            + "If the setting is 0 or less, the current actual distance to target will be used.")]
        [FormerlySerializedAs("m_LineOfSightFeelerDistance")]
        [FormerlySerializedAs("m_DistanceLimit")]
        public float DistanceLimit;

        /// <summary>
        /// Don't take action unless occlusion has lasted at least this long.
        /// </summary>
        [Tooltip("Don't take action unless occlusion has lasted at least this long.")]
        [FormerlySerializedAs("m_MinimumOcclusionTime")]
        public float MinimumOcclusionTime;

        /// <summary>
        /// Camera will try to maintain this distance from any obstacle.
        /// Increase this value if you are seeing inside obstacles due to a large
        /// FOV on the camera.
        /// </summary>
        [Tooltip("Camera will try to maintain this distance from any obstacle.  Try to keep this value small.  "
            + "Increase it if you are seeing inside obstacles due to a large FOV on the camera.")]
        [FormerlySerializedAs("m_CameraRadius")]
        public float CameraRadius = 0.1f;

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
        [FormerlySerializedAs("m_Strategy")]
        public ResolutionStrategy Strategy = ResolutionStrategy.PreserveCameraHeight;

        /// <summary>
        /// Upper limit on how many obstacle hits to process.  Higher numbers may impact performance.
        /// In most environments, 4 is enough.
        /// </summary>
        [RangeSlider(1, 10)]
        [Tooltip("Upper limit on how many obstacle hits to process.  Higher numbers may impact performance.  "
            + "In most environments, 4 is enough.")]
        [FormerlySerializedAs("m_MaximumEffort")]
        public int MaximumEffort = 4;

        /// <summary>
        /// Smoothing to apply to obstruction resolution.  Nearest camera point is held for at least this long.
        /// </summary>
        [RangeSlider(0, 2)]
        [Tooltip("Smoothing to apply to obstruction resolution.  Nearest camera point is held for at least this long")]
        [FormerlySerializedAs("m_SmoothingTime")]
        public float SmoothingTime;

        /// <summary>
        /// How gradually the camera returns to its normal position after having been corrected.
        /// Higher numbers will move the camera more gradually back to normal.
        /// </summary>
        [RangeSlider(0, 10)]
        [Tooltip("How gradually the camera returns to its normal position after having been corrected.  "
            + "Higher numbers will move the camera more gradually back to normal.")]
        [FormerlySerializedAs("m_Smoothing")]
        [FormerlySerializedAs("m_Damping")]
        public float Damping;

        /// <summary>
        /// How gradually the camera moves to resolve an occlusion.
        /// Higher numbers will move the camera more gradually.
        /// </summary>
        [RangeSlider(0, 10)]
        [Tooltip("How gradually the camera moves to resolve an occlusion.  "
            + "Higher numbers will move the camera more gradually.")]
        [FormerlySerializedAs("m_DampingWhenOccluded")]
        public float DampingWhenOccluded;

        /// <summary>If greater than zero, a higher score will be given to shots when the target is closer to
        /// this distance.  Set this to zero to disable this feature</summary>
        [Header("Shot Evaluation")]
        [Tooltip("If greater than zero, a higher score will be given to shots when the target is closer to this distance.  "
            + "Set this to zero to disable this feature.")]
        [FormerlySerializedAs("m_OptimalTargetDistance")]
        public float OptimalTargetDistance;

        /// <summary>See whether an object is blocking the camera's view of the target</summary>
        /// <param name="vcam">The virtual camera in question.  This might be different from the
        /// virtual camera that owns the collider, in the event that the camera has children</param>
        /// <returns>True if something is blocking the view</returns>
        public bool IsTargetObscured(ICinemachineCamera vcam)
        {
            return GetExtraState<VcamExtraState>(vcam).TargetObscured;
        }

        /// <summary>See whether the virtual camera has been moved nby the collider</summary>
        /// <param name="vcam">The virtual camera in question.  This might be different from the
        /// virtual camera that owns the collider, in the event that the camera has children</param>
        /// <returns>True if the virtual camera has been displaced due to collision or
        /// target obstruction</returns>
        public bool CameraWasDisplaced(ICinemachineCamera vcam)
        {
            return GetCameraDisplacementDistance(vcam) > 0;
        }

        /// <summary>See how far the virtual camera wa moved nby the collider</summary>
        /// <param name="vcam">The virtual camera in question.  This might be different from the
        /// virtual camera that owns the collider, in the event that the camera has children</param>
        /// <returns>True if the virtual camera has been displaced due to collision or
        /// target obstruction</returns>
        public float GetCameraDisplacementDistance(ICinemachineCamera vcam)
        {
            return GetExtraState<VcamExtraState>(vcam).PreviousDisplacement.magnitude;
        }

        void OnValidate()
        {
            DistanceLimit = Mathf.Max(0, DistanceLimit);
            MinimumOcclusionTime = Mathf.Max(0, MinimumOcclusionTime);
            CameraRadius = Mathf.Max(0, CameraRadius);
            MinimumDistanceFromTarget = Mathf.Max(0.01f, MinimumDistanceFromTarget);
            OptimalTargetDistance = Mathf.Max(0, OptimalTargetDistance);
        }

        private void Reset()
        {
            CollideAgainst = 1;
            IgnoreTag = string.Empty;
            TransparentLayers = 0;
            MinimumDistanceFromTarget = 0.1f;
            AvoidObstacles = true;
            DistanceLimit = 0;
            MinimumOcclusionTime = 0;
            CameraRadius = 0.1f;
            Strategy = ResolutionStrategy.PreserveCameraHeight;
            MaximumEffort = 4;
            SmoothingTime = 0;
            Damping = 0;
            DampingWhenOccluded = 0;
            OptimalTargetDistance = 0;
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
        class VcamExtraState
        {
            public Vector3 PreviousDisplacement;
            public bool TargetObscured;
            public float OcclusionStartTime;
            public List<Vector3> DebugResolutionPath;
            public Vector3 PreviousCameraOffset;
            public Vector3 PreviousCameraPosition;
            public float PreviousDampTime;

            public void AddPointToDebugPath(Vector3 p)
            {
#if UNITY_EDITOR
                if (DebugResolutionPath == null)
                    DebugResolutionPath = new List<Vector3>();
                DebugResolutionPath.Add(p);
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

        /// <summary>Inspector API for debugging collision resolution path</summary>
        public List<List<Vector3>> DebugPaths
        {
            get
            {
                List<List<Vector3>> list = new List<List<Vector3>>();
                List<VcamExtraState> extraStates = GetAllExtraStates<VcamExtraState>();
                foreach (var v in extraStates)
                    if (v.DebugResolutionPath != null && v.DebugResolutionPath.Count > 0)
                        list.Add(v.DebugResolutionPath);
                return list;
            }
        }

        /// <summary>
        /// Report maximum damping time needed for this component.
        /// </summary>
        /// <returns>Highest damping setting in this component</returns>
        public override float GetMaxDampTime() 
        { 
            return Mathf.Max(Damping, Mathf.Max(DampingWhenOccluded, SmoothingTime)); 
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
                extra.DebugResolutionPath?.RemoveRange(0, extra.DebugResolutionPath.Count);
            
                if (AvoidObstacles)
                {
                    var initialCamPos = state.GetCorrectedPosition();

                    // Rotate the previous collision correction along with the camera
                    var dampingBypass = Quaternion.Euler(state.PositionDampingBypass);
                    extra.PreviousDisplacement = dampingBypass * extra.PreviousDisplacement;

                    // Calculate the desired collision correction
                    var displacement = PreserveLineOfSight(ref state, ref extra);
                    if (MinimumOcclusionTime > Epsilon)
                    {
                        // If minimum occlusion time set, ignore new occlusions until they've lasted long enough
                        var now = CinemachineCore.CurrentTime;
                        if (displacement.AlmostZero())
                            extra.OcclusionStartTime = 0; // no occlusion
                        else
                        {
                            if (extra.OcclusionStartTime <= 0)
                                extra.OcclusionStartTime = now; // occlusion timer starts now
                            if (now - extra.OcclusionStartTime < MinimumOcclusionTime)
                                displacement = extra.PreviousDisplacement;
                        }
                    }

                    // Apply distance smoothing - this can artificially hold the camera closer
                    // to the target for a while, to reduce popping in and out on bumpy objects
                    if (SmoothingTime > Epsilon)
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

                    // Apply additional correction due to camera radius
                    var cameraPos = initialCamPos + displacement;
                    displacement += RespectCameraRadius(cameraPos, state.HasLookAt() ? state.ReferenceLookAt : cameraPos);

                    // Apply damping
                    float dampTime = DampingWhenOccluded;
                    if (deltaTime >= 0 && VirtualCamera.PreviousStateIsValid && DampingWhenOccluded + Damping > Epsilon)
                    {
                        // To ease the transition between damped and undamped regions, we damp the damp time
                        var dispSqrMag = displacement.sqrMagnitude;
                        dampTime = dispSqrMag > extra.PreviousDisplacement.sqrMagnitude ? DampingWhenOccluded : Damping;
                        if (dispSqrMag < Epsilon)
                            dampTime = extra.PreviousDampTime - Damper.Damp(extra.PreviousDampTime, dampTime, deltaTime);

                        var prevDisplacement = state.ReferenceLookAt + dampingBypass * extra.PreviousCameraOffset - initialCamPos;
                        displacement = prevDisplacement + Damper.Damp(displacement - prevDisplacement, dampTime, deltaTime);
                    }
                    
                    state.PositionCorrection += displacement;
                    cameraPos = state.GetCorrectedPosition();

                    // Adjust the damping bypass to account for the displacement
                    if (state.HasLookAt() && VirtualCamera.PreviousStateIsValid)
                    {
                        var dir0 = extra.PreviousCameraPosition - state.ReferenceLookAt;
                        var dir1 = cameraPos - state.ReferenceLookAt;
                        if (dir0.sqrMagnitude > Epsilon && dir1.sqrMagnitude > Epsilon)
                            state.PositionDampingBypass = UnityVectorExtensions.SafeFromToRotation(
                                dir0, dir1, state.ReferenceUp).eulerAngles;
                    }

                    extra.PreviousDisplacement = displacement;
                    extra.PreviousCameraOffset = cameraPos - state.ReferenceLookAt;
                    extra.PreviousCameraPosition = cameraPos;
                    extra.PreviousDampTime = dampTime;
                }
            }
            // Rate the shot after the aim was set
            if (stage == CinemachineCore.Stage.Aim)
            {
                var extra = GetExtraState<VcamExtraState>(vcam);
                extra.TargetObscured = IsTargetOffscreen(state) || CheckForTargetObstructions(state);

                // GML these values are an initial arbitrary attempt at rating quality
                if (extra.TargetObscured)
                    state.ShotQuality *= 0.2f;
                if (!extra.PreviousDisplacement.AlmostZero())
                    state.ShotQuality *= 0.8f;

                float nearnessBoost = 0;
                const float kMaxNearBoost = 0.2f;
                if (OptimalTargetDistance > 0 && state.HasLookAt())
                {
                    var distance = Vector3.Magnitude(state.ReferenceLookAt - state.GetFinalPosition());
                    if (distance <= OptimalTargetDistance)
                    {
                        var threshold = OptimalTargetDistance / 2;
                        if (distance >= threshold)
                            nearnessBoost = kMaxNearBoost * (distance - threshold)
                                / (OptimalTargetDistance - threshold);
                    }
                    else
                    {
                        distance -= OptimalTargetDistance;
                        float threshold = OptimalTargetDistance * 3;
                        if (distance < threshold)
                            nearnessBoost = kMaxNearBoost * (1f - (distance / threshold));
                    }
                    state.ShotQuality *= (1f + nearnessBoost);
                }
            }
        }

        Vector3 PreserveLineOfSight(ref CameraState state, ref VcamExtraState extra)
        {
            var displacement = Vector3.zero;
            if (state.HasLookAt() && CollideAgainst != 0
                && CollideAgainst != TransparentLayers)
            {
                var cameraPos = state.GetCorrectedPosition();
                var lookAtPos = state.ReferenceLookAt;
                var hitInfo = new RaycastHit();
                displacement = PullCameraInFrontOfNearestObstacle(
                    cameraPos, lookAtPos, CollideAgainst & ~TransparentLayers, ref hitInfo);
                var pos = cameraPos + displacement;
                if (hitInfo.collider != null)
                {
                    extra.AddPointToDebugPath(pos);
                    if (Strategy != ResolutionStrategy.PullCameraForward)
                    {
                        Vector3 targetToCamera = cameraPos - lookAtPos;
                        pos = PushCameraBack(
                            pos, targetToCamera, hitInfo, lookAtPos,
                            new Plane(state.ReferenceUp, cameraPos),
                            targetToCamera.magnitude, MaximumEffort, ref extra);
                    }
                }
                displacement = pos - cameraPos;
            }
            return displacement;
        }

        Vector3 PullCameraInFrontOfNearestObstacle(
            Vector3 cameraPos, Vector3 lookAtPos, int layerMask, ref RaycastHit hitInfo)
        {
            var displacement = Vector3.zero;
            var dir = cameraPos - lookAtPos;
            var targetDistance = dir.magnitude;
            if (targetDistance > Epsilon)
            {
                dir /= targetDistance;
                var minDistanceFromTarget = Mathf.Max(MinimumDistanceFromTarget, Epsilon);
                if (targetDistance < minDistanceFromTarget + Epsilon)
                    displacement = dir * (minDistanceFromTarget - targetDistance);
                else
                {
                    var rayLength = targetDistance - minDistanceFromTarget;
                    if (DistanceLimit > Epsilon)
                        rayLength = Mathf.Min(DistanceLimit, rayLength);

                    // Make a ray that looks towards the camera, to get the obstacle closest to target
                    var ray = new Ray(cameraPos - rayLength * dir, dir);
                    rayLength += k_PrecisionSlush;
                    if (rayLength > Epsilon)
                    {
                        if (RuntimeUtility.RaycastIgnoreTag(
                            ray, out hitInfo, rayLength, layerMask, IgnoreTag))
                        {
                            // Pull camera forward in front of obstacle
                            float adjustment = Mathf.Max(0, hitInfo.distance - k_PrecisionSlush);
                            displacement = ray.GetPoint(adjustment) - cameraPos;
                        }
                    }
                }
            }
            return displacement;
        }

        Vector3 PushCameraBack(
            Vector3 currentPos, Vector3 pushDir, RaycastHit obstacle,
            Vector3 lookAtPos, Plane startPlane, float targetDistance, int iterations,
            ref VcamExtraState extra)
        {
            // Take a step along the wall.
            var pos = currentPos;
            var dir = Vector3.zero;
            if (!GetWalkingDirection(pos, pushDir, obstacle, ref dir))
                return pos;

            Ray ray = new Ray(pos, dir);
            float distance = GetPushBackDistance(ray, startPlane, targetDistance, lookAtPos);
            if (distance <= Epsilon)
                return pos;

            // Check only as far as the obstacle bounds
            float clampedDistance = ClampRayToBounds(ray, distance, obstacle.collider.bounds);
            distance = Mathf.Min(distance, clampedDistance + k_PrecisionSlush);

            if (RuntimeUtility.RaycastIgnoreTag(
                ray, out var hitInfo, distance, CollideAgainst & ~TransparentLayers, IgnoreTag))
            {
                // We hit something.  Stop there and take a step along that wall.
                var adjustment = hitInfo.distance - k_PrecisionSlush;
                pos = ray.GetPoint(adjustment);
                extra.AddPointToDebugPath(pos);
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
            if (d < Epsilon || RuntimeUtility.RaycastIgnoreTag(
                    new Ray(lookAtPos, dir), out _, d - k_PrecisionSlush, 
                        CollideAgainst & ~TransparentLayers, IgnoreTag))
                return currentPos;

            // All clear
            ray = new Ray(pos, dir);
            extra.AddPointToDebugPath(pos);
            distance = GetPushBackDistance(ray, startPlane, targetDistance, lookAtPos);
            if (distance > Epsilon)
            {
                if (!RuntimeUtility.RaycastIgnoreTag(
                    ray, out hitInfo, distance, CollideAgainst & ~TransparentLayers, IgnoreTag))
                {
                    pos = ray.GetPoint(distance); // no obstacles - all good
                    extra.AddPointToDebugPath(pos);
                }
                else
                {
                    // We hit something.  Stop there and maybe take a step along that wall
                    float adjustment = hitInfo.distance - k_PrecisionSlush;
                    pos = ray.GetPoint(adjustment);
                    extra.AddPointToDebugPath(pos);
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
            if (Strategy == ResolutionStrategy.PreserveCameraDistance)
                return maxDistance;

            if (!startPlane.Raycast(ray, out var distance))
                distance = 0;
            distance = Mathf.Min(maxDistance, distance);
            if (distance < Epsilon)
                return 0;

            // If we are close to parallel to the plane, we have to take special action
            var angle = Mathf.Abs(UnityVectorExtensions.Angle(startPlane.normal, ray.direction) - 90);
            if (angle < k_AngleThreshold)
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
            if (CameraRadius < Epsilon || CollideAgainst == 0)
                return result;

            var dir = cameraPos - lookAtPos;
            var distance = dir.magnitude;
            if (distance > Epsilon)
                dir /= distance;

            // Pull it out of any intersecting obstacles
            RaycastHit hitInfo;
            int numObstacles = Physics.OverlapSphereNonAlloc(
                cameraPos, CameraRadius, s_ColliderBuffer,
                CollideAgainst, QueryTriggerInteraction.Ignore);
            if (numObstacles == 0 && TransparentLayers != 0
                && distance > MinimumDistanceFromTarget + Epsilon)
            {
                // Make sure the camera position isn't completely inside an obstacle.
                // OverlapSphereNonAlloc won't catch those.
                float d = distance - MinimumDistanceFromTarget;
                Vector3 targetPos = lookAtPos + dir * MinimumDistanceFromTarget;
                if (RuntimeUtility.RaycastIgnoreTag(new Ray(targetPos, dir), 
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
                scratchCollider.radius = CameraRadius;

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
                var minDistance = Mathf.Max(MinimumDistanceFromTarget, CameraRadius) + k_PrecisionSlush;
                var newOffset = cameraPos + result - lookAtPos;
                if (newOffset.magnitude < minDistance)
                    result = lookAtPos - cameraPos + dir * minDistance;
            }

            return result;
        }

        bool CheckForTargetObstructions(CameraState state)
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
                if (RuntimeUtility.RaycastIgnoreTag(ray, out _,
                        distance - MinimumDistanceFromTarget,
                        CollideAgainst & ~TransparentLayers, IgnoreTag))
                    return true;
            }
            return false;
        }

        static bool IsTargetOffscreen(CameraState state)
        {
            if (state.HasLookAt())
            {
                var dir = state.ReferenceLookAt - state.GetCorrectedPosition();
                dir = Quaternion.Inverse(state.GetCorrectedOrientation()) * dir;
                if (state.Lens.Orthographic)
                {
                    if (Mathf.Abs(dir.y) > state.Lens.OrthographicSize)
                        return true;
                    if (Mathf.Abs(dir.x) > state.Lens.OrthographicSize * state.Lens.Aspect)
                        return true;
                }
                else
                {
                    var fov = state.Lens.FieldOfView / 2;
                    var angle = UnityVectorExtensions.Angle(dir.ProjectOntoPlane(Vector3.right), Vector3.forward);
                    if (angle > fov)
                        return true;

                    fov = Mathf.Rad2Deg * Mathf.Atan(Mathf.Tan(fov * Mathf.Deg2Rad) * state.Lens.Aspect);
                    angle = UnityVectorExtensions.Angle(dir.ProjectOntoPlane(Vector3.up), Vector3.forward);
                    if (angle > fov)
                        return true;
                }
            }
            return false;
        }
    }
}
#endif
