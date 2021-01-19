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

        static Assembly s_CinemachineAssembly;

        // register an event handler when the class is initialized
        static CinemachineEditorAnalytics()
        {
            EditorApplication.playModeStateChanged += SendAnalyticsOnPlayEnter;

            s_CinemachineAssembly = typeof(CinemachineBrain).Assembly;
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

            // Register our event like this
            EditorAnalytics.RegisterEventWithLimit("cm_create_vcam", k_MaxEventsPerHour, k_MaxNumberOfElements, k_VendorKey);

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
                if (vcamBase == null) continue;

                var vcam = vcamBase as CinemachineVirtualCamera;
                if (vcam != null)
                {
                    vcamDatas.Add(ConvertVcamToVcamData(vcam, i.ToString()));
                }
                else // Composite vcam (Freelook, Mixing, Statedriven, Clearshot):
                {
                    GetExtensions(vcamBase, out List<string> vcamExtensions, out int customExtensionCount);

                    string id = i.ToString();
                    vcamDatas.Add(new VcamData
                    {
                        id = id,
                        vcam_class = GetVcamTypeName(vcamBase),
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

                    var vcamChildren = vcamBase.GetComponentsInChildren<CinemachineVirtualCamera>();
                    for (var c = 0; c < vcamChildren.Length; c++)
                    {
                        vcamDatas.Add(ConvertVcamToVcamData(vcamChildren[c], id + "." + c));
                    }
                }
            }

            var projectData = new ProjectData
            {
                brain_count = cinemachineCore.BrainCount,
                vcam_count = cinemachineCore.VirtualCameraCount,
                cam_count = Camera.allCamerasCount,
                vcams = vcamDatas,
                time_elapsed = Time.realtimeSinceStartup - startTime,
            };

            // Register our event like this
            EditorAnalytics.RegisterEventWithLimit("cm_vcams_on_play", k_MaxEventsPerHour, k_MaxNumberOfElements, k_VendorKey);

            // Send the data to the database
            EditorAnalytics.SendEventWithLimit("cm_vcams_on_play", projectData);
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
        const string k_CustomStr = "Custom"; // used for hiding user created class names
        static VcamData ConvertVcamToVcamData(in CinemachineVirtualCamera vcam, string id)
        {
            if (vcam == null) return k_NullData;

            // collect extensions
            GetExtensions(vcam, out List<string> vcamExtensions, out int customExtensionCount);

            // collect components on vcam
            int customComponentCount = 0;
            string bodyComponent = "", aimComponent = "", noiseComponent = "";
            var cmComps = vcam.GetComponentPipeline();
            if (cmComps != null)
            {
                for (var i = 0; i < cmComps.Length; i++)
                {
                    var cmComp = cmComps[i];
                    switch (cmComp.Stage)
                    {
                        case CinemachineCore.Stage.Body:
                            bodyComponent = GetTypeName(cmComp.GetType(), ref customComponentCount);
                            break;
                        case CinemachineCore.Stage.Aim:
                            aimComponent = GetTypeName(cmComp.GetType(), ref customComponentCount);
                            break;
                        case CinemachineCore.Stage.Noise:
                            noiseComponent = GetTypeName(cmComp.GetType(), ref customComponentCount);
                            break;
                        default:
                            break;
                    }
                }
            }

            return new VcamData
            {
                id = id,
                vcam_class = GetVcamTypeName(vcam),
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

        static void GetExtensions(CinemachineVirtualCameraBase vcamBase, out List<string> vcamExtensions, out int customExtensionCount)
        {
            customExtensionCount = 0;

            // collect extensions on vcam
            vcamExtensions = new List<string>();
            var extensions = vcamBase.mExtensions;
            if (extensions != null)
            {
                for (var i = 0; i < extensions.Count; i++)
                {
                    vcamExtensions.Add(GetTypeName(extensions[i].GetType(), ref customExtensionCount));
                }
            }
        }

        static string GetVcamTypeName(CinemachineVirtualCameraBase vcamBase)
        {
            var type = vcamBase.GetType();
            return s_CinemachineAssembly == type.Assembly ? type.Name : k_CustomStr;
        }

        static string GetTypeName(Type type, ref int customTypeCount)
        {
            if (typeof(CinemachineBrain).Assembly != type.Assembly)
            {
                ++customTypeCount;
                return k_CustomStr;
            }
            return type.Name;
        }
    }
}
