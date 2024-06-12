#if CINEMACHINE_TIMELINE

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Collections.Generic;

namespace Unity.Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineShot))]
    class CinemachineShotEditor : UnityEditor.Editor
    {
        CinemachineShot Target => target as CinemachineShot;

        [InitializeOnLoad]
        class SyncCacheEnabledSetting
        {
            static SyncCacheEnabledSetting() => TargetPositionCache.UseCache = CinemachineTimelinePrefs.UseScrubbingCache.Value;
        }

        public static CinemachineVirtualCameraBase CreatePassiveVcamFromSceneView()
        {
            var vcam = CinemachineMenu.CreatePassiveCmCamera("CinemachineCamera", null, false);
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

#if CINEMACHINE_TIMELINE_1_8_2
        VisualElement m_ParentElement;
        VisualElement m_CreateButton;
        CinemachineVirtualCameraBase m_CachedReferenceObject;
        readonly List<MonoBehaviour> m_ComponentsCache = new ();
        readonly List<Subeditor> m_Subeditors = new ();

        class Subeditor
        {
            // Keep track of which component types are expanded
            static Dictionary<System.Type, bool> s_EditorExpanded = new ();

            UnityEditor.Editor m_Editor;
            
            public Object Target { get; private set; }
            public Foldout Foldout { get; private set; }

            public Subeditor(Object target)
            {
                Target = target;

                // Target can be null for behaviours with missing scripts
                if (target == null)
                    return;

                CreateCachedEditor(target, null, ref m_Editor);

                // Wrap editor in a foldout
                var type = target.GetType();
                s_EditorExpanded.TryGetValue(type, out var expanded);
                Foldout = new Foldout { text = type.Name, value = expanded, style = { marginTop = 4, marginLeft = 0 }};
                Foldout.AddToClassList("clip-inspector-custom-properties__foldout"); // make it pretty
                Foldout.Add(new InspectorElement(m_Editor) { style = { paddingLeft = 0, paddingRight = 0 }});
                Foldout.RegisterValueChangedCallback((evt) => 
                {
                    if (evt.target == Foldout)
                        s_EditorExpanded[type] = evt.newValue;
                });
                Foldout.contentContainer.style.marginLeft = 0; // kill the indent
            }

            public void Dispose()
            {
                Foldout?.parent?.Remove(Foldout);
                if (m_Editor != null)
                    DestroyImmediate(m_Editor);
                Foldout = null;
                m_Editor = null;
                Target = null;
            }
        }

        void DestroySubeditors()
        {
            for (int i = 0; i < m_Subeditors.Count; ++i)
                m_Subeditors[i].Dispose();
            m_Subeditors.Clear();
        }

        void OnDisable() => DestroySubeditors();

        public override VisualElement CreateInspectorGUI()
        {
            m_ParentElement = new VisualElement();

            // Auto-create shots
            var toggle = m_ParentElement.AddChild(new Toggle(CinemachineTimelinePrefs.s_AutoCreateLabel.text) 
            { 
                tooltip = CinemachineTimelinePrefs.s_AutoCreateLabel.tooltip,
                value = CinemachineTimelinePrefs.AutoCreateShotFromSceneView.Value
            });
            toggle.AddToClassList(InspectorUtility.AlignFieldClassName);
            toggle.RegisterValueChangedCallback((evt) => CinemachineTimelinePrefs.AutoCreateShotFromSceneView.Value = evt.newValue);

            // Cached scrubbing
            var row = m_ParentElement.AddChild(new InspectorUtility.LeftRightRow());
            row.Left.AddChild(new Label(CinemachineTimelinePrefs.s_ScrubbingCacheLabel.text) 
            { 
                tooltip = CinemachineTimelinePrefs.s_ScrubbingCacheLabel.tooltip, 
                style = { alignSelf = Align.Center, flexGrow = 1 }
            });
            var cacheToggle = row.Right.AddChild(new Toggle 
            { 
                tooltip = CinemachineTimelinePrefs.s_ScrubbingCacheLabel.tooltip,
                value = CinemachineTimelinePrefs.UseScrubbingCache.Value,
                style = { flexGrow = 0, marginRight = 5 }
            });
            row.Right.Add(new Label { text = "(experimental)", style = { flexGrow = 1, alignSelf = Align.Center } });
            var clearCacheButton = row.Right.AddChild(new Button 
            {
                text = "Clear",
                style = { flexGrow = 0, alignSelf = Align.Center, marginLeft = 5 }
            });
            clearCacheButton.RegisterCallback<ClickEvent>((evt) => TargetPositionCache.ClearCache());
            clearCacheButton.SetEnabled(CinemachineTimelinePrefs.UseScrubbingCache.Value);
            cacheToggle.RegisterValueChangedCallback((evt) => 
            {
                CinemachineTimelinePrefs.UseScrubbingCache.Value = evt.newValue;
                clearCacheButton.SetEnabled(evt.newValue);
            });

            // Camera Reference - we do it in IMGUI until the ExposedReference UITK bugs are fixed
            m_ParentElement.AddSpace();
            var vcamProperty = serializedObject.FindProperty(() => Target.VirtualCamera);
            row = m_ParentElement.AddChild(new InspectorUtility.LeftRightRow());
            row.Left.AddChild(new Label("Cinemachine Camera") 
            { 
                tooltip = "The Cinemachine camera to use for this shot", 
                style = { alignSelf = Align.Center, flexGrow = 1 }
            });
            row.Right.Add(new IMGUIContainer(() =>
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(vcamProperty, GUIContent.none);
                if (EditorGUI.EndChangeCheck())
                    serializedObject.ApplyModifiedProperties();
            }) { style = { flexGrow = 1, marginBottom = 2 }} );
            m_CreateButton = row.Right.AddChild(new Button(() => 
            {
                vcamProperty.exposedReferenceValue = CreatePassiveVcamFromSceneView();
                vcamProperty.serializedObject.ApplyModifiedProperties();
            })
            {
                text = "Create",
                tooltip = "Create a passive Cinemachine camera matching the scene view",
                style = { flexGrow = 0, alignSelf = Align.Center, marginLeft = 5 }
            });

            // Display name
            m_ParentElement.Add(new PropertyField(serializedObject.FindProperty(() => Target.DisplayName)));
            m_ParentElement.AddSpace();

            // Component editors
            m_ParentElement.TrackAnyUserActivity(UpdateComponentEditors);

            return m_ParentElement;
        }

        void UpdateComponentEditors()
        {
            if (m_ParentElement == null || serializedObject == null)
                return;

            var vcamProperty = serializedObject.FindProperty(() => Target.VirtualCamera);
            m_CreateButton.SetVisible(vcamProperty.exposedReferenceValue as CinemachineVirtualCameraBase == null);
            var vcam = vcamProperty.exposedReferenceValue as CinemachineVirtualCameraBase;

            m_ComponentsCache.Clear();
            if (vcam != null)
                vcam.GetComponents(m_ComponentsCache);

            bool dirty = m_CachedReferenceObject != vcam || m_Subeditors.Count != m_ComponentsCache.Count + 1;
            for (int i = 0; !dirty && i < m_ComponentsCache.Count; ++i)
                dirty = m_Subeditors[i + 1].Target != m_ComponentsCache[i];
            if (dirty)
            {
                DestroySubeditors();
                m_CachedReferenceObject = vcam;
                if (vcam != null)
                {
                    m_Subeditors.Add(new Subeditor(vcam.transform));
                    for (int i = 0; i < m_ComponentsCache.Count; ++i)
                        m_Subeditors.Add(new Subeditor(m_ComponentsCache[i]));
                    for (int i = 0; i < m_Subeditors.Count; ++i)
                        m_ParentElement.Add(m_Subeditors[i].Foldout);
                }
            }
        }


#else // IMGUI VERSION - used for older Timeline versions
        readonly GUIContent s_CmCameraLabel = new ("CinemachineCamera", "The Cinemachine camera to use for this shot");
        readonly GUIContent m_ClearText = new ("Clear", "Clear the target position scrubbing cache");

        void OnDisable() => DestroyComponentEditors();
        void OnDestroy() => DestroyComponentEditors();

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

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
            var vcamProperty = serializedObject.FindProperty(() => Target.VirtualCamera);
            CinemachineVirtualCameraBase vcam
                = vcamProperty.exposedReferenceValue as CinemachineVirtualCameraBase;
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

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serializedObject.FindProperty(() => Target.DisplayName));
            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "For best inspector display, please upgrade Timeline to version 1.8.2 or later", 
                MessageType.Info);

            EditorGUI.BeginChangeCheck();
            if (vcam != null)
                DrawSubeditors(vcam);

            // by default timeline rebuilds the entire graph when something changes,
            // but if a property of the virtual camera changes, we only need to re-evaluate the timeline.
            // this prevents flicker on post processing updates
            if (EditorGUI.EndChangeCheck())
            {
                UnityEditor.Timeline.TimelineEditor.Refresh(UnityEditor.Timeline.RefreshReason.SceneNeedsUpdate);
                GUI.changed = false;
            }
        }

        void DrawSubeditors(CinemachineVirtualCameraBase vcam)
        {
            // Create an editor for each of the cinemachine virtual cam and its components
            GUIStyle foldoutStyle = new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold };
            UpdateComponentEditors(vcam);
            if (m_editors != null)
            {
                foreach (UnityEditor.Editor e in m_editors)
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

        CinemachineVirtualCameraBase m_cachedReferenceObject;
        UnityEditor.Editor[] m_editors = null;
        static Dictionary<System.Type, bool> s_EditorExpanded = new();

        void UpdateComponentEditors(CinemachineVirtualCameraBase obj)
        {
            MonoBehaviour[] components = null;
            if (obj != null)
                components = obj.gameObject.GetComponents<MonoBehaviour>();
            int numComponents = (components == null) ? 0 : components.Length;
            int numEditors = (m_editors == null) ? 0 : m_editors.Length;
            if (m_cachedReferenceObject != obj || (numComponents + 1) != numEditors)
            {
                DestroyComponentEditors();
                m_cachedReferenceObject = obj;
                if (obj != null)
                {
                    m_editors = new UnityEditor.Editor[components.Length + 1];
                    CreateCachedEditor(obj.gameObject.GetComponent<Transform>(), null, ref m_editors[0]);
                    for (int i = 0; i < components.Length; ++i)
                        CreateCachedEditor(components[i], null, ref m_editors[i + 1]);
                }
            }
        }

        void DestroyComponentEditors()
        {
            m_cachedReferenceObject = null;
            if (m_editors != null)
            {
                for (int i = 0; i < m_editors.Length; ++i)
                {
                    if (m_editors[i] != null)
                        Object.DestroyImmediate(m_editors[i]);
                    m_editors[i] = null;
                }
                m_editors = null;
            }
        }
#endif
    }
}
#endif
