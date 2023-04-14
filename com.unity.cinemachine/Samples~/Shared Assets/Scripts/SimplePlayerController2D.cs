using UnityEngine;

namespace Unity.Cinemachine.Samples
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class SimplePlayerController2D : SimplePlayerControllerBase
    {
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

        void Update()
        {
            Cursor.lockState = LockCursor ? CursorLockMode.Locked : CursorLockMode.None;
            PreUpdate?.Invoke();
            var vel = m_Rigidbody2D.velocity;

            // Compute the new velocity and move the player, but only if not mid-jump
            if (m_IsGrounded)
            {
                // Read the input from the user
                m_IsSprinting = Sprint.Value > 0.5f;
                var desiredVel = new Vector2(MoveX.Value * (m_IsSprinting ? SprintSpeed : Speed), 0);
                if (Mathf.Max(MoveZ.Value, Jump.Value) > 0.01f)
                {
                    desiredVel.y = m_IsSprinting ? SprintJumpSpeed : JumpSpeed;
                    vel.y = m_IsSprinting ? SprintJumpSpeed : JumpSpeed;
                }
                vel.x = desiredVel.x;
            }

            // Rotate the player to face movement direction
            if (vel.x > Speed * 0.5f)
                transform.rotation = Quaternion.identity;
            if (vel.x < -Speed * 0.5f)
                transform.rotation = Quaternion.Euler(new Vector3(0, 180, 0));

            m_Rigidbody2D.velocity = vel;

            PostUpdate?.Invoke(
                Quaternion.Inverse(transform.rotation) * new Vector3(0, vel.y, vel.x), 
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
