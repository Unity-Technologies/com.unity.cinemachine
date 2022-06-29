using UnityEngine;
using UnityEngine.Splines;

namespace Cinemachine
{
    /// <summary>
    /// This is a very simple behaviour that constrains its transform to a Spline.  
    /// It can be used to animate any objects along a path, or as a tracking target for 
    /// Cinemachine Cameras.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [HelpURL(Documentation.BaseURL + "manual/CinemachineSplineCart.html")]
    public class CinemachineSplineCart : MonoBehaviour
    {
        /// <summary>The Spline container to which the camera will be constrained.</summary>
        [Tooltip("The Spline container to which the cart will be constrained.")]
        public SplineContainer Spline;
        
        /// <summary>This enum defines the options available for the update method.</summary>
        public enum UpdateMethods
        {
            /// <summary>Updated in normal MonoBehaviour Update.</summary>
            Update,
            /// <summary>Updated in sync with the Physics module, in FixedUpdate</summary>
            FixedUpdate,
            /// <summary>Updated in normal MonoBehaviour LateUpdate</summary>
            LateUpdate
        };

        /// <summary>When to move the cart, if Speed is non-zero</summary>
        [Tooltip("When to move the cart, if Speed is non-zero")]
        public UpdateMethods UpdateMethod = UpdateMethods.Update;

        /// <summary>How to interpret the Spline Position</summary>
        [Tooltip("How to interpret the Spline Position.  If set to Knot, values are as follows: "
            + "0 represents the first knot on the path, 1 is the second, and so on.  "
            + "Values in-between are points on the spline in between the knots.  "
            + "If set to Distance, then Spline Position represents distance along the spline.")]
        public PathIndexUnit PositionUnits = PathIndexUnit.Distance;

        /// <summary>Move the cart with this speed</summary>
        [Tooltip("Move the cart with this speed along the spline.  "
            + " The value is interpreted according to the Position Units setting.")]
        public float Speed;

        /// <summary>The cart's current position on the spline, in position units</summary>
        [Tooltip("The position along the spline at which the cart will be placed.  "
            + "This can be animated directly or, if the velocity is non-zero, will be updated automatically.  "
            + "The value is interpreted according to the Position Units setting.")]
        public float SplinePosition;

        CinemachineSplineRoll m_RollCache; // don't use this directly - use SplineRoll

        void Reset()
        {
            Spline = null;
            UpdateMethod = UpdateMethods.Update;
            PositionUnits = PathIndexUnit.Distance;
            Speed = 0;
            SplinePosition = 0;
        }

        void OnEnable()
        {
            RefreshRollCache();
        }

        void FixedUpdate()
        {
            if (UpdateMethod == UpdateMethods.FixedUpdate)
                SetCartPosition(SplinePosition + Speed * Time.deltaTime);
        }

        void Update()
        {
            float speed = Application.isPlaying ? Speed : 0;
            if (UpdateMethod == UpdateMethods.Update)
                SetCartPosition(SplinePosition + speed * Time.deltaTime);
        }

        void LateUpdate()
        {
            if (!Application.isPlaying)
                SetCartPosition(SplinePosition);
            else if (UpdateMethod == UpdateMethods.LateUpdate)
                SetCartPosition(SplinePosition + Speed * Time.deltaTime);
        }

        void SetCartPosition(float distanceAlongPath)
        {
            if (Spline != null && Spline.Spline != null)
            {
                SplinePosition = Spline.Spline.StandardizeSplinePosition(distanceAlongPath, PositionUnits, Spline.Spline.GetLength());
                var t = Spline.Spline.ConvertIndexUnit(SplinePosition, PositionUnits, PathIndexUnit.Normalized);
                Spline.EvaluateSplineWithRoll(SplineRoll, t, out var pos, out var rot);
                transform.position = pos;
                transform.rotation = rot;
            }
        }

        CinemachineSplineRoll SplineRoll
        {
            get
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    RefreshRollCache();
#endif
                return m_RollCache;
            }
        }
        
        void RefreshRollCache()
        {
            // check if we have CinemachineSplineRoll
            TryGetComponent(out m_RollCache);
#if UNITY_EDITOR
            // need to tell CinemachineSplineRoll about its spline for gizmo drawing purposes
            if (m_RollCache != null)
                m_RollCache.SplineContainer = Spline; 
#endif
            // check if our spline has CinemachineSplineRoll
            if (Spline != null && m_RollCache == null)
                Spline.TryGetComponent(out m_RollCache);
        }
    }
}
