using System;
using UnityEngine;
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
        Button m_CloseButton;

        void OnEnable()
        {
            // try to center vertically too (horizontally it is) alignSelf centered
            var uiDocument = GetComponent<UIDocument>();
            
            m_HelpToggleBox = uiDocument.rootVisualElement.Q("HelpToggleBox");
            m_HelpToggle = new Toggle("Help")
            {
                visible = VisibleAtStart
            };
            m_HelpToggle.AddToClassList("helpToggle");
            m_HelpToggleBox.Add(m_HelpToggle);
            m_HelpBox = uiDocument.rootVisualElement.Q("HelpBox");
            if (uiDocument.rootVisualElement.Q("HelpTitle") is Label helpTitle)
                helpTitle.text = HelpTitle;
            if (uiDocument.rootVisualElement.Q("HelpTextField") is TextField helpText)
                helpText.value = HelpText;
            m_CloseButton = uiDocument.rootVisualElement.Q("CloseButton") as Button;
            
            m_HelpToggle.RegisterValueChangedCallback(evt => m_HelpBox.visible = evt.newValue);
            m_CloseButton.RegisterCallback<ClickEvent>(_ => m_HelpToggle.value = false);
        }

        void OnDisable() => m_HelpToggleBox.Remove(m_HelpToggle);
    }
}
