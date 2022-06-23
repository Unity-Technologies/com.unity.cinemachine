using UnityEngine;
using System;

namespace Cinemachine
{
    /// <summary>
    /// This is a deprecated component.  Use CinemachineRotationComposer instead.
    /// </summary>
    [AddComponentMenu("")] // Don't display in add component menu
    [CameraPipeline(CinemachineCore.Stage.Aim)]
    [Obsolete("CinemachineComposer has been deprecated. Use CinemachineRotationComposer instead")]
    public class CinemachineComposer : CinemachineComponentBase 
    {
        [Obsolete("m_TrackedObjectOffset has been deprecated. Use CinemachineRotationComposer.TrackedObjectOffset instead")]
        public Vector3 m_TrackedObjectOffset;

        [Obsolete("m_LookaheadTime has been deprecated. Use CinemachineRotationComposer.Lookahead.Time instead")]
        public float m_LookaheadTime;
        [Obsolete("m_LookaheadSmoothing has been deprecated. Use CinemachineRotationComposer.Lookahead.Smoothing instead")]
        public float m_LookaheadSmoothing;
        [Obsolete("m_LookaheadIgnoreY has been deprecated. Use CinemachineRotationComposer.Lookahead.IgnoreY instead")]
        public bool m_LookaheadIgnoreY;
        [Obsolete("m_HorizontalDamping has been deprecated. Use CinemachineRotationComposer.Damping.x instead")]
        public float m_HorizontalDamping;
        [Obsolete("m_VerticalDamping has been deprecated. Use CinemachineRotationComposer.Damping.y instead")]
        public float m_VerticalDamping;

        [Obsolete("m_ScreenX has been deprecated. Use CinemachineRotationComposer.Composition.ScreenPosition.x instead")]
        public float m_ScreenX;
        [Obsolete("m_ScreenY has been deprecated. Use CinemachineRotationComposer.Composition.ScreenPosition.y instead")]
        public float m_ScreenY;
        [Obsolete("m_DeadZoneWidth has been deprecated. Use CinemachineRotationComposer.Composition.DeadZoneSize.x instead")]
        public float m_DeadZoneWidth;
        [Obsolete("m_DeadZoneHeight has been deprecated. Use CinemachineRotationComposer.Composition.DeadZoneSize.y instead")]
        public float m_DeadZoneHeight;
        [Obsolete("m_SoftZoneWidth has been deprecated. Use CinemachineRotationComposer.Composition.SoftZoneSize.x instead")]
        public float m_SoftZoneWidth;
        [Obsolete("m_SoftZoneHeight has been deprecated. Use CinemachineRotationComposer.Composition.SoftZoneSize.y instead")]
        public float m_SoftZoneHeight;
        [Obsolete("m_BiasX has been deprecated. Use CinemachineRotationComposer.Composition.CenterShift.x instead")]
        public float m_BiasX;
        [Obsolete("m_BiasY has been deprecated. Use CinemachineRotationComposer.Composition.CenterShift.y instead")]
        public float m_BiasY;

        [Obsolete("m_CenterOnActivate has been deprecated. Use CinemachineRotationComposer.CenterOnActivate instead")]
        public bool m_CenterOnActivate;

        public override bool IsValid => false;
        public override CinemachineCore.Stage Stage => CinemachineCore.Stage.Aim;
        public override void MutateCameraState(ref CameraState curState, float deltaTime) {}
    }
}
