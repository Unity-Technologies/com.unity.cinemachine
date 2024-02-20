using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Unity.Cinemachine.Samples
{
    /// <summary>
    /// This script keeps a player upright on surfaces.
    /// It rotates the player to match the surface normal.
    /// Player is expected to NOT have a collider on it.  To support a player with a collider,
    /// the raycasts would have to be modified to exclude the player's collider.
    /// This script assumes that the pivot point of the player is at the bottom.
    /// </summary>
    public class PlayerOnSurface : MonoBehaviour
    {
        [Tooltip("How fast the player rotates to match the surface normal")]
        public float RotationDamping = 0.2f;
        [Tooltip("What layers to consider as ground")]
        public LayerMask GroundLayers = 1;
        [Tooltip("How far to raycast when checking for ground")]
        public float MaxRaycastDistance = 5;
        [Tooltip("The approximate height of the player.  Used to compute where raycasts begin")]
        public float PlayerHeight = 1;

        [Tooltip("If enabled, then player will fall towads the nearest surface when in free fall")]
        public bool FreeFallControl;

        [Header("Events")]
        [Tooltip("This event is sent when the player moves from one surface to another.")]
        public UnityEvent<Collider> SurfaceChanged = new ();

        Vector3 m_PreviousGroundPoint;
        Vector3 m_PreviousPosition;
        Collider m_CurrentSurface;
        float m_FreeFallRaycastAngle = 0;

        public bool PreviousSateIsValid { get; set; }
        
        void OnEnable() => PreviousSateIsValid = false;

        void OnValidate()
        {
            RotationDamping = Mathf.Max(0, RotationDamping);
            MaxRaycastDistance = Mathf.Max(PlayerHeight * 0.5f, MaxRaycastDistance);
            PlayerHeight = Mathf.Max(0, PlayerHeight);
        }

        // Rotate the player to match the normal of the surface it's standing on
        void LateUpdate()
        {
            var tr = transform;
            var desiredUp = tr.up;
            var down = -desiredUp;
            var damping = RotationDamping;

            var originOffset = 0.25f * PlayerHeight * desiredUp;
            var downRaycastOrigin = tr.position + originOffset;
            var fwdRaycastOrigin = tr.position + 2 * originOffset;
            var playerRadius = 0.25f * PlayerHeight; // Approximate player radius - can convert to a parameter if needed

            // Check whether we have walked into a surface
            bool haveHit = false;
            if (PreviousSateIsValid)
            {
                var dir = fwdRaycastOrigin - m_PreviousPosition;
                var dirLen = dir.magnitude;
                if (dirLen < 0.0001f)
                    dir = tr.forward;
                else
                    dir /= dirLen;
                if (Physics.Raycast(m_PreviousPosition, dir, out var forwardHit, 
                    dirLen + playerRadius, GroundLayers, QueryTriggerInteraction.Ignore))
                {
                    haveHit = true;
                    desiredUp = CaptureUpDirection(forwardHit);
                }
            }
            m_PreviousPosition = fwdRaycastOrigin;
            PreviousSateIsValid = true;

            // Find the ground under our feet
            if (!haveHit && Physics.Raycast(downRaycastOrigin, down, out var hit, 
                MaxRaycastDistance, GroundLayers, QueryTriggerInteraction.Ignore))
            {
                haveHit = true;
                desiredUp = CaptureUpDirection(hit);
            }

            // If nothing is directly under our feet, try to find a surface in the direction
            // where we came from.  This handles the case of sudden convex direction changes in the floor
            // (e.g. going around the lip of a surface)
            if (!haveHit && Physics.Raycast(downRaycastOrigin, m_PreviousGroundPoint - downRaycastOrigin, out hit, 
                MaxRaycastDistance, GroundLayers, QueryTriggerInteraction.Ignore))
            {
                haveHit = true;
                desiredUp = SmoothedNormal(hit);
            }

            // If we don't have a hit by now, we're in free fall
            if (haveHit)
                m_FreeFallRaycastAngle = 0;
            else
            {
                SetCurrentSurface(null);
                if (FreeFallControl 
                    && Vector3.Dot(downRaycastOrigin - m_PreviousGroundPoint, desiredUp) <= 0
                    && FindNearestSurface(downRaycastOrigin, out var surfacePoint))
                {
                    desiredUp = (downRaycastOrigin - surfacePoint).normalized;
                    //damping = 0;
                }
            }

            // Rotate to match the desired up direction
            float t = Damper.Damp(1, damping, Time.deltaTime);
            var fwd = tr.forward.ProjectOntoPlane(desiredUp);
            if (fwd.sqrMagnitude > 0.0001f)
                tr.rotation = Quaternion.Slerp(tr.rotation, Quaternion.LookRotation(fwd, desiredUp), t);
            else
            {
                // Rotating 90 degrees - can't preserve the forward
                var axis = Vector3.Cross(tr.up, desiredUp);
                var angle = UnityVectorExtensions.SignedAngle(tr.up, desiredUp, axis);
                var rot = Quaternion.Slerp(Quaternion.identity, Quaternion.AngleAxis(angle, axis), t);
                tr.rotation = rot * tr.rotation;
            }
        }

        Vector3 CaptureUpDirection(RaycastHit hit)
        {
            m_PreviousGroundPoint = hit.point; // Capture the last point where there was ground under our feet
            SetCurrentSurface(hit.collider); // Capture the current ground surface
            return SmoothedNormal(hit);
        }

        void SetCurrentSurface(Collider surface)
        {
            // If the surface has changed, send an event
            if (surface != m_CurrentSurface)
            {
                m_CurrentSurface = surface;
                SurfaceChanged.Invoke(m_CurrentSurface);
            }
        }

        bool FindNearestSurface(Vector3 playerPos, out Vector3 surfacePoint)
        {
            surfacePoint = playerPos - transform.up; // default is to continue falling down

            // We'll spread out a number of vertical sweeps over several frames
            var up = transform.up;
            var dir = Quaternion.AngleAxis(m_FreeFallRaycastAngle, transform.right) * -up;

            // We'll do a vertical sweep to find the nearest surface 
            const float kVerticalStep = 10.0f;
            m_FreeFallRaycastAngle += kVerticalStep;
            if (m_FreeFallRaycastAngle > 180 - kVerticalStep)
                m_FreeFallRaycastAngle = kVerticalStep;

            float nearestDistance = float.MaxValue;
            const float kHorizontalalSteps = 12;
            const float korizontalStepSize = 360.0f / kHorizontalalSteps;
            for (int i = 0; i <= kHorizontalalSteps; ++i)
            {
                //Debug.DrawLine(playerPos, playerPos + dir * MaxRaycastDistance, Color.yellow, 1);
                if (Physics.Raycast(playerPos, dir, out var hit, 
                    MaxRaycastDistance, GroundLayers, QueryTriggerInteraction.Ignore))
                {
                    if (hit.distance < nearestDistance)
                    {
                        nearestDistance = hit.distance;
                        surfacePoint = hit.point;
                    }
                }
                if (m_FreeFallRaycastAngle == 0)
                    break;
                dir = Quaternion.AngleAxis(korizontalStepSize, -up) * dir;
            }
            return nearestDistance != float.MaxValue;
        }

        // This code smooths the normals of a mesh so that they don't change abruptly.
        // We cache the mesh data for efficiency to reduce allocations.
        struct MeshCache
        {
            public MeshCollider Mesh;
            public Vector3[] Normals;
            public int[] Indices;
        }
        List<MeshCache> m_MeshCacheList = new(); 
        const int kMaxMeshCacheSize = 5;
        MeshCache GetMeshCache(MeshCollider collider)
        {
            for (int i = 0; i < m_MeshCacheList.Count; ++i)
                if (m_MeshCacheList[i].Mesh == collider)
                    return m_MeshCacheList[i];
            if (m_MeshCacheList.Count >= kMaxMeshCacheSize)
                m_MeshCacheList.RemoveAt(0); // discard oldest
            var m = collider.sharedMesh;
            var mc = new MeshCache { Mesh = collider, Normals = m.normals, Indices = m.triangles };
            m_MeshCacheList.Add(mc);
            return mc;
        }
        Vector3 SmoothedNormal(RaycastHit hit)
        {
            var mc = hit.collider as MeshCollider;
            if (mc == null)
                return hit.normal;
            var m = GetMeshCache(mc);
            var n0 = m.Normals[m.Indices[hit.triangleIndex*3 + 0]];
            var n1 = m.Normals[m.Indices[hit.triangleIndex*3 + 1]];
            var n2 = m.Normals[m.Indices[hit.triangleIndex*3 + 2]];
            var b = hit.barycentricCoordinate;
            var localNormal = (b[0] * n0 + b[1] * n1 + b[2] * n2).normalized;
            return mc.transform.TransformDirection(localNormal);
        }
    }
}
