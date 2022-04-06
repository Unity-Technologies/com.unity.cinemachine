using UnityEngine;
using System;
using Cinemachine.Utility;
using System.Collections.Generic;

namespace Cinemachine
{
    /// <summary>
    /// Components that hold InputAxisValue structs
    /// </summary>
    public interface IInputAxisTarget
    {
        /// <summary>
        /// Desscribes an axis for an axis driver
        /// </summary>
        public struct AxisDescriptor
        {
            /// <summary>The axis to drive</summary>
            public InputAxis Axis;
            /// <summary>The name to display for the axis</summary>
            public string Name;
            /// <summary>Indicates what axis is being driven: 0=x, 1=y, 2=z.  
            /// Used only for setting up default values.</summary>
            public int AxisIndex;
        }

        /// <summary>
        /// Report the input axis to be driven, and their names
        /// </summary>
        /// <param name="axes">Axes to drive</param>
        public void GetInputAxes(List<AxisDescriptor> axes);

        /// <summary>
        /// Delegate to be called when input needs to be reset and recentering cancelled.
        /// </summary>
        public delegate void ResetHandler();

        /// <summary>
        /// Register a handler that will be called when input needs to be reset
        /// </summary>
        /// <param name="handler">Then hanlder to register</param>
        public void RegisterResetHandler(ResetHandler handler);

        /// <summary>
        /// Unregister a handler that will be called when input needs to be reset
        /// </summary>
        /// <param name="handler">Then hanlder to unregister</param>
        public void UnregisterResetHandler(ResetHandler handler);
    }

    /// <summary>
    /// Defines an input axis.  This is a field that can take on any value in a range, 
    /// with optional wrapping to form a loop.
    /// </summary>
    [Serializable]
    public class InputAxis
    {
        /// <summary>The current value of the axis.  You can drive this directly from a script</summary>
        [Tooltip("The current value of the axis.  You can drive this directly from a script.")]
        public float Value;

        /// <summary>The current value of the axis.  You can drive this directly from a script</summary>
        [Tooltip("The centered, or at-rest value of this axis.")]
        public float Center;

        /// <summary>The valid range for the axis value.  Value will be clapmed to this range.</summary>
        [Tooltip("The valid range for the axis value.  Value will be clapmed to this range.")]
        [Vector2AsRangeProperty]
        public Vector2 Range;

        /// <summary>If set, then the axis will wrap around at the min/max values, forming a loop</summary>
        [Tooltip("If set, then the axis will wrap around at the min/max values, forming a loop")]
        public bool Wrap;

        /// <summary>Flags controlling inspector display.  Used only in the editor.</summary>
        public enum Flags 
        { 
            /// <summary>No flags</summary>
            None = 0, 
            /// <summary>Range and center are not editable by the user</summary>
            RangeIsDriven = 1, 
            /// <summary>Recentering is not available</summary>
            HideRecentering = 2 
        };

        /// <summary>Flags controlling inspector display.  Used only in the editor.</summary>
        [HideInInspector]
        public Flags InspectorFlags;

        /// <summary>Defines the settings for automatic recentering</summary>
        [Serializable] 
        public struct RecenteringSettings
        {
            /// <summary>If set, will enable automatic recentering of the axis</summary>
            [Tooltip("If set, will enable automatic recentering of the axis")]
            public bool Enabled;

            /// <summary>If no user input has been detected on the axis for this man
            /// seconds, recentering will begin.</summary>
            [Tooltip("If no user input has been detected on the axis for this many "
                + "seconds, recentering will begin.")]
            public float Wait;

            /// <summary>How long it takes to reach center once recentering has started</summary>
            [Tooltip("How long it takes to reach center once recentering has started.")]
            public float Time;

            /// <summary>Default value</summary>
            public static RecenteringSettings Default => new RecenteringSettings { Wait = 1, Time = 2 };
        }

        /// <summary>Controls automatic recentering of axis value.</summary>
        [FoldoutWithEnabledButton]
        public RecenteringSettings Recentering;

        /// <summary>Clamp the value to range, taking wrap into account</summary>
        public float ClampValue(float v)
        {
            float r = Range.y - Range.x;
            var v1 = (v - Range.x) % r;
            v1 += v1 < 0 ? r : 0;
            v1 += Range.x;
            v1 = (Wrap && r > UnityVectorExtensions.Epsilon) ? v1 : v;
            return Mathf.Clamp(v1, Range.x, Range.y);
        }

        /// <summary>Clamp and scale the value to range 0...1, taking wrap into account</summary>
        public float GetNormalizedValue()
        {
            float v = ClampValue(Value);
            float r = Range.y - Range.x;
            return (v - Range.x) / (r > UnityVectorExtensions.Epsilon ? r : 1);
        }

        /// <summary>Return clamped value</summary>
        public float GetClampedValue()
        {
            return ClampValue(Value);
        }

        /// <summary>Make sure the settings are well-formed</summary>
        public void Validate()
        {
            Range.y = Mathf.Max(Range.x, Range.y);
            Center = ClampValue(Center);
            Value = ClampValue(Value);
            Recentering.Wait = Mathf.Max(0, Recentering.Wait);
            Recentering.Time = Mathf.Max(0, Recentering.Time); 
        }
    }

    /// <summary>Settings for controlling how input value is processed</summary>
    [Serializable]
    public struct InputAxisControl
    {
        /// <summary>The value of the user input for this frame.</summary>
        [Tooltip("The value of the user input for this frame")]
        public float InputValue;

        /// <summary>The amount of time in seconds it takes to accelerate to
        /// MaxSpeed with the supplied Axis at its maximum value</summary>
        [Tooltip("The amount of time in seconds it takes to accelerate to MaxSpeed with the "
            + "supplied Axis at its maximum value")]
        public float AccelTime;

        /// <summary>The amount of time in seconds it takes to decelerate
        /// the axis to zero if the supplied axis is in a neutral position</summary>
        [Tooltip("The amount of time in seconds it takes to decelerate the axis to zero if "
            + "the supplied axis is in a neutral position")]
        public float DecelTime;

        /// <summary>Call from OnValidate: Make sure the fields are sensible</summary>
        public void Validate()
        {
            AccelTime = Mathf.Max(0, AccelTime);
            DecelTime = Mathf.Max(0, DecelTime);
        }
    }

    /// <summary>
    /// This object drives an input axis.  
    /// It reads raw input, applies it to the axis value, with acceleration and deceleration, 
    /// and manages recentering.
    /// </summary>
    [Serializable]
    public struct InputAxisDriver
    {
        /// Internal state
        float m_CurrentSpeed;
        const float Epsilon = UnityVectorExtensions.Epsilon;
        float m_LastUpdateTime;
        float m_RecenteringVelocity;

        /// <summary>Update the axis</summary>
        /// <param name="deltaTime">current deltaTime</param>
        /// <param name="axis">The InputAxisValue to update</param>
        /// <param name="control">Parameter for controlling the behaviour of the axis</param>
        /// <returns>True if the axis value changed due to user input, false otherwise</returns>
        public bool UpdateInput(
            float deltaTime, InputAxis axis, 
            ref InputAxisControl control)
        {
            float input = control.InputValue;
            if (deltaTime > Epsilon)
            {
                var speed = input / deltaTime;
                var dampTime = Mathf.Abs(speed) < Mathf.Abs(m_CurrentSpeed) ? control.DecelTime : control.AccelTime;
                speed = m_CurrentSpeed + Damper.Damp(speed - m_CurrentSpeed, dampTime, deltaTime);
                m_CurrentSpeed = speed;

                // Decelerate to the end points of the range if not wrapping
                float range = axis.Range.y - axis.Range.x;
                if (!axis.Wrap && control.DecelTime > Epsilon && range > Epsilon)
                {
                    var v0 = axis.ClampValue(axis.Value);
                    var v = axis.ClampValue(v0 + speed * deltaTime);
                    var d = (speed > 0) ? axis.Range.y - v : v - axis.Range.x;
                    if (d < (0.1f * range) && Mathf.Abs(speed) > Epsilon)
                        speed = Damper.Damp(v - v0, control.DecelTime, deltaTime) / deltaTime;
                }
                input = speed * deltaTime;
            }
            axis.Value = axis.ClampValue(axis.Value + input);
            bool gotInput = Mathf.Abs(control.InputValue) > Epsilon;
            if (gotInput)
                CancelRecentering();
            return gotInput;
        }

        /// <summary>Call this to manage recentering axis valkue to axis center.</summary>
        /// <param name="deltaTime"></param>
        /// <param name="axis"></param>
        /// <param name="recentering"></param>
        public void DoRecentering(float deltaTime, InputAxis axis)
        {
            if (!axis.Recentering.Enabled 
                    || (axis.InspectorFlags & InputAxis.Flags.HideRecentering) != 0 
                    || CurrentTime - m_LastUpdateTime < axis.Recentering.Wait)
                return;

            var v = axis.ClampValue(axis.Value);
            var c = axis.ClampValue(axis.Center);
            var distance = Mathf.Abs(c - v);
            if (distance < Epsilon || axis.Recentering.Time < Epsilon)
                v = c;
            else
            {
                // Determine the direction
                float r = axis.Range.y - axis.Range.x;
                if (axis.Wrap && distance > r * 0.5f)
                    v += Mathf.Sign(c - v) * r;

                // Damp our way there
                v = Mathf.SmoothDamp(
                    v, c, ref m_RecenteringVelocity,
                    axis.Recentering.Time * 0.5f, 9999, deltaTime);
            }
            axis.Value = axis.ClampValue(v);
        }

        /// <summary>Cancel any current recenting in progress, and reset the wait time</summary>
        public void CancelRecentering()
        {
            m_LastUpdateTime = CurrentTime;
            m_RecenteringVelocity = 0;
        }

        /// <summary>Reset axis to at-rest state</summary>
        /// <param name="axis">The axis to reset</param>
        public void Reset(InputAxis axis)
        {
            m_LastUpdateTime = CurrentTime;
            m_CurrentSpeed = 0;
            m_RecenteringVelocity = 0;
            if (axis.Recentering.Enabled)
                axis.Value = axis.ClampValue(axis.Center);
        }

        float CurrentTime => Time.realtimeSinceStartup;
    }
}
