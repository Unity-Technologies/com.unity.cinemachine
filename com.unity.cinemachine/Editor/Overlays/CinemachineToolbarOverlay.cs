using System;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace Unity.Cinemachine.Editor
{
    /// <summary>
    /// This is a generic Tool class for Cinemachine tools.
    /// To create a new tool, inherit from CinemachineTool and implement GetIcon().
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
    public abstract class CinemachineTool : EditorTool, IDrawSelectedHandles
    {
        GUIContent m_IconContent;
        
        /// <summary>Implement this to set your Tool's icon and tooltip.</summary>
        /// <returns>A GUIContent with an icon set.</returns>
        protected abstract GUIContent GetIcon();

        /// <summary>This lets the editor find the icon of the tool.</summary>
        public override GUIContent toolbarIcon
        {
            get
            {
                if (m_IconContent == null || m_State.refreshIcon)
                {
                    m_IconContent = GetIcon();
                    m_State.refreshIcon = false;
                }
                return m_IconContent;
            }
        }

        /// <summary>This is called when the Tool is selected in the editor.</summary>
        public override void OnActivated()
        {
            base.OnActivated();
            CinemachineSceneToolUtility.SetTool(true, GetType());
            m_State.refreshIcon = true;
            m_State.isSelected = true;
        }

        /// <summary>This is called when the Tool is deselected in the editor.</summary>
        public override void OnWillBeDeactivated()
        {
            base.OnWillBeDeactivated();
            CinemachineSceneToolUtility.SetTool(false, GetType());
            m_State.refreshIcon = true;
            m_State.isSelected = false;
        }
        
        /// <summary>This checks whether this tool should be displayed or not.</summary>
        /// <returns>True, when this tool is to be drawn. False, otherwise.</returns>
        public override bool IsAvailable() => CinemachineSceneToolUtility.IsToolRequired(GetType());

        /// <summary>Implement IDrawSelectedHandles to draw gizmos for this tool even if it is not the active tool.</summary>
        public void OnDrawHandles()
        {
        }

        /// <summary>Get the path to the tool's icon asset.</summary>
        /// <returns>The path to the icon asset.</returns>
        private protected string GetIconPath()
        {
            m_State.refreshIcon = m_State.isProSkin != EditorGUIUtility.isProSkin;
            m_State.isProSkin = EditorGUIUtility.isProSkin;
            return $"{CinemachineCore.kPackageRoot}/Editor/EditorResources/Handles/" +
                (m_State.isProSkin ? 
                    (m_State.isSelected ? "Dark-Selected" : "Dark") : 
                    (m_State.isSelected ? "Light-Selected" : "Light")) + "/";
        }

        struct ToolState
        {
            public bool isSelected;
            public bool isProSkin;
            public bool refreshIcon;
        }
        ToolState m_State = new() { refreshIcon = true };
    }
    
    [EditorTool("Field of View Tool", typeof(CinemachineVirtualCameraBase))]
    class FoVTool : CinemachineTool
    {
        protected override GUIContent GetIcon() =>
            new GUIContent
            {
                image = AssetDatabase.LoadAssetAtPath<Texture2D>(GetIconPath() + "FOV.png"),
                tooltip = "Field of View Tool",
            };
    }
    
    [EditorTool("Far-Near Clip Tool", typeof(CinemachineVirtualCameraBase))]
    class FarNearClipTool : CinemachineTool
    {
        protected override GUIContent GetIcon() =>
            new GUIContent
            {
                image = AssetDatabase.LoadAssetAtPath<Texture2D>(GetIconPath() + "FarNearClip.png"),
                tooltip = "Far/Near Clip Tool",
            };
    }
    
    [EditorTool("Follow Offset Tool", typeof(CinemachineVirtualCameraBase))]
    class FollowOffsetTool : CinemachineTool
    {
        protected override GUIContent GetIcon() =>
            new GUIContent
            {
                image = AssetDatabase.LoadAssetAtPath<Texture2D>(GetIconPath() + "FollowOffset.png"),
                tooltip = "Follow Offset Tool",
            };
    }

    [EditorTool("Tracked Object Offset Tool", typeof(CinemachineVirtualCameraBase))]
    class TrackedObjectOffsetTool : CinemachineTool
    {
        protected override GUIContent GetIcon() =>
            new GUIContent
            {
                image = AssetDatabase.LoadAssetAtPath<Texture2D>(GetIconPath() + "TrackedObjectOffset.png"),
                tooltip = "Tracked Object Offset Tool",
            };
    }

#if false // We disable this tool window, because it has only one thing in it, which isn't so useful and is a bit confusing tbh
    /// <summary>
    /// To add your custom tools (EditorToolbarElement) to the Cinemachine Tool Settings toolbar,
    /// set CinemachineToolSettingsOverlay.customToolbarItems with your custom tools' IDs.
    ///
    /// By default, CinemachineToolSettingsOverlay.customToolbarItems is null.
    /// </summary>
    [Overlay(typeof(SceneView), "Cinemachine Tool Settings")]
    [Icon(CinemachineCore.kPackageRoot + "/Editor/EditorResources/Icons/CmCamera@256.png")]
    public class CinemachineToolSettingsOverlay : Overlay, ICreateToolbar
    {
        static readonly string[] k_CmToolbarItems = { OrbitalFollowOrbitSelection.id };
        
        /// <summary>
        /// Override this method to return your visual element content.
        /// By default, this draws the same visual element as the HorizontalToolbar
        /// </summary>
        /// <returns>VisualElement for the Panel content.</returns>
        public override VisualElement CreatePanelContent() => CreateContent(Layout.HorizontalToolbar);
        
        /// <summary>Set this with your custom tools' IDs.</summary>
        public static string[] customToolbarItems = null;
        
        /// <summary>The list of tools that this toolbar is going to contain.</summary>
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
    class OrbitalFollowOrbitSelection : EditorToolbarDropdown
    {
        public const string id = "OrbitalFollowOrbitSelection/Dropdown";
        static int s_SelectedOrbit;
        Texture2D[] m_Icons;

        public OrbitalFollowOrbitSelection()
        {
            tooltip = "OrbitalFollow Orbit Selection";
            clicked += OrbitalFollowOrbitSelectionMenu;
            EditorApplication.update += DisplayAndUpdateOrbitIfRequired;
            
            m_Icons = new Texture2D[]
            {
                AssetDatabase.LoadAssetAtPath<Texture2D>($"{CinemachineCore.kPackageRoot}/Editor/EditorResources/Handles/FreelookRigTop.png"),
                AssetDatabase.LoadAssetAtPath<Texture2D>($"{CinemachineCore.kPackageRoot}/Editor/EditorResources/Handles/FreelookRigMiddle.png"),
                AssetDatabase.LoadAssetAtPath<Texture2D>($"{CinemachineCore.kPackageRoot}/Editor/EditorResources/Handles/FreelookRigBottom.png"),
            };
        }

        ~OrbitalFollowOrbitSelection()
        {
            clicked -= OrbitalFollowOrbitSelectionMenu;
            EditorApplication.update -= DisplayAndUpdateOrbitIfRequired;
        }
        
        readonly Type m_OrbitalFollowSelectionType = typeof(OrbitalFollowOrbitSelection);
        void DisplayAndUpdateOrbitIfRequired()
        {
            var active = Selection.activeObject as GameObject;
            if (active != null)
            {
                if (active.TryGetComponent<CinemachineOrbitalFollow>(out var orbitalFollow)
                    && CinemachineSceneToolUtility.IsToolRequired(m_OrbitalFollowSelectionType) 
                    && orbitalFollow.OrbitStyle == CinemachineOrbitalFollow.OrbitStyles.ThreeRing)
                {
                    style.display = DisplayStyle.Flex; // display menu
                   
                    var verticalAxis = orbitalFollow.VerticalAxis;
                    var centerToleranceRange = Mathf.Min(Mathf.Abs(verticalAxis.Range.x - verticalAxis.Center), 
                        Mathf.Abs(verticalAxis.Range.y - verticalAxis.Center)) / 2f;
                    s_SelectedOrbit = (Math.Abs(verticalAxis.Value - verticalAxis.Center) < centerToleranceRange) 
                        ? 1 
                        : (verticalAxis.Value > verticalAxis.Center ? 0 : 2);
                    
                    text = CinemachineOrbitalFollowEditor.orbitNames[s_SelectedOrbit].text;
                    icon = m_Icons[s_SelectedOrbit];
                    
                    return;
                }
            }
            style.display = DisplayStyle.None; // hide menu
        }

        void OrbitalFollowOrbitSelectionMenu()
        {
            var menu = new GenericMenu();
            for (var i = 0; i < CinemachineOrbitalFollowEditor.orbitNames.Length; ++i)
            {
                var rigIndex = i; // need to capture index for the lambda below
                menu.AddItem(CinemachineOrbitalFollowEditor.orbitNames[i], false, () =>
                {
                    s_SelectedOrbit = rigIndex;
                    var active = Selection.activeObject as GameObject;
                    if (active != null)
                    {
                        if (active.TryGetComponent<CinemachineOrbitalFollow>(out var orbitalFollow))
                        {
                            orbitalFollow.VerticalAxis.Value = s_SelectedOrbit switch
                            {
                                0 => orbitalFollow.VerticalAxis.Range.y,
                                2 => orbitalFollow.VerticalAxis.Range.x,
                                _ => orbitalFollow.VerticalAxis.Center
                            };
                        }
                    }
                });
            }
            menu.DropDown(worldBound);
        }
    }
#endif
}
