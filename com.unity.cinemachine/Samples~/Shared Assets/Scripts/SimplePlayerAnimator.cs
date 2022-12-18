using UnityEngine;

namespace Cinemachine.Examples
{
    /// <summary>
    /// Add-on for SimplePlayerController that controls animation for the Cameron character.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class SimplePlayerAnimator : MonoBehaviour
    {
        [Tooltip("Tune this to the animation in the model: feet should not slide when walking at this speed")]
        public float NormalWalkSpeed = 1.7f;

        [Tooltip("Tune this to the animation in the model: feet should not slide when sprinting at this speed")]
        public float NormalSprintSpeed = 5;

        [Tooltip("Never speed up the sprint animation more than this, to avoid absurdly fast movement")]
        public float MaxSprintScale = 1.4f;

        [Tooltip("Scale factor for the overall speed of the jump animation")]
        public float JumpAnimationScale = 0.65f;

        Animator m_Animator;
        SimplePlayerController m_Controller;
        Vector3 m_PreviousPosition; // used if m_Controller == null
        const float k_IdleThreshold = 0.3f;

        void Start()
        {
            m_PreviousPosition = transform.position;
            TryGetComponent(out m_Animator);
            if (TryGetComponent(out m_Controller))
            {
                m_Controller.StartJump += () => m_Animator.SetTrigger("Jump");
                m_Controller.EndJump += () => m_Animator.SetTrigger("Land");
                m_Controller.PostUpdate += () =>
                {
                    var vel = m_Controller.GetPlayerVelocity();
                    UpdateAnimation(vel);
                    m_Animator.SetFloat("JumpScale", JumpAnimationScale * (m_Controller.IsSprinting 
                        ? m_Controller.JumpSpeed / m_Controller.SprintJumpSpeed : 1));
                };
            }
        }

        // LateUpdate so we normally don't have to worry about script execution order:
        // we can assume that the player has already been moved
        void LateUpdate()
        {
            // In no-controller mode, we monitor the player's motion and deduce the appropriate animation.
            // We don't support jumping in this mode.
            if (m_Controller == null)
            {
                // Get velocity in player-local coords
                var pos = transform.position;
                var vel = Quaternion.Inverse(transform.rotation) * (pos - m_PreviousPosition) / Time.deltaTime;
                m_PreviousPosition = pos;
                UpdateAnimation(vel);
            }
        }

        void UpdateAnimation(Vector3 vel)
        {
            // Set animation params for current velocity
            vel.y = 0; // we don't consider vertical movement
            var speed = vel.magnitude;
            bool isRunning = speed > NormalWalkSpeed * 2;
            bool isWalking = !isRunning && speed > k_IdleThreshold;
            var dir = speed > k_IdleThreshold ? vel / speed : Vector3.zero;
            var motionScale = isWalking ? speed / NormalWalkSpeed : 1;

            // We scale the sprint animation speed to loosely match the actual speed, but we cheat
            // at the high end to avoid making the animation look ridiculous
            if (isRunning)
                motionScale = (speed < NormalSprintSpeed) 
                    ? speed / NormalSprintSpeed
                    : Mathf.Min(MaxSprintScale, 1 + (speed - NormalSprintSpeed) / (3 * NormalSprintSpeed));
            
            m_Animator.SetFloat("DirX", dir.x);
            m_Animator.SetFloat("DirZ", dir.z);
            m_Animator.SetFloat("MotionScale", motionScale);
            m_Animator.SetBool("Walking", isWalking);
            m_Animator.SetBool("Running", isRunning);
        }
    }
}
