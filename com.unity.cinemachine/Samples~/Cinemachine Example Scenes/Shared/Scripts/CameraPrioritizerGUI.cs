using System.Collections.Generic;
using UnityEngine;

namespace Cinemachine.Examples
{
    public partial class CameraPrioritizerGUI : MonoBehaviour
    {
        public Vector2 Position = new Vector3(30, 30);
        public int Priority = 100;
        public List<CmCamera> Cameras;

        void OnGUI()
        {
            var numNodes = Cameras?.Count;
            if (numNodes == 0)
                return;

            var size = new Vector2(10, 10);
            for (int i = 0; i < numNodes; ++i)
            {
                var s = GUI.skin.button.CalcSize(new GUIContent(Cameras[i].Name));
                size.x = Mathf.Max(size.x, s.x);
                size.y = Mathf.Max(size.y, s.y);
            }

            const float vSpace = 3.0f;
            var pos = Position;
            var baseColor = GUI.color;
            var liveColor = Color.Lerp(Color.red, Color.yellow, 0.8f);
            for (int i = 0; i < numNodes; ++i)
            {
                var cam = Cameras[i];
                if (cam != null)
                {
                    GUI.color = CinemachineCore.Instance.IsLive(cam) ? liveColor : baseColor;
                    if (GUI.Button(new Rect(pos, size), cam.Name))
                    {
                        cam.Priority = Priority;
                        cam.Prioritize();
#if UNITY_EDITOR
                        UnityEditor.Selection.activeGameObject = cam.gameObject;
#endif
                    }
                    pos.y += size.y + vSpace;
                }
            }
            GUI.color = baseColor;
        }
    }
}
