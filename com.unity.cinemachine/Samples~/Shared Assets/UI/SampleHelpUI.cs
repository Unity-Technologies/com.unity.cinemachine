using System;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Displays a button in the game view that will bring up a window with scrollable text.
/// </summary>
public class SampleHelpUI : MonoBehaviour
{
    public bool IsVisible;
    public string HelpTitle;
    [TextArea(minLines: 10, maxLines: 50)]
    public string HelpText;

    VisualElement m_HelpBox;
    Toggle m_HelpToggle;
    Button m_CloseButton;

    void OnEnable()
    {
        var uiDocument = GetComponent<UIDocument>();

        m_HelpToggle = uiDocument.rootVisualElement.Q("HelpToggle") as Toggle;
        m_HelpBox = uiDocument.rootVisualElement.Q("HelpBox");
        if (uiDocument.rootVisualElement.Q("HelpTitle") is Label helpTitle)
            helpTitle.text = HelpTitle;
        if (uiDocument.rootVisualElement.Q("HelpTextField") is TextField helpText)
            helpText.value = HelpText;
        m_CloseButton = uiDocument.rootVisualElement.Q("CloseButton") as Button;

        m_HelpToggle.RegisterValueChangedCallback(HelpToggleChanged);
        m_CloseButton.RegisterCallback<ClickEvent>(CloseHelpBox);
        
        m_HelpToggle.value = m_HelpBox.visible = IsVisible;
    }

    void OnDisable()
    {
        m_CloseButton.UnregisterCallback<ClickEvent>(CloseHelpBox);
        m_HelpToggle.UnregisterValueChangedCallback(HelpToggleChanged);
    }
    
    void OnValidate() => m_HelpToggle.value = m_HelpBox.visible = IsVisible;

    void HelpToggleChanged(ChangeEvent<bool> evt) => m_HelpBox.visible = IsVisible = evt.newValue;

    void CloseHelpBox(ClickEvent evt) => m_HelpToggle.value = false;
}
