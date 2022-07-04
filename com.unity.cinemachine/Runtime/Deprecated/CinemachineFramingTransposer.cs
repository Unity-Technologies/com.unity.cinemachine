using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Cinemachine
{
    /// <summary>
    /// This is a deprecated component.  Use CinemachinePositionComposer instead.
    /// </summary>
    [AddComponentMenu("")] // Don't display in add component menu
    [Obsolete("CinemachineFramingTransposer has been deprecated. Use CinemachinePositionComposer instead")]
    [CameraPipeline(CinemachineCore.Stage.Body)]
    public class CinemachineFramingTransposer : CinemachineComponentBase
    {
        [Obsolete("CinemachineFramingTransposer.m_TrackedObjectOffset has been deprecated. Use CinemachinePositionComposer.TrackedObjectOffset instead")]
        public Vector3 m_TrackedObjectOffset;
        [Obsolete("CinemachineFramingTransposer.m_LookaheadTime has been deprecated. Use CinemachinePositionComposer.Lookahead.Time instead")]
        public float m_LookaheadTime;
        [Obsolete("CinemachineFramingTransposer.m_LookaheadSmoothing has been deprecated. Use CinemachinePositionComposer.Lookahead.Smoothing instead")]
        public float m_LookaheadSmoothing;
        [Obsolete("CinemachineFramingTransposer.m_LookaheadIgnoreY has been deprecated. Use CinemachinePositionComposer.Lookahead.IgnoreY instead")]
        public bool m_LookaheadIgnoreY;
        [Obsolete("CinemachineFramingTransposer.m_XDamping has been deprecated. Use CinemachinePositionComposer.Damping.x instead")]
        public float m_XDamping;
        [Obsolete("CinemachineFramingTransposer.m_YDamping has been deprecated. Use CinemachinePositionComposer.Damping.y instead")]
        public float m_YDamping;
        [Obsolete("CinemachineFramingTransposer.m_ZDamping has been deprecated. Use CinemachinePositionComposer.Damping.z instead")]
        public float m_ZDamping;
        [Obsolete("CinemachineFramingTransposer.m_TargetMovementOnly has been deprecated. Use CinemachinePositionComposer.TargetMovementOnly instead")]
        public bool m_TargetMovementOnly;
        [Obsolete("CinemachineFramingTransposer.m_ScreenX has been deprecated. Use CinemachinePositionComposer.Composition.ScreenPosition.x instead")]
        public float m_ScreenX;
        [Obsolete("CinemachineFramingTransposer.m_ScreenY has been deprecated. Use CinemachinePositionComposer.Composition.ScreenPosition.y instead")]
        public float m_ScreenY;
        [Obsolete("CinemachineFramingTransposer.m_CameraDistance has been deprecated. Use CinemachinePositionComposer.CameraDistance instead")]
        public float m_CameraDistance;
        [Obsolete("CinemachineFramingTransposer.m_DeadZoneWidth has been deprecated. Use CinemachinePositionComposer.Composition.DeadZone.x instead")]
        public float m_DeadZoneWidth;
        [Obsolete("CinemachineFramingTransposer.m_DeadZoneHeight has been deprecated. Use CinemachinePositionComposer.Composition.DeadZone.y instead")]
        public float m_DeadZoneHeight;
        [FormerlySerializedAs("m_DistanceDeadZoneSize")]
        [Obsolete("CinemachineFramingTransposer.m_DeadZoneDepth has been deprecated. Use CinemachinePositionComposer.DeadZoneDepth instead")]
        public float m_DeadZoneDepth;
        [Obsolete("CinemachineFramingTransposer.m_UnlimitedSoftZone has been deprecated. Use CinemachinePositionComposer.UnlimitedSoftZone instead")]
        public bool m_UnlimitedSoftZone;
        [Obsolete("CinemachineFramingTransposer.m_SoftZoneWidth has been deprecated. Use CinemachinePositionComposer.Composition.SovtZone.x instead")]
        public float m_SoftZoneWidth;
        [Obsolete("CinemachineFramingTransposer.m_SoftZoneHeight has been deprecated. Use CinemachinePositionComposer.Composition.SovtZone.y instead")]
        public float m_SoftZoneHeight;
        [Obsolete("CinemachineFramingTransposer.m_BiasX has been deprecated. Use CinemachinePositionComposer.Composition.Bias.x instead")]
        public float m_BiasX;
        [Obsolete("CinemachineFramingTransposer.m_BiasY has been deprecated. Use CinemachinePositionComposer.Composition.Bias.y instead")]
        public float m_BiasY;
        [Obsolete("CinemachineFramingTransposer.m_CenterOnActivate has been deprecated. Use CinemachinePositionComposer.CenterOnActivate instead")]
        public bool m_CenterOnActivate;

        [FormerlySerializedAs("m_FramingMode")]
        [Obsolete("CinemachineFramingTransposer. has been deprecated. Use CinemachinePositionComposer. instead")]
        public FramingMode m_GroupFramingMode;

        [Obsolete("CinemachineFramingTransposer.m_AdjustmentMode has been deprecated. Use CinemachinePositionComposer.AdjustmentMode instead")]
        public AdjustmentMode m_AdjustmentMode;

        [Obsolete("CinemachineFramingTransposer.m_GroupFramingSize has been deprecated. Use CinemachinePositionComposer.GroupFramingSize instead")]
        public float m_GroupFramingSize;
        [Obsolete("CinemachineFramingTransposer.m_MaxDollyIn has been deprecated. Use CinemachinePositionComposer.DollyRange.x instead")]
        public float m_MaxDollyIn;
        [Obsolete("CinemachineFramingTransposer.m_MaxDollyOut has been deprecated. Use CinemachinePositionComposer.DollyRange.y instead")]
        public float m_MaxDollyOut;
        [Obsolete("CinemachineFramingTransposer.m_MinimumDistance has been deprecated. Use CinemachinePositionComposer.TargetDistanceRange.x instead")]
        public float m_MinimumDistance;
        [Obsolete("CinemachineFramingTransposer.m_MaximumDistance has been deprecated. Use CinemachinePositionComposer.TargetDistanceRange.y instead")]
        public float m_MaximumDistance;
        [Obsolete("CinemachineFramingTransposer.m_MinimumFOV has been deprecated. Use CinemachinePositionComposer.FovRange.x instead")]
        public float m_MinimumFOV;
        [Obsolete("CinemachineFramingTransposer.m_MaximumFOV has been deprecated. Use CinemachinePositionComposer.FovRange.y instead")]
        public float m_MaximumFOV;
        [Obsolete("CinemachineFramingTransposer.m_MinimumOrthoSize has been deprecated. Use CinemachinePositionComposer.OrthoSizeRange.x instead")]
        public float m_MinimumOrthoSize;
        [Obsolete("CinemachineFramingTransposer.m_MaximumOrthoSize has been deprecated. Use CinemachinePositionComposer.OrthoSizeRange.y instead")]
        public float m_MaximumOrthoSize;
        
        [Obsolete("CinemachineFramingTransposer.FramingMode has been deprecated. Use CinemachinePositionComposer.FramingModes instead")]
        public enum FramingMode
        {
            [Obsolete("CinemachineFramingTransposer.FramingMode has been deprecated. Use CinemachinePositionComposer.FramingModes instead")]
            Horizontal,
            [Obsolete("CinemachineFramingTransposer.FramingMode has been deprecated. Use CinemachinePositionComposer.FramingModes instead")]
            Vertical,
            [Obsolete("CinemachineFramingTransposer.FramingMode has been deprecated. Use CinemachinePositionComposer.FramingModes instead")]
            HorizontalAndVertical,
            [Obsolete("CinemachineFramingTransposer.FramingMode has been deprecated. Use CinemachinePositionComposer.FramingModes instead")]
            None
        };

        [Obsolete("CinemachineFramingTransposer.AdjustmentMode has been deprecated. Use CinemachinePositionComposer.AdjustmentModes instead")]
        public enum AdjustmentMode
        {
            [Obsolete("CinemachineFramingTransposer.AdjustmentMode has been deprecated. Use CinemachinePositionComposer.AdjustmentModes instead")]
            ZoomOnly,
            [Obsolete("CinemachineFramingTransposer.AdjustmentMode has been deprecated. Use CinemachinePositionComposer.AdjustmentModes instead")]
            DollyOnly,
            [Obsolete("CinemachineFramingTransposer.AdjustmentMode has been deprecated. Use CinemachinePositionComposer.AdjustmentModes instead")]
            DollyThenZoom
        };

        public override bool IsValid => false;
        public override CinemachineCore.Stage Stage => CinemachineCore.Stage.Aim;
        public override void MutateCameraState(ref CameraState curState, float deltaTime) {}

        // Helper to upgrade to CM3
        internal void UpgradeToCm3(MonoBehaviour b)
        {
            var c = b as CinemachinePositionComposer;

            c.TrackedObjectOffset = m_TrackedObjectOffset;
            c.Lookahead = new LookaheadSettings
            {
                Enabled = m_LookaheadTime > 0,
                Time = m_LookaheadTime,
                Smoothing = m_LookaheadSmoothing,
                IgnoreY = m_LookaheadIgnoreY
            };
            c.CameraDistance = m_CameraDistance;
            c.DeadZoneDepth = m_DeadZoneDepth;
            c.Damping = new Vector3(m_XDamping, m_YDamping, m_ZDamping);
            c.Composition = new ScreenComposerSettings
            {
                ScreenPosition = new Vector2(m_ScreenX, m_ScreenY) - new Vector2(0.5f, 0.5f),
                DeadZoneSize = new Vector2(m_DeadZoneWidth, m_DeadZoneHeight),
                SoftZoneSize = new Vector2(m_SoftZoneWidth, m_SoftZoneHeight),
                Bias = new Vector2(m_BiasX, m_BiasY)
            };
            c.UnlimitedSoftZone = m_UnlimitedSoftZone;
            c.CenterOnActivate = m_CenterOnActivate;
            c.GroupFramingMode = m_GroupFramingMode == FramingMode.None
                ? CinemachinePositionComposer.FramingModes.None 
                : (CinemachinePositionComposer.FramingModes)((int)m_GroupFramingMode + 1);
            c.AdjustmentMode = (CinemachinePositionComposer.AdjustmentModes)m_AdjustmentMode;
            c.GroupFramingSize = m_GroupFramingSize;
            c.DollyRange = new Vector2(-m_MaxDollyIn, m_MaxDollyOut);
            c.TargetDistanceRange = new Vector2(m_MinimumDistance, m_MaximumDistance);
            c.FovRange = new Vector2(m_MinimumFOV, m_MaximumFOV);
            c.OrthoSizeRange = new Vector2(m_MinimumOrthoSize, m_MaximumOrthoSize);
        }
    }
}
