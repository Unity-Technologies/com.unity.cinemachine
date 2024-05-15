using System;
using UnityEngine;
using UnityEngine.Splines;

namespace Unity.Cinemachine
{
    /// <summary>
    /// Extension that can be added to a SplineContainer or a CinemachineCamera that uses a SplineContainer, for example a CinemachineCamera
    /// that has SplineDolly as Body component.
    /// - When CinemachineSplineRoll is added to a gameObject that has SplineContainer,
    /// then the roll affects any CinemachineCamera that reads that SplineContainer globally.
    /// - When CinemachineSplineRoll is added to a CinemachineCamera, then roll only affects that CinemachineCamera locally.
    /// </summary>
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [AddComponentMenu("Cinemachine/Helpers/Cinemachine Spline Roll")]
    [HelpURL(Documentation.BaseURL + "manual/CinemachineSplineRoll.html")]
    public class CinemachineSplineRoll : MonoBehaviour
    {
        /// <summary>Structure to hold roll value for a specific location on the track.</summary>
        [Serializable]
        public struct RollData
        {
            /// <summary>
            /// Roll (in degrees) around the forward direction for specific location on the track.
            /// When placed on a SplineContainer, this is going to be a global override that affects all vcams using the Spline.
            /// When placed on a CinemachineCamera, this is going to be a local override that only affects that CinemachineCamera.
            /// </summary>
            [Tooltip("Roll (in degrees) around the forward direction for specific location on the track.\n" +
                "- When placed on a SplineContainer, this is going to be a global override that affects all vcams using the Spline.\n" +
                "- When placed on a CinemachineCamera, this is going to be a local override that only affects that CinemachineCamera.")]
            public float Value;

            /// <summary> Implicit conversion to float </summary>
            /// <param name="roll"> The RollData setting to convert. </param>
            /// <returns> The value of the RollData setting. </returns>
            public static implicit operator float(RollData roll) => roll.Value;

            /// <summary> Implicit conversion from float </summary>
            /// <param name="roll"> The value with which to initialize the RollData setting. </param> 
            /// <returns>A new RollData setting with the given value. </returns>
            public static implicit operator RollData(float roll) => new () { Value = roll };
        }

        /// <summary>Interpolator for the RollData</summary>
        public struct LerpRollData : IInterpolator<RollData>
        {
            /// <inheritdoc/>
            public RollData Interpolate(RollData a, RollData b, float t) => new() { Value = Mathf.Lerp(a.Value, b.Value, t) };
        }

        /// <summary>
        /// Roll (in degrees) around the forward direction for specific location on the track.
        /// When placed on a SplineContainer, this is going to be a global override that affects all vcams using the Spline.
        /// When placed on a CinemachineCamera, this is going to be a local override that only affects that CinemachineCamera.
        /// </summary>
        [HideFoldout]
        public SplineData<RollData> Roll;

#if UNITY_EDITOR
        // Only needed for drawing the gizmo
        internal SplineContainer Spline
        {
            get 
            { 
                // In case behaviour was re-parented in the editor, we check every time
                if (TryGetComponent(out ISplineReferencer referencer))
                    return referencer.Spline;
                if (TryGetComponent(out SplineContainer spline))
                    return spline;
                return null;
            }
        }
#endif
        void OnEnable() {} // Needed so we can disable it in the editor

        /// <summary>Cache for clients that use CinemachineSplineRoll</summary>
        internal struct RollCache
        {
            CinemachineSplineRoll m_RollCache;

            public void Refresh(MonoBehaviour owner)
            {
                m_RollCache = null;

                // Check if owner has CinemachineSplineRoll
                if (!owner.TryGetComponent(out m_RollCache) && owner is ISplineReferencer referencer)
                {
                    // Check if the spline has CinemachineSplineRoll
                    var spline = referencer.Spline;
                    if (spline != null)
                        spline.TryGetComponent(out m_RollCache);
                }
            }

            public CinemachineSplineRoll GetSplineRoll(MonoBehaviour owner)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    Refresh(owner);
#endif
                return m_RollCache;
            }
        }
    }
}
