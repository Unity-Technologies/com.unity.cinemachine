#if UNITY_2021_2_OR_NEWER
using System;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace Cinemachine.Editor
{
#if UNITY_2022_1_OR_NEWER
    /// <summary>
    /// This is a generic Tool class for Cinemachine tools.
    /// To create a new tool, inherit from this class and implement OnEnable to set m_IconContent's image and tooltip
    /// attributes.
    /// Your new tool will need to have the [EditorTool("tool-name", typeof(CinemachineVirtualCameraBase))] attribute.
    ///
    /// A tool will be drawn iff it has been registered using CinemachineSceneToolUtility.RegisterTool.
    /// This is generally done in the OnEnable function of the editor script of the cinemachine component
    /// (CinemachineVirtualCamera, CinemachineComponentBase), for which the tool was meant.
    /// To unregister, call CinemachineSceneToolUtility.UnregisterTool in the same script's OnDisable function.
    ///
    /// To draw the handles related to the tool, you need to implement your drawing function and call it in the
    /// editor script's OnSceneGUI function. An alternative for drawing handles is to override this function's
    /// OnToolGUI or OnDrawHandles functions (see EditorTool or IDrawSelectedHandles docs for more information).
    ///
    /// To check, if a tool has been enabled/disabled in the editor script, use CinemachineSceneToolUtility.IsToolActive.
    /// </summary>
    public class CinemachineTool : EditorTool, IDrawSelectedHandles
    {
        protected GUIContent m_IconContent;

        /// <summary>This lets the editor find the icon of the tool.</summary>
        public override GUIContent toolbarIcon => m_IconContent;

        // <summary>This is called when the Tool is selected in the editor.</summary>
        public override void OnActivated()
        {
            base.OnActivated();
            CinemachineSceneToolUtility.SetTool(true, GetType());
        }

        // <summary>This is called when the Tool is deselected in the editor.</summary>
        public override void OnWillBeDeactivated()
        {
            base.OnWillBeDeactivated();
            CinemachineSceneToolUtility.SetTool(false, GetType());
        }
        
        // <summary>This checks whether this tool should be displayed or not.</summary>
        public override bool IsAvailable()
        {
            return CinemachineSceneToolUtility.IsToolRequired(GetType());
        }
        
        // Implement IDrawSelectedHandles to draw gizmos for this tool even if it is not the active tool.
        public void OnDrawHandles()
        {
        }
    }
    
    [EditorTool("Field of View Tool", typeof(CinemachineVirtualCameraBase))]
    class FoVTool : CinemachineTool
    {
        void OnEnable()
        {
            m_IconContent = new GUIContent()
            {
                image = AssetDatabase.LoadAssetAtPath<Texture2D>(ScriptableObjectUtility.CinemachineRealativeInstallPath 
                    + "/Editor/EditorResources/FOV.png"),
                tooltip = "Field of View Tool",
            };
        }
    }
    
    [EditorTool("Far-Near Clip Tool", typeof(CinemachineVirtualCameraBase))]
    class FarNearClipTool : CinemachineTool
    {
        void OnEnable()
        {
            m_IconContent = new GUIContent()
            {
                image = AssetDatabase.LoadAssetAtPath<Texture2D>(ScriptableObjectUtility.CinemachineRealativeInstallPath 
                    + "/Editor/EditorResources/FarNearClip.png"),
                tooltip = "Far/Near Clip Tool",
            };
        }
    }
    
    [EditorTool("Follow Offset Tool", typeof(CinemachineVirtualCameraBase))]
    class FollowOffsetTool : CinemachineTool
    {
        void OnEnable()
        {
            m_IconContent = new GUIContent()
            {
                image = AssetDatabase.LoadAssetAtPath<Texture2D>(ScriptableObjectUtility.CinemachineRealativeInstallPath 
                    + "/Editor/EditorResources/FollowOffset.png"),
                tooltip = "Follow Offset Tool"
            };
        }
    }

    [EditorTool("Tracked Object Offset Tool", typeof(CinemachineVirtualCameraBase))]
    class TrackedObjectOffsetTool : CinemachineTool
    {
        void OnEnable()
        {
            m_IconContent = new GUIContent()
            {
                image = AssetDatabase.LoadAssetAtPath<Texture2D>(ScriptableObjectUtility.CinemachineRealativeInstallPath
                    + "/Editor/EditorResources/TrackedObjectOffset.png"),
                tooltip = "Tracked Object Offset Tool",
            };
        }
    }

    /// <summary>
    /// To add your custom tools (EditorToolbarElement) to the Cinemachine Tool Settings toolbar,
    /// set CinemachineToolSettingsOverlay.customToolbarItems with your custom tools' IDs.
    ///
    /// By default, CinemachineToolSettingsOverlay.customToolbarItems is null.
    /// </summary>
    [Overlay(typeof(SceneView), "Cinemachine Tool Settings")]
    [Icon("Packages/com.unity.cinemachine/Gizmos/cm_logo.png")]
    public class CinemachineToolSettingsOverlay : Overlay, ICreateToolbar
    {
        static readonly string[] k_CmToolbarItems = { FreelookRigSelection.id };

        public override VisualElement CreatePanelContent() => CreateContent(Layout.HorizontalToolbar);

        /// <summary>Set this with your custom tools' IDs.</summary>
        public static string[] customToolbarItems = null;
        public IEnumerable<string> toolbarElements
        {
            get
            {
                if (customToolbarItems != null)
                {
                    var toolbarItems = new List<string>(k_CmToolbarItems);
                    toolbarItems.AddRange(customToolbarItems);
                    return toolbarItems;
                }
                
                return k_CmToolbarItems;
            }
        }
    }
    
    [EditorToolbarElement(id, typeof(SceneView))]
    class FreelookRigSelection : EditorToolbarDropdown
    {
        public const string id = "FreelookRigSelection/Dropdown";
        public static int SelectedRig;

        public FreelookRigSelection()
        {
            tooltip = "Freelook Rig Selection";
            clicked += FreelookRigSelectionMenu;
            EditorApplication.update += ShadowSelectedRigName;
            EditorApplication.update += DisplayIfRequired;
        }

        ~FreelookRigSelection()
        {
            clicked -= FreelookRigSelectionMenu;
            EditorApplication.update -= ShadowSelectedRigName;
            EditorApplication.update -= DisplayIfRequired;
        }

        Type m_FreelookRigSelectionType = typeof(FreelookRigSelection);
        void DisplayIfRequired() => style.display = 
            CinemachineSceneToolUtility.IsToolRequired(m_FreelookRigSelectionType) 
                ? DisplayStyle.Flex : DisplayStyle.None;

        // text is currently only visibly in Panel mode due to this bug: https://jira.unity3d.com/browse/STO-2278
        void ShadowSelectedRigName() => text = CinemachineFreeLookEditor.RigNames[Mathf.Clamp(
                SelectedRig, 0, CinemachineFreeLookEditor.RigNames.Length - 1)].text;

        void FreelookRigSelectionMenu()
        {
            var menu = new GenericMenu();
            for (var i = 0; i < CinemachineFreeLookEditor.RigNames.Length; ++i)
            {
                var rigIndex = i; // vital to capture the index here for the lambda below
                menu.AddItem(CinemachineFreeLookEditor.RigNames[i], false, () =>
                {
                    SelectedRig = rigIndex;
                    var active = Selection.activeObject as GameObject;
                    if (active != null)
                    {
                        var freelook = active.GetComponent<CinemachineFreeLook>();
                        if (freelook != null)
                            CinemachineFreeLookEditor.SetSelectedRig(freelook, rigIndex);
                    }
                });
            }
            menu.DropDown(worldBound);
        }
    }
#else
    /// <summary>
    /// To display a CinemachineExclusiveEditorToolbarToggle in the Cinemachine Toolbar.
    /// </summary>
    [Overlay(typeof(SceneView), "Cinemachine")]
    [Icon("Packages/com.unity.cinemachine/Gizmos/cm_logo.png")]
    class CinemachineToolbarOverlay : ToolbarOverlay
    {
        public CinemachineToolbarOverlay()
            : base(
                FreelookRigSelection.id,
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
    class FreelookRigSelection : EditorToolbarDropdown
    {
        public const string id = "FreelookRigSelection/Dropdown";
        public static int SelectedRig;

        public FreelookRigSelection()
        {
            tooltip = "Freelook Rig Selection";
            clicked += FreelookRigSelectionMenu;
            CinemachineSceneToolUtility.RegisterToolHandlers(GetType(), isOn => {}, 
                display => style.display = display ? DisplayStyle.Flex : DisplayStyle.None);
            EditorApplication.update += ShadowSelectedRigName;
        }

        ~FreelookRigSelection()
        {
            clicked -= FreelookRigSelectionMenu;
            EditorApplication.update -= ShadowSelectedRigName;
        }

        void ShadowSelectedRigName()
        {
            text = CinemachineFreeLookEditor.RigNames[Mathf.Clamp(SelectedRig, 0, CinemachineFreeLookEditor.RigNames.Length-1)].text;
        }
        
        void FreelookRigSelectionMenu()
        {
            var menu = new GenericMenu();
            for (var i = 0; i < CinemachineFreeLookEditor.RigNames.Length; ++i)
            {
                var rigIndex = i; // vital to capture the index here for the lambda below
                menu.AddItem(CinemachineFreeLookEditor.RigNames[i], false, () =>
                {
                    SelectedRig = rigIndex;
                    var active = Selection.activeObject as GameObject;
                    if (active != null)
                    {
                        var freelook = active.GetComponent<CinemachineFreeLook>();
                        if (freelook != null)
                            CinemachineFreeLookEditor.SetSelectedRig(freelook, rigIndex);
                    }
                });
            }
            menu.DropDown(worldBound);
        }
    }
#endif
}
#endif