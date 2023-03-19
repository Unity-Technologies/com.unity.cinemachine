using System;
using UnityEngine;
using UnityEngine.Events;

namespace Unity.Cinemachine
{
    /// <summary>
    /// This component will generate camera-specific activation and deactivation events.
    /// Add it to a Cinemachine Camera.
    /// 
    /// GML todo: add events for
    ///  - Blend finished
    ///  - Camera deactivated
    ///  - Camera activated (deprecate OnCameraLive)
    /// </summary>
    [AddComponentMenu("Cinemachine/Helpers/Cinemachine Camera Events")]
    [SaveDuringPlay]
    [HelpURL(Documentation.BaseURL + "manual/CinemachineCameraEvents.html")]
    public class CinemachineCameraEvents : MonoBehaviour
    {
        /// <summary>
        /// This event is fired when a virtual camera is activated.
        /// If a blend is involved, it will be fired at the start of the blend.
        /// 
        /// Parameter ordering is: incomingCam, outgoingCam.
        /// </summary>
        [Serializable]
        public class OnCameraLiveEvent : UnityEvent<ICinemachineCamera, ICinemachineCamera> {}

        /// <summary>This event fires when the CinemachineCamera goes Live</summary>
        [Tooltip("This event fires when the CinemachineCamera goes Live")]
        public OnCameraLiveEvent OnCameraLive = new ();

        CinemachineVirtualCameraBase m_Vcam;

        void OnEnable()
        {
            TryGetComponent(out m_Vcam);
            if (m_Vcam != null)
            {
                CinemachineCore.CameraActivatedEvent.AddListener(OnCameraActivated);
            }
        }

        void OnDisable()
        {
            CinemachineCore.CameraActivatedEvent.RemoveListener(OnCameraActivated);
        }

        void OnCameraActivated(ICinemachineCamera.ActivationEventParams evt)
        {
            if (evt.IncomingCamera == (ICinemachineCamera)m_Vcam)
                OnCameraLive.Invoke(evt.IncomingCamera, evt.OutgoingCamera);
        }
    }
}
