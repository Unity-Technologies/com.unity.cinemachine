using System.Collections.Generic;
using UnityEngine;

namespace Cinemachine.Examples
{
    public class CarController : MonoBehaviour, IInputAxisSource
    {
        public float MotorStrength;
        public float BrakeStrength;
        public float MaxTurnAngle;

        public WheelCollider FrontLeftWheelCollider;
        public WheelCollider FrontRightWheelCollider;
        public WheelCollider RearLeftWheelCollider;
        public WheelCollider RearRightWheelCollider;

        public Transform FrontLeftWheel;
        public Transform FrontRightWhee;
        public Transform RearLeftWheel;
        public Transform RearRightWheel;

        [Header("Input Axes")]
        [Tooltip("X Axis movement.  Value is -1..1.  Controls the sideways movement")]
        public InputAxis MoveX = new InputAxis { Range = new Vector2(-1, 1) };

        [Tooltip("Z Axis movement.  Value is -1..1. Controls the forward movement")]
        public InputAxis MoveZ = new InputAxis { Range = new Vector2(-1, 1) };

        [Tooltip("Jump movement.  Value is 0 or 1. Controls the braking movement")]
        public InputAxis Brake = new InputAxis { Range = new Vector2(0, 1) };


        /// Report the available input axes to the input axis controller.
        /// We use the Input Axis Controller because it works with both the Input package
        /// and the Legacy input system.  This is sample code and we
        /// want it to work everywhere.
        void IInputAxisSource.GetInputAxes(List<IInputAxisSource.AxisDescriptor> axes)
        {
            axes.Add(new IInputAxisSource.AxisDescriptor { Axis = MoveX, Name = "Move X", AxisIndex = 0 });
            axes.Add(new IInputAxisSource.AxisDescriptor { Axis = MoveZ, Name = "Move Z", AxisIndex = 1 });
            axes.Add(new IInputAxisSource.AxisDescriptor { Axis = Brake, Name = "Brake" });
        }

        void Update()
        {
            // Acceleration
            var force = MotorStrength * MoveZ.Value;
            FrontLeftWheelCollider.motorTorque = force;
            FrontRightWheelCollider.motorTorque = force;

            // Braking
            force = BrakeStrength * Brake.Value;
            FrontRightWheelCollider.brakeTorque = force;
            FrontLeftWheelCollider.brakeTorque = force;
            RearLeftWheelCollider.brakeTorque = force;
            RearRightWheelCollider.brakeTorque = force;
            if (Brake.Value > 0.99f)
                MoveZ.Value = 0;

            // Steering
            force = MaxTurnAngle * MoveX.Value;
            FrontLeftWheelCollider.steerAngle = force;
            FrontRightWheelCollider.steerAngle = force;

            // Place the wheels
            UpdateWheel(FrontLeftWheelCollider, FrontLeftWheel);
            UpdateWheel(FrontRightWheelCollider, FrontRightWhee);
            UpdateWheel(RearRightWheelCollider, RearRightWheel);
            UpdateWheel(RearLeftWheelCollider, RearLeftWheel);
        }

        void UpdateWheel(WheelCollider c, Transform t)
        {
            c.GetWorldPose(out var pos, out var rot);
            t.SetPositionAndRotation(pos, rot);
        }
    }
}