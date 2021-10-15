#if UNITY_2021_2_OR_NEWER
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

namespace Cinemachine.Editor
{
    /// <summary>
    /// To display a CinemachineEditorToolbarToggle in the Cinemachine Toolbar,
    /// TODO: extendable when another PR lands. 
    /// </summary>
    [Overlay(typeof(SceneView), "Cinemachine")]
    [Icon("Packages/com.unity.cinemachine/Gizmos/cm_logo.png")]
    class CinemachineToolbarOverlay : ToolbarOverlay
    {
        public CinemachineToolbarOverlay()
            : base(
                SoloVcamTool.id,
                FoVTool.id,
                FarNearClipTool.id,
                FollowOffsetTool.id,
                TrackedObjectOffsetTool.id
            )
        {
            CinemachineSceneToolUtility.RegisterToolbarIsDisplayedHandler(() => displayed);
            CinemachineSceneToolUtility.RegisterToolbarDisplayHandler(v =>
            {
                if (displayed == v)
                {
                    return false;
                }
                displayed = v;
                return true;
            });
        }
    }

    abstract class CinemachineEditorToolbarToggle : EditorToolbarToggle
    {
        protected CinemachineEditorToolbarToggle()
        {
            var type = GetType();
            this.RegisterValueChangedCallback(
                v => CinemachineSceneToolUtility.SetTool(v.newValue, type));
            CinemachineSceneToolUtility.RegisterToolHandlers(type, isOn => value = isOn, 
                display => style.display = display ? DisplayStyle.Flex : DisplayStyle.None);
        }
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
        }
    }
    
    [EditorToolbarElement(id, typeof(SceneView))]
    class SoloVcamTool : CinemachineEditorToolbarToggle
    {
        public const string id = "SoloVcamTool/Toggle";

        public SoloVcamTool()
        {
            onIcon = EditorGUIUtility.IconContent("animationvisibilitytoggleon@2x").image as Texture2D;
            offIcon = EditorGUIUtility.IconContent("animationvisibilitytoggleoff@2x").image as Texture2D;
            tooltip = "Solo Vcam Tool";
        }
    }
}
#endif