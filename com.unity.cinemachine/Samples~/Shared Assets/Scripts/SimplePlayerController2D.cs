using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Unity.Cinemachine.Samples
{
    /// <summary>
    /// This is a very basic 2D implementation of SimplePlayerControllerBase.
    ///
    /// It requires a [Rigidbody2D](https://docs.unity3d.com/ScriptReference/Rigidbody2D.html) component
    /// to be placed on the player GameObject.  Because it works with a Rigidbody2D, motion control is
    /// implemented in the `FixedUpdate()` method.
    ///
    /// Ground detection only works if the player has a small trigger collider under its feet.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class SimplePlayerController2D : MonoBehaviour, IInputAxisOwner, ISimplePlayerAnimatable
    {
        [Tooltip("Ground speed when walking")]
        public float Speed = 1f;
        [Tooltip("Ground speed when sprinting")]
        public float SprintSpeed = 4;
        [Tooltip("Initial vertical speed when jumping")]
        public float JumpSpeed = 4;
        [Tooltip("Initial vertical speed when sprint-jumping")]
        public float SprintJumpSpeed = 6;

        [Tooltip("Reference to the child object that holds the player's visible geometry.  "
            + "'It is rotated to face the direction of motion")]
        public Transform PlayerGeometry;

        [Tooltip("Makes possible to influence the direction of motion while the character is "
            + "in the air.  Otherwise, the more realistic rule that the feet must be touching the ground applies.")]
        public bool MotionControlWhileInAir;

        /// We use the InputAxis because it works with both the Input package
        /// and the Legacy input system.  The axis values are driven by the InputAxisController.
        /// This can be replaced by any other desired method of reading user input.
        [Header("Input Axes")]
        [Tooltip("X Axis movement.  Value is -1..1.  Controls the sideways movement")]
        public InputAxis MoveX = InputAxis.DefaultMomentary;

        [Tooltip("Z Axis movement.  Value is -1..1. Controls the forward movement")]
        public InputAxis MoveZ = InputAxis.DefaultMomentary;

        [Tooltip("Jump movement.  Value is 0 or 1. Controls the vertical movement")]
        public InputAxis Jump = InputAxis.DefaultMomentary;

        [Tooltip("Sprint movement.  Value is 0 or 1. If 1, then is sprinting")]
        public InputAxis Sprint = InputAxis.DefaultMomentary;

        /// Report the available input axes so they can be discovered by the InpusAxisController component.
        void IInputAxisOwner.GetInputAxes(List<IInputAxisOwner.AxisDescriptor> axes)
        {
            axes.Add(new () { DrivenAxis = () => ref MoveX, Name = "Move X", Hint = IInputAxisOwner.AxisDescriptor.Hints.X });
            axes.Add(new () { DrivenAxis = () => ref MoveZ, Name = "Move Z", Hint = IInputAxisOwner.AxisDescriptor.Hints.Y });
            axes.Add(new () { DrivenAxis = () => ref Jump, Name = "Jump" });
            axes.Add(new () { DrivenAxis = () => ref Sprint, Name = "Sprint" });
        }
        
        [Header("Events")]
        [Tooltip("This event is sent when the player lands after a jump.")]
        public UnityEvent Landed = new ();

        public Action PreUpdate;

        bool m_IsSprinting;
        bool m_IsGrounded;
        Rigidbody2D m_Rigidbody2D;

#if UNITY_6000_1_OR_NEWER
        public bool IsMoving => Mathf.Abs(m_Rigidbody2D.linearVelocity.x) > 0.01f;
#else
        #pragma warning disable CS0618 // obsolete for 6000.0.0f11 and newer
        public bool IsMoving => Mathf.Abs(m_Rigidbody2D.velocity.x) > 0.01f;
        #pragma warning restore CS0618
#endif

        // ISimplePlayerAnimatable implementation
        public bool IsJumping => !m_IsGrounded;
        public float JumpScale => m_IsSprinting ? JumpSpeed / SprintJumpSpeed : 1;
        public Vector3 LocalSpaceVelocity
        {
            get
            {
                var vel = m_Rigidbody2D.velocity;
                return new Vector3(0, vel.y, Mathf.Abs(vel.x));  // always moving forwards              
            }
        }

        public bool IsSprinting => m_IsSprinting;

        void Start() => TryGetComponent(out m_Rigidbody2D);

        private void OnEnable()
        {
            m_IsGrounded = true;
            m_IsSprinting = false;
        }

        void FixedUpdate()
        {
            //PreUpdate?.Invoke();
#if UNITY_6000_1_OR_NEWER
            var vel = m_Rigidbody2D.linearVelocity;
#else
            #pragma warning disable CS0618 // obsolete for 6000.0.0f11 and newer
            var vel = m_Rigidbody2D.velocity;
            #pragma warning restore CS0618
#endif

            // Compute the new velocity and move the player, but only if not mid-jump
            if (m_IsGrounded || MotionControlWhileInAir)
            {
                // Read the input from the user
                m_IsSprinting = Sprint.Value > 0.5f;
                vel.x = MoveX.Value * (m_IsSprinting ? SprintSpeed : Speed);
                if (m_IsGrounded && Mathf.Max(MoveZ.Value, Jump.Value) > 0.01f)
                    vel.y = m_IsSprinting ? SprintJumpSpeed : JumpSpeed;
            }

            // Rotate the player to face movement direction
            if (PlayerGeometry.rotation != null)
            {
                if (vel.x > Speed * 0.5f)
                    PlayerGeometry.rotation = Quaternion.Euler(new Vector3(0, 90, 0));
                if (vel.x < -Speed * 0.5f)
                    PlayerGeometry.rotation = Quaternion.Euler(new Vector3(0, -90, 0));
            }
#if UNITY_6000_1_OR_NEWER
            m_Rigidbody2D.linearVelocity = vel;
#else
            #pragma warning disable CS0618 // obsolete for 6000.0.0f11 and newer
            m_Rigidbody2D.velocity = vel;
            #pragma warning restore CS0618
#endif
        }

        // Ground detection only works if the player has a small trigger collider under its feet
        void OnTriggerEnter2D(Collider2D collision)
        {
            if (!collision.isTrigger && !m_IsGrounded)
            {
                Landed.Invoke();
                m_IsGrounded = true;
            }
        }
        void OnTriggerExit2D(Collider2D collision)
        {
            if (!collision.isTrigger && m_IsGrounded)
                m_IsGrounded = false;
        }
        void OnTriggerStay2D(Collider2D collision) => OnTriggerEnter2D(collision);
    }
}
