using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;

#if CINEMACHINE_HDRP
using UnityEngine.Rendering.HighDefinition;
#endif

namespace Unity.Cinemachine
{
    /// <summary>
    /// This behaviour will drive the Camera focusDistance property. It can be used to hold focus onto
    /// a specific object, or (in HDRP) to auto-detect what is in front of the camera and focus on that.
    ///
    /// Camera.focusDistance is only available in physical mode, and appropriate processing
    /// must be installed for it to have any visible effect.
    ///
    /// This component's ScreenCenter mode is only available in HDRP projects, and in this mode
    /// the component cannot be dynamically added at runtime; it must be added in the editor.
    /// </summary>
    [ExecuteAlways]
    [AddComponentMenu("Cinemachine/Procedural/Extensions/Cinemachine Auto Focus")]
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
            /// <summary>HDRP only: Focus will be on whatever is located in the depth buffer.
            /// at the center of the screen</summary>
            ScreenCenter
        };

        /// <summary>The camera's focus distance will be set to the distance from the camera to
        /// the selected target.  The Focus Offset field will then modify that distance.</summary>
        [Tooltip("The camera's focus distance will be set to the distance from the camera to "
            + "the selected target.  The Focus Offset field will then modify that distance.")]
        public FocusTrackingMode FocusTarget;

        /// <summary>The target to use if Focus Target is set to Custom Target</summary>
        [Tooltip("The target to use if Focus Target is set to Custom Target")]
        public Transform CustomTarget;

        /// <summary>Offsets the sharpest point away in depth from the focus target location</summary>
        [Tooltip("Offsets the sharpest point away in depth from the focus target location.")]
        public float FocusDepthOffset;

        /// <summary>
        /// Set this to make the focus adjust gradually to the desired setting.  The
        /// value corresponds approximately to the time the focus will take to adjust to the new value.
        /// </summary>
        [Tooltip("The value corresponds approximately to the time the focus will take to adjust to the new value.")]
        public float Damping;

#if CINEMACHINE_HDRP
        /// <summary>
        /// Radius of the AutoFocus sensor in the center of the screen.  A value of 1 would fill the screen.
        /// It's recommended to keep this quite small.  Default value is 0.02.
        /// </summary>
        [Tooltip("Radius of the AutoFocus sensor in the center of the screen.  A value of 1 would fill the screen.  "
            + "It's recommended to keep this quite small.  Default value is 0.02")]
        [Range(0, 0.1f)]
        public float AutoDetectionRadius;

        CustomPassVolume m_CustomPassVolume;

        /// <summary>Serialized so that the compute shader is included in the build.</summary>
        [SerializeField, NoSaveDuringPlay]
        ComputeShader m_ComputeShader;

        void OnDisable()
        {
            ReleaseFocusVolume();
        }
#endif

        class VcamExtraState : VcamExtraStateBase
        {
            public float CurrentFocusDistance;
        }

        void Reset()
        {
            Damping = 0.2f;
            FocusTarget = FocusTrackingMode.None;
            CustomTarget = null;
            FocusDepthOffset = 0;
#if CINEMACHINE_HDRP
            AutoDetectionRadius = 0.02f;
#endif
        }

        void OnValidate()
        {
            Damping = Mathf.Max(0, Damping);
        }

        /// <summary>Apply PostProcessing effects</summary>
        /// <param name="vcam">The virtual camera being processed</param>
        /// <param name="stage">The current pipeline stage</param>
        /// <param name="state">The current virtual camera state</param>
        /// <param name="deltaTime">The current applicable deltaTime</param>
        protected override void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
#if CINEMACHINE_HDRP
            if (FocusTarget != FocusTrackingMode.ScreenCenter || !CinemachineCore.IsLive(vcam))
                ReleaseFocusVolume();
#endif
            // Set the focus after the camera has been fully positioned
            if (stage == CinemachineCore.Stage.Finalize && FocusTarget != FocusTrackingMode.None)
            {
                var extra = GetExtraState<VcamExtraState>(vcam);
                float focusDistance = 0;
                Transform focusTarget = null;
                switch (FocusTarget)
                {
                    default:
                        break;
                    case FocusTrackingMode.LookAtTarget:
                        if (state.HasLookAt())
                            focusDistance =  (state.GetFinalPosition() - state.ReferenceLookAt).magnitude;
                        else
                            focusTarget = vcam.LookAt; // probably null, but doesn't hurt
                        break;
                    case FocusTrackingMode.FollowTarget:
                        focusTarget = vcam.Follow;
                        break;
                    case FocusTrackingMode.CustomTarget:
                        focusTarget = CustomTarget;
                        break;
#if CINEMACHINE_HDRP
                    case FocusTrackingMode.ScreenCenter:
                        focusDistance = FetchAutoFocusDistance(vcam, extra);
                        if (focusDistance < 0)
                            return; // not available, abort
                        break;
#endif
                }
                if (focusTarget != null)
                    focusDistance += (state.GetFinalPosition() - focusTarget.position).magnitude;

                focusDistance = Mathf.Max(0.1f, focusDistance + FocusDepthOffset);

                // Apply damping
                if (deltaTime >= 0 && vcam.PreviousStateIsValid)
                    focusDistance = extra.CurrentFocusDistance + Damper.Damp(
                        focusDistance - extra.CurrentFocusDistance, Damping, deltaTime);
                extra.CurrentFocusDistance = focusDistance;
                state.Lens.PhysicalProperties.FocusDistance = focusDistance;
            }
        }

#if CINEMACHINE_HDRP
        float FetchAutoFocusDistance(CinemachineVirtualCameraBase vcam, VcamExtraState extra)
        {
            var volume = GetFocusVolume(vcam);
            if (volume != null && volume.customPasses.Count > 0)
            {
                if (volume.customPasses[0] is FocusDistance fd)
                {
                    fd.KernelRadius = AutoDetectionRadius;
                    fd.CurrentFocusDistance = vcam.PreviousStateIsValid ? extra.CurrentFocusDistance : 0;
                    return fd.ComputedFocusDistance;
                }
            }
            return -1; // unavailable
        }

        static Dictionary<Camera, int> s_VolumeRefCounts;
        static List<CustomPassVolume> s_ScratchList;

        [RuntimeInitializeOnLoadMethod]
        static void InitializeModule()
        {
            s_VolumeRefCounts = null;
            s_ScratchList = null;
        }

        CustomPassVolume GetFocusVolume(CinemachineVirtualCameraBase vcam)
        {
            if (s_VolumeRefCounts == null || s_VolumeRefCounts.Count == 0)
            {
                s_VolumeRefCounts = new Dictionary<Camera, int>();
                s_ScratchList = new List<CustomPassVolume>();
                m_CustomPassVolume = null; // re-fetch after domain reload
            }
            if (m_CustomPassVolume == null)
            {
                var brain = CinemachineCore.FindPotentialTargetBrain(vcam);
                var cam = brain == null ? null : brain.OutputCamera;
                if (cam != null)
                {
                    // Find an existing custom pass volume with our custom shader pass
                    s_ScratchList.Clear();
                    cam.GetComponents(s_ScratchList);
                    for (int i = 0; i < s_ScratchList.Count; ++i)
                    {
                        var v = s_ScratchList[i];
                        if (v.injectionPoint == CustomPassInjectionPoint.AfterOpaqueDepthAndNormal
                            && v.customPasses.Count == 1
                            && v.customPasses[0] is FocusDistance)
                        {
                            m_CustomPassVolume = v;
                            if (!s_VolumeRefCounts.ContainsKey(cam))
                                s_VolumeRefCounts[cam] = 0;
                            break;
                        }
                    }
                    if (m_CustomPassVolume == null)
                    {
                        m_CustomPassVolume = cam.gameObject.AddComponent<CustomPassVolume>();
                        m_CustomPassVolume.hideFlags = HideFlags.HideAndDontSave;
                        m_CustomPassVolume.isGlobal = true;
                        m_CustomPassVolume.injectionPoint = CustomPassInjectionPoint.AfterOpaqueDepthAndNormal;
                        m_CustomPassVolume.targetCamera = cam;
#if UNITY_EDITOR
                        m_CustomPassVolume.runInEditMode = true;
#endif
                        var pass = m_CustomPassVolume.AddPassOfType<FocusDistance>() as FocusDistance;
                        pass.ComputeShader = m_ComputeShader;
                        pass.PushToCamera = false;
                        pass.CurrentFocusDistance = cam.focusDistance;
                        pass.Camera = cam;
                        pass.targetColorBuffer = CustomPass.TargetBuffer.None;
                        pass.targetDepthBuffer = CustomPass.TargetBuffer.Camera;
                        pass.clearFlags = ClearFlag.None;
                        pass.KernelRadius = AutoDetectionRadius;
                        pass.name = GetType().Name;

                        s_VolumeRefCounts[cam] = 0;
                    }
                    var refs = s_VolumeRefCounts[cam];
                    ++refs;
                    s_VolumeRefCounts[cam] = refs;
                    m_CustomPassVolume.enabled = refs > 0;
                }
            }
            return m_CustomPassVolume;
        }

        void ReleaseFocusVolume()
        {
            if (m_CustomPassVolume != null && s_VolumeRefCounts != null)
            {
                if (m_CustomPassVolume.TryGetComponent(out Camera cam))
                {
                    var refs = s_VolumeRefCounts[cam];
                    --refs;
                    s_VolumeRefCounts[cam] = refs;
                    m_CustomPassVolume.enabled = refs > 0;
                }
            }
            m_CustomPassVolume = null;
        }
#endif
    }
}
