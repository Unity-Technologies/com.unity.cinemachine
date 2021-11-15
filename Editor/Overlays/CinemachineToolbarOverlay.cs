#if UNITY_2021_2_OR_NEWER
using System;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

namespace Cinemachine.Editor
{
    /// <summary>
    /// TODO: describe ux
    /// </summary>
    public class CinemachineTool : EditorTool, IDrawSelectedHandles
    {
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

        // IsAvailable() can be polled frequently, make sure that it is not an expensive check
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
                text = "Field of View Tool",
                tooltip = "Field of View Tool",
            };
        }
    }
    
    [EditorTool("Far/Near Clip Tool", typeof(CinemachineVirtualCameraBase))]
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
                text = "Far/Near Clip Tool",
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
                text = "Follow Offset Tool",
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
                text = "Tracked Object Offset Tool",
                tooltip = "Tracked Object Offset Tool",
            };
        }
    }

    // TODO: FreelookRigSelection dropdown needs to be in the Tools Settings toolbar
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
}
#endif