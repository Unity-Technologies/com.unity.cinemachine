#if CINEMACHINE_PHYSICS

using UnityEngine;

namespace Unity.Cinemachine
{
    /// <summary>
    /// An add-on module for CinemachineCamera that post-processes
    /// the final position of the camera. It will confine the camera's position
    /// to the volume specified in the Bounding Volume field.
    /// </summary>
    [AddComponentMenu("Cinemachine/Procedural/Extensions/Cinemachine Confiner 3D")]
    [SaveDuringPlay]
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [HelpURL(Documentation.BaseURL + "manual/CinemachineConfiner3D.html")]
    public class CinemachineConfiner3D : CinemachineExtension
    {
        /// <summary>The volume within which the camera is to be contained.</summary>
        [Tooltip("The volume within which the camera is to be contained")]
        public Collider BoundingVolume;

        /// <summary>Size of the slow-down zone at the edge of the bounding volume.</summary>
        [Tooltip("Size of the slow-down zone at the edge of the bounding volume.")]
        public float SlowingDistance = 0;

        /// <summary>See whether the virtual camera has been moved by the confiner</summary>
        /// <param name="vcam">The virtual camera in question.  This might be different from the
        /// virtual camera that owns the confiner, in the event that the camera has children</param>
        /// <returns>True if the virtual camera has been repositioned</returns>
        public bool CameraWasDisplaced(CinemachineVirtualCameraBase vcam) => GetCameraDisplacementDistance(vcam) > 0;

        /// <summary>See how far virtual camera has been moved by the confiner</summary>
        /// <param name="vcam">The virtual camera in question.  This might be different from the
        /// virtual camera that owns the confiner, in the event that the camera has children</param>
        /// <returns>True if the virtual camera has been repositioned</returns>
        public float GetCameraDisplacementDistance(CinemachineVirtualCameraBase vcam)
            => GetExtraState<VcamExtraState>(vcam).PreviousDisplacement.magnitude;

        void Reset()
        {
            BoundingVolume = null;
            SlowingDistance = 0;
        }

        void OnValidate()
        {
            SlowingDistance = Mathf.Max(0, SlowingDistance);
        }

        class VcamExtraState : VcamExtraStateBase
        {
            public Vector3 PreviousDisplacement;
            public Vector3 PreviousCameraPosition;
        };

        /// <summary>Check if the bounding volume is defined</summary>
        public bool IsValid => BoundingVolume != null && BoundingVolume.enabled && BoundingVolume.gameObject.activeInHierarchy;

        /// <summary>
        /// Report maximum damping time needed for this component.
        /// </summary>
        /// <returns>Highest damping setting in this component</returns>
        public override float GetMaxDampTime() => SlowingDistance * 0.2f; // just an approximation - we don't know the time

        /// <summary>This is called to notify the extension that a target got warped,
        /// so that the extension can update its internal state to make the camera
        /// also warp seamlessly.  Base class implementation does nothing.</summary>
        /// <param name="vcam">The camera to warp</param>
        /// <param name="target">The object that was warped</param>
        /// <param name="positionDelta">The amount the target's position changed</param>
        public override void OnTargetObjectWarped(
            CinemachineVirtualCameraBase vcam, Transform target, Vector3 positionDelta)
        {
            var extra = GetExtraState<VcamExtraState>(vcam);
            if (extra.Vcam.Follow == target)
                extra.PreviousCameraPosition += positionDelta;
        }

        /// <summary>
        /// Callback to do the camera confining
        /// </summary>
        /// <param name="vcam">The virtual camera being processed</param>
        /// <param name="stage">The current pipeline stage</param>
        /// <param name="state">The current virtual camera state</param>
        /// <param name="deltaTime">The current applicable deltaTime</param>
        protected override void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            if (stage == CinemachineCore.Stage.Body && IsValid)
            {
                var extra = GetExtraState<VcamExtraState>(vcam);
                var camPos = state.GetCorrectedPosition();

                // Snap the point inside the bounds
                var newPos = ConfinePoint(camPos);
                if (SlowingDistance > Epsilon && deltaTime >= 0 && vcam.PreviousStateIsValid)
                {
                    // Reduce speed if moving towards the edge and close enough to it
                    var prevPos = extra.PreviousCameraPosition;
                    var dir = newPos - prevPos;
                    var speed = dir.magnitude;
                    if (speed > Epsilon)
                    {
                        var t = GetDistanceFromEdge(prevPos, dir / speed, SlowingDistance) / SlowingDistance;

                        // This formula is found to give a smooth slowing curve while ensuring
                        // that it comes to a full stop in a reasonable time
                        newPos = Vector3.Lerp(prevPos, newPos, t * t * t + 0.05f);
                    }
                }
                var displacement = newPos - camPos;
                state.PositionCorrection += displacement;
                extra.PreviousCameraPosition = state.GetCorrectedPosition();
                extra.PreviousDisplacement = displacement;
            }
        }

        Vector3 ConfinePoint(Vector3 p)
        {
            var mesh = BoundingVolume as MeshCollider;
            if (mesh != null && !mesh.convex)
                return p;
            return BoundingVolume.ClosestPoint(p);
        }

        // Returns distance from edge in direction of motion, or max if distance is greater than max.
        // dirUnit must be unit length.
        float GetDistanceFromEdge(Vector3 p, Vector3 dirUnit, float max)
        {
            p += dirUnit * max;
            return max - (ConfinePoint(p) - p).magnitude;
        }
    }
}
#endif

