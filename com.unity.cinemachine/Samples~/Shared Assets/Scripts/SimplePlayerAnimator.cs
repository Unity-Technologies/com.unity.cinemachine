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

        SimplePlayerControllerBase m_Controller;
        Vector3 m_PreviousPosition; // used if m_Controller == null or disabled

        protected struct AnimationParams
        {
            public bool IsWalking;
            public bool IsRunning;
            public bool IsJumping;
            public bool LandTriggered;
            public bool JumpTriggered;
            public Vector3 Direction; // normalized direction of motion
            public float MotionScale; // scale factor for the animation speed
            public float JumpScale; // scale factor for the jump animation
        }
        AnimationParams m_AnimationParams;

        const float k_IdleThreshold = 0.2f;

        public enum States { Idle, Walk, Run, Jump, RunJump }

        /// <summary>Current state of the player</summary>
        public States State
        {
            get
            {
                if (m_AnimationParams.IsJumping)
                    return m_AnimationParams.IsRunning ? States.RunJump : States.Jump;
                if (m_AnimationParams.IsRunning)
                    return States.Run;
                return m_AnimationParams.IsWalking ? States.Walk : States.Idle;
            }
        }

        protected virtual void Start()
        {
            m_PreviousPosition = transform.position;
            m_Controller = GetComponentInParent<SimplePlayerControllerBase>();
            if (m_Controller != null)
            {
                // Install our callbacks to handle jump and animation based on velocity
                m_Controller.StartJump += () => m_AnimationParams.JumpTriggered = true;
                m_Controller.EndJump += () => m_AnimationParams.LandTriggered = true;
                m_Controller.PostUpdate += (vel, jumpAnimationScale) => UpdateAnimationState(vel, jumpAnimationScale);
            }
        }

        /// <summary>
        /// LateUpdate is used to avoid having to worry about script execution order:
        /// it can be assumed that the player has already been moved.
        /// </summary>
        protected virtual void LateUpdate()
        {
            // In no-controller mode, we monitor the player's motion and deduce the appropriate animation.
            // We don't support jumping in this mode.
            if (m_Controller == null || !m_Controller.enabled)
            {
                // Get velocity in player-local coords
                var pos = transform.position;
                var vel = Quaternion.Inverse(transform.rotation) * (pos - m_PreviousPosition) / Time.deltaTime;
                m_PreviousPosition = pos;
                UpdateAnimationState(vel, 1);
            }
        }

        /// <summary>
        /// Update the animation based on the player's velocity.
        /// Override this to interact appropriately with your animation controller.
        /// </summary>
        /// <param name="vel">Player's velocity, in player-local coordinates.</param>
        /// <param name="jumpAnimationScale">Scale factor to apply to the jump animation.
        /// It can be used to slow down the jump animation for longer jumps.</param>
        void UpdateAnimationState(Vector3 vel, float jumpAnimationScale)
        {
            vel.y = 0; // we don't consider vertical movement
            var speed = vel.magnitude;

            // Hysteresis reduction
            bool isRunning = speed > NormalWalkSpeed * 2 + (m_AnimationParams.IsRunning ? -0.15f : 0.15f);
            bool isWalking = !isRunning && speed > k_IdleThreshold + (m_AnimationParams.IsWalking ? -0.05f : 0.05f);
            m_AnimationParams.IsWalking = isWalking;
            m_AnimationParams.IsRunning = isRunning;

            // Set the normalized direction of motion and scale the animation speed to match motion speed
            m_AnimationParams.Direction = speed > k_IdleThreshold ? vel / speed : Vector3.zero;
            m_AnimationParams.MotionScale = isWalking ? speed / NormalWalkSpeed : 1;
            m_AnimationParams.JumpScale = JumpAnimationScale * jumpAnimationScale;

            // We scale the sprint animation speed to loosely match the actual speed, but we cheat
            // at the high end to avoid making the animation look ridiculous
            if (isRunning)
                m_AnimationParams.MotionScale = (speed < NormalSprintSpeed)
                    ? speed / NormalSprintSpeed
                    : Mathf.Min(MaxSprintScale, 1 + (speed - NormalSprintSpeed) / (3 * NormalSprintSpeed));

            UpdateAnimation(m_AnimationParams);

            if (m_AnimationParams.JumpTriggered)
                m_AnimationParams.IsJumping = true;
            if (m_AnimationParams.LandTriggered)
                m_AnimationParams.IsJumping = false;

            m_AnimationParams.JumpTriggered = false;
            m_AnimationParams.LandTriggered = false;
        }

        /// <summary>
        /// Update the animation based on the player's state.
        /// Override this to interact appropriately with your animation controller.
        /// </summary>
        protected virtual void UpdateAnimation(AnimationParams animationParams)
        {
            if (!TryGetComponent(out Animator animator))
            {
                Debug.LogError("SimplePlayerAnimator: An Animator component is required");
                return;
            }
            animator.SetFloat("DirX", animationParams.Direction.x);
            animator.SetFloat("DirZ", animationParams.Direction.z);
            animator.SetFloat("MotionScale", animationParams.MotionScale);
            animator.SetBool("Walking", animationParams.IsWalking);
            animator.SetBool("Running", animationParams.IsRunning);
            animator.SetFloat("JumpScale", animationParams.JumpScale);

            if (m_AnimationParams.JumpTriggered)
                animator.SetTrigger("Jump");
            if (m_AnimationParams.LandTriggered)
                animator.SetTrigger("Land");
        }
    }
}
