using System.Collections;
using UnityEngine;
using UnityEditor;
using System;
using UnityEngine.UIElements;

namespace Cinemachine.Editor
{
    static class UpgradeManagerInspectorHelpers
    {
        public static void DrawUpgradeControls(UnityEditor.Editor editor, string buttonText)
        {
            var attrs = editor.serializedObject.targetObject.GetType()
                .GetCustomAttributes(typeof(ObsoleteAttribute), true);
            if (attrs != null && attrs.Length > 0)
            {
                var pos = EditorGUILayout.GetControlRect(false, 1);
                InspectorUtility.HelpBoxWithButton(
                    "Cinemachine can upgrade your project data automatically", MessageType.Info,
                    new GUIContent("Learn more..."), () =>
                    {
                        UnityEditor.PopupWindow.Show(pos, new UpgraderPopup() { Editor = editor, ButtonText = buttonText });
                    });
                EditorGUILayout.Space();
            }
        }

        class UpgraderPopup : PopupWindowContent
        {
            public UnityEditor.Editor Editor;
            public string ButtonText;

            // This defines the window width and the max window height
            Vector2 m_windowSize = new(400, 600);

            public override Vector2 GetWindowSize()
            {
                return m_windowSize;
            }

            public override void OnGUI(Rect rect) {}

            public override void OnOpen()
            {
                var ux = editorWindow.rootVisualElement.AddChild(new VisualElement() 
                {
                    style = { alignContent = Align.Center, alignItems = Align.FlexStart } 
                });
                ux.RegisterCallback<GeometryChangedEvent>((e) => m_windowSize = e.newRect.size);

                // Header
                ux.AddChild(new TextElement()
                {
                    text = "Cinemachine Upgrader",
                    style = { unityFontStyleAndWeight = FontStyle.Bold, marginTop = 10, alignSelf = Align.Center }
                });
                ux.AddChild(new TextElement()
                {
                    text = "Unity can upgrade the Cinemachine data in this project to the new format.  However, custom "
                        + "scripts that interact with these objects will not necessarily be upgraded.  If your custom scripts "
                        + "reference obsolete classes and APIs, they will probably break.  Please see the Cinemachine Upgrade Guide "
                        + "for tips and techniques to smooth the upgrade process.",
                    style = { marginLeft = 10, marginRight = 10, marginTop = 10, marginBottom = 10, alignSelf = Align.Center }
                });
                ux.AddChild(new Image()
                {
                    image = CinemachineSettings.CinemachineLogoTexture,
                    style = { alignSelf = Align.Center, maxHeight = 64 }
                });

                // Upgrade current object
                ux.AddChild(new TextElement()
                {
                    text = "Unity can upgrade the objects currently being inspected, but only if none of them "
                        + "are prefab instances.  Undo is supported for this operation.",
                    style = { marginLeft = 10, marginRight = 10, marginTop = 10, marginBottom = 10, alignSelf = Align.Center }
                });
                ux.AddChild(new Button(() =>
                {
                    Undo.SetCurrentGroupName(ButtonText);
                    for (int i = 0; i < Editor.targets.Length; ++i)
                        CinemachineUpgradeManager.UpgradeSingleObject(((MonoBehaviour)Editor.targets[i]).gameObject);
                    editorWindow.Close();
                }) { 
                    text = ButtonText, 
                    style = { flexGrow = 0, alignSelf = Align.Center } 
                }).SetEnabled(!CinemachineUpgradeManager.ObjectsUsePrefabs(Editor.targets));

                // Upgrade current scene
                ux.AddChild(new TextElement()
                {
                    text = "Unity can upgrade all the Cinemachine objects in the current scene, but only if none of them "
                        + "are prefab instances.  Undo is supported for this operation.",
                    style = { marginLeft = 10, marginRight = 10, marginTop = 20, marginBottom = 10, alignSelf = Align.Center }
                });
                ux.AddChild(new Button(() =>
                {
                    Undo.SetCurrentGroupName("Upgrade all objects in Scene");
                    CinemachineUpgradeManager.UpgradeObjectsInCurrentScene();
                    editorWindow.Close();
                }) { 
                    text = "Upgrade all objects in Scene", 
                    style = { flexGrow = 0, alignSelf = Align.Center } 
                }).SetEnabled(!CinemachineUpgradeManager.CurrentSceneUsesPrefabs());

                // Upgrde project
                ux.AddChild(new TextElement()
                {
                    text = "Unity can upgrade all the Cinemachine objects in the project's scenes and prefabs "
                        + "to the new data format.  Undo is NOT supported for this operation, so be sure to make a backup first.",
                    style = { marginLeft = 10, marginRight = 10, marginTop = 20, marginBottom = 10, alignSelf = Align.Center }
                });
                ux.AddChild(new Button(() =>
                {
                    CinemachineUpgradeManager.UpgradeProject();
                    editorWindow.Close();
                }) { text = "Upgrade all Project data to Cinemachine 3...", style = { flexGrow = 0, alignSelf = Align.Center } });
                
                ux.AddSpace();
            }
        }
    }
}
