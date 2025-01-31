using UnityEngine;
using System;
using UnityEngine.Splines;
using UnityEngine.Serialization;

namespace Unity.Cinemachine
{
    /// <summary>
    /// A Cinemachine Camera Body component that constrains camera motion to a Spline.
    /// The camera can move along the spline.
    ///
    /// This behaviour can operate in two modes: manual positioning, and Auto-Dolly positioning.
    /// In Manual mode, the camera's position is specified by animating the Spline Position field.
    /// In Auto-Dolly mode, the Spline Position field is animated automatically every frame by finding
    /// the position on the spline that's closest to the camera's tracking target.
    /// </summary>
    [AddComponentMenu("Cinemachine/Procedural/Position Control/Cinemachine Spline Dolly")]
    [SaveDuringPlay]
    [DisallowMultipleComponent]
    [CameraPipeline(CinemachineCore.Stage.Body)]
    [HelpURL(Documentation.BaseURL + "manual/CinemachineSplineDolly.html")]
    public class CinemachineSplineDolly : CinemachineComponentBase, ISplineReferencer
    {
        /// <summary>
        /// Holds the Spline container, the spline position, and the position unit type
        /// </summary>
        [SerializeField, FormerlySerializedAs("SplineSettings")]
        SplineSettings m_SplineSettings = new () { Units = PathIndexUnit.Normalized };

        /// <summary>Where to put the camera relative to the spline position.  X is perpendicular
        /// to the spline, Y is up, and Z is parallel to the spline.</summary>
        [Tooltip("Where to put the camera relative to the spline position.  X is perpendicular "
            + "to the spline, Y is up, and Z is parallel to the spline.")]
        public Vector3 SplineOffset = Vector3.zero;

        /// <summary>How to set the camera's rotation and Up.  This will affect the screen composition.</summary>
        [Tooltip("How to set the camera's rotation and Up.  This will affect the screen composition, because "
            + "the camera Aim behaviours will always try to respect the Up direction.")]
        [FormerlySerializedAs("CameraUp")]
        public RotationMode CameraRotation = RotationMode.Default;

        /// <summary>Different ways to set the camera's up vector</summary>
        public enum RotationMode
        {
            /// <summary>Leave the camera's up vector alone.  It will be set according to the Brain's WorldUp.</summary>
            Default,
            /// <summary>Take the up vector from the spline's up vector at the current point</summary>
            Spline,
            /// <summary>Take the up vector from the spline's up vector at the current point, but with the roll zeroed out</summary>
            SplineNoRoll,
            /// <summary>Take the up vector from the Follow target's up vector</summary>
            FollowTarget,
            /// <summary>Take the up vector from the Follow target's up vector, but with the roll zeroed out</summary>
            FollowTargetNoRoll,
        };

        /// <summary>Settings for controlling damping</summary>
        [Serializable]
        public struct DampingSettings
        {
            /// <summary>Enables damping, which causes the camera to move gradually towards
            /// the desired spline position.</summary>
            [Tooltip("Enables damping, which causes the camera to move gradually towards the desired spline position")]
            public bool Enabled;

            /// <summary>How aggressively the camera tries to maintain the offset along
            /// the x, y, or z directions in spline local space.
            /// Meaning:
            /// - x represents the axis that is perpendicular to the spline. Use this to smooth out
            /// imperfections in the path. This may move the camera off the spline.
            /// - y represents the axis that is defined by the spline-local up direction. Use this to smooth out
            /// imperfections in the path. This may move the camera off the spline.
            /// - z represents the axis that is parallel to the spline. This won't move the camera off the spline.
            /// Smaller numbers are more responsive. Larger numbers give a heavier more slowly responding camera.
            /// Using different settings per axis can yield a wide range of camera behaviors.</summary>
            [Tooltip("How aggressively the camera tries to maintain the offset along the "
                + "x, y, or z directions in spline local space. \n"
                + "- x represents the axis that is perpendicular to the spline. Use this to smooth out "
                + "imperfections in the path. This may move the camera off the spline.\n"
                + "- y represents the axis that is defined by the spline-local up direction. Use this to smooth out "
                + "imperfections in the path. This may move the camera off the spline.\n"
                + "- z represents the axis that is parallel to the spline. This won't move the camera off the spline.\n\n"
                + "Smaller numbers are more responsive, larger numbers give a heavier more slowly responding camera. "
                + "Using different settings per axis can yield a wide range of camera behaviors.")]
            public Vector3 Position;

            /// <summary>How aggressively the camera tries to maintain the desired rotation.
            /// This is only used if Camera Rotation is not Default.</summary>
            [Range(0f, 20f)]
            [Tooltip("How aggressively the camera tries to maintain the desired rotation.  "
                + "This is only used if Camera Rotation is not Default.")]
            public float Angular;
        }

        /// <summary>Settings for controlling damping, which causes the camera to
        /// move gradually towards the desired spline position</summary>
        [FoldoutWithEnabledButton]
        [Tooltip("Settings for controlling damping, which causes the camera to "
            + "move gradually towards the desired spline position")]
        public DampingSettings Damping;

        /// <summary>Controls how automatic dolly occurs</summary>
        [NoSaveDuringPlay]
        [FoldoutWithEnabledButton]
        [Tooltip("Controls how automatic dolly occurs.  A tracking target may be necessary to use this feature.")]
        public SplineAutoDolly AutomaticDolly;

        // State info for damping
        float m_PreviousSplinePosition;
        Quaternion m_PreviousRotation;
        Vector3 m_PreviousPosition;

        CinemachineSplineRoll.RollCache m_RollCache;

        // In-editor only: CM 3.0.x Legacy support =================================
        [SerializeField, HideInInspector, NoSaveDuringPlay, FormerlySerializedAs("CameraPosition")] private float m_LegacyPosition = -1;
        [SerializeField, HideInInspector, NoSaveDuringPlay, FormerlySerializedAs("PositionUnits")] private PathIndexUnit m_LegacyUnits;
        [SerializeField, HideInInspector, NoSaveDuringPlay, FormerlySerializedAs("Spline")] private SplineContainer m_LegacySpline;
        void PerformLegacyUpgrade()
        {
            if (m_LegacyPosition != -1)
            {
                m_SplineSettings.Position = m_LegacyPosition;
                m_SplineSettings.Units = m_LegacyUnits;
                m_LegacyPosition = -1;
                m_LegacyUnits = 0;
            }
            if (m_LegacySpline != null)
            {
                m_SplineSettings.Spline = m_LegacySpline;
                m_LegacySpline = null;
            }
        }
        // =================================

        /// <inheritdoc/>
        public ref SplineSettings SplineSettings => ref m_SplineSettings;

        /// <summary>The Spline container to which the camera will be constrained.</summary>
        public SplineContainer Spline
        {
            get => m_SplineSettings.Spline;
            set => m_SplineSettings.Spline = value;
        }

        /// <summary>The position along the spline at which the camera will be placed. This can be animated directly,
        /// or set automatically by the Auto-Dolly feature to get as close as possible to the Follow target.
        /// The value is interpreted according to the Position Units setting.</summary>
        public float CameraPosition
        {
            get => m_SplineSettings.Position;
            set => m_SplineSettings.Position = value;
        }

        /// <summary>How to interpret the Spline Position:
        /// - Distance: Values range from 0 (start of Spline) to Length of the Spline (end of Spline).
        /// - Normalized: Values range from 0 (start of Spline) to 1 (end of Spline).
        /// - Knot: Values are defined by knot indices and a fractional value representing the normalized
        /// interpolation between the specific knot index and the next knot."</summary>
        public PathIndexUnit PositionUnits
        {
            get => m_SplineSettings.Units;
            set => m_SplineSettings.ChangeUnitPreservePosition(value);
        }

        void OnValidate()
        {
            PerformLegacyUpgrade(); // only called in-editor
            Damping.Position.x = Mathf.Clamp(Damping.Position.x, 0, 20);
            Damping.Position.y = Mathf.Clamp(Damping.Position.y, 0, 20);
            Damping.Position.z = Mathf.Clamp(Damping.Position.z, 0, 20);
            Damping.Angular = Mathf.Clamp(Damping.Angular, 0, 20);
            AutomaticDolly.Method?.Validate();
        }

        void Reset()
        {
            m_SplineSettings = new SplineSettings { Units = PathIndexUnit.Normalized };
            SplineOffset = Vector3.zero;
            CameraRotation = RotationMode.Default;
            Damping = default;
            AutomaticDolly.Method = null;
        }

        /// <inheritdoc/>
        protected override void OnEnable()
        {
            base.OnEnable();
            m_RollCache.Refresh(this);
            AutomaticDolly.Method?.Reset();
        }

        /// <inheritdoc/>
        protected override void OnDisable()
        {
            m_SplineSettings.InvalidateCache();
            base.OnDisable();
        }

        /// <summary>True if component is enabled and has a spline</summary>
        public override bool IsValid => enabled && Spline != null;

        /// <summary>Get the Cinemachine Pipeline stage that this component implements.
        /// Always returns the Body stage</summary>
        public override CinemachineCore.Stage Stage => CinemachineCore.Stage.Body;

        /// <summary>
        /// Report maximum damping time needed for this component.
        /// </summary>
        /// <returns>Highest damping setting in this component</returns>
        public override float GetMaxDampTime() => !Damping.Enabled ? 0 :
            Mathf.Max(Mathf.Max(Damping.Position.x, Mathf.Max(Damping.Position.y, Damping.Position.z)), Damping.Angular);

        /// <summary>Positions the virtual camera according to the transposer rules.</summary>
        /// <param name="curState">The current camera state</param>
        /// <param name="deltaTime">Used for damping.  If less that 0, no damping is done.</param>
        public override void MutateCameraState(ref CameraState curState, float deltaTime)
        {
            if (!IsValid)
                return;

            var spline = m_SplineSettings.GetCachedSpline();
            if (spline == null)
                return;

            var splinePos = spline.StandardizePosition(CameraPosition, PositionUnits, out var maxPos);

            // Init previous frame state info
            if (deltaTime < 0 || !VirtualCamera.PreviousStateIsValid)
            {
                m_PreviousSplinePosition = splinePos;
                m_PreviousPosition = curState.RawPosition;
                m_PreviousRotation = curState.RawOrientation;
                m_RollCache.Refresh(this);
            }

            // Invoke AutoDolly algorithm to get new desired spline position
            if (AutomaticDolly.Enabled && AutomaticDolly.Method != null)
                splinePos = AutomaticDolly.Method.GetSplinePosition(
                    this, FollowTarget, Spline, splinePos, PositionUnits, deltaTime);

            // Apply damping in the spline direction
            if (Damping.Enabled && deltaTime >= 0 && VirtualCamera.PreviousStateIsValid)
            {
                // If spline is closed, we choose shortest path for damping
                var prev = m_PreviousSplinePosition;
                if (spline.Closed && Mathf.Abs(splinePos - prev) > maxPos * 0.5f)
                    prev += (splinePos > prev) ? maxPos : -maxPos;

                // Do the damping
                splinePos = prev + Damper.Damp(splinePos - prev, Damping.Position.z, deltaTime);
            }
            m_PreviousSplinePosition = CameraPosition = splinePos;

            spline.EvaluateSplineWithRoll(
                Spline.transform, spline.ConvertIndexUnit(splinePos, PositionUnits, PathIndexUnit.Normalized),
                m_RollCache.GetSplineRoll(this), out var newPos, out var newSplineRotation);

            // Apply the offset to get the new camera position
            var offsetX = newSplineRotation * Vector3.right;
            var offsetY = newSplineRotation * Vector3.up;
            var offsetZ = newSplineRotation * Vector3.forward;
            newPos += SplineOffset.x * offsetX;
            newPos += SplineOffset.y * offsetY;
            newPos += SplineOffset.z * offsetZ;

            // Apply damping to the remaining directions
            if (Damping.Enabled && deltaTime >= 0 && VirtualCamera.PreviousStateIsValid)
            {
                var currentCameraPos = m_PreviousPosition;
                var delta = (currentCameraPos - newPos);
                var delta1 = Vector3.Dot(delta, offsetY) * offsetY;
                var delta0 = delta - delta1;

                delta0 = Damper.Damp(delta0, Damping.Position.x, deltaTime);
                delta1 = Damper.Damp(delta1, Damping.Position.y, deltaTime);
                newPos = currentCameraPos - (delta0 + delta1);
            }
            curState.RawPosition = m_PreviousPosition = newPos;

            // Set the orientation and up
            var newRot = GetCameraRotationAtSplinePoint(newSplineRotation, curState.ReferenceUp, out bool isDefault);
            if (Damping.Enabled && deltaTime >= 0 && VirtualCamera.PreviousStateIsValid)
            {
                float t = VirtualCamera.DetachedFollowTargetDamp(1, Damping.Angular, deltaTime);
                newRot = Quaternion.Slerp(m_PreviousRotation, newRot, t);
            }
            m_PreviousRotation = newRot;
            curState.RawOrientation = newRot;

            if (!isDefault)
                curState.ReferenceUp = curState.RawOrientation * Vector3.up;
        }

        Quaternion GetCameraRotationAtSplinePoint(Quaternion splineOrientation, Vector3 up, out bool isDefault)
        {
            isDefault = false;
            switch (CameraRotation)
            {
                default:
                case RotationMode.Default: break;
                case RotationMode.Spline: return splineOrientation;
                case RotationMode.SplineNoRoll:
                    return Quaternion.LookRotation(splineOrientation * Vector3.forward, up);
                case RotationMode.FollowTarget:
                    if (FollowTarget != null)
                        return FollowTargetRotation;
                    break;
                case RotationMode.FollowTargetNoRoll:
                    if (FollowTarget != null)
                        return Quaternion.LookRotation(FollowTargetRotation * Vector3.forward, up);
                    break;
            }
            isDefault = true;
            return Quaternion.LookRotation(VirtualCamera.transform.rotation * Vector3.forward, up);
        }
    }
}
