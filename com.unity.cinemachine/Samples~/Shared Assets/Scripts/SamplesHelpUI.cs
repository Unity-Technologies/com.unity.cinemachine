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

        VisualElement m_HelpBox;
        Button m_HelpButton;

        void OnEnable()
        {
            var uiDocument = GetComponent<UIDocument>();
            m_HelpButton = uiDocument.rootVisualElement.Q("HelpButton") as Button;
            if (m_HelpButton == null)
            {
                Debug.LogError("Cannot find HelpToggleBox.  Is the source asset set in the UIDocument?");
                return;
            }
            m_HelpButton.RegisterCallback(new EventCallback<ClickEvent>(_ => OpenHelpBox()));
            
            m_HelpBox = uiDocument.rootVisualElement.Q("HelpTextBox");
            if (uiDocument.rootVisualElement.Q("HelpTextBox__Title") is Label helpTitle) 
                helpTitle.text = string.IsNullOrEmpty(HelpTitle) ? SceneManager.GetActiveScene().name : HelpTitle;

            if (uiDocument.rootVisualElement.Q("HelpTextBox__TextField") is TextField helpText)
                helpText.value = HelpText;
            if (uiDocument.rootVisualElement.Q("HelpTextBox__CloseButton") is Button closeButton) 
                closeButton.RegisterCallback<ClickEvent>(_ => CloseHelpBox());
            
            m_HelpBox.visible = VisibleAtStart;
            m_HelpButton.visible = !VisibleAtStart;
        }

        void OnDisable()
        {
            CloseHelpBox();
            m_HelpButton.UnregisterCallback(new EventCallback<ClickEvent>(_ => OpenHelpBox()));
            m_HelpButton.visible = false;
        }
        
        void OpenHelpBox()
        {
            if (m_HelpButton != null)
                m_HelpButton.visible = false;
            if (m_HelpBox != null)
                m_HelpBox.visible = true;
        }
        
        void CloseHelpBox()
        {
            if (m_HelpButton != null)
                m_HelpButton.visible = true;
            if (m_HelpBox != null)
                m_HelpBox.visible = false;
            
            OnHelpDismissed.Invoke();
        }

        
    }
}
