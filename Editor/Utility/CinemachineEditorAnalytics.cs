using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Cinemachine.Editor
{
    [InitializeOnLoad]
    static class CinemachineEditorAnalytics
    {
        const int k_MaxEventsPerHour = 1000;
        const int k_MaxNumberOfElements = 1000;
        const string k_VendorKey = "unity.cinemachine";
        
        // register an event handler when the class is initialized
        static CinemachineEditorAnalytics()
        {
            EditorApplication.playModeStateChanged += SendAnalyticsOnPlayEnter;
        }

        /// <summary>
        /// Send analytics event when using Create -> Cinemachine menu
        /// </summary>
        /// <param name="name">Name of the vcam created</param>
        public static void SendCreateEvent(string name)
        {
            if (!EditorAnalytics.enabled)
                return;

            var data = new CreateEventData
            {
                vcam_created = name,
            };

            // Register our event
            EditorAnalytics.RegisterEventWithLimit("cm_create_vcam", 
                k_MaxEventsPerHour, k_MaxNumberOfElements, k_VendorKey);

            // Send the data to the database
            EditorAnalytics.SendEventWithLimit("cm_create_vcam", data);
        }

        struct CreateEventData
        {
            public string vcam_created; // vcam created from Create -> Cinemachine menu
        }

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

            var startTime = Time.realtimeSinceStartup;

            var cinemachineCore = CinemachineCore.Instance;
            var vcamCount = cinemachineCore.VirtualCameraCount;
            var vcamDatas = new List<VcamData>();

            // collect data from all vcams
            for (int i = 0; i < vcamCount; ++i)
            {
                var vcamBase = cinemachineCore.GetVirtualCamera(i);
                CollectData(vcamBase, i.ToString(), ref vcamDatas);
            }

            var projectData = new ProjectData
            {
                brain_count = cinemachineCore.BrainCount,
                vcam_count = cinemachineCore.VirtualCameraCount,
                cam_count = Camera.allCamerasCount,
                vcams = vcamDatas,
                time_elapsed = Time.realtimeSinceStartup - startTime,
            };

            // Register our event
            EditorAnalytics.RegisterEventWithLimit("cm_vcams_on_play", 
                k_MaxEventsPerHour, k_MaxNumberOfElements, k_VendorKey);

            // Send the data to the database
            EditorAnalytics.SendEventWithLimit("cm_vcams_on_play", projectData);
        }

        static void CollectData(CinemachineVirtualCameraBase vcamBase, string id, ref List<VcamData> vcamDatas)
        {
            if (vcamBase == null) return;

            var vcam = vcamBase as CinemachineVirtualCamera;
            if (vcam != null)
            {
                vcamDatas.Add(ConvertVcamToVcamData(vcam, id));
            }
            else // Composite vcam (Freelook, Mixing, Statedriven, Clearshot):
            {
                GetExtensions(vcamBase, out List<string> vcamExtensions, out int customExtensionCount);

                vcamDatas.Add(new VcamData
                {
                    id = id,
                    vcam_class = GetTypeName(vcamBase.GetType()),
                    has_follow_target = vcamBase.Follow != null,
                    has_lookat_target = vcamBase.LookAt != null,
                    blend_hint = "",
                    inherit_position = false,
                    standby_update = vcamBase.m_StandbyUpdate.ToString(),
                    mode_overwrite = "",
                    body_component = "",
                    aim_component = "",
                    noise_component = "",
                    custom_component_count = 0,
                    extensions = vcamExtensions.ToArray(),
                    custom_extension_count = customExtensionCount,
                });

                var vcamChildren = 
                    vcamBase.GetComponentsInChildren<CinemachineVirtualCameraBase>();
                for (var c = 1; c < vcamChildren.Length; c++)
                {
                    if ((CinemachineVirtualCameraBase)vcamChildren[c].ParentCamera == vcamBase)
                        CollectData(vcamChildren[c], id + "." + c, ref vcamDatas);
                }
            }
        }

        [Serializable]
        struct ProjectData
        {
            public int brain_count;
            public int vcam_count;
            public int cam_count;
            public List<VcamData> vcams;
            public float time_elapsed;
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
        }

        static readonly VcamData k_NullData = new VcamData { vcam_class = "null" };
        static VcamData ConvertVcamToVcamData(in CinemachineVirtualCamera vcam, string id)
        {
            if (vcam == null) return k_NullData;

            // collect extensions on vcam
            GetExtensions(vcam, out List<string> vcamExtensions, out int customExtensionCount);

            // collect components on vcam
            GetComponents(vcam, out string bodyComponent, out string aimComponent, 
                out string noiseComponent, out int customComponentCount);

            return new VcamData
            {
                id = id,
                vcam_class = GetTypeName(vcam.GetType()),
                has_follow_target = vcam.Follow != null,
                has_lookat_target = vcam.LookAt != null,
                blend_hint = vcam.m_Transitions.m_BlendHint.ToString(),
                inherit_position = vcam.m_Transitions.m_InheritPosition,
                standby_update = vcam.m_StandbyUpdate.ToString(),
                mode_overwrite = vcam.m_Lens.ModeOverride.ToString(),
                body_component = bodyComponent,
                aim_component = aimComponent,
                noise_component = noiseComponent,
                custom_component_count = customComponentCount,
                extensions = vcamExtensions.ToArray(),
                custom_extension_count = customExtensionCount,
            };
        }

        static void GetExtensions(CinemachineVirtualCameraBase vcamBase, 
            out List<string> vcamExtensions, out int customCount)
        {
            customCount = 0;

            // collect extensions on vcam
            vcamExtensions = new List<string>();
            var extensions = vcamBase.mExtensions;
            if (extensions != null)
            {
                for (var i = 0; i < extensions.Count; i++)
                {
                    vcamExtensions.Add(GetTypeName(extensions[i].GetType(), ref customCount));
                }
            }
        }

        static void GetComponents(CinemachineVirtualCamera vcam,
            out string body, out string aim, out string noise, out int customCount)
        {
            customCount = 0;
            body = aim = noise = "";
            var cmComps = vcam.GetComponentPipeline();
            if (cmComps != null)
            {
                for (var i = 0; i < cmComps.Length; i++)
                {
                    var componentName = GetTypeName(cmComps[i].GetType(), ref customCount);
                    switch (cmComps[i].Stage)
                    {
                        case CinemachineCore.Stage.Body:
                            body = componentName;
                            break;
                        case CinemachineCore.Stage.Aim:
                            aim = componentName;
                            break;
                        case CinemachineCore.Stage.Noise:
                            noise = componentName;
                            break;
                        default:
                            break;
                    }
                }
            }
        }
        
        static string GetTypeName(Type type)
        {
            var _ = 0;
            return GetTypeName(type, ref _);
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
