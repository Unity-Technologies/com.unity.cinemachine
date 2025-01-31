using UnityEngine;

namespace Unity.Cinemachine
{
    /// <summary>
    /// An abstract representation of a mutator acting on a CinemachineCamera
    /// </summary>
    [ExecuteAlways]
    public abstract class CinemachineComponentBase : MonoBehaviour
    {
        /// <summary>Useful constant for very small floats</summary>
        protected const float Epsilon = UnityVectorExtensions.Epsilon;

        CinemachineVirtualCameraBase m_VcamOwner;

        /// <summary>Get the associated CinemachineVirtualCameraBase</summary>
        public CinemachineVirtualCameraBase VirtualCamera
        {
            get
            {
                if (m_VcamOwner == null)
                    TryGetComponent(out m_VcamOwner);
#if !CINEMACHINE_NO_CM2_SUPPORT
                if (m_VcamOwner == null && transform.parent != null)
                    transform.parent.TryGetComponent(out m_VcamOwner);
#endif
                return m_VcamOwner;
            }
        }

        /// <summary>
        /// Standard OnEnable call.  Derived classes should call the base class implementation.
        /// Handles pipeline validation.
        /// </summary>
        protected virtual void OnEnable()
        {
            var vcam = VirtualCamera as CinemachineCamera;
            if (vcam != null)
                vcam.InvalidatePipelineCache();
        }

        /// <summary>
        /// Standard OnDisable call.  Derived classes should call the base class implementation.
        /// Handles pipeline validation.
        /// </summary>
        protected virtual void OnDisable()
        {
            var vcam = VirtualCamera as CinemachineCamera;
            if (vcam != null)
                vcam.InvalidatePipelineCache();
        }

        /// <summary>Returns the owner vcam's Follow target.</summary>
        public Transform FollowTarget
        {
            get
            {
                var vcam = VirtualCamera;
                return vcam == null ? null : vcam.ResolveFollow(vcam.Follow);
            }
        }

        /// <summary>Returns the owner vcam's LookAt target.</summary>
        public Transform LookAtTarget
        {
            get
            {
                CinemachineVirtualCameraBase vcam = VirtualCamera;
                return vcam == null ? null : vcam.ResolveLookAt(vcam.LookAt);
            }
        }

        /// <summary>Get Follow target as ICinemachineTargetGroup, or null if target is not a group</summary>
        public ICinemachineTargetGroup FollowTargetAsGroup
        {
            get
            {
                CinemachineVirtualCameraBase vcam = VirtualCamera;
                return vcam == null ? null : vcam.FollowTargetAsGroup;
            }
        }

        /// <summary>Get the position of the Follow target.  Special handling: If the Follow target is
        /// a VirtualCamera, returns the vcam State's position, not the transform's position</summary>
        public Vector3 FollowTargetPosition
        {
            get
            {
                var vcam = VirtualCamera.FollowTargetAsVcam;
                if (vcam != null)
                    return vcam.State.GetFinalPosition();
                Transform target = FollowTarget;
                if (target != null)
                    return TargetPositionCache.GetTargetPosition(target);
                return Vector3.zero;
            }
        }

        /// <summary>Get the rotation of the Follow target.  Special handling: If the Follow target is
        /// a VirtualCamera, returns the vcam State's rotation, not the transform's rotation</summary>
        public Quaternion FollowTargetRotation
        {
            get
            {
                var vcam = VirtualCamera.FollowTargetAsVcam;
                if (vcam != null)
                    return vcam.State.GetFinalOrientation();
                Transform target = FollowTarget;
                if (target != null)
                    return TargetPositionCache.GetTargetRotation(target);
                return Quaternion.identity;
            }
        }

        /// <summary>Get LookAt target as ICinemachineTargetGroup, or null if target is not a group</summary>
        public ICinemachineTargetGroup LookAtTargetAsGroup => VirtualCamera.LookAtTargetAsGroup;

        /// <summary>Get the position of the LookAt target.  Special handling: If the LookAt target is
        /// a VirtualCamera, returns the vcam State's position, not the transform's position</summary>
        public Vector3 LookAtTargetPosition
        {
            get
            {
                var vcam = VirtualCamera.LookAtTargetAsVcam;
                if (vcam != null)
                    return vcam.State.GetFinalPosition();
                Transform target = LookAtTarget;
                if (target != null)
                    return TargetPositionCache.GetTargetPosition(target);
                return Vector3.zero;
            }
        }

        /// <summary>Get the rotation of the LookAt target.  Special handling: If the LookAt target is
        /// a VirtualCamera, returns the vcam State's rotation, not the transform's rotation</summary>
        public Quaternion LookAtTargetRotation
        {
            get
            {
                var vcam = VirtualCamera.LookAtTargetAsVcam;
                if (vcam != null)
                    return vcam.State.GetFinalOrientation();
                Transform target = LookAtTarget;
                if (target != null)
                    return TargetPositionCache.GetTargetRotation(target);
                return Quaternion.identity;
            }
        }

        /// <summary>Returns the owner vcam's CameraState.</summary>
        public CameraState VcamState
        {
            get
            {
                CinemachineVirtualCameraBase vcam = VirtualCamera;
                return vcam == null ? CameraState.Default : vcam.State;
            }
        }

        /// <summary>Returns true if this object is enabled and set up to produce results.</summary>
        public abstract bool IsValid { get; }

        /// <summary>Override this to do such things as offset the ReferenceLookAt.
        /// Base class implementation does nothing.</summary>
        /// <param name="curState">Input state that must be mutated</param>
        /// <param name="deltaTime">Current effective deltaTime</param>
        public virtual void PrePipelineMutateCameraState(ref CameraState curState, float deltaTime) {}

        /// <summary>What part of the pipeline this fits into</summary>
        // GML todo: remove this - use attribute
        public abstract CinemachineCore.Stage Stage { get; }

        /// <summary>Special for Body Stage components that want to be applied after Aim
        /// stage because they use the aim as inout for the procedural placement</summary>
        public virtual bool BodyAppliesAfterAim => false;

        /// <summary>Mutates the camera state.  This state will later be applied to the camera.</summary>
        /// <param name="curState">Input state that must be mutated</param>
        /// <param name="deltaTime">Delta time for time-based effects (ignore if less than 0)</param>
        public abstract void MutateCameraState(ref CameraState curState, float deltaTime);

        /// <summary>Notification that this virtual camera is going live.
        /// Base class implementation does nothing.</summary>
        /// <param name="fromCam">The camera being deactivated.  May be null.</param>
        /// <param name="worldUp">Default world Up, set by the CinemachineBrain</param>
        /// <param name="deltaTime">Delta time for time-based effects (ignore if less than or equal to 0)</param>
        /// <returns>True if the vcam should do an internal update as a result of this call</returns>
        public virtual bool OnTransitionFromCamera(
            ICinemachineCamera fromCam, Vector3 worldUp, float deltaTime) => false;

        /// <summary>This is called to notify the component that a target got warped,
        /// so that the component can update its internal state to make the camera
        /// also warp seamlessly.  Base class implementation does nothing.</summary>
        /// <param name="target">The object that was warped</param>
        /// <param name="positionDelta">The amount the target's position changed</param>
        public virtual void OnTargetObjectWarped(Transform target, Vector3 positionDelta) {}

        /// <summary>
        /// Force the virtual camera to assume a given position and orientation.
        /// Procedural placement then takes over.
        /// Base class implementation does nothing.</summary>
        /// <param name="pos">World-space position to take</param>
        /// <param name="rot">World-space orientation to take</param>
        public virtual void ForceCameraPosition(Vector3 pos, Quaternion rot) {}

        /// <summary>
        /// Report maximum damping time needed for this component.
        /// Only used in editor for timeline scrubbing.
        /// </summary>
        /// <returns>Highest damping setting in this component</returns>
        public virtual float GetMaxDampTime() => 0;

        /// <summary>
        /// True if this component tries to make the camera look at the Tracking Target.
        /// Used by inspector to warn the user of potential improper setup.
        /// </summary>
        internal virtual bool CameraLooksAtTarget { get => false; }
    }
}
