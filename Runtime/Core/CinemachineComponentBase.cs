using UnityEngine;

namespace Cinemachine
{
    /// <summary>
    /// An abstract representation of a mutator acting on a Cinemachine Virtual Camera
    /// </summary>
    [DocumentationSorting(DocumentationSortingAttribute.Level.API)]
    public abstract class CinemachineComponentBase : MonoBehaviour
    {
        /// <summary>Useful constant for very small floats</summary>
        protected const float Epsilon = Utility.UnityVectorExtensions.Epsilon;

        /// <summary>Get the associated CinemachineVirtualCameraBase</summary>
        public CinemachineVirtualCameraBase VirtualCamera
        {
            get
            {
                if (m_vcamOwner == null)
                    m_vcamOwner = GetComponent<CinemachineVirtualCameraBase>();
                if (m_vcamOwner == null && transform.parent != null)
                    m_vcamOwner = transform.parent.GetComponent<CinemachineVirtualCameraBase>();
                return m_vcamOwner;
            }
        }
        CinemachineVirtualCameraBase m_vcamOwner;

        /// <summary>Returns the owner vcam's Follow target.</summary>
        public Transform FollowTarget
        {
            get
            {
                CinemachineVirtualCameraBase vcam = VirtualCamera;
                return vcam == null ? null : vcam.Follow;
            }
        }

        /// <summary>Returns the owner vcam's LookAt target.</summary>
        public Transform LookAtTarget
        {
            get
            {
                CinemachineVirtualCameraBase vcam = VirtualCamera;
                return vcam == null ? null : vcam.LookAt;
            }
        }

        private Transform mCachedFollowTarget;
        private CinemachineVirtualCameraBase mCachedFollowTargetVcam;
        private ICinemachineTargetGroup mCachedFollowTargetGroup;

        void UpdateFollowTargetCache()
        {
            mCachedFollowTargetVcam = null;
            mCachedFollowTargetGroup = null;
            mCachedFollowTarget = FollowTarget;
            if (mCachedFollowTarget != null)
            {
                mCachedFollowTargetVcam = mCachedFollowTarget.GetComponent<CinemachineVirtualCameraBase>();
                mCachedFollowTargetGroup = mCachedFollowTarget.GetComponent<ICinemachineTargetGroup>();
            }
        }

        /// <summary>Get Follow target as ICinemachineTargetGroup, or null if target is not a group</summary>
        public ICinemachineTargetGroup AbstractFollowTargetGroup
        {
            get
            {
                if (FollowTarget != mCachedFollowTarget)
                    UpdateFollowTargetCache();
                return mCachedFollowTargetGroup;
            }
        }

        /// <summary>Get Follow target as CinemachineTargetGroup, or null if target is not a CinemachineTargetGroup</summary>
        public CinemachineTargetGroup FollowTargetGroup
        {
            get { return AbstractFollowTargetGroup as CinemachineTargetGroup; }
        }

        /// <summary>Get the position of the Follow target.  Special handling: If the Follow target is
        /// a VirtualCamera, returns the vcam State's position, not the transform's position</summary>
        public Vector3 FollowTargetPosition
        {
            get
            {
                Transform target = FollowTarget;
                if (target != mCachedFollowTarget)
                    UpdateFollowTargetCache();
                if (mCachedFollowTargetVcam != null)
                    return mCachedFollowTargetVcam.State.FinalPosition;
                if (target != null)
                    return target.position;
                return Vector3.zero;
            }
        }

        /// <summary>Get the rotation of the Follow target.  Special handling: If the Follow target is
        /// a VirtualCamera, returns the vcam State's rotation, not the transform's rotation</summary>
        public Quaternion FollowTargetRotation
        {
            get
            {
                Transform target = FollowTarget;
                if (target != mCachedFollowTarget)
                {
                    mCachedFollowTargetVcam = null;
                    mCachedFollowTarget = target;
                    if (target != null)
                        mCachedFollowTargetVcam = target.GetComponent<CinemachineVirtualCameraBase>();
                }
                if (mCachedFollowTargetVcam != null)
                    return mCachedFollowTargetVcam.State.FinalOrientation;
                if (target != null)
                    return target.rotation;
                return Quaternion.identity;
            }
        }

        private Transform mCachedLookAtTarget;
        private CinemachineVirtualCameraBase mCachedLookAtTargetVcam;
        private ICinemachineTargetGroup mCachedLookAtTargetGroup;

        void UpdateLookAtTargetCache()
        {
            mCachedLookAtTargetVcam = null;
            mCachedLookAtTargetGroup = null;
            mCachedLookAtTarget = LookAtTarget;
            if (mCachedLookAtTarget != null)
            {
                mCachedLookAtTargetVcam = mCachedLookAtTarget.GetComponent<CinemachineVirtualCameraBase>();
                mCachedLookAtTargetGroup = mCachedLookAtTarget.GetComponent<ICinemachineTargetGroup>();
            }
        }

        /// <summary>Get LookAt target as ICinemachineTargetGroup, or null if target is not a group</summary>
        public ICinemachineTargetGroup AbstractLookAtTargetGroup
        {
            get
            {
                if (LookAtTarget != mCachedLookAtTarget)
                    UpdateLookAtTargetCache();
                return mCachedLookAtTargetGroup;
            }
        }

        /// <summary>Get LookAt target as CinemachineTargetGroup, or null if target is not a CinemachineTargetGroup</summary>
        public CinemachineTargetGroup LookAtTargetGroup
        {
            get { return AbstractLookAtTargetGroup as CinemachineTargetGroup; }
        }

        /// <summary>Get the position of the LookAt target.  Special handling: If the LookAt target is
        /// a VirtualCamera, returns the vcam State's position, not the transform's position</summary>
        public Vector3 LookAtTargetPosition
        {
            get
            {
                Transform target = LookAtTarget;
                if (target != mCachedLookAtTarget)
                    UpdateLookAtTargetCache();
                if (mCachedLookAtTargetVcam != null)
                    return mCachedLookAtTargetVcam.State.FinalPosition;
                if (target != null)
                    return target.position;
                return Vector3.zero;
            }
        }

        /// <summary>Get the rotation of the LookAt target.  Special handling: If the LookAt target is
        /// a VirtualCamera, returns the vcam State's rotation, not the transform's rotation</summary>
        public Quaternion LookAtTargetRotation
        {
            get
            {
                Transform target = LookAtTarget;
                if (target != mCachedLookAtTarget)
                    UpdateLookAtTargetCache();
                if (mCachedLookAtTargetVcam != null)
                    return mCachedLookAtTargetVcam.State.FinalOrientation;
                if (target != null)
                    return target.rotation;
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

        /// <summary>Override this to do such things as offset the RefereceLookAt.
        /// Base class implementation does nothing.</summary>
        /// <param name="curState">Input state that must be mutated</param>
        public virtual void PrePipelineMutateCameraState(ref CameraState curState, float deltaTime) {}

        /// <summary>What part of the pipeline this fits into</summary>
        public abstract CinemachineCore.Stage Stage { get; }

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
            ICinemachineCamera fromCam, Vector3 worldUp, float deltaTime,
            ref CinemachineVirtualCameraBase.TransitionParams transitionParams)
        { return false; }

        /// <summary>This is called to notify the component that a target got warped,
        /// so that the component can update its internal state to make the camera
        /// also warp seamlessy.  Base class implementation does nothing.</summary>
        /// <param name="target">The object that was warped</param>
        /// <param name="positionDelta">The amount the target's position changed</param>
        public virtual void OnTargetObjectWarped(Transform target, Vector3 positionDelta) {}
    }
}
