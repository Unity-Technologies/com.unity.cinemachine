using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Cinemachine.Examples
{   
    [SaveDuringPlay]
    public class SamplesGUI : MonoBehaviour
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

        [Tooltip("Screen position of the button group in normalized screen coords")]
        public Vector2 Position = new Vector3(0.01f, 0.04f);

        [Tooltip("The buttons to be displyed")]
        public List<Item> Buttons = new ();

        Vector2 m_ButtonSize;

        void OnGUI()
        {
            // Establish button size (only once)
            if (m_ButtonSize == Vector2.zero)
            {
                foreach (var item in Buttons)
                {
                    var s = GUI.skin.box.CalcSize(new GUIContent(item.Name));
                    m_ButtonSize.x = Mathf.Max(m_ButtonSize.x, s.x + s.y/2);
                    m_ButtonSize.y = s.y;
                }
            }

            // Draw buttons
            const float vSpace = 3.0f;
            var pos = Position * new Vector2(Screen.width, Screen.height);

            foreach (var item in Buttons)
            {
                var r = new Rect(pos, m_ButtonSize);
                if (item.IsToggle)
                {
                    GUI.Label(r, "", GUI.skin.box); // background
                    var prevToggleValue = item.ToggleValue;
                    item.ToggleValue = GUI.Toggle(r, item.ToggleValue, item.Name);
                    if (prevToggleValue != item.ToggleValue)
                        item.OnValueChanged.Invoke(item.ToggleValue);
                }
                else
                {
                    if (GUI.Button(r, item.Name))
                        item.OnClick.Invoke();
                }
                pos.y += m_ButtonSize.y + vSpace;
            }
        }
    }
}
