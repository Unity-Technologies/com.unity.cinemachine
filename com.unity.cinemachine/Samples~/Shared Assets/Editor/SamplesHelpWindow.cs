using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditorInternal;

namespace Cinemachine.Examples
{
    class SamplesHelpWindow : EditorWindow
    {
        CinemachineInformations[] m_DisplayedInformation;
        bool m_IsShown;
        int m_Counter;
        GameObject m_CurrentSelection;
        Label m_FeatureLabel;
        Label m_DescriptionLabel;
        Toolbar m_Toolbar;
        bool m_EnsureExpanded;
        int m_CurrentlyShown;
        
        public static void Init(CinemachineInformations[] displayedInformation, int selected)
        {
            SamplesHelpWindow samplesHelpWindow = (SamplesHelpWindow) GetWindow(typeof(SamplesHelpWindow), false, "Cinemachine sample");
            samplesHelpWindow.m_CurrentlyShown = selected;
            samplesHelpWindow.maxSize = new Vector2(300f, 110f);
            samplesHelpWindow.minSize = samplesHelpWindow.maxSize;
            samplesHelpWindow.Show();
            samplesHelpWindow.m_DisplayedInformation = displayedInformation;
            samplesHelpWindow.m_IsShown = false;
            samplesHelpWindow.m_Counter = 0;
            samplesHelpWindow.m_CurrentSelection = Selection.activeGameObject;
            samplesHelpWindow.UpdateText();
        }

        /*void InitializeToolbar()
        {
            if(m_Toolbar != null)
                m_Toolbar.Clear();
            
            for (var i = 0; i < m_DisplayedInformation.Length; i++)
            {
                var currentInformation = i;
                var button = new ToolbarButton(() =>
                {
                    m_Counter = 0;
                    m_EnsureExpanded = false;
                    m_IsShown = false;
                    m_CurrentlyShown = currentInformation;
                    Selection.activeGameObject = m_DisplayedInformation[currentInformation].target;
                    m_CurrentSelection = Selection.activeGameObject;
                    UpdateText();
                });
                button.text = m_DisplayedInformation[i].feature;
                if (m_CurrentlyShown == i)
                {
                    button.Focus();
                }
                m_Toolbar.Add(button);
            }
        }*/
        
        void UpdateText()
        {
            if (m_DisplayedInformation is not null && m_DisplayedInformation.Length > 0)
            {
                titleContent.text = m_DisplayedInformation[m_CurrentlyShown].feature;
                m_DescriptionLabel.text = m_DisplayedInformation[m_CurrentlyShown].description;
                m_FeatureLabel.text = m_DisplayedInformation[m_CurrentlyShown].feature;
                titleContent.text = m_DisplayedInformation[m_CurrentlyShown].targetComponent;
                m_EnsureExpanded = false;
                m_IsShown = false;
                m_Counter = 0;
            }
        }

        int GetNextItem()
        {
            var nextItem = m_CurrentlyShown + 1;
            if (nextItem >= m_DisplayedInformation.Length)
                nextItem = 0;
            return nextItem;
        }
        
        void OnDestroy()
        {
            Highlighter.Stop();
        }
        
        public void CreateGUI()
        {
            m_FeatureLabel = new Label();
            m_FeatureLabel.style.fontSize = 14;
            m_DescriptionLabel = new Label();
            GroupBox groupBox = new GroupBox();
            m_DescriptionLabel.style.whiteSpace = new StyleEnum<WhiteSpace>(WhiteSpace.Normal);
            groupBox.Add(m_DescriptionLabel);
            rootVisualElement.Add(m_FeatureLabel);
            rootVisualElement.Add(groupBox);
            m_Toolbar = new Toolbar();
            m_Toolbar.style.position = new StyleEnum<Position>(Position.Absolute);
            m_Toolbar.style.bottom = 0;
            m_Toolbar.style.left = 0;
            m_Toolbar.style.right = 0;
            var nextButton = new Button(() =>
            {
                m_CurrentlyShown = GetNextItem();
                m_EnsureExpanded = false;
                m_IsShown = false;
                Selection.activeGameObject = m_DisplayedInformation[m_CurrentlyShown].target;
                m_CurrentSelection = Selection.activeGameObject;
                UpdateText();
            });
            nextButton.text = "Next";
            var previousButton = new Button(() =>
            {
                m_CurrentlyShown = GetNextItem();
                m_EnsureExpanded = false;
                m_IsShown = false;
                Selection.activeGameObject = m_DisplayedInformation[m_CurrentlyShown].target;
                m_CurrentSelection = Selection.activeGameObject;
                UpdateText();
            });
            nextButton.style.alignSelf = new StyleEnum<Align>(Align.Center);
            previousButton.text = "Previous";
            rootVisualElement.Add(m_Toolbar);
            m_Toolbar.Add(previousButton);
            m_Toolbar.Add(nextButton);
            UpdateText();
        }

        void Update()
        {
            if (!m_EnsureExpanded)
            {
                Assembly cinemachineAssembly = m_DisplayedInformation[m_CurrentlyShown].assembly;
                string nameSpace = m_DisplayedInformation[m_CurrentlyShown].assemblyName;
                var targetType = cinemachineAssembly.GetType($"{nameSpace}.{m_DisplayedInformation[m_CurrentlyShown].targetComponent}");
                var targetComponent = m_DisplayedInformation[m_CurrentlyShown].target.GetComponent(targetType);
                InternalEditorUtility.SetIsInspectorExpanded(targetComponent, true);
                m_EnsureExpanded = true;
            }
            
            if (Selection.activeGameObject != m_CurrentSelection)
                Close();
            
            m_Counter++;
            if(m_Counter < 2)
                return;

            if (!m_IsShown)
            {
                m_IsShown = Highlighter.Highlight("Inspector", m_DisplayedInformation[m_CurrentlyShown].feature);
                if (!m_IsShown)
                {
                    var sampleHelp = FindObjectOfType<SamplesHelpGUI>();
                    sampleHelp.InvalidTutorial();
                    Selection.activeGameObject = sampleHelp.gameObject;
                    m_IsShown = true;
                }
            }
        }
    }
}
