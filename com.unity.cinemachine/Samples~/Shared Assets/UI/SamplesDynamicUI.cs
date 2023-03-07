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
        public UnityEvent OnClick = new();

        [Tooltip("Event sent when toggle button is clicked")]
        public UnityEvent<bool> OnValueChanged = new();
    }
    
    [Tooltip("The buttons to be displayed")]
    public List<Item> Buttons = new ();

    VisualElement m_TogglesAndButtons;
    List<KeyValuePair<Toggle, EventCallback<ChangeEvent<bool>>>> m_OnValueChangedCallbacks;
    List<KeyValuePair<Button, Action>> m_OnClickCallbacks;
    void OnEnable()
    {
        var uiDocument = GetComponent<UIDocument>();
        
        m_TogglesAndButtons = uiDocument.rootVisualElement.Q("TogglesAndButtons");
        m_OnValueChangedCallbacks = new ();
        m_OnClickCallbacks = new();
        
        foreach (var item in Buttons)
        {
            if (item.IsToggle)
            {
                var toggle = new Toggle
                {
                    label = item.Name,
                    value = item.ToggleValue
                };
                toggle.RegisterValueChangedCallback(Callback);
                m_TogglesAndButtons.Add(toggle);
                m_OnValueChangedCallbacks.Add(new(toggle, Callback));
                
                // local function
                void Callback(ChangeEvent<bool> e) => item.OnValueChanged.Invoke(e.newValue);
            }
            else
            {
                var button = new Button
                {
                    text = item.Name
                };
                button.clickable.clicked += Callback;
                
                m_OnClickCallbacks.Add(new (button, Callback));
                
                // local function
                void Callback() => item.OnClick.Invoke();
            }
        }
    }

    void OnDisable()
    {
        foreach (var callback in m_OnValueChangedCallbacks)
        {
            callback.Key.UnregisterValueChangedCallback(callback.Value);
            m_TogglesAndButtons.Remove(callback.Key);
        }

        m_OnValueChangedCallbacks.Clear();
        foreach (var callback in m_OnClickCallbacks)
        {
            callback.Key.clickable.clicked -= callback.Value;
            m_TogglesAndButtons.Remove(callback.Key);
        }

        m_OnClickCallbacks.Clear();
    }
}
