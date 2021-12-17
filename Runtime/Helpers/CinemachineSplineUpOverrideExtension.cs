#if CINEMACHINE_UNITY_SPLINES
using System;
using UnityEngine;
using Unity.Mathematics;
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
    public class CinemachineSplineUpOverrideExtension : MonoBehaviour
    {
        /// <summary>
        /// Up vector overrides for specific location on the track. Vectors with magnitude 0 are replaced with Vector3.up.
        /// When placed on a SplineContainer, this is going to be a global override that affects all vcams using the Spline.
        /// Whem placed on a vcam, this is going to be a local override that only affects that vcam.
        /// </summary>
        [Tooltip("Up vector overrides for specific location on the track. Vectors with magnitude 0 are replaced with Vector3.up.\n" +
            "- When placed on a SplineContainer, this is going to be a global override that affects all vcams using the Spline.\n" +
            "- When placed on a vcam, this is going to be a local override that only affects that vcam.")]
        [TiltHandle]
        public SplineData<float3> UpOverride;

#if UNITY_EDITOR
        // this is used by TiltHandle Drawer
        internal SplineContainer splineContainer; 
#endif

        /// <summary>
        /// TiltHandle needs to know for which Spline to draw its handles.
        /// If SplineRollExtension is added to a GameObject with a SplineContainer, then use that as target.
        /// Otherwise check if we are on a vcam that uses a SplineContainer, then use that as target.
        /// </summary>
        void Awake()
        {
            if (
#if UNITY_EDITOR
                !TryGetComponent(out splineContainer) &&  
#endif
                TryGetComponent(out CinemachineVirtualCamera vcam))
            {
                // set splineContainer through the vcam
                var body = vcam.GetCinemachineComponent(CinemachineCore.Stage.Body);
                var splineDataCacheUpdate = body.GetType().GetMethod("UpdateOverrideCache",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (splineDataCacheUpdate != null)
                {
                    splineDataCacheUpdate.Invoke(body, null);
                }
            }
        }

        float3 m_Up = Vector3.up; // just to avoid cast and conversion
        void OnValidate()
        {
            if (UpOverride != null)
            {
                for (int i = 0; i < UpOverride.Count; ++i)
                {
                    var data = UpOverride[i];

                    // replace vectors with magnitude 0 with Vector3.up
                    if (math.lengthsq(data.Value) == 0)
                    {
                        data.Value = m_Up;
                        UpOverride[i] = data;
                    }
                }
            }
        }
    }
    
    [AttributeUsage(AttributeTargets.Field)]
    public class TiltHandleAttribute : SplineDataHandleAttribute {}
}
#endif
