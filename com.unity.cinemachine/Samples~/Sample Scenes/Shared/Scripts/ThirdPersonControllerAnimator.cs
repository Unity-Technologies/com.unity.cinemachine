using UnityEngine;

namespace Cinemachine.Examples
{
    /// <summary>
    /// Add-on for ThirdPersonController that controls animation for the Cameron character.
    /// </summary>
    [RequireComponent(typeof(ThirdPersonController))]
    [RequireComponent(typeof(Animator))]
    public class ThirdPersonControllerAnimator : MonoBehaviour
    {
        // Tune this to the animation in the model: feet should not slide when walking at this speed
        public float NormalWalkSpeed = 1; 

        public float SprintAnimationScale = 1;
        public float JumpAnimationScale = 1;

        Animator m_Animator;
        ThirdPersonController m_Controller;

        void Start()
        {
            TryGetComponent(out m_Animator);
            if (TryGetComponent(out m_Controller))
            {
                m_Controller.StartJump += () => m_Animator.SetTrigger("Jump");
                m_Controller.EndJump += () => m_Animator.SetTrigger("Land");
            }
        }

        void LateUpdate()
        {
            var vel = m_Controller.GetPlayerVelocity();
            var speed = new Vector2(vel.x, vel.z).magnitude;

            var speedX = m_Controller.Strafe ? vel.x : 0;
            var speedZ = m_Controller.Strafe ? vel.z : speed;
            
            var animSpeedZ = Mathf.Min(speedZ, m_Controller.Speed) / (m_Controller.Speed * 2);
            var sprintSpeed = Mathf.Abs(speedZ) - m_Controller.Speed;
            if (sprintSpeed > 0 && speedZ > m_Controller.Speed)
                animSpeedZ += Mathf.Sign(speedZ) * sprintSpeed / (m_Controller.SprintSpeed - m_Controller.Speed);

            var animSpeedX = Mathf.Clamp(speedX / m_Controller.Speed, -1, 1);
            var animSpeedY = Mathf.Clamp(vel.y / (m_Controller.IsSprinting ? m_Controller.SprintJumpSpeed : m_Controller.JumpSpeed), -1, 1);

            m_Animator.SetFloat("SpeedZ", animSpeedZ);
            m_Animator.SetFloat("SpeedX", animSpeedX);
            m_Animator.SetFloat("SpeedY", animSpeedY);
            m_Animator.SetFloat("WalkScale", m_Controller.IsSprinting ? SprintAnimationScale : m_Controller.Speed / NormalWalkSpeed);
            m_Animator.SetFloat("JumpScale", JumpAnimationScale * (m_Controller.IsSprinting ? m_Controller.JumpSpeed / m_Controller.SprintJumpSpeed : 1));
        }
    }
}
