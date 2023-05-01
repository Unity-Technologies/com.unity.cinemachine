using UnityEngine;

namespace Unity.Cinemachine
{
    /// <summary>
    /// This component will generate camera-specific activation and deactivation events.
    /// Add it to a Cinemachine Camera.
    /// </summary>
    [AddComponentMenu("Cinemachine/Helpers/Cinemachine Camera Events")]
    [SaveDuringPlay]
    [HelpURL(Documentation.BaseURL + "manual/CinemachineCameraEvents.html")]
    public class CinemachineCameraEvents : MonoBehaviour
    {
        /// <summary>
        /// This event is fired when a virtual camera is activated.  
        /// If a blend is involved, it will be fired at the start of the blend.
        /// </summary>
        [Tooltip("This event will fire whenever a virtual camera becomes active in the context of a mixer.  "
            + "If a blend is involved, then the event will fire on the first frame of the blend.")]
        public CinemachineCore.CameraEvent CameraActivatedEvent = new ();

        /// <summary>
        /// This event will fire whenever a virtual stops being live.  
        /// If a blend is involved, then the event will fire after the last frame of the blend.
        /// </summary>
        [Tooltip("This event will fire whenever a virtual stops being live.  If a blend is "
            + "involved, then the event will fire after the last frame of the blend.")]
        public CinemachineCore.CameraEvent CameraDeactivatedEvent = new ();

        /// <summary>
        /// This event will fire whenever a blend is created that involves this camera.  
        /// The handler can modify any settings in the blend, except the cameras themselves.
        /// </summary>
        [Tooltip("This event will fire whenever a blend is created that involves this camera.  "
            + "The handler can modify any settings in the blend, except the cameras themselves.")]
        public CinemachineCore.BlendEvent BlendCreatedEvent = new ();

        /// <summary>
        /// This event will fire whenever a virtual camera finishes blending in.  
        /// It will not fire if the blend length is zero.
        /// </summary>
        [Tooltip("This event will fire whenever a virtual camera finishes blending in.  "
            + "It will not fire if the blend length is zero.")]
        public CinemachineCore.CameraEvent BlendFinishedEvent = new ();
        
        CinemachineVirtualCameraBase m_Vcam;

        void OnEnable()
        {
            TryGetComponent(out m_Vcam);
            if (m_Vcam != null)
            {
                CinemachineCore.CameraActivatedEvent.AddListener(OnCameraActivated);
                CinemachineCore.CameraDeactivatedEvent.AddListener(OnCameraDeactivated);
                CinemachineCore.BlendCreatedEvent.AddListener(OnBlendCreated);
                CinemachineCore.BlendFinishedEvent.AddListener(OnBlendFinished);
            }
        }

        void OnDisable()
        {
            CinemachineCore.CameraActivatedEvent.RemoveListener(OnCameraActivated);
            CinemachineCore.CameraDeactivatedEvent.RemoveListener(OnCameraDeactivated);
            CinemachineCore.BlendCreatedEvent.RemoveListener(OnBlendCreated);
            CinemachineCore.BlendFinishedEvent.RemoveListener(OnBlendFinished);
        }

        void OnCameraActivated(ICinemachineCamera.ActivationEventParams evt)
        {
            if (evt.IncomingCamera == (ICinemachineCamera)m_Vcam)
                CameraActivatedEvent.Invoke(evt.Origin, evt.IncomingCamera);
        }

        void OnBlendCreated(CinemachineCore.BlendEventParams evt)
        {
            if (evt.Blend.CamA == (ICinemachineCamera)m_Vcam || evt.Blend.CamB == (ICinemachineCamera)m_Vcam)
                BlendCreatedEvent.Invoke(evt);
        }

        void OnBlendFinished(ICinemachineMixer mixer, ICinemachineCamera cam)
        {
            if (cam == (ICinemachineCamera)m_Vcam)
                BlendFinishedEvent.Invoke(mixer, cam);
        }

        void OnCameraDeactivated(ICinemachineMixer mixer, ICinemachineCamera cam)
        {
            if (cam == (ICinemachineCamera)m_Vcam)
                CameraActivatedEvent.Invoke(mixer, cam);
        }
    }
}
