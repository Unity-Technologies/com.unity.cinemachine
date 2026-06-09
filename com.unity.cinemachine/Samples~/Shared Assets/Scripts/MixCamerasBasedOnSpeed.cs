using UnityEngine;

namespace Unity.Cinemachine.Samples
{
    [RequireComponent(typeof(CinemachineMixingCamera))]
    public class MixCamerasBasedOnSpeed : MonoBehaviour
    {
        [Tooltip("At and above this speed, the second camera is going to be in control completely.")]
        public float MaxSpeed;
        public Rigidbody Rigidbody;
        CinemachineMixingCamera m_Mixer;
        float m_CurrentWeight;

        void Start()
        {
            m_Mixer = GetComponent<CinemachineMixingCamera>();
        }

        void OnValidate()
        {
            MaxSpeed = Mathf.Max(1, MaxSpeed);
        }

        void Update()
        {
            if (Rigidbody == null)
                return;

#if UNITY_2023_3_OR_NEWER
            var t = Mathf.Clamp01(Rigidbody.linearVelocity.magnitude / MaxSpeed);
#else
            var t = Mathf.Clamp01(Rigidbody.velocity.magnitude / MaxSpeed);
#endif
            // Weights are not smoothed by cinemachine. If the weights come from a non-smooth source
            // (in this case physics), it is important to smooth them to avoid jitter
            m_CurrentWeight = Mathf.Lerp(m_CurrentWeight, t, Time.deltaTime);

            m_Mixer.Weight0 = 1 - m_CurrentWeight;
            m_Mixer.Weight1 = m_CurrentWeight;
        }
    }
}
