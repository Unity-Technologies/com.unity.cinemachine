using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
#if UNITY_2023_2_OR_NEWER
using UnityEngine.Analytics;
#endif

namespace Unity.Cinemachine.Editor
{
    [InitializeOnLoad]
    static class CinemachineEditorAnalytics
    {
        const int k_MaxEventsPerHour = 360;
        const int k_MaxNumberOfElements = 1000;
        const string k_VendorKey = "unity.cinemachine";
        const string k_CreateVcamEventName = "cm_create_vcam";
        const string k_VcamsOnPlayEventName = "cm_vcams_on_play";

        // register an event handler when the class is initialized
        static CinemachineEditorAnalytics()
        {
            EditorApplication.playModeStateChanged += SendAnalyticsOnPlayEnter;
        }

#if UNITY_2023_2_OR_NEWER
        [AnalyticInfo(eventName: k_CreateVcamEventName, vendorKey: k_VendorKey,
            maxEventsPerHour:k_MaxEventsPerHour, maxNumberOfElements:k_MaxNumberOfElements)]
        class CreateEventAnalytic : IAnalytic
        {
            public string vcam_created; // vcam created from Create -> Cinemachine menu

            public bool TryGatherData(out IAnalytic.IData data, out Exception error)
            {
                error = null;
                data = new CreateEventData { vcam_created = vcam_created};
                return true;
            }
        }

        [Serializable] class CreateEventData : IAnalytic.IData
#else
        struct CreateEventData
#endif
        {
            public string vcam_created; // vcam created from Create -> Cinemachine menu
        }

        /// <summary>
        /// Send analytics event when using Create -> Cinemachine menu
        /// </summary>
        /// <param name="name">Name of the vcam created</param>
        public static void SendCreateEvent(string name)
        {
            if (!EditorAnalytics.enabled)
                return;

#if UNITY_2023_2_OR_NEWER
            EditorAnalytics.SendAnalytic(new CreateEventAnalytic { vcam_created = name });
#else
            var data = new CreateEventData { vcam_created = name };

            // Register our event
            EditorAnalytics.RegisterEventWithLimit(k_CreateVcamEventName,
                k_MaxEventsPerHour, k_MaxNumberOfElements, k_VendorKey);

            // Send the data to the database
            EditorAnalytics.SendEventWithLimit(k_CreateVcamEventName, data);
#endif
        }

#if UNITY_2023_2_OR_NEWER
        [AnalyticInfo(eventName: k_VcamsOnPlayEventName, vendorKey: k_VendorKey,
            maxEventsPerHour:k_MaxEventsPerHour, maxNumberOfElements:k_MaxNumberOfElements)]
        class PlayModeEventAnalytic : IAnalytic
        {
            public bool TryGatherData(out IAnalytic.IData data, out Exception error)
            {
                error = null;
                var projectData = new ProjectData();
                CollectOnPlayEnterData(ref projectData);
                data = projectData;
                return true;
            }
        }
        [Serializable]
        class ProjectData : IAnalytic.IData
        {
            public int brain_count;
            public int vcam_count;
            public int cam_count;
            public VcamData[] vcams;
            public float time_elapsed;
        }
#else
        [Serializable]
        struct ProjectData
        {
            public int brain_count;
            public int vcam_count;
            public int cam_count;
            public List<VcamData> vcams;
            public float time_elapsed;
        }
#endif

        /// <summary>
        /// Send analytics event when using entering playmode
        /// </summary>
        /// <param name="state">State change to detect entering playmode</param>
        static void SendAnalyticsOnPlayEnter(PlayModeStateChange state)
        {
            // Only send analytics if it is enabled
            if (!EditorAnalytics.enabled)
                return;

            // Only send data when entering playmode
            if (state != PlayModeStateChange.EnteredPlayMode)
                return;

#if UNITY_2023_2_OR_NEWER
            EditorAnalytics.SendAnalytic(new PlayModeEventAnalytic());
#else
            var projectData = new ProjectData();
            CollectOnPlayEnterData(ref projectData);

            // Register our event
            EditorAnalytics.RegisterEventWithLimit(k_VcamsOnPlayEventName,
                k_MaxEventsPerHour, k_MaxNumberOfElements, k_VendorKey);

            // Send the data to the database
            EditorAnalytics.SendEventWithLimit(k_VcamsOnPlayEventName, projectData);
#endif
        }


        /// <summary>
        /// Send analytics event when using entering playmode
        /// </summary>
        /// <param name="state">State change to detect entering playmode</param>
        static void CollectOnPlayEnterData(ref ProjectData projectData)
        {
            var startTime = Time.realtimeSinceStartup;
            var vcamCount = CinemachineCore.VirtualCameraCount;
            var vcamDatas = new List<VcamData>();

            // collect data from all vcams
            for (int i = 0; i < vcamCount; ++i)
            {
                var vcamBase = CinemachineCore.GetVirtualCamera(i);
                CollectVcamData(vcamBase, i.ToString(), ref vcamDatas);
            }

            projectData.brain_count = CinemachineBrain.ActiveBrainCount;
            projectData.vcam_count = CinemachineCore.VirtualCameraCount;
            projectData.cam_count = Camera.allCamerasCount;
#if UNITY_2023_2_OR_NEWER
            projectData.vcams = vcamDatas.ToArray();
#else
            projectData.vcams = vcamDatas;
#endif
            projectData.time_elapsed = Time.realtimeSinceStartup - startTime;
        }

        static void CollectVcamData(CinemachineVirtualCameraBase vcamBase, string id, ref List<VcamData> vcamDatas)
        {
            if (vcamBase == null)
                return;

            var vcamData = new VcamData(id, vcamBase);

            // VirtualCamera
            var vcam = vcamBase as CinemachineCamera;
            if (vcam != null)
            {
                vcamData.SetTransitionsAndLens(vcam.BlendHint, vcam.Lens);
                vcamData.SetComponents(vcam.GetComponents<CinemachineComponentBase>());
                vcamDatas.Add(vcamData);
                return;
            }

            var vcamChildren = vcamBase.GetComponentsInChildren<CinemachineVirtualCameraBase>();
            for (var c = 1; c < vcamChildren.Length; c++)
            {
                if (vcamChildren[c].ParentCamera == (ICinemachineCamera)vcamBase)
                    CollectVcamData(vcamChildren[c], id + "." + c, ref vcamDatas);
            }
        }

        [Serializable]
        struct VcamData
        {
            public string id;
            public string vcam_class;
            public bool has_follow_target;
            public bool has_lookat_target;
            public string blend_hint;
            public bool inherit_position;
            public string standby_update;
            public string mode_overwrite;
            public string body_component;
            public string aim_component;
            public string noise_component;
            public int custom_component_count;
            public string[] extensions;
            public int custom_extension_count;

            public VcamData(string id, CinemachineVirtualCameraBase vcamBase) : this()
            {
                var _ = 0;

                this.id = id;
                vcam_class = GetTypeName(vcamBase.GetType(), ref _);
                has_follow_target = vcamBase.Follow != null;
                has_lookat_target = vcamBase.LookAt != null;
                blend_hint = "";
                inherit_position = false;
                standby_update = vcamBase.StandbyUpdate.ToString();
                mode_overwrite = "";
                body_component = "";
                aim_component = "";
                noise_component = "";
                custom_component_count = 0;
                custom_extension_count = 0;
                var vcamExtensions = vcamBase.Extensions;
                if (vcamExtensions != null)
                {
                    extensions = new string[vcamExtensions.Count];
                    for (var i = 0; i < vcamExtensions.Count; i++)
                    {
                        extensions[i] = (GetTypeName(vcamExtensions[i].GetType(), ref custom_extension_count));
                    }
                }
                else
                {
                    extensions = Array.Empty<string>();
                }
            }

            public void SetTransitionsAndLens(CinemachineCore.BlendHints hints, LensSettings lens)
            {
                blend_hint = hints.ToString();
                inherit_position = (hints & CinemachineCore.BlendHints.InheritPosition) != 0;
                mode_overwrite = lens.ModeOverride.ToString();
            }

            public void SetComponents(CinemachineComponentBase[] cmComps)
            {
                custom_component_count = 0;
                body_component = aim_component = noise_component = "";
                if (cmComps != null)
                {
                    for (var i = 0; i < cmComps.Length; i++)
                    {
                        if (cmComps[i] == null)
                            continue;
                        var componentName = GetTypeName(cmComps[i].GetType(), ref custom_component_count);
                        switch (cmComps[i].Stage)
                        {
                            case CinemachineCore.Stage.Body:
                                body_component = componentName;
                                break;
                            case CinemachineCore.Stage.Aim:
                                aim_component = componentName;
                                break;
                            case CinemachineCore.Stage.Noise:
                                noise_component = componentName;
                                break;
                            default:
                                break;
                        }
                    }
                }
            }

            static string GetTypeName(Type type, ref int customTypeCount)
            {
                if (typeof(CinemachineBrain).Assembly != type.Assembly)
                {
                    ++customTypeCount;
                    return "Custom";
                }
                return type.Name;
            }
        }
    }
}
