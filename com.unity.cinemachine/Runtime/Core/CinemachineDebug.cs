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

        const string k_DebugUIName = "CinemachineDebugUI";
        static GameObject s_UIDocumentHolder;
        static UIDocument s_UIDocument;
        public static VisualElement CreateRuntimeUIContainer()
        {
            if (s_UIDocumentHolder == null)
            {
                s_UIDocumentHolder = GameObject.Find(k_DebugUIName);
                if (s_UIDocumentHolder == null)
                {
                    s_UIDocumentHolder = new GameObject(k_DebugUIName)
                    {
                        hideFlags = HideFlags.NotEditable | HideFlags.HideInHierarchy,
                        tag = "EditorOnly"
                    };
                }
            }
            if (!s_UIDocumentHolder.TryGetComponent(out s_UIDocument))
            {
                s_UIDocument = s_UIDocumentHolder.AddComponent<UIDocument>();
                
                const string path = "Packages/com.unity.cinemachine/Runtime/UI/";
                s_UIDocument.panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(path + "CinemachinePanelSettings.asset");
            }
            
            var viewportContainer = new VisualElement
            {
                name = "CinemachineDebugUI_ViewportContainer",
                style = { position = new StyleEnum<Position>(Position.Absolute) }
            };
            s_UIDocument.rootVisualElement?.Add(viewportContainer);
            return viewportContainer;
        }

        /// <summary>Position the input visual element in the panel defined by s_ViewportContainer and the camera.</summary>
        /// <param name="visualElement">The visual element to position. It must be a child of s_ViewportContainer</param>
        /// <param name="camera">The camera that defines the view area.</param>
        public static void PositionWithinCameraView(VisualElement visualElement, Camera camera)
        {
            if (s_UIDocument == null || s_UIDocument.rootVisualElement == null)
                return;
            
            var panel = s_UIDocument.rootVisualElement.panel;
            var screenPanelSpace = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(Screen.width, Screen.height));
            var viewport = camera.pixelRect;
            var viewPortMaxPanelSpace = RuntimePanelUtils.ScreenToPanel(panel, viewport.max);
            var viewPortMinPanelSpace = RuntimePanelUtils.ScreenToPanel(panel, viewport.min);
            visualElement.style.top = new Length(screenPanelSpace.y - viewPortMaxPanelSpace.y, LengthUnit.Pixel);
            visualElement.style.bottom = new Length(viewPortMinPanelSpace.y, LengthUnit.Pixel);
            visualElement.style.left = new Length(viewPortMinPanelSpace.x, LengthUnit.Pixel);
            visualElement.style.right = new Length(screenPanelSpace.x - viewPortMaxPanelSpace.x, LengthUnit.Pixel);
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
