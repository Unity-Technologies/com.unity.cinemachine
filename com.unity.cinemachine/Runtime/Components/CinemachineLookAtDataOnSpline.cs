using System;
using UnityEngine;
using UnityEngine.Splines;

namespace Unity.Cinemachine
{
    /// <summary>
    /// CinemachineLookAtDataOnSpline is a component that allows the camera to look at 
    /// specific points in the world as it moves along a spline.
    /// </summary>
    [ExecuteAlways, SaveDuringPlay]
    [CameraPipeline(CinemachineCore.Stage.Aim)]
    [AddComponentMenu("Cinemachine/Procedural/Rotation Control/Cinemachine Look At Data On Spline")]
    [DisallowMultipleComponent]
    [HelpURL(Documentation.BaseURL + "manual/CinemachineLookAtDataOnSpline.html")]
    public class CinemachineLookAtDataOnSpline : CinemachineComponentBase
    {
        /// <summary>LookAt targets for the camera at specific points on the Spline</summary>
        [Serializable]
        public struct Item
        {
            /// <summary>The worldspace point to look at</summary>
            [Tooltip("The point that the camera should look at, in world space co-ordinates.")]
            public Vector3 LookAtPoint;
        
            /// <summary>Easing value for the Bezier curve. 0 is linear, 1 is smooth.</summary>
            [Tooltip("Controls how to ease in and out of this data point.  A value of 0 will linearly interpolate between "
                + "LookAt points, while a value of 1 will slow down and briefly pause the rotation to look at the target.")]
            [Range(0, 1)]
            public float Easing;
        }

        /// <summary>Interpolator for the LookAtData</summary>
        internal struct LerpRotation : IInterpolator<Item>
        {
            public Item Interpolate(Item a, Item b, float t)
            {
                var p1 = Vector3.Lerp(Vector3.Lerp(a.LookAtPoint, b.LookAtPoint, 0.33f), a.LookAtPoint, a.Easing);
                var p2 = Vector3.Lerp(Vector3.Lerp(b.LookAtPoint, a.LookAtPoint, 0.33f), b.LookAtPoint, b.Easing);
                return new Item
                {
                    LookAtPoint = SplineHelpers.Bezier3(t, a.LookAtPoint, p1, p2, b.LookAtPoint),
                    Easing = Mathf.Lerp(a.Easing, b.Easing, t)
                };
            }
        }

        /// <summary>LookAt targets for the camera at specific points on the Spline</summary>
        [Tooltip("LookAt targets for the camera at specific points on the Spline")]
        public SplineData<Item> LookAtData = new () { DefaultValue = new Item { Easing = 1 } };

        void Reset() => LookAtData = new SplineData<Item> { DefaultValue = new Item { Easing = 1 } };

        /// <inheritdoc/>
        public override bool IsValid => enabled && LookAtData != null && GetTargets(out _, out _);

        /// <inheritdoc/>
        public override CinemachineCore.Stage Stage => CinemachineCore.Stage.Aim;

        /// <inheritdoc/>
        public override void MutateCameraState(ref CameraState state, float deltaTime)
        {
            if (!GetTargets(out var spline, out var dolly))
                return;

            var splinePath = spline.Spline;
            if (splinePath == null || splinePath.Count == 0)
                return;

            var item = LookAtData.Evaluate(splinePath, dolly.CameraPosition, dolly.PositionUnits, new LerpRotation());
            var dir = item.LookAtPoint - state.RawPosition;
            if (dir.sqrMagnitude > UnityVectorExtensions.Epsilon)
            {
                var up = state.ReferenceUp;
                if (Vector3.Cross(dir, up).sqrMagnitude < UnityVectorExtensions.Epsilon)
                {
                    // Look direction is parallel to the up vector
                    up = state.RawOrientation * Vector3.back;
                    if (Vector3.Cross(dir, up).sqrMagnitude < UnityVectorExtensions.Epsilon)
                        up = state.RawOrientation * Vector3.left;
                }
                state.RawOrientation = Quaternion.LookRotation(dir, up);
            }
            state.ReferenceLookAt = item.LookAtPoint;
        }

        /// <summary>
        /// API for the inspector: Get the spline and the required CinemachineTrackDolly component.
        /// </summary>
        /// <param name="spline">The spline being augmented</param>
        /// <param name="dolly">The associated CinemachineTrackDolly component</param>
        /// <returns></returns>
        internal bool GetTargets(out SplineContainer spline, out CinemachineSplineDolly dolly)
        {
            if (TryGetComponent(out dolly))
            {
                spline = dolly.Spline;
                return spline != null && spline.Spline != null;
            }
            spline = null;
            return false;
        }
    }
}
