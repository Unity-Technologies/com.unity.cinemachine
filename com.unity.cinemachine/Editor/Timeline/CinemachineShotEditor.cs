#if CINEMACHINE_TIMELINE

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Collections.Generic;

namespace Cinemachine.Editor
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
        List<Foldout> m_Foldouts = new();
        List<UnityEngine.Object> m_EmbeddedTargets = new();
        VisualElement m_ParentElement;
        VisualElement m_CreateButton;

        SerializedProperty m_vcamProperty;
        CinemachineVirtualCameraBase m_CachedReferenceObject;
        List<MonoBehaviour> m_ComponentsCache = new();
        static Dictionary<System.Type, bool> s_EditorExpanded = new();

        readonly GUIContent s_CmCameraLabel = new ("Cinemachine Camera", "The Cinemachine camera to use for this shot");

        public override VisualElement CreateInspectorGUI()
        {
            m_ParentElement = new VisualElement();

            // Auto-create shots
            var toggle = m_ParentElement.AddChild(new Toggle(CinemachineTimelinePrefs.s_AutoCreateLabel.text) 
            { 
                tooltip = CinemachineTimelinePrefs.s_AutoCreateLabel.tooltip,
                value = CinemachineTimelinePrefs.AutoCreateShotFromSceneView.Value
            });
            toggle.AddToClassList(InspectorUtility.kAlignFieldClass);
            toggle.RegisterValueChangedCallback((evt) => CinemachineTimelinePrefs.AutoCreateShotFromSceneView.Value = evt.newValue);

            // Cached scrubbing
            var row = m_ParentElement.AddChild(new InspectorUtility.LeftRightContainer());
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
            m_vcamProperty = serializedObject.FindProperty(() => Target.VirtualCamera);
            row = m_ParentElement.AddChild(new InspectorUtility.LeftRightContainer());
            row.Left.AddChild(new Label(s_CmCameraLabel.text) 
            { 
                tooltip = s_CmCameraLabel.tooltip, 
                style = { alignSelf = Align.Center, flexGrow = 1 }
            });
            row.Right.Add(new IMGUIContainer(() =>
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(m_vcamProperty, GUIContent.none);
                if (EditorGUI.EndChangeCheck())
                    serializedObject.ApplyModifiedProperties();
            }) { style = { flexGrow = 1, marginBottom = 2 }} );
            m_CreateButton = row.Right.AddChild(new Button(() => 
            {
                m_vcamProperty.exposedReferenceValue = CreatePassiveVcamFromSceneView();
                m_vcamProperty.serializedObject.ApplyModifiedProperties();
            })
            {
                text = "Create",
                tooltip = "Create a passive Cinemachine camera matching the scene view",
                style = { flexGrow = 0, alignSelf = Align.Center, marginLeft = 5 }
            });

            // Display name
            m_ParentElement.Add(new PropertyField(serializedObject.FindProperty(() => Target.DisplayName)));
            m_ParentElement.AddSpace();

            // This timer is required because with the current implementation of ExposedReference 
            // it is not possible to track property changes or monitor Undo.
            // GML todo: remove when UITK ExposedReference bugs are fixed.
            m_ParentElement.schedule.Execute(UpdateComponentEditors).Every(250); 

            return m_ParentElement;
        }

        void UpdateComponentEditors()
        {
            if (m_ParentElement == null || serializedObject == null)
                return;

            m_CreateButton.SetVisible(m_vcamProperty.exposedReferenceValue as CinemachineVirtualCameraBase == null);
            var vcam = m_vcamProperty.exposedReferenceValue as CinemachineVirtualCameraBase;
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
                    var f = new Foldout { text = type.Name, value = expanded, style = { marginTop = 4, marginLeft = 0 }};
                    f.AddToClassList("clip-inspector-custom-properties__foldout"); // make it pretty
                    f.Add(new InspectorElement(t) { style = { paddingLeft = 0, paddingRight = 0 }});
                    f.RegisterValueChangedCallback((evt) => 
                    {
                        if (evt.target == f)
                            s_EditorExpanded[type] = evt.newValue;
                    });
                    f.contentContainer.style.marginLeft = 0; // kill the indent

                    m_Foldouts.Add(f);
                    m_ParentElement.Add(f);
                }
                m_CachedReferenceObject = vcam;
            }
        }

        void UpdateEmbeddedTargets(CinemachineVirtualCameraBase vcam)
        {
            m_ComponentsCache.Clear();
            if (vcam == null)
                m_EmbeddedTargets.Clear();
            else
            {
                vcam.GetComponents(m_ComponentsCache);
                if (m_ComponentsCache.Count + 1 != m_EmbeddedTargets.Count)
                {
                    m_EmbeddedTargets.Clear();
                    m_EmbeddedTargets.Add(vcam.transform);
                    for (int i = 0; i < m_ComponentsCache.Count; ++i)
                        m_EmbeddedTargets.Add(m_ComponentsCache[i]);
                }
            }
        }    

#else // IMGUI VERSION
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
                        UnityEngine.Object.DestroyImmediate(m_editors[i]);
                    m_editors[i] = null;
                }
                m_editors = null;
            }
        }
#endif
    }
}
#endif
