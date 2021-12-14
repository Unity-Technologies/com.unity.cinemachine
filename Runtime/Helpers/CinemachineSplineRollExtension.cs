using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace Cinemachine
{
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
        
        internal SplineContainer splineContainer;

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
                
                // TODO: notify spline or vcam about change!
            }

            var splineContainer = GetComponent<SplineContainer>();
        }
    }
    
    [AttributeUsage(AttributeTargets.Field)]
    public class TiltHandleAttribute : SplineDataHandleAttribute {}
}
