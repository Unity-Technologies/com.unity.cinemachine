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

        public delegate void ToolValueSet(bool v);
        static ToolValueSet[] s_ToolSetHandlers;
        public static void SetToolHandler(CinemachineSceneTool tool, ToolValueSet handler)
        {
            s_ToolSetHandlers[(int)tool] = handler;
        }

        public static void SetTool(bool active, CinemachineSceneTool tool)
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
            for (var i = 1; i < s_ToolSetHandlers.Length; ++i) // start from 1, because 0 is CinemachineSceneTool.None
            {
                if (s_ToolSetHandlers[i] != null)
                    s_ToolSetHandlers[i].Invoke(i == activeToolIndex);
            }
            if (s_ActiveTool != CinemachineSceneTool.None)
            {
                Tools.current = Tool.None; // Cinemachine tools are exclusive with unity tools
            }
        }

        static CinemachineSceneToolUtility()
        {
            s_ToolSetHandlers = new ToolValueSet[Enum.GetNames(typeof(CinemachineSceneTool)).Length];
            EditorApplication.update += EnsureUnityToolsAreExclusiveWithCinemachineTools;
        }
        
        static void EnsureUnityToolsAreExclusiveWithCinemachineTools()
        {
            if (Tools.current != Tool.None)
            {
                SetTool(true, CinemachineSceneTool.None);
            }
        }
    }
}