using System.Collections.Generic;
using UnityEngine;

namespace Unity.Cinemachine.Samples
{
    /// <summary>
    /// This script keeps a player upright on surfaces.
    /// It rotates the player to match the surface normal.
    /// Player should NOT have a collider on it - it should be a trigger.
    /// </summary>
    public class PlayerOnSurface : MonoBehaviour
    {
        [Tooltip("How fast the capsule rotates to match the surface normal")]
        public float RotationDamping = 0.2f;
        [Tooltip("What layers to consider as ground")]
        public LayerMask CollideAgainst = 1;
        [Tooltip("How far to raycast when checking for ground")]
        public float MaxRaycastDistance = 5;
        [Tooltip("The approximate height of the player.  Used to compute where raycasts begin")]
        public float PlayerHeight = 1;
        [Tooltip("If true, then the camera up can influence capsule rotation when in free fall")]
        public bool CameraControlsFreeFall = false;

        Vector3 m_PreviousGround;
        Vector3 m_CameraSpaceUp;
        Vector3 m_PreviousPosition;

        public bool PreviousSateIsValid { get; set; }

        // Rotate the capsule to match the normal of the surface it's standing on
        void LateUpdate()
        {
            var tr = transform;
            var desiredUp = tr.up;
            var originOffset = 0.2f * PlayerHeight * desiredUp;
            var p0 = tr.position + originOffset; // raycast origin
            var damping = RotationDamping;
            var down = -desiredUp;
            var cam = CameraControlsFreeFall ? Camera.main.transform : null;

            // Check whether we have walked into a surface
            bool haveHit = false;
            if (PreviousSateIsValid)
            {
                var dir = p0 - m_PreviousPosition;
                var dirLen = dir.magnitude;
                if (dirLen > 0.0001f)
                {
                    if (Physics.Raycast(m_PreviousPosition, dir, out var ht, 
                        0.5f * PlayerHeight, CollideAgainst, QueryTriggerInteraction.Ignore))
                    {
                        haveHit = true;
                        desiredUp = CaptureUpDirection(ht, cam);
                        PreviousSateIsValid = false;
                    }
                }
            }
            m_PreviousPosition = p0;

            // Find the ground under our feet
            if (!haveHit && Physics.Raycast(p0, down, out var hit, 
                MaxRaycastDistance, CollideAgainst, QueryTriggerInteraction.Ignore))
            {
                haveHit = true;
                desiredUp = CaptureUpDirection(hit, cam);
            }

            // If nothing is directly under our feet, try to find a surface in the direction
            // where we came from.  This handles the case of sudden convex direction changes in the floor
            // (e.g. going around the lip of a surface)
            if (!haveHit && Physics.Raycast(p0, m_PreviousGround - p0, out hit, 
                MaxRaycastDistance, CollideAgainst, QueryTriggerInteraction.Ignore))
            {
                haveHit = true;
                desiredUp = SmoothedNormal(hit);
            }

            // If we're in free fall, optionally allow the camera direction to influence which way is up
            if (!haveHit && cam != null)
            {
                desiredUp = cam.TransformDirection(m_CameraSpaceUp);
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
            PreviousSateIsValid = true;
        }

        Vector3 CaptureUpDirection(RaycastHit hit, Transform cam)
        {
            var desiredUp = SmoothedNormal(hit);

            // Capture the last point where the capsule is touching the ground
            m_PreviousGround = hit.point;

            // While things are stable, capture the camera space up vector in camera space.
            // We use this later in free-fall, to allow the camera to influence which way is up.
            m_CameraSpaceUp = desiredUp;
            if (cam != null)
                m_CameraSpaceUp = cam.InverseTransformDirection(desiredUp);

            return desiredUp;
        }

        // This code smooths the normals of a mesh so that they don't change abruptly.
        // We cache the mesh data for efficiency to reduce allocations.
        struct MeshCache
        {
            public MeshCollider Mesh;
            public Vector3[] Normals;
            public int[] Indices;
        }
        List<MeshCache> m_MeshCacheList = new List<MeshCache>(); 
        const int kMaxMeshCacheSize = 5;
        MeshCache GetMeshCache(MeshCollider collider)
        {
            for (int i = 0; i < m_MeshCacheList.Count; ++i)
                if (m_MeshCacheList[i].Mesh == collider)
                    return m_MeshCacheList[i];
            if (m_MeshCacheList.Count >= kMaxMeshCacheSize)
                m_MeshCacheList.RemoveAt(0);
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
