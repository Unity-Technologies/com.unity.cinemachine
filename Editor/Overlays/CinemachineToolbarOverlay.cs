#if UNITY_2021_2_OR_NEWER
using System;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEngine;

namespace Cinemachine.Editor
{
    /// <summary>
    /// This is a generic Tool class for Cinemachine tools.
    /// To add a new tool, inherit from this class and implement the default Constructor to set m_Type, and
    /// implement OnEnable to set m_IconContent.
    ///
    /// A tool will be drawn iff it has been registered using CinemachineSceneToolUtility.RegisterTool.
    /// This is generally done in the OnEnable function of the editor script of the cinemachine component
    /// (CinemahcineVirtualCamera, CinemachineComponentBase), for which the tool was meant.
    /// To unregister, call CinemachineSceneToolUtility.UnregisterTool in the same script's OnDisable function.
    ///
    /// To draw the handles related to the tool, you need to implement your drawing function and call it in the
    /// editor script's OnSceneGUI function.
    ///
    /// To check, if a tool has been enabled/disabled in the editor, use CinemachineSceneToolUtility.IsToolActive.
    /// </summary>
    public class CinemachineTool : EditorTool, IDrawSelectedHandles
    {
        /// <summary>This lets the editor find the icon of the tool.</summary>
        public override GUIContent toolbarIcon => m_IconContent; 
        
        protected GUIContent m_IconContent;
        protected Type m_Type;

        public override void OnActivated()
        {
            base.OnActivated();
            CinemachineSceneToolUtility.SetTool(true, m_Type);
        }

        public override void OnWillBeDeactivated()
        {
            base.OnWillBeDeactivated();
            CinemachineSceneToolUtility.SetTool(false, m_Type);
        }

        public override bool IsAvailable()
        {
            return CinemachineSceneToolUtility.IsToolRequired(m_Type);
        }

        // Move Editor.OnSceneGUI code into the tool implementation
        public override void OnToolGUI(EditorWindow window)
        {
        }

        // Implement IDrawSelectedHandles to draw gizmos for this tool even if it is not the active tool
        public void OnDrawHandles()
        {
        }
    }
    
    [EditorTool("Field of View Tool", typeof(CinemachineVirtualCameraBase))]
    class FoVTool : CinemachineTool
    {
        FoVTool()
        {
            m_Type = typeof(FoVTool);
        }
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
        FarNearClipTool()
        {
            m_Type = typeof(FarNearClipTool);
        }
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
        FollowOffsetTool()
        {
            m_Type = typeof(FollowOffsetTool);
        }
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
        TrackedObjectOffsetTool()
        {
            m_Type = typeof(TrackedObjectOffsetTool);
        }

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

    [Overlay(typeof(SceneView), "Cinemachine Tool Settings")]
    [Icon("Packages/com.unity.cinemachine/Gizmos/cm_logo.png")]
    class CinemachineToolbarOverlay : ToolbarOverlay
    {
        public CinemachineToolbarOverlay()
            : base(FreelookRigSelection.id)
        {
            // CinemachineSceneToolUtility.RegisterToolbarIsDisplayedHandler(() => displayed);
            // CinemachineSceneToolUtility.RegisterToolbarDisplayHandler(v =>
            // {
            //     if (displayed == v)
            //     {
            //         return false;
            //     }
            //
            //     displayed = v;
            //     return true;
            // });
            
            EditorApplication.update += () =>
            {
                var freelook = Selection.activeGameObject.GetComponent<CinemachineFreeLook>();
                displayed = freelook != null;
            };
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
            // CinemachineSceneToolUtility.RegisterToolHandlers(GetType(), isOn => {}, 
            //     display => style.display = display ? DisplayStyle.Flex : DisplayStyle.None);
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
}
#endif