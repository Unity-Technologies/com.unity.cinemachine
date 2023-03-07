using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

public class SamplesDynamicUI : MonoBehaviour
{
    [Serializable]
    public class Item
    {
        [Tooltip("Text displayed on button")]
        public string Name = "Text";

        [Tooltip("Set this to true to create a toggle button")]
        public bool IsToggle;

        [Tooltip("Value of the toggle")]
        public bool ToggleValue;

        [Tooltip("Event sent when button is clicked")]
        public UnityEvent OnClick = new UnityEvent();

        [Tooltip("Event sent when toggle button is clicked")]
        public UnityEvent<bool> OnValueChanged = new UnityEvent<bool>();
    }
    
    [Tooltip("The buttons to be displayed")]
    public List<Item> Buttons = new ();

    Vector2 m_ButtonSize;
    List<Toggle> m_Toggles;
    List<EventCallback<ChangeEvent<bool>>> m_OnValueChangedCallbacks;

    void OnEnable()
    {
        var uiDocument = GetComponent<UIDocument>();
        var togglesAndButtons = uiDocument.rootVisualElement.Q("TogglesAndButtons");
        m_Toggles = new List<Toggle>();
        m_OnValueChangedCallbacks = new List<EventCallback<ChangeEvent<bool>>>();
        
        // var uiDocument = GetComponent<UIDocument>();
        //
        // m_DynamicUI = uiDocument.rootVisualElement.Q("DynamicUI");
        foreach (var item in Buttons)
        {
            

            if (item.IsToggle)
            {
                var toggle = new Toggle
                {
                    label = item.Name,
                    value = item.ToggleValue
                };

                void Callback(ChangeEvent<bool> _) => item.OnValueChanged.Invoke(item.ToggleValue);
                m_OnValueChangedCallbacks.Add(Callback);
                toggle.RegisterValueChangedCallback(Callback);
                togglesAndButtons.Add(toggle);
                m_Toggles.Add(toggle);
            }
            else
            {
                var button = new Button();
                button.text = item.Name;
                button.clickable.clicked += item.OnClick.Invoke;
            }
        }
    }

    void OnDisable()
    {
        
        // var uiDocument = GetComponent<UIDocument>();
        //
        // m_DynamicUI = uiDocument.rootVisualElement.Q("DynamicUI");
        foreach (var item in Buttons)
        {
            

            if (item.IsToggle)
            {
                var toggle = new Toggle();
                toggle.label = item.Name;
                toggle.value = item.ToggleValue;

                void Callback(ChangeEvent<bool> _) => item.OnValueChanged.Invoke(item.ToggleValue);
                m_OnValueChangedCallbacks.Add(Callback);
                toggle.RegisterValueChangedCallback(Callback);
            }
            else
            {
                var button = new Button();
                button.text = item.Name;
                button.clickable.clicked += item.OnClick.Invoke;
            }
        }
        m_OnValueChangedCallbacks.Clear();
    }
}
