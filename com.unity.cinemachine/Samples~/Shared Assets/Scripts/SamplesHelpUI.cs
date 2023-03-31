using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Unity.Cinemachine.Samples
{
    /// <summary>
    /// Displays a button in the game view that will bring up a window with scrollable text.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class SamplesHelpUI : MonoBehaviour
    {
        public bool VisibleAtStart;
        public string HelpTitle;
        [TextArea(minLines: 10, maxLines: 50)]
        public string HelpText;

        [Tooltip("Event sent when the help window is dismissed")]
        public UnityEvent OnHelpDismissed = new ();

        VisualElement m_HelpBox, m_HelpToggleBox;
        Toggle m_HelpToggle;

        void OnEnable()
        {
            var uiDocument = GetComponent<UIDocument>();
            m_HelpToggleBox = uiDocument.rootVisualElement.Q("HelpToggleBox");
            if (m_HelpToggleBox == null)
            {
                Debug.LogError("Cannot find HelpToggleBox.  Is the source asset set in the UIDocument?");
                return;
            }
            m_HelpToggle = new Toggle("Help");
            m_HelpToggle.AddToClassList("helpToggle");
            m_HelpToggle.RegisterValueChangedCallback((evt) => 
            {
                m_HelpBox.visible = evt.newValue;
                if (!evt.newValue)
                    OnHelpDismissed.Invoke();
            });
            m_HelpToggleBox.Add(m_HelpToggle);
            
            m_HelpBox = uiDocument.rootVisualElement.Q("HelpTextBox");
            if (uiDocument.rootVisualElement.Q("HelpTextBox__Title") is Label helpTitle) 
                helpTitle.text = string.IsNullOrEmpty(HelpTitle) ? SceneManager.GetActiveScene().name : HelpTitle;

            if (uiDocument.rootVisualElement.Q("HelpTextBox__TextField") is TextField helpText)
                helpText.value = HelpText;
            if (uiDocument.rootVisualElement.Q("HelpTextBox_CloseButton") is Button closeButton) 
                closeButton.RegisterCallback<ClickEvent>(_ => CloseHelpBox());
            
            m_HelpBox.visible = m_HelpToggle.value = VisibleAtStart;
        }

        void OnDisable()
        {
            CloseHelpBox();
            m_HelpToggleBox?.Remove(m_HelpToggle);
        }

        void CloseHelpBox()
        {
            if (m_HelpToggle != null)
                m_HelpToggle.value = false;
        }
    }
}
