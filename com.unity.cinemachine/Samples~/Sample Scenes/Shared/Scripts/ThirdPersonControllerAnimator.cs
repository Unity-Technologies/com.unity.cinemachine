using UnityEngine;

namespace Cinemachine.Examples
{
    [RequireComponent(typeof(ThirdPersonController))]
    [RequireComponent(typeof(Animator))]
    public class ThirdPersonControllerAnimator : MonoBehaviour
    {
        // Tune this to the animation in the model: feet should not slide when walking at this speed
        public float NormalWalkSpeed = 1; 

        public float SprintAnimationScale = 1;
        public float JumpAnimationScale = 1;

        public string AnimationSpeedVariable = "Speed";
        public string AnimationWalkScaleVariable = "WalkScale";
        public string AnimationJumpScaleVariable = "JumpScale";
        public string AnimationStartJumpTrigger = "Jump";
        public string AnimationEndJumpTrigger = "Land";

        Animator m_Animator;
        ThirdPersonController m_Controller;

        void Start()
        {
            TryGetComponent(out m_Animator);
            if (TryGetComponent(out m_Controller))
            {
                m_Controller.StartJump += () => m_Animator.SetTrigger(AnimationStartJumpTrigger);
                m_Controller.EndJump += () => m_Animator.SetTrigger(AnimationEndJumpTrigger);
            }
        }

        void LateUpdate()
        {
            var curSpeed = m_Controller.VelocityXZ.magnitude;
            var animSpeed = Mathf.Lerp(0, 0.5f, curSpeed / m_Controller.Speed);
            if (m_Controller.SprintSpeed > m_Controller.Speed && curSpeed > m_Controller.Speed)
                animSpeed += Mathf.Lerp(0, 0.5f, (curSpeed - m_Controller.Speed) / (m_Controller.SprintSpeed - m_Controller.Speed));
            m_Animator.SetFloat(AnimationSpeedVariable, animSpeed);
            m_Animator.SetFloat(AnimationWalkScaleVariable, m_Controller.IsSprinting ? SprintAnimationScale : m_Controller.Speed / NormalWalkSpeed);
            m_Animator.SetFloat(AnimationJumpScaleVariable, JumpAnimationScale * (m_Controller.IsSprinting ? m_Controller.JumpSpeed / m_Controller.SprintJumpSpeed : 1));
        }
    }
}
