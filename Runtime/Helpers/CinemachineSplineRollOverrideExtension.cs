#if CINEMACHINE_UNITY_SPLINES
using System;
using UnityEngine;
using UnityEngine.Splines;

namespace Cinemachine
{
    /// <summary>
    /// Extension that can be added to a SplineContainer or a vcam that uses a SplineContainer, for example a vcam
    /// that has SplineDolly as Body component.
    /// - When CinemachineSplineRollExtension is added to a gameObject that has SplineContainer, then any vcam that reads
    /// that SplineContainer will be affected by the CinemachineSplineRollExtension overrides. It works as a global
    /// override in this case.
    /// - When CinemachineSplineRollExtension is added to a vcam, then the overrides are local, they only affect that
    /// one vcam.
    /// </summary>
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    public class CinemachineSplineRollOverrideExtension : MonoBehaviour
    {
        /// <summary>
        /// Roll (in angles) along the forward direction for specific location on the track.
        /// When placed on a SplineContainer, this is going to be a global override that affects all vcams using the Spline.
        /// Whem placed on a vcam, this is going to be a local override that only affects that vcam.
        /// </summary>
        [Tooltip("Roll (in angles) along the forward direction for specific location on the track.\n" +
            "- When placed on a SplineContainer, this is going to be a global override that affects all vcams using the Spline.\n" +
            "- When placed on a vcam, this is going to be a local override that only affects that vcam.")]
        [RollHandle]
        public SplineData<float> RollOverride;

#if UNITY_EDITOR
        internal SplineContainer splineContainer; // this is needed by the RollHandle to work in the scene view
#endif

        /// <summary>
        /// RollHandle needs to know for which Spline to draw its handles.
        /// If SplineRollExtension is added to a GameObject with a SplineContainer, then use that as target.
        /// Otherwise check if we are on a vcam that uses a SplineContainer, then use that as target.
        /// </summary>
        void Awake()
        {
            if (
#if UNITY_EDITOR
                !TryGetComponent(out splineContainer) && // this is needed by the RollHandle to work in the scene view
#endif
                TryGetComponent(out CinemachineVirtualCamera vcam))
            {
                // set splineContainer through the vcam
                var body = vcam.GetCinemachineComponent(CinemachineCore.Stage.Body);
                if (body != null)
                {
                    var splineDataCacheUpdate = body.GetType().GetMethod("UpdateSplineRollOverrideCache",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (splineDataCacheUpdate != null)
                    {
                        splineDataCacheUpdate.Invoke(body, null);
                    }
                }
            }
        }
    }
    
    [AttributeUsage(AttributeTargets.Field)]
    public class RollHandleAttribute : SplineDataHandleAttribute {}
}
#endif
