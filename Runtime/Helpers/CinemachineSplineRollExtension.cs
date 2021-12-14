using System;
using Unity.Mathematics;
using UnityEngine;
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
        
        internal SplineContainer splineContainer; // this is used by Handle Drawer

        void OnEnable()
        {
            UpdateCache();
        }

        void OnDisable()
        {
            UpdateCache();
        }

        void UpdateCache()
        {
            Debug.Log("CinemachineSplineRollExtension->UpdateCache");
            if (splineContainer != null && splineContainer.Spline != null)
            {
                // trigger change event in spline hack to notify all vcams that might be using this splineContainer
                splineContainer.Spline[0] = splineContainer.Spline[0];
            }
            else if (splineContainer == null)
            {
                var vcam = GetComponent<CinemachineVirtualCamera>();
                if (vcam != null)
                {
                    var dolly = vcam.GetCinemachineComponent<CinemachineSplineDolly>();
                    if (dolly != null)
                    {
                        dolly.UpdateOverrideCache();
                    }
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
