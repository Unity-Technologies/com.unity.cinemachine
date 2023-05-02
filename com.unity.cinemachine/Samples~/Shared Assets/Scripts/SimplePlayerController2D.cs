using UnityEngine;

namespace Unity.Cinemachine.Samples
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class SimplePlayerController2D : SimplePlayerControllerBase
    {
        public Transform PlayerGeometry;
        public bool MotionControlWhileInAir;

        bool m_IsSprinting;
        bool m_IsGrounded;
        Rigidbody2D m_Rigidbody2D;

        public bool IsSprinting => m_IsSprinting;
        public bool IsJumping => !m_IsGrounded;
        public bool IsMoving => Mathf.Abs(m_Rigidbody2D.velocity.x) > 0.01f;

        void Start() => TryGetComponent(out m_Rigidbody2D);
        private void OnEnable()
        {
            m_IsGrounded = true;
            m_IsSprinting = false;
        }

        void FixedUpdate()
        {
            PreUpdate?.Invoke();
            var vel = m_Rigidbody2D.velocity;

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
            m_Rigidbody2D.velocity = vel;

            PostUpdate?.Invoke(
                new Vector3(0, vel.y, Mathf.Abs(vel.x)), 
                m_IsSprinting ? JumpSpeed / SprintJumpSpeed : 1);
        }

        // Ground detection only works if the player has a small trigger collider under its feet
        void OnTriggerEnter2D(Collider2D collision)
        {
            if (!collision.isTrigger && !m_IsGrounded)
            {
                EndJump?.Invoke();
                Landed.Invoke();
                m_IsGrounded = true;
            }
        }
        void OnTriggerExit2D(Collider2D collision)
        {
            if (!collision.isTrigger && m_IsGrounded)
            {
                StartJump?.Invoke();
                m_IsGrounded = false;
            }
        }
        void OnTriggerStay2D(Collider2D collision) => OnTriggerEnter2D(collision);
    }
}
