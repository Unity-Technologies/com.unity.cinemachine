using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Unity.Cinemachine.Samples
{
    /// <summary>
    /// Runtime UI script that can create toggles and buttons.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
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

        VisualElement m_Root;
        List<VisualElement> m_DynamicElements;

        void OnEnable()
        {
            var uiDocument = GetComponent<UIDocument>();

            m_Root = uiDocument.rootVisualElement.Q("TogglesAndButtons");
            m_DynamicElements = new List<VisualElement>(Buttons.Count);
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
                    m_Root.Add(toggle);
                    m_DynamicElements.Add(toggle);
                }
                else
                {
                    var button = new Button
                    {
                        text = item.Name,
                        focusable = false,
                        style =
                        {
                            backgroundColor = new StyleColor(new Color(196, 196, 196, 255)),
                            alignSelf = new StyleEnum<Align>(Align.FlexStart),
                            unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold),
                        }
                    };
                    button.clickable.clicked += item.OnClick.Invoke;
                    m_Root.Add(button);
                    m_DynamicElements.Add(button);
                }

            m_Root.visible = Buttons.Count > 0;
        }

        void OnDisable()
        {
            foreach (var element in m_DynamicElements)
                m_Root.Remove(element);
            m_DynamicElements.Clear();
        }
    }
}
