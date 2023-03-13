using System.Collections.Generic;
using UnityEngine;

namespace Unity.Cinemachine.Samples
{
    /// <summary>
    /// Add-on for SimplePlayerController that controls the player's Aiming Core.
    /// This component expects to be in a child object of the player, to decouple player aiming from player rotation.
    /// This only works in worlds where CharacterController is valid, ie when up is world up.
    /// </summary>
    public class SimplePlayerAimController : MonoBehaviour, IInputAxisOwner
    {
        public bool LockPlayerToCamera;
        public float RotationDamping = 0.2f;

        [Tooltip("Horizontal Rotation.")]
        public InputAxis HorizontalLook = new () { Range = new Vector2(-180, 180), Wrap = true, Recentering = InputAxis.RecenteringSettings.Default };

        [Tooltip("Vertical Rotation.")]
        public InputAxis VerticalLook = new () { Range = new Vector2(-70, 70), Recentering = InputAxis.RecenteringSettings.Default };


        SimplePlayerController m_Controller;

        /// Report the available input axes to the input axis controller.
        /// We use the Input Axis Controller because it works with both the Input package
        /// and the Legacy input system.  This is sample code and we
        /// want it to work everywhere.
        void IInputAxisOwner.GetInputAxes(List<IInputAxisOwner.AxisDescriptor> axes)
        {
            axes.Add(new () { DrivenAxis = () => ref HorizontalLook, Name = "Horizontal Look", Hint = IInputAxisOwner.AxisDescriptor.Hints.X });
            axes.Add(new () { DrivenAxis = () => ref VerticalLook, Name = "Vertical Look", Hint = IInputAxisOwner.AxisDescriptor.Hints.Y });
        }

        void OnValidate()
        {
            HorizontalLook.Validate();
            VerticalLook.Range.x = Mathf.Clamp(VerticalLook.Range.x, -90, 90);
            VerticalLook.Range.y = Mathf.Clamp(VerticalLook.Range.y, -90, 90);
            VerticalLook.Validate();
        }
        
        void Start()
        {
            m_Controller = GetComponentInParent<SimplePlayerController>();
            if (m_Controller == null)
                Debug.LogError("SimplePlayerController not found on parent object");
            else
            {
                m_Controller.Strafe = true;
                m_Controller.PreUpdate += UpdateRotation;
            }
        }

        public void RecenterPlayer(float damping = 0)
        {
            var rot = transform.rotation.eulerAngles;
            var parentRot = m_Controller.transform.rotation.eulerAngles;
            var delta = rot.y - parentRot.y;
            if (delta > 180)
                delta -= 360;
            if (delta < -180)
                delta += 360;
            delta = Damper.Damp(delta, damping, Time.deltaTime);
            parentRot.y += delta;
            m_Controller.transform.rotation = Quaternion.Euler(parentRot);

            HorizontalLook.Value -= delta;
            transform.rotation = Quaternion.Euler(rot);
        }

        void UpdateRotation()
        {
            transform.localRotation = Quaternion.Euler(VerticalLook.Value, HorizontalLook.Value, 0);
            if (LockPlayerToCamera)
            {
                var yaw = transform.rotation.eulerAngles.y;
                var parentRot = m_Controller.transform.rotation.eulerAngles;
                HorizontalLook.Value = 0;
                m_Controller.transform.rotation = Quaternion.Euler(new Vector3(parentRot.x, yaw, parentRot.z));
            }
            else
            {
                // If the player is moving, rotate its yaw to match the camera direction,
                // otherwise let the camera orbit
                if (m_Controller.IsMoving)
                    RecenterPlayer(RotationDamping);
            }
            var gotInput = VerticalLook.TrackValueChange() | HorizontalLook.TrackValueChange();
            VerticalLook.DoRecentering(Time.deltaTime, gotInput);
            HorizontalLook.DoRecentering(Time.deltaTime, gotInput);
        }
    }
}