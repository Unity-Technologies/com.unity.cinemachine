using UnityEngine;
using UnityEngine.SceneManagement;

namespace Cinemachine.Examples
{
    /// <summary>
    /// Displays a button in the game view that will bring up a window with text.
    /// </summary>
    public class SamplesHelpGUI : MonoBehaviour
    {
        [Tooltip("The text to display on the button")]
        public string ButtonText = "Help";

        [Tooltip("Screen position of the button in normalized screen coords")]
        public Vector2 Position = new Vector3(0.9f, 0.05f);

        [TextArea(minLines: 10, maxLines: 50)]
        public string HelpText;

        bool m_ToggleState = false;
        Vector2 m_Size = Vector2.zero;

        private void OnGUI()
        {
            // Establish button size (only once)
            if (m_Size == Vector2.zero)
                m_Size = GUI.skin.button.CalcSize(new GUIContent(ButtonText));

            if (m_ToggleState)
            {
                var size = GUI.skin.label.CalcSize(new GUIContent(HelpText));
                var halfSize = size * 0.5f;

                float maxWidth = Mathf.Min(Screen.width / 2, size.x);
                float left = Screen.width * 0.5f - maxWidth * 0.5f;
                float top = Screen.height * 0.4f - halfSize.y;

                var windowRect = new Rect(left, top, maxWidth, size.y);
                var title = SceneManager.GetActiveScene().name;
                GUILayout.Window(400, windowRect, (id) => 
                {
                    GUILayout.BeginVertical(GUI.skin.box);
                    GUILayout.Label(HelpText);
                    GUILayout.EndVertical();
                    if (GUILayout.Button("Got it!"))
                        m_ToggleState = false;
                }, title);
            }
            else
            {
                var pos = Position * new Vector2(Screen.width, Screen.height);
                if (GUI.Button(new Rect(pos, m_Size), ButtonText))
                    m_ToggleState = true;
            }
        }
    }
}