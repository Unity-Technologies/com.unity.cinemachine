#if CINEMACHINE_TIMELINE

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.Timeline;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineShot))]
    sealed class CinemachineShotEditor : UnityEditor.Editor
    {
        CinemachineShot Target => target as CinemachineShot;

        CinemachineVirtualCameraBase m_CachedReferenceObject;
        List<MonoBehaviour> m_ComponentsCache = new();
        static Dictionary<System.Type, bool> s_EditorExpanded = new();

#if false // Waiting for Timeline to implement inspector in UITK
        // UITK Version

        List<InspectorElement> m_Foldouts = new();
        List<UnityEngine.Object> m_EmbeddedTargets = new();
        VisualElement m_ParentElement;

        void OnEnable() => Undo.undoRedoPerformed += UpdateComponentEditors;
        void OnDisable() => Undo.undoRedoPerformed -= UpdateComponentEditors;

        public override VisualElement CreateInspectorGUI()
        {
            var m_ParentElement = new VisualElement();

            // Auto-create shots
            var toggle = m_ParentElement.AddChild(new Toggle(CinemachineTimelinePrefs.s_AutoCreateLabel.text) 
            { 
                tooltip = CinemachineTimelinePrefs.s_AutoCreateLabel.tooltip,
                value = CinemachineTimelinePrefs.AutoCreateShotFromSceneView.Value
            });
            toggle.AddToClassList(InspectorUtility.kAlignFieldClass);
            toggle.RegisterValueChangedCallback((evt) => CinemachineTimelinePrefs.AutoCreateShotFromSceneView.Value = evt.newValue);

            // Cached scrubbing
            var row = m_ParentElement.AddChild(new VisualElement { style = { flexDirection = FlexDirection.Row }});
            var cacheToggle = row.AddChild(new Toggle(CinemachineTimelinePrefs.s_ScrubbingCacheLabel.text) 
            { 
                tooltip = CinemachineTimelinePrefs.s_ScrubbingCacheLabel.tooltip,
                value = CinemachineTimelinePrefs.UseScrubbingCache.Value,
                style = { flexGrow = 0 }
            });
            cacheToggle.AddToClassList(InspectorUtility.kAlignFieldClass);

            row.Add(new Label { text = "(experimental)", style = { flexGrow = 1, alignSelf = Align.Center } });
            var clearCacheButton = row.AddChild(new Button 
            {
                text = "Clear",
                style = { flexGrow = 0, alignSelf = Align.Center }
            });
            clearCacheButton.RegisterCallback<ClickEvent>((evt) => TargetPositionCache.ClearCache());
            clearCacheButton.SetEnabled(CinemachineTimelinePrefs.UseScrubbingCache.Value);
            cacheToggle.RegisterValueChangedCallback((evt) => 
            {
                CinemachineTimelinePrefs.UseScrubbingCache.Value = evt.newValue;
                clearCacheButton.SetEnabled(evt.newValue);
            });

            m_ParentElement.Add(new PropertyField(serializedObject.FindProperty(() => Target.VirtualCamera)));
            m_ParentElement.Add(new PropertyField(serializedObject.FindProperty(() => Target.DisplayName)));

            UpdateComponentEditors();
            return m_ParentElement;
        }

        void UpdateComponentEditors()
        {
            if (m_ParentElement == null || serializedObject == null)
                return;

            var vcamProperty = serializedObject.FindProperty(() => Target.VirtualCamera); 
            var vcam = vcamProperty.exposedReferenceValue as CinemachineVirtualCameraBase;
            UpdateEmbeddedTargets(vcam);
            if (m_CachedReferenceObject != vcam || m_Foldouts.Count != m_EmbeddedTargets.Count)
            {
                // Remove foldouts
                foreach (var f in m_Foldouts)
                    m_ParentElement.Remove(f);
                m_Foldouts.Clear();

                // Add new foldouts
                foreach (var t in m_EmbeddedTargets)
                {
                    var type = t.GetType();
                    if (!s_EditorExpanded.TryGetValue(type, out var expanded))
                        expanded = false;
                    var f = new Foldout { text = type.Name, value = expanded, style = { unityFontStyleAndWeight = FontStyle.Bold }};
                    f.Add(new InspectorElement(t) { style = { paddingLeft = 0, unityFontStyleAndWeight = FontStyle.Normal }});
                    f.RegisterValueChangedCallback((evt) =>
                    {
                        if (evt.target == f)
                            s_EditorExpanded[type] = evt.newValue;
                    });
                    f.SetVisible((t.hideFlags & HideFlags.HideInInspector) != 0);

                    m_ParentElement.Add(f);
                }
                m_CachedReferenceObject = vcam;
            }
        }

        void UpdateEmbeddedTargets(CinemachineVirtualCameraBase obj)
        {
            m_ComponentsCache.Clear();
            if (obj != null)
                obj.GetComponents(m_ComponentsCache);
            if (m_ComponentsCache.Count != m_EmbeddedTargets.Count)
            {
                m_EmbeddedTargets.Clear();
                m_EmbeddedTargets.Add(obj.transform);
                for (int i = 0; i < m_ComponentsCache.Count; ++i)
                    if (m_ComponentsCache[i] != obj)
                        m_EmbeddedTargets.Add(m_ComponentsCache[i]);
            }
        }

#else
        // IMGUI version

        UnityEditor.Editor[] m_Editors = null;

        void OnDisable() => DestroyComponentEditors();
        void OnDestroy() => DestroyComponentEditors();

        readonly GUIContent s_CmCameraLabel = new GUIContent("CmCamera", "The Cinemachine camera to use for this shot");
        readonly GUIContent m_ClearText = new GUIContent("Clear", "Clear the target position scrubbing cache");

        public override void OnInspectorGUI()
        {
            EditorGUI.indentLevel = 0; // otherwise subeditor layouts get screwed up

            CinemachineTimelinePrefs.AutoCreateShotFromSceneView.Value = EditorGUILayout.Toggle(
                CinemachineTimelinePrefs.s_AutoCreateLabel, CinemachineTimelinePrefs.AutoCreateShotFromSceneView.Value);

            Rect rect;
            GUI.enabled = !Application.isPlaying;
            rect = EditorGUILayout.GetControlRect();
            var r = rect;
            r.width = EditorGUIUtility.labelWidth + EditorGUIUtility.singleLineHeight;
            if (Application.isPlaying)
                EditorGUI.Toggle(r, CinemachineTimelinePrefs.s_ScrubbingCacheLabel, false);
            else
                CinemachineTimelinePrefs.UseScrubbingCache.Value = EditorGUI.Toggle(
                    r, CinemachineTimelinePrefs.s_ScrubbingCacheLabel, CinemachineTimelinePrefs.UseScrubbingCache.Value);
            r.x += r.width; r.width = rect.width - r.width;
            var buttonWidth = GUI.skin.button.CalcSize(m_ClearText).x;
            r.width -= buttonWidth;
            EditorGUI.LabelField(r, "(experimental)");
            r.x += r.width; r.width =buttonWidth;
            GUI.enabled &= !TargetPositionCache.IsEmpty;
            if (GUI.Button(r, m_ClearText))
                TargetPositionCache.ClearCache();
            GUI.enabled = true;

            EditorGUILayout.Space();
            EditorGUI.BeginChangeCheck();
            SerializedProperty vcamProperty = serializedObject.FindProperty(() => Target.VirtualCamera); 
            CinemachineVirtualCameraBase vcam = vcamProperty.exposedReferenceValue as CinemachineVirtualCameraBase;
            if (vcam != null)
                EditorGUILayout.PropertyField(vcamProperty, s_CmCameraLabel);
            else
            {
                GUIContent createLabel = new GUIContent("Create");
                Vector2 createSize = GUI.skin.button.CalcSize(createLabel);

                rect = EditorGUILayout.GetControlRect(true);
                rect.width -= createSize.x;

                EditorGUI.PropertyField(rect, vcamProperty, s_CmCameraLabel);
                rect.x += rect.width; rect.width = createSize.x;
                if (GUI.Button(rect, createLabel))
                {
                    vcam = CreatePassiveVcamFromSceneView();
                    vcamProperty.exposedReferenceValue = vcam;
                }
                serializedObject.ApplyModifiedProperties();
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty(() => Target.DisplayName));

            if (vcam != null)
                DrawSubeditors(vcam);

            // by default timeline rebuilds the entire graph when something changes,
            // but if a property of the virtual camera changes, we only need to re-evaluate the timeline.
            // this prevents flicker on post processing updates
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                TimelineEditor.Refresh(RefreshReason.SceneNeedsUpdate);
                GUI.changed = false;
            }
        }

        void DrawSubeditors(CinemachineVirtualCameraBase vcam)
        {
            // Create an editor for each of the cinemachine virtual cam and its components
            GUIStyle foldoutStyle = new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold };
            UpdateComponentEditors(vcam);
            if (m_Editors != null)
            {
                foreach (UnityEditor.Editor e in m_Editors)
                {
                    if (e == null || e.target == null || (e.target.hideFlags & HideFlags.HideInInspector) != 0)
                        continue;

                    // Separator line - how do you make a thinner one?
                    GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) } );

                    bool expanded = true;
                    if (!s_EditorExpanded.TryGetValue(e.target.GetType(), out expanded))
                        expanded = true;
                    expanded = EditorGUILayout.Foldout(
                        expanded, e.target.GetType().Name, true, foldoutStyle);
                    if (expanded)
                        e.OnInspectorGUI();
                    s_EditorExpanded[e.target.GetType()] = expanded;
                }
            }
        }

        void UpdateComponentEditors(CinemachineVirtualCameraBase obj)
        {
            m_ComponentsCache.Clear();
            if (obj != null)
                obj.gameObject.GetComponents(m_ComponentsCache);
            int numComponents = m_ComponentsCache.Count;
            int numEditors = (m_Editors == null) ? 0 : m_Editors.Length;
            if (m_CachedReferenceObject != obj || (numComponents + 1) != numEditors)
            {
                DestroyComponentEditors();
                m_CachedReferenceObject = obj;
                if (obj != null)
                {
                    m_Editors = new UnityEditor.Editor[m_ComponentsCache.Count + 1];
                    CreateCachedEditor(obj.gameObject.GetComponent<Transform>(), null, ref m_Editors[0]);
                    for (int i = 0; i < m_ComponentsCache.Count; ++i)
                        CreateCachedEditor(m_ComponentsCache[i], null, ref m_Editors[i + 1]);
                }
            }
        }

        void DestroyComponentEditors()
        {
            m_CachedReferenceObject = null;
            if (m_Editors != null)
            {
                for (int i = 0; i < m_Editors.Length; ++i)
                {
                    if (m_Editors[i] != null)
                        UnityEngine.Object.DestroyImmediate(m_Editors[i]);
                    m_Editors[i] = null;
                }
                m_Editors = null;
            }
        }
#endif

        [InitializeOnLoad]
        class SyncCacheEnabledSetting
        {
            static SyncCacheEnabledSetting() => TargetPositionCache.UseCache = CinemachineTimelinePrefs.UseScrubbingCache.Value;
        }

        public static CinemachineVirtualCameraBase CreatePassiveVcamFromSceneView()
        {
            var vcam = CinemachineMenu.CreatePassiveCmCamera("CmCamera", null, false);
            vcam.StandbyUpdate = CinemachineVirtualCameraBase.StandbyUpdateMode.Never;

#if false 
            // GML this is too bold.  What if timeline is a child of something moving?
            // also, SetActive(false) prevents the animator from being able to animate the object
            vcam.gameObject.SetActive(false);
            var d = TimelineEditor.inspectedDirector;
            if (d != null)
                Undo.SetTransformParent(vcam.transform, d.transform, "");
#endif
            return vcam;
        }
    }
}
#endif
