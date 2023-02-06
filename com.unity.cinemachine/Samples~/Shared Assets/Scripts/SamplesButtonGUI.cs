using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Cinemachine.Examples
{
    public class SamplesButtonGUI : MonoBehaviour
    {
        [Tooltip("Screen position of the button in normalized screen coords")]
        public Vector2 Position = new Vector3(0.01f, 0.01f);

        [Tooltip("Text displayed on button")]
        public string Text = "";

        public UnityEvent OnClick = new UnityEvent();

        Vector2 m_Size = Vector2.zero;

        void OnGUI()
        {
            // Establish button size (only once)
            if (m_Size == Vector2.zero)
            {
                var s = GUI.skin.box.CalcSize(new GUIContent(Text));
                m_Size.x = Mathf.Max(m_Size.x, s.x + s.y);
                m_Size.y = Mathf.Max(m_Size.y, s.y);
            }

            // Draw buttons
            const float vSpace = 3.0f;
            var pos = Position * new Vector2(Screen.width, Screen.height);

            if (GUI.Button(new Rect(pos, m_Size), Text))
                OnClick.Invoke();
            pos.y += m_Size.y + vSpace;
        }
    }
}
