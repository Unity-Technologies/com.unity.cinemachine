using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEditor.EditorTools;
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
            this.RegisterValueChangedCallback(Test);
        }

        void Test(ChangeEvent<bool> evt)
        {
            if (evt.newValue)
            {
                Debug.Log("FoVTool ON");
            }
            else
            {
                Debug.Log("FoVTool OFF");
            }
        }
    }

    [EditorToolbarElement(id, typeof(SceneView))]
    class FarNearClipTool : EditorToolbarToggle
    {
        public const string id = "FarNearClipTool/Toggle";

        public FarNearClipTool()
        {
            icon = EditorGUIUtility.IconContent("d_BillboardRenderer Icon").image as Texture2D;
            this.RegisterValueChangedCallback(Test);
        }

        void Test(ChangeEvent<bool> evt)
        {
            if (evt.newValue)
            {
                Debug.Log("FarNearClipTool ON");
            }
            else
            {
                Debug.Log("FarNearClipTool OFF");
            }
        }
    }

    [EditorToolbarElement(id, typeof(SceneView))]
    class FollowOffsetTool : EditorToolbarToggle
    {
        public const string id = "FollowOffsetTool/Toggle";

        public FollowOffsetTool()
        {
            icon = EditorGUIUtility.IconContent("MoveTool@2x").image as Texture2D;
            this.RegisterValueChangedCallback(Test);
            this.RegisterValueChangedCallback(CinemachineVirtualCameraToolbarHandleDrawer.FollowOffsetToolSelection);
        }

        void Test(ChangeEvent<bool> evt)
        {
            if (evt.newValue)
            {
                Debug.Log("FollowOffsetTool ON");
            }
            else
            {
                Debug.Log("FollowOffsetTool OFF");
            }
        }
    }

    [EditorToolbarElement(id, typeof(SceneView))]
    class TrackedObjectOffsetTool : EditorToolbarToggle
    {
        public const string id = "TrackedObjectOffsetTool/Toggle";

        public TrackedObjectOffsetTool()
        {
            icon = EditorGUIUtility.IconContent("d_Toolbar Plus@2x").image as Texture2D;
            this.RegisterValueChangedCallback(Test);
            this.RegisterValueChangedCallback(CinemachineVirtualCameraToolbarHandleDrawer.TrackedObjectOffsetToolSelection);
        }

        void Test(ChangeEvent<bool> evt)
        {
            if (evt.newValue)
            {
                Debug.Log("TrackedObjectOffsetTool ON");
            }
            else
            {
                Debug.Log("TrackedObjectOffsetTool OFF");
            }
        }
    }

    [EditorToolbarElement(id, typeof(SceneView))]
    class CreateCube : EditorToolbarButton //, IAccessContainerWindow
    {
        // This ID is used to populate toolbar elements.
        public const string id = "ExampleToolbar/Button";

        // IAccessContainerWindow provides a way for toolbar elements to access the `EditorWindow` in which they exist.
        // Here we use `containerWindow` to focus the camera on our newly instantiated objects after creation.
        //public EditorWindow containerWindow { get; set; }

        // Because this is a VisualElement, it is appropriate to place initialization logic in the constructor.
        // In this method you can also register to any additional events as required. In this example there is a tooltip, an icon, and an action.


        public CreateCube()
        {
            // A toolbar element can be either text, icon, or a combination of the two. Keep in mind that if a toolbar is
            // docked horizontally the text will be clipped, so usually it's a good idea to specify an icon.
            icon = EditorGUIUtility.IconContent("d_PreMatCube@2x").image as Texture2D;
            tooltip = "Instantiate a cube in the scene.";
            clicked += OnClick;
        }

        // This method will be invoked when the `Create Cube` button is clicked.
        void OnClick()
        {
            var newObj = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;

            // When writing editor tools don't forget to be a good citizen and implement Undo!
            Undo.RegisterCreatedObjectUndo(newObj.gameObject, "Create Cube");

            //if (containerWindow is SceneView view)
            //    view.FrameSelected();
        }

    }

// All Overlays must be tagged with the OverlayAttribute
    [Overlay(typeof(SceneView), "Cm Vcam Tools")]

// IconAttribute provides a way to define an icon for when an Overlay is in collapsed form. If not provided, the name initials are used.
    [Icon("Assets/unity.png")]

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