using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Cinemachine.Samples
{
    /// <summary>
    /// This interface should be implemented by a character controller in order to be compatible 
    /// with the SimplePlayerAimController.
    /// 
    /// The aim controller will want to read and change the player's rotation and strafe mode.
    /// </summary>
    public interface ISimplePlayerAimable
    {
        /// <summary>
        /// Called from Update(), this will read the player's rotation, and write it in the event
        /// that the player needs to rotate towards the aiming direction.
        /// </summary>
        public Quaternion PlayerRotation { get; set; }

        /// <summary>
        /// Read the player's current Up direction.
        /// </summary>
        public Vector3 PlayerUp { get; }

        /// <summary>
        /// True if the player is moving, false if standing still.
        /// </summary>
        public bool IsMoving { get; }

        /// <summary>
        /// If true, player can move in non-forward directions.  
        /// Otherwise, player turns to face its direction of motion.
        /// </summary>
        public bool StrafeMode { get; set; }

        /// <summary>
        /// This action must be invoked prior to the player controller updating its motion.
        /// The aim controller may wat to change the strafe mode.
        /// </summary>
        public ref Action PreUpdateAction { get; }
    }
    
    /// <summary>
    /// This component expects to be on a child object of a player that has a controller behaviour 
    /// implementing the ISimplePlayerAimable interface.  It works intimately with that behaviour.
    //
    /// The purpose of the aiming core is to decouple the camera rotation from the player rotation.
    /// Camera rotation is determined by the rotation of the player core GameObject, and this behaviour
    /// provides input axes for controlling it.  When the player core is used as the target for
    /// a CinemachineCamera with a ThirdPersonFollow component, the camera will look along the core's
    /// forward axis, and pivot around the core's origin.
    ///
    /// The aiming core is also used to define the origin and direction of player shooting, if player
    /// has that ability.
    ///
    /// To implement player shooting, add a SimplePlayerShoot behaviour (or similar) to this GameObject.
    /// </summary>
    public class SimplePlayerAimController : MonoBehaviour, Unity.Cinemachine.IInputAxisOwner
    {
        public enum CouplingMode { Coupled, CoupledWhenMoving, Decoupled }

        [Tooltip("How the player's rotation is coupled to the camera's rotation.  Three modes are available:\n"
            + "<b>Coupled</b>: The player rotates with the camera.  Sideways movement will result in strafing.\n"
            + "<b>Coupled When Moving</b>: Camera can rotate freely around the player when the player is stationary, "
                + "but the player will rotate to face camera forward when it starts moving.\n"
            + "<b>Decoupled</b>: The player's rotation is independent of the camera's rotation.")]
        public CouplingMode PlayerRotation;

        [Tooltip("How fast the player rotates to face the camera direction when the player starts moving.  "
            + "Only used when Player Rotation is Coupled When Moving.")]
        public float RotationDamping = 0.2f;

        ISimplePlayerAimable m_Controller;
        Quaternion m_DesiredWorldRotation;
        Transform m_Transform; // cached for efficiency

        /// User input:
        /// 
        /// We use InputAxis to hold aim state because it can be driven by both the Input package
        /// and the Legacy input system, so that the sample code will always work.
        /// Also it implements recentering, which is nice.
        /// 
        [Tooltip("Horizontal Rotation.  Value is in degrees, with 0 being centered.")]
        public InputAxis HorizontalLook = new () { Range = new Vector2(-180, 180), Wrap = true, Recentering = InputAxis.RecenteringSettings.Default };

        [Tooltip("Vertical Rotation.  Value is in degrees, with 0 being centered.")]
        public InputAxis VerticalLook = new () { Range = new Vector2(-70, 70), Recentering = InputAxis.RecenteringSettings.Default };

        /// Report the available input axes so that the InpusAxisController component 
        /// will discover the axes and drive them with user input.
        /// You may remove this implementation and drive the InputAxis values in an alternative way.
        void IInputAxisOwner.GetInputAxes(List<IInputAxisOwner.AxisDescriptor> axes)
        {
            axes.Add(new () { DrivenAxis = () => ref HorizontalLook, Name = "Horizontal Look", Hint = IInputAxisOwner.AxisDescriptor.Hints.X });
            axes.Add(new () { DrivenAxis = () => ref VerticalLook, Name = "Vertical Look", Hint = IInputAxisOwner.AxisDescriptor.Hints.Y });
        }

        Vector2 AxisValues
        { 
            get => new Vector2(VerticalLook.Value, HorizontalLook.Value); 
            set 
            {
                VerticalLook.Value = value.x;
                HorizontalLook.Value = value.y;
            }
        }

        void OnValidate()
        {
            HorizontalLook.Validate();
            VerticalLook.Range.x = Mathf.Clamp(VerticalLook.Range.x, -90, 90);
            VerticalLook.Range.y = Mathf.Clamp(VerticalLook.Range.y, -90, 90);
            VerticalLook.Validate();
        }

        void OnEnable()
        {
            m_Controller = GetComponentInParent<ISimplePlayerAimable>();
            m_Transform = transform;
            if (m_Controller != null)
                m_Controller.PreUpdateAction += OnPreUpdate;
            else
                Debug.LogError("ISimplePlayerAimable not found on parent object");
        }

        void OnDisable()
        {
            if (m_Controller != null)
                m_Controller.PreUpdateAction -= OnPreUpdate;
        }

        /// <summary>Rotates the player to match the aim direction.</summary>
        /// <param name="damping">How long the recentering should take</param>
        public void RecenterPlayer(float damping = 0)
        {
            if (m_Controller == null)
                return;

            // Get my rotation relative to parent
            var rot = m_Transform.localRotation.eulerAngles;
            rot.y = NormalizeAngle(rot.y);
            var delta = rot.y;
            delta = Damper.Damp(delta, damping, Time.deltaTime);

            // Rotate the parent towards me
            m_Controller.PlayerRotation = Quaternion.AngleAxis(
                delta, m_Controller.PlayerUp) * m_Controller.PlayerRotation;

            // Rotate me in the opposite direction, by setting both my rotation and axis value.
            // This will keep the aim direction constant.
            var axisValues = AxisValues;
            axisValues.y -= delta;
            AxisValues = axisValues;
            rot.y -= delta;
            m_Transform.localRotation = Quaternion.Euler(rot);
        }

        /// <summary>
        /// This is a helper fucntion provided for any scripts that might want to programatically
        /// set the aim direction without changing player's rotation.
        /// Here we only set the axis values, we let the player controller do the actual rotation.
        /// </summary>
        /// <param name="worldspaceDirection">Direction to look in, in worldspace</param>
        public void SetLookDirection(Vector3 worldspaceDirection)
        {
            if (m_Controller == null)
                return;
            var rot = (Quaternion.Inverse(m_Controller.PlayerRotation)
                * Quaternion.LookRotation(worldspaceDirection, m_Controller.PlayerUp)).eulerAngles;
            AxisValues = new Vector2(VerticalLook.ClampValue(NormalizeAngle(rot.x)), HorizontalLook.ClampValue(rot.y));
        }

        // This is called by the player controller before it updates its own rotation.
        // We use this opportunity to set the strafe mode and pre-rotate the player if necessary.
        void OnPreUpdate()
        {
            // Set the aim direction from the current user input
            m_Transform.localRotation = Quaternion.Euler(AxisValues);

            // Couple it to the player's rotation if desired
            m_DesiredWorldRotation = m_Transform.rotation;
            switch (PlayerRotation)
            {
                case CouplingMode.Coupled:
                {
                    // Lock the player yaw to the aim rotation
                    m_Controller.StrafeMode = true;
                    RecenterPlayer(); 
                    break;
                }
                case CouplingMode.CoupledWhenMoving:
                {
                    // If the player is moving, rotate its yaw to match the aim rotation,
                    // otherwise let the camera orbit
                    m_Controller.StrafeMode = true;
                    if (m_Controller.IsMoving)
                        RecenterPlayer(RotationDamping);
                    break;
                }
                case CouplingMode.Decoupled:
                {
                    // Let the camera orbit
                    m_Controller.StrafeMode = false;
                    break;
                }
            }
        }

        // Update our rotation after player controller has updated its own.
        void LateUpdate()
        {
            VerticalLook.UpdateRecentering(Time.deltaTime, VerticalLook.TrackValueChange());
            HorizontalLook.UpdateRecentering(Time.deltaTime, HorizontalLook.TrackValueChange());

            if (m_Controller != null && PlayerRotation == CouplingMode.Decoupled)
            {
                // After player has been rotated, we subtract any rotation change
                // from our own transform, to maintain our world rotation
                m_Transform.rotation = m_DesiredWorldRotation;
                var delta = (Quaternion.Inverse(m_Controller.PlayerRotation) * m_DesiredWorldRotation).eulerAngles;
                AxisValues = new Vector2(NormalizeAngle(delta.x), NormalizeAngle(delta.y));
            }
        }

        static float NormalizeAngle(float angle)
        {
            while (angle > 180)
                angle -= 360;
            while (angle < -180)
                angle += 360;
            return angle;
        }
    }
}
