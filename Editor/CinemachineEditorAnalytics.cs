using System.Collections.Generic;
using Cinemachine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class CinemachineEditorAnalytics : IPostprocessBuildWithReport
{
    const int k_MaxEventsPerHour = 1000;
    const int k_MaxNumberOfElements = 1000;

    // These following two constants will be a part of the table name
    const string k_VendorKey = "unity.cinemachine";

    // This is the struct that will be sent.
    // This is preferred to be snake_cased
    // These names are the columns
    // Available data types are the same as in our JSON https://docs.unity3d.com/Manual/JSONSerialization.html
    // From the Doc:
    //   Plain class or struct with the [Serializable] attribute.
    //   When you pass in an object to the standard Unity serializer for processing, the same rules and limitations apply as they do in the Inspector:
    //   Unity serializes fields only; and types like Dictionary<> are not supported.
    //   Unity does not support passing other types directly to the API, such as primitive types or arrays.
    //   If you need to convert those, wrap them in a class or struct of some sort.
    struct CreateEventData
    {
        public string node_created;
    }
    public static void SendCreateEvent(string name)
    {
        if (!EditorAnalytics.enabled)
            return;
        
        var data = new CreateEventData
        {
            node_created = name,
        };

        // Register our event like this
        EditorAnalytics.RegisterEventWithLimit("editor_analytics_cm_create_node", k_MaxEventsPerHour, k_MaxNumberOfElements, k_VendorKey);

        // Send the data to the database
        EditorAnalytics.SendEventWithLimit("editor_analytics_cm_create_node", data);
    }
    
    struct AddExtensionEventData
    {
        public string extension_added;
    }
    public static void SendAddExtensionEvent(string name)
    {
        if (!EditorAnalytics.enabled)
            return;
        
        var data = new AddExtensionEventData
        {
            extension_added = name,
        };

        // Register our event like this
        EditorAnalytics.RegisterEventWithLimit("editor_analytics_cm_create_extension", k_MaxEventsPerHour, k_MaxNumberOfElements, k_VendorKey);

        // Send the data to the database
        EditorAnalytics.SendEventWithLimit("editor_analytics_cm_create_extension", data);
    }
    
    struct ModifyBodyEventData
    {
        public string body_added;
    }
    struct ModifyAimEventData
    {
        public string aim_added;
    }
    public static void SendModifyAimBodyEvent(CinemachineCore.Stage stage, string name)
    {
        if (!EditorAnalytics.enabled)
            return;

        if (stage == CinemachineCore.Stage.Body)
        {
            var data = new ModifyBodyEventData
            {
                body_added = name,
            };
            
            // Register our event like this
            EditorAnalytics.RegisterEventWithLimit("editor_analytics_cm_select_follow", k_MaxEventsPerHour, k_MaxNumberOfElements, k_VendorKey);

            // Send the data to the database
            EditorAnalytics.SendEventWithLimit("editor_analytics_cm_select_follow", data);
        }
        else if (stage == CinemachineCore.Stage.Aim)
        {
            var data = new ModifyAimEventData
            {
                aim_added = name,
            };
            
            // Register our event like this
            EditorAnalytics.RegisterEventWithLimit("editor_analytics_cm_select_aim", k_MaxEventsPerHour, k_MaxNumberOfElements, k_VendorKey);

            // Send the data to the database
            EditorAnalytics.SendEventWithLimit("editor_analytics_cm_select_aim", data);
        }
    }
    
    struct ProjectData
    {
        public int brain_count;
        public int vcam_count;
    }

    struct VcamData
    {
        public string vcam_class;
        public bool has_follow_target;
        public bool has_lookat_target;
        public string blend_hint;
        public bool inherit_position;
        public string stand_by_update;
        public string mode_overwrite;
        public string[] components;
        public string[] extensions;
    }
    
    public int callbackOrder { get; }
    public void OnPostprocessBuild(BuildReport report)
    {
        SendAnalyticsOnBuild();
    }

    public static void SendAnalyticsOnBuild()
    {
        if (!EditorAnalytics.enabled)
            return;
        
        // collect num of vcams
        var cinemachineCore = CinemachineCore.Instance;
        var vcamCount = cinemachineCore.VirtualCameraCount;
        List<VcamData> vcamDatas = new List<VcamData>();
        for (int i = 0; i < vcamCount; ++i)
        {
            var vcamBase = cinemachineCore.GetVirtualCamera(i);
            if (vcamBase == null) continue;
            
            var vcamExtensions = new List<string>();
            var extensions = vcamBase.mExtensions;
            if (extensions != null)
            {
                foreach (var extension in extensions)
                {
                    vcamExtensions.Add(extension.GetType().ToString());
                }
            }

            Debug.Log("vcamBase.GetType()="+vcamBase.GetType());
            var vcam = vcamBase as CinemachineVirtualCamera;
            if (vcam != null)
            {
                var vcamComponents = new List<string>();
                var cmComps = vcam.GetComponentPipeline();
                if (cmComps != null)
                {
                    foreach (var cmComp in cmComps)
                    {
                        vcamComponents.Add(cmComp.GetType().ToString());
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
                    components = vcamComponents.ToArray(),
                    extensions = vcamExtensions.ToArray(),
                });
            }
            else
            {
                vcamDatas.Add(new VcamData
                {
                    vcam_class =
                        vcamBase.GetType()
                            .ToString(), // todo: if we want this, then I need to write a vcam to vcamData converted for each class (VirtualCamera, Freelook, Statedriven, Mixing...)
                    has_follow_target = vcamBase.Follow != null,
                    has_lookat_target = vcamBase.LookAt != null,
                    blend_hint = "",
                    inherit_position = false,
                    stand_by_update = vcamBase.m_StandbyUpdate.ToString(),
                    mode_overwrite = "",
                    components = null,
                    extensions = vcamExtensions.ToArray(),
                });
            }
        }

        var projectData = new ProjectData
        {
            brain_count = cinemachineCore.BrainCount,
            vcam_count = cinemachineCore.VirtualCameraCount,
        };
        

        // Register our event like this
        EditorAnalytics.RegisterEventWithLimit("vcams_at_build", k_MaxEventsPerHour, k_MaxNumberOfElements, k_VendorKey);

        // Send the data to the database
        EditorAnalytics.SendEventWithLimit("vcams_at_build", projectData);


        // Register our event like this
        EditorAnalytics.RegisterEventWithLimit("vcam_at_build", k_MaxEventsPerHour, k_MaxNumberOfElements, k_VendorKey);
        
        // Send the data to the database
        foreach (var vcamData in vcamDatas)
        {
            EditorAnalytics.SendEventWithLimit("vcam_at_build", vcamData);
        }
    }
}
