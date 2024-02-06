using UnityEngine;
using UnityEngine.Splines;

namespace Unity.Cinemachine
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
        /// <summary>
        /// Holds the Spline container, the spline position, and the position unit type
        /// </summary>
        public SplinePosition SplinePosition = new SplinePosition { Units = PathIndexUnit.Normalized };
        
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

        /// <summary>Controls how automatic dollying occurs</summary>
        [FoldoutWithEnabledButton]
        [Tooltip("Controls how automatic dollying occurs.  A tracking target may be necessary to use this feature.")]
        public SplineAutoDolly AutomaticDolly;
        
        /// <summary>Used only by Automatic Dolly settings that require it</summary>
        [Tooltip("Used only by Automatic Dolly settings that require it")]
        public Transform TrackingTarget;

        /// <summary>The Spline container to which the cart will be constrained.</summary>
        public SplineContainer Spline
        {
            get => SplinePosition.Spline;
            set => SplinePosition.Spline = value;
        }

        /// <summary>The cart's current position on the spline, in spline position units</summary>
        public float PositionOnSpline
        {
            get => SplinePosition.Position;
            set => SplinePosition.Position = value;
        }

        /// <summary>How to interpret PositionOnSpline:
        /// - Distance: Values range from 0 (start of Spline) to Length of the Spline (end of Spline).
        /// - Normalized: Values range from 0 (start of Spline) to 1 (end of Spline).
        /// - Knot: Values are defined by knot indices and a fractional value representing the normalized
        /// interpolation between the specific knot index and the next knot."</summary>
        public PathIndexUnit Units
        {
            get => SplinePosition.Units;
            set => SplinePosition.ChangeUnitPreservePosition(value);
        }

        CinemachineSplineRoll m_RollCache; // don't use this directly - use SplineRoll

        private void OnValidate()
        {
            if (AutomaticDolly.Method != null)
                AutomaticDolly.Method.Validate();
        }

        void Reset()
        {
            SplinePosition = new SplinePosition { Units = PathIndexUnit.Normalized };
            UpdateMethod = UpdateMethods.Update;
            AutomaticDolly.Method = null;
            TrackingTarget = null;
        }

        void OnEnable()
        {
            RefreshRollCache();
            if (AutomaticDolly.Method != null)
                AutomaticDolly.Method.Reset();
        }

        void FixedUpdate()
        {
            if (UpdateMethod == UpdateMethods.FixedUpdate)
                UpdateCartPosition();
        }

        void Update()
        {
            if (!Application.isPlaying)
                SetCartPosition(PositionOnSpline);
            else if (UpdateMethod == UpdateMethods.Update)
                UpdateCartPosition();
        }

        void LateUpdate()
        {
            if (!Application.isPlaying)
                SetCartPosition(PositionOnSpline);
            else if (UpdateMethod == UpdateMethods.LateUpdate)
                UpdateCartPosition();
        }

        void UpdateCartPosition()
        {
            if (AutomaticDolly.Enabled && AutomaticDolly.Method != null)
                PositionOnSpline = AutomaticDolly.Method.GetSplinePosition(
                    this, TrackingTarget, Spline, PositionOnSpline, Units, Time.deltaTime);
            SetCartPosition(PositionOnSpline);
        }

        void SetCartPosition(float distanceAlongPath)
        {
            if (Spline.IsValid())
            {
                PositionOnSpline = Spline.Spline.StandardizePosition(distanceAlongPath, Units, Spline.Spline.GetLength());
                var t = Spline.Spline.ConvertIndexUnit(PositionOnSpline, Units, PathIndexUnit.Normalized);
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
