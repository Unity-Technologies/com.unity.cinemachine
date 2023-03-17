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
    }
}
