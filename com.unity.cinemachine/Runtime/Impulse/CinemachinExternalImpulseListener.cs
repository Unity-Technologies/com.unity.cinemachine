using UnityEngine;
using UnityEngine.Serialization;

namespace Unity.Cinemachine
{
    /// <summary>
    /// This behaviour can be attached to any object to make it shake in response to Impulses.
    ///
    /// This behaviour can be attached to the main Camera with the CinemachineBrain,
    /// to allow the main camera to shake without putting Listeners on the virtual cameras.
    /// In this case, camera shake is not dependent on the active virtual camera.
    ///
    /// It is also possible to put this behaviour on other scene objects to shake them
    /// in response to impulses.
    /// </summary>
    [SaveDuringPlay]
    [AddComponentMenu("Cinemachine/Helpers/Cinemachine External Impulse Listener")]
    [HelpURL(Documentation.BaseURL + "manual/CinemachineExternalImpulseListener.html")]
    public class CinemachineExternalImpulseListener : MonoBehaviour
    {
        Vector3 m_ImpulsePosLastFrame;
        Quaternion m_ImpulseRotLastFrame;

        /// <summary>
        /// Impulse events on channels not included in the mask will be ignored.
        /// </summary>
        [Tooltip("Impulse events on channels not included in the mask will be ignored.")]
        [CinemachineImpulseChannelProperty]
        [FormerlySerializedAs("m_ChannelMask")]
        public int ChannelMask;

        /// <summary>
        /// Gain to apply to the Impulse signal.
        /// </summary>
        [Tooltip("Gain to apply to the Impulse signal.  1 is normal strength.  Setting this to 0 completely mutes the signal.")]
        [FormerlySerializedAs("m_Gain")]
        public float Gain;

        /// <summary>
        /// Enable this to perform distance calculation in 2D (ignore Z).
        /// </summary>
        [Tooltip("Enable this to perform distance calculation in 2D (ignore Z)")]
        [FormerlySerializedAs("m_Use2DDistance")]
        public bool Use2DDistance;

        /// <summary>
        /// Enable this to process all impulse signals in camera space.
        /// </summary>
        [Tooltip("Enable this to process all impulse signals in camera space")]
        [FormerlySerializedAs("m_UseLocalSpace")]
        public bool UseLocalSpace;

        /// <summary>
        /// This controls the secondary reaction of the listener to the incoming impulse.
        /// The impulse might be for example a sharp shock, and the secondary reaction could
        /// be a vibration whose amplitude and duration is controlled by the size of the
        /// original impulse.  This allows different listeners to respond in different ways
        /// to the same impulse signal.
        /// </summary>
        [Tooltip("This controls the secondary reaction of the listener to the incoming impulse.  "
            + "The impulse might be for example a sharp shock, and the secondary reaction could "
            + "be a vibration whose amplitude and duration is controlled by the size of the "
            + "original impulse.  This allows different listeners to respond in different ways "
            + "to the same impulse signal.")]
        [FormerlySerializedAs("m_ReactionSettings")]
        public CinemachineImpulseListener.ImpulseReaction ReactionSettings;

        private void Reset()
        {
            ChannelMask = 1;
            Gain = 1;
            Use2DDistance = false;
            UseLocalSpace = true;
            ReactionSettings = new CinemachineImpulseListener.ImpulseReaction
            {
                AmplitudeGain = 1,
                FrequencyGain = 1,
                Duration = 1f
            };
        }

        private void OnEnable()
        {
            m_ImpulsePosLastFrame = Vector3.zero;
            m_ImpulseRotLastFrame = Quaternion.identity;
        }

        private void Update()
        {
            // Unapply previous shake
            transform.position -= m_ImpulsePosLastFrame;
            transform.rotation = transform.rotation * Quaternion.Inverse(m_ImpulseRotLastFrame);
        }

        // We do this in LateUpdate specifically to support attaching this script to the
        // Camera with the CinemachineBrain.  Script execution order is after the brain.
        private void LateUpdate()
        {
            // Apply the shake
            bool haveImpulse = CinemachineImpulseManager.Instance.GetImpulseAt(
                transform.position, Use2DDistance, ChannelMask,
                out m_ImpulsePosLastFrame, out m_ImpulseRotLastFrame);
            bool haveReaction = ReactionSettings.GetReaction(
                Time.deltaTime, m_ImpulsePosLastFrame, out var reactionPos, out var reactionRot);

            if (haveImpulse)
            {
                m_ImpulseRotLastFrame = Quaternion.SlerpUnclamped(
                    Quaternion.identity, m_ImpulseRotLastFrame, Gain);
                m_ImpulsePosLastFrame *= Gain;
            }
            if (haveReaction)
            {
                m_ImpulsePosLastFrame += reactionPos;
                m_ImpulseRotLastFrame *= reactionRot;
            }
            if (haveImpulse || haveReaction)
            {
                if (UseLocalSpace)
                    m_ImpulsePosLastFrame = transform.rotation * m_ImpulsePosLastFrame;

                transform.position += m_ImpulsePosLastFrame;
                transform.rotation = transform.rotation * m_ImpulseRotLastFrame;
            }
        }
    }
}
