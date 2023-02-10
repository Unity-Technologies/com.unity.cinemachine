using UnityEngine;

namespace Cinemachine.Examples
{
    [RequireComponent(typeof(CinemachineMixingCamera))]
    public class MixCamerasBasedOnSpeed : MonoBehaviour
    {
        [Tooltip("At and above this speed, the second camera is going to be in control completely.")]
        public float MaxSpeed;
        public Rigidbody Rigidbody;
        CinemachineMixingCamera m_Mixer;
        float m_WeightDamping;
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
        
            var t = Mathf.Clamp01(Rigidbody.velocity.magnitude / MaxSpeed);
            m_Mixer.Weight1 = Mathf.SmoothDamp(m_Mixer.Weight1, Mathf.Clamp01(Rigidbody.velocity.magnitude / MaxSpeed), ref m_WeightDamping, 0.1f);
            m_Mixer.Weight0 = 1 - m_Mixer.Weight1;
        }
    }
}
