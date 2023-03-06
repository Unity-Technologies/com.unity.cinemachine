using UnityEngine;
using UnityEngine.Serialization;

namespace Unity.Cinemachine
{
    /// <summary>
    /// A definition of an impulse signal that gets propagated to listeners
    /// </summary>
    [HelpURL(Documentation.BaseURL + "manual/CinemachineImpulseFixedSignals.html")]
    public class CinemachineFixedSignal : SignalSourceAsset
    {
        /// <summary>The raw signal shape along the X axis</summary>
        [Tooltip("The raw signal shape along the X axis")]
        [FormerlySerializedAs("m_XCurve")]
        public AnimationCurve XCurve;

        /// <summary>The raw signal shape along the Y axis</summary>
        [Tooltip("The raw signal shape along the Y axis")]
        [FormerlySerializedAs("m_YCurve")]
        public AnimationCurve YCurve;

        /// <summary>The raw signal shape along the Z axis</summary>
        [Tooltip("The raw signal shape along the Z axis")]
        [FormerlySerializedAs("m_ZCurve")]
        public AnimationCurve ZCurve;

        /// <summary>
        /// Returns the length on seconds of the signal.  
        /// Returns 0 for signals of indeterminate length.
        /// </summary>
        public override float SignalDuration 
        { 
            get
            {
                return Mathf.Max(
                    AxisDuration(XCurve), 
                    Mathf.Max(AxisDuration(YCurve), AxisDuration(ZCurve)));
            }
        }

        float AxisDuration(AnimationCurve axis)
        {
            float duration = 0;
            if (axis != null && axis.length > 1)
            {
                float start = axis[0].time;
                duration = axis[axis.length-1].time - start;
            }
            return duration;
        }
    
        /// <summary>Get the raw signal at this time</summary>
        /// <param name="timeSinceSignalStart">The time since in seconds since the start of the signal</param>
        /// <param name="pos">The position impulse signal</param>
        /// <param name="rot">The rotation impulse signal</param>
        public override void GetSignal(float timeSinceSignalStart, out Vector3 pos, out Quaternion rot)
        {
            rot = Quaternion.identity;
            pos = new Vector3(
                AxisValue(XCurve, timeSinceSignalStart),
                AxisValue(YCurve, timeSinceSignalStart),
                AxisValue(ZCurve, timeSinceSignalStart));
        }

        float AxisValue(AnimationCurve axis, float time)
        {
            if (axis == null || axis.length == 0)
                return 0;
            return axis.Evaluate(time);
        }
    }
}
