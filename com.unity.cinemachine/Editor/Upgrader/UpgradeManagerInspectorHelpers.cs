#if !CINEMACHINE_NO_CM2_SUPPORT
using UnityEngine;
using UnityEditor;
using System;
using UnityEngine.UIElements;
using UnityEditor.SceneManagement;

namespace Unity.Cinemachine.Editor
{
    static class UpgradeManagerInspectorHelpers
    {
        public static void IMGUI_DrawUpgradeControls(UnityEditor.Editor editor, string className)
        {
            var attrs = editor.serializedObject.targetObject.GetType()
                .GetCustomAttributes(typeof(ObsoleteAttribute), true);
            if (attrs != null && attrs.Length > 0)
            {
                var pos = EditorGUILayout.GetControlRect(false, 1);
                InspectorUtility.HelpBoxWithButton(
                    "Cinemachine can upgrade your project data automatically.", MessageType.Info,
                    new GUIContent("Upgrade Now..."), () =>
                    {
                        UnityEditor.PopupWindow.Show(pos, new UpgraderPopup() { Editor = editor, ClassName = className });
                    });
                EditorGUILayout.Space();
            }
        }

        class UpgraderPopup : PopupWindowContent
        {
            public UnityEditor.Editor Editor;
            public string ClassName;

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
                        + "reference obsolete classes and APIs, they will probably break.  "
                        + "Please see the <a href=\"" + Documentation.BaseURL + "manual/CinemachineUpgradeFrom2.html\">Cinemachine Upgrade Guide</a> "
                        + "for tips and techniques to smooth the upgrade process.\n\n"
                        + "<b>NOTE:</b> Error and warning messages may be logged to the console window during this process.",
                    enableRichText = true,
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
                        + "are prefabs or prefab instances.  Undo is supported for this operation.",
                    style = { marginLeft = 10, marginRight = 10, marginTop = 10, marginBottom = 10, alignSelf = Align.Center }
                });
                var text = "Upgrade this object to " + ClassName;
                ux.AddChild(new Button(() =>
                {
                    Undo.SetCurrentGroupName(text);
                    for (int i = 0; i < Editor.targets.Length; ++i)
                        CinemachineUpgradeManager.UpgradeSingleObject(((MonoBehaviour)Editor.targets[i]).gameObject);
                    editorWindow.Close();
                }) { 
                    text = text, 
                    style = { flexGrow = 0, alignSelf = Align.Center } 
                }).SetEnabled(PrefabStageUtility.GetCurrentPrefabStage() == null && !CinemachineUpgradeManager.ObjectsUsePrefabs(Editor.targets));

                // Upgrade current scene
                ux.AddChild(new TextElement()
                {
                    text = "Unity can upgrade all the Cinemachine objects in the current scene, but only if none of them "
                        + "are prefabs or prefab instances.  Undo is supported for this operation.",
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
                }).SetEnabled(PrefabStageUtility.GetCurrentPrefabStage() == null && !CinemachineUpgradeManager.CurrentSceneUsesPrefabs());

                // Upgrade project
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
                }) { text = "Upgrade Entire Project to Cinemachine 3...", style = { flexGrow = 0, alignSelf = Align.Center } });
                
                ux.AddSpace();
            }
        }
    }
}
#endif
