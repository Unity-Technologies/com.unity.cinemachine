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
    /// the Collider will attempt to preserve the line of sight
    /// with the LookAt target of the virtual camera by moving 
    /// away from objects that will obstruct the view.
    /// 
    /// Additionally, the Collider can be used to assess the shot quality and 
    /// report this as a field in the camera State.
    /// </summary>
    [DocumentationSorting(DocumentationSortingAttribute.Level.UserRef)]
    [ExecuteInEditMode]
    [AddComponentMenu("")] // Hide in menu
    [SaveDuringPlay]
    public class CinemachineCollider : CinemachineExtension
    {
        /// <summary>Objects on these layers will be detected.</summary>
        [Header("Obstacle Detection")]
        [Tooltip("Objects on these layers will be detected")]
        public LayerMask m_CollideAgainst = 1;

        /// <summary>Obstacles with this tag will be ignored.  It is a good idea to set this field to the target's tag</summary>
        [TagField]
        [Tooltip("Obstacles with this tag will be ignored.  It is a good idea to set this field to the target's tag")]
        public string m_IgnoreTag = string.Empty;

        /// <summary>Objects on these layers will never obstruct view of the target.</summary>
        [Tooltip("Objects on these layers will never obstruct view of the target")]
        public LayerMask m_TransparentLayers = 0;

        /// <summary>Obstacles closer to the target than this will be ignored</summary>
        [Tooltip("Obstacles closer to the target than this will be ignored")]
        public float m_MinimumDistanceFromTarget = 0.1f;

        /// <summary>
        /// When enabled, will attempt to resolve situations where the line of sight to the 
        /// target is blocked by an obstacle
        /// </summary>
        [Space]
        [Tooltip("When enabled, will attempt to resolve situations where the line of sight to the target is blocked by an obstacle")]
        [FormerlySerializedAs("m_PreserveLineOfSight")]
        public bool m_AvoidObstacles = true;

        /// <summary>
        /// The raycast distance to test for when checking if the line of sight to this camera's target is clear.
        /// </summary>
        [Tooltip("The maximum raycast distance when checking if the line of sight to this camera's target is clear.  If the setting is 0 or less, the current actual distance to target will be used.")]
        [FormerlySerializedAs("m_LineOfSightFeelerDistance")]
        public float m_DistanceLimit = 0f;

        /// <summary>
        /// Don't take action unless occlusion has lasted at least this long.
        /// </summary>
        [Tooltip("Don't take action unless occlusion has lasted at least this long.")]
        public float m_MinimumOcclusionTime = 0f;

        /// <summary>
        /// Camera will try to maintain this distance from any obstacle.  
        /// Increase this value if you are seeing inside obstacles due to a large 
        /// FOV on the camera.
        /// </summary>
        [Tooltip("Camera will try to maintain this distance from any obstacle.  Try to keep this value small.  Increase it if you are seeing inside obstacles due to a large FOV on the camera.")]
        public float m_CameraRadius = 0.1f;

        /// <summary>The way in which the Collider will attempt to preserve sight of the target.</summary>
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
        /// <summary>The way in which the Collider will attempt to preserve sight of the target.</summary>
        [Tooltip("The way in which the Collider will attempt to preserve sight of the target.")]
        public ResolutionStrategy m_Strategy = ResolutionStrategy.PreserveCameraHeight;

        /// <summary>
        /// Upper limit on how many obstacle hits to process.  Higher numbers may impact performance.  
        /// In most environments, 4 is enough.  
        /// </summary>
        [Range(1, 10)]
        [Tooltip("Upper limit on how many obstacle hits to process.  Higher numbers may impact performance.  In most environments, 4 is enough.")]
        public int m_MaximumEffort = 4;

        /// <summary>
        /// The gradualness of collision resolution.  Higher numbers will move the 
        /// camera more gradually away from obstructions.
        /// </summary>
        [Range(0, 10)]
        [Tooltip("The gradualness of collision resolution.  Higher numbers will move the camera more gradually away from obstructions.")]
        [FormerlySerializedAs("m_Smoothing")]
        public float m_Damping = 0;

        /// <summary>If greater than zero, a higher score will be given to shots when the target is closer to
        /// this distance.  Set this to zero to disable this feature</summary>
        [Header("Shot Evaluation")]
        [Tooltip("If greater than zero, a higher score will be given to shots when the target is closer to this distance.  Set this to zero to disable this feature.")]
        public float m_OptimalTargetDistance = 0;

        /// <summary>See wheter an object is blocking the camera's view of the target</summary>
        /// <param name="vcam">The virtual camera in question.  This might be different from the
        /// virtual camera that owns the collider, in the event that the camera has children</param>
        /// <returns>True if something is blocking the view</returns>
        public bool IsTargetObscured(ICinemachineCamera vcam)
        {
            return GetExtraState<VcamExtraState>(vcam).targetObscured;
        }

        /// <summary>See whether the virtual camera has been moved nby the collider</summary>
        /// <param name="vcam">The virtual camera in question.  This might be different from the
        /// virtual camera that owns the collider, in the event that the camera has children</param>
        /// <returns>True if the virtual camera has been displaced due to collision or
        /// target obstruction</returns>
        public bool CameraWasDisplaced(ICinemachineCamera vcam)
        {
            return GetExtraState<VcamExtraState>(vcam).colliderDisplacement > 0;
        }

        private void OnValidate()
        {
            m_DistanceLimit = Mathf.Max(0, m_DistanceLimit);
            m_MinimumOcclusionTime = Mathf.Max(0, m_MinimumOcclusionTime);
            m_CameraRadius = Mathf.Max(0, m_CameraRadius);
            m_MinimumDistanceFromTarget = Mathf.Max(0.01f, m_MinimumDistanceFromTarget);
            m_OptimalTargetDistance = Mathf.Max(0, m_OptimalTargetDistance);
        }

        /// <summary>Cleanup</summary>
        protected override void OnDestroy()
        {
            base.OnDestroy();
            CleanupCameraCollider();
        }

        /// This must be small but greater than 0 - reduces false results due to precision
        protected const float PrecisionSlush = 0.001f;

        /// <summary>
        /// Per-vcam extra state info
        /// </summary>
        protected class VcamExtraState
        {
            public Vector3 m_previousDisplacement;
            public float colliderDisplacement;
            public bool targetObscured;
            public float occlusionStartTime;
            public List<Vector3> debugResolutionPath;

            public void AddPointToDebugPath(Vector3 p)
            {
#if UNITY_EDITOR
                if (debugResolutionPath == null)
                    debugResolutionPath = new List<Vector3>();
                debugResolutionPath.Add(p);
#endif
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
                    if (v.debugResolutionPath != null)
                        list.Add(v.debugResolutionPath);
                return list;
            }
        }

        /// <summary>Callback to do the collision resolution and shot evaluation</summary>
        protected override void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            VcamExtraState extra = null;
            if (stage == CinemachineCore.Stage.Body)
            {
                extra = GetExtraState<VcamExtraState>(vcam);
                extra.targetObscured = false;
                extra.colliderDisplacement = 0;
                extra.debugResolutionPath = null;
            }

            // Move the body before the Aim is calculated
            if (stage == CinemachineCore.Stage.Body)
            {
                if (m_AvoidObstacles)
                {
                    Vector3 displacement = Vector3.zero;
                    displacement = PreserveLignOfSight(ref state, ref extra);
                    if (m_MinimumOcclusionTime > Epsilon)
                    {
                        float now = Time.timeSinceLevelLoad;
                        if (displacement.sqrMagnitude < Epsilon)
                            extra.occlusionStartTime = 0;
                        else
                        {
                            if (extra.occlusionStartTime <= 0)
                                extra.occlusionStartTime = now;
                            if (now - extra.occlusionStartTime < m_MinimumOcclusionTime)
                                displacement = extra.m_previousDisplacement;
                        }
                    }
                    if (m_Damping > 0 && deltaTime >= 0)
                    {
                        Vector3 delta = displacement - extra.m_previousDisplacement;
                        delta = Damper.Damp(delta, m_Damping, deltaTime);
                        displacement = extra.m_previousDisplacement + delta;
                    }
                    if (m_CameraRadius > Epsilon)
                        displacement += RespectCameraRadius(displacement, ref state);
                    extra.m_previousDisplacement = displacement;
                    state.PositionCorrection += displacement;
                    extra.colliderDisplacement += displacement.magnitude;
                }
            }
            // Rate the shot after the aim was set
            if (stage == CinemachineCore.Stage.Aim)
            {
                extra = GetExtraState<VcamExtraState>(vcam);
                extra.targetObscured = IsTargetOffscreen(state) || CheckForTargetObstructions(state);

                // GML these values are an initial arbitrary attempt at rating quality
                if (extra.targetObscured)
                    state.ShotQuality *= 0.2f;
                if (extra.colliderDisplacement > 0)
                    state.ShotQuality *= 0.8f;

                float nearnessBoost = 0;
                const float kMaxNearBoost = 0.2f;
                if (m_OptimalTargetDistance > 0 && state.HasLookAt)
                {
                    float distance = Vector3.Magnitude(state.ReferenceLookAt - state.FinalPosition);
                    if (distance <= m_OptimalTargetDistance)
                    {
                        float threshold = m_OptimalTargetDistance / 2;
                        if (distance >= threshold)
                            nearnessBoost = kMaxNearBoost * (distance - threshold)
                                / (m_OptimalTargetDistance - threshold);
                    }
                    else
                    {
                        distance -= m_OptimalTargetDistance;
                        float threshold = m_OptimalTargetDistance * 3;
                        if (distance < threshold)
                            nearnessBoost = kMaxNearBoost * (1f - (distance / threshold));
                    }
                    state.ShotQuality *= (1f + nearnessBoost);
                }
            }
        }

        /// <summary>
        /// Displace the camera so that target is visible
        /// </summary>
        /// <param name="state">current camera state</param>
        /// <param name="extra">extra state data</param>
        /// <returns>Camera displacement</returns>
        protected virtual Vector3 PreserveLignOfSight(ref CameraState state, ref VcamExtraState extra)
        {
            Vector3 displacement = Vector3.zero;
            if (state.HasLookAt && m_CollideAgainst != 0 
                && m_CollideAgainst != m_TransparentLayers)
            {
                Vector3 cameraPos = state.CorrectedPosition;
                Vector3 lookAtPos = state.ReferenceLookAt;
                Vector3 pos = cameraPos;
                Vector3 dir = pos - lookAtPos;
                float targetDistance = dir.magnitude;
                float minDistanceFromTarget = Mathf.Max(m_MinimumDistanceFromTarget, Epsilon);
                if (targetDistance > minDistanceFromTarget)
                {
                    dir.Normalize();
                    float rayLength = targetDistance - minDistanceFromTarget;
                    if (m_DistanceLimit > Epsilon)
                        rayLength = Mathf.Min(m_DistanceLimit, rayLength);

                    // Make a ray that looks towards the camera, to get the most distant obstruction
                    Ray ray = new Ray(pos - rayLength * dir, dir);
                    rayLength += PrecisionSlush;
                    if (rayLength > Epsilon)
                    {
                        RaycastHit hitInfo;
                        if (RaycastIgnoreTag(ray, out hitInfo, rayLength, 
                            m_CollideAgainst & ~m_TransparentLayers))
                        {
                            // Pull camera forward in front of obstacle
                            float adjustment = Mathf.Max(0, hitInfo.distance - PrecisionSlush);
                            pos = ray.GetPoint(adjustment);
                            extra.AddPointToDebugPath(pos);
                            if (m_Strategy != ResolutionStrategy.PullCameraForward)
                            {
                                pos = PushCameraBack(
                                    pos, dir, hitInfo, lookAtPos, 
                                    new Plane(state.ReferenceUp, cameraPos),
                                    targetDistance, m_MaximumEffort, ref extra);
                            }
                        }
                    }
                }
                displacement = pos - cameraPos;
            }
            return displacement;
        }

        private bool RaycastIgnoreTag(
            Ray ray, out RaycastHit hitInfo, float rayLength, int layerMask)
        {
            while (Physics.Raycast(
                ray, out hitInfo, rayLength, layerMask, 
                QueryTriggerInteraction.Ignore))
            {
                if (m_IgnoreTag.Length == 0 || !hitInfo.collider.CompareTag(m_IgnoreTag))
                    return true;

                // Ignore the hit.  Pull ray origin forward in front of obstacle
                Ray inverseRay = new Ray(ray.GetPoint(rayLength), -ray.direction);
                if (!hitInfo.collider.Raycast(inverseRay, out hitInfo, rayLength))
                    break; // should never happen!
                rayLength = hitInfo.distance - PrecisionSlush;
                if (rayLength < Epsilon)
                    break;
                ray.origin = inverseRay.GetPoint(rayLength);
            }
            return false;
        }
        
        /// <summary>
        /// Try to restore original camera distance/height after the camera has
        /// been pulled in front of an obstacle.  This is a recursive algorithm
        /// that iterates until max iterations is reached, or the camera has been
        /// pushed back the required amount.
        /// </summary>
        /// <param name="currentPos"></param>
        /// <param name="pushDir"></param>
        /// <param name="obstacle"></param>
        /// <param name="lookAtPos"></param>
        /// <param name="startPlane"></param>
        /// <param name="targetDistance"></param>
        /// <param name="iterations"></param>
        /// <param name="extra"></param>
        /// <returns>New camera position</returns>
        protected Vector3 PushCameraBack(
            Vector3 currentPos, Vector3 pushDir, RaycastHit obstacle,
            Vector3 lookAtPos, Plane startPlane, float targetDistance, int iterations,
            ref VcamExtraState extra)
        {
            // Take a step along the wall.
            Vector3 pos = currentPos;
            Vector3 dir = Vector3.zero;
            if (!GetWalkingDirection(pos, pushDir, obstacle, ref dir))
                return pos;

            Ray ray = new Ray(pos, dir);
            float distance = GetPushBackDistance(ray, startPlane, targetDistance, lookAtPos);
            if (distance <= Epsilon)
                return pos;

            // Check only as far as the obstacle bounds
            float clampedDistance = ClampRayToBounds(ray, distance, obstacle.collider.bounds);
            distance = Mathf.Min(distance, clampedDistance + PrecisionSlush);

            RaycastHit hitInfo;
            if (RaycastIgnoreTag(ray, out hitInfo, distance, 
                    m_CollideAgainst & ~m_TransparentLayers))
            {
                // We hit something.  Stop there and take a step along that wall.
                float adjustment = hitInfo.distance - PrecisionSlush;
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
            float d = dir.magnitude;
            RaycastHit hitInfo2;
            if (d < Epsilon || RaycastIgnoreTag(
                    new Ray(lookAtPos, dir), out hitInfo2, d - PrecisionSlush, 
                    m_CollideAgainst & ~m_TransparentLayers))
                return currentPos;

            // All clear
            ray = new Ray(pos, dir);
            extra.AddPointToDebugPath(pos);
            distance = GetPushBackDistance(ray, startPlane, targetDistance, lookAtPos);
            if (distance > Epsilon)
            {
                if (!RaycastIgnoreTag(ray, out hitInfo, distance, 
                        m_CollideAgainst & ~m_TransparentLayers))
                {
                    pos = ray.GetPoint(distance); // no obstacles - all good
                    extra.AddPointToDebugPath(pos);
                }
                else 
                {
                    // We hit something.  Stop there and maybe take a step along that wall
                    float adjustment = hitInfo.distance - PrecisionSlush;
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

        private RaycastHit[] m_CornerBuffer = new RaycastHit[4];
        private bool GetWalkingDirection(
            Vector3 pos, Vector3 pushDir, RaycastHit obstacle, ref Vector3 outDir)
        {
            Vector3 normal2 = obstacle.normal;

            // Check for nearby obstacles.  Are we in a corner?
            float nearbyDistance = PrecisionSlush * 5;
            int numFound = Physics.SphereCastNonAlloc(
                pos, nearbyDistance, pushDir.normalized, m_CornerBuffer, 0, 
                m_CollideAgainst & ~m_TransparentLayers, QueryTriggerInteraction.Ignore);
            if (numFound > 1)
            {
                // Calculate the second normal
                for (int i = 0; i < numFound; ++i)
                {
                    if (m_CornerBuffer[i].collider == null)
                        continue;
                    if (m_IgnoreTag.Length > 0 && m_CornerBuffer[i].collider.CompareTag(m_IgnoreTag))
                        continue;
                    Type type = m_CornerBuffer[i].collider.GetType();
                    if (type == typeof(BoxCollider) 
                        || type == typeof(SphereCollider) 
                        || type == typeof(CapsuleCollider))
                    {
                        Vector3 p = m_CornerBuffer[i].collider.ClosestPoint(pos);
                        Vector3 d = p - pos;
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
            Vector3 dir = Vector3.Cross(obstacle.normal, normal2);
            if (dir.AlmostZero())
                dir = Vector3.ProjectOnPlane(pushDir, obstacle.normal);
            else 
            {
                float dot = Vector3.Dot(dir, pushDir);
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

        const float AngleThreshold = 0.1f;
        float GetPushBackDistance(Ray ray, Plane startPlane, float targetDistance, Vector3 lookAtPos)
        {
            float maxDistance = targetDistance - (ray.origin - lookAtPos).magnitude;
            if (maxDistance < Epsilon)
                return 0;
            if (m_Strategy == ResolutionStrategy.PreserveCameraDistance)
                return maxDistance;

            float distance;
            if (!startPlane.Raycast(ray, out distance))
                distance = 0;
            distance = Mathf.Min(maxDistance, distance);
            if (distance < Epsilon)
                return 0;

            // If we are close to parallel to the plane, we have to take special action
            float angle = Mathf.Abs(Vector3.Angle(startPlane.normal, ray.direction) - 90);
            if (angle < AngleThreshold)
                distance = Mathf.Lerp(0, distance, angle / AngleThreshold);
            return distance;
        }
                
        float ClampRayToBounds(Ray ray, float distance, Bounds bounds)
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

        private Collider[] mColliderBuffer = new Collider[5];
        private SphereCollider mCameraCollider;
        private GameObject mCameraColliderGameObject;
        /// <summary>
        /// Make sure the camera is not closer than m_CameraRadius to an obstacle.
        /// If it is, push it out, taking the shortest path.
        /// </summary>
        /// <param name="cameraDisplacement">camera offset away from its position in state</param>
        /// <param name="state">current camera state</param>
        /// <returns>Offset to apply to the camera position</returns>
        protected Vector3 RespectCameraRadius(Vector3 cameraDisplacement, ref CameraState state)
        {
            Vector3 result = Vector3.zero;
            if (m_CameraRadius < Epsilon || m_CollideAgainst == 0)
            {
                if (mCameraColliderGameObject != null)
                    CleanupCameraCollider();
                return result;
            }
            Vector3 cameraPos = state.CorrectedPosition + cameraDisplacement;
            int numObstacles = Physics.OverlapSphereNonAlloc(
                cameraPos, m_CameraRadius, mColliderBuffer, 
                m_CollideAgainst, QueryTriggerInteraction.Ignore);
            if (numObstacles == 0 && state.HasLookAt && m_TransparentLayers != 0)
            {
                // Make sure the camera position isn't completely inside an obstacle.
                // OverlapSphereNonAlloc won't catch those.
                Vector3 dir = cameraPos - state.ReferenceLookAt;
                float distance = dir.magnitude;
                if (distance > m_MinimumDistanceFromTarget + Epsilon)
                {
                    dir /= distance;
                    distance -= m_MinimumDistanceFromTarget;
                    RaycastHit hitInfo;
                    if (RaycastIgnoreTag(
                        new Ray(state.ReferenceLookAt + dir * m_MinimumDistanceFromTarget, dir),
                            out hitInfo, distance, m_CollideAgainst))
                    {
                        mColliderBuffer[0] = hitInfo.collider;
                        numObstacles = 1;
                    }
                }
            }
            if (numObstacles > 0)
            {
                if (mCameraColliderGameObject == null)
                {
                    mCameraColliderGameObject = new GameObject("CinemachineCollider Collider");
                    mCameraColliderGameObject.hideFlags = HideFlags.HideAndDontSave;
                    mCameraColliderGameObject.transform.position = Vector3.zero;
                    mCameraColliderGameObject.SetActive(true);
                    mCameraCollider = mCameraColliderGameObject.AddComponent<SphereCollider>();
                    var rb = mCameraColliderGameObject.AddComponent<Rigidbody>();
                    rb.detectCollisions = false;
                    rb.isKinematic = true;
                }
                mCameraCollider.radius = m_CameraRadius;
                for (int i = 0; i < numObstacles; ++i)
                {
                    Collider c = mColliderBuffer[i];
                    if (m_IgnoreTag.Length > 0 && c.CompareTag(m_IgnoreTag))
                        continue;
                    Vector3 dir;
                    float distance;
                    if (Physics.ComputePenetration(
                        mCameraCollider, cameraPos, Quaternion.identity, 
                        c, c.transform.position, c.transform.rotation,
                        out dir, out distance))
                    {
                        result += dir * distance;   // naive, but maybe enough
                    }
                }
            }
            return result;
        }

        private void CleanupCameraCollider()
        {
            RuntimeUtility.DestroyObject(mCameraColliderGameObject);
            mCameraColliderGameObject = null;
            mCameraCollider = null;
        }

        private bool CheckForTargetObstructions(CameraState state)
        {
            if (state.HasLookAt)
            {
                Vector3 lookAtPos = state.ReferenceLookAt;
                Vector3 pos = state.CorrectedPosition;
                Vector3 dir = lookAtPos - pos;
                float distance = dir.magnitude;
                if (distance < Mathf.Max(m_MinimumDistanceFromTarget, Epsilon))
                    return true;
                Ray ray = new Ray(pos, dir.normalized);
                RaycastHit hitInfo;
                if (RaycastIgnoreTag(ray, out hitInfo, 
                        distance - m_MinimumDistanceFromTarget, 
                        m_CollideAgainst & ~m_TransparentLayers))
                    return true;
            }
            return false;
        }

        private bool IsTargetOffscreen(CameraState state)
        {
            if (state.HasLookAt)
            {
                Vector3 dir = state.ReferenceLookAt - state.CorrectedPosition;
                dir = Quaternion.Inverse(state.CorrectedOrientation) * dir;
                if (state.Lens.Orthographic)
                {
                    if (Mathf.Abs(dir.y) > state.Lens.OrthographicSize)
                        return true;
                    if (Mathf.Abs(dir.x) > state.Lens.OrthographicSize * state.Lens.Aspect)
                        return true;
                }
                else
                {
                    float fov = state.Lens.FieldOfView / 2;
                    float angle = Vector3.Angle(dir.ProjectOntoPlane(Vector3.right), Vector3.forward);
                    if (angle > fov)
                        return true;

                    fov = Mathf.Rad2Deg * Mathf.Atan(Mathf.Tan(fov * Mathf.Deg2Rad) * state.Lens.Aspect);
                    angle = Vector3.Angle(dir.ProjectOntoPlane(Vector3.up), Vector3.forward);
                    if (angle > fov)
                        return true;
                }
            }
            return false;
        }
    }
}
