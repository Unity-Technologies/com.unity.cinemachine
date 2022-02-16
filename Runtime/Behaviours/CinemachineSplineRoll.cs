#if CINEMACHINE_UNITY_SPLINES
using System;
using UnityEngine;
using UnityEngine.Splines;

namespace Cinemachine
{
    /// <summary>
    /// Extension that can be added to a SplineContainer or a vcam that uses a SplineContainer, for example a vcam
    /// that has SplineDolly as Body component.
    /// - When CinemachineSplineRollExtension is added to a gameObject that has SplineContainer,
    /// then the roll affects any vcam that reads that SplineContainer globally.
    /// - When CinemachineSplineRoll is added to a vcam, then roll only affects that vcam locally.
    /// </summary>
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    public class CinemachineSplineRoll : MonoBehaviour
    {
        /// <summary>
        /// Roll (in angles) around the forward direction for specific location on the track.
        /// When placed on a SplineContainer, this is going to be a global override that affects all vcams using the Spline.
        /// When placed on a vcam, this is going to be a local override that only affects that vcam.
        /// </summary>
        [Tooltip("Roll (in angles) around the forward direction for specific location on the track.\n" +
            "- When placed on a SplineContainer, this is going to be a global override that affects all vcams using the Spline.\n" +
            "- When placed on a vcam, this is going to be a local override that only affects that vcam.")]
        [SplineRollHandle]
        public SplineData<float> RollOverride;

#if UNITY_EDITOR
        internal SplineContainer SplineContainer; // SplineRollHandle needs this for drawing the handles
#endif

        // Check if CinemachineSplineRoll is attached to a SplineContainer
        void Awake() => TryGetComponent(out SplineContainer);
        
        void OnEnable() {} // Needed, so we can disable it in the editor
    }
    
    [AttributeUsage(AttributeTargets.Field)]
    public class SplineRollHandleAttribute : SplineDataHandleAttribute {}
}
#endif
