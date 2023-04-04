using System;
using UnityEngine;
using System.Collections.Generic;
using System.Text;
#if UNITY_EDITOR && CINEMACHINE_UIELEMENTS
using UnityEditor;
using UnityEngine.UIElements;
#endif

namespace Unity.Cinemachine
{
    /// <summary>Manages onscreen positions for Cinemachine debugging output</summary>
    static class CinemachineDebug
    {
        static List<StringBuilder> s_AvailableStringBuilders;
        
#if UNITY_EDITOR && CINEMACHINE_UIELEMENTS
        const string k_DebugUIName = "CinemachineDebugUI";
        static Dictionary<Camera, VisualElement> s_CameraViewRectContainers = new();
        static GameObject s_UIDocumentHolder;
        static GameObject GetUIDocumentHolder()
        {
            if (s_UIDocumentHolder == null)
            {
                s_UIDocumentHolder = GameObject.Find(k_DebugUIName);
                if (s_UIDocumentHolder == null)
                {
                    s_UIDocumentHolder = new GameObject(k_DebugUIName)
                    {
                        hideFlags = HideFlags.HideAndDontSave,
                        tag = "EditorOnly"
                    };
                }
            }
            return s_UIDocumentHolder;
        }
        static UIDocument s_UIDocument;
        public static VisualElement CreateRuntimeUIContainer(Camera outputCamera)
        {
            var uiDocumentHolder = GetUIDocumentHolder();
            if (!uiDocumentHolder.TryGetComponent(out s_UIDocument))
            {
                s_UIDocument = uiDocumentHolder.AddComponent<UIDocument>();
                
                const string path = "Packages/com.unity.cinemachine/Runtime/Debug/";
                s_UIDocument.panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(path + "CinemachinePanelSettings.asset");
            }

            if (s_CameraViewRectContainers.ContainsKey(outputCamera))
            {
                if (s_CameraViewRectContainers[outputCamera].childCount != 0)
                    return s_CameraViewRectContainers[outputCamera];
                
                s_CameraViewRectContainers[outputCamera].RemoveFromHierarchy();
                s_CameraViewRectContainers.Remove(outputCamera);
            }

            var viewportContainer = new VisualElement
            {
                name = "CinemachineDebugUI_ViewportContainer",
                style =
                {
                    position = new StyleEnum<Position>(Position.Absolute),
                    flexWrap = new StyleEnum<Wrap>(Wrap.Wrap),
                }
            };
            SetRoot(viewportContainer);
            viewportContainer.schedule.Execute(() => PositionWithinCameraView(viewportContainer, outputCamera)).Every(0);
            s_CameraViewRectContainers.Add(outputCamera, viewportContainer);
            
            return viewportContainer;
            
            // local functions
            static void SetRoot(VisualElement viewportContainer)
            {
                // Need to wait until rootVisualElement is created.
                if (s_UIDocument.rootVisualElement == null)
                {
                    EditorApplication.delayCall += () => SetRoot(viewportContainer);
                    return;
                }
                s_UIDocument.rootVisualElement.Add(viewportContainer);
            }
            static void PositionWithinCameraView(VisualElement visualElement, Camera camera)
            {
                if (s_UIDocument == null || s_UIDocument.rootVisualElement == null || camera == null)
                    return;
                if (visualElement.childCount == 0)
                {
                    visualElement.RemoveFromHierarchy();
                    s_CameraViewRectContainers.Remove(camera);
                }

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
