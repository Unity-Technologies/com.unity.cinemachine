using Cinemachine.Utility;
using UnityEngine;
using UnityEditor.Toolbars;
using UnityEditor.Overlays;
using UnityEngine.UIElements;
using UnityEditor;

namespace Cinemachine.Editor
{
    /// <summary>
    /// To display a CinemachineEditorToolbarToggle in the Cinemachine Toolbar, add it's ID to the constructor.
    /// </summary>
    [Overlay(typeof(SceneView), "Cinemachine")]
    [Icon("Packages/com.unity.cinemachine/Gizmos/cm_logo.png")]
    public class CinemachineVirtualCameraToolbar : ToolbarOverlay
    {
        CinemachineVirtualCameraToolbar()
            : base(
                FoVTool.id,
                FarNearClipTool.id,
                FollowOffsetTool.id,
                TrackedObjectOffsetTool.id
            )
        {
            CinemachineSceneToolUtility.RegisterToolbarIsDisplayedHandler(IsDisplayed);
        }

        bool IsDisplayed()
        {
            return displayed;
        }
    }
    
    abstract class CinemachineEditorToolbarToggle : EditorToolbarToggle
    {
        protected void RegisterWithCinemachine(CinemachineSceneTool tool)
        {
            m_Tool = tool;
            this.RegisterValueChangedCallback(Register);
            CinemachineSceneToolUtility.RegisterToolToggleHandler(m_Tool, SetToggle);
            CinemachineSceneToolUtility.RegisterToolIsDisplayedHandler(m_Tool, Display);
        }

        CinemachineSceneTool m_Tool;
        void Register(ChangeEvent<bool> v) => CinemachineSceneToolUtility.SetTool(v.newValue, m_Tool);
        void SetToggle(bool isOn) => value = isOn;
        void Display(bool display) => style.display = display ? DisplayStyle.Flex : DisplayStyle.None;
    }
    
    [EditorToolbarElement(id, typeof(SceneView))]
    class FoVTool : CinemachineEditorToolbarToggle
    {
        public const string id = "FoVTool/Toggle";

        public FoVTool()
        {
            icon = AssetDatabase.LoadAssetAtPath<Texture2D>(ScriptableObjectUtility.CinemachineRealativeInstallPath 
                + "/Editor/EditorResources/FOV.png");
            tooltip = "Field of View Tool";
            RegisterWithCinemachine(CinemachineSceneTool.FoV);
        }
    }

    [EditorToolbarElement(id, typeof(SceneView))]
    class FarNearClipTool : CinemachineEditorToolbarToggle
    {
        public const string id = "FarNearClipTool/Toggle";

        public FarNearClipTool()
        {
            icon = AssetDatabase.LoadAssetAtPath<Texture2D>(ScriptableObjectUtility.CinemachineRealativeInstallPath 
                + "/Editor/EditorResources/FarNearClip.png");
            tooltip = "Far/Near Clip Tool";
            RegisterWithCinemachine(CinemachineSceneTool.FarNearClip);
        }
    }

    [EditorToolbarElement(id, typeof(SceneView))]
    class FollowOffsetTool : CinemachineEditorToolbarToggle
    {
        public const string id = "FollowOffsetTool/Toggle";

        public FollowOffsetTool()
        {
            icon = AssetDatabase.LoadAssetAtPath<Texture2D>(ScriptableObjectUtility.CinemachineRealativeInstallPath 
                + "/Editor/EditorResources/FollowOffset.png");
            tooltip = "Follow Offset Tool";
            RegisterWithCinemachine(CinemachineSceneTool.FollowOffset);
        }
    }

    [EditorToolbarElement(id, typeof(SceneView))]
    class TrackedObjectOffsetTool : CinemachineEditorToolbarToggle
    {
        public const string id = "TrackedObjectOffsetTool/Toggle";

        public TrackedObjectOffsetTool()
        {
            icon = AssetDatabase.LoadAssetAtPath<Texture2D>(ScriptableObjectUtility.CinemachineRealativeInstallPath 
                + "/Editor/EditorResources/TrackedObjectOffset.png");
            tooltip = "Tracked Object Offset Tool";
            RegisterWithCinemachine(CinemachineSceneTool.TrackedObjectOffset);
        }
    }
}