using System;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

namespace Cinemachine.Samples
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class CinemachineSplineDollyCart : MonoBehaviour
    {
        /// <summary>SplineContainer that contains the spline which defines the dolly track.</summary>
        [Tooltip("SplineContainer that contains the spline which defines the dolly track.")]
        public SplineContainer m_Track;
        
        /// <summary>This enum defines the options available for the update method.</summary>
        public enum UpdateMethod
        {
            /// <summary>Updated in normal MonoBehaviour Update.</summary>
            Update,
            /// <summary>Updated in sync with the Physics module, in FixedUpdate</summary>
            FixedUpdate,
            /// <summary>Updated in normal MonoBehaviour LateUpdate</summary>
            LateUpdate
        };

        /// <summary>When to move the cart, if Velocity is non-zero</summary>
        [Tooltip("When to move the cart, if Velocity is non-zero")]
        public UpdateMethod m_UpdateMethod = UpdateMethod.Update;

        /// <summary>The cart's current position on the path.</summary>
        [Tooltip("The position along the path at which the cart will be placed.  This can be animated directly or, " +
            "if the velocity is non-zero, will be updated automatically.  \n" +
            "The value is interpreted according to the Position Units setting.")]
        public float m_Position;
        
        /// <summary>How to interpret the Path Position</summary>
        [Tooltip("How to interpret the Path Position:\n"+
            "- Distance: Values range from 0 (start of Spline) to Length of the Spline (end of Spline).\n"+
            "- Normalized: Values range from 0 (start of Spline) to 1 (end of Spline).\n"+
            "- Knot: Values are defined by knot indices and a fractional value representing the"+
            "normalized interpolation between the specific knot index and the next knot.\n")]
        public PathIndexUnit m_PositionUnit;
        
        /// <summary>Default speed of the cart on the track.</summary>
        [Tooltip("Default speed of the cart on the track. Unit is defined by PositionUnit.")]
        public float m_DefaultSpeed = 10f;

        /// <summary>Positive speed value overrides for specific positions on the track. Values that are less than or equal to 0, are replaced with m_DefaultSpeed.</summary>
        [Tooltip("Positive speed value overrides for specific positions on the track. Values that are less than or equal to 0, are replaced with m_DefaultSpeed.")]
        [SpeedHandle(50f)]
        public SplineData<float> m_SpeedOverride;
        
        /// <summary>Default tilt of the cart on the track.</summary>
        [Tooltip("Default tilt of the cart on the track.")]
        public Vector3 m_DefaultTilt = Vector3.up;
        
        /// <summary>Tilt value overrides for specific location on the track. Vectors with magnitude 0 are replaced with m_DefaultTilt.</summary>
        [Tooltip("Tilt value overrides for specific location on the track. Vectors with magnitude 0 are replaced with m_DefaultTilt.")]
        [TiltHandle]
        public SplineData<float3> m_TiltOverride;

        /// <summary>Default offset of the cart on the track.</summary>
        public float m_DefaultOffset = 0f;
        
        /// <summary>Offset value overrides for specific location on the track. Could be useful for simulating a Jib Arm.</summary>
        [Tooltip("Offset value overrides for specific location on the track. Could be useful for simulating a Jib Arm.")]
        [DriftHandle]
        public SplineData<float> m_OffsetOverride;
        
        
        void OnValidate()
        {
            if (m_SpeedOverride != null)
                for(int index = 0; index < m_SpeedOverride.Count; index++)
                {
                    var data = m_SpeedOverride[index];
                    //We don't want to have a value that is negative or null as it might block the simulation
                    if(data.Value <= 0)
                    {
                        data.Value = m_DefaultSpeed;
                        m_SpeedOverride[index] = data;
                    }
                }
            
            if (m_TiltOverride != null)
                for(int index = 0; index < m_TiltOverride.Count; index++)
                {
                    var data = m_TiltOverride[index];
                    //We don't want to have a up vector of magnitude 0
                    if(math.length(data.Value) == 0)
                    {
                        data.Value = m_DefaultTilt;
                        m_TiltOverride[index] = data;
                    }
                }
        }

        void Update()
        {
            if (m_UpdateMethod == UpdateMethod.Update)
                CalculateCartPosition();
        }

        void LateUpdate()
        {
            if (m_UpdateMethod == UpdateMethod.LateUpdate)
                CalculateCartPosition();
        }

        void FixedUpdate()
        {
            if (m_UpdateMethod == UpdateMethod.FixedUpdate)
                CalculateCartPosition();
        }

        float m_CurrentSpeed;
        float m_NormalizedPosition;
        void CalculateCartPosition()
        {
            if(m_Track == null)
                return;
            if (m_Track.Spline.Count == 1)
            {
                m_Position = 0;
                transform.position = m_Track.transform.TransformPoint(m_Track.Spline[0].Position);
                return;
            }
            if (!Application.isPlaying) 
                m_CurrentSpeed = 0;

            var spline = m_Track.Spline;
            m_NormalizedPosition = spline.ConvertIndexUnit(m_Position + m_CurrentSpeed * Time.deltaTime, m_PositionUnit, PathIndexUnit.Normalized);
            m_NormalizedPosition = spline.Closed ? m_NormalizedPosition % 1f : Mathf.Clamp01(m_NormalizedPosition);
            
            if (m_SpeedOverride != null && m_SpeedOverride.Count > 0)
                m_CurrentSpeed = m_SpeedOverride.Evaluate(spline, m_NormalizedPosition, PathIndexUnit.Normalized, 
                    new UnityEngine.Splines.Interpolators.LerpFloat());
            else
                m_CurrentSpeed = m_DefaultSpeed;

            SplineUtility.Evaluate(spline, m_NormalizedPosition, 
                out var posOnSplineLocal, out var direction, out var upSplineDirection);
            direction = FixDirection(direction, spline);
            var right = math.normalize(math.cross(upSplineDirection, direction));
            var offsetOverride = 
                (m_OffsetOverride == null || m_OffsetOverride.Count == 0) ? 
                    m_DefaultOffset : 
                    m_OffsetOverride.Evaluate(spline, m_NormalizedPosition, PathIndexUnit.Normalized, 
                        new UnityEngine.Splines.Interpolators.LerpFloat());
            
            transform.position = m_Track.transform.TransformPoint(posOnSplineLocal + offsetOverride * right);

            var up = 
                (m_TiltOverride == null  || m_TiltOverride.Count == 0) ?
                    m_DefaultTilt : 
                    (Vector3)m_TiltOverride.Evaluate(spline, m_NormalizedPosition,PathIndexUnit.Normalized, 
                        new UnityEngine.Splines.Interpolators.LerpFloat3());

            var rot = Quaternion.LookRotation(direction, upSplineDirection);
            transform.rotation = Quaternion.LookRotation(direction, rot * up);
            
            // convert unit back to user's preference
            m_Position = spline.ConvertIndexUnit(m_NormalizedPosition, PathIndexUnit.Normalized, m_PositionUnit);
        }
        
        static float3 FixDirection(float3 dir, Spline spline)
        {
            return dir.x == 0 && dir.y == 0 && dir.z == 0 ? math.normalize(spline[1].Position - spline[0].Position) : dir;
        }
    }
    
    // Attribute handles for dolly cart
    [AttributeUsage(AttributeTargets.Field)]
    public class SpeedHandleAttribute : SplineDataHandleAttribute
    {
        public float maxSpeed;
        public SpeedHandleAttribute(float maxSpeed)
        {
            this.maxSpeed = maxSpeed;
        }
    }
    
    [AttributeUsage(AttributeTargets.Field)]
    public class DriftHandleAttribute : SplineDataHandleAttribute {}
}