using System;
using System.Collections.Generic;
using Cinemachine;
using UnityEditor;
using UnityEngine;

public static class CinemachineEditorAnalytics
{
    const int k_MaxEventsPerHour = 1000;
    const int k_MaxNumberOfElements = 1000;

    // These following two constants will be a part of the table name
    const string k_VendorKey = "unity.cinemachine";

    private struct CreateEventData
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
        
        var data = new CreateEventData
        {
            vcam_created = name,
        };
        
        // Register our event like this
        EditorAnalytics.RegisterEventWithLimit("cm_create_vcam", k_MaxEventsPerHour, k_MaxNumberOfElements, k_VendorKey);

        // Send the data to the database
        EditorAnalytics.SendEventWithLimit("cm_create_vcam", data);
    }

    private const string k_customStr = "Custom";

    [Serializable]
    private struct ProjectData
    {
        public int brain_count;
        public int vcam_count;
        public int cam_count;
        public List<VcamData> vcams;
        public float time_elapsed;
    }

    [Serializable]
    private struct VcamData
    {
        public string vcam_class;
        public bool has_follow_target;
        public bool has_lookat_target;
        public string blend_hint;
        public bool inherit_position;
        public string stand_by_update;
        public string mode_overwrite;
        public string body_component;
        public string aim_component;
        public string noise_component;
        public int custom_component_count;
        public string[] extensions;
        public int custom_extension_count;
    }
    
    // register an event handler when the class is initialized
    static CinemachineEditorAnalytics()
    {
        EditorApplication.playModeStateChanged += SendAnalyticsOnPlayEnter;
    }

    public static void SendAnalyticsOnPlayEnter(PlayModeStateChange state)
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
        int customExtensionCount = 0;
        
        // collect data from all vcams
        for (int i = 0; i < vcamCount; ++i)
        {
            var vcamBase = cinemachineCore.GetVirtualCamera(i);
            if (vcamBase == null) continue;
            
            // collect extensions on vcam
            var vcamExtensions = new List<string>();
            var extensions = vcamBase.mExtensions;
            if (extensions != null)
            {
                foreach (var extension in extensions)
                {
                    string extensionName = extension.GetType().ToString();
                    switch (extensionName)
                    {
                        case "CinemachineCameraOffset":
                        case "Cinemachine3rdPersonAim":
                        case "CinemachineCollider":
                        case "CinemachineConfiner":
                        case "CinemachineConfiner2D":
                        case "CinemachineFollowZoom":
                        case "CinemachineRecomposer":
                        case "CinemachineStoryboard":
                            break;
                        default:
                            extensionName = k_customStr; // hide users extension's name
                            customExtensionCount++;
                            break;
                    }
                    vcamExtensions.Add(extensionName);
                }
            }

            var vcam = vcamBase as CinemachineVirtualCamera;
            
            // collect components on vcam
            int customComponentCount = 0;
            string bodyComponent = "", aimComponent = "", noiseComponent = "";
            if (vcam != null)
            {
                var cmComps = vcam.GetComponentPipeline();
                if (cmComps != null)
                {
                    foreach (var cmComp in cmComps)
                    {
                        var type = cmComp.GetType().ToString();
                        int indexOf = type.IndexOf("Cinemachine.");
                        if (indexOf >= 0)
                        {
                            type = type.Substring("Cinemachine.".Length);
                        }
                        switch (cmComp.Stage)
                        {
                            case CinemachineCore.Stage.Body:
                                switch (type) // TODO: string to consts
                                {
                                    // built in
                                    case "Cinemachine3rdPersonFollow":
                                    case "CinemachineFramingTransposer":
                                    case "CinemachineHardLockToTarget":
                                    case "CinemachineOrbitalTransposer":
                                    case "CinemachineTrackedDolly":
                                    case "CinemachineTransposer":
                                        bodyComponent = type;
                                        break;
                                    default:
                                        bodyComponent = k_customStr; // hide users component's name
                                        customComponentCount++;
                                        break;
                                }
                                break;
                            case CinemachineCore.Stage.Aim:
                                switch (type)
                                {
                                    case "CinemachineComposer":
                                    case "CinemachineGroupComposer":
                                    case "CinemachineHardLookAt":
                                    case "CinemachinePOV":
                                    case "CinemachineSameAsFollowTarget":
                                        aimComponent = type;
                                        break;
                                    default:
                                        aimComponent = k_customStr;  // hide users component's name
                                        customComponentCount++;
                                        break;
                                }
                                break;
                            case CinemachineCore.Stage.Noise:
                                switch (type) // TODO: string to consts
                                {
                                    case "CinemachineBasicMultiChannelPerlin":
                                        noiseComponent = type;
                                        break;
                                    default:
                                        noiseComponent = k_customStr;  // hide users component's name
                                        customComponentCount++;
                                        break;
                                }
                                break;
                            default:
                                customComponentCount++;
                                break;
                        }
                    }
                }

                vcamDatas.Add(new VcamData
                {
                    vcam_class =
                        vcamBase.GetType()
                            .ToString(), // todo: if we want this, then I need to write a vcam to vcamData converted for each class (VirtualCamera, Freelook, Statedriven, Mixing...)
                    has_follow_target = vcamBase.Follow != null,
                    has_lookat_target = vcamBase.LookAt != null,
                    blend_hint = vcam.m_Transitions.m_BlendHint.ToString(),
                    inherit_position = vcam.m_Transitions.m_InheritPosition,
                    stand_by_update = vcamBase.m_StandbyUpdate.ToString(),
                    mode_overwrite = vcam.m_Lens.ModeOverride.ToString(),
                    body_component = bodyComponent,
                    aim_component = aimComponent,
                    noise_component = noiseComponent,
                    custom_component_count = customComponentCount,
                    extensions = vcamExtensions.ToArray(),
                    custom_extension_count = customExtensionCount,
                });
            }
            else
            {
                vcamDatas.Add(new VcamData
                {
                    vcam_class =
                        vcamBase.GetType().ToString(),
                    has_follow_target = vcamBase.Follow != null,
                    has_lookat_target = vcamBase.LookAt != null,
                    blend_hint = "",
                    inherit_position = false,
                    stand_by_update = vcamBase.m_StandbyUpdate.ToString(),
                    mode_overwrite = "",
                    body_component = "",
                    aim_component = "",
                    noise_component = "",
                    custom_component_count = 0,
                    extensions = vcamExtensions.ToArray(),
                    custom_extension_count = customExtensionCount,
                });
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

        var json = JsonUtility.ToJson(projectData);
        Debug.Log(json);
        // Send the data to the database
        EditorAnalytics.SendEventWithLimit("cm_vcams_on_play", projectData);
    }
}
