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
        public static bool IsToolOn(CinemachineSceneTool tool)
        {
            return s_ActiveTool == tool;
        }

        public delegate void ToolValueSet(bool v);


        static ToolValueSet[] s_ToolSetHandlers;
        public static void SetHandler(CinemachineSceneTool tool, ToolValueSet handler)
        {
            s_ToolSetHandlers[(int)tool] = handler;
        }

        public static void ToolValueChanged(UnityEngine.UIElements.ChangeEvent<bool> evt, CinemachineSceneTool tool)
        {
            // TODO: generic tool change
        }
        
        public static void FovToolSelectionToolSelection(UnityEngine.UIElements.ChangeEvent<bool> evt)
        {
            if (evt.newValue)
                SetActiveTool(CinemachineSceneTool.FoV);
        }

        public static void FarNearClipSelectionToolSelection(UnityEngine.UIElements.ChangeEvent<bool> evt)
        {
            if (evt.newValue)
                SetActiveTool(CinemachineSceneTool.FarNearClip);
        }
        
        public static void FollowOffsetToolSelection(UnityEngine.UIElements.ChangeEvent<bool> evt)
        {
            if (evt.newValue)
                SetActiveTool(CinemachineSceneTool.FollowOffset);
        }
        public static void TrackedObjectOffsetToolSelection(UnityEngine.UIElements.ChangeEvent<bool> evt)
        {
            if (evt.newValue)
                SetActiveTool(CinemachineSceneTool.TrackedObjectOffset);
        }

        static void SetActiveTool(CinemachineSceneTool activeTool)
        {
            s_ActiveTool = activeTool;
            var activeToolIndex = (int)activeTool;
            for (var i = 1; i < s_ToolSetHandlers.Length; ++i) // start from 1, because 0 is CinemachineSceneTool.None
            {
                if (s_ToolSetHandlers[i] != null)
                    s_ToolSetHandlers[i].Invoke(i == activeToolIndex);
            }
            if (activeTool != CinemachineSceneTool.None)
            {
                Tools.current = Tool.None; // cinemachine tools are exclusive with unity tools
            }
        }

        static CinemachineSceneToolUtility()
        {
            s_ToolSetHandlers = new ToolValueSet[Enum.GetNames(typeof(CinemachineSceneTool)).Length];
            EditorApplication.update += CheckUnityTools;
        }
        
        static void CheckUnityTools()
        {
            // If a Unity tool is selected, our tools should be deselected
            if (Tools.current != Tool.None)
            {
                SetActiveTool(CinemachineSceneTool.None);
            }
        }
    }
}