using UnityEngine;
using Cinemachine.Utility;

namespace Cinemachine
{
    /// <summary>
    /// This is a CinemachineComponent in the Aim section of the component pipeline.
    /// Its job is to aim the camera at a target object, with configurable offsets, damping, 
    /// and composition rules.
    /// 
    /// In addition, if the target is a CinemachineTargetGroup, the behaviour
    /// will adjust the FOV and the camera distance to ensure that the entire group of targets
    /// is framed properly.
    /// </summary>
    [DocumentationSorting(DocumentationSortingAttribute.Level.UserRef)]
    [AddComponentMenu("")] // Don't display in add component menu
    [RequireComponent(typeof(CinemachinePipeline))]
    [SaveDuringPlay]
    public class CinemachineGroupComposer : CinemachineComposer
    {
        /// <summary>How much of the screen to fill with the bounding box of the targets.</summary>
        [Space]
        [Tooltip("The bounding box of the targets should occupy this amount of the screen space.  1 means fill the whole screen.  0.5 means fill half the screen, etc.")]
        public float m_GroupFramingSize = 0.8f;

        /// <summary>What screen dimensions to consider when framing</summary>
        [DocumentationSorting(DocumentationSortingAttribute.Level.UserRef)]
        public enum FramingMode
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
        public FramingMode m_FramingMode = FramingMode.HorizontalAndVertical;

        /// <summary>How aggressively the camera tries to frame the group.
        /// Small numbers are more responsive</summary>
        [Range(0, 20)]
        [Tooltip("How aggressively the camera tries to frame the group. Small numbers are more responsive, rapidly adjusting the camera to keep the group in the frame.  Larger numbers give a more heavy slowly responding camera.")]
        public float m_FrameDamping = 2f;

        /// <summary>How to adjust the camera to get the desired framing</summary>
        public enum AdjustmentMode
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
        [Tooltip("How to adjust the camera to get the desired framing.  You can zoom, dolly in/out, or do both.")]
        public AdjustmentMode m_AdjustmentMode = AdjustmentMode.DollyThenZoom;

        /// <summary>How much closer to the target can the camera go?</summary>
        [Tooltip("The maximum distance toward the target that this behaviour is allowed to move the camera.")]
        public float m_MaxDollyIn = 5000f;

        /// <summary>How much farther from the target can the camera go?</summary>
        [Tooltip("The maximum distance away the target that this behaviour is allowed to move the camera.")]
        public float m_MaxDollyOut = 5000f;

        /// <summary>Set this to limit how close to the target the camera can get</summary>
        [Tooltip("Set this to limit how close to the target the camera can get.")]
        public float m_MinimumDistance = 1;

        /// <summary>Set this to limit how far from the taregt the camera can get</summary>
        [Tooltip("Set this to limit how far from the target the camera can get.")]
        public float m_MaximumDistance = 5000f;

        /// <summary>If adjusting FOV, will not set the FOV lower than this</summary>
        [Range(1, 179)]
        [Tooltip("If adjusting FOV, will not set the FOV lower than this.")]
        public float m_MinimumFOV = 3;

        /// <summary>If adjusting FOV, will not set the FOV higher than this</summary>
        [Range(1, 179)]
        [Tooltip("If adjusting FOV, will not set the FOV higher than this.")]
        public float m_MaximumFOV = 60;

        /// <summary>If adjusting Orthographic Size, will not set it lower than this</summary>
        [Tooltip("If adjusting Orthographic Size, will not set it lower than this.")]
        public float m_MinimumOrthoSize = 1;

        /// <summary>If adjusting Orthographic Size, will not set it higher than this</summary>
        [Tooltip("If adjusting Orthographic Size, will not set it higher than this.")]
        public float m_MaximumOrthoSize = 100;

        private void OnValidate()
        {
            m_GroupFramingSize = Mathf.Max(0.001f, m_GroupFramingSize);
            m_MaxDollyIn = Mathf.Max(0, m_MaxDollyIn);
            m_MaxDollyOut = Mathf.Max(0, m_MaxDollyOut);
            m_MinimumDistance = Mathf.Max(0, m_MinimumDistance);
            m_MaximumDistance = Mathf.Max(m_MinimumDistance, m_MaximumDistance);
            m_MinimumFOV = Mathf.Max(1, m_MinimumFOV);
            m_MaximumFOV = Mathf.Clamp(m_MaximumFOV, m_MinimumFOV, 179);
            m_MinimumOrthoSize = Mathf.Max(0.01f, m_MinimumOrthoSize);
            m_MaximumOrthoSize = Mathf.Max(m_MinimumOrthoSize, m_MaximumOrthoSize);
        }
        
        /// <summary>Applies the composer rules and orients the camera accordingly</summary>
        /// <param name="curState">The current camera state</param>
        /// <param name="deltaTime">Used for calculating damping.  If less than
        /// zero, then target will snap to the center of the dead zone.</param>
        public override void MutateCameraState(ref CameraState curState, float deltaTime)
        {
            // Can't do anything without a group to look at
            CinemachineTargetGroup group = LookAtTargetGroup;
            if (group == null)
            {
                base.MutateCameraState(ref curState, deltaTime);
                return;
            }

            if (!IsValid || !curState.HasLookAt)
            {
                m_prevTargetHeight = 0;
                m_prevCameraOffset = Vector3.zero;
                return;
            }

            bool canMoveCamera 
                = !curState.Lens.Orthographic && m_AdjustmentMode != AdjustmentMode.ZoomOnly;

            // Get the bounding box from camera's POV in view space
            Vector3 observerPosition = curState.RawPosition;
            BoundingSphere s = group.Sphere;
            Vector3 groupCenter = s.position;
            Vector3 currentOffset = groupCenter - observerPosition;
            float currentDistance = currentOffset.magnitude;
            if (currentDistance < Epsilon)
                return;  // navel-gazing, get outa here

            Vector3 fwd = currentOffset / currentDistance;
            LastBoundsMatrix = Matrix4x4.TRS(observerPosition, 
                    Quaternion.LookRotation(fwd, curState.ReferenceUp), Vector3.one);
            Bounds b;
            if (curState.Lens.Orthographic)
            {
                b = group.GetViewSpaceBoundingBox(LastBoundsMatrix);
                Vector3 sizeDelta = new Vector3(b.center.x, b.center.y, 0);
                b.size += sizeDelta.Abs() * 2;
                b.center = new Vector3(0, 0, b.center.z);
                LastBounds = b;
            }
            else
            {
                if (canMoveCamera)
                {
                    // Get an upper bound on the distance
                    b = group.GetViewSpaceBoundingBox(LastBoundsMatrix);
                    groupCenter = LastBoundsMatrix.MultiplyPoint3x4(b.center);

                    // Now try to get closer
                    float distance = GetTargetHeight(b) 
                        / (2f * Mathf.Tan(curState.Lens.FieldOfView * Mathf.Deg2Rad / 2f));
                    Vector3 nearCenter = b.center; nearCenter.z -= b.extents.z;
                    nearCenter = LastBoundsMatrix.MultiplyPoint3x4(nearCenter);
                    Vector3 newFwd = (groupCenter - nearCenter).normalized;
                    if (!newFwd.AlmostZero())
                        fwd = newFwd;
                    observerPosition = nearCenter - (fwd * distance);
                    LastBoundsMatrix = Matrix4x4.TRS(observerPosition, 
                            Quaternion.LookRotation(fwd, curState.ReferenceUp), Vector3.one);
                }

                b = GetScreenSpaceGroupBoundingBox(group, LastBoundsMatrix, out fwd);
                LastBoundsMatrix = Matrix4x4.TRS(observerPosition, 
                        Quaternion.LookRotation(fwd, curState.ReferenceUp), Vector3.one);
                LastBounds = b;
                groupCenter = LastBoundsMatrix.MultiplyPoint3x4(b.center);
                currentOffset = groupCenter - curState.RawPosition;
                currentDistance = currentOffset.magnitude;
            }

            // Adjust bounds for framing size
            Vector3 extents = b.extents / m_GroupFramingSize;
            extents.z = Mathf.Min(b.extents.z, extents.z);
            b.extents = extents;

            // Apply damping
            float targetHeight = GetTargetHeight(b);
            if (deltaTime >= 0)
            {
                float delta = targetHeight - m_prevTargetHeight;
                delta = Damper.Damp(delta, m_FrameDamping, deltaTime);
                targetHeight = m_prevTargetHeight + delta;
            }
            m_prevTargetHeight = targetHeight;

            // Move the camera
            if (canMoveCamera)
            {
                // What distance would be needed to get the target height, at the current FOV
                float depth = b.extents.z;
                float d = (groupCenter - observerPosition).magnitude;
                if (d > Epsilon * 10)
                {
                    float nearTargetHeight = targetHeight * (d - depth) / d;
                    float targetDistance = nearTargetHeight 
                        / (2f * Mathf.Tan(curState.Lens.FieldOfView * Mathf.Deg2Rad / 2f));

                    // Clamp to respect min/max distance settings to the near surface of the bounds
                    float cameraDistance = targetDistance;
                    cameraDistance = Mathf.Clamp(cameraDistance, currentDistance - m_MaxDollyIn, currentDistance + m_MaxDollyOut);
                    cameraDistance -= depth;
                    cameraDistance = Mathf.Clamp(cameraDistance, m_MinimumDistance, m_MaximumDistance);
                    cameraDistance += depth;

                    // Apply
                    Vector3 newCamOffset 
                        = (groupCenter - (fwd * (cameraDistance + depth))) - curState.RawPosition;
                    if (deltaTime >= 0)
                    {
                        Vector3 delta = newCamOffset - m_prevCameraOffset;
                        delta = Damper.Damp(delta, m_FrameDamping, deltaTime);
                        newCamOffset = m_prevCameraOffset + delta;
                    }
                    m_prevCameraOffset = newCamOffset;
                    curState.PositionCorrection += newCamOffset;
                }
            }
            // Apply zoom
            if (curState.Lens.Orthographic || m_AdjustmentMode != AdjustmentMode.DollyOnly)
            {
                float nearBoundsDistance = (groupCenter - curState.CorrectedPosition).magnitude;
                float currentFOV = 179;
                if (nearBoundsDistance > Epsilon)
                    currentFOV = 2f * Mathf.Atan(targetHeight / (2 * nearBoundsDistance)) * Mathf.Rad2Deg;

                LensSettings lens = curState.Lens;
                lens.FieldOfView = Mathf.Clamp(currentFOV, m_MinimumFOV, m_MaximumFOV);
                lens.OrthographicSize = Mathf.Clamp(targetHeight / 2, m_MinimumOrthoSize, m_MaximumOrthoSize);
                curState.Lens = lens;
            }

            // Now compose normally
            curState.ReferenceLookAt = GetLookAtPointAndSetTrackedPoint(groupCenter);
            base.MutateCameraState(ref curState, deltaTime);
        }

        // State for damping
        float m_prevTargetHeight; 
        Vector3 m_prevCameraOffset = Vector3.zero;

        /// <summary>For editor visulaization of the calculated bounding box of the group</summary>
        public Bounds LastBounds { get; private set; }

        /// <summary>For editor visualization of the calculated bounding box of the group</summary>
        public Matrix4x4 LastBoundsMatrix { get; private set; }

        float GetTargetHeight(Bounds b)
        {
            switch (m_FramingMode)
            {
                case FramingMode.Horizontal:
                    return Mathf.Max(Epsilon, b.size.x ) / VcamState.Lens.Aspect;
                case FramingMode.Vertical:
                    return Mathf.Max(Epsilon, b.size.y);
                default:
                case FramingMode.HorizontalAndVertical:
                    return Mathf.Max(
                        Mathf.Max(Epsilon, b.size.x) / VcamState.Lens.Aspect, 
                        Mathf.Max(Epsilon, b.size.y));
            }
        }

        /// <param name="observer">Point of view</param>
        /// <param name="newFwd">New forward direction to use when interpreting the return value</param>
        /// <returns>Bounding box in a slightly rotated version of observer, as specified by newFwd</returns>
        static Bounds GetScreenSpaceGroupBoundingBox(
            CinemachineTargetGroup group, Matrix4x4 observer, out Vector3 newFwd)
        {
            Vector2 minAngles, maxAngles, zRange;
            group.GetViewSpaceAngularBounds(observer, out minAngles, out maxAngles, out zRange);
            Vector2 shift = (minAngles + maxAngles) / 2;

            newFwd = Quaternion.identity.ApplyCameraRotation(shift, Vector3.up) * Vector3.forward;
            newFwd = observer.MultiplyVector(newFwd);

            float d = (zRange.y + zRange.x);
            Vector2 angles = (maxAngles - shift) * Mathf.Deg2Rad;
            angles = Vector2.Min(angles, new Vector2(89.5f, 89.5f));
            return new Bounds(
                new Vector3(0, 0, d/2), 
                new Vector3(Mathf.Tan(angles.y) * d, Mathf.Tan(angles.x) * d, zRange.y - zRange.x));
        }
    }
}
