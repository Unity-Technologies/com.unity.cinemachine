#if CINEMACHINE_UNITY_SPLINES
using System;
using UnityEngine;
using UnityEngine.Splines;

namespace Cinemachine
{
    /// <summary>
    /// Extension that can be added to a SplineContainer or a vcam that uses a SplineContainer, for example a vcam
    /// that has SplineDolly as Body component.
    /// - When CinemachineSplineRoll is added to a gameObject that has SplineContainer,
    /// then the roll affects any vcam that reads that SplineContainer globally.
    /// - When CinemachineSplineRoll is added to a vcam, then roll only affects that vcam locally.
    /// </summary>
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    public class CinemachineSplineRoll : MonoBehaviour
    {
        /// <summary>
        /// Roll (in degrees) around the forward direction for specific location on the track.
        /// When placed on a SplineContainer, this is going to be a global override that affects all vcams using the Spline.
        /// When placed on a vcam, this is going to be a local override that only affects that vcam.
        /// </summary>
        [Tooltip("Roll (in degrees) around the forward direction for specific location on the track.\n" +
            "- When placed on a SplineContainer, this is going to be a global override that affects all vcams using the Spline.\n" +
            "- When placed on a vcam, this is going to be a local override that only affects that vcam.")]
        [SplineRollHandle]
        public SplineData<float> Roll;

#if UNITY_EDITOR
        // Only needed for drawing the gizmo
        internal SplineContainer SplineContainer
        {
            get 
            { 
                // In case behavour was reparented in the editor, we check every time
                TryGetComponent(out SplineContainer container);
                if (container != null)
                    return container;
                return m_SplineContainer;
            }
            set { m_SplineContainer = value; }
        }
        SplineContainer m_SplineContainer;
#endif
        void OnEnable() {} // Needed, so we can disable it in the editor
    }
    
    [AttributeUsage(AttributeTargets.Field)]
    public class SplineRollHandleAttribute : SplineDataHandleAttribute {}
}
#endif
