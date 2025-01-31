using System;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Unity.Cinemachine
{
    /// <summary>
    /// As a part of the Cinemachine Pipeline implementing the Noise stage, this
    /// component adds Perlin Noise to the Camera state, in the Correction
    /// channel of the CameraState.
    ///
    /// The noise is created by using a predefined noise profile asset.  This defines the
    /// shape of the noise over time.  You can scale this in amplitude or in time, to produce
    /// a large family of different noises using the same profile.
    /// </summary>
    /// <seealso cref="NoiseSettings"/>
    [AddComponentMenu("Cinemachine/Procedural/Noise/Cinemachine Basic Multi Channel Perlin")]
    [SaveDuringPlay]
    [DisallowMultipleComponent]
    [CameraPipeline(CinemachineCore.Stage.Noise)]
    [HelpURL(Documentation.BaseURL + "manual/CinemachineBasicMultiChannelPerlin.html")]
    public class CinemachineBasicMultiChannelPerlin
        : CinemachineComponentBase, CinemachineFreeLookModifier.IModifiableNoise
    {
        /// <summary>
        /// Serialized property for referencing a NoiseSettings asset
        /// </summary>
        [Tooltip("The asset containing the Noise Profile.  Define the frequencies and amplitudes "
            + "there to make a characteristic noise profile.  Make your own or just use one of the many presets.")]
        [FormerlySerializedAs("m_Definition")]
        [FormerlySerializedAs("m_NoiseProfile")]
        public NoiseSettings NoiseProfile;

        /// <summary>
        /// When rotating the camera, offset the camera's pivot position by this much (camera space)
        /// </summary>
        [Tooltip("When rotating the camera, offset the camera's pivot position by this much (camera space)")]
        [FormerlySerializedAs("m_PivotOffset")]
        public Vector3 PivotOffset = Vector3.zero;

        /// <summary>
        /// Gain to apply to the amplitudes defined in the settings asset.
        /// </summary>
        [Tooltip("Gain to apply to the amplitudes defined in the NoiseSettings asset.  1 is normal.  "
            + "Setting this to 0 completely mutes the noise.")]
        [FormerlySerializedAs("m_AmplitudeGain")]
        public float AmplitudeGain = 1f;

        /// <summary>
        /// Scale factor to apply to the frequencies defined in the settings asset.
        /// </summary>
        [Tooltip("Scale factor to apply to the frequencies defined in the NoiseSettings asset.  1 is normal.  "
            + "Larger magnitudes will make the noise shake more rapidly.")]
        [FormerlySerializedAs("m_FrequencyGain")]
        public float FrequencyGain = 1f;

        private bool m_Initialized = false;
        private float m_NoiseTime = 0;

        [SerializeField, HideInInspector, NoSaveDuringPlay, FormerlySerializedAs("mNoiseOffsets")]
        private Vector3 m_NoiseOffsets = Vector3.zero;

        (float, float) CinemachineFreeLookModifier.IModifiableNoise.NoiseAmplitudeFrequency
        {
            get => (AmplitudeGain, FrequencyGain);
            set { AmplitudeGain = value.Item1; FrequencyGain = value.Item2; }
        }

        /// <summary>True if the component is valid, i.e. it has a noise definition and is enabled.</summary>
        public override bool IsValid { get => enabled && NoiseProfile != null; }

        /// <summary>Get the Cinemachine Pipeline stage that this component implements.
        /// Always returns the Noise stage</summary>
        public override CinemachineCore.Stage Stage { get { return CinemachineCore.Stage.Noise; } }

        /// <summary>Applies noise to the Correction channel of the CameraState if the
        /// delta time is greater than 0.  Otherwise, does nothing.</summary>
        /// <param name="curState">The current camera state</param>
        /// <param name="deltaTime">How much to advance the perlin noise generator.
        /// Noise is only applied if this value is greater than or equal to 0</param>
        public override void MutateCameraState(ref CameraState curState, float deltaTime)
        {
            if (!IsValid || deltaTime < 0)
            {
                m_Initialized = false;
                return;
            }

            if (!m_Initialized)
                Initialize();

            if (TargetPositionCache.CacheMode == TargetPositionCache.Mode.Playback
                    && TargetPositionCache.HasCurrentTime)
                m_NoiseTime = TargetPositionCache.CurrentTime * FrequencyGain;
            else
                m_NoiseTime += deltaTime * FrequencyGain;
            curState.PositionCorrection += curState.GetCorrectedOrientation() * NoiseSettings.GetCombinedFilterResults(
                    NoiseProfile.PositionNoise, m_NoiseTime, m_NoiseOffsets) * AmplitudeGain;
            Quaternion rotNoise = Quaternion.Euler(NoiseSettings.GetCombinedFilterResults(
                    NoiseProfile.OrientationNoise, m_NoiseTime, m_NoiseOffsets) * AmplitudeGain);
            if (PivotOffset != Vector3.zero)
            {
                Matrix4x4 m = Matrix4x4.Translate(-PivotOffset);
                m = Matrix4x4.Rotate(rotNoise) * m;
                m = Matrix4x4.Translate(PivotOffset) * m;
                curState.PositionCorrection += curState.GetCorrectedOrientation() * m.MultiplyPoint(Vector3.zero);
            }
            curState.OrientationCorrection = curState.OrientationCorrection * rotNoise;
        }

        /// <summary>Generate a new random seed</summary>
        public void ReSeed()
        {
            m_NoiseOffsets = new Vector3(
                    Random.Range(-1000f, 1000f),
                    Random.Range(-1000f, 1000f),
                    Random.Range(-1000f, 1000f));
        }

        void Initialize()
        {
            m_Initialized = true;
            m_NoiseTime = CinemachineCore.CurrentTime * FrequencyGain;
            if (m_NoiseOffsets == Vector3.zero)
                ReSeed();
        }
    }
}
