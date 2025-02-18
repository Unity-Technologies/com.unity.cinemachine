using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Events;

namespace Unity.Cinemachine.Samples
{
    /// <summary>
    /// This is a basic 3D character controller using a Rigidbody.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class SimplePlayerControllerRigidbody : MonoBehaviour, IInputAxisOwner, ISimplePlayerAimable, ISimplePlayerAnimatable
    {
        [Tooltip("Ground speed when walking")]
        public float Speed = 1f;
        [Tooltip("Ground speed when sprinting")]
        public float SprintSpeed = 4;
        [Tooltip("Initial vertical speed when jumping")]
        public float JumpSpeed = 4;
        [Tooltip("Initial vertical speed when sprint-jumping")]
        public float SprintJumpSpeed = 6;
        
        [Tooltip("Transition duration (in seconds) when the player changes velocity or rotation.")]
        public float Damping = 0.5f;

        [Tooltip("Makes the player strafe when moving sideways, otherwise it turns to face the direction of motion.")]
        public bool Strafe = false;

        public enum ForwardModes { Camera, Player, World };

        [Tooltip("Reference frame for the input controls:\n"
            + "<b>Camera</b>: Input forward is camera forward direction.\n"
            + "<b>Player</b>: Input forward is Player's forward direction.\n"
            + "<b>World</b>: Input forward is World forward direction.")]
        public ForwardModes InputForward = ForwardModes.Camera;

        [Tooltip("If non-null, take the input frame from this camera instead of Camera.main. Useful for split-screen games.")]
        public Camera CameraOverride;

        [Tooltip("Layers to include in ground detection via Raycasts.")]
        public LayerMask GroundLayers = 1;

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

//        const float kDelayBeforeInferringJump = 0.3f;
//        float m_TimeLastGrounded = 0;

//        Vector3 m_CurrentVelocityXZ;
        Vector3 m_LastInput;
//        float m_CurrentVelocityY;
        bool m_IsSprinting;
        bool m_IsJumping;

        Rigidbody m_Rb;

//        public bool IsSprinting => m_IsSprinting;
        public Camera Camera => CameraOverride == null ? Camera.main : CameraOverride;
//        public bool IsGrounded() => GetDistanceFromGround(m_Transform.position, UpDirection, 10) < 0.01f;

        // ISimplePlayerAimable implementation
        public Quaternion PlayerRotation { get => m_Rb.rotation; set => m_Rb.rotation = value; }
        public Vector3 PlayerUp => m_Rb.rotation * Vector3.up;
        public bool IsMoving => m_LastInput.sqrMagnitude > 0.01f;
        public bool StrafeMode { get => Strafe; set => Strafe = value; }
        public ref Action PreUpdateAction => ref PreUpdate;

        // ISimplePlayerAnimatable implementation
        public bool IsJumping => m_IsJumping;
        public float JumpScale => m_IsSprinting ? JumpSpeed / SprintJumpSpeed : 1;
        public Vector3 LocalSpaceVelocity => m_Rb.rotation * Velocity;

        Vector3 Velocity
        {
            get 
            {
#if UNITY_6000_1_OR_NEWER
                return m_Rb.linearVelocity;
#else
                #pragma warning disable CS0618 // obsolete for 6000.0.0f11 and newer
                return m_Rb.velocity;
                #pragma warning restore CS0618
#endif
            }
            set
            {
#if UNITY_6000_1_OR_NEWER
                m_Rb.linearVelocity = value;
#else
                #pragma warning disable CS0618 // obsolete for 6000.0.0f11 and newer
                m_Rb.velocity = value;
                #pragma warning restore CS0618
#endif
            }
        }

        void Start() 
        {
            m_Rb = GetComponent<Rigidbody>();
        }

        private void OnEnable()
        {
//            m_CurrentVelocityY = 0;
            m_IsSprinting = false;
            m_IsJumping = false;
//            m_TimeLastGrounded = Time.time;
        }

        void FixedUpdate()
        {
            PreUpdate?.Invoke();

            // Process Jump and gravity
            bool justLanded = ProcessJump();

            // Get the reference frame for the input
            var rawInput = new Vector3(MoveX.Value, 0, MoveZ.Value);
            var inputFrame = GetInputFrame();

            // Read the input from the user and put it in the input frame
            m_LastInput = inputFrame * rawInput;
            if (m_LastInput.sqrMagnitude > 1)
                m_LastInput.Normalize();

            var vel = Velocity;
            float velY = vel.y; vel.y = 0;

            // Compute the new velocity and move the player
            if (MotionControlWhileInAir || !m_IsJumping)
            {
                m_IsSprinting = Sprint.Value > 0.5f;
                var desiredVelocity = m_LastInput * (m_IsSprinting ? SprintSpeed : Speed);
                var damping = justLanded ? 0 : Damping;
                if (Vector3.Angle(vel, desiredVelocity) < 100)
                    vel = Vector3.Slerp(vel, desiredVelocity, Damper.Damp(1, damping, Time.deltaTime));
                else
                    vel += Damper.Damp(desiredVelocity - vel, damping, Time.deltaTime);
            }

            // Apply the position change
            Velocity = new Vector3(vel.x, velY, vel.z);

            // If not strafing, rotate the player to face movement direction
            if (!Strafe && vel.sqrMagnitude > 0.001f)
            {
                var fwd = inputFrame * Vector3.forward;
                var qA = m_Rb.rotation;
                var qB = Quaternion.LookRotation(
                    (InputForward == ForwardModes.Player && Vector3.Dot(fwd, vel) < 0) ? -vel : vel, Vector3.up);
                var damping = justLanded ? 0 : Damping;
                m_Rb.rotation = Quaternion.Slerp(qA, qB, Damper.Damp(1, damping, Time.deltaTime));
            }
        }

        Quaternion GetInputFrame()
        {
            // Get the raw input frame, depending of forward mode setting
            return InputForward switch
            {
                ForwardModes.Camera => Camera.transform.rotation,
                ForwardModes.Player => m_Rb.rotation,
                _ => Quaternion.identity,
            };
        }

        bool ProcessJump()
        {
            bool justLanded = false;
#if false
            var now = Time.time;
            bool grounded = IsGrounded();

            m_CurrentVelocityY -= Gravity * Time.deltaTime;

            if (!m_IsJumping)
            {
                // Process jump command
                if (grounded && Jump.Value > 0.01f)
                {
                    m_IsJumping = true;
                    m_CurrentVelocityY = m_IsSprinting ? SprintJumpSpeed : JumpSpeed;
                }
                // If we are falling, assume the jump pose
                if (!grounded && now - m_TimeLastGrounded > kDelayBeforeInferringJump)
                    m_IsJumping = true;

                if (m_IsJumping)
                    grounded = false;
            }

            if (grounded)
            {
                m_TimeLastGrounded = Time.time;
                m_CurrentVelocityY = 0;

                // If we were jumping, complete the jump
                if (m_IsJumping)
                {
                    m_IsJumping = false;
                    justLanded = true;
                    Landed.Invoke();
                }
            }
#endif
            return justLanded;
        }

         float GetDistanceFromGround(Vector3 pos, Vector3 up, float max)
        {
            float kExtraHeight = 0;
            if (Physics.Raycast(pos + up * kExtraHeight, -up, out var hit,
                    max + kExtraHeight, GroundLayers, QueryTriggerInteraction.Ignore))
                return hit.distance - kExtraHeight;
            return max + 1;
        }
    }
}
