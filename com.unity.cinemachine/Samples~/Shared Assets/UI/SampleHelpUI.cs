using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Unity.Cinemachine.Samples
{
    /// <summary>
    /// Displays a button in the game view that will bring up a window with scrollable text.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class SampleHelpUI : MonoBehaviour
    {
        public bool VisibleAtStart; // TODO: no sync
        public string HelpTitle;
        [TextArea(minLines: 10, maxLines: 50)]
        public string HelpText;

        VisualElement m_HelpBox, m_HelpToggleBox;
        Toggle m_HelpToggle;

        void OnEnable()
        {
            // try to center vertically too (horizontally it is) alignSelf centered
            var uiDocument = GetComponent<UIDocument>();
            
            m_HelpToggleBox = uiDocument.rootVisualElement.Q("HelpToggleBox");
            m_HelpToggle = new Toggle("Help");
            m_HelpToggle.AddToClassList("helpToggle");
            m_HelpToggle.RegisterValueChangedCallback(evt => m_HelpBox.visible = evt.newValue);
            m_HelpToggleBox.Add(m_HelpToggle);
            
            m_HelpBox = uiDocument.rootVisualElement.Q("HelpTextBox");
            if (uiDocument.rootVisualElement.Q("HelpTextBox__Title") is Label helpTitle)
            {
                if (HelpTitle == string.Empty)
                    HelpTitle = SceneManager.GetActiveScene().name;
                helpTitle.text = HelpTitle;
            }

            if (uiDocument.rootVisualElement.Q("HelpTextBox__TextField") is TextField helpText)
                helpText.value = HelpText;
            if (uiDocument.rootVisualElement.Q("HelpTextBox_CloseButton") is Button closeButton) 
                closeButton.RegisterCallback<ClickEvent>(_ => CloseHelpBox());
            
            m_HelpBox.visible = m_HelpToggle.value = VisibleAtStart;
        }

        void OnDisable()
        {
            CloseHelpBox();
            m_HelpToggleBox.Remove(m_HelpToggle);
        }

        void CloseHelpBox() => m_HelpToggle.value = false;
    }
}
