using UnityEngine;
using System;
using Cinemachine.Utility;
using UnityEngine.Serialization;

namespace Cinemachine
{
    /// <summary>
    /// Axis state for defining how to react to player input.
    /// The settings here control the responsiveness of the axis to player input.
    /// </summary>
    [DocumentationSorting(DocumentationSortingAttribute.Level.UserRef)]
    [Serializable]
    public struct AxisState
    {
        /// <summary>The current value of the axis</summary>
        [NoSaveDuringPlay]
        [Tooltip("The current value of the axis.")]
        public float Value;

        /// <summary>How fast the axis value can travel.  Increasing this number
        /// makes the behaviour more responsive to joystick input</summary>
        [Tooltip("The maximum speed of this axis in units/second")]
        public float m_MaxSpeed;

        /// <summary>The amount of time in seconds it takes to accelerate to
        /// MaxSpeed with the supplied Axis at its maximum value</summary>
        [Tooltip("The amount of time in seconds it takes to accelerate to MaxSpeed with the supplied Axis at its maximum value")]
        public float m_AccelTime;

        /// <summary>The amount of time in seconds it takes to decelerate
        /// the axis to zero if the supplied axis is in a neutral position</summary>
        [Tooltip("The amount of time in seconds it takes to decelerate the axis to zero if the supplied axis is in a neutral position")]
        public float m_DecelTime;

        /// <summary>The name of this axis as specified in Unity Input manager.
        /// Setting to an empty string will disable the automatic updating of this axis</summary>
        [FormerlySerializedAs("m_AxisName")]
        [Tooltip("The name of this axis as specified in Unity Input manager. Setting to an empty string will disable the automatic updating of this axis")]
        public string m_InputAxisName;

        /// <summary>The value of the input axis.  A value of 0 means no input
        /// You can drive this directly from a
        /// custom input system, or you can set the Axis Name and have the value
        /// driven by the internal Input Manager</summary>
        [NoSaveDuringPlay]
        [Tooltip("The value of the input axis.  A value of 0 means no input.  You can drive this directly from a custom input system, or you can set the Axis Name and have the value driven by the internal Input Manager")]
        public float m_InputAxisValue;

        /// <summary>If checked, then the raw value of the input axis will be inverted
        /// before it is used.</summary>
        [FormerlySerializedAs("m_InvertAxis")]
        [Tooltip("If checked, then the raw value of the input axis will be inverted before it is used")]
        public bool m_InvertInput;

        /// <summary>The minimum value for the axis</summary>
        [Tooltip("The minimum value for the axis")]
        public float m_MinValue;

        /// <summary>The maximum value for the axis</summary>
        [Tooltip("The maximum value for the axis")]
        public float m_MaxValue;

        /// <summary>If checked, then the axis will wrap around at the min/max values, forming a loop</summary>
        [Tooltip("If checked, then the axis will wrap around at the min/max values, forming a loop")]
        public bool m_Wrap;

        /// <summary>Automatic recentering.  Valid only if HasRecentering is true</summary>
        [Tooltip("Automatic recentering to at-rest position")]
        public Recentering m_Recentering;

        private float mCurrentSpeed;

        /// <summary>Constructor with specific values</summary>
        public AxisState(
            float minValue, float maxValue, bool wrap, bool rangeLocked,
            float maxSpeed, float accelTime, float decelTime,
            string name, bool invert)
        {
            m_MinValue = minValue;
            m_MaxValue = maxValue;
            m_Wrap = wrap;
            ValueRangeLocked = rangeLocked;

            HasRecentering = false;
            m_Recentering = new Recentering(false, 1, 2);

            m_MaxSpeed = maxSpeed;
            m_AccelTime = accelTime;
            m_DecelTime = decelTime;
            Value = (minValue + maxValue) / 2;
            m_InputAxisName = name;
            m_InputAxisValue = 0;
            m_InvertInput = invert;

            mCurrentSpeed = 0f;
        }

        /// <summary>Call from OnValidate: Make sure the fields are sensible</summary>
        public void Validate()
        {
            m_MaxSpeed = Mathf.Max(0, m_MaxSpeed);
            m_AccelTime = Mathf.Max(0, m_AccelTime);
            m_DecelTime = Mathf.Max(0, m_DecelTime);
            m_MaxValue = Mathf.Clamp(m_MaxValue, m_MinValue, m_MaxValue);
        }

        const float Epsilon = UnityVectorExtensions.Epsilon;

        public void Reset()
        {
            m_InputAxisValue = 0;
            mCurrentSpeed = 0;
        }

        /// <summary>
        /// Updates the state of this axis based on the axis defined
        /// by AxisState.m_AxisName
        /// </summary>
        /// <param name="deltaTime">Delta time in seconds</param>
        /// <returns>Returns <b>true</b> if this axis' input was non-zero this Update,
        /// <b>false</b> otherwise</returns>
        public bool Update(float deltaTime)
        {
            if (!string.IsNullOrEmpty(m_InputAxisName))
            {
                try
                {
                    m_InputAxisValue = CinemachineCore.GetInputAxis(m_InputAxisName);
                }
                catch (ArgumentException e)
                {
                    Debug.LogError(e.ToString());
                }
            }

            float input = m_InputAxisValue;
            if (m_InvertInput)
                input *= -1f;

            if (m_MaxSpeed > Epsilon)
            {
                float targetSpeed = input * m_MaxSpeed;
                if (Mathf.Abs(targetSpeed) < Epsilon
                    || (Mathf.Sign(mCurrentSpeed) == Mathf.Sign(targetSpeed)
                        && Mathf.Abs(targetSpeed) <  Mathf.Abs(mCurrentSpeed)))
                {
                    // Need to decelerate
                    float a = Mathf.Abs(targetSpeed - mCurrentSpeed) / Mathf.Max(Epsilon, m_DecelTime);
                    float delta = Mathf.Min(a * deltaTime, Mathf.Abs(mCurrentSpeed));
                    mCurrentSpeed -= Mathf.Sign(mCurrentSpeed) * delta;
                }
                else
                {
                    // Accelerate to the target speed
                    float a = Mathf.Abs(targetSpeed - mCurrentSpeed) / Mathf.Max(Epsilon, m_AccelTime);
                    mCurrentSpeed += Mathf.Sign(targetSpeed) * a * deltaTime;
                    if (Mathf.Sign(mCurrentSpeed) == Mathf.Sign(targetSpeed)
                        && Mathf.Abs(mCurrentSpeed) > Mathf.Abs(targetSpeed))
                    {
                        mCurrentSpeed = targetSpeed;
                    }
                }
            }

            // Clamp our max speeds so we don't go crazy
            float maxSpeed = GetMaxSpeed();
            mCurrentSpeed = Mathf.Clamp(mCurrentSpeed, -maxSpeed, maxSpeed);

            Value += mCurrentSpeed * deltaTime;
            bool isOutOfRange = (Value > m_MaxValue) || (Value < m_MinValue);
            if (isOutOfRange)
            {
                if (m_Wrap)
                {
                    if (Value > m_MaxValue)
                        Value = m_MinValue + (Value - m_MaxValue);
                    else
                        Value = m_MaxValue + (Value - m_MinValue);
                }
                else
                {
                    Value = Mathf.Clamp(Value, m_MinValue, m_MaxValue);
                    mCurrentSpeed = 0f;
                }
            }
            return Mathf.Abs(input) > Epsilon;
        }

        // MaxSpeed may be limited as we approach the range ends, in order
        // to prevent a hard bump
        private float GetMaxSpeed()
        {
            float range = m_MaxValue - m_MinValue;
            if (!m_Wrap && range > 0)
            {
                float threshold = range / 10f;
                if (mCurrentSpeed > 0 && (m_MaxValue - Value) < threshold)
                {
                    float t = (m_MaxValue - Value) / threshold;
                    return Mathf.Lerp(0, m_MaxSpeed, t);
                }
                else if (mCurrentSpeed < 0 && (Value - m_MinValue) < threshold)
                {
                    float t = (Value - m_MinValue) / threshold;
                    return Mathf.Lerp(0, m_MaxSpeed, t);
                }
            }
            return m_MaxSpeed;
        }

        /// <summary>Value range is locked, i.e. not adjustable by the user (used by editor)</summary>
        public bool ValueRangeLocked { get; set; }

        /// <summary>True if the Recentering member is valid (bcak-compatibility support:
        /// old versions had recentering in a separate structure)</summary>
        public bool HasRecentering { get; set; }

        /// <summary>Helper for automatic axis recentering</summary>
        [DocumentationSorting(DocumentationSortingAttribute.Level.UserRef)]
        [Serializable]
        public struct Recentering
        {
            /// <summary>If checked, will enable automatic recentering of the
            /// axis. If FALSE, recenting is disabled.</summary>
            [Tooltip("If checked, will enable automatic recentering of the axis. If unchecked, recenting is disabled.")]
            public bool m_enabled;

            /// <summary>If no input has been detected, the camera will wait
            /// this long in seconds before moving its heading to the default heading.</summary>
            [Tooltip("If no user input has been detected on the axis, the axis will wait this long in seconds before recentering.")]
            public float m_WaitTime;

            /// <summary>How long it takes to reach destination once recentering has started</summary>
            [Tooltip("How long it takes to reach destination once recentering has started.")]
            public float m_RecenteringTime;

            /// <summary>Constructor with specific field values</summary>
            public Recentering(bool enabled, float waitTime,  float recenteringTime)
            {
                m_enabled = enabled;
                m_WaitTime = waitTime;
                m_RecenteringTime = recenteringTime;
                mLastAxisInputTime = 0;
                mRecenteringVelocity = 0;
                m_LegacyHeadingDefinition = m_LegacyVelocityFilterStrength = -1;
            }

            /// <summary>Call this from OnValidate()</summary>
            public void Validate()
            {
                m_WaitTime = Mathf.Max(0, m_WaitTime);
                m_RecenteringTime = Mathf.Max(0, m_RecenteringTime);
            }

            // Internal state
            float mLastAxisInputTime;
            float mRecenteringVelocity;
            public void CopyStateFrom(ref Recentering other)
            {
                if (mLastAxisInputTime != other.mLastAxisInputTime)
                    other.mRecenteringVelocity = 0;
                mLastAxisInputTime = other.mLastAxisInputTime;
            }

            /// <summary>Cancel any recenetering in progress.</summary>
            public void CancelRecentering()
            {
                mLastAxisInputTime = Time.time;
                mRecenteringVelocity = 0;
            }

            /// <summary>Skip the wait time and start recentering now (only if enabled).</summary>
            public void RecenterNow()
            {
                mLastAxisInputTime = 0;
            }

            /// <summary>Bring the axis back to the cenetered state (only if enabled).</summary>
            public void DoRecentering(ref AxisState axis, float deltaTime, float recenterTarget)
            {
                if (!m_enabled)
                    return;

                if (deltaTime < 0)
                {
                    CancelRecentering();
                    axis.Value = recenterTarget;
                }
                else if (Time.time > (mLastAxisInputTime + m_WaitTime))
                {
                    // Scale value determined heuristically, to account for accel/decel
                    float recenterTime = m_RecenteringTime / 3f;
                    if (recenterTime <= deltaTime)
                        axis.Value = recenterTarget;
                    else
                    {
                        float headingError = Mathf.DeltaAngle(axis.Value, recenterTarget);
                        float absHeadingError = Mathf.Abs(headingError);
                        if (absHeadingError < UnityVectorExtensions.Epsilon)
                        {
                            axis.Value = recenterTarget;
                            mRecenteringVelocity = 0;
                        }
                        else
                        {
                            float scale = deltaTime / recenterTime;
                            float desiredVelocity = Mathf.Sign(headingError)
                                * Mathf.Min(absHeadingError, absHeadingError * scale);
                            // Accelerate to the desired velocity
                            float accel = desiredVelocity - mRecenteringVelocity;
                            if ((desiredVelocity < 0 && accel < 0) || (desiredVelocity > 0 && accel > 0))
                                desiredVelocity = mRecenteringVelocity + desiredVelocity * scale;
                            axis.Value += desiredVelocity;
                            mRecenteringVelocity = desiredVelocity;
                        }
                    }
                }
            }

            // Legacy support
            [SerializeField] [HideInInspector] [FormerlySerializedAs("m_HeadingDefinition")] private int m_LegacyHeadingDefinition;
            [SerializeField] [HideInInspector] [FormerlySerializedAs("m_VelocityFilterStrength")] private int m_LegacyVelocityFilterStrength;
            internal bool LegacyUpgrade(ref int heading, ref int velocityFilter)
            {
                if (m_LegacyHeadingDefinition != -1 && m_LegacyVelocityFilterStrength != -1)
                {
                    heading = m_LegacyHeadingDefinition;
                    velocityFilter = m_LegacyVelocityFilterStrength;
                    m_LegacyHeadingDefinition = m_LegacyVelocityFilterStrength = -1;
                    return true;
                }
                return false;
            }
        }
    }
}
