using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Cinemachine.Utility;

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
        /// Add help box for CinemachineComponentBase or CinemachineExtension editors, 
        /// prompting to solve a missing CinemachineCamera component or a missing tracking target
        /// </summary>
        public void AddMissingCmCameraHelpBox(
            VisualElement ux, RequiredTargets requiredTargets = RequiredTargets.None)
        {
            EditorApplication.update -= UpdateState;
            EditorApplication.update += UpdateState;
            var targets = Targets;
            m_NoCameraHelp = ux.AddChild(InspectorUtility.CreateHelpBoxWithButton(
                "This component is intended to be used only with a CinemachineCamera.", HelpBoxMessageType.Warning,
                "Add\nCinemachineCamera", () => AddCmCameraToTargets(targets)));

            m_RequiredTargets = requiredTargets;
            string text = string.Empty;
            switch (requiredTargets)
            {
                case RequiredTargets.Follow: text = "A Tracking Target is required in the CinemachineCamera."; break;
                case RequiredTargets.LookAt: text = "A LookAt Tracking Target is required in the CinemachineCamera."; break;
                case RequiredTargets.FollowGroup: text = "Tracking Target in the CinemachineCamera must be a Target Group."; break;
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
                        Undo.AddComponent<CinemachineCamera>(t.gameObject);
                }
                else
                {
                    var x = targets[i] as CinemachineExtension;
                    if (x != null && x.ComponentOwner == null)
                        Undo.AddComponent<CinemachineCamera>(x.gameObject).AddExtension(x);
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
                    noCamera |= x.ComponentOwner == null;
                    switch (m_RequiredTargets)
                    {
                        case RequiredTargets.Follow: noTarget |= noCamera || x.ComponentOwner.Follow == null; break;
                        case RequiredTargets.LookAt: noTarget |= noCamera || x.ComponentOwner.LookAt == null; break;
                        case RequiredTargets.FollowGroup: noTarget |= noCamera || x.ComponentOwner.FollowTargetAsGroup == null; break;
                        default: break;
                    }
                    noTarget = noCamera || x.ComponentOwner.Follow == null;
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
                    noCamera |= x.ComponentOwner == null;
                    switch (requiredTargets)
                    {
                        case RequiredTargets.Follow: noTarget |= noCamera || x.ComponentOwner.Follow == null; break;
                        case RequiredTargets.LookAt: noTarget |= noCamera || x.ComponentOwner.LookAt == null; break;
                        case RequiredTargets.FollowGroup: noTarget |= noCamera || x.ComponentOwner.FollowTargetAsGroup == null; break;
                        default: break;
                    }
                    noTarget = noCamera || x.ComponentOwner.Follow == null;
                }
            }
            if (noCamera)
            {
                InspectorUtility.HelpBoxWithButton(
                    "This component is intended to be used only with a CinemachineCamera.", UnityEditor.MessageType.Warning,
                    new GUIContent("Add\nCinemachineCamera"), () => AddCmCameraToTargets(targets));
                EditorGUILayout.Space();
            }
            else if (noTarget)
            {
                string text = string.Empty;
                switch (requiredTargets)
                {
                    case RequiredTargets.Follow: text = "A Tracking Target is required in the CinemachineCamera."; break;
                    case RequiredTargets.LookAt: text = "A LookAt Tracking Target is required in the CinemachineCamera."; break;
                    case RequiredTargets.FollowGroup: text = "Tracking Target in the CinemachineCamera must be a Target Group."; break;
                    default: break;
                }
                if (text.Length > 0)
                    EditorGUILayout.HelpBox(text, UnityEditor.MessageType.Warning);
                EditorGUILayout.Space();
            }
        }

        public static void OnGUI_DrawOnscreenTargetMarker(
            ICinemachineTargetGroup group, Vector3 worldPoint, 
            Quaternion vcamRotation, Camera camera)
        {
            var c = camera.WorldToScreenPoint(worldPoint);
            c.y = Screen.height - c.y;
            if (c.z < 0)
                return;

            var oldColor = GUI.color;
            float radius = 0;
            if (group != null)
            {
                var p2 = camera.WorldToScreenPoint(
                    worldPoint + vcamRotation * new Vector3(group.Sphere.radius, 0, 0));
                radius = Mathf.Abs(p2.x - c.x);
            }
            var r = new Rect(c, Vector2.zero).Inflated(Vector2.one * CinemachineComposerPrefs.TargetSize.Value);
            GUI.color = new Color(0, 0, 0, CinemachineComposerPrefs.OverlayOpacity.Value);
            GUI.DrawTexture(r.Inflated(new Vector2(1, 1)), Texture2D.whiteTexture, ScaleMode.StretchToFill);
            var color = CinemachineComposerPrefs.TargetColour.Value;
            GUI.color = color;
            GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill);
            if (radius > CinemachineComposerPrefs.TargetSize.Value)
            {
                color.a = Mathf.Lerp(1f, CinemachineComposerPrefs.OverlayOpacity.Value, (radius - 10f) / 50f);
                GUI.color = color;
                GUI.DrawTexture(r.Inflated(new Vector2(radius, radius)),
                    GetTargetMarkerTex(), ScaleMode.StretchToFill);
            }
            GUI.color = oldColor;
        }

        static Texture2D s_TargetMarkerTex = null;
        static Texture2D GetTargetMarkerTex()
        {
            if (s_TargetMarkerTex == null)
            {
                // Create a texture from scratch!
                // Oh gawd there has to be a nicer way to do this
                const int size = 128;
                const float th = 1f;
                var pix = new Color32[size * size];
                float radius = size / 2 - th;
                var center = new Vector2(size-1, size-1) / 2;
                for (int y = 0; y < size; ++y)
                {
                    for (int x = 0; x < size; ++x)
                    {
                        float d = Vector2.Distance(new Vector2(x, y), center);
                        d = Mathf.Abs((d - radius) / th);
                        var a = Mathf.Clamp01(1 - d);
                        pix[y * size + x] = new Color(1, 1, 1, a);
                    }
                }
                s_TargetMarkerTex = new Texture2D(size, size);
                s_TargetMarkerTex.SetPixels32(pix);
                s_TargetMarkerTex.Apply();
            }
            return s_TargetMarkerTex;
        }
    }
}
