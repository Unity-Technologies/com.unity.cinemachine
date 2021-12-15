#if CINEMACHINE_UNITY_SPLINES
using System;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Splines;

namespace Cinemachine
{
    [ExecuteInEditMode] // for onEnable, onDisable
    [DisallowMultipleComponent]
    public class CinemachineSplineRollExtension : MonoBehaviour
    {
        /// <summary>
        /// Tilt value overrides for specific location on the track. Vectors with magnitude 0 are replaced with m_DefaultTilt.
        /// When placed on a SplineContainer, this is going to be a global override that affects all vcams using the Spline.
        /// Whem placed on a vcam, this is going to be a local override that only affects that vcam.
        /// </summary>
        [Tooltip("Roll value overrides for specific location on the track. Vectors with magnitude 0 are replaced with Vector3.up.\n" +
            "- When placed on a SplineContainer, this is going to be a global override that affects all vcams using the Spline.\n" +
            "- When placed on a vcam, this is going to be a local override that only affects that vcam.")]
        [TiltHandle]
        public SplineData<float3> RollOverride;

#if UNITY_EDITOR
        // this is used by TiltHandle Drawer
        internal SplineContainer splineContainer; 
#endif

        void Awake()
        {
            UpdateCache();
        }

        /// <summary>
        /// If SplineRollExtension is on a GameObject with a SplineContainer, then use that as target.
        /// Otherwise check if we are on a vcam that uses a SplineContainer, then use that as target.
        /// </summary>
        void UpdateCache()
        {
            if (
#if UNITY_EDITOR
                !TryGetComponent(out splineContainer) &&  
#endif
                TryGetComponent(out CinemachineVirtualCamera vcam))
            {
                var body = vcam.GetCinemachineComponent(CinemachineCore.Stage.Body);
                var splineDataCacheUpdate = body.GetType().GetMethod("UpdateOverrideCache",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (splineDataCacheUpdate != null)
                {
                    splineDataCacheUpdate.Invoke(body, null);
                }
            }
        }


        float3 m_Up = Vector3.up;
        void OnValidate()
        {
            if (RollOverride != null)
            {
                for (int i = 0; i < RollOverride.Count; ++i)
                {
                    var data = RollOverride[i];

                    // replace vectors with magnitude 0 with Vector3.up
                    if (math.length(data.Value) == 0)
                    {
                        data.Value = m_Up;
                        RollOverride[i] = data;
                    }
                }
            }
        }
    }
    
    [AttributeUsage(AttributeTargets.Field)]
    public class TiltHandleAttribute : SplineDataHandleAttribute {}
}
#endif
