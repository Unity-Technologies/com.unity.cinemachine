using UnityEngine;

namespace Unity.Cinemachine
{
    /// <summary>
    /// This component will generate CinemachineBrain-specific events.
    /// </summary>
    [AddComponentMenu("Cinemachine/Helpers/Cinemachine Brain Events")]
    [SaveDuringPlay]
    [HelpURL(Documentation.BaseURL + "manual/CinemachineBrainEvents.html")]
    public class CinemachineBrainEvents : CinemachineMixerEventsBase
    {
        /// <summary>
        /// This is the CinemachineBrain emitting the events.  If null and the current
        /// GameObject has a CinemachineBrain component, that component will be used.
        /// </summary>
        [Tooltip("This is the CinemachineBrain emitting the events.  If null and the current "
            + "GameObject has a CinemachineBrain component, that component will be used.")]
        public CinemachineBrain Brain;

        /// <summary>This event will fire after the brain updates its Camera.
        /// This evenet will only fire if this component is attached to a CinemachineBrain.</summary>
        [Tooltip("This event will fire after the brain updates its Camera.")]
        public CinemachineCore.BrainEvent BrainUpdatedEvent = new ();

        /// <summary>
        /// Get the object being monitored.  This is the object that will generate the events.
        /// </summary>
        /// <returns>The ICinemachineMixer object that is the origin of the events.</returns>
        protected override ICinemachineMixer GetMixer() => Brain;

        void OnEnable()
        {
            if (Brain == null)
                TryGetComponent(out Brain);
            if (Brain != null)
            {
                InstallHandlers(Brain);
                CinemachineCore.CameraUpdatedEvent.AddListener(OnCameraUpdated);
            }
        }

        void OnDisable()
        {
            UninstallHandlers();
            CinemachineCore.CameraUpdatedEvent.RemoveListener(OnCameraUpdated);
        }

        void OnCameraUpdated(CinemachineBrain brain)
        {
            if (brain == Brain)
                BrainUpdatedEvent.Invoke(brain);
        }
    }
}
