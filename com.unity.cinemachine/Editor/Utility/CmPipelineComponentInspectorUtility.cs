using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Cinemachine.Utility;

namespace Cinemachine.Editor
{
    /// <summary>
    /// Helpers for drawing CmComponentBase or CmExtension inspectors.
    /// </summary>
    static class CmPipelineComponentInspectorUtility
    {
        public enum RequiredTargets { None, Tracking, LookAt, Group };

        const string k_NeedTarget = "A Tracking Target is required in the CinemachineCamera.";
        const string k_NeedLookAt = "A LookAt Tracking Target is required in the CinemachineCamera.";
        const string k_NeedGroup = "The Tracking Target in the CinemachineCamera must be a Target Group.";
        const string k_NeedCamera = "This component is intended to be used only with a CinemachineCamera.";
        const string k_AddCamera = "Add\nCinemachineCamera";

        /// <summary>
        /// Add help box for CinemachineComponentBase or CinemachineExtension editors, 
        /// prompting to solve a missing CinemachineCamera component or a missing tracking target
        /// </summary>
        public static void AddMissingCmCameraHelpBox(
            this UnityEditor.Editor editor, VisualElement ux, RequiredTargets requiredTargets = RequiredTargets.None)
        {
            var targets = editor.targets;
            var noCameraHelp = ux.AddChild(InspectorUtility.CreateHelpBoxWithButton(
                k_NeedCamera, HelpBoxMessageType.Warning,
                k_AddCamera, () => AddCmCameraToTargets(targets)));

            var text = string.Empty;
            switch (requiredTargets)
            {
                case RequiredTargets.Tracking: text = k_NeedTarget; break;
                case RequiredTargets.LookAt: text = k_NeedLookAt; break;
                case RequiredTargets.Group: text = k_NeedGroup; break;
            }
            VisualElement noTargetHelp = null;
            if (text.Length > 0)
                noTargetHelp = ux.AddChild(new HelpBox(text, HelpBoxMessageType.Warning));

            // Update state
            ux.TrackAnyUserActivity(() =>
            {
                if (editor == null || editor.target == null)
                    return;  // target was deleted
                
                var noCamera = false;
                var noTarget = false;
                for (int i = 0; i < targets.Length && !noCamera; ++i)
                {
                    var t = targets[i] as CinemachineComponentBase;
                    if (t != null)
                    {
                        noCamera |= t.VirtualCamera == null || t.VirtualCamera is CinemachineCameraManagerBase;
                        switch (requiredTargets)
                        {
                            case RequiredTargets.Tracking: noTarget |= t.FollowTarget == null; break;
                            case RequiredTargets.LookAt: noTarget |= t.LookAtTarget == null; break;
                            case RequiredTargets.Group: noTarget |= t.FollowTargetAsGroup == null; break;
                        }
                    }
                    else
                    {
                        var x = targets[i] as CinemachineExtension;
                        noCamera |= x.ComponentOwner == null;
                        switch (requiredTargets)
                        {
                            case RequiredTargets.Tracking: noTarget |= noCamera || x.ComponentOwner.Follow == null; break;
                            case RequiredTargets.LookAt: noTarget |= noCamera || x.ComponentOwner.LookAt == null; break;
                            case RequiredTargets.Group: noTarget |= noCamera || x.ComponentOwner.FollowTargetAsGroup == null; break;
                        }
                    }
                }
                noCameraHelp?.SetVisible(noCamera);
                noTargetHelp?.SetVisible(noTarget && !noCamera);
            });
        }

        static void AddCmCameraToTargets(Object[] targets)
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
                const float radius = size / 2 - th;
                var pix = new Color32[size * size];
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

        /// IMGUI support - to be removed when IMGUI is gone
        public static void IMGUI_DrawMissingCmCameraHelpBox(
            this UnityEditor.Editor editor, RequiredTargets requiredTargets = RequiredTargets.None)
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
                        case RequiredTargets.Tracking: noTarget |= t.FollowTarget == null; break;
                        case RequiredTargets.LookAt: noTarget |= t.LookAtTarget == null; break;
                        case RequiredTargets.Group: noTarget |= t.FollowTargetAsGroup == null; break;
                    }
                }
                else
                {
                    var x = targets[i] as CinemachineExtension;
                    noCamera |= x.ComponentOwner == null;
                    switch (requiredTargets)
                    {
                        case RequiredTargets.Tracking: noTarget |= noCamera || x.ComponentOwner.Follow == null; break;
                        case RequiredTargets.LookAt: noTarget |= noCamera || x.ComponentOwner.LookAt == null; break;
                        case RequiredTargets.Group: noTarget |= noCamera || x.ComponentOwner.FollowTargetAsGroup == null; break;
                    }
                }
            }
            if (noCamera)
            {
                InspectorUtility.HelpBoxWithButton(
                    k_NeedCamera, MessageType.Warning,
                    new GUIContent(k_AddCamera), () => AddCmCameraToTargets(targets));
                EditorGUILayout.Space();
            }
            else if (noTarget)
            {
                var text = string.Empty;
                switch (requiredTargets)
                {
                    case RequiredTargets.Tracking: text = k_NeedTarget; break;
                    case RequiredTargets.LookAt: text = k_NeedLookAt; break;
                    case RequiredTargets.Group: text = k_NeedGroup; break;
                }
                if (text.Length > 0)
                    EditorGUILayout.HelpBox(text, MessageType.Warning);
                EditorGUILayout.Space();
            }
        }
    }
}
