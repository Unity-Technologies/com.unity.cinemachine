using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Splines;

namespace Cinemachine
{
    /// <summary>
    /// This is a very simple behaviour that constrains its transform to a CinemachinePath.  
    /// It can be used to animate any objects along a path, or as a Follow target for 
    /// Cinemachine Virtual Cameras.
    /// </summary>
    [DocumentationSorting(DocumentationSortingAttribute.Level.UserRef)]
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [HelpURL(Documentation.BaseURL + "manual/CinemachineDollyCart.html")]
    public class CinemachineSplineDollyCart : MonoBehaviour
    {
        /// <summary>The path to follow</summary>
        [Tooltip("The path to follow")]
        public SplineContainer m_Path;
        
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

        /// <summary>How to interpret the Path Position</summary>
        [Tooltip("How to interpret the Path Position:\n"+
        "- Distance: Values range from 0 (start of Spline) to Length of the Spline (end of Spline).\n"+
        "- Normalized: Values range from 0 (start of Spline) to 1 (end of Spline).\n"+
        "- Knot: Values are defined by knot indices and a fractional value representing the"+
        "normalized interpolation between the specific knot index and the next knot.\n")]
        public PathIndexUnit m_PositionUnits = PathIndexUnit.Distance;

        /// <summary>Move the cart with this speed</summary>
        [Tooltip("Move the cart with this speed along the path.  The value is interpreted according to the Position Units setting.")]
        [FormerlySerializedAs("m_Velocity")]
        public float m_Speed;

        /// <summary>The cart's current position on the path, in distance units</summary>
        [Tooltip("The position along the path at which the cart will be placed.  This can be animated directly or, if the velocity is non-zero, will be updated automatically.  The value is interpreted according to the Position Units setting.")]
        [FormerlySerializedAs("m_CurrentDistance")]
        public float m_Position;

        void FixedUpdate()
        {
            if (m_UpdateMethod == UpdateMethod.FixedUpdate)
                SetCartPosition(m_Position + m_Speed * Time.deltaTime);
        }

        void Update()
        {           
            float speed = Application.isPlaying ? m_Speed : 0;
            if (m_UpdateMethod == UpdateMethod.Update)
                SetCartPosition(m_Position + speed * Time.deltaTime);
        }

        void LateUpdate()
        {
            if (!Application.isPlaying)
                SetCartPosition(m_Position);
            else if (m_UpdateMethod == UpdateMethod.LateUpdate)
                SetCartPosition(m_Position + m_Speed * Time.deltaTime);
        }

        void SetCartPosition(float distanceAlongPath)
        {
            if (m_Path != null)
            {
                var splinePath = m_Path.Spline;
                var normalizedPath = splinePath.ConvertIndexUnit(distanceAlongPath, m_PositionUnits, PathIndexUnit.Normalized);
                m_Path.Evaluate(normalizedPath, out var newCameraPos, out _, out var newUpVector);
                transform.position = newCameraPos;
                transform.rotation = Quaternion.FromToRotation(m_Path.transform.up, newUpVector);
                m_Position = splinePath.ConvertIndexUnit(normalizedPath, PathIndexUnit.Normalized, m_PositionUnits);
            } else
            {
                m_Position = 0;
            }
        }
    }
}
