using UnityEngine;
using System;
using Unity.Mathematics;
using UnityEngine.Splines;

namespace Unity.Cinemachine
{
    /// <summary>
    /// This behaviour can be attached to a GameObject with a SplineContainer.  
    /// It proivdes a function to apply smoothing to the spline.
    /// Smoothing auto-adjusts the knot tangents to maintain second-order smoothness of the spline, making it suitable
    /// for camera paths.
    /// 
    /// Smoothing is costly, because the entire path has to be considered when adjusting each knot.  
    /// 
    /// In Editor mode, an option is provided to automatically smooth the spline whenever it is modified.  
    /// In runtime mode, smoothing must be invoked manually by calling SmoothSplineNow().
    /// </summary>
    [ExecuteAlways]
    [AddComponentMenu("Cinemachine/Helpers/Cinemachine Spline Smoother")]
    [HelpURL(Documentation.BaseURL + "manual/CinemachineSplineSmoother.html")]
    [RequireComponent(typeof(SplineContainer))]
    public class CinemachineSplineSmoother : MonoBehaviour
    {
        /// <summary>
        /// If checked, the spline will be automatically smoothed whenever it is modified.
        /// </summary>
        [Tooltip("If checked, the spline will be automatically smoothed whenever it is modified (editor only).")]
        public bool AutoSmooth = true;

#if UNITY_EDITOR
        internal static Action<CinemachineSplineSmoother> OnEnableCallback;
        internal static Action<CinemachineSplineSmoother> OnDisableCallback;

        void OnEnable() => OnEnableCallback?.Invoke(this);
        void OnDisable() => OnDisableCallback?.Invoke(this);

        // Editor only, implements auto-smooth
        internal void OnSplineModified(Spline spline)
        {
            if (enabled && AutoSmooth && TryGetComponent(out SplineContainer container) && spline == container.Spline)
                SmoothSplineNow();
        }
#endif

        /// <summary>
        /// Apply smoothing to the spline.  
        /// Knot settings will be adjusted to produce second-order smoothness of the path, making it 
        /// suitable for use as a camera path.
        /// 
        /// This is an expensive operation.  Use with caution.
        /// </summary>
        public void SmoothSplineNow()
        {
            if (TryGetComponent<SplineContainer>(out var container) && container.Spline != null)
            {
                var spline = container.Spline;
                int numPoints = spline.Count;
                if (numPoints < 3)
                    return; // doesn't work if too few points

                float3[] p1 = new float3[numPoints];
                float3[] p2 = new float3[numPoints];
                float3[] knots = new float3[numPoints];
                for (int i = 0; i < numPoints; ++i)
                    knots[i] = spline[i].Position;
                if (spline.Closed)
                    SplineHelpers.ComputeSmoothControlPointsLooped(ref knots, ref p1, ref p2);
                else
                    SplineHelpers.ComputeSmoothControlPoints(ref knots, ref p1, ref p2);

                for (int i = 0; i < numPoints; i++)
                {
                    spline.SetTangentMode(i, TangentMode.Mirrored);
                    var knot = spline[i];

                    var up = math.mul(knot.Rotation, new float3(0, 1, 0));
                    var fwd = p1[i] - knots[i];
                    var back = p2[i > 0 ? i - 1 : numPoints - 1] - knots[i];
                    var len = (math.length(back) + math.length(fwd)) * 0.5f;

                    knot.Rotation = quaternion.LookRotationSafe(fwd, up);
                    knot.TangentIn =  (i == 0 && !spline.Closed) ? default : new float3(0, 0, -len);
                    knot.TangentOut = (i == numPoints - 1 && !spline.Closed) ? default : new float3(0, 0, len);
                    spline[i] = knot;
                }
            }
        }
    }
}
