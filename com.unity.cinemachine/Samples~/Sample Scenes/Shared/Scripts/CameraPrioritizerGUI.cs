using System.Collections.Generic;
using UnityEngine;

namespace Cinemachine.Examples
{
    /// <summary>
    /// Displayes a simple GUI in the game view for selecting the current camera.
    /// </summary>
    [SaveDuringPlay]
    public class CameraPrioritizerGUI : MonoBehaviour
    {
        public Vector2 Position = new Vector3(30, 10);
        public float Width = 100;
        public int Priority = 100;

        public List<CinemachineVirtualCameraBase> Cameras = new();

        public void OnGUI()
        {
            var numNodes = Cameras?.Count;
            if (numNodes == 0)
                return;
            var style = GUI.skin.button;
            var pos = Position;
            var size = style.CalcSize(new GUIContent("Button")); size.x = Width;
            const float vSpace = 3.0f;
            var baseColor = GUI.color;
            var liveColor = Color.Lerp(Color.red, Color.yellow, 0.8f);
            for (int i = 0; i < numNodes; ++i)
            {
                var vcam = Cameras[i];
                if (vcam != null)
                {
                    GUI.color = CinemachineCore.Instance.IsLive(vcam) ? liveColor : baseColor;
                    if (GUI.Button(new Rect(pos, size), vcam.Name))
                    {
                        vcam.Priority = Priority;
                        vcam.Prioritize();
                    }
                    pos.y += size.y + vSpace;
                }
            }
            GUI.color = baseColor;
        }
    }
}
