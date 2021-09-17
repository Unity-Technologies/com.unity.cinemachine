using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEditor.EditorTools;
using UnityEditor.Toolbars;
using UnityEditor.Overlays;
using UnityEngine.UIElements;
using UnityEditor;

// Use [EditorToolbarElement(Identifier, EditorWindowType)] to register toolbar elements for use in ToolbarOverlay implementation.
[EditorToolbarElement(id, typeof(SceneView))]
class DropdownExample : EditorToolbarDropdown
{
    public const string id = "ExampleToolbar/Dropdown";

    static string dropChoice = null;

    public DropdownExample()
    {
        text = "Axis";
        clicked += ShowDropdown;
    }

    void ShowDropdown()
    {
        var menu = new GenericMenu();
        menu.AddItem(new GUIContent("X"), dropChoice == "X", () => { text = "X"; dropChoice = "X"; });
        menu.AddItem(new GUIContent("Y"), dropChoice == "Y", () => { text = "Y"; dropChoice = "Y"; });
        menu.AddItem(new GUIContent("Z"), dropChoice == "Z", () => { text = "Z"; dropChoice = "Z"; });
        menu.ShowAsContext();
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
class DropdownToggleExample : EditorToolbarDropdownToggle, IAccessContainerWindow
{
    public const string id = "ExampleToolbar/DropdownToggle";

    // This property is specified by IAccessContainerWindow and is used to access the Overlay's EditorWindow.
    public EditorWindow containerWindow { get; set; }
    static int colorIndex = 0;
    static readonly Color[] colors = new Color[] { Color.red, Color.green, Color.cyan };
    public DropdownToggleExample()
    {
        text = "Color Bar";
        tooltip = "Display a color rectangle in the top left of the Scene view. Toggle on or off, and open the dropdown" +
                  "to change the color.";

        // When the dropdown is opened, ShowColorMenu is invoked and we can create a popup menu.
        dropdownClicked += ShowColorMenu;
        
        // Subscribe to the Scene view OnGUI callback so that we can draw our color swatch.
        SceneView.duringSceneGui += DrawColorSwatch;
    }

    void DrawColorSwatch(SceneView view)
    {
        // Test that this callback is for the Scene View that we're interested in, and also check if the toggle is on
        // or off (value).
        if (view != containerWindow || !value)
        {
            return;
        }

        Handles.BeginGUI();
        GUI.color = colors[colorIndex];
        GUI.DrawTexture(new Rect(8, 8, 120, 24), Texture2D.whiteTexture);
        GUI.color = Color.white;
        Handles.EndGUI();
    }

    // When the dropdown button is clicked, this method will create a popup menu at the mouse cursor position.
    void ShowColorMenu()
    {
        var menu = new GenericMenu();
        menu.AddItem(new GUIContent("Red"), colorIndex == 0, () => colorIndex = 0);
        menu.AddItem(new GUIContent("Green"), colorIndex == 1, () => colorIndex = 1);
        menu.AddItem(new GUIContent("Blue"), colorIndex == 2, () => colorIndex = 2);
        menu.ShowAsContext();
    }
}

[EditorToolbarElement(id, typeof(SceneView))]
class CreateCube : EditorToolbarButton//, IAccessContainerWindow
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
[Overlay(typeof(SceneView), "ElementToolbars Example")]
// IconAttribute provides a way to define an icon for when an Overlay is in collapsed form. If not provided, the name initials are used.
[Icon("Assets/unity.png")]

// Toolbar Overlays must inherit `ToolbarOverlay` and implement a parameter-less constructor. The contents of a toolbar are populated with string IDs, which are passed to the base constructor. IDs are defined by EditorToolbarElementAttribute.
public class EditorToolbarExample : ToolbarOverlay
{
    // ToolbarOverlay implements a parameterless constructor, passing the EditorToolbarElementAttribute ID. 
    // This is the only code required to implement a toolbar Overlay. Unlike panel overlays, the contents are defined
    // as standalone pieces that will be collected to form a strip of elements.

    EditorToolbarExample() : base(
        CreateCube.id,
        FollowOffsetTool.id,
        DropdownExample.id,
        DropdownToggleExample.id
        )
    { }
}

/*
public class EditorToolbarExample : Overlay
{
    public override VisualElement CreatePanelContent()
    {
        var root = new VisualElement() { name = "My Tool Root" };
        root.Add(new CreateCube());
        root.Add(new FollowOffsetTool());
        root.Add(new DropdownExample());
        root.Add(new DropdownToggleExample());
        return root;
    }
}
*/
