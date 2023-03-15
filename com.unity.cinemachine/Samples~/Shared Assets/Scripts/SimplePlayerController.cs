using System.Collections.Generic;
using UnityEngine;
using System;

namespace Unity.Cinemachine.Samples
{
    public class SimplePlayerController : MonoBehaviour, IInputAxisOwner
    {
        public float Speed = 1f;
        public float SprintSpeed = 4;
        public float JumpSpeed = 4;
        public float SprintJumpSpeed = 6;
    
        public float Damping = 0.5f;

        public enum ForwardModes { Camera, Player, World };
        public ForwardModes InputForward = ForwardModes.Camera;
        public enum UpModes { Player, World };
        public UpModes UpMode = UpModes.World;

        public bool Strafe = false;
        public bool LockCursor = false;

        public Action PreUpdate;
        public Action PostUpdate;
        public Action StartJump;
        public Action EndJump;

        [Header("Input Axes")]
        [Tooltip("X Axis movement.  Value is -1..1.  Controls the sideways movement")]
        public InputAxis MoveX = InputAxis.DefaultMomentary;

        [Tooltip("Z Axis movement.  Value is -1..1. Controls the forward movement")]
        public InputAxis MoveZ = InputAxis.DefaultMomentary;

        [Tooltip("Jump movement.  Value is 0 or 1. Controls the vertical movement")]
        public InputAxis Jump = InputAxis.DefaultMomentary;

        [Tooltip("Sprint movement.  Value is 0 or 1. If 1, then is sprinting")]
        public InputAxis Sprint = InputAxis.DefaultMomentary;

        Vector3 m_CurrentVelocityXZ;
        Vector3 m_LastInput;
        float m_CurrentVelocityY;
        bool m_IsSprinting;
        bool m_IsJumping;
        CharacterController m_Controller; // optional

        // Get velocity relative to player transform
        public Vector3 GetPlayerVelocity()
        {
            var vel = Quaternion.Inverse(transform.rotation) * m_CurrentVelocityXZ;
            vel.y = m_CurrentVelocityY;
            return vel;
        }

        public bool IsSprinting => m_IsSprinting;
        public bool IsJumping => m_IsJumping;
        public bool IsMoving => m_LastInput.sqrMagnitude > 0.01f;

        public void EnableLockCursor(bool enable) => LockCursor = enable;

        /// Report the available input axes to the input axis controller.
        /// We use the Input Axis Controller because it works with both the Input package
        /// and the Legacy input system.  This is sample code and we
        /// want it to work everywhere.
        void IInputAxisOwner.GetInputAxes(List<IInputAxisOwner.AxisDescriptor> axes)
        {
            axes.Add(new () { DrivenAxis = () => ref MoveX, Name = "Move X", Hint = IInputAxisOwner.AxisDescriptor.Hints.X });
            axes.Add(new () { DrivenAxis = () => ref MoveZ, Name = "Move Z", Hint = IInputAxisOwner.AxisDescriptor.Hints.Y });
            axes.Add(new () { DrivenAxis = () => ref Jump, Name = "Jump" });
            axes.Add(new () { DrivenAxis = () => ref Sprint, Name = "Sprint" });
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
            Cursor.lockState = LockCursor ? CursorLockMode.Locked : CursorLockMode.None;

            PreUpdate?.Invoke();

            // Process Jump and gravity
            bool justLanded = ProcessJump();

            // Get the reference frame for the input
            if (!GetInputFrame(out var inputFrame))
                return; // no input frame, give up

            // Read the input from the user and put it in the input frame
            m_LastInput = inputFrame * new Vector3(MoveX.Value, 0, MoveZ.Value);
            if (m_LastInput.sqrMagnitude > 1)
                m_LastInput.Normalize();

            // Compute the new velocity and move the player, but only if not mid-jump
            if (!m_IsJumping)
            {
                m_IsSprinting = Sprint.Value > 0.5f;
                var desiredVelocity = m_LastInput * (m_IsSprinting ? SprintSpeed : Speed);
                var damping = justLanded ? 0 : Damping;
                if (Vector3.Angle(m_CurrentVelocityXZ, desiredVelocity) < 100)
                    m_CurrentVelocityXZ = Vector3.Slerp(
                        m_CurrentVelocityXZ, desiredVelocity, 
                        Damper.Damp(1, damping, Time.deltaTime));
                else
                    m_CurrentVelocityXZ += Damper.Damp(
                        desiredVelocity - m_CurrentVelocityXZ, damping, Time.deltaTime);
            }
            
            // Apply the position change
            ApplyMotion();

            // If not strafing, rotate the player to face movement direction
            if (!Strafe && m_CurrentVelocityXZ.sqrMagnitude > 0.001f)
            {
                var fwd = inputFrame * new Vector3(0, 0, 1);
                var qA = transform.rotation;
                var qB = Quaternion.LookRotation(
                    (InputForward == ForwardModes.Player && Vector3.Dot(fwd, m_CurrentVelocityXZ) < 0)
                        ? -m_CurrentVelocityXZ : m_CurrentVelocityXZ, UpDirection);
                var damping = justLanded ? 0 : Damping;
                transform.rotation = Quaternion.Slerp(qA, qB, Damper.Damp(1, damping, Time.deltaTime));
            }

            PostUpdate?.Invoke();
        }

        Vector3 UpDirection => UpMode == UpModes.World ? Vector3.up : transform.up;

        // Get the reference frame for the input
        bool GetInputFrame(out Quaternion inputFrame)
        {
            var up = UpDirection;
            var fwd = InputForward switch
            {
                ForwardModes.Camera => Camera.main.transform.forward,
                ForwardModes.Player => transform.forward,
                _ => Vector3.forward,
            };
            fwd = fwd.ProjectOntoPlane(up);
            if (fwd.sqrMagnitude < 0.001f)
            {
                inputFrame = Quaternion.identity;
                return false;
            }
            inputFrame = Quaternion.LookRotation(fwd, up);
            return true;
        }

        bool ProcessJump()
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
            if (IsGrounded() && m_CurrentVelocityY < 0)
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

        bool IsGrounded()
        {
            if (m_Controller != null)
                return m_Controller.isGrounded;

            // No controller - must compute manually with raycast
            return GetDistanceFromGround(transform.position, UpDirection, 10) < 0.01f;
        }

        void ApplyMotion()
        {
            if (m_Controller != null)
                m_Controller.Move((m_CurrentVelocityY * UpDirection + m_CurrentVelocityXZ) * Time.deltaTime);
            else
            {
                var pos = transform.position + m_CurrentVelocityXZ * Time.deltaTime;

                // Don't fall below ground
                var up = UpDirection;
                var altitude = GetDistanceFromGround(pos, up, 10);
                if (altitude < 0 && m_CurrentVelocityY <= 0)
                {
                    pos -= altitude * up;
                    m_CurrentVelocityY = 0;
                }
                else if (m_CurrentVelocityY < 0)
                {
                    var dy = -m_CurrentVelocityY * Time.deltaTime;
                    if (dy > altitude)
                    {
                        pos -= altitude * up;
                        m_CurrentVelocityY = 0;
                    }
                }
                transform.position = pos + m_CurrentVelocityY * up * Time.deltaTime;
            }
        }

        float GetDistanceFromGround(Vector3 pos, Vector3 up, float max)
        {
            float hExtraHeight = 2;
            if (Physics.Raycast(pos + up * hExtraHeight, -up, out var hit, max + hExtraHeight, LayerMask.GetMask("Default")))
                return hit.distance - hExtraHeight; 
            return max + 1;
        }
    }
}
