using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine.UIElements;

namespace Unity.Cinemachine
{
    /// <summary>Manages onscreen positions for Cinemachine debugging output</summary>
    class CinemachineDebug
    {
        static List<StringBuilder> m_AvailableStringBuilders;

#if CINEMACHINE_UNITY_IMGUI
        /// <summary>Reserve an on-screen rectangle for debugging output.</summary>
        /// <param name="client">The client caller.  This is used as a handle.</param>
        /// <param name="text">Sample text, for determining rectangle size</param>
        /// <param name="style">What style will be used to draw, used here for
        /// determining rect size</param>
        /// <returns>An area on the game screen large enough to print the text
        /// in the style indicated</returns>
        public static Rect GetScreenPos(Camera camera, string text, GUIStyle style)
        {
            var size = style.CalcSize(new GUIContent(text));
            var pos = Vector2.zero;
            if (camera != null)
            {
                var r = camera.rect; 
                pos.x += r.x * Screen.width;
                pos.y += (1 - r.y - r.height) * Screen.height;
            }
            return new Rect(pos, size);
        }
#endif
        public static Rect GetCameraRect(Camera outputCamera, LensSettings lens)
        {
            Rect cameraRect = outputCamera.pixelRect;
            float screenHeight = cameraRect.height;
            float screenWidth = cameraRect.width;

            float screenAspect = screenWidth / screenHeight;
            switch (outputCamera.gateFit)
            {
                case Camera.GateFitMode.Vertical:
                    screenWidth = screenHeight * lens.Aspect;
                    cameraRect.position += new Vector2((cameraRect.width - screenWidth) * 0.5f, 0);
                    break;
                case Camera.GateFitMode.Horizontal:
                    screenHeight = screenWidth / lens.Aspect;
                    cameraRect.position += new Vector2(0, (cameraRect.height - screenHeight) * 0.5f);
                    break;
                case Camera.GateFitMode.Overscan:
                    if (screenAspect < lens.Aspect)
                    {
                        screenHeight = screenWidth / lens.Aspect;
                        cameraRect.position += new Vector2(0, (cameraRect.height - screenHeight) * 0.5f);
                    }
                    else
                    {
                        screenWidth = screenHeight * lens.Aspect;
                        cameraRect.position += new Vector2((cameraRect.width - screenWidth) * 0.5f, 0);
                    }
                    break;
                case Camera.GateFitMode.Fill:
                    if (screenAspect > lens.Aspect)
                    {
                        screenHeight = screenWidth / lens.Aspect;
                        cameraRect.position += new Vector2(0, (cameraRect.height - screenHeight) * 0.5f);
                    }
                    else
                    {
                        screenWidth = screenHeight * lens.Aspect;
                        cameraRect.position += new Vector2((cameraRect.width - screenWidth) * 0.5f, 0);
                    }
                    break;
                case Camera.GateFitMode.None:
                    break;
            }

            cameraRect = new Rect(cameraRect.position, new Vector2(screenWidth, screenHeight));

            // Invert Y
            float h = cameraRect.height;
            cameraRect.yMax = Screen.height - cameraRect.yMin;
            cameraRect.yMin = cameraRect.yMax - h;

            // Shift the guides along with the lens
            if (lens.IsPhysicalCamera)
                cameraRect.position += new Vector2(
                    -screenWidth * lens.PhysicalProperties.LensShift.x, screenHeight * lens.PhysicalProperties.LensShift.y);

            return cameraRect;
        }
        
        public static VisualElement[] CreateRuntimeUIContainers()
        {
            var assembly = typeof(EditorWindow).Assembly;
            var type = assembly.GetType("UnityEditor.GameView");
            var gameViews = Resources.FindObjectsOfTypeAll(type);

            var runtimeUIContainers = new VisualElement[gameViews.Length];
            for (int i = 0; i < gameViews.Length; ++i)
            {
                var gameViewRoot = (gameViews[0] as EditorWindow).rootVisualElement;
                runtimeUIContainers[i] = new VisualElement { name = "CinemachineRuntimeUIContainer" };
                gameViewRoot.Add(runtimeUIContainers[i]);
            }

            return runtimeUIContainers;
        }

        public static void PositionWithinCameraView(VisualElement visualElement, Camera camera, LensSettings lens)
        {
            // source:
            // - https://github.cds.internal.unity3d.com/unity/unity/blob/e3b4b5cd738c7d63c68471c6f80930455695de16/Editor/Mono/GameView/GameView.cs#L203
            // - https://github.cds.internal.unity3d.com/unity/unity/blob/661772bfdb9f6e0d13c5a0a14524195b0df8920e/Editor/Mono/EditorGUI.cs#L126
            const float gameViewToolbarHeight = 21f; // could also get it with reflection from EditorGUI
            
            var cameraRect = GetCameraRect(camera, lens);
            var top = cameraRect.y + gameViewToolbarHeight;
            var left = cameraRect.x;
            visualElement.style.position = new StyleEnum<Position>(Position.Absolute);
            visualElement.style.top = new Length(top, LengthUnit.Pixel);
            visualElement.style.left = new Length(left, LengthUnit.Pixel);
        }

        /// <summary>
        /// Delegate for OnGUI debugging.  
        /// This will be called by the CinemachineBrain in its OnGUI (editor only)
        /// </summary>
        public delegate void OnGUIDelegate(CinemachineBrain brain);

        /// <summary>
        /// Delegate for OnGUI debugging.  
        /// This will be called by the CinemachineBrain in its OnGUI (editor only)
        /// </summary>
        public static OnGUIDelegate OnGUIHandlers;

        /// <summary>Get a pre-allocated StringBuilder from the pool</summary>
        /// <returns>The pre-allocated StringBuilder from the pool.  
        /// Client must call ReturnToPool when done</returns>
        public static StringBuilder SBFromPool()
        {
            if (m_AvailableStringBuilders == null || m_AvailableStringBuilders.Count == 0)
                return new StringBuilder();
            var sb = m_AvailableStringBuilders[m_AvailableStringBuilders.Count - 1];
            m_AvailableStringBuilders.RemoveAt(m_AvailableStringBuilders.Count - 1);
            sb.Length = 0;
            return sb;
        }

        /// <summary>Return a StringBuilder to the pre-allocated pool</summary>
        /// <param name="sb">The string builder object to return to the pool</param>
        public static void ReturnToPool(StringBuilder sb)
        {
            if (m_AvailableStringBuilders == null)
                m_AvailableStringBuilders = new List<StringBuilder>();
            m_AvailableStringBuilders.Add(sb);
        }
    }
}
