using System.Collections.Generic;
using Cinemachine.Utility;
using UnityEngine;

namespace Cinemachine.Examples
{
    [RequireComponent(typeof(InputAxisController))]
    public class PlayerMovement : MonoBehaviour, IInputAxisSource
    {
        public float MovementSpeed = 5;

        public InputAxis MoveX = new InputAxis { Range = new Vector2(-1, 1) };
        public InputAxis MoveZ = new InputAxis { Range = new Vector2(-1, 1) };
        public InputAxis LookX = new InputAxis { Range = new Vector2(-180, 180), Wrap = true };

        /// Report the available input axes to the input axis controller.
        /// We use the Input Axis Controller because it works with both the Input package
        /// and the Legacy input system.  This is sample code and we
        /// want it to work everywhere.
        void IInputAxisSource.GetInputAxes(List<IInputAxisSource.AxisDescriptor> axes)
        {
            axes.Add(new IInputAxisSource.AxisDescriptor { Axis = MoveX, Name = "Move X", AxisIndex = 0 });
            axes.Add(new IInputAxisSource.AxisDescriptor { Axis = MoveZ, Name = "Move Z", AxisIndex = 1 });
            axes.Add(new IInputAxisSource.AxisDescriptor { Axis = LookX, Name = "Look X", AxisIndex = 0 });
        }

        void Update()
        {
            var moveDir = new Vector3(MoveX.Value, 0, MoveZ.Value);
            if (!moveDir.AlmostZero())
            {
                moveDir = moveDir.normalized;
                transform.position += transform.TransformDirection(moveDir) * Time.deltaTime * MovementSpeed;
            }
            var rot = transform.rotation.eulerAngles;
            rot.y = LookX.Value;
            transform.rotation = Quaternion.Euler(rot);
        }
    }
}