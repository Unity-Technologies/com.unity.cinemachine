using System.Collections;
using UnityEngine;
using UnityEditor;
using System;

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
#if false // For testing only - do not release with this because no prefab support and no animation fixup
                if (GUI.Button(EditorGUILayout.GetControlRect(), new GUIContent(buttonText)))
                {
                    Undo.SetCurrentGroupName(buttonText);
                    for (int i = 0; i < editor.targets.Length; ++i)
                        CinemachineUpgradeManager.UpgradeSingleObject(((MonoBehaviour)editor.targets[i]).gameObject);
                    GUIUtility.ExitGUI();
                }
                if (GUI.Button(EditorGUILayout.GetControlRect(), new GUIContent("Upgrade all objects in Scene...")))
                {
                    Undo.SetCurrentGroupName("Upgrade all objects in Scene");
                    CinemachineUpgradeManager.UpgradeObjectsInCurrentScene();
                    GUIUtility.ExitGUI();
                }
#endif
                if (GUI.Button(EditorGUILayout.GetControlRect(), new GUIContent("Upgrade Project to Cinemachine 3...")))
                {
                    CinemachineUpgradeManager.UpgradeProject();
                    GUIUtility.ExitGUI();
                }
                EditorGUILayout.Space();
            }
        }
    }
}
