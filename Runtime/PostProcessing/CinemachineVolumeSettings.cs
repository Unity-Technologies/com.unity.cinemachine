using UnityEngine;

#if CINEMACHINE_HDRP
    using System.Collections.Generic;
    using UnityEngine.Rendering;
    #if CINEMACHINE_HDRP_7_0_0
        using UnityEngine.Rendering.HighDefinition;
    #else
        using UnityEngine.Experimental.Rendering.HDPipeline;
    #endif
#elif CINEMACHINE_LWRP_7_0_0
    using System.Collections.Generic;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.Universal;
#endif

namespace Cinemachine.PostFX
{
#if !(CINEMACHINE_HDRP || CINEMACHINE_LWRP_7_0_0)
    // Workaround for Unity scripting bug
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
    [DocumentationSorting(DocumentationSortingAttribute.Level.UserRef)]
#if UNITY_2018_3_OR_NEWER
    [ExecuteAlways]
#else
    [ExecuteInEditMode]
#endif
    [AddComponentMenu("")] // Hide in menu
    [SaveDuringPlay]
    public class CinemachineVolumeSettings : CinemachineExtension
    {
        [Tooltip("If checked, then the Focus Distance will be set to the distance between the camera and the LookAt target.  Requires DepthOfField effect in the Profile")]
        public bool m_FocusTracksTarget;

        [Tooltip("Offset from target distance, to be used with Focus Tracks Target.  Offsets the sharpest point away from the LookAt target.")]
        public float m_FocusOffset;

        [Tooltip("This Post-Processing profile will be applied whenever this virtual camera is live")]
        public VolumeProfile m_Profile;

        class VcamExtraState
        {
            public VolumeProfile mProfileCopy;

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
                mProfileCopy = profile;
            }

            public void DestroyProfileCopy()
            {
                if (mProfileCopy != null)
                    RuntimeUtility.DestroyObject(mProfileCopy);
                mProfileCopy = null;
            }
        }

        /// <summary>True if the profile is enabled and nontrivial</summary>
        public bool IsValid { get { return m_Profile != null && m_Profile.components.Count > 0; } }

        /// <summary>Called by the editor when the shared asset has been edited</summary>
        public void InvalidateCachedProfile()
        {
            var list = GetAllExtraStates<VcamExtraState>();
            for (int i = 0; i < list.Count; ++i)
                list[i].DestroyProfileCopy();
        }

        protected override void OnDestroy()
        {
            InvalidateCachedProfile();
            base.OnDestroy();
        }

        protected override void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            // Set the focus after the camera has been fully positioned.
            // GML todo: what about collider?
            if (stage == CinemachineCore.Stage.Aim)
            {
                var extra = GetExtraState<VcamExtraState>(vcam);
                if (!IsValid)
                    extra.DestroyProfileCopy();
                else
                {
                    var profile = m_Profile;

                    // Handle Follow Focus
                    if (!m_FocusTracksTarget)
                        extra.DestroyProfileCopy();
                    else
                    {
                        if (extra.mProfileCopy == null)
                            extra.CreateProfileCopy(m_Profile);
                        profile = extra.mProfileCopy;
                        DepthOfField dof;
                        if (profile.TryGet(out dof))
                        {
                            float focusDistance = m_FocusOffset;
                            if (state.HasLookAt)
                                focusDistance += (state.FinalPosition - state.ReferenceLookAt).magnitude;
                            dof.focusDistance.value = Mathf.Max(0, focusDistance);
                            profile.isDirty = true;
                        }
                    }
                    // Apply the post-processing
                    state.AddCustomBlendable(new CameraState.CustomBlendable(profile, 1));
                }
            }
        }

        static void OnCameraCut(CinemachineBrain brain)
        {
            //Debug.Log($"Camera cut to {brain.ActiveVirtualCamera.Name}");

#if CINEMACHINE_HDRP_7_0_0
            // Reset temporal effects
            var cam = brain.OutputCamera;
            if (cam != null)
            {
    #if CINEMACHINE_HDRP_7_1_0
                HDCamera hdCam = HDCamera.GetOrCreate(cam);
    #else
                HDCamera hdCam = HDCamera.GetOrCreate(cam, new XRPass());
    #endif
                hdCam.volumetricHistoryIsValid = false;
                hdCam.colorPyramidHistoryIsValid = false;
                hdCam.Reset();
            }
#endif
        }

        static void ApplyPostFX(CinemachineBrain brain)
        {
            CameraState state = brain.CurrentCameraState;
            int numBlendables = state.NumCustomBlendables;
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
                var profile = b.m_Custom as VolumeProfile;
                if (!(profile == null)) // in case it was deleted
                {
                    var v = volumes[i];
                    if (firstVolume == null)
                        firstVolume = v;
                    v.sharedProfile = profile;
                    v.isGlobal = true;
                    v.priority = float.MaxValue - (numBlendables - i - 1) * 1.0e35f;
                    v.weight = b.m_Weight;
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
        static  List<Volume> sVolumes = new List<Volume>();
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
#if CINEMACHINE_LWRP_7_0_0 && !CINEMACHINE_HDRP
                var data = brain.gameObject.GetComponent<UniversalAdditionalCameraData>();
#else
                var data = brain.gameObject.GetComponent<HDAdditionalCameraData>();
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
            // Afetr the brain pushes the state to the camera, hook in to the PostFX
            CinemachineCore.CameraUpdatedEvent.RemoveListener(ApplyPostFX);
            CinemachineCore.CameraUpdatedEvent.AddListener(ApplyPostFX);
            CinemachineCore.CameraCutEvent.RemoveListener(OnCameraCut);
            CinemachineCore.CameraCutEvent.AddListener(OnCameraCut);
        }
    }
#endif
}
