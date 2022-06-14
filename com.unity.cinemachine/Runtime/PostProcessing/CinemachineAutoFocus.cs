using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;
using Cinemachine.Utility;

#if CINEMACHINE_HDRP
using UnityEngine.Rendering.HighDefinition;
#endif

namespace Cinemachine.PostFX
{
#if !CINEMACHINE_HDRP
    /// <summary>
    /// This behaviour will drive the CmCamera Lens's FocusDistance setting.
    /// It can be used to hold focus onto a specific object, or to auto-detect what is in front
    /// of the camera and focus on that.
    /// 
    /// This component is only available in HDRP projects.
    /// </summary>
    [AddComponentMenu("")] // Hide in menu
    public class CinemachineAutoFocus : MonoBehaviour {}
#else
    /// <summary>
    /// This behaviour will drive the CmCamera Lens's FocusDistance setting.
    /// It can be used to hold focus onto a specific object, or to auto-detect what is in front
    /// of the camera and focus on that.
    /// 
    /// This component is only available in HDRP projects.
    /// </summary>
    [ExecuteAlways]
    [AddComponentMenu("")] // Hide in menu
    [SaveDuringPlay]
    [DisallowMultipleComponent]
    [HelpURL(Documentation.BaseURL + "manual/CinemachineAutoFocus.html")]
    public class CinemachineAutoFocus : CinemachineExtension
    {
        /// <summary>The reference object for focus tracking</summary>
        public enum FocusTrackingMode
        {
            /// <summary>No focus tracking</summary>
            None,
            /// <summary>Focus offset is relative to the LookAt target</summary>
            LookAtTarget,
            /// <summary>Focus offset is relative to the Follow target</summary>
            FollowTarget,
            /// <summary>Focus offset is relative to the Custom target set here</summary>
            CustomTarget,
            /// <summary>Focus offset is relative to the camera</summary>
            Camera,
            /// <summary>Focus will be on whatever is located in the depth buffer 
            /// at the center of the screen</summary>
            Automatic
        };

        /// <summary>The camera's focus disttance will be set to the distance from the selected 
        /// target to the camera.  The Focus Offset field will then modify that distance</summary>
        [Tooltip("The camera's focus disttance will be set to the distance from the selected "
            + "target to the camera.  The Focus Offset field will then modify that distance.")]
        public FocusTrackingMode FocusTarget;

        /// <summary>The target to use if Focus Target is set to Custom Target</summary>
        [Tooltip("The target to use if Focus Target is set to Custom Target")]
        public Transform CustomTarget;

        /// <summary>Offsets the sharpest point away from the focus target location</summary>
        [Tooltip("Offsets the sharpest point away from the focus target location.")]
        public float FocusOffset;

        /// <summary>
        /// Set this to make the focus adjust gradually to the desired setting.  The
        /// value corresponds approximately to the time the focus will take to adjust to the new value.
        /// </summary>
        [Tooltip("The value corresponds approximately to the time the focus will take to adjust to the new value.")]
        public float Damping;

        /// <summary>Apply PostProcessing effects</summary>
        /// <param name="vcam">The virtual camera being processed</param>
        /// <param name="stage">The current pipeline stage</param>
        /// <param name="state">The current virtual camera state</param>
        /// <param name="deltaTime">The current applicable deltaTime</param>
        protected override void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            // Set the focus after the camera has been fully positioned
            if (stage == CinemachineCore.Stage.Finalize && FocusTarget != FocusTrackingMode.None)
            {
                float focusDistance = 0;
                Transform focusTarget = null;
                switch (FocusTarget)
                {
                    default: 
                        break;
                    case FocusTrackingMode.LookAtTarget: 
                        focusDistance = (state.FinalPosition - state.ReferenceLookAt).magnitude; 
                        break;
                    case FocusTrackingMode.FollowTarget: 
                        focusTarget = VirtualCamera.Follow; 
                        break;
                    case FocusTrackingMode.CustomTarget: 
                        focusTarget = CustomTarget; 
                        break;
                    case FocusTrackingMode.Automatic:
                        focusDistance = FetchAutoFocusDistance();
                        break;
                }
                if (focusTarget != null)
                    focusDistance += (state.FinalPosition - focusTarget.position).magnitude;

                focusDistance = Mathf.Max(0, focusDistance + FocusOffset);

                // Apply damping
                var extra = GetExtraState<VcamExtraState>(vcam);
                if (deltaTime >= 0 && vcam.PreviousStateIsValid)
                    focusDistance = extra.CurrentFocusDistance + Damper.Damp(
                        focusDistance - extra.CurrentFocusDistance, Damping, deltaTime);
                extra.CurrentFocusDistance = focusDistance;
                state.Lens.FocusDistance = focusDistance;                            
            }
        }

        class VcamExtraState
        {
            public float CurrentFocusDistance;
        }

        float FetchAutoFocusDistance()
        {
            return 0; // GML todo
        }
#endif
    }
}
