using UnityEngine;
using UnityEngine.SceneManagement;

namespace Cinemachine.Examples
{
    public class SamplesHelpText : MonoBehaviour
    {
        public string ButtonText = "Help";
        public Vector2 Position = new Vector3(30, 30);

        [TextArea(minLines: 10, maxLines: 50)]
        public string HelpText;

        bool m_ToggleState = false;

        private void OnGUI()
        {
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
                var size = GUI.skin.button.CalcSize(new GUIContent(ButtonText));
                var pos = new Vector2(Screen.width - Position.x - size.x, Position.y);
                if (GUI.Button(new Rect(pos, size), ButtonText))
                    m_ToggleState = true;
            }
        }
    }
}