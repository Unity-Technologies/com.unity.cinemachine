using UnityEngine;
using System;
using Cinemachine.Utility;
using System.Collections.Generic;

namespace Cinemachine
{
    /// <summary>
    /// Components that hold InputAxisValue structs must implement this interface to be discoverable.
    /// </summary>
    public interface IInputAxisSource
    {
        /// <summary>
        /// Desscribes an axis for an axis driver
        /// </summary>
        public struct AxisDescriptor
        {
            /// <summary>Delegate to get a reference to the axis being driven</summary>
            /// <returns>A reference to the axis being driven</returns>
            public delegate ref InputAxis AxisGetter();

            /// <summary>The axis to drive</summary>
            public AxisGetter GetAxis;

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
    }

    /// <summary>
    /// Components that can generate an input axis reset must implement this interface.
    /// </summary>
    public interface IInputAxisResetSource
    {
        /// <summary>
        /// Delegate to be called when input needs to be reset and recentering cancelled.
        /// </summary>
        public delegate void ResetHandler();

        /// <summary>
        /// Register a handler that will be called when input needs to be reset
        /// </summary>
        /// <param name="handler">Then handler to register</param>
        public void RegisterResetHandler(ResetHandler handler);

        /// <summary>
        /// Unregister a handler that will be called when input needs to be reset
        /// </summary>
        /// <param name="handler">Then handler to unregister</param>
        public void UnregisterResetHandler(ResetHandler handler);
    }

    /// <summary>
    /// Defines an input axis.  This is a field that can take on any value in a range, 
    /// with optional wrapping to form a loop.
    /// </summary>
    [Serializable]
    public struct InputAxis
    {
        /// <summary>The current value of the axis.  You can drive this directly from a script</summary>
        [Tooltip("The current value of the axis.  You can drive this directly from a script.")]
        [NoSaveDuringPlay]
        public float Value;

        /// <summary>The current value of the axis.  You can drive this directly from a script</summary>
        [Tooltip("The centered, or at-rest value of this axis.")]
        public float Center;

        /// <summary>The valid range for the axis value.  Value will be clamped to this range.</summary>
        [Tooltip("The valid range for the axis value.  Value will be clamped to this range.")]
        [Vector2AsRange]
        public Vector2 Range;

        /// <summary>If set, then the axis will wrap around at the min/max values, forming a loop</summary>
        [Tooltip("If set, then the axis will wrap around at the min/max values, forming a loop")]
        public bool Wrap;

        /// <summary>Some usages require restricted functionality.  The possible restrictions are defined here.</summary>
        public enum RestrictionFlags 
        { 
            /// <summary>No restrictions</summary>
            None = 0, 
            /// <summary>Range and center are not editable by the user</summary>
            RangeIsDriven = 1, 
            /// <summary>Indicates that recentering this axis is not possible</summary>
            NoRecentering = 2
        };

        /// <summary>Some usages require restricted functionality.  This is set here.</summary>
        [HideInInspector]
        public RestrictionFlags Restrictions;

        /// <summary>Clamp the value to range, taking wrap into account</summary>
        /// <param name="v">The value to clamp</param>
        /// <returns>The value clamped to the axis range</returns>
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
        /// <returns>The axis value, mapped onto [0...1]</returns>
        public float GetNormalizedValue()
        {
            float v = ClampValue(Value);
            float r = Range.y - Range.x;
            return (v - Range.x) / (r > UnityVectorExtensions.Epsilon ? r : 1);
        }

        /// <summary>Get the clamped axis value</summary>
        /// <returns>The axis value, clamped to the axis range</returns>
        public float GetClampedValue() => ClampValue(Value);

        /// <summary>Make sure the settings are well-formed</summary>
        public void Validate()
        {
            Range.y = Mathf.Max(Range.x, Range.y);
            Center = ClampValue(Center);
            Value = ClampValue(Value);
        }
    }

    /// <summary>Defines the settings for automatic recentering</summary>
    [Serializable] 
    public struct InputAxisRecenteringSettings
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
        public static InputAxisRecenteringSettings Default => new() { Wait = 1, Time = 2 };

        /// <summary>Call from OnValidate: Make sure the fields are sensible</summary>
        public void Validate()
        {
            Wait = Mathf.Max(0, Wait);
            Time = Mathf.Max(0, Time); 
        }
    }
  
     /// <summary>Settings for controlling how input value is processed</summary>
    [Serializable]
    public struct InputAxisControl
    {
        /// <summary>The value of the user input for this frame.</summary>
        [Tooltip("The value of the user input for this frame")]
        [NoSaveDuringPlay]
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
        const float k_Epsilon = UnityVectorExtensions.Epsilon;
        float m_LastUpdateTime;
        float m_RecenteringVelocity;
        bool m_ForceRecenter;

        /// <summary>Apply the input value to the axis value</summary>
        /// <param name="deltaTime">current deltaTime</param>
        /// <param name="axis">The InputAxisValue to update</param>
        /// <param name="control">Parameter for controlling the behaviour of the axis</param>
        public void ProcessInput(
            float deltaTime, ref InputAxis axis, 
            ref InputAxisControl control)
        {
            var input = control.InputValue;
            if (deltaTime < 0)
                m_CurrentSpeed = 0;
            else
            {
                var dampTime = Mathf.Abs(input) < Mathf.Abs(m_CurrentSpeed) ? control.DecelTime : control.AccelTime;
                m_CurrentSpeed += Damper.Damp(input - m_CurrentSpeed, dampTime, deltaTime);

                // Decelerate to the end points of the range if not wrapping
                float range = axis.Range.y - axis.Range.x;
                if (!axis.Wrap && control.DecelTime > k_Epsilon && range > k_Epsilon)
                {
                    var v0 = axis.ClampValue(axis.Value);
                    var v = axis.ClampValue(v0 + m_CurrentSpeed * deltaTime);
                    var d = (m_CurrentSpeed > 0) ? axis.Range.y - v : v - axis.Range.x;
                    if (d < (0.1f * range) && Mathf.Abs(m_CurrentSpeed) > k_Epsilon)
                        m_CurrentSpeed = Damper.Damp(v - v0, control.DecelTime, deltaTime) / deltaTime;
                }
            }
            axis.Value = axis.ClampValue(axis.Value + m_CurrentSpeed * deltaTime);

            if (Mathf.Abs(control.InputValue) > k_Epsilon)
                CancelRecentering();
        }

        /// <summary>Call this to manage recentering axis value to axis center.</summary>
        /// <param name="deltaTime">Current deltaTime</param>
        /// <param name="axis">The axis to recenter</param>
        /// <param name="recentering">The recentering settings</param>
        public void DoRecentering(float deltaTime, ref InputAxis axis, in InputAxisRecenteringSettings recentering)
        {
            if (m_ForceRecenter || 
                (recentering.Enabled 
                && CurrentTime - m_LastUpdateTime > recentering.Wait 
                && (axis.Restrictions & InputAxis.RestrictionFlags.NoRecentering) == 0))
            {
                var v = axis.ClampValue(axis.Value);
                var c = axis.ClampValue(axis.Center);
                var distance = Mathf.Abs(c - v);
                if (distance < k_Epsilon || recentering.Time < k_Epsilon)
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
                        recentering.Time * 0.5f, 9999, deltaTime);
                }
                axis.Value = axis.ClampValue(v);

                // Are we there yet?
                if (Mathf.Abs(axis.Value - c) < k_Epsilon)
                    m_ForceRecenter = false;
            }
        }

        /// <summary>Cancel any current recentering in progress, and reset the wait time</summary>
        public void RecenterNow() => m_ForceRecenter = true;

        /// <summary>Cancel any current recentering in progress, and reset the wait time</summary>
        public void CancelRecentering()
        {
            m_LastUpdateTime = CurrentTime;
            m_RecenteringVelocity = 0;
            m_ForceRecenter = false;
        }

        /// <summary>Reset axis to at-rest state</summary>
        /// <param name="axis">The axis to reset</param>
        /// <param name="recentering">The recentering settings</param>
        public void Reset(ref InputAxis axis, in InputAxisRecenteringSettings recentering)
        {
            m_LastUpdateTime = CurrentTime;
            m_CurrentSpeed = 0;
            m_RecenteringVelocity = 0;
            if (recentering.Enabled)
                axis.Value = axis.ClampValue(axis.Center);
            m_ForceRecenter = false;
        }

        static float CurrentTime => CinemachineCore.CurrentUnscaledTime;
    }
}
