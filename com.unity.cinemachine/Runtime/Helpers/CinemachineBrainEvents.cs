using System;
using UnityEngine;
using UnityEngine.Events;

namespace Unity.Cinemachine
{
    /// <summary>
    /// This component will generate brain-specific events.
    /// Add it to a a GameObject that has a CinemachineBrain.
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
        public CinemachineCore.BrainEvent CameraCutEvent = new ();

        /// <summary>This event will fire after the brain updates its Camera</summary>
        [Tooltip("This event will fire after the brain updates its Camera.")]
        public CinemachineCore.BrainEvent UpdatedEvent = new ();

        CinemachineBrain m_Brain;

        void OnEnable()
        {
            TryGetComponent(out m_Brain);
            if (m_Brain != null)
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
            if (evt.Origin == (ICinemachineMixer)m_Brain)
            {
                CameraActivatedEvent.Invoke(m_Brain, evt.IncomingCamera);
                if (evt.IsCut)
                    CameraCutEvent.Invoke(m_Brain);
            }
        }

        void OnCameraDeactivated(ICinemachineMixer mixer, ICinemachineCamera cam)
        {
            if (mixer == (ICinemachineMixer)m_Brain)
                CameraDeactivatedEvent.Invoke(mixer, cam);
        }

        void OnBlendFinished(ICinemachineMixer mixer, ICinemachineCamera cam)
        {
            if (mixer == (ICinemachineMixer)m_Brain)
                CameraBlendFinishedEvent.Invoke(m_Brain, cam);
        }

        void OnCameraUpdated(CinemachineBrain brain)
        {
            if (brain == m_Brain)
                UpdatedEvent.Invoke(brain);
        }
    }
}
