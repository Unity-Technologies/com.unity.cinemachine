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
        // public VcamData[] vcams;
        
        // public string ToJson()
        // {
        //     // {"brain_count":1,"vcam_count":3}
        //     string jsonFormat = "{";
        //     jsonFormat += "\"" + nameof(brain_count) + "\":" + brain_count + ",\"" 
        //                   + nameof(vcam_count) + "\":" + vcam_count + ",\"vcams\":[";
        //     
        //     // ,"addresses":[{"status":"current","address":"123 First Avenue","city":"Seattle","state":"WA","zip":"11111","numberOfYears":"1"},{"status":"previous","address":"456 Main Street","city":"Portland","state":"OR","zip":"22222","numberOfYears":"5"}]}
        //     for (var i = 0; i < vcams.Length; i++)
        //     {
        //         var vcam = vcams[i];
        //         jsonFormat += JsonUtility.ToJson(vcam);
        //
        //         if (i < vcams.Length - 1)
        //             jsonFormat += ",";
        //     }
        //
        //     return jsonFormat + "]}";
        // }
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
            // vcams = vcamDatas.ToArray(),
        };
        
        // TODO: server does not understand json data weirdly...
        // instead disinsect tables
        // vcam cont, brain count - table 1
        // vams rows, send separately.

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

// This should be called when entering playmode
    // [MenuItem("Test Analytics/Send Data")]
    public static void SendTestEvent()
    {
        // // First thing we should check is to see if the user has Analytics turned on or not.
        // // So we only send data if we really should send data.
        // if (!EditorAnalytics.enabled)
        //     return;
        //
        // // If Analytics is on, we need to register our event like this
        // EditorAnalytics.RegisterEventWithLimit(k_EventName, k_MaxEventsPerHour, k_MaxNumberOfElements, k_VendorKey);
        //
        // /////////////// SOME DATA /////////////////////////////
        // // Lets grab some data to send.
        // // Looking at the Analytics Data we want to send, we need to do the following
        // // Create a hashset, since we only care about unique names
        // HashSet<string> allGameObjectNames = new HashSet<string>();
        //
        // // Get all GameObjects in the scene
        // GameObject[] allGameObjects = Object.FindObjectsOfType<GameObject>();
        //
        // // Iterate over all the GameObjects and get their names
        // foreach (GameObject gameObject in allGameObjects)
        // {
        //     allGameObjectNames.Add(gameObject.name);
        // }
        //
        // // Get the amount of game objects
        // int amountOfObjects = allGameObjects.Length;
        //
        // // Get the active Editor Scene Name
        // string sceneName = EditorSceneManager.GetActiveScene().name;
        // /////////////// SOME DATA /////////////////////////////
        //
        // // Populate the analytics data with the gathered data
        // AnalyticsData data = new AnalyticsData()
        // {
        //     all_object_names = allGameObjectNames.ToArray(),
        //     amount_of_objects = amountOfObjects,
        //     scene_name = sceneName
        // };
        //
        // // Send the data to the database
        // EditorAnalytics.SendEventWithLimit(k_EventName, data);
    }
}
