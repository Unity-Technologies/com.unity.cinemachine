using System;
using UnityEditor;

namespace Cinemachine.Editor
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
    /// Static class that manages Cinemachine Tools. It knows which tool is active,
    /// and ensures that no tools is active at the same time.
    /// The tools and editors requiring tools register/unregister themselves here for control. 
    /// </summary>
    static class CinemachineSceneToolUtility
    {   
        /// <summary>
        /// Checks whether tool is the currently active tool.
        /// </summary>
        /// <param name="tool">Tool to check.</param>
        /// <returns>True, when the tool is the active tool. False, otherwise.</returns>
        public static bool IsToolActive(CinemachineSceneTool tool)
        {
            return s_ActiveTool == tool;
        }
        static CinemachineSceneTool s_ActiveTool;

        /// <summary>
        /// Register your CinemachineSceneTool from the editor script's OnEnable function.
        /// This way CinemachineTools will know which tools to display.
        /// </summary>
        /// <param name="tool">Tool to register</param>
        public static void RegisterTool(CinemachineSceneTool tool)
        {
            s_RequiredTools[(int)tool] = true;
        }
        
        /// <summary>
        /// Unregister your CinemachineSceneTool from the editor script's OnDisable function.
        /// This way CinemachineTools will know which tools to display.
        /// </summary>
        /// <param name="tool">Tool to register</param>
        public static void UnregisterTool(CinemachineSceneTool tool)
        {
            s_RequiredTools[(int)tool] = false;
        }
        static bool[] s_RequiredTools;

        public delegate void ToolHandler(bool v);
        static ToolHandler[] s_ToolToggleSetters;
        internal static void RegisterToolToggleHandler(CinemachineSceneTool tool, ToolHandler handler)
        {
            s_ToolToggleSetters[(int)tool] = handler;
        }
        
        static ToolHandler[] s_ToolIsDisplayedSetters;
        internal static void RegisterToolIsDisplayedHandler(CinemachineSceneTool tool, ToolHandler handler)
        {
            s_ToolIsDisplayedSetters[(int)tool] = handler;
        }

        public delegate bool ToolbarHandler();
        static ToolbarHandler s_ToolBarIsDisplayed;
        internal static void RegisterToolbarIsDisplayedHandler(ToolbarHandler handler)
        {
            s_ToolBarIsDisplayed = handler;
        }

        internal static void SetTool(bool active, CinemachineSceneTool tool)
        {
            if (active)
            {
                s_ActiveTool = tool;
                EnsureCinemachineToolsAreExclusiveWithUnityTools();
            }
            else
            {
                s_ActiveTool = s_ActiveTool == tool ? CinemachineSceneTool.None : s_ActiveTool;
            }
        }

        static void EnsureCinemachineToolsAreExclusiveWithUnityTools()
        {
            var activeToolIndex = (int)s_ActiveTool;
            for (var i = 1; i < s_ToolToggleSetters.Length; ++i) // start from 1, because 0 is CinemachineSceneTool.None
            {
                if (s_ToolToggleSetters[i] != null)
                    s_ToolToggleSetters[i].Invoke(i == activeToolIndex);
            }
            if (s_ActiveTool != CinemachineSceneTool.None)
            {
                Tools.current = Tool.None; // Cinemachine tools are exclusive with unity tools
            }
        }

#if UNITY_2021_2_OR_NEWER
        static CinemachineSceneToolUtility()
        {
            s_ToolToggleSetters = new ToolHandler[Enum.GetNames(typeof(CinemachineSceneTool)).Length];
            s_ToolIsDisplayedSetters = new ToolHandler[Enum.GetNames(typeof(CinemachineSceneTool)).Length];
            s_RequiredTools = new bool[Enum.GetNames(typeof(CinemachineSceneTool)).Length];
            EditorApplication.update += () =>
            {
                var cmToolbarIsHidden = !s_ToolBarIsDisplayed();
                // if a unity tool is selected or cmToolbar is hidden, unselect our tools.
                if (Tools.current != Tool.None || cmToolbarIsHidden)
                {
                    SetTool(true, CinemachineSceneTool.None);
                }

                if (!cmToolbarIsHidden)
                {
                    // only display cm tools that are relevant for the current selection
                    for (var i = 1; i < s_ToolIsDisplayedSetters.Length; ++i) // start from 1, because 0 is CinemachineSceneTool.None
                    {
                        s_ToolIsDisplayedSetters[i].Invoke(s_RequiredTools[i]);
                    }
                }
            };
        }
#endif
    }
}