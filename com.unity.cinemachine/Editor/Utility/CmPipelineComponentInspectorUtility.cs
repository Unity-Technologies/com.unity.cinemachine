using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Cinemachine.Editor
{
    /// <summary>
    /// Helpers for drawing CmComponentBase or CmExtension inspectors.
    /// GML todo: make a common base class for CmComponentBase and CmExtension.
    /// </summary>
    class CmPipelineComponentInspectorUtility
    {
        UnityEditor.Editor m_Editor;

        UnityEngine.Object[] Targets => m_Editor.targets;

        VisualElement m_NoCameraHelp;
        VisualElement m_NoTargetHelp;
        RequiredTargets m_RequiredTargets;

        public CmPipelineComponentInspectorUtility(UnityEditor.Editor editor) { m_Editor = editor; }

        /// <summary>Call from Inspector's OnDisable</summary>
        public void OnDisable()
        {
            EditorApplication.update -= UpdateState;
        }

        public enum RequiredTargets { None, Follow, LookAt, FollowGroup };

        /// <summary>
        /// Add helpbox for CinemachineComponentBase or CinemachineExtension editors, 
        /// prompting to solve a missing CmCamera component or a missing tracking target
        /// </summary>
        public void AddMissingCmCameraHelpBox(
            VisualElement ux, RequiredTargets requiredTargets = RequiredTargets.None)
        {
            EditorApplication.update -= UpdateState;
            EditorApplication.update += UpdateState;
            var targets = Targets;
            m_NoCameraHelp = ux.AddChild(InspectorUtility.CreateHelpBoxWithButton(
                "This component is intended to be used only with a CmCamera.", HelpBoxMessageType.Warning,
                "Add\nCmCamera", () => AddCmCameraToTargets(targets)));

            m_RequiredTargets = requiredTargets;
            string text = string.Empty;
            switch (requiredTargets)
            {
                case RequiredTargets.Follow: text = "A Tracking Target is required in the CmCamera."; break;
                case RequiredTargets.LookAt: text = "A LookAt Tracking Target is required in the CmCamera."; break;
                case RequiredTargets.FollowGroup: text = "Tracking Target in the CmCamera must be a Target Group."; break;
                default: break;
            }
            if (text.Length > 0)
                m_NoTargetHelp = ux.AddChild(new HelpBox(text, HelpBoxMessageType.Warning));
        }

        static void AddCmCameraToTargets(UnityEngine.Object[] targets)
        {
            for (int i = 0; i < targets.Length; ++i)
            {
                var t = targets[i] as CinemachineComponentBase;
                if (t != null)
                {
                    if (t.VirtualCamera == null)
                        Undo.AddComponent<CmCamera>(t.gameObject);
                }
                else
                {
                    var x = targets[i] as CinemachineExtension;
                    if (x != null && x.VirtualCamera == null)
                        Undo.AddComponent<CmCamera>(x.gameObject).AddExtension(x);
                }
            }
        }

        public void UpdateState()
        {
            if (m_Editor == null || m_Editor.target == null)
                return;  // target was deleted
            bool noCamera = false;
            bool noTarget = false;
            var targets = Targets;
            for (int i = 0; i < targets.Length && !noCamera; ++i)
            {
                var t = targets[i] as CinemachineComponentBase;
                if (t != null)
                {
                    noCamera |= t.VirtualCamera == null || t.VirtualCamera is CinemachineCameraManagerBase;
                    switch (m_RequiredTargets)
                    {
                        case RequiredTargets.Follow: noTarget |= t.FollowTarget == null; break;
                        case RequiredTargets.LookAt: noTarget |= t.LookAtTarget == null; break;
                        case RequiredTargets.FollowGroup: noTarget |= t.FollowTargetAsGroup == null; break;
                        default: break;
                    }
                }
                else
                {
                    var x = targets[i] as CinemachineExtension;
                    noCamera |= x.VirtualCamera == null;
                    switch (m_RequiredTargets)
                    {
                        case RequiredTargets.Follow: noTarget |= noCamera || x.VirtualCamera.Follow == null; break;
                        case RequiredTargets.LookAt: noTarget |= noCamera || x.VirtualCamera.LookAt == null; break;
                        case RequiredTargets.FollowGroup: noTarget |= noCamera || x.VirtualCamera.FollowTargetAsGroup == null; break;
                        default: break;
                    }
                    noTarget = noCamera || x.VirtualCamera.Follow == null;
                }
            }
            if (m_NoCameraHelp != null)
                m_NoCameraHelp.SetVisible(noCamera);
            if (m_NoTargetHelp != null)
                m_NoTargetHelp.SetVisible(noTarget && !noCamera);
        }

        /// <summary>
        /// IMGUI support - to be removed when IMGUI is gone
        /// </summary>
        public static void IMGUI_DrawMissingCmCameraHelpBox(
            UnityEditor.Editor editor, RequiredTargets requiredTargets = RequiredTargets.None)
        {
            bool noCamera = false;
            bool noTarget = false;
            var targets = editor.targets;
            for (int i = 0; i < targets.Length && !noCamera; ++i)
            {
                var t = targets[i] as CinemachineComponentBase;
                if (t != null)
                {
                    noCamera |= t.VirtualCamera == null || t.VirtualCamera is CinemachineCameraManagerBase;
                    switch (requiredTargets)
                    {
                        case RequiredTargets.Follow: noTarget |= t.FollowTarget == null; break;
                        case RequiredTargets.LookAt: noTarget |= t.LookAtTarget == null; break;
                        case RequiredTargets.FollowGroup: noTarget |= t.FollowTargetAsGroup == null; break;
                        default: break;
                    }
                }
                else
                {
                    var x = targets[i] as CinemachineExtension;
                    noCamera |= x.VirtualCamera == null;
                    switch (requiredTargets)
                    {
                        case RequiredTargets.Follow: noTarget |= noCamera || x.VirtualCamera.Follow == null; break;
                        case RequiredTargets.LookAt: noTarget |= noCamera || x.VirtualCamera.LookAt == null; break;
                        case RequiredTargets.FollowGroup: noTarget |= noCamera || x.VirtualCamera.FollowTargetAsGroup == null; break;
                        default: break;
                    }
                    noTarget = noCamera || x.VirtualCamera.Follow == null;
                }
            }
            if (noCamera)
            {
                InspectorUtility.HelpBoxWithButton(
                    "This component is intended to be used only with a CmCamera.", UnityEditor.MessageType.Warning,
                    new GUIContent("Add\nCmCamera"), () => AddCmCameraToTargets(targets));
                EditorGUILayout.Space();
            }
            else if (noTarget)
            {
                string text = string.Empty;
                switch (requiredTargets)
                {
                    case RequiredTargets.Follow: text = "A Tracking Target is required in the CmCamera."; break;
                    case RequiredTargets.LookAt: text = "A LookAt Tracking Target is required in the CmCamera."; break;
                    case RequiredTargets.FollowGroup: text = "Tracking Target in the CmCamera must be a Target Group."; break;
                    default: break;
                }
                if (text.Length > 0)
                    EditorGUILayout.HelpBox(text, UnityEditor.MessageType.Warning);
                EditorGUILayout.Space();
            }
        }
    }
}
