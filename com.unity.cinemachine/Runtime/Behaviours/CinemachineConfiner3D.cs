#if CINEMACHINE_PHYSICS 

using UnityEngine;
using Cinemachine.Utility;

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

        [Tooltip("How gradually to return the camera to the bounding volume if it goes beyond the borders.  "
            + "Higher numbers are more gradual.")]
        [RangeSlider(0, 10)]
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
            => GetExtraState<VcamExtraState>(vcam).ConfinerDisplacement;

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
            public float ConfinerDisplacement;
        };

        /// <summary>Check if the bounding volume is defined</summary>
        public bool IsValid => BoundingVolume != null && BoundingVolume.enabled && BoundingVolume.gameObject.activeInHierarchy;

        /// <summary>
        /// Report maximum damping time needed for this component.
        /// </summary>
        /// <returns>Highest damping setting in this component</returns>
        public override float GetMaxDampTime() => Damping;

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
            if (IsValid && stage == CinemachineCore.Stage.Body)
            {
                var extra = GetExtraState<VcamExtraState>(vcam);
                var displacement = ConfinePoint(state.GetCorrectedPosition());

                if (Damping > 0 && deltaTime >= 0 && VirtualCamera.PreviousStateIsValid)
                {
                    var delta = displacement - extra.PreviousDisplacement;
                    delta = Damper.Damp(delta, Damping, deltaTime);
                    displacement = extra.PreviousDisplacement + delta;
                }
                extra.PreviousDisplacement = displacement;
                state.PositionCorrection += displacement;
                extra.ConfinerDisplacement = displacement.magnitude;
            }
        }

        Vector3 ConfinePoint(Vector3 camPos)
        {
            var mesh = BoundingVolume as MeshCollider;
            if (mesh != null && !mesh.convex)
                return Vector3.zero;
            return BoundingVolume.ClosestPoint(camPos) - camPos;
        }
    }
}
#endif

