using System;
using UnityEngine;

namespace Unity.Cinemachine.Samples
{
    /// <summary>
    /// This interface should be implemented by a character controller in order to be compatible 
    /// with the SimplePlayerAnimator and support jumping.
    /// </summary>
    public interface ISimplePlayerAnimatable
    {
        /// <summary>True if player is in a jump state, false otherwise</summary>
        public bool IsJumping { get; }

        /// <summary>Scale at which to play the jump animation (can be slower for longer-lasting jumps).
        /// 1 is normal speed, < 1 is slower, > 1 is faster.</summary>
        public float JumpScale { get; }

        /// <summary>Velocity in player-local space</summary>
        public Vector3 LocalSpaceVelocity { get; }
    }
    
    /// <summary>
    /// This is a behaviour whose job it is to drive animation based on the player's motion.
    /// It is a sample implementation that you can modify or replace with your own.  As shipped, it is
    /// hardcoded to work specifically with the sample `CameronSimpleController` Animation controller, which
    /// is set up with states that the SimplePlayerAnimator knows about.  You can modify
    /// this class to work with your own animation controller.
    ///
    /// SimplePlayerAnimator works with or without a ISimplePlayerAnimatable alongside.
    /// Without one, it monitors the transform's position and drives the animation accordingly.
    /// You can see it used like this in some of the sample scenes, such as RunningRace or ClearShot.
    /// In this mode, is it unable to detect the player's grounded state, and so it always
    /// assumes that the player is grounded.
    ///
    /// When a ISimplePlayerAnimatable is detected, the SimplePlayerAnimator can detect
    /// jump state and react accordingly.
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

        ISimplePlayerAnimatable m_Controller;
        Vector3 m_PreviousPosition; // used if m_Controller == null or disabled
        Transform m_Transform;  // cached for efficiency
        Animator m_Animator;

        // These match the blackboard values of the motion controller
        protected struct AnimationParams
        {
            public bool IsWalking;
            public bool IsRunning;
            public bool IsJumping;
            public Vector3 Direction; // normalized direction of motion
            public float MotionScale; // scale factor for the animation speed
            public float JumpScale; // scale factor for the jump animation
        }
        AnimationParams m_AnimationParams;

        const float k_IdleThreshold = 0.2f;

        public enum States { Idle, Walk, Run, Jump }

        /// <summary>Current state of the player</summary>
        public States State
        {
            get
            {
                if (m_AnimationParams.IsJumping)
                    return States.Jump;
                if (m_AnimationParams.IsRunning)
                    return States.Run;
                if (m_AnimationParams.IsWalking)
                    return States.Walk;
                return States.Idle;
            }
        }

        protected virtual void Start()
        {
            m_Transform = transform;
            m_Animator = GetComponent<Animator>();
            m_PreviousPosition = m_Transform.position;
            m_Controller = GetComponentInParent<ISimplePlayerAnimatable>();
        }

        /// <summary>
        /// LateUpdate is used to avoid having to worry about script execution order:
        /// it can be assumed that the player has already been moved.
        /// </summary>
        protected virtual void LateUpdate()
        {
            // In no-controller mode, we monitor the player's motion and deduce the appropriate animation.
            // We don't support jumping in this mode.
            if (m_Controller == null || (m_Controller is MonoBehaviour b && !b.enabled))
            {
                // Get velocity in player-local coords
                var pos = m_Transform.position;
                var vel = Quaternion.Inverse(m_Transform.rotation) * (pos - m_PreviousPosition) / Time.deltaTime;
                m_PreviousPosition = pos;
                UpdateAnimationState(vel, false, 1);
                return;
            }

            // If there is a controller, we can handle jumping
            UpdateAnimationState(m_Controller.LocalSpaceVelocity, m_Controller.IsJumping, m_Controller.JumpScale);
        }

        /// <summary>
        /// Update the animation based on the player's velocity.
        /// Override this to interact appropriately with your animation controller.
        /// </summary>
        /// <param name="vel">Player's velocity, in player-local coordinates.</param>
        /// <param name="isJumping">True if player is in a jump state.
        /// <param name="jumpAnimationScale">Scale factor to apply to the jump animation.
        /// It can be used to slow down the jump animation for longer jumps.</param>
        void UpdateAnimationState(Vector3 vel, bool isJumping, float jumpAnimationScale)
        {
            vel.y = 0; // we don't consider vertical movement
            var speed = vel.magnitude;

            // Hysteresis reduction
            bool isRunning = speed > NormalWalkSpeed * 2 + (m_AnimationParams.IsRunning ? -0.15f : 0.15f);
            bool isWalking = !isRunning && speed > k_IdleThreshold + (m_AnimationParams.IsWalking ? -0.05f : 0.05f);
            m_AnimationParams.IsWalking = isWalking;
            m_AnimationParams.IsRunning = isRunning;
            m_AnimationParams.IsJumping = isJumping;

            // Set the normalized direction of motion and scale the animation speed to match motion speed
            m_AnimationParams.Direction = speed > k_IdleThreshold ? vel / speed : Vector3.zero;
            m_AnimationParams.MotionScale = isWalking ? speed / NormalWalkSpeed : 1;
            m_AnimationParams.JumpScale = JumpAnimationScale * jumpAnimationScale;

            // We scale the sprint animation speed to loosely match the actual speed, but we cheat
            // at the high end to avoid making the animation look ridiculous
            if (isRunning)
            {
                m_AnimationParams.MotionScale = (speed < NormalSprintSpeed)
                    ? speed / NormalSprintSpeed
                    : Mathf.Min(MaxSprintScale, 1 + (speed - NormalSprintSpeed) / (3 * NormalSprintSpeed));
            }
            UpdateAnimation(m_AnimationParams);
        }

        /// <summary>
        /// Update the animation based on the player's state.
        /// Override this to interact appropriately with your animation controller.
        /// </summary>
        protected virtual void UpdateAnimation(AnimationParams animationParams)
        {
            m_Animator.SetFloat("DirX", animationParams.Direction.x);
            m_Animator.SetFloat("DirZ", animationParams.Direction.z);
            m_Animator.SetFloat("MotionScale", animationParams.MotionScale);
            m_Animator.SetBool("Walking", animationParams.IsWalking);
            m_Animator.SetBool("Running", animationParams.IsRunning);
            m_Animator.SetBool("Jumping", animationParams.IsJumping);
            m_Animator.SetFloat("JumpScale", animationParams.JumpScale);
        }
    }
}
