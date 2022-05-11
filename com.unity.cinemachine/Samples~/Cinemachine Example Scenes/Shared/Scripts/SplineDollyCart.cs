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
        /// <summary>SplineContainer that contains the spline on which to dolly.</summary>
        [Tooltip("SplineContainer that contains the spline on which to dolly.")]
        public SplineContainer m_Spline;
        
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

        /// <summary>The cart's current position on the spline.</summary>
        [Tooltip("The position along the spline at which the cart will be placed.  This can be animated directly or, " +
            "if the velocity is non-zero, will be updated automatically.  \n" +
            "The value is interpreted according to the Position Units setting.")]
        public float position;
        
        /// <summary>How to interpret the Spline Position</summary>
        [Tooltip("How to interpret the position:\n"+
            "- Distance: Values range from 0 (start of Spline) to Length of the Spline (end of Spline).\n"+
            "- Normalized: Values range from 0 (start of Spline) to 1 (end of Spline).\n"+
            "- Knot: Values are defined by knot indices and a fractional value representing the"+
            "normalized interpolation between the specific knot index and the next knot.\n")]
        public PathIndexUnit positionUnit;
        
        /// <summary>Default speed of the cart on the spline.</summary>
        [Tooltip("Default speed of the cart on the spline. Unit is defined by PositionUnit.")]
        public float speed = 10f;

        /// <summary>
        /// Roll (in angles) along the forward direction for specific location on the spline.
        /// </summary>
        [Tooltip("Roll (in angles) along the forward direction for specific location on the spline.")]
        [SplineRollHandle]
        public SplineData<float> rollOverride;

        /// <summary>
        /// Subscribe to onSplineChanged if you'd like to react to changes to the Spline attached to this vcam.
        /// This action is invoked by the Spline's changed event when a spline property is modified. Available in editor only.
        /// </summary>
        public event Action onSplineChanged;
        
        bool m_Registered = false;
        SplineContainer m_SplineCache;
        void OnValidate()
        {
            if (m_SplineCache != null)
            {
                m_SplineCache.Spline.changed -= onSplineChanged;
                m_SplineCache = m_Spline;
                m_Registered = false;
            }
            if (!m_Registered && m_Spline != null && m_Spline.Spline != null)
            {
                m_Registered = true;
                m_SplineCache = m_Spline;
                m_Spline.Spline.changed += onSplineChanged;
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
            if(m_Spline == null)
                return;
            if (m_Spline.Spline.Count == 1)
            {
                position = 0;
                transform.position = m_Spline.transform.TransformPoint(m_Spline.Spline[0].Position);
                return;
            }
            if (!Application.isPlaying) 
                m_CurrentSpeed = 0;

            var spline = m_Spline.Spline;
            m_NormalizedPosition = spline.ConvertIndexUnit(position + m_CurrentSpeed * Time.deltaTime, positionUnit, PathIndexUnit.Normalized);
            m_NormalizedPosition = spline.Closed ? m_NormalizedPosition % 1f : Mathf.Clamp01(m_NormalizedPosition);
            
            m_CurrentSpeed = speed;

            SplineUtility.Evaluate(spline, m_NormalizedPosition, 
                out var posOnSplineLocal, out var direction, out var upSplineDirection);
            direction = FixDirection(direction, spline);
            transform.position = m_Spline.transform.TransformPoint(posOnSplineLocal);

            var roll = 
                (rollOverride == null || rollOverride.Count == 0) ?
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
}