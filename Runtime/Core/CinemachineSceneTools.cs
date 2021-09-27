using System;
using UnityEditor;

namespace Cinemachine.Utility
{
    public enum CinemachineSceneTool
    {
        None,
        FoV,
        FarNearClip,
        FollowOffset,
        TrackedObjectOffset,
    };
    
    /// <summary>
    /// Hack class that manages Cinemachine Tools. It knows which tool is active,
    /// and ensures that no tools is active at the same time.
    /// The tools register themselves here for control. 
    /// </summary>
    static class CinemachineSceneToolUtility
    {   
        static CinemachineSceneTool s_ActiveTool;
        public static bool IsToolActive(CinemachineSceneTool tool)
        {
            return s_ActiveTool == tool;
        }

        public delegate void ToolHandler(bool v);
        static ToolHandler[] s_ToolValueSetters;

        internal static void SetToolValueSetter(CinemachineSceneTool tool, ToolHandler setter)
        {
            s_ToolValueSetters[(int)tool] = setter;
        }

        static ToolHandler[] s_ToolEnableSetters;

        internal static void SetToolEnableSetter(CinemachineSceneTool tool, ToolHandler setter)
        {
            s_ToolEnableSetters[(int)tool] = setter;
        }

        static ToolHandler s_ToolbarEnableSetter;

        internal static void SetToolbarEnableSetter(ToolHandler setter)
        {
            s_ToolbarEnableSetter = setter;
        }

        internal static void SetTool(bool active, CinemachineSceneTool tool)
        {
            if (active)
            {
                s_ActiveTool = tool;
                EnsureToolsAreExclusive();
            }
            else
            {
                s_ActiveTool = s_ActiveTool == tool ? CinemachineSceneTool.None : s_ActiveTool;
            }
        }

        static void EnsureToolsAreExclusive()
        {
            var activeToolIndex = (int)s_ActiveTool;
            for (var i = 1; i < s_ToolValueSetters.Length; ++i) // start from 1, because 0 is CinemachineSceneTool.None
            {
                if (s_ToolValueSetters[i] != null)
                    s_ToolValueSetters[i].Invoke(i == activeToolIndex);
            }
            if (s_ActiveTool != CinemachineSceneTool.None)
            {
                Tools.current = Tool.None; // Cinemachine tools are exclusive with unity tools
            }
        }

        static CinemachineSceneToolUtility()
        {
            s_ToolValueSetters = new ToolHandler[Enum.GetNames(typeof(CinemachineSceneTool)).Length];
            s_ToolEnableSetters = new ToolHandler[Enum.GetNames(typeof(CinemachineSceneTool)).Length];
            EditorApplication.update += EnsureUnityToolsAreExclusiveWithCinemachineTools;
        }
        
        static void EnsureUnityToolsAreExclusiveWithCinemachineTools()
        {
            if (Tools.current != Tool.None)
            {
                SetTool(true, CinemachineSceneTool.None);
            }

            var vcamIsSelected = false;
            foreach (var go in Selection.gameObjects)
            {
                var vcams = go.GetComponents<CinemachineVirtualCameraBase>();
                if (vcams.Length > 0)
                {
                    vcamIsSelected = true;
                    break;
                }
            }
            
            s_ToolbarEnableSetter?.Invoke(vcamIsSelected);

            // TODO: Hide tools that are irrelevant for the current selection
            // foreach (var enabler in s_ToolEnableSetters)
            // {
            //     enabler?.Invoke(vcamIsSelected);
            // }
        }
    }
}