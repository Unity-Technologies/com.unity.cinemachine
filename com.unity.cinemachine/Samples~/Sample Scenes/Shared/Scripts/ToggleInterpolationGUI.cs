using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cinemachine.Examples
{
    /// <summary>
    /// Displays buttons in the game view to toggle interpplation state of rigidbodies
    /// </summary>
    public class ToggleInterpolationGUI : MonoBehaviour
    {
        [Tooltip("Screen position of the button in normalized screen coords")]
        public Vector2 Position = new Vector3(0.8f, 0.15f);

        [Serializable]
        public struct Item
        {
            public string Text;
            public Rigidbody Rb;
        }
        public List<Item> Items = new();

        Vector2 m_Size = Vector2.zero;

        private void OnGUI()
        {
            // Establish button size (only once)
            if (m_Size == Vector2.zero)
            {
                for (int i = 0; i < Items.Count; ++i)
                {
                    var s = GUI.skin.box.CalcSize(new GUIContent(Items[i].Text));
                    m_Size.x = Mathf.Max(m_Size.x, s.x + s.y);
                    m_Size.y = Mathf.Max(m_Size.y, s.y);
                }
            }

            // Draw buttons
            const float vSpace = 3.0f;
            var pos = Position * new Vector2(Screen.width, Screen.height);
            for (int i = 0; i < Items.Count; ++i)
            {
                var rb = Items[i].Rb;
                if (rb != null)
                {
                    var r = new Rect(pos, m_Size);
                    GUI.Label(r, "", GUI.skin.box); // background
                    var oldValue = rb.interpolation == RigidbodyInterpolation.Interpolate;
                    var newValue = GUI.Toggle(r, oldValue, Items[i].Text);
                    if (oldValue != newValue)
                        rb.interpolation = newValue ? RigidbodyInterpolation.Interpolate : RigidbodyInterpolation.None;
                    pos.y += m_Size.y + vSpace;
                }
            }
        }
    }
}