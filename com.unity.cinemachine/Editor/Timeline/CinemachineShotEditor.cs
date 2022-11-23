//#define GML_USE_UITK // This is not working because Timeline assumes IMGUI

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
#if GML_USE_UITK
    sealed class CinemachineShotEditor : UnityEditor.Editor
    {
        CinemachineShot Target => target as CinemachineShot;
#else
    sealed class CinemachineShotEditor : BaseEditor<CinemachineShot>
    {
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

        void OnDisable() => DestroyComponentEditors();
        void OnDestroy() => DestroyComponentEditors();

#if GML_USE_UITK
        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();

            // Auto-create shots
            var toggle = ux.AddChild(new Toggle(CinemachineTimelinePrefs.s_AutoCreateLabel.text) 
            { 
                tooltip = CinemachineTimelinePrefs.s_AutoCreateLabel.tooltip,
                value = CinemachineTimelinePrefs.AutoCreateShotFromSceneView.Value
            });
            toggle.AddToClassList(InspectorUtility.kAlignFieldClass);
            toggle.RegisterValueChangedCallback((evt) => CinemachineTimelinePrefs.AutoCreateShotFromSceneView.Value = evt.newValue);

            // Cached scrubbing
            var row = ux.AddChild(new VisualElement { style = { flexDirection = FlexDirection.Row }});
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

            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.VirtualCamera)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.DisplayName)));

            return ux;
        }
 #else
        readonly GUIContent s_CmCameraLabel = new GUIContent("CmCamera", "The Cinemachine camera to use for this shot");
        readonly GUIContent m_ClearText = new GUIContent("Clear", "Clear the target position scrubbing cache");

        /// <summary>Get the property names to exclude in the inspector.</summary>
        /// <param name="excluded">Add the names to this list</param>
        protected override void GetExcludedPropertiesInInspector(List<string> excluded)
        {
            base.GetExcludedPropertiesInInspector(excluded);
            excluded.Add(FieldPath(x => x.VirtualCamera));
        }

        public override void OnInspectorGUI()
        {
            BeginInspector();
            SerializedProperty vcamProperty = FindProperty(x => x.VirtualCamera);
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
            DrawRemainingPropertiesInInspector();

            if (vcam != null)
                DrawSubeditors(vcam);

            // by default timeline rebuilds the entire graph when something changes,
            // but if a property of the virtual camera changes, we only need to re-evaluate the timeline.
            // this prevents flicker on post processing updates
            if (EditorGUI.EndChangeCheck())
            {
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
#endif

        CinemachineVirtualCameraBase m_CachedReferenceObject;
        UnityEditor.Editor[] m_Editors = null;
        static Dictionary<System.Type, bool> s_EditorExpanded = new();

        void UpdateComponentEditors(CinemachineVirtualCameraBase obj)
        {
            MonoBehaviour[] components = null;
            if (obj != null)
                components = obj.gameObject.GetComponents<MonoBehaviour>();
            int numComponents = (components == null) ? 0 : components.Length;
            int numEditors = (m_Editors == null) ? 0 : m_Editors.Length;
            if (m_CachedReferenceObject != obj || (numComponents + 1) != numEditors)
            {
                DestroyComponentEditors();
                m_CachedReferenceObject = obj;
                if (obj != null)
                {
                    m_Editors = new UnityEditor.Editor[components.Length + 1];
                    CreateCachedEditor(obj.gameObject.GetComponent<Transform>(), null, ref m_Editors[0]);
                    for (int i = 0; i < components.Length; ++i)
                        CreateCachedEditor(components[i], null, ref m_Editors[i + 1]);
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
    }
}
#endif
