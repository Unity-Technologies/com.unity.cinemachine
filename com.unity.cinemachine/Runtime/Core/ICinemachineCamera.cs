using System;
using UnityEngine;
using UnityEngine.Events;

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
        /// Returns the ICinemachineMixer within which this Camera is nested, or null.
        /// </summary>
        ICinemachineMixer ParentCamera { get; }

        /// <summary>
        /// Update the camera's state.
        /// The implementation must guarantee against multiple calls per frame, and should
        /// use CinemachineCore.UpdateVirtualCamera(ICinemachineCamera, Vector3, float), which
        /// has protection against multiple calls per frame.
        /// </summary>
        /// <param name="worldUp">Default world Up, set by the CinemachineBrain</param>
        /// <param name="deltaTime">Delta time for time-based effects (ignore if less than 0)</param>
        void UpdateCameraState(Vector3 worldUp, float deltaTime);

        /// <summary>
        /// Notification that this camera is being activated.  This is sent to the newly activated camera.  
        /// Multiple camera may be active simultaneously for a while, if blending.
        /// evt.IncomingCamera will always be "this".
        /// </summary>
        /// <param name="evt">Context for the camera activation.</param>
        void OnCameraActivated(ActivationEventParams evt);

        /// <summary>This is sent with ActivationEvent</summary>
        public struct ActivationEventParams
        {
            /// <summary>Object that originated the event</summary>
            public ICinemachineMixer Origin;
            /// <summary>Camera that is being deactivated (may be null)</summary>
            public ICinemachineCamera OutgoingCamera;
            /// <summary>Camera that is being activated (may be null)</summary>
            public ICinemachineCamera IncomingCamera;
            /// <summary>If true, then the transition is instantaneous</summary>
            public bool IsCut;
            /// <summary>Up direction for this frame.  Unity vector in world coords.</summary>
            public Vector3 WorldUp;
            /// <summary>Effective deltaTime for this frame</summary>
            public float DeltaTime;
        }

        /// <summary>Event that is fired when a Cinemachine camera is activated.</summary>
        [Serializable] public class ActivationEvent : UnityEvent<ActivationEventParams> {}
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
