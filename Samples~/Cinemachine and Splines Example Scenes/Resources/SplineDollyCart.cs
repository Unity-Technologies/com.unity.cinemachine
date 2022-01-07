using System;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

namespace Cinemachine.Samples
{
    /// <summary>
    /// This is a modified version of the AnimateCarAlongSpline script found in the Splines package.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class SplineDollyCart : MonoBehaviour
    {
        /// <summary>SplineContainer that contains the spline which defines the dolly track.</summary>
        [Tooltip("SplineContainer that contains the spline which defines the dolly track.")]
        public SplineContainer track;
        
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
        public UpdateMethod updateMethod = UpdateMethod.Update;

        /// <summary>The cart's current position on the path.</summary>
        [Tooltip("The position along the path at which the cart will be placed.  This can be animated directly or, " +
            "if the velocity is non-zero, will be updated automatically.  \n" +
            "The value is interpreted according to the Position Units setting.")]
        public float position;
        
        /// <summary>How to interpret the Path Position</summary>
        [Tooltip("How to interpret the Path Position:\n"+
            "- Distance: Values range from 0 (start of Spline) to Length of the Spline (end of Spline).\n"+
            "- Normalized: Values range from 0 (start of Spline) to 1 (end of Spline).\n"+
            "- Knot: Values are defined by knot indices and a fractional value representing the"+
            "normalized interpolation between the specific knot index and the next knot.\n")]
        public PathIndexUnit positionUnit;
        
        /// <summary>Default speed of the cart on the track.</summary>
        [Tooltip("Default speed of the cart on the track. Unit is defined by PositionUnit.")]
        public float defaultSpeed = 10f;

        /// <summary>Positive speed value overrides for specific positions on the track. Values that are less than or equal to 0, are replaced with m_DefaultSpeed.</summary>
        [Tooltip("Positive speed value overrides for specific positions on the track. Values that are less than or equal to 0, are replaced with m_DefaultSpeed.")]
        [SpeedHandle(50f)]
        public SplineData<float> speedOverride;

        /// <summary>
        /// Roll (in angles) along the forward direction for specific location on the track.
        /// </summary>
        [Tooltip("Roll (in angles) along the forward direction for specific location on the track.")]
        [RollHandle]
        public SplineData<float> rollOverride;

        /// <summary>Default offset of the cart on the track.</summary>
        public float defaultOffset = 0f;
        
        /// <summary>Offset value overrides for specific location on the track. Could be useful for simulating a Jib Arm.</summary>
        [Tooltip("Offset value overrides for specific location on the track. Could be useful for simulating a Jib Arm.")]
        [DriftHandle]
        public SplineData<float> offsetOverride;
        
        /// <summary>Override this if you'd like to react to spline changes. By default, in Editor only,
        /// it warns the user when a change happens to the spline about a potentially affected vcam</summary>
        protected virtual void OnChangeEvent()
        {
#if UNITY_EDITOR
            Debug.Log("Spline ("+ track.gameObject.name +") used by "+ name +" has changed!");
#endif
        }
        
        bool m_Registered = false;
        SplineContainer m_TrackCache;
        void OnValidate()
        {
            if (speedOverride != null)
                for(int index = 0; index < speedOverride.Count; index++)
                {
                    var data = speedOverride[index];
                    //We don't want to have a value that is negative or null as it might block the simulation
                    if(data.Value <= 0)
                    {
                        data.Value = defaultSpeed;
                        speedOverride[index] = data;
                    }
                }

            if (m_TrackCache != null)
            {
                m_TrackCache.Spline.changed -= OnChangeEvent;
                m_TrackCache = track;
                m_Registered = false;
            }
            if (!m_Registered && track != null && track.Spline != null)
            {
                m_Registered = true;
                m_TrackCache = track;
                track.Spline.changed += OnChangeEvent;
            }
        }

        void Update()
        {
            if (updateMethod == UpdateMethod.Update)
                CalculateCartPosition();
        }

        void LateUpdate()
        {
            if (updateMethod == UpdateMethod.LateUpdate)
                CalculateCartPosition();
        }

        void FixedUpdate()
        {
            if (updateMethod == UpdateMethod.FixedUpdate)
                CalculateCartPosition();
        }

        float m_CurrentSpeed;
        float m_NormalizedPosition;
        void CalculateCartPosition()
        {
            if(track == null)
                return;
            if (track.Spline.Count == 1)
            {
                position = 0;
                transform.position = track.transform.TransformPoint(track.Spline[0].Position);
                return;
            }
            if (!Application.isPlaying) 
                m_CurrentSpeed = 0;

            var spline = track.Spline;
            m_NormalizedPosition = spline.ConvertIndexUnit(position + m_CurrentSpeed * Time.deltaTime, positionUnit, PathIndexUnit.Normalized);
            m_NormalizedPosition = spline.Closed ? m_NormalizedPosition % 1f : Mathf.Clamp01(m_NormalizedPosition);
            
            if (speedOverride != null && speedOverride.Count > 0)
                m_CurrentSpeed = speedOverride.Evaluate(spline, m_NormalizedPosition, PathIndexUnit.Normalized, 
                    new UnityEngine.Splines.Interpolators.LerpFloat());
            else
                m_CurrentSpeed = defaultSpeed;

            SplineUtility.Evaluate(spline, m_NormalizedPosition, 
                out var posOnSplineLocal, out var direction, out var upSplineDirection);
            direction = FixDirection(direction, spline);
            var right = math.normalize(math.cross(upSplineDirection, direction));
            var offsetOverride = 
                (this.offsetOverride == null || this.offsetOverride.Count == 0) ? 
                    defaultOffset : 
                    this.offsetOverride.Evaluate(spline, m_NormalizedPosition, PathIndexUnit.Normalized, 
                        new UnityEngine.Splines.Interpolators.LerpFloat());
            
            transform.position = track.transform.TransformPoint(posOnSplineLocal + offsetOverride * right);

            var roll = 
                (rollOverride == null  || rollOverride.Count == 0) ?
                    0 : 
                    rollOverride.Evaluate(spline, m_NormalizedPosition,PathIndexUnit.Normalized, 
                        new UnityEngine.Splines.Interpolators.LerpFloat());

            var rollRotation = Quaternion.AngleAxis(-roll, direction);
            transform.rotation = Quaternion.LookRotation(direction, rollRotation * upSplineDirection);
            
            // convert unit back to user's preference
            position = spline.ConvertIndexUnit(m_NormalizedPosition, PathIndexUnit.Normalized, positionUnit);
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