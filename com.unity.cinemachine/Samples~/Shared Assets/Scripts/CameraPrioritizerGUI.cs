using System.Collections.Generic;
using UnityEngine;

namespace Unity.Cinemachine.Samples
{
    /// <summary>
    /// Displayes a simple GUI in the game view for selecting the current camera.
    /// </summary>
    [SaveDuringPlay]
    public class CameraPrioritizerGUI : MonoBehaviour
    {
        [Tooltip("Screen position in normalized screen coords")]
        public Vector2 Position = new Vector3(0.05f, 0.05f);

        public List<CinemachineVirtualCameraBase> Cameras = new();

        Vector2 m_Size = Vector2.zero;
        Color m_LiveColor;

        void OnGUI()
        {
            var numNodes = Cameras?.Count;
            if (numNodes == 0)
                return;

            // Establish button size and color (only once)
            if (m_Size == Vector2.zero)
            {
                m_LiveColor = Color.Lerp(Color.red, Color.yellow, 0.8f);
                for (int i = 0; i < Cameras.Count; ++i)
                {
                    if (Cameras[i] == null)
                        continue;
                    var size = GUI.skin.button.CalcSize(new GUIContent(Cameras[i].Name));
                    m_Size.x = Mathf.Max(m_Size.x, size.x);
                    m_Size.y = Mathf.Max(m_Size.y, size.y);
                }
            }

            // Draw the selection buttons
            var pos = Position * new Vector2(Screen.width, Screen.height);
            const float vSpace = 3.0f;
            var baseColor = GUI.color;
            for (int i = 0; i < numNodes; ++i)
            {
                var vcam = Cameras[i];
                if (vcam != null)
                {
                    GUI.color = CinemachineCore.IsLive(vcam) ? m_LiveColor : baseColor;
                    if (GUI.Button(new Rect(pos, m_Size), vcam.Name))
                        vcam.Prioritize();
                    pos.y += m_Size.y + vSpace;
                }
            }
            GUI.color = baseColor;
        }
    }
}
