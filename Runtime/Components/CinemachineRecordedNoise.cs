using UnityEngine;
using UnityEngine.Serialization;

namespace Cinemachine
{
    /// <summary>
    /// As a part of the Cinemachine Pipeline implementing the Noise stage, this
    /// component adds noise to the Camera state, in the Correction
    /// channel of the CameraState.
    /// </summary>
    [DocumentationSorting(DocumentationSortingAttribute.Level.UserRef)]
    [AddComponentMenu("")] // Don't display in add component menu
    [SaveDuringPlay]
#if UNITY_2018_3_OR_NEWER
    [ExecuteAlways]
#else
    [ExecuteInEditMode]
#endif
    public class CinemachineRecordedNoise : CinemachineComponentBase
    {
        /// <summary>
        /// Defines the rotation signal that will be applied to the camera
        /// </summary>
        [Tooltip("Defines the rotation signal that will be applied to the camera")]
        [CinemachineEmbeddedAssetProperty(true)]
        public CinemachineFixedSignal m_SignalSource = null;

        /// <summary>
        /// When rotating the camera, offset the camera's pivot position by this much (camera space)
        /// </summary>
        [Tooltip("When rotating the camera, offset the camera's pivot position by this much (camera space)")]
        public Vector3 m_PivotOffset = Vector3.zero;

        /// <summary>
        /// Gain to apply to the amplitudes defined in the AnimationClip.
        /// </summary>
        [Tooltip("Gain to apply to the amplitudes defined in the AnimationClip asset.  1 is normal.  Setting this to 0 completely mutes the noise.")]
        public float m_AmplitudeGain = 1f;

        /// <summary>
        /// Scale factor to apply to the frequencies defined in the AnimationClip.
        /// </summary>
        [Tooltip("Scale factor to apply to the frequencies defined in the AnimationClip asset.  1 is normal.  Larger magnitudes will make the noise shake more rapidly.")]
        public float m_FrequencyGain = 1f;

        /// <summary>True if the component is valid, i.e. it has a noise definition and is enabled.</summary>
        public override bool IsValid { get { return enabled && m_SignalSource != null; } }

        /// <summary>Get the Cinemachine Pipeline stage that this component implements.
        /// Always returns the Noise stage</summary>
        public override CinemachineCore.Stage Stage { get { return CinemachineCore.Stage.Noise; } }


        void OnEnable() { Initialize(); }

        /// <summary>Applies noise to the Correction channel of the CameraState if the
        /// delta time is greater than 0.  Otherwise, does nothing.</summary>
        /// <param name="curState">The current camera state</param>
        /// <param name="deltaTime">How much to advance the perlin noise generator.
        /// Noise is only applied if this value is greater than or equal to 0</param>
        public override void MutateCameraState(ref CameraState curState, float deltaTime)
        {
            if (!IsValid || deltaTime < 0 || mLength < Epsilon)
            {
                mNoiseTime = 0;
                return;
            }

            mNoiseTime += deltaTime * m_FrequencyGain;
            if (mNoiseTime > mLength)
                mNoiseTime %= mLength;
            if (mNoiseTime < 0)
                mNoiseTime = mLength + (mNoiseTime % mLength);

            // We use pos component of signal as euler
            m_SignalSource.GetSignal(mNoiseTime, out Vector3 pos, out Quaternion rot);
            pos *= m_AmplitudeGain;
            Quaternion q = Quaternion.Euler(pos);
            curState.OrientationCorrection = curState.OrientationCorrection * q;

            if (m_PivotOffset != Vector3.zero)
            {
                Matrix4x4 m = Matrix4x4.Translate(-m_PivotOffset);
                m = Matrix4x4.Rotate(curState.CorrectedOrientation) * m;
                m = Matrix4x4.Translate(m_PivotOffset) * m;
                curState.PositionCorrection += m.MultiplyPoint(Vector3.zero);
            }
        }

        float mNoiseTime = 0;
        float mLength;

        private void Initialize()
        {
            mNoiseTime = 0;
            mLength = m_SignalSource == null ? 0 : m_SignalSource.SignalDuration;
        }
    }
}
