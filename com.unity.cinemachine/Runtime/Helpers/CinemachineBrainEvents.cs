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
        /// This event is fired when a virtual camera is activated.
        /// If a blend is involved, it will be fired at the start of the blend.
        /// </summary>
        [Serializable]
        public class ActivationEvent : UnityEvent<CinemachineBrain, ICinemachineCamera> {}
        
        /// <summary>
        /// This event is fired when a virtual camera is activated.  
        /// If a blend is involved, it will be fired at the start of the blend.
        /// </summary>
        [Tooltip("This event will fire whenever a virtual camera goes live.  If a blend is "
            + "involved, then the event will fire on the first frame of the blend.")]
        public ActivationEvent CameraActivatedEvent = new ();

        /// <summary>
        /// This event is fired when there is a camera cut.  A camera cut is a camera 
        /// activation with a zero-length blend.  
        /// </summary>
        [Tooltip("This event is fired when there is a camera cut.  A camera cut is a camera "
            + "activation with a zero-length blend.")]
        public CinemachineCore.BrainEvent CameraCutEvent = new ();

        /// <summary>This event will fire after the brain updates its Camera</summary>
        [Tooltip("This event will fire after the brain updates its Camera.")]
        public CinemachineCore.BrainEvent CameraUpdatedEvent = new ();

        CinemachineBrain m_Brain;

        void OnEnable()
        {
            TryGetComponent(out m_Brain);
            if (m_Brain != null)
            {
                CinemachineCore.CameraActivatedEvent.AddListener(OnCameraActivated);
                CinemachineCore.CameraUpdatedEvent.AddListener(OnCameraUpdated);
            }
        }

        void OnDisable()
        {
            CinemachineCore.CameraActivatedEvent.RemoveListener(OnCameraActivated);
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

        void OnCameraUpdated(CinemachineBrain brain)
        {
            if (brain == m_Brain)
                CameraUpdatedEvent.Invoke(brain);
        }
    }
}
