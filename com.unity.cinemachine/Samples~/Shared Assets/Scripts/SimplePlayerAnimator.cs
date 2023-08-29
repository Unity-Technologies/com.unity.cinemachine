using UnityEngine;

namespace Unity.Cinemachine.Samples
{
    /// <summary>
    /// Add-on for SimplePlayerController that controls animation for the Cameron character.
    /// It is hardcoded specifically to drive the CameronSimpleController animation controller asset.
    /// If the SimplePlayerController behaviour is present, this behaviour will work with it, otherwise
    /// it will monitor player velocity and apply the appropriate animation based on player movement.
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
        SimplePlayerControllerBase m_Controller;
        Vector3 m_PreviousPosition; // used if m_Controller == null or disabled
        bool m_WasWalking;
        bool m_WasRunning;
        const float k_IdleThreshold = 0.2f;

        void Start()
        {
            m_PreviousPosition = transform.position;
            TryGetComponent(out m_Animator);
            m_Controller = GetComponentInParent<SimplePlayerControllerBase>();
            if (m_Controller != null)
            {
                // Install our callbacks to handle jump and animation based on velocity
                m_Controller.StartJump += () => m_Animator.SetTrigger("Jump");
                m_Controller.EndJump += () => m_Animator.SetTrigger("Land");
                m_Controller.PostUpdate += (vel, jumpAnimationScale) => UpdateAnimation(vel, jumpAnimationScale);
            }
        }
        
        // We use LateUpdate so we normally don't have to worry about script execution order:
        // we can assume that the player has already been moved.
        void LateUpdate()
        {
            // In no-controller mode, we monitor the player's motion and deduce the appropriate animation.
            // We don't support jumping in this mode.
            if (m_Controller == null || !m_Controller.enabled)
            {
                // Get velocity in player-local coords
                var pos = transform.position;
                var vel = Quaternion.Inverse(transform.rotation) * (pos - m_PreviousPosition) / Time.deltaTime;
                m_PreviousPosition = pos;
                UpdateAnimation(vel, 1);
            }
        }
        
        // Set animation params for current velocity
        void UpdateAnimation(Vector3 vel, float jumpAnimationScale)
        {
            vel.y = 0; // we don't consider vertical movement
            var speed = vel.magnitude;

            // Hysteresis reduction
            bool isRunning = speed > NormalWalkSpeed * 2 + (m_WasRunning ? -0.15f : 0.15f);
            bool isWalking = !isRunning && speed > k_IdleThreshold + (m_WasWalking ? -0.05f : 0.05f);
            m_WasWalking = isWalking;
            m_WasRunning = isRunning;

            // Set the normalized direction of motion and scale the animation speed to match motion speed
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
            m_Animator.SetFloat("JumpScale", JumpAnimationScale * jumpAnimationScale);
        }
    }
}
