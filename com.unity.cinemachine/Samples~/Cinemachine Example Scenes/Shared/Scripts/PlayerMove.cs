using System.Collections.Generic;
using Cinemachine.Utility;
using UnityEngine;

namespace Cinemachine.Examples
{
    /// <summary>Very simple motion controller, no physics</summary>
    [RequireComponent(typeof(InputAxisController))]
    public class PlayerMove : MonoBehaviour, IInputAxisSource
    {
        public float Speed = 10;
        public float VelocityDamping = 0.5f;
        public float JumpStrength = 5;

        public enum ForwardMode
        {
            Camera,
            Player,
            World
        };
        public ForwardMode InputForward = ForwardMode.Camera;
        public bool RotatePlayer = false;

        [Header("Input Axes")]
        [Tooltip("X Axis movement.  Value is -1..1.  Controls the sideways movement")]
        public InputAxis MoveX = new InputAxis { Range = new Vector2(-1, 1) };

        [Tooltip("Z Axis movement.  Value is -1..1. Controls the forward movement")]
        public InputAxis MoveZ = new InputAxis { Range = new Vector2(-1, 1) };

        [Tooltip("Jump movement.  Value is 0..1. Controls the vertical movement")]
        public InputAxis Jump = new InputAxis { Range = new Vector2(0, 1) };

        /// Report the available input axes to the input axis controller.
        /// We use the Input Axis Controller because it works with both the Input package
        /// and the Legacy input system.  This is sample code and we
        /// want it to work everywhere.
        void IInputAxisSource.GetInputAxes(List<IInputAxisSource.AxisDescriptor> axes)
        {
            axes.Add(new IInputAxisSource.AxisDescriptor { Axis = MoveX, Name = "Move X", AxisIndex = 0 });
            axes.Add(new IInputAxisSource.AxisDescriptor { Axis = MoveZ, Name = "Move Z", AxisIndex = 1 });
            axes.Add(new IInputAxisSource.AxisDescriptor { Axis = Jump, Name = "Jump", AxisIndex = 2 });
        }

        Vector3 m_currentVleocity;
        float m_currentJumpSpeed;
        float m_restY;

        private void OnEnable()
        {
            m_currentJumpSpeed = 0;
            m_restY = transform.position.y;
        }

        void Update()
        {
            // Get the reference frame for the input
            var fwd = InputForward switch
            {
                ForwardMode.Camera => Camera.main.transform.forward,
                ForwardMode.Player => transform.forward,
                _ => Vector3.forward,
            };
            fwd.y = 0; // confine to xz plane
            fwd = fwd.normalized;
            if (fwd.sqrMagnitude < 0.01f)
                return;
            var inputFrame = Quaternion.LookRotation(fwd, Vector3.up);

            // Read the input from the user and put it in the input frame
            var input = inputFrame * new Vector3(MoveX.Value, 0, MoveZ.Value).normalized;

            // Compute the new velocity and apply it
            var dt = Time.deltaTime;
            var desiredVelocity = input * Speed;
            var deltaVel = desiredVelocity - m_currentVleocity;
            m_currentVleocity += Damper.Damp(deltaVel, VelocityDamping, dt);
            transform.position += m_currentVleocity * dt;

            // Rotate the player to face movement direction
            if (RotatePlayer && m_currentVleocity.sqrMagnitude > 0.01f)
            {
                var qA = transform.rotation;
                var qB = Quaternion.LookRotation(
                    (InputForward == ForwardMode.Player && Vector3.Dot(fwd, m_currentVleocity) < 0)
                        ? -m_currentVleocity
                        : m_currentVleocity);
                transform.rotation = Quaternion.Slerp(qA, qB, Damper.Damp(1, VelocityDamping, dt));
            }

            // Process jump
            const float Gravity = -10;
            if (Jump.Value > 0)
                m_currentJumpSpeed = JumpStrength;
            if (m_currentJumpSpeed != 0)
                m_currentJumpSpeed += Gravity * dt;

            var p = transform.position;
            p.y += m_currentJumpSpeed * dt;
            if (p.y < m_restY)
            {
                p.y = m_restY;
                m_currentJumpSpeed = 0;
            }
            transform.position = p;
        }
    }
}