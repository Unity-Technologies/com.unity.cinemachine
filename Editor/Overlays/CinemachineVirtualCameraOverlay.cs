using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

[Overlay(typeof(SceneView), "CinemachineVirtualCameraOverlay", true)]
public class CinemachineVirtualCameraOverlay : Overlay
{
    public override VisualElement CreatePanelContent()
    {
        var root = new VisualElement() { name = "My Toolbar Root" };
        root.Add(new Label() { text = "Hello" });
        return root;
    }
}
