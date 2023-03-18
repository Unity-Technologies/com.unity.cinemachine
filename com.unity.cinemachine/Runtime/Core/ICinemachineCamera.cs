using UnityEngine;

namespace Unity.Cinemachine
{
    /// <summary>
    /// An abstract representation of a virtual camera which lives within the Unity scene
    /// </summary>
    public interface ICinemachineCamera
    {
        /// <summary>
        /// Gets the name of this virtual camera. For use when deciding how to blend
        /// to or from this camera
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets a brief debug description of this camera, for use when displaying debug info
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Camera state at the current time.
        /// </summary>
        CameraState State { get; }

        /// <summary>Will return false if this references a deleted object</summary>
        bool IsValid { get; }

        /// <summary>
        /// Update the camera's state.
        /// The implementation must guarantee against multiple calls per frame, and should
        /// use CinemachineCore.UpdateVirtualCamera(ICinemachineCamera, Vector3, mfloat), which
        /// has protection against multiple calls per frame.
        /// </summary>
        /// <param name="worldUp">Default world Up, set by the CinemachineBrain</param>
        /// <param name="deltaTime">Delta time for time-based effects (ignore if less than 0)</param>
        void UpdateCameraState(Vector3 worldUp, float deltaTime);

        /// <summary>
        /// Notification that a new camera is being activated.  This is sent to the
        /// currently active camera.  Both may be active simultaneously for a while, if blending.
        /// </summary>
        /// <param name="fromCam">The camera being deactivated.  May be null.</param>
        /// <param name="worldUp">Default world Up, set by the CinemachineBrain</param>
        /// <param name="deltaTime">Delta time for time-based effects (ignore if less than 0)</param>
        void OnTransitionFromCamera(ICinemachineCamera fromCam, Vector3 worldUp, float deltaTime);

        /// <summary>
        /// Returns the ICinemachineMixer within which this Camera is nested, or null.
        /// </summary>
        ICinemachineMixer ParentCamera { get; }
    }

    /// <summary>
    /// This is a ICinemachineCamera that can own child ICinemachineCameras.
    /// ICinemachineCamera nesting is defined using this interface.
    /// </summary>
    public interface ICinemachineMixer : ICinemachineCamera
    {
        /// <summary>Check whether the cam is a live child of this camera.</summary>
        /// <param name="child">The child ICienamchineCamera to check</param>
        /// <param name="dominantChildOnly">If true, will only return true if this vcam is the dominant live child</param>
        /// <returns>True if the vcam is currently actively influencing the state of this vcam</returns>
        bool IsLiveChild(ICinemachineCamera child, bool dominantChildOnly = false);
    }
}
