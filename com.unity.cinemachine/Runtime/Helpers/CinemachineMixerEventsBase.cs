using UnityEngine;

namespace Unity.Cinemachine
{
    /// <summary>
    /// This is a base class for components that will generate mixer-specific events.
    /// </summary>
    [SaveDuringPlay]
    public abstract class CinemachineMixerEventsBase : MonoBehaviour
    {
        /// <summary>
        /// This event will fire whenever a virtual camera goes live.
        /// If a blend is involved, it will be fired at the start of the blend.
        /// </summary>
        [Space]
        [Tooltip("This event will fire whenever a virtual camera goes live.  If a blend is "
            + "involved, then the event will fire on the first frame of the blend.")]
        public CinemachineCore.CameraEvent CameraActivatedEvent = new ();

        /// <summary>
        /// This event will fire whenever a virtual stops being live.
        /// If a blend is involved, then the event will fire after the last frame of the blend.
        /// </summary>
        [Tooltip("This event will fire whenever a virtual stops being live.  If a blend is "
            + "involved, then the event will fire after the last frame of the blend.")]
        public CinemachineCore.CameraEvent CameraDeactivatedEvent = new ();

        /// <summary>
        /// This event will fire whenever a blend is created in the root frame of this Brain.
        /// The handler can modify any settings in the blend, except the cameras themselves.
        /// Note: timeline tracks will not generate these events.
        /// </summary>
        [Tooltip("This event will fire whenever a blend is created in the root frame of this Brain.  "
            + "The handler can modify any settings in the blend, except the cameras themselves.  "
            + "Note: timeline tracks will not generate these events.")]
        public CinemachineCore.BlendEvent BlendCreatedEvent = new ();

        /// <summary>
        /// This event will fire whenever a virtual camera finishes blending in.
        /// It will not fire if the blend length is zero.
        /// </summary>
        [Tooltip("This event will fire whenever a virtual camera finishes blending in.  "
            + "It will not fire if the blend length is zero.")]
        public CinemachineCore.CameraEvent BlendFinishedEvent = new ();

        /// <summary>
        /// This event is fired when there is a camera cut.  A camera cut is a camera
        /// activation with a zero-length blend.
        /// </summary>
        [Tooltip("This event is fired when there is a camera cut.  A camera cut is a camera "
            + "activation with a zero-length blend.")]
        public CinemachineCore.CameraEvent CameraCutEvent = new ();

        /// <summary>
        /// Get the object being monitored.  This is the object that will generate the events.
        /// </summary>
        /// <returns>The ICinemachineMixer object that is the origin of the events.</returns>
        protected abstract ICinemachineMixer GetMixer();

        /// <summary>Install the event handlers.  Call this from OnEnable().</summary>
        /// <param name="mixer">The mixer object to monitor.</param>
        protected void InstallHandlers(ICinemachineMixer mixer)
        {
            if (mixer != null)
            {
                CinemachineCore.CameraActivatedEvent.AddListener(OnCameraActivated);
                CinemachineCore.CameraDeactivatedEvent.AddListener(OnCameraDeactivated);
                CinemachineCore.BlendCreatedEvent.AddListener(OnBlendCreated);
                CinemachineCore.BlendFinishedEvent.AddListener(OnBlendFinished);
            }
        }

        /// <summary>Uninstall the event handlers.  Call the from OnDisable().</summary>
        protected void UninstallHandlers()
        {
            CinemachineCore.CameraActivatedEvent.RemoveListener(OnCameraActivated);
            CinemachineCore.CameraDeactivatedEvent.RemoveListener(OnCameraDeactivated);
            CinemachineCore.BlendCreatedEvent.RemoveListener(OnBlendCreated);
            CinemachineCore.BlendFinishedEvent.RemoveListener(OnBlendFinished);
        }

        void OnCameraActivated(ICinemachineCamera.ActivationEventParams evt)
        {
            var mixer = GetMixer();
            if (evt.Origin == mixer)
            {
                CameraActivatedEvent.Invoke(mixer, evt.IncomingCamera);
                if (evt.IsCut)
                    CameraCutEvent.Invoke(mixer, evt.IncomingCamera);
            }
        }

        void OnCameraDeactivated(ICinemachineMixer mixer, ICinemachineCamera cam)
        {
            if (mixer == GetMixer())
                CameraDeactivatedEvent.Invoke(mixer, cam);
        }

        void OnBlendCreated(CinemachineCore.BlendEventParams evt)
        {
            if (evt.Origin == GetMixer())
                BlendCreatedEvent.Invoke(evt);
        }

        void OnBlendFinished(ICinemachineMixer mixer, ICinemachineCamera cam)
        {
            if (mixer == GetMixer())
                BlendFinishedEvent.Invoke(mixer, cam);
        }
    }
}
