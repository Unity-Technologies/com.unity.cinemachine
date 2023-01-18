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
    [AddComponentMenu("Cinemachine/Helpers/Cinemachine Spline Cart")]
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

        /// <summary>Controls how automatic dollying occurs</summary>
        [Tooltip("Controls how automatic dollying occurs.  A tracking target may be necessary to use this feature.")]
        public SplineAutoDolly AutomaticDolly;
        
        /// <summary>Used only by Automatic Dolly settings that require it</summary>
        [Tooltip("Used only by Automatic Dolly settings that require it")]
        public Transform TrackingTarget;

        /// <summary>The cart's current position on the spline, in position units</summary>
        [NoSaveDuringPlay]
        [Tooltip("The position along the spline at which the cart will be placed.  "
            + "This can be animated directly or, if the velocity is non-zero, will be updated automatically.  "
            + "The value is interpreted according to the Position Units setting.")]
        public float SplinePosition;

        CinemachineSplineRoll m_RollCache; // don't use this directly - use SplineRoll

        private void OnValidate()
        {
            if (AutomaticDolly.Implementation != null)
                AutomaticDolly.Implementation.Validate();
        }

        void Reset()
        {
            Spline = null;
            UpdateMethod = UpdateMethods.Update;
            PositionUnits = PathIndexUnit.Distance;
            AutomaticDolly.Implementation = null;
            TrackingTarget = null;
            SplinePosition = 0;
        }

        void OnEnable()
        {
            RefreshRollCache();
            if (AutomaticDolly.Implementation != null)
                AutomaticDolly.Implementation.Reset();
        }

        void FixedUpdate()
        {
            if (UpdateMethod == UpdateMethods.FixedUpdate)
                UpdateCartPosition();
        }

        void Update()
        {
            if (!Application.isPlaying)
                SetCartPosition(SplinePosition);
            else if (UpdateMethod == UpdateMethods.Update)
                UpdateCartPosition();
        }

        void LateUpdate()
        {
            if (!Application.isPlaying)
                SetCartPosition(SplinePosition);
            else if (UpdateMethod == UpdateMethods.LateUpdate)
                UpdateCartPosition();
        }

        void UpdateCartPosition()
        {
            if (AutomaticDolly.Implementation != null)
                SplinePosition = AutomaticDolly.Implementation.GetSplinePosition(
                    this, TrackingTarget, Spline, SplinePosition, PositionUnits, Time.deltaTime);
            SetCartPosition(SplinePosition);
        }

        void SetCartPosition(float distanceAlongPath)
        {
            if (Spline.IsValid())
            {
                SplinePosition = Spline.Spline.StandardizePosition(distanceAlongPath, PositionUnits, Spline.Spline.GetLength());
                var t = Spline.Spline.ConvertIndexUnit(SplinePosition, PositionUnits, PathIndexUnit.Normalized);
                Spline.EvaluateSplineWithRoll(SplineRoll, transform.rotation, t, out var pos, out var rot);
                transform.SetPositionAndRotation(pos, rot);
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
                m_RollCache.Container = Spline; 
#endif
            // check if our spline has CinemachineSplineRoll
            if (Spline != null && m_RollCache == null)
                Spline.TryGetComponent(out m_RollCache);
        }
    }
}
