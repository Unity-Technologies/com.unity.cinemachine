using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEditor;

namespace Cinemachine.Editor
{
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
        public static bool IsToolActive(Type tool)
        {
            return s_ActiveTool == tool;
        }
        static Type s_ActiveTool;

        /// <summary>
        /// Register your Type from the editor script's OnEnable function.
        /// This way CinemachineTools will know which tools to display.
        /// </summary>
        /// <param name="tool">Tool to register</param>
        public static void RegisterTool(Type tool)
        {
            s_RequiredTools.Add(tool);
        }
        
        /// <summary>
        /// Unregister your Type from the editor script's OnDisable function.
        /// This way CinemachineTools will know which tools to display.
        /// </summary>
        /// <param name="tool">Tool to register</param>
        public static void UnregisterTool(Type tool)
        {
            s_RequiredTools.Remove(tool);
        }
        static HashSet<Type> s_RequiredTools;

        public delegate void ToolHandler(bool v);
        struct CinemachineSceneToolDelegates
        {
            public ToolHandler ToggleSetter;
            public ToolHandler IsDisplayedSetter;
        }
        static SortedDictionary<Type, CinemachineSceneToolDelegates> s_Tools;

        internal static void RegisterToolHandlers(Type tool, ToolHandler toggleSetter, ToolHandler isDisplayedSetter)
        {
            if (s_Tools.ContainsKey(tool))
            {
                s_Tools.Remove(tool);
            }
            
            s_Tools.Add(tool, new CinemachineSceneToolDelegates
            {
                ToggleSetter = toggleSetter,
                IsDisplayedSetter = isDisplayedSetter,
            });
        }

        public delegate bool ToolbarHandler();
        static ToolbarHandler s_ToolBarIsDisplayed;
        internal static void RegisterToolbarIsDisplayedHandler(ToolbarHandler handler)
        {
            s_ToolBarIsDisplayed = handler;
        }

        internal static void SetTool(bool active, Type tool)
        {
            if (active)
            {
                s_ActiveTool = tool;
                EnsureCinemachineToolsAreExclusiveWithUnityTools();
            }
            else
            {
                s_ActiveTool = s_ActiveTool == tool ? null : s_ActiveTool;
            }
        }

        static void EnsureCinemachineToolsAreExclusiveWithUnityTools()
        {
            foreach (var (key, value) in s_Tools)
            {
                value.ToggleSetter(key == s_ActiveTool);
            }
            if (s_ActiveTool != null)
            {
                Tools.current = Tool.None; // Cinemachine tools are exclusive with unity tools
            }
        }

#if UNITY_2021_2_OR_NEWER
        static CinemachineSceneToolUtility()
        {
            s_Tools = new SortedDictionary<Type, CinemachineSceneToolDelegates>(Comparer<Type>.Create(
                (x, y) => x.ToString().CompareTo(y.ToString())));
            s_RequiredTools = new HashSet<Type>();
            
            EditorApplication.update += () =>
            {
                var cmToolbarIsHidden = !s_ToolBarIsDisplayed();
                // if a unity tool is selected or cmToolbar is hidden, unselect our tools.
                if (Tools.current != Tool.None || cmToolbarIsHidden)
                {
                    SetTool(true, null);
                }

                if (!cmToolbarIsHidden)
                {
                    // only display cm tools that are relevant for the current selection
                    foreach (var (key, value) in s_Tools)
                    {
                        value.IsDisplayedSetter(s_RequiredTools.Contains(key));
                    }
                }
            };
        }
#endif
    }
}