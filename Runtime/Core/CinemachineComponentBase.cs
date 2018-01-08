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
                    m_vcamOwner = gameObject.transform.parent.gameObject.GetComponent<CinemachineVirtualCameraBase>();
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

        /// <summary>Get the position of the Follow target.  Special handling: If the Follow target is
        /// a VirtualCamera, returns the vcam State's position, not the transform's position</summary>
        public Vector3 FollowTargetPosition
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

        /// <summary>Get the position of the LookAt target.  Special handling: If the LookAt target is
        /// a VirtualCamera, returns the vcam State's position, not the transform's position</summary>
        public Vector3 LookAtTargetPosition
        {
            get 
            {
                Transform target = LookAtTarget;
                if (target != mCachedLookAtTarget)
                {
                    mCachedLookAtTargetVcam = null;
                    mCachedLookAtTarget = target;
                    if (target != null)
                        mCachedLookAtTargetVcam = target.GetComponent<CinemachineVirtualCameraBase>();
                }
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
                {
                    mCachedLookAtTargetVcam = null;
                    mCachedLookAtTarget = target;
                    if (target != null)
                        mCachedLookAtTargetVcam = target.GetComponent<CinemachineVirtualCameraBase>();
                }
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
        public virtual void PrePipelineMutateCameraState(ref CameraState state) {}

        /// <summary>What part of the pipeline this fits into</summary>
        public abstract CinemachineCore.Stage Stage { get; }

        /// <summary>Mutates the camera state.  This state will later be applied to the camera.</summary>
        /// <param name="curState">Input state that must be mutated</param>
        /// <param name="deltaTime">Delta time for time-based effects (ignore if less than 0)</param>
        public abstract void MutateCameraState(ref CameraState curState, float deltaTime);

        /// <summary>API for the editor, to process a position drag from the user.
        /// Base class implementation does nothing.</summary>
        /// <param name="delta">The amount dragged this frame</param>
        public virtual void OnPositionDragged(Vector3 delta) {}
    }
}
