using System;
using UnityEngine;
using System.Collections.Generic;
using System.Text;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.UIElements;
#endif

namespace Unity.Cinemachine
{
    /// <summary>Manages onscreen positions for Cinemachine debugging output</summary>
    static class CinemachineDebug
    {
        static List<StringBuilder> s_AvailableStringBuilders;

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

        
#if UNITY_EDITOR
        const string k_DebugUIName = "CinemachineDebugUI";
        static Dictionary<Camera, VisualElement> s_CameraViewRectContainers = new();
        static GameObject s_UIDocumentHolder;
        static UIDocument s_UIDocument;
        public static VisualElement CreateRuntimeUIContainer(Camera outputCamera)
        {
            if (s_UIDocumentHolder == null)
            {
                s_UIDocumentHolder = GameObject.Find(k_DebugUIName);
                if (s_UIDocumentHolder == null)
                {
                    s_UIDocumentHolder = new GameObject(k_DebugUIName)
                    {
                        hideFlags = HideFlags.NotEditable | HideFlags.HideInHierarchy,
                        tag = "EditorOnly" // TODO: we may want runtime too
                    };
                }
            }
            if (!s_UIDocumentHolder.TryGetComponent(out s_UIDocument))
            {
                s_UIDocument = s_UIDocumentHolder.AddComponent<UIDocument>();
                
                const string path = "Packages/com.unity.cinemachine/Runtime/Debug/";
                s_UIDocument.panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(path + "CinemachinePanelSettings.asset"); // TODO: find a runtime compatible way
            }

            if (s_CameraViewRectContainers.ContainsKey(outputCamera))
                return s_CameraViewRectContainers[outputCamera];

            var viewportContainer = new VisualElement
            {
                name = "CinemachineDebugUI_ViewportContainer",
                style =
                {
                    position = new StyleEnum<Position>(Position.Absolute),
                    flexWrap = new StyleEnum<Wrap>(Wrap.Wrap),
                }
            };
            // need to use delayCall, because rootVisualElement may not be built at this point yet
            EditorApplication.delayCall += () => s_UIDocument.rootVisualElement.Add(viewportContainer);
            var brain = outputCamera.GetComponent<CinemachineBrain>();
            viewportContainer.schedule.Execute(() => PositionWithinCameraView(viewportContainer, outputCamera, brain)).Every(0); // TODO: can we do better?
            s_CameraViewRectContainers.Add(outputCamera, viewportContainer);
            
            return viewportContainer;
            
            static void PositionWithinCameraView(VisualElement visualElement, Camera camera, CinemachineBrain brain)
            {
                if (s_UIDocument == null || s_UIDocument.rootVisualElement == null || 
                    camera == null || brain == null || !brain.ShowDebugText)
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
        }
#endif

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
            if (s_AvailableStringBuilders == null || s_AvailableStringBuilders.Count == 0)
                return new StringBuilder();
            var sb = s_AvailableStringBuilders[s_AvailableStringBuilders.Count - 1];
            s_AvailableStringBuilders.RemoveAt(s_AvailableStringBuilders.Count - 1);
            sb.Length = 0;
            return sb;
        }

        /// <summary>Return a StringBuilder to the pre-allocated pool</summary>
        /// <param name="sb">The string builder object to return to the pool</param>
        public static void ReturnToPool(StringBuilder sb)
        {
            if (s_AvailableStringBuilders == null)
                s_AvailableStringBuilders = new List<StringBuilder>();
            s_AvailableStringBuilders.Add(sb);
        }
    }
}
