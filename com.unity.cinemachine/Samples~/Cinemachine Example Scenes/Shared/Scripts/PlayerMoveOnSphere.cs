using System.Collections.Generic;
using Cinemachine.Utility;
using UnityEngine;

namespace Cinemachine.Examples
{
    [RequireComponent(typeof(InputAxisController))]
    public class PlayerMoveOnSphere : MonoBehaviour, IInputAxisSource
    {
        public SphereCollider Sphere;

        public float Speed = 10;
        public bool RotatePlayer = true;
        public float RotationDamping = 0.5f;

        public InputAxis MoveX = new InputAxis { Range = new Vector2(-1, 1) };
        public InputAxis MoveZ = new InputAxis { Range = new Vector2(-1, 1) };

        /// Report the available input axes to the input axis controller.
        /// We use the Input Axis Controller because it works with both the Input package
        /// and the Legacy input system.  This is sample code and we
        /// want it to work everywhere.
        void IInputAxisSource.GetInputAxes(List<IInputAxisSource.AxisDescriptor> axes)
        {
            axes.Add(new IInputAxisSource.AxisDescriptor { Axis = MoveX, Name = "Move X", AxisIndex = 0 });
            axes.Add(new IInputAxisSource.AxisDescriptor { Axis = MoveZ, Name = "Move Z", AxisIndex = 1 });
        }

        void Update()
        {
            var moveDir = new Vector3(MoveX.Value, 0, MoveZ.Value);
            if (!moveDir.AlmostZero())
            {
                moveDir = new Vector3(MoveX.Value, 0, MoveZ.Value).normalized;
                moveDir = Camera.main.transform.rotation * moveDir;
                transform.position += moveDir * Time.deltaTime * Speed;

                if (RotatePlayer)
                {
                    float t = Damper.Damp(1, RotationDamping, Time.deltaTime);
                    Quaternion newRotation = Quaternion.LookRotation(moveDir, transform.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, newRotation, t);
                }
            }
            // Stick to sphere surface
            if (Sphere != null)
            {
                var up = transform.position - Sphere.transform.position;
                up = up.normalized;
                var fwd = transform.forward.ProjectOntoPlane(up);
                transform.position = Sphere.transform.position + up * (Sphere.radius + transform.localScale.y / 2);
                transform.rotation = Quaternion.LookRotation(fwd, up);
            }
        }
    }
}
