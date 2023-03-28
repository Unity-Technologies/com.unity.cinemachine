using UnityEngine;

namespace Unity.Cinemachine
{
    /// <summary>
    /// This component will generate mixer-specific events.
    /// Add it to a a GameObject that has a CinemachineBrain or another ICinemachineMixer, 
    /// such as a CinemachineCameraManager.
    /// </summary>
    [AddComponentMenu("Cinemachine/Helpers/Cinemachine Brain Events")]
    [SaveDuringPlay]
    [HelpURL(Documentation.BaseURL + "manual/CinemachineBrainEvents.html")]
    public class CinemachineBrainEvents : MonoBehaviour
    {
        /// <summary>
        /// This event will fire whenever a virtual camera goes live.  
        /// If a blend is involved, it will be fired at the start of the blend.
        /// </summary>
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
        /// This event will fire whenever a virtual camera finishes blending in.  
        /// It will not fire if the blend length is zero.
        /// </summary>
        [Tooltip("This event will fire whenever a virtual camera finishes blending in.  "
            + "It will not fire if the blend length is zero.")]
        public CinemachineCore.CameraEvent CameraBlendFinishedEvent = new ();

        /// <summary>
        /// This event is fired when there is a camera cut.  A camera cut is a camera 
        /// activation with a zero-length blend.  
        /// </summary>
        [Tooltip("This event is fired when there is a camera cut.  A camera cut is a camera "
            + "activation with a zero-length blend.")]
        public CinemachineCore.CameraEvent CameraCutEvent = new ();

        /// <summary>This event will fire after the brain updates its Camera.
        /// This evenet will only fire if this component is attached to a CinemachineBrain.</summary>
        [Tooltip("This event will fire after the brain updates its Camera.")]
        public CinemachineCore.BrainEvent BrainUpdatedEvent = new ();

        ICinemachineMixer m_Mixer;

        void OnEnable()
        {
            TryGetComponent(out m_Mixer);
            if (m_Mixer != null)
            {
                CinemachineCore.CameraActivatedEvent.AddListener(OnCameraActivated);
                CinemachineCore.CameraDeactivatedEvent.AddListener(OnCameraDeactivated);
                CinemachineCore.BlendFinishedEvent.AddListener(OnBlendFinished);
                CinemachineCore.CameraUpdatedEvent.AddListener(OnCameraUpdated);
            }
        }

        void OnDisable()
        {
            CinemachineCore.CameraActivatedEvent.RemoveListener(OnCameraActivated);
            CinemachineCore.CameraDeactivatedEvent.RemoveListener(OnCameraDeactivated);
            CinemachineCore.BlendFinishedEvent.RemoveListener(OnBlendFinished);
            CinemachineCore.CameraUpdatedEvent.RemoveListener(OnCameraUpdated);
        }

        void OnCameraActivated(ICinemachineCamera.ActivationEventParams evt)
        {
            if (evt.Origin == m_Mixer)
            {
                CameraActivatedEvent.Invoke(m_Mixer, evt.IncomingCamera);
                if (evt.IsCut)
                    CameraCutEvent.Invoke(m_Mixer, evt.IncomingCamera);
            }
        }

        void OnCameraDeactivated(ICinemachineMixer mixer, ICinemachineCamera cam)
        {
            if (mixer == m_Mixer)
                CameraDeactivatedEvent.Invoke(mixer, cam);
        }

        void OnBlendFinished(ICinemachineMixer mixer, ICinemachineCamera cam)
        {
            if (mixer == m_Mixer)
                CameraBlendFinishedEvent.Invoke(m_Mixer, cam);
        }

        void OnCameraUpdated(CinemachineBrain brain)
        {
            if ((ICinemachineMixer)brain == m_Mixer)
                BrainUpdatedEvent.Invoke(brain);
        }
    }
}
