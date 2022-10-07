using UnityEngine;
using UnityEngine.Serialization;

#if CINEMACHINE_HDRP
    using System.Collections.Generic;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.HighDefinition;
#elif CINEMACHINE_LWRP_7_3_1
    using System.Collections.Generic;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.Universal;
#endif

namespace Cinemachine
{
#if !(CINEMACHINE_HDRP || CINEMACHINE_LWRP_7_3_1)
    // Workaround for Unity scripting bug
    /// <summary>
    /// This behaviour is a liaison between Cinemachine with the Post-Processing v3 module.
    ///
    /// As a component on the Virtual Camera, it holds
    /// a Post-Processing Profile asset that will be applied to the Unity camera whenever
    /// the Virtual camera is live.  It also has the optional functionality of animating
    /// the Focus Distance and DepthOfField properties of the Camera State, and
    /// applying them to the current Post-Processing profile, provided that profile has a
    /// DepthOfField effect that is enabled.
    /// </summary>
    [AddComponentMenu("")] // Hide in menu
    public class CinemachineVolumeSettings : MonoBehaviour {}
#else
    /// <summary>
    /// This behaviour is a liaison between Cinemachine with the Post-Processing v3 module.
    ///
    /// As a component on the Virtual Camera, it holds
    /// a Post-Processing Profile asset that will be applied to the Unity camera whenever
    /// the Virtual camera is live.  It also has the optional functionality of animating
    /// the Focus Distance and DepthOfField properties of the Camera State, and
    /// applying them to the current Post-Processing profile, provided that profile has a
    /// DepthOfField effect that is enabled.
    /// </summary>
    [ExecuteAlways]
    [AddComponentMenu("Cinemachine/Procedural/Extensions/Cinemachine Volume Settings")]
    [SaveDuringPlay]
    [DisallowMultipleComponent]
    [HelpURL(Documentation.BaseURL + "manual/CinemachineVolumeSettings.html")]
    public class CinemachineVolumeSettings : CinemachineExtension
    {
        /// <summary>
        /// This is the priority for the vcam's PostProcessing volumes.  It's set to a high
        /// number in order to ensure that it overrides other volumes for the active vcam.
        /// You can change this value if necessary to work with other systems.
        /// </summary>
        public static float s_VolumePriority = 1000f;

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
            Camera
        };

        /// <summary>If the profile has the appropriate overrides, will set the base focus 
        /// distance to be the distance from the selected target to the camera.
        /// The Focus Offset field will then modify that distance</summary>
        [Tooltip("If the profile has the appropriate overrides, will set the base focus "
            + "distance to be the distance from the selected target to the camera."
            + "The Focus Offset field will then modify that distance.")]
        [FormerlySerializedAs("m_FocusTracking")]
        public FocusTrackingMode FocusTracking;

        /// <summary>The target to use if Focus Tracks Target is set to Custom Target</summary>
        [Tooltip("The target to use if Focus Tracks Target is set to Custom Target")]
        [FormerlySerializedAs("m_FocusTarget")]
        public Transform FocusTarget;

        /// <summary>Offset from target distance, to be used with Focus Tracks Target.  
        /// Offsets the sharpest point away from the focus target</summary>
        [Tooltip("Offset from target distance, to be used with Focus Tracks Target.  "
            + "Offsets the sharpest point away from the focus target.")]
        [FormerlySerializedAs("m_FocusOffset")]
        public float FocusOffset;

        /// <summary>
        /// This profile will be applied whenever this virtual camera is live
        /// </summary>
        [Tooltip("This profile will be applied whenever this virtual camera is live")]
        [FormerlySerializedAs("m_Profile")]
        public VolumeProfile Profile;

        /// <summary>This is obsolete, please use m_FocusTracking</summary>
        [HideInInspector, SerializeField, FormerlySerializedAs("m_FocusTracksTarget")]
        bool m_LegacyFocusTracksTarget;

        class VcamExtraState
        {
            public VolumeProfile ProfileCopy;

            public void CreateProfileCopy(VolumeProfile source)
            {
                DestroyProfileCopy();
                VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
                if (source != null)
                {
                    foreach (var item in source.components)
                    {
                        var itemCopy = Instantiate(item);
                        profile.components.Add(itemCopy);
                        profile.isDirty = true;
                    }
                }
                ProfileCopy = profile;
            }

            public void DestroyProfileCopy()
            {
                if (ProfileCopy != null)
                    RuntimeUtility.DestroyObject(ProfileCopy);
                ProfileCopy = null;
            }
        }

        /// <summary>True if the profile is enabled and nontrivial</summary>
        public bool IsValid => Profile != null && Profile.components.Count > 0;

        /// <summary>Called by the editor when the shared asset has been edited</summary>
        public void InvalidateCachedProfile()
        {
            var list = GetAllExtraStates<VcamExtraState>();
            for (int i = 0; i < list.Count; ++i)
                list[i].DestroyProfileCopy();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            // Map legacy m_FocusTracksTarget to focus mode
            if (m_LegacyFocusTracksTarget)
            {
                FocusTracking = VirtualCamera.LookAt != null 
                    ? FocusTrackingMode.LookAtTarget : FocusTrackingMode.Camera;
            }
            m_LegacyFocusTracksTarget = false;
        }

        protected override void OnDestroy()
        {
            InvalidateCachedProfile();
            base.OnDestroy();
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
            // Set the focus after the camera has been fully positioned.
            if (stage == CinemachineCore.Stage.Finalize)
            {
                var extra = GetExtraState<VcamExtraState>(vcam);
                if (!IsValid)
                    extra.DestroyProfileCopy();
                else
                {
                    var profile = Profile;

                    // Handle Follow Focus
                    if (FocusTracking == FocusTrackingMode.None)
                        extra.DestroyProfileCopy();
                    else
                    {
                        if (extra.ProfileCopy == null)
                            extra.CreateProfileCopy(Profile);
                        profile = extra.ProfileCopy;
                        if (profile.TryGet(out DepthOfField dof))
                        {
                            float focusDistance = FocusOffset;
                            if (FocusTracking == FocusTrackingMode.LookAtTarget)
                                focusDistance += (state.GetFinalPosition()- state.ReferenceLookAt).magnitude;
                            else
                            {
                                Transform focusTarget = null;
                                switch (FocusTracking)
                                {
                                    default: break;
                                    case FocusTrackingMode.FollowTarget: focusTarget = VirtualCamera.Follow; break;
                                    case FocusTrackingMode.CustomTarget: focusTarget = FocusTarget; break;
                                }
                                if (focusTarget != null)
                                    focusDistance += (state.GetFinalPosition() - focusTarget.position).magnitude;
                            }
                            focusDistance = Mathf.Max(0, focusDistance);
                            dof.focusDistance.value = focusDistance;
#if CINEMACHINE_HDRP
                            state.Lens.FocusDistance = focusDistance;
#endif
                            profile.isDirty = true;
                        }
                    }
                    // Apply the post-processing
                    state.AddCustomBlendable(new CameraState.CustomBlendableItems.Item { Custom = profile, Weight = 1 });
                }
            }
        }

        static void OnCameraCut(CinemachineBrain brain)
        {
            //Debug.Log($"Camera cut to {brain.ActiveVirtualCamera.Name}");

#if CINEMACHINE_HDRP
            // Reset temporal effects
            var cam = brain.OutputCamera;
            if (cam != null)
            {
                HDCamera hdCam = HDCamera.GetOrCreate(cam);
                hdCam.volumetricHistoryIsValid = false;
                hdCam.colorPyramidHistoryIsValid = false;
                hdCam.Reset();
            }
#elif CINEMACHINE_LDRP_7_3_1
            // Reset temporal effects
            var cam = brain.OutputCamera;
            if (cam != null)
            {
                HDCamera hdCam = HDCamera.GetOrCreate(cam);
                hdCam.volumetricHistoryIsValid = false;
                hdCam.colorPyramidHistoryIsValid = false;
                hdCam.Reset();
            }
#endif
        }

        static void ApplyPostFX(CinemachineBrain brain)
        {
            CameraState state = brain.CurrentCameraState;
            int numBlendables = state.GetNumCustomBlendables();
            var volumes = GetDynamicBrainVolumes(brain, numBlendables);
            for (int i = 0; i < volumes.Count; ++i)
            {
                volumes[i].weight = 0;
                volumes[i].sharedProfile = null;
                volumes[i].profile = null;
            }
            Volume firstVolume = null;
            int numPPblendables = 0;
            for (int i = 0; i < numBlendables; ++i)
            {
                var b = state.GetCustomBlendable(i);
                var profile = b.Custom as VolumeProfile;
                if (!(profile == null)) // in case it was deleted
                {
                    var v = volumes[i];
                    if (firstVolume == null)
                        firstVolume = v;
                    v.sharedProfile = profile;
                    v.isGlobal = true;
                    v.priority = s_VolumePriority - (numBlendables - i) - 1;
                    v.weight = b.Weight;
                    ++numPPblendables;
                }
#if true // set this to true to force first weight to 1
                // If more than one volume, then set the frst one's weight to 1
                if (numPPblendables > 1)
                    firstVolume.weight = 1;
#endif
            }
//            if (firstVolume != null)
//                Debug.Log($"Applied post FX for {numPPblendables} PP blendables in {brain.ActiveVirtualCamera.Name}");
        }

        static string sVolumeOwnerName = "__CMVolumes";
        static List<Volume> sVolumes = new List<Volume>();
        static List<Volume> GetDynamicBrainVolumes(CinemachineBrain brain, int minVolumes)
        {
            // Locate the camera's child object that holds our dynamic volumes
            GameObject volumeOwner = null;
            Transform t = brain.transform;
            int numChildren = t.childCount;

            sVolumes.Clear();
            for (int i = 0; volumeOwner == null && i < numChildren; ++i)
            {
                GameObject child = t.GetChild(i).gameObject;
                if (child.hideFlags == HideFlags.HideAndDontSave)
                {
                    child.GetComponents(sVolumes);
                    if (sVolumes.Count > 0)
                        volumeOwner = child;
                }
            }

            if (minVolumes > 0)
            {
                if (volumeOwner == null)
                {
                    volumeOwner = new GameObject(sVolumeOwnerName);
                    volumeOwner.hideFlags = HideFlags.HideAndDontSave;
                    volumeOwner.transform.parent = t;
                }

                // Update the volume's layer so it will be seen
#if CINEMACHINE_HDRP
                brain.gameObject.TryGetComponent<HDAdditionalCameraData>(out var data);
#elif CINEMACHINE_LWRP_7_3_1
                brain.gameObject.TryGetComponent<UniversalAdditionalCameraData>(out var data);
#endif
                if (data != null)
                {
                    int mask = data.volumeLayerMask;
                    for (int i = 0; i < 32; ++i)
                    {
                        if ((mask & (1 << i)) != 0)
                        {
                            volumeOwner.layer = i;
                            break;
                        }
                    }
                }

                while (sVolumes.Count < minVolumes)
                    sVolumes.Add(volumeOwner.gameObject.AddComponent<Volume>());
            }
            return sVolumes;
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoad]
        class EditorInitialize { static EditorInitialize() { InitializeModule(); } }
#endif
        [RuntimeInitializeOnLoadMethod]
        static void InitializeModule()
        {
            // After the brain pushes the state to the camera, hook in to the PostFX
            CinemachineCore.CameraUpdatedEvent.RemoveListener(ApplyPostFX);
            CinemachineCore.CameraUpdatedEvent.AddListener(ApplyPostFX);
            CinemachineCore.CameraCutEvent.RemoveListener(OnCameraCut);
            CinemachineCore.CameraCutEvent.AddListener(OnCameraCut);
        }
    }
#endif
}
