using UnityEngine;

namespace Unity.Cinemachine.Samples
{
    /// <summary>
    /// This is a behaviour whose job it is to drive animation based on the player's motion.  
    /// It is a sample implementation that you can modify or replace with your own.  As shipped, it is 
    /// hardcoded to work specifically with the sample `CameronSimpleController` Animation controller, which 
    /// is set up with states that the SimplePlayerAnimator knows about.  You can modify 
    /// this class to work with your own animation controller.
    /// 
    /// SimplePlayerAnimator works with or without a SimplePlayerControllerBase alongside.  
    /// Without one, it monitors the transform's position and drives the animation accordingly.  
    /// You can see it used like this in some of the sample scenes, such as RunningRace or ClearShot.  
    /// In this mode, is it unable to detect the player's grounded state, and so it always 
    /// assumes that the player is grounded.
    /// 
    /// When a SimplePlayerControllerBase is detected, the SimplePlayerAnimator installs callbacks 
    /// and expects to be driven by the SimplePlayerControllerBase using the STartJump, EndJump, 
    /// and PostUpdate callbacks.
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
                m_Controller.StartJump += () => OnJump(true);
                m_Controller.EndJump += () => OnJump(false);
                m_Controller.PostUpdate += (vel, jumpAnimationScale) => UpdateAnimation(vel, jumpAnimationScale);
            }
        }
        
        /// <summary>
        /// LateUpdate is used to avoid having to worry about script execution order:
        /// it can be assumed that the player has already been moved.
        /// </summary>
        virtual protected void LateUpdate()
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

        /// <summary>
        /// Called by the SimplePlayerControllerBase when the player starts or ends a jump.
        /// Override this to interact appropriately with your animation controller.
        /// </summary>
        /// <param name="jumping">True when jump starts, false when it ends.</param>
        virtual protected void OnJump(bool jumping) => m_Animator.SetTrigger(jumping ? "Jump" : "Land");
        
        /// <summary>
        /// Update the animation based on the player's velocity.
        /// Override this to interact appropriately with your animation controller.
        /// </summary>
        /// <param name="vel">Player's velocity, in player-local coordinates.</param>
        /// <param name="jumpAnimationScale">Scale factor to apply to the jump animation.  
        /// It can be used to slow down the jump animation for longer jumps.</param>
        virtual protected void UpdateAnimation(Vector3 vel, float jumpAnimationScale)
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
