using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Unity.Cinemachine
{
    /// <summary>
    /// An extension for CinemachineCamera which post-processes
    /// the final position of the camera.  It listens for CinemachineImpulse
    /// signals on the specified channels, and moves the camera in response to them.
    /// </summary>
    [SaveDuringPlay]
    [AddComponentMenu("Cinemachine/Procedural/Extensions/Cinemachine Impulse Listener")]
    [ExecuteAlways]
    [HelpURL(Documentation.BaseURL + "manual/CinemachineImpulseListener.html")]
    public class CinemachineImpulseListener : CinemachineExtension
    {
        /// <summary>
        /// When to apply the impulse reaction.  Default is Noise.  
        /// Modify this if necessary to influence the ordering of extension effects
        /// </summary>
        [Tooltip("When to apply the impulse reaction.  Default is after the Noise stage.  "
            + "Modify this if necessary to influence the ordering of extension effects")]
        [FormerlySerializedAs("m_ApplyAfter")]
        public CinemachineCore.Stage ApplyAfter = CinemachineCore.Stage.Aim; // legacy compatibility setting

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
        [Tooltip("Gain to apply to the Impulse signal.  1 is normal strength.  "
            + "Setting this to 0 completely mutes the signal.")]
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
        [FormerlySerializedAs("m_UseCameraSpace")]
        public bool UseCameraSpace;

        /// <summary>
        /// This controls the secondary reaction of the listener to the incoming impulse.  
        /// The impulse might be for example a sharp shock, and the secondary reaction could
        /// be a vibration whose amplitude and duration is controlled by the size of the 
        /// original impulse.  This allows different listeners to respond in different ways 
        /// to the same impulse signal.
        /// </summary>
        [Serializable]
        public struct ImpulseReaction
        {
            /// <summary>
            /// Secondary shake that will be triggered by the primary impulse
            /// </summary>
            [Tooltip("Secondary shake that will be triggered by the primary impulse.")]
            public NoiseSettings m_SecondaryNoise;

            /// <summary>
            /// Gain to apply to the amplitudes defined in the signal source asset.
            /// </summary>
            [Tooltip("Gain to apply to the amplitudes defined in the signal source.  "  
                + "1 is normal.  Setting this to 0 completely mutes the signal.")]
            [FormerlySerializedAs("m_AmplitudeGain")]
            public float AmplitudeGain;
        
            /// <summary>
            /// Scale factor to apply to the time axis.
            /// </summary>
            [Tooltip("Scale factor to apply to the time axis.  1 is normal.  "
                + "Larger magnitudes will make the signal progress more rapidly.")]
           [FormerlySerializedAs("m_FrequencyGain")]
           public float FrequencyGain;

            /// <summary>
            /// How long the secondary reaction lasts.
            /// </summary>
            [Tooltip("How long the secondary reaction lasts.")]
            [FormerlySerializedAs("m_Duration")]
            public float Duration;

            float m_CurrentAmount;
            float m_CurrentTime;
            float m_CurrentDamping;

            bool m_Initialized;

            [SerializeField, HideInInspector, NoSaveDuringPlay]
            Vector3 m_NoiseOffsets;

            /// <summary>Generate a new random seed</summary>
            public void ReSeed()
            {
                m_NoiseOffsets = new Vector3(
                        UnityEngine.Random.Range(-1000f, 1000f),
                        UnityEngine.Random.Range(-1000f, 1000f),
                        UnityEngine.Random.Range(-1000f, 1000f));
            }

            /// <summary>
            /// Get the rection effect for a given impulse at a given time.
            /// </summary>
            /// <param name="deltaTime">Current time interval</param>
            /// <param name="impulsePos">The input impulse signal at this time</param>
            /// <param name="pos">output reaction position delta</param>
            /// <param name="rot">output reaction rotation delta</param>
            /// <returns>True if there is a reaction effect, false otherwise</returns>
            public bool GetReaction(
                float deltaTime, Vector3 impulsePos, 
                out Vector3 pos, out Quaternion rot)
            {
                if (!m_Initialized)
                {
                    m_Initialized = true;
                    m_CurrentAmount = 0;
                    m_CurrentDamping = 0;
                    m_CurrentTime = CinemachineCore.CurrentTime * FrequencyGain;
                    if (m_NoiseOffsets == Vector3.zero)
                        ReSeed();
                }

                // Is there any reacting to do?
                pos = Vector3.zero;
                rot = Quaternion.identity;
                var sqrMag = impulsePos.sqrMagnitude;
                if (m_SecondaryNoise == null || (sqrMag < 0.001f && m_CurrentAmount < 0.0001f))
                    return false;

                // Advance the current reaction time
                if (TargetPositionCache.CacheMode == TargetPositionCache.Mode.Playback
                        && TargetPositionCache.HasCurrentTime)
                    m_CurrentTime = TargetPositionCache.CurrentTime * FrequencyGain;
                else
                    m_CurrentTime += deltaTime * FrequencyGain;

                // Adjust the envelope height and duration of the secondary noise, 
                // according to the strength of the incoming signal
                m_CurrentAmount = Mathf.Max(m_CurrentAmount, Mathf.Sqrt(sqrMag));
                m_CurrentDamping = Mathf.Max(m_CurrentDamping, Mathf.Max(1, Mathf.Sqrt(m_CurrentAmount)) * Duration);

                var gain = m_CurrentAmount * AmplitudeGain;
                pos = NoiseSettings.GetCombinedFilterResults(
                        m_SecondaryNoise.PositionNoise, m_CurrentTime, m_NoiseOffsets) * gain;
                rot = Quaternion.Euler(NoiseSettings.GetCombinedFilterResults(
                        m_SecondaryNoise.OrientationNoise, m_CurrentTime, m_NoiseOffsets) * gain);

                m_CurrentAmount -= Damper.Damp(m_CurrentAmount, m_CurrentDamping, deltaTime);
                m_CurrentDamping -= Damper.Damp(m_CurrentDamping, m_CurrentDamping, deltaTime);
                return true;
            }
        }

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
        public ImpulseReaction ReactionSettings;

        private void Reset()
        {
            ApplyAfter = CinemachineCore.Stage.Noise; // this is the default setting
            ChannelMask = 1;
            Gain = 1;
            Use2DDistance = false;
            UseCameraSpace = true;
            ReactionSettings = new ImpulseReaction 
            { 
                AmplitudeGain = 1, 
                FrequencyGain = 1,
                Duration = 1f
            };
        }

        /// <summary>React to any detected impulses</summary>
        /// <param name="vcam">The virtual camera being processed</param>
        /// <param name="stage">The current pipeline stage</param>
        /// <param name="state">The current virtual camera state</param>
        /// <param name="deltaTime">The current applicable deltaTime</param>
        protected override void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            if (stage == ApplyAfter && deltaTime >= 0)
            {
                bool haveImpulse = CinemachineImpulseManager.Instance.GetImpulseAt(
                    state.GetFinalPosition(), Use2DDistance, ChannelMask, 
                    out var impulsePos, out var impulseRot);
                bool haveReaction = ReactionSettings.GetReaction(
                    deltaTime, impulsePos, out var reactionPos, out var reactionRot);

                if (haveImpulse)
                {
                    impulseRot = Quaternion.SlerpUnclamped(Quaternion.identity, impulseRot, Gain);
                    impulsePos *= Gain;
                }
                if (haveReaction)
                {
                    impulsePos += reactionPos;
                    impulseRot *= reactionRot;
                }
                if (haveImpulse || haveReaction)
                {
                    if (UseCameraSpace)
                        impulsePos = state.RawOrientation * impulsePos;
                    state.PositionCorrection += impulsePos;
                    state.OrientationCorrection = state.OrientationCorrection * impulseRot;
                }
            }
        }
    }
}
