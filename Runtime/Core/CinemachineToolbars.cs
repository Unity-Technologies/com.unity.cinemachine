using System;
using UnityEditor;

namespace Cinemachine.Utility
{
    public enum CinemachineSceneTool
    {
        FoV,
        FarNearClip,
        FollowOffset,
        TrackedObjectOffset,
    };
    
    static class CinemachineSceneToolUtility
    {
        public static bool IsToolOn(CinemachineSceneTool tool)
        {
            switch (tool)
            {
                case CinemachineSceneTool.FoV:
                    return s_FoVTool;
                case CinemachineSceneTool.FarNearClip:
                    return s_FarNearClipTool;
                case CinemachineSceneTool.FollowOffset:
                    return s_FollowOffsetTool;
                case CinemachineSceneTool.TrackedObjectOffset:
                    return s_TrackedObjectOffsetTool;
                default:
                    throw new ArgumentOutOfRangeException(nameof(tool), tool, null);
            }
        }
        
        public delegate void ToolValueSet(bool v);

        public static ToolValueSet FoVToolHandler;
        public static ToolValueSet FarNearClipToolHandler;
        public static ToolValueSet FollowOffsetToolHandler;
        public static ToolValueSet TrackedObjectOffsetToolHandler;
        
        public static void FovToolSelectionToolSelection(UnityEngine.UIElements.ChangeEvent<bool> evt)
        {
            s_FoVTool = evt.newValue;
            if (s_FoVTool)
                SetTools(true, false, false, false);
        }

        public static void FarNearClipSelectionToolSelection(UnityEngine.UIElements.ChangeEvent<bool> evt)
        {
            s_FarNearClipTool = evt.newValue;
            if (s_FarNearClipTool)
                SetTools(false, true, false, false);
        }
        
        public static void FollowOffsetToolSelection(UnityEngine.UIElements.ChangeEvent<bool> evt)
        {
            s_FollowOffsetTool = evt.newValue;
            if (s_FollowOffsetTool)
                SetTools(false, false, true, false);
        }
        public static void TrackedObjectOffsetToolSelection(UnityEngine.UIElements.ChangeEvent<bool> evt)
        {
            s_TrackedObjectOffsetTool = evt.newValue;
            if (s_TrackedObjectOffsetTool)
                SetTools(false, false, false, true);
        }

        static void SetTools(bool fov, bool farNearClip, bool followOffset, bool trackedObjectOffset)
        {
            if (fov || farNearClip || followOffset || trackedObjectOffset)
            {
                Tools.current = Tool.View; // cinemachine tools are exclusive with unity tools
            }
            
            s_FoVTool = fov;
            s_FarNearClipTool = farNearClip;
            s_FollowOffsetTool = followOffset;
            s_TrackedObjectOffsetTool = trackedObjectOffset;

            FoVToolHandler(fov);
            FarNearClipToolHandler(farNearClip);
            FollowOffsetToolHandler(followOffset);
            TrackedObjectOffsetToolHandler(trackedObjectOffset);
        }

        static bool s_FoVTool;
        static bool s_FarNearClipTool;
        static bool s_FollowOffsetTool;
        static bool s_TrackedObjectOffsetTool;
        static CinemachineSceneToolUtility()
        {
            s_FoVTool = s_FarNearClipTool = s_FollowOffsetTool = s_TrackedObjectOffsetTool = false;
            EditorApplication.update += CheckUnityTools;
        }
        
        static void CheckUnityTools()
        {
            if (Tools.current != Tool.View)
            {
                SetTools(false, false, false, false);
            }
        }
    }
}
