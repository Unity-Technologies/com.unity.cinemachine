using UnityEngine;

namespace Cinemachine.Examples
{
    public class ExampleHelpWindow : MonoBehaviour
    {
        public string title;
        [TextArea(minLines: 10, maxLines: 50)]
        public string description;

        bool m_MShowingHelpWindow = true;
        const float k_Padding = 40f;

        void OnGUI()
        {
            if (m_MShowingHelpWindow)
            {
                Vector2 size = GUI.skin.label.CalcSize(new GUIContent(description));
                var halfSize = size * 0.5f;

                float maxWidth = Mathf.Min(Screen.width - k_Padding, size.x);
                float left = Screen.width * 0.5f - maxWidth * 0.5f;
                float top = Screen.height * 0.4f - halfSize.y;

                var windowRect = new Rect(left, top, maxWidth, size.y);
                GUILayout.Window(400, windowRect, _ => DrawWindow(), title);
            }
        }

        void DrawWindow()
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(description);
            GUILayout.EndVertical();
            if (GUILayout.Button("Got it!"))
            {
                m_MShowingHelpWindow = false;
            }
        }
    }
}