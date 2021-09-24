using Cinemachine.Utility;
using UnityEngine;
using UnityEditor.Toolbars;
using UnityEditor.Overlays;
using UnityEngine.UIElements;
using UnityEditor;

namespace Cinemachine.Editor
{
    abstract class CinemachineEditorToolbarToggle : EditorToolbarToggle
    {
        protected void RegisterWithCinemachine(CinemachineSceneTool tool)
        {
            m_MyTool = tool;
            this.RegisterValueChangedCallback(Register);
            CinemachineSceneToolUtility.SetToolHandler(m_MyTool, SetValue);
        }

        CinemachineSceneTool m_MyTool;
        void Register(ChangeEvent<bool> v) => CinemachineSceneToolUtility.SetTool(v.newValue, m_MyTool);
        void SetValue(bool v) => value = v;
    }
    
    [EditorToolbarElement(id, typeof(SceneView))]
    class FoVTool : CinemachineEditorToolbarToggle
    {
        public const string id = "FoVTool/Toggle";

        public FoVTool()
        {
            icon = EditorGUIUtility.IconContent("d_BillboardAsset Icon").image as Texture2D;
            RegisterWithCinemachine(CinemachineSceneTool.FoV);
        }
    }

    [EditorToolbarElement(id, typeof(SceneView))]
    class FarNearClipTool : CinemachineEditorToolbarToggle
    {
        public const string id = "FarNearClipTool/Toggle";

        public FarNearClipTool()
        {
            icon = EditorGUIUtility.IconContent("d_BillboardRenderer Icon").image as Texture2D;
            RegisterWithCinemachine(CinemachineSceneTool.FarNearClip);
        }
    }

    [EditorToolbarElement(id, typeof(SceneView))]
    class FollowOffsetTool : CinemachineEditorToolbarToggle
    {
        public const string id = "FollowOffsetTool/Toggle";

        public FollowOffsetTool()
        {
            icon = EditorGUIUtility.IconContent("MoveTool@2x").image as Texture2D;
            RegisterWithCinemachine(CinemachineSceneTool.FollowOffset);
        }
    }

    [EditorToolbarElement(id, typeof(SceneView))]
    class TrackedObjectOffsetTool : CinemachineEditorToolbarToggle
    {
        public const string id = "TrackedObjectOffsetTool/Toggle";

        public TrackedObjectOffsetTool()
        {
            icon = EditorGUIUtility.IconContent("d_Toolbar Plus@2x").image as Texture2D;
            RegisterWithCinemachine(CinemachineSceneTool.TrackedObjectOffset);
        }
    }


// All Overlays must be tagged with the OverlayAttribute
    [Overlay(typeof(SceneView), "Cinemachine")]

// IconAttribute provides a way to define an icon for when an Overlay is in collapsed form. If not provided, the name initials are used.
    [Icon("Packages/com.unity.cinemachine/Gizmos/cm_logo.png")]

// Toolbar Overlays must inherit `ToolbarOverlay` and implement a parameter-less constructor. The contents of a toolbar are populated with string IDs, which are passed to the base constructor. IDs are defined by EditorToolbarElementAttribute.
    public class CinemachineVirtualCameraToolbar : ToolbarOverlay
    {
        // ToolbarOverlay implements a parameterless constructor, passing the EditorToolbarElementAttribute ID. 
        // This is the only code required to implement a toolbar Overlay. Unlike panel overlays, the contents are defined
        // as standalone pieces that will be collected to form a strip of elements.

        CinemachineVirtualCameraToolbar()
            : base(
                FoVTool.id,
                FarNearClipTool.id,
                FollowOffsetTool.id,
                TrackedObjectOffsetTool.id
            ) {}
    }
}