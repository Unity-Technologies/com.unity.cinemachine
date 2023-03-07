using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Unity.Cinemachine.Samples
{
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
        public List<Item> Buttons = new();

        Color m_UnityLightGray = new Color(196, 196, 196, 255);
        VisualElement m_DynamicBox;
        List<VisualElement> m_DynamicELements;

        void OnEnable()
        {
            var uiDocument = GetComponent<UIDocument>();

            m_DynamicBox = uiDocument.rootVisualElement.Q("TogglesAndButtons");
            m_DynamicELements = new List<VisualElement>(Buttons.Count);
            foreach (var item in Buttons)
                if (item.IsToggle)
                {
                    var toggle = new Toggle
                    {
                        label = item.Name,
                        value = item.ToggleValue,
                        focusable = false,
                        style =
                        {
                            flexDirection = new StyleEnum<FlexDirection>(FlexDirection.RowReverse),
                            alignSelf = new StyleEnum<Align>(Align.FlexStart),
                            unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold),
                        }
                    };
                    toggle.RegisterValueChangedCallback(e => item.OnValueChanged.Invoke(e.newValue));
                    m_DynamicBox.Add(toggle);
                    m_DynamicELements.Add(toggle);
                }
                else
                {
                    var button = new Button
                    {
                        text = item.Name,
                        focusable = false,
                        style =
                        {
                            backgroundColor = new StyleColor(m_UnityLightGray),
                            alignSelf = new StyleEnum<Align>(Align.FlexStart),
                            unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold),
                        }
                    };
                    button.clickable.clicked += item.OnClick.Invoke;
                    m_DynamicBox.Add(button);
                    m_DynamicELements.Add(button);
                }

            m_DynamicBox.visible = Buttons.Count > 0;
        }

        void OnDisable()
        {
            foreach (var element in m_DynamicELements)
                m_DynamicBox.Remove(element);
            m_DynamicELements.Clear();
        }
    }
}
