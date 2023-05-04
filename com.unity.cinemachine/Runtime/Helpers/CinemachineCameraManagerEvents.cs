using UnityEngine;

namespace Unity.Cinemachine
{
    /// <summary>
    /// This component will generate mixer-specific events.
    /// </summary>
    [AddComponentMenu("Cinemachine/Helpers/Cinemachine Camera Manager Events")]
    [SaveDuringPlay]
    [HelpURL(Documentation.BaseURL + "manual/CinemachineCameraManagerEvents.html")]
    public class CinemachineCameraManagerEvents : CinemachineMixerEventsBase
    {
        /// <summary>
        /// This is the CinemachineBrain emitting the events.  If null and the current
        /// GameObject has a CinemachineBrain component, that component will be used.
        /// </summary>
        [Tooltip("This is the CinemachineCameraManager emitting the events.  If null and the current "
            + "GameObject has a CinemachineCameraManager component, that component will be used.")]
        public CinemachineCameraManagerBase CameraManager;

        /// <summary>
        /// Get the object being monitored.  This is the object that will generate the events.
        /// </summary>
        /// <returns>The ICinemachineMixer object that is the origin of the events.</returns>
        protected override ICinemachineMixer GetMixer() => CameraManager;

        void OnEnable()
        {
            if (CameraManager == null)
                TryGetComponent(out CameraManager);
            InstallHandlers(CameraManager);
        }

        void OnDisable()
        {
            UninstallHandlers();
        }
    }
}
