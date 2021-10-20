#if UNITY_2021_2_OR_NEWER
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

namespace Cinemachine.Editor
{
    /// <summary>
    /// To display a CinemachineExclusiveEditorToolbarToggle in the Cinemachine Toolbar,
    /// TODO: Make this extendable when another PR lands (see: CMCL-501),
    /// </summary>
    [Overlay(typeof(SceneView), "Cinemachine")]
    [Icon("Packages/com.unity.cinemachine/Gizmos/cm_logo.png")]
    class CinemachineToolbarOverlay : ToolbarOverlay
    {
        public CinemachineToolbarOverlay()
            : base(
                FreelookRigSelection.id,
#if CINEMACHINE_EXPERIMENTAL_VCAM
                NewFreelookRigSelection.id,
#endif
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

    /// <summary>
    /// Creates a toggle tool on the Cinemachine toolbar that is exclusive with other
    /// CinemachineExclusiveEditorToolbarToggles. Meaning, that maximum one
    /// CinemachineExclusiveEditorToolbarToggle can be active at any time.
    /// </summary>
    abstract class CinemachineExclusiveEditorToolbarToggle : EditorToolbarToggle
    {
        protected CinemachineExclusiveEditorToolbarToggle()
        {
            var type = GetType();
            this.RegisterValueChangedCallback(
                v => CinemachineSceneToolUtility.SetTool(v.newValue, type));
            CinemachineSceneToolUtility.RegisterExclusiveToolHandlers(type, isOn => value = isOn, 
                display => style.display = display ? DisplayStyle.Flex : DisplayStyle.None);
        }
    }
    
    [EditorToolbarElement(id, typeof(SceneView))]
    class FoVTool : CinemachineExclusiveEditorToolbarToggle
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
    class FarNearClipTool : CinemachineExclusiveEditorToolbarToggle
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
    class FollowOffsetTool : CinemachineExclusiveEditorToolbarToggle
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
    class TrackedObjectOffsetTool : CinemachineExclusiveEditorToolbarToggle
    {
        public const string id = "TrackedObjectOffsetTool/Toggle";

        public TrackedObjectOffsetTool()
        {
            icon = AssetDatabase.LoadAssetAtPath<Texture2D>(ScriptableObjectUtility.CinemachineRealativeInstallPath 
                + "/Editor/EditorResources/TrackedObjectOffset.png");
            tooltip = "Tracked Object Offset Tool";
        }
    }
    
    /// <summary>
    /// Creates a toggle tool on the Cinemachine toolbar.
    /// </summary>
    abstract class CinemachineEditorToolbarToggle : EditorToolbarToggle
    {
        protected CinemachineEditorToolbarToggle()
        {
            CinemachineSceneToolUtility.RegisterToolHandlers(GetType(), isOn => value = isOn, 
                display => style.display = display ? DisplayStyle.Flex : DisplayStyle.None);
        }
    }
    
    [EditorToolbarElement(id, typeof(SceneView))]
    class SoloVcamTool : CinemachineEditorToolbarToggle
    {
        public const string id = "SoloVcamTool/Toggle";

        public SoloVcamTool()
        {
            this.RegisterValueChangedCallback(
                v => CinemachineSceneToolUtility.SetSolo(v.newValue));
            
            onIcon = EditorGUIUtility.IconContent("animationvisibilitytoggleon@2x").image as Texture2D;
            offIcon = EditorGUIUtility.IconContent("animationvisibilitytoggleoff@2x").image as Texture2D;
            tooltip = "Solo Vcam Tool";
        }
    }
    
    
    [EditorToolbarElement(id, typeof(SceneView))]
    class FreelookRigSelection : EditorToolbarDropdown
    {
        public const string id = "FreelookRigSelection/Dropdown";

        public FreelookRigSelection()
        {
            tooltip = "Freelook Rig Selection";
            clicked += FreelookRigSelectionMenu;
            CinemachineSceneToolUtility.RegisterToolHandlers(GetType(), isOn => {}, 
                display => style.display = display ? DisplayStyle.Flex : DisplayStyle.None);
            EditorApplication.update += ShadowSelectedRigName;
        }

        void ShadowSelectedRigName()
        {
            text = CinemachineFreeLookEditor.s_RigNames[CinemachineFreeLookEditor.s_SelectedRig].text;
        }
        
        void FreelookRigSelectionMenu()
        {
            var menu = new GenericMenu();
            for (var i = 0; i < CinemachineFreeLookEditor.s_RigNames.Length; ++i)
            {
                var rigIndex = i; // vital to capture the index here for the lambda below
                menu.AddItem(CinemachineFreeLookEditor.s_RigNames[i], false, () =>
                {
                    CinemachineFreeLookEditor.s_SelectedRig = rigIndex;
                    InspectorUtility.RepaintGameView();
                });
            }
            menu.DropDown(worldBound);
        }
    }
   
    #if CINEMACHINE_EXPERIMENTAL_VCAM
    [EditorToolbarElement(id, typeof(SceneView))]
    class NewFreelookRigSelection : EditorToolbarDropdown
    {
        public const string id = "NewFreelookRigSelection/Dropdown";

        public NewFreelookRigSelection()
        {
            tooltip = "New Freelook Rig Selection";
            clicked += NewFreelookRigSelectionMenu;
            CinemachineSceneToolUtility.RegisterToolHandlers(GetType(), isOn => {}, 
                display => style.display = display ? DisplayStyle.Flex : DisplayStyle.None);
            EditorApplication.update += ShadowSelectedRigName;
        }

        void ShadowSelectedRigName()
        {
            text = CinemachineNewFreeLookEditor.s_RigNames[CinemachineNewFreeLookEditor.s_SelectedRig].text;
        }
        
        void NewFreelookRigSelectionMenu()
        {
            var menu = new GenericMenu();
            for (var i = 0; i < CinemachineNewFreeLookEditor.s_RigNames.Length; ++i)
            {
                var rigIndex = i; // vital to capture the index here for the lambda below
                menu.AddItem(CinemachineNewFreeLookEditor.s_RigNames[i], false, () =>
                {
                    CinemachineNewFreeLookEditor.s_SelectedRig = rigIndex;
                    InspectorUtility.RepaintGameView();
                });
            }
            menu.DropDown(worldBound);
        }
    }
    #endif
}
#endif