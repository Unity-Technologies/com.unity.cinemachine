using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Cinemachine.Examples
{
    [CustomEditor(typeof(SamplesHelpGUI))]
    [CanEditMultipleObjects]
    public class SamplesHelpInspector : UnityEditor.Editor
    {
#if EditCinemachineSamples //We don't display the editor by default to simplify the interface for users.
        SerializedProperty m_ButtonText;
        SerializedProperty m_Position;
        SerializedProperty m_Displayed;
#endif
        SerializedProperty m_HelpText;
        SerializedProperty m_SampleInformations;
        GUIStyle m_TextStyle;
        List<CinemachineInformations> m_Informations;

        const string k_ShowButtonText = "Show";
        const string k_SelectButtonText = "Select";
        const string k_InspectorWindowName = "Inspector";

        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new VisualElement();
            
#if EditCinemachineSamples
            m_ButtonText = serializedObject.FindProperty("ButtonText");
            m_Position = serializedObject.FindProperty("Position");
            m_Displayed = serializedObject.FindProperty("Displayed");
#endif
            
            m_HelpText = serializedObject.FindProperty("HelpText");
            m_SampleInformations = serializedObject.FindProperty("sampleInformations");
            if (m_Informations == null)
            {
                m_Informations = new List<CinemachineInformations>();
                var arraySize = m_SampleInformations.arraySize;
                for (var i = 0; i < arraySize; i++)
                {
                    var sampleInformation =
                        (CinemachineInformations) m_SampleInformations.GetArrayElementAtIndex(i).boxedValue;
                    m_Informations.Add(sampleInformation);
                }
            }
            
            var isTutorialValid = serializedObject.FindProperty("isTutorialValid");
            for (var i = 0; i < m_Informations.Count; i++)
            {
                if (m_Informations[i].target == null)
                {
                    isTutorialValid.boolValue = false;
                    serializedObject.ApplyModifiedProperties();
                }
            }
            
#if EditCinemachineSamples
            root.Add(new PropertyField(m_ButtonText));
            root.Add(new PropertyField(m_Position));
            root.Add(new PropertyField(m_Displayed));
            root.Add(new PropertyField(m_HelpText));
            root.Add(new PropertyField(m_SampleInformations));
            root.Add(new PropertyField(isTutorialValid));
#endif
            Label about = new Label("About");
            about.style.fontSize = 14;
            root.Add(about);
            GroupBox aboutGroup = new GroupBox();
            Label textField = new Label(m_HelpText.stringValue);
            textField.style.whiteSpace = new StyleEnum<WhiteSpace>(WhiteSpace.Normal);
            aboutGroup.Add(textField);
            root.Add(aboutGroup);
            Label title = new Label("Features");
            title.style.fontSize = 14;
            root.Add(title);
            if (m_Informations.Count == 0)
                title.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);

            for (var i = 0; i < m_Informations.Count; i++)
            {
                var value = i;
                Button button = new Button(() =>
                {
                    // Needs to be checked before Multiple inspector check otherwise will loose the value
                    if (ActiveEditorTracker.sharedTracker.isLocked) 
                    {
                        if (!EditorUtility.DisplayDialog("Unlock inspector", "Unlock inspector to continue.",
                                "Unlock", "Cancel"))
                            return;

                        ActiveEditorTracker.sharedTracker.isLocked = false;
                    }

                    if (IsMultipleInspectors())
                    {
                        if (!EditorUtility.DisplayDialog("Multiple inspectors",
                                "Multiple inspectors is not supported. \n \nNote: We recommend using the default layout (Top right) when using the samples.",
                                "Close inspector", "Cancel"))
                            return;

                        KeepOneInspector();
                    }

                    Selection.activeGameObject = m_Informations[value].target;
                    SamplesHelpWindow.Init(m_Informations.ToArray(), value);

                    Highlighter.Stop();
                });
                button.RegisterCallback<MouseEnterEvent>(
                    e =>
                    {
                        if (!ActiveEditorTracker.sharedTracker.isLocked && isTutorialValid.boolValue && SceneManager.sceneCount == 1)
                        {
                            button.text = k_SelectButtonText;
                            EditorGUIUtility.PingObject(m_Informations[value].target);
                        }
                    });
                button.RegisterCallback<MouseOutEvent>(
                    e => { button.text = k_ShowButtonText; });
                button.style.width = new StyleLength(60);
                GroupBox groupBox = new GroupBox();
                Label label = new Label($"{m_Informations[i].targetComponent}: {m_Informations[i].feature}");
                label.style.unityTextAlign = new StyleEnum<TextAnchor>(TextAnchor.MiddleCenter);
                groupBox.Add(button);
                button.SetEnabled(isTutorialValid.boolValue && SceneManager.sceneCount == 1);
                groupBox.Add(label);
                groupBox.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);
                button.text = k_ShowButtonText;
                root.Add(groupBox);
            }

            if (m_Informations.Count > 0)
            {
                if (!isTutorialValid.boolValue)
                {
                    HelpBox helpBox = new HelpBox("Reimport this sample to enable.", HelpBoxMessageType.Info);
                    root.Add(helpBox);
                }
                else if (SceneManager.sceneCount > 1)
                {
                    HelpBox helpBox = new HelpBox("Multi-scene is not supported. Load only one to enable.",
                        HelpBoxMessageType.Info);
                    root.Add(helpBox);
                }
            }


            return root;
        }

        void KeepOneInspector()
        {
            EditorWindow[] windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
            var inspectorCount = 0;
            for (var i = windows.Length - 1; i >= 0; i--) // We process the list in reverse to keep the tracked window ActiveEditorTracker.sharedTracker.isLocked 
            {
                if (windows[i].titleContent.text == k_InspectorWindowName)
                {
                    inspectorCount++;
                    if (inspectorCount > 1)
                        windows[i].Close();
                }
            }
        }

        bool IsMultipleInspectors()
        {
            EditorWindow[] windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
            var inspectorCount = 0;
            foreach (var window in windows)
            {
                if (window.titleContent.text == k_InspectorWindowName)
                {
                    inspectorCount++;
                    if (inspectorCount > 1)
                        return true;
                }
            }

            return false;
        }
    }
}