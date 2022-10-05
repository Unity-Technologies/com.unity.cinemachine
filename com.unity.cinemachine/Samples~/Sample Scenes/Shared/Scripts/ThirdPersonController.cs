using System.Collections.Generic;
using UnityEngine;
using Cinemachine.Utility;
using System;

namespace Cinemachine.Examples
{
    public class ThirdPersonController : MonoBehaviour, IInputAxisSource
    {
        public float Speed = 1f;
        public float SprintSpeed = 4;
        public float JumpSpeed = 4;
        public float SprintJumpSpeed = 6;
    
        public float VelocityDamping = 0.5f;
        public float RotationDamping = 0.5f;

        public enum ForwardModes { Camera, Player, World };
        public ForwardModes InputForward = ForwardModes.Camera;
        public enum UpModes { Player, World };
        public UpModes UpMode = UpModes.World;
        public bool Strafe = false;

        public Action StartJump;
        public Action EndJump;

        [Header("Input Axes")]
        [Tooltip("X Axis movement.  Value is -1..1.  Controls the sideways movement")]
        public InputAxis MoveX = new InputAxis { Range = new Vector2(-1, 1) };

        [Tooltip("Z Axis movement.  Value is -1..1. Controls the forward movement")]
        public InputAxis MoveZ = new InputAxis { Range = new Vector2(-1, 1) };

        [Tooltip("Jump movement.  Value is 0 or 1. Controls the vertical movement")]
        public InputAxis Jump = new InputAxis { Range = new Vector2(0, 1) };

        [Tooltip("Sprint movement.  Value is 0 or 1. If true, then is sprinting")]
        public InputAxis Sprint = new InputAxis { Range = new Vector2(0, 1) };

        Vector3 m_CurrentVelocityXZ;
        float m_CurrentVelocityY;
        bool m_IsSprinting;
        bool m_IsJumping;
        float m_DistanceFromGround; // Valid only when not using character controller
        CharacterController m_Controller; // optional

        public Vector3 VelocityXZ => m_CurrentVelocityXZ;
        public float VelocityY => m_CurrentVelocityY;
        public bool IsSprinting => m_IsSprinting;
        public bool IsJumping => m_IsJumping;

        /// Report the available input axes to the input axis controller.
        /// We use the Input Axis Controller because it works with both the Input package
        /// and the Legacy input system.  This is sample code and we
        /// want it to work everywhere.
        void IInputAxisSource.GetInputAxes(List<IInputAxisSource.AxisDescriptor> axes)
        {
            axes.Add(new IInputAxisSource.AxisDescriptor { Axis = MoveX, Name = "Move X", AxisIndex = 0 });
            axes.Add(new IInputAxisSource.AxisDescriptor { Axis = MoveZ, Name = "Move Z", AxisIndex = 1 });
            axes.Add(new IInputAxisSource.AxisDescriptor { Axis = Jump, Name = "Jump", AxisIndex = 2 });
            axes.Add(new IInputAxisSource.AxisDescriptor { Axis = Sprint, Name = "Sprint", AxisIndex = 3 });
        }

        void Start()
        {
            TryGetComponent(out m_Controller);
        }

        private void OnEnable()
        {
            m_CurrentVelocityY = 0;
            m_IsSprinting = false;
            m_IsJumping = false;
        }

        void Update()
        {
            var up = UpMode == UpModes.World ? Vector3.up : transform.up;

            // Process Jump and gravity
            bool justLanded = ProcessJump(up);

            // Get the reference frame for the input
            var fwd = InputForward switch
            {
                ForwardModes.Camera => Camera.main.transform.forward,
                ForwardModes.Player => transform.forward,
                _ => Vector3.forward,
            };
            fwd = fwd.ProjectOntoPlane(up);
            fwd = fwd.normalized;
            if (fwd.sqrMagnitude < 0.01f)
                return;
            var inputFrame = Quaternion.LookRotation(fwd, Vector3.up);

            // Read the input from the user and put it in the input frame
            var input = inputFrame * new Vector3(MoveX.Value, 0, MoveZ.Value);
            if (input.sqrMagnitude > 1)
                input.Normalize();

            // Compute the new velocity and move the player, but only if not mid-jump
            if (!m_IsJumping)
            {
                m_IsSprinting = Sprint.Value > 0.5f;
                var desiredVelocity = input * (m_IsSprinting ? SprintSpeed : Speed);
                var damping = justLanded ? 0 : VelocityDamping;
                m_CurrentVelocityXZ += Damper.Damp(desiredVelocity - m_CurrentVelocityXZ, damping, Time.deltaTime);
            }
            
            // Apply the position change
            ApplyMotion(up);

            // If not strafing, rotate the player to face movement direction
            if (!Strafe && m_CurrentVelocityXZ.sqrMagnitude > 0.001f)
            {
                var qA = transform.rotation;
                var qB = Quaternion.LookRotation(
                    (InputForward == ForwardModes.Player && Vector3.Dot(fwd, m_CurrentVelocityXZ) < 0)
                        ? -m_CurrentVelocityXZ : m_CurrentVelocityXZ);
                var damping = justLanded ? 0 : RotationDamping;
                transform.rotation = Quaternion.Slerp(qA, qB, Damper.Damp(1, damping, Time.deltaTime));
            }
        }

        bool ProcessJump(Vector3 up)
        {
            const float kGravity = -9.8f;
            bool justLanded = false;
            m_CurrentVelocityY += kGravity * Time.deltaTime;
            if (!m_IsJumping && Jump.Value > 0.01f)
            {
                m_IsJumping = true;
                if (StartJump != null)
                    StartJump();
                m_CurrentVelocityY = m_IsSprinting ? SprintJumpSpeed : JumpSpeed;
            }
            if (IsGrounded(up) && m_CurrentVelocityY < 0)
            {
                m_CurrentVelocityY = 0;
                if (m_IsJumping)
                {
                    if (EndJump != null)
                        EndJump();
                    m_IsJumping = false;
                    justLanded = true;
                }
            }
            return justLanded;
        }

        bool IsGrounded(Vector3 up)
        {
            if (m_Controller != null)
                return m_Controller.isGrounded;

            // No controller - must compute manually with raycast
            m_DistanceFromGround = 0;
            if (Physics.Raycast(transform.position + up, -up, out var hit, 10, LayerMask.GetMask("Default")))
                m_DistanceFromGround = hit.distance - 1; // cache it for later (save a raycast)
            return m_DistanceFromGround <= 0.01f;
        }

        void ApplyMotion(Vector3 up)
        {
            var deltaPos = (m_CurrentVelocityY * up + m_CurrentVelocityXZ) * Time.deltaTime;
            if (m_Controller != null)
                m_Controller.Move(deltaPos);
            else
            {
                // Don't fall below ground
                if (m_CurrentVelocityY < 0 || m_DistanceFromGround < 0)
                {
                    var dy = -m_CurrentVelocityY * Time.deltaTime;
                    deltaPos += Mathf.Max(0, dy - m_DistanceFromGround) * up;
                }
                transform.position += deltaPos;
            }
        }
    }
}
