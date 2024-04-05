using UnityEngine;
using UnityEngine.Serialization;
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
        public SplineSettings SplineSettings = new () { Units = PathIndexUnit.Normalized };
        
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
            get => SplineSettings.Spline;
            set => SplineSettings.Spline = value;
        }

        /// <summary>The cart's current position on the spline, in spline position units</summary>
        public float SplinePosition
        {
            get => SplineSettings.Position;
            set => SplineSettings.Position = value;
        }

        /// <summary>How to interpret PositionOnSpline:
        /// - Distance: Values range from 0 (start of Spline) to Length of the Spline (end of Spline).
        /// - Normalized: Values range from 0 (start of Spline) to 1 (end of Spline).
        /// - Knot: Values are defined by knot indices and a fractional value representing the normalized
        /// interpolation between the specific knot index and the next knot."</summary>
        public PathIndexUnit PositionUnits
        {
            get => SplineSettings.Units;
            set => SplineSettings.ChangeUnitPreservePosition(value);
        }

        CinemachineSplineRoll m_RollCache; // don't use this directly - use SplineRoll

        // In-editor only: CM 3.0.x Legacy support =================================
        [SerializeField, HideInInspector, FormerlySerializedAs("SplinePosition")] private float m_LegacyPosition = -1;
        [SerializeField, HideInInspector, FormerlySerializedAs("PositionUnits")] private PathIndexUnit m_LegacyUnits;
        [SerializeField, HideInInspector, FormerlySerializedAs("Spline")] private SplineContainer m_LegacySpline;
        void PerformLegacyUpgrade()
        {
            if (m_LegacyPosition != -1)
            {
                SplineSettings.Position = m_LegacyPosition;
                SplineSettings.Units = m_LegacyUnits;
                m_LegacyPosition = -1;
                m_LegacyUnits = 0;
            }
            if (m_LegacySpline != null)
            {
                SplineSettings.Spline = m_LegacySpline;
                m_LegacySpline = null;
            }
        }
        // =================================

        private void OnValidate()
        {
            PerformLegacyUpgrade(); // only called in-editor
            AutomaticDolly.Method?.Validate();
        }

        void Reset()
        {
            SplineSettings = new SplineSettings { Units = PathIndexUnit.Normalized };
            UpdateMethod = UpdateMethods.Update;
            AutomaticDolly.Method = null;
            TrackingTarget = null;
        }

        void OnEnable()
        {
            RefreshRollCache();
            AutomaticDolly.Method?.Reset();
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
            if (AutomaticDolly.Enabled && AutomaticDolly.Method != null)
                SplinePosition = AutomaticDolly.Method.GetSplinePosition(
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
                transform.ConservativeSetPositionAndRotation(pos, rot);
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
