using Cinemachine.Utility;
using UnityEngine;
using UnityEditor.Toolbars;
using UnityEditor.Overlays;
using UnityEngine.UIElements;
using UnityEditor;

namespace Cinemachine.Editor
{
    [EditorToolbarElement(id, typeof(SceneView))]
    class FoVTool : EditorToolbarToggle
    {
        public const string id = "FoVTool/Toggle";

        public FoVTool()
        {
            icon = EditorGUIUtility.IconContent("d_BillboardAsset Icon").image as Texture2D;
            this.RegisterValueChangedCallback(CinemachineSceneToolUtility.FovToolSelectionToolSelection);
            CinemachineSceneToolUtility.SetHandler(CinemachineSceneTool.FoV, SetValue);
        }

        void SetValue(bool v)
        {
            value = v;
        }
    }

    [EditorToolbarElement(id, typeof(SceneView))]
    class FarNearClipTool : EditorToolbarToggle
    {
        public const string id = "FarNearClipTool/Toggle";

        public FarNearClipTool()
        {
            icon = EditorGUIUtility.IconContent("d_BillboardRenderer Icon").image as Texture2D;
            this.RegisterValueChangedCallback(CinemachineSceneToolUtility.FarNearClipSelectionToolSelection);
            CinemachineSceneToolUtility.SetHandler(CinemachineSceneTool.FarNearClip, SetValue);
        }

        void SetValue(bool v)
        {
            value = v;
        }
    }

    [EditorToolbarElement(id, typeof(SceneView))]
    class FollowOffsetTool : EditorToolbarToggle
    {
        public const string id = "FollowOffsetTool/Toggle";

        public FollowOffsetTool()
        {
            icon = EditorGUIUtility.IconContent("MoveTool@2x").image as Texture2D;
            this.RegisterValueChangedCallback(CinemachineSceneToolUtility.FollowOffsetToolSelection);
            CinemachineSceneToolUtility.SetHandler(CinemachineSceneTool.FollowOffset, SetValue);
        }

        void SetValue(bool v)
        {
            value = v;
        }
    }

    [EditorToolbarElement(id, typeof(SceneView))]
    class TrackedObjectOffsetTool : EditorToolbarToggle
    {
        public const string id = "TrackedObjectOffsetTool/Toggle";

        public TrackedObjectOffsetTool()
        {
            icon = EditorGUIUtility.IconContent("d_Toolbar Plus@2x").image as Texture2D;
            this.RegisterValueChangedCallback(CinemachineSceneToolUtility.TrackedObjectOffsetToolSelection);
            CinemachineSceneToolUtility.SetHandler(CinemachineSceneTool.TrackedObjectOffset, SetValue);
        }

        void SetValue(bool v)
        {
            value = v;
        }
    }


// All Overlays must be tagged with the OverlayAttribute
    [Overlay(typeof(SceneView), "Cinemachine")]

// IconAttribute provides a way to define an icon for when an Overlay is in collapsed form. If not provided, the name initials are used.
    [Icon("Packages/Cinemachine/Gizmos/cm_logo.png")]

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