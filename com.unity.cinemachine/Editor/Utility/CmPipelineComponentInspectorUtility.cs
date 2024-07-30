using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.Cinemachine.Editor
{
    /// <summary>
    /// Helpers for drawing CmComponentBase or CmExtension inspectors.
    /// </summary>
    static class CmPipelineComponentInspectorUtility
    {
        const string k_NeedTarget = "A Tracking Target is required in the CinemachineCamera.";
        const string k_NeedLookAt = "A LookAt Target is required in the CinemachineCamera.";
        const string k_NeedGroupTarget = "The Tracking Target in the CinemachineCamera must be a Target Group.";
        const string k_NeedGroupLookAt = "The LookAt Target in the CinemachineCamera must be a Target Group.";
        const string k_NeedCamera = "This component is intended to be used only with a CinemachineCamera.";
        const string k_AddCamera = "Add CinemachineCamera";
        const string k_DuplicateComponent = "This component is redundant and will be ignored.";

        /// <summary>
        /// Add help box for CinemachineComponentBase or CinemachineExtension editors, 
        /// prompting to solve a missing CinemachineCamera component or a missing tracking target
        /// </summary>
        public static void AddMissingCmCameraHelpBox(this UnityEditor.Editor editor, VisualElement ux)
        {
            // Look for a RequiredTargetAttribute, but only if component is on a CinemachineCamera
            var requiredTargets = RequiredTargetAttribute.RequiredTargets.None;
            if (editor.target is Component c && c != null && c.TryGetComponent<CinemachineCamera>(out _))
            {
                var a = editor.target.GetType().GetCustomAttribute<RequiredTargetAttribute>();
                if (a != null)
                    requiredTargets = a.RequiredTarget;
            }
            var targets = editor.targets;
            var noCameraHelp = ux.AddChild(InspectorUtility.HelpBoxWithButton(
                k_NeedCamera, HelpBoxMessageType.Warning,
                k_AddCamera, () => AddCmCameraToTargets(targets)));

            var targetText = string.Empty;
            var lookAtText = string.Empty;
            switch (requiredTargets)
            {
                case RequiredTargetAttribute.RequiredTargets.Tracking: targetText = k_NeedTarget; break;
                case RequiredTargetAttribute.RequiredTargets.LookAt: targetText = k_NeedTarget; lookAtText = k_NeedLookAt; break;
                case RequiredTargetAttribute.RequiredTargets.GroupLookAt: targetText = k_NeedGroupTarget; lookAtText = k_NeedGroupLookAt; break;
            }
            var noTargetHelp = targetText.Length > 0 ? ux.AddChild(new HelpBox(targetText, HelpBoxMessageType.Warning)) : null;
            var noLookAtHelp = lookAtText.Length > 0 ? ux.AddChild(new HelpBox(lookAtText, HelpBoxMessageType.Warning)) : null;
            var duplicateHelp = ux.AddChild(new HelpBox(k_DuplicateComponent, HelpBoxMessageType.Error));

            // Update state
            ux.TrackAnyUserActivity(() =>
            {
                if (editor == null || editor.target == null)
                    return;  // target was deleted
                
                var noCamera = false;
                var noTarget = false;
                var noLookAtTarget = false;
                var isDuplicate = false;
                for (int i = 0; i < targets.Length && !noCamera; ++i)
                {
                    if (targets[i] is CinemachineComponentBase c)
                    {
                        var vcam = c.VirtualCamera;
                        noCamera |= vcam == null || vcam is CinemachineCameraManagerBase;
                        if (vcam != null)
                            vcam.UpdateTargetCache();
                        switch (requiredTargets)
                        {
                            case RequiredTargetAttribute.RequiredTargets.Tracking: noTarget |= c.FollowTarget == null; break;
                            case RequiredTargetAttribute.RequiredTargets.LookAt: noLookAtTarget |= c.LookAtTarget == null; break;
                            case RequiredTargetAttribute.RequiredTargets.GroupLookAt: 
                                noLookAtTarget |= c.LookAtTargetAsGroup == null || !c.LookAtTargetAsGroup.IsValid; break;
                        }
                        if (vcam != null && vcam.GetCinemachineComponent(c.Stage) != c)
                            isDuplicate = true;
                    }
                    else if (targets[i] is CinemachineExtension x)
                    {
                        var vcam = x.ComponentOwner;
                        noCamera |= vcam == null;
                        if (vcam != null)
                        {
                            vcam.UpdateTargetCache();
                            switch (requiredTargets)
                            {
                                case RequiredTargetAttribute.RequiredTargets.Tracking: noTarget |= vcam.Follow == null; break;
                                case RequiredTargetAttribute.RequiredTargets.LookAt: noLookAtTarget |= vcam.LookAt == null; break;
                                case RequiredTargetAttribute.RequiredTargets.GroupLookAt: 
                                    noLookAtTarget |= vcam.LookAtTargetAsGroup == null || !vcam.LookAtTargetAsGroup.IsValid; break;
                            }
                        }
                    }
                    else if (targets[i] is MonoBehaviour b)
                        noCamera |= !b.TryGetComponent<CinemachineVirtualCameraBase>(out _);
                }
                noCameraHelp?.SetVisible(noCamera);
                noTargetHelp?.SetVisible(noTarget && !noCamera);
                noLookAtHelp?.SetVisible(noLookAtTarget && !noCamera);
                duplicateHelp?.SetVisible(isDuplicate);
            });
        }

        static void AddCmCameraToTargets(Object[] targets)
        {
            for (int i = 0; i < targets.Length; ++i)
            {
                if (targets[i] is CinemachineComponentBase c)
                {
                    if (c.VirtualCamera == null)
                        Undo.AddComponent<CinemachineCamera>(c.gameObject);
                }
                else if (targets[i] is CinemachineExtension x)
                {
                    if (x != null && x.ComponentOwner == null)
                        Undo.AddComponent<CinemachineCamera>(x.gameObject).AddExtension(x);
                }
                else if (targets[i] is MonoBehaviour b)
                {
                    if (!b.TryGetComponent<CinemachineVirtualCameraBase>(out _))
                        Undo.AddComponent<CinemachineCamera>(b.gameObject);
                }
            }
        }

        static List<Type> s_AllAxisControllerTypes;

        public static void AddInputControllerHelp(
            this UnityEditor.Editor editor, VisualElement ux, string text)
        {
            if (s_AllAxisControllerTypes == null)
            {
                var allTypes = ReflectionHelpers.GetTypesDerivedFrom(typeof(IInputAxisController),
                    (t) => !t.IsAbstract && typeof(MonoBehaviour).IsAssignableFrom(t) 
                        && t.GetCustomAttribute<ObsoleteAttribute>() == null);
                s_AllAxisControllerTypes = new();
                var iter = allTypes.GetEnumerator();
                while (iter.MoveNext())
                    s_AllAxisControllerTypes.Add(iter.Current);
            }
            ContextualMenuManipulator menu = null;
            if (s_AllAxisControllerTypes.Count > 1)
            {
                menu = new ContextualMenuManipulator((evt) => 
                {
                    for (int i = 0; i < s_AllAxisControllerTypes.Count; ++i)
                    {
                        var t = s_AllAxisControllerTypes[i];
                        evt.menu.AppendAction(ObjectNames.NicifyVariableName(t.Name), (action) => AddController(t));
                    }
                });
            }

            var help = ux.AddChild(InspectorUtility.HelpBoxWithButton(
                text, HelpBoxMessageType.Info, "Add Input Controller", 
                () => 
                {
                    if (s_AllAxisControllerTypes.Count == 1) 
                        AddController(s_AllAxisControllerTypes[0]);
                },
                menu));

            ux.TrackAnyUserActivity(() =>
            {
                if (editor == null)
                    return;  // target was deleted
                var noHandler = false;
                for (int i = 0; i < editor.targets.Length; ++i)
                    if (editor.targets[i] is IInputAxisResetSource src)
                        noHandler |= !src.HasResetHandler;
                help.SetVisible(noHandler);
            });

            // Local fucntion
            void AddController(Type controllerType)
            {
                Undo.SetCurrentGroupName("Add Input Controller");
                for (int i = 0; i < editor.targets.Length; ++i)
                {
                    if (editor.targets[i] is IInputAxisResetSource src && !src.HasResetHandler)
                    {
                        var t = editor.targets[i] as MonoBehaviour;
                        if (!t.TryGetComponent<IInputAxisController>(out var c))
                            Undo.AddComponent(t.gameObject, controllerType);
                        else if (c is MonoBehaviour b && !b.enabled)
                        {
                            Undo.RecordObject(b, "enable controller");
                            b.enabled = true;
                        }
                    }
                }
            };
        }
            
        public static void OnGUI_DrawOnscreenTargetMarker(Vector3 worldPoint, Camera camera)
        {
            var c = camera.WorldToScreenPoint(worldPoint);
            c.y = Screen.height - c.y;
            if (c.z > 0)
            {
                var oldColor = GUI.color;
                var r = new Rect(c, Vector2.zero).Inflated(Vector2.one * CinemachineComposerPrefs.TargetSize.Value);
                GUI.color = new Color(0, 0, 0, CinemachineComposerPrefs.OverlayOpacity.Value);
                GUI.DrawTexture(r.Inflated(new Vector2(1, 1)), Texture2D.whiteTexture, ScaleMode.StretchToFill);
                var color = CinemachineComposerPrefs.TargetColour.Value;
                GUI.color = color;
                GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill);
                GUI.color = oldColor;
            }
        }

        public static void OnGUI_DrawOnscreenGroupSizeMarker(
            Bounds groupBounds, Matrix4x4 cameraViewMatrix, Camera camera)
        {
            var c = groupBounds.center; c.z -= groupBounds.extents.z; groupBounds.center = c;
            var e = groupBounds.extents; e.z = 0; groupBounds.extents = e;
                
            c = camera.WorldToScreenPoint(cameraViewMatrix.MultiplyPoint3x4(c));
            if (c.z < 0)
                return;
            e = camera.WorldToScreenPoint(cameraViewMatrix.MultiplyPoint3x4(groupBounds.center + e));
            var groupSize = new Vector2(Mathf.Abs(e.x - c.x), Mathf.Abs(e.y - c.y));

            var radius = Mathf.Max(groupSize.x, groupSize.y);
            if (radius > CinemachineComposerPrefs.TargetSize.Value)
            {
                var oldColor = GUI.color;
                var color = CinemachineComposerPrefs.TargetColour.Value;
                color.a = Mathf.Lerp(1f, CinemachineComposerPrefs.OverlayOpacity.Value, (radius - 10f) / 100f);
                GUI.color = color;
                c.y = camera.pixelHeight - c.y;
                var r = new Rect(c, Vector2.zero).Inflated(groupSize);
                GUI.DrawTexture(r, GetTargetMarkerTex(), ScaleMode.StretchToFill);
                GUI.color = oldColor;
            }
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

#if !CINEMACHINE_NO_CM2_SUPPORT
        /// IMGUI support - to be removed when IMGUI is gone
        public static void IMGUI_DrawMissingCmCameraHelpBox(this UnityEditor.Editor editor)
        {
            // Look for a RequiredTargetAttribute
            var requiredTargets = RequiredTargetAttribute.RequiredTargets.None;
            var a = editor.target.GetType().GetCustomAttribute<RequiredTargetAttribute>();
            if (a != null)
                requiredTargets = a.RequiredTarget;
                
            bool noCamera = false;
            bool noTarget = false;
            var targets = editor.targets;
            for (int i = 0; i < targets.Length && !noCamera; ++i)
            {
                if (targets[i] is CinemachineComponentBase c)
                {
                    var vcam = c.VirtualCamera;
                    noCamera |= vcam == null || vcam is CinemachineCameraManagerBase;
                    if (vcam != null)
                        vcam.UpdateTargetCache();
                    switch (requiredTargets)
                    {
                        case RequiredTargetAttribute.RequiredTargets.Tracking: noTarget |= c.FollowTarget == null; break;
                        case RequiredTargetAttribute.RequiredTargets.LookAt: noTarget |= c.LookAtTarget == null; break;
                        case RequiredTargetAttribute.RequiredTargets.GroupLookAt: 
                            noTarget |= c.LookAtTargetAsGroup == null || !c.LookAtTargetAsGroup.IsValid; break;
                    }
                }
                else if (targets[i] is CinemachineExtension x)
                {
                    var vcam = x.ComponentOwner;
                    noCamera |= vcam == null;
                    if (vcam != null)
                    {
                        vcam.UpdateTargetCache();
                        switch (requiredTargets)
                        {
                            case RequiredTargetAttribute.RequiredTargets.Tracking: noTarget |= vcam.Follow == null; break;
                            case RequiredTargetAttribute.RequiredTargets.LookAt: noTarget |= vcam.LookAt == null; break;
                            case RequiredTargetAttribute.RequiredTargets.GroupLookAt: 
                                noTarget |= vcam.LookAtTargetAsGroup == null || !vcam.LookAtTargetAsGroup.IsValid; break;
                        }
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
                    case RequiredTargetAttribute.RequiredTargets.Tracking: text = k_NeedTarget; break;
                    case RequiredTargetAttribute.RequiredTargets.LookAt: text = k_NeedLookAt; break;
                    case RequiredTargetAttribute.RequiredTargets.GroupLookAt: text = k_NeedGroupLookAt; break;
                }
                if (text.Length > 0)
                    EditorGUILayout.HelpBox(text, MessageType.Warning);
                EditorGUILayout.Space();
            }
        }
#endif
    }
}
