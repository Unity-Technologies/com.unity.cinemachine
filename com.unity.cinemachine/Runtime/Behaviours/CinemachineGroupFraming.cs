﻿using UnityEngine;

namespace Unity.Cinemachine
{
    /// <summary>
    /// An add-on module for Cinemachine Camera that adjusts the framing if the tracking
    /// target implements ICinemachineTargetGroup.  
    /// 
    /// An attempt will be made to fit the entire target group within the specified framing.
    /// Camera position and/or rotation may be adjusted, depending on the settings.
    /// </summary>
    [AddComponentMenu("Cinemachine/Procedural/Extensions/Cinemachine Group Framing")]
    [ExecuteAlways]
    [SaveDuringPlay]
    [HelpURL(Documentation.BaseURL + "manual/CinemachineGroupFraming.html")]
    public class CinemachineGroupFraming : CinemachineExtension
    {
        /// <summary>What screen dimensions to consider when framing</summary>
        public enum FramingModes
        {
            /// <summary>Consider only the horizontal dimension.  Vertical framing is ignored.</summary>
            Horizontal,
            /// <summary>Consider only the vertical dimension.  Horizontal framing is ignored.</summary>
            Vertical,
            /// <summary>The larger of the horizontal and vertical dimensions will dominate, to get the best fit.</summary>
            HorizontalAndVertical
        };

        /// <summary>What screen dimensions to consider when framing</summary>
        [Tooltip("What screen dimensions to consider when framing.  Can be Horizontal, Vertical, or both")]
        public FramingModes FramingMode = FramingModes.HorizontalAndVertical;

        /// <summary>How much of the screen to fill with the bounding box of the targets.</summary>
        [Tooltip("The bounding box of the targets should occupy this amount of the screen space.  "
            + "1 means fill the whole screen.  0.5 means fill half the screen, etc.")]
        [RangeSlider(0, 2)]
        public float FramingSize = 0.8f;

        /// <summary>How aggressively the camera tries to frame the group.
        /// Small numbers are more responsive</summary>
        [RangeSlider(0, 20)]
        [Tooltip("How aggressively the camera tries to frame the group. Small numbers are more responsive, "
            + "rapidly adjusting the camera to keep the group in the frame.  Larger numbers give a heavier "
            + "more slowly responding camera.")]
        public float Damping = 2f;
        
        /// <summary>How to adjust the camera to get the desired framing size</summary>
        public enum SizeAdjustmentModes
        {
            /// <summary>Do not move the camera, only adjust the FOV.</summary>
            ZoomOnly,
            /// <summary>Just move the camera, don't change the FOV.</summary>
            DollyOnly,
            /// <summary>Move the camera as much as permitted by the ranges, then
            /// adjust the FOV if necessary to make the shot.</summary>
            DollyThenZoom
        };

        /// <summary>How to adjust the camera to get the desired framing</summary>
        [Tooltip("How to adjust the camera to get the desired framing size.  You can zoom, dolly in/out, or do both.")]
        public SizeAdjustmentModes SizeAdjustment = SizeAdjustmentModes.DollyThenZoom;

        /// <summary>How to adjust the camera to get the desired horizontal and vertical framing</summary>
        public enum LateralAdjustmentModes
        {
            /// <summary>Do not rotate the camera to reframe, only change the position.</summary>
            ChangePosition,
            /// <summary>Rotate the camera to reframe, do not change the position.</summary>
            ChangeRotation
        };

        /// <summary>How to adjust the camera to get the desired horizontal and vertical framing</summary>
        [Tooltip("How to adjust the camera to get the desired horizontal and vertical framing.")]
        public LateralAdjustmentModes LateralAdjustment = LateralAdjustmentModes.ChangePosition;
        
        /// <summary>Allowable FOV range, if adjusting FOV</summary>
        [Tooltip("Allowable FOV range, if adjusting FOV.")]
        [MinMaxRangeSlider(1, 179)]
        public Vector2 FovRange = new Vector2(1, 100);

        /// <summary>Allowable range for the camera to move. 0 is the undollied position.  
        /// Negative values move the camera closer to the target.</summary>
        [Tooltip("Allowable range for the camera to move.  0 is the undollied position.  "
            + "Negative values move the camera closer to the target.")]
        [Vector2AsRange]
        public Vector2 DollyRange = new Vector2(-100, 100);

        /// <summary>Allowable orthographic size range, if adjusting orthographic size</summary>
        [Tooltip("Allowable orthographic size range, if adjusting orthographic size.")]
        [Vector2AsRange]
        public Vector2 OrthoSizeRange = new Vector2(1, 1000);

        const float k_MinimumGroupSize = 0.01f;

        void OnValidate()
        {
            FramingSize = Mathf.Max(k_MinimumGroupSize, FramingSize);
            Damping = Mathf.Max(0, Damping);
            DollyRange.y = Mathf.Max(DollyRange.x, DollyRange.y);
            FovRange.x = Mathf.Clamp(FovRange.x, 1, 179);
            FovRange.y = Mathf.Max(FovRange.x, FovRange.y);
            FovRange.y = Mathf.Clamp(FovRange.y, 1, 179);
            OrthoSizeRange.x = Mathf.Max(0.01f, OrthoSizeRange.x);
            OrthoSizeRange.y = Mathf.Max(OrthoSizeRange.x, OrthoSizeRange.y);
        }

        void Reset()
        {
            FramingMode = FramingModes.HorizontalAndVertical;
            SizeAdjustment = SizeAdjustmentModes.DollyThenZoom;
            LateralAdjustment = LateralAdjustmentModes.ChangePosition;
            FramingSize = 0.8f;
            Damping = 2;
            DollyRange = new Vector2(-100, 100);
            FovRange = new Vector2(1, 100);
            OrthoSizeRange = new Vector2(1, 1000);
        }

        /// <summary>For editor visualization of the calculated bounding box of the group</summary>
        internal Bounds GroupBounds;

        /// <summary>For editor visualization of the calculated bounding box of the group</summary>
        internal Matrix4x4 GroupBoundsMatrix;

        class VcamExtraState : VcamExtraStateBase
        {
            public Vector3 PosAdjustment;
            public Vector2 RotAdjustment;
            public float FovAdjustment;

            public void Reset()
            {
                PosAdjustment = Vector3.zero;
                RotAdjustment = Vector2.zero;
                FovAdjustment = 0;
            }
        };

        /// <summary>
        /// Report maximum damping time needed for this extension.
        /// Only used in editor for timeline scrubbing.
        /// </summary>
        /// <returns>Highest damping setting in this extension</returns>
        public override float GetMaxDampTime() => Damping;

        /// <summary>Callback to tweak the settings</summary>
        /// <param name="vcam">The virtual camera being processed</param>
        /// <param name="stage">The current pipeline stage</param>
        /// <param name="state">The current virtual camera state</param>
        /// <param name="deltaTime">The current applicable deltaTime</param>
        protected override void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            // We have to do it after both Body and Aim, and the only way to ensure that is to
            // do it after noise (because body and aim can be inverted).
            // We ignore the noise effect anyway, so it doesn't hurt.
            if (stage != CinemachineCore.Stage.Noise)
                return;
            
            var group = vcam.LookAtTargetAsGroup;
            group ??= vcam.FollowTargetAsGroup;
            if (group == null)
                return;

            var extra = GetExtraState<VcamExtraState>(vcam);
            if (!vcam.PreviousStateIsValid)
                extra.Reset();

            if (state.Lens.Orthographic)
                OrthoFraming(vcam, group, extra, ref state, deltaTime);
            else
                PerspectiveFraming(vcam, group, extra, ref state, deltaTime);
        }

        void OrthoFraming(
            CinemachineVirtualCameraBase vcam, ICinemachineTargetGroup group, 
            VcamExtraState extra, ref CameraState state, float deltaTime)
        {
            var damping = vcam.PreviousStateIsValid && deltaTime >= 0 ? Damping : 0;

            // Position adjustment: work in camera-local coords
            GroupBoundsMatrix = Matrix4x4.TRS(state.RawPosition, state.RawOrientation, Vector3.one);
            GroupBounds = group.GetViewSpaceBoundingBox(GroupBoundsMatrix, true);
            var camPos = GroupBounds.center; 
            camPos.z = 0; // don't change the camera's distance from the group

            extra.PosAdjustment += vcam.DetachedFollowTargetDamp(camPos - extra.PosAdjustment, damping, deltaTime);
            state.RawPosition += state.RawOrientation * extra.PosAdjustment;
            
            // Ortho size adjustment
            var targetHeight = GetFrameHeight(GroupBounds.size / FramingSize, state.Lens.Aspect) * 0.5f;
            targetHeight = Mathf.Clamp(targetHeight, OrthoSizeRange.x, OrthoSizeRange.y);

            var lens = state.Lens;
            var deltaFov = targetHeight - lens.OrthographicSize;
            extra.FovAdjustment += vcam.DetachedFollowTargetDamp(deltaFov - extra.FovAdjustment, damping, deltaTime);
            lens.OrthographicSize += extra.FovAdjustment;
            state.Lens = lens;
        }

        void PerspectiveFraming(
            CinemachineVirtualCameraBase vcam, ICinemachineTargetGroup group, 
            VcamExtraState extra, ref CameraState state, float deltaTime)
        {
            var damping = vcam.PreviousStateIsValid && deltaTime >= 0 ? Damping : 0;

            var camPos = state.RawPosition;
            var camRot = state.RawOrientation;
            var up = camRot * Vector3.up;
            var fov = state.Lens.FieldOfView;

            // Get a naive bounds for the group, and pull the camera out as far as we can
            // to see as many members as possible.  Group members behind the camera will be ignored.
            var canDollyOut = SizeAdjustment != SizeAdjustmentModes.ZoomOnly;
            var dollyRange = canDollyOut ? DollyRange : Vector2.zero;
            var m = Matrix4x4.TRS(camPos, camRot, Vector3.one);
            var b = group.GetViewSpaceBoundingBox(m, canDollyOut);
            
            var moveCamera = LateralAdjustment == LateralAdjustmentModes.ChangePosition;
            if (!moveCamera)
            {
                // Set up the initial rotation
                var fwd = m.MultiplyPoint3x4(b.center) - camPos;
                if (!Vector3.Cross(fwd, up).AlmostZero())
                    camRot = Quaternion.LookRotation(fwd, up);
            }
            const float slush = 5;  // avoid the members getting too close to the camera
            var dollyAmount = Mathf.Clamp(Mathf.Min(0, b.center.z) - b.extents.z - slush, dollyRange.x, dollyRange.y);
            camPos += camRot * new Vector3(0, 0, dollyAmount);

            // Approximate looking at the group center, then correct for actual center
            ComputeCameraViewGroupBounds(group, ref camPos, ref camRot, moveCamera);

            AdjustSize(group, state.Lens.Aspect, ref camPos, ref camRot, ref fov, ref dollyAmount);

            // Apply the adjustments
            var lens = state.Lens;
            var deltaFov = fov - lens.FieldOfView;
            extra.FovAdjustment += vcam.DetachedFollowTargetDamp(deltaFov - extra.FovAdjustment, damping, deltaTime);
            lens.FieldOfView += extra.FovAdjustment;
            state.Lens = lens;

            var deltaRot = state.RawOrientation.GetCameraRotationToTarget(camRot * Vector3.forward, up);
            extra.RotAdjustment.x += vcam.DetachedFollowTargetDamp(deltaRot.x - extra.RotAdjustment.x, damping, deltaTime);
            extra.RotAdjustment.y += vcam.DetachedFollowTargetDamp(deltaRot.y - extra.RotAdjustment.y, damping, deltaTime);
            state.OrientationCorrection = state.OrientationCorrection * Quaternion.identity.ApplyCameraRotation(extra.RotAdjustment, up);

            var deltaPos = Quaternion.Inverse(state.RawOrientation) * (camPos - state.RawPosition);
            extra.PosAdjustment += vcam.DetachedFollowTargetDamp(deltaPos - extra.PosAdjustment, damping, deltaTime);
            state.PositionCorrection += state.RawOrientation * extra.PosAdjustment;
        }

        void AdjustSize(
            ICinemachineTargetGroup group, float aspect,
            ref Vector3 camPos, ref Quaternion camRot, ref float fov, ref float dollyAmount)
        {
            // Dolly mode: Adjust camera distance
            if (SizeAdjustment != SizeAdjustmentModes.ZoomOnly)
            {
                // What distance from near edge would be needed to get the desired frame height, at the current FOV
                var frameHeight = GetFrameHeight(GroupBounds.size / FramingSize, aspect);
                var currentDistance = GroupBounds.center.z - GroupBounds.extents.z;
                var desiredDistance = frameHeight / (2f * Mathf.Tan(fov * Mathf.Deg2Rad / 2f));
                float dolly = currentDistance - desiredDistance;

                // Clamp to respect min/max camera movement
                dolly = Mathf.Clamp(dolly + dollyAmount, DollyRange.x, DollyRange.y) - dollyAmount;
                dollyAmount += dolly;

                // Because moving the camera affects the view space bounds, we recompute after movement
                camPos += camRot * new Vector3(0, 0, dolly);
                ComputeCameraViewGroupBounds(group, ref camPos, ref camRot, true);
            }
            // Zoom mode: Adjust lens 
            if (SizeAdjustment != SizeAdjustmentModes.DollyOnly)
            {
                var frameHeight = GetFrameHeight(GroupBounds.size / FramingSize, aspect);
                var distance = GroupBounds.center.z - GroupBounds.extents.z;
                if (distance > Epsilon)
                    fov = 2f * Mathf.Atan(frameHeight / (2 * distance)) * Mathf.Rad2Deg;
                fov = Mathf.Clamp(fov, FovRange.x, FovRange.y);
            }
        }

        /// <summary>Computes GroupBoundsMatrix and GroupBounds</summary>
        void ComputeCameraViewGroupBounds(
            ICinemachineTargetGroup group, ref Vector3 camPos, ref Quaternion camRot, bool moveCamera)
        {
            GroupBoundsMatrix = Matrix4x4.TRS(camPos, camRot, Vector3.one);

            // Initial naive approximation
            if (moveCamera)
            {
                GroupBounds = group.GetViewSpaceBoundingBox(GroupBoundsMatrix, false);
                var pos = GroupBounds.center; pos.z = 0;
                camPos = GroupBoundsMatrix.MultiplyPoint3x4(pos);
                GroupBoundsMatrix = Matrix4x4.TRS(camPos, camRot, Vector3.one);
            }

            group.GetViewSpaceAngularBounds(GroupBoundsMatrix, out var minAngles, out var maxAngles, out var zRange);
            var shift = (minAngles + maxAngles) / 2;
            var adjustment = Quaternion.identity.ApplyCameraRotation(shift, Vector3.up);

            if (moveCamera)
            {
                // We shift only in the camera XY plane - there is no Z movement.
                // The result is approximate - accuracy drops when there are big z differences in members.
                // This could be improved with multiple iterations, but it's not worth it.
                var dir = adjustment * Vector3.forward;
                new Plane(Vector3.forward, new Vector3(0, 0, zRange.x)).Raycast(new Ray(Vector3.zero, dir), out var t);
                camPos = dir * t; camPos.z = 0;
                camPos = GroupBoundsMatrix.MultiplyPoint3x4(camPos);
                GroupBoundsMatrix.SetColumn(3, camPos);

                // Account for parallax: recompute bounds after shifting position.
                group.GetViewSpaceAngularBounds(GroupBoundsMatrix, out minAngles, out maxAngles, out zRange);
            }
            else
            {
                // Rotate to look at center - no parallax shift to worry about
                camRot = camRot * adjustment;
                GroupBoundsMatrix = Matrix4x4.TRS(camPos, camRot, Vector3.one);
                minAngles -= shift;
                maxAngles -= shift;
            }

            // For width and height (in camera space) of the bounding box, we use the values
            // at the near end of the box. The gizmo drawer will take this into account
            // when displaying the frustum bounds of the group
            Vector2 angles = new Vector2(89.5f, 89.5f);
            if (zRange.x > 0)
            {
                angles = Vector2.Max(maxAngles, UnityVectorExtensions.Abs(minAngles));
                angles = Vector2.Min(angles, new Vector2(89.5f, 89.5f));
            }
            var twiceNear = zRange.x * 2;
            angles *= Mathf.Deg2Rad;
            GroupBounds =  new Bounds(
                new Vector3(0, 0, (zRange.x + zRange.y) * 0.5f),
                new Vector3(Mathf.Tan(angles.y) * twiceNear, Mathf.Tan(angles.x) * twiceNear, zRange.y - zRange.x));
        }

        float GetFrameHeight(Vector2 boundsSize, float aspect)
        {
            float h;
            switch (FramingMode)
            {
                case FramingModes.Horizontal: h = Mathf.Max(Epsilon, boundsSize.x) / aspect; break;
                case FramingModes.Vertical: h = Mathf.Max(Epsilon, boundsSize.y); break;
                default:
                case FramingModes.HorizontalAndVertical:
                    h = Mathf.Max(Mathf.Max(Epsilon, boundsSize.x) / aspect, Mathf.Max(Epsilon, boundsSize.y));
                    break;
            }
            return Mathf.Max(h, k_MinimumGroupSize);
        }
    }
}
