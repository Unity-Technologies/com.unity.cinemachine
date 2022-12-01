#if CINEMACHINE_PHYSICS 

using UnityEngine;

namespace Cinemachine
{
    /// <summary>
    /// An add-on module for CmCamera that post-processes
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
        public float Damping = 0;
        
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
            Damping = 0;
        }

        void OnValidate()
        {
            Damping = Mathf.Max(0, Damping);
        }

        class VcamExtraState
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
        public override float GetMaxDampTime() => Damping;

        /// <summary>This is called to notify the extension that a target got warped,
        /// so that the extension can update its internal state to make the camera
        /// also warp seamlessly.  Base class implementation does nothing.</summary>
        /// <param name="target">The object that was warped</param>
        /// <param name="positionDelta">The amount the target's position changed</param>
        public override void OnTargetObjectWarped(Transform target, Vector3 positionDelta) 
            => VirtualCamera.PreviousStateIsValid = false;  // invalidate the vcam velocity calcualtion
        
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

                // If initially outside the bounds, snap it in, no damping
                var newPos = ConfinePoint(camPos);
                var displacement = newPos - camPos;
                if (Damping > Epsilon && deltaTime >= 0 && VirtualCamera.PreviousStateIsValid)
                {
                    // We can only damp if the camera is moving
                    var prevPos = extra.PreviousCameraPosition;
                    var dir = newPos - prevPos;
                    var speed = dir.magnitude;
                    if (speed > Epsilon)
                    {
                        // Reduce the speed if moving towards the edge
                        dir /= speed;
                        var slowingThreshold = 2 * Damping; // because the first half of the slowing is barely noticeable
                        var t = GetDistanceFromEdge(prevPos, dir, slowingThreshold) / slowingThreshold;

                        // This formula is found to give a nice slowing curve while ensuring
                        // that it comes to a stop in a reasonable time
                        newPos = Vector3.Lerp(prevPos, newPos, t * t + 0.05f);

                        displacement = newPos - camPos;
                    }
                }
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

        // Returns distance from edge in direwction of motion, or max if distance is greater than max.
        float GetDistanceFromEdge(Vector3 p, Vector3 dirUnit, float max)
        {
            p += dirUnit * max;
            return max - (ConfinePoint(p) - p).magnitude;
        }
    }
}
#endif

