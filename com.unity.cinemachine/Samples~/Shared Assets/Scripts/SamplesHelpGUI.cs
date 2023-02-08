using UnityEngine;
using UnityEngine.Events;
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

        [Tooltip("Text is displayed when this is checked")]
        public bool Displayed = false;

        [TextArea(minLines: 10, maxLines: 50)]
        public string HelpText;

        [Tooltip("Event sent when the help window is dismissed")]
        public UnityEvent OnHelpDismissed = new UnityEvent();

        Vector2 m_Size = Vector2.zero;

        private void OnGUI()
        {
            // Establish button size (only once)
            if (m_Size == Vector2.zero)
                m_Size = GUI.skin.button.CalcSize(new GUIContent(ButtonText));

            if (Displayed)
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
                    {
                        Displayed = false;
                        OnHelpDismissed.Invoke();
                    }
                }, title);
            }
            else
            {
                var pos = Position * new Vector2(Screen.width, Screen.height);
                if (GUI.Button(new Rect(pos, m_Size), ButtonText))
                    Displayed = true;
            }
        }
    }
}