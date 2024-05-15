using System;
using UnityEngine;
using UnityEngine.Splines;

namespace Unity.Cinemachine
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
    [AddComponentMenu("Cinemachine/Helpers/Cinemachine Spline Roll")]
    [HelpURL(Documentation.BaseURL + "manual/CinemachineSplineRoll.html")]
    public class CinemachineSplineRoll : MonoBehaviour
    {
        /// <summary>
        /// Roll (in degrees) around the forward direction for specific location on the track.
        /// When placed on a SplineContainer, this is going to be a global override that affects all vcams using the Spline.
        /// When placed on a vcam, this is going to be a local override that only affects that vcam.
        /// </summary>
        [HideFoldout]
        [Tooltip("Roll (in degrees) around the forward direction for specific location on the track.\n" +
            "- When placed on a SplineContainer, this is going to be a global override that affects all vcams using the Spline.\n" +
            "- When placed on a vcam, this is going to be a local override that only affects that vcam.")]
        public SplineData<float> Roll;

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
