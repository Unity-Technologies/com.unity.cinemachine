using UnityEngine;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Cinemachine.Editor
{
    /// <summary>
    /// Helper for drawing embedded asset editors
    /// </summary>
    class EmbeddeAssetEditor<T> where T : ScriptableObject
    {
        /// <summary>
        /// Called after the asset editor is created, in case it needs to be customized
        /// </summary>
        public OnCreateEditorDelegate OnCreateEditor;
        public delegate void OnCreateEditorDelegate(UnityEditor.Editor editor);

        /// <summary>
        /// Called when the asset being edited was changed by the user.
        /// </summary>
        public OnChangedDelegate OnChanged;
        public delegate void OnChangedDelegate(T obj);

        /// <summary>
        /// Free the resources in OnDisable()
        /// </summary>
        public void OnDisable()
        {
            DestroyEditor();
        }

        GUIContent m_CreateButtonGUIContent = new ("Create Asset", "Create a new shared settings asset");

        UnityEditor.Editor m_Editor = null;
        InspectorUtility.LeftRightContainer m_UnassignedUx;
        VisualElement m_AssignedUx;
        VisualElement m_EmbeddedInspectorParent;
        InspectorElement m_EmbeddedInspectorElement;
        static bool s_CustomBlendsExpanded;

        const int kIndentOffset = 3;

        /// <summary>
        /// Call this from OnInspectorGUI.  Will draw the asset reference field, and
        /// the embedded editor, or a Create Asset button, if no asset is set.
        /// </summary>
        public void DrawEditorCombo(
            SerializedProperty property,
            string title, string defaultName, string extension, string message,
            bool indent)
        {
            if (m_Editor == null)
                UpdateEditor(property);
            if (m_Editor == null)
                AssetFieldWithCreateButton(property, title, defaultName, extension, message);
            else
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                Rect rect = EditorGUILayout.GetControlRect(true);
                rect.height = EditorGUIUtility.singleLineHeight;
                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(rect, property);
                if (EditorGUI.EndChangeCheck())
                {
                    property.serializedObject.ApplyModifiedProperties();
                    UpdateEditor(property);
                }
                if (m_Editor != null)
                {
                    Rect foldoutRect = new Rect(
                        rect.x - kIndentOffset, rect.y, rect.width + kIndentOffset, rect.height);
                    property.isExpanded = EditorGUI.Foldout(
                        foldoutRect, property.isExpanded, GUIContent.none, true);

                    bool canEditAsset = AssetDatabase.IsOpenForEdit(m_Editor.target, StatusQueryOptions.UseCachedIfPossible);
                    GUI.enabled = canEditAsset;
                    if (property.isExpanded)
                    {
                        EditorGUILayout.Separator();
                        EditorGUILayout.HelpBox(
                            "This is a shared asset.  Changes made here will apply to all users of this asset.", 
                            MessageType.Info);
                        EditorGUI.BeginChangeCheck();
                        if (indent)
                            ++EditorGUI.indentLevel;
                        m_Editor.OnInspectorGUI();
                        if (indent)
                            --EditorGUI.indentLevel;
                        if (EditorGUI.EndChangeCheck() && (OnChanged != null))
                            OnChanged(property.objectReferenceValue as T);
                    }
                    GUI.enabled = true;
                    if (m_Editor.target != null)
                    {
                        if (!canEditAsset && GUILayout.Button("Check out"))
                        {
                            Task task = Provider.Checkout(AssetDatabase.GetAssetPath(m_Editor.target), CheckoutMode.Asset);
                            task.Wait();
                        }
                    }
                }
                EditorGUILayout.EndVertical();
            }
        }

        void AssetFieldWithCreateButton(
            SerializedProperty property,
            string title, string defaultName, string extension, string message)
        {
            EditorGUI.BeginChangeCheck();

            float hSpace = 5;
            float buttonWidth = GUI.skin.button.CalcSize(m_CreateButtonGUIContent).x;
            Rect r = EditorGUILayout.GetControlRect(true);
            r.width -= buttonWidth + hSpace;
            EditorGUI.PropertyField(r, property);
            r.x += r.width + hSpace; r.width = buttonWidth;
            if (GUI.Button(r, m_CreateButtonGUIContent))
            {
                string newAssetPath = EditorUtility.SaveFilePanelInProject(
                        title, defaultName, extension, message);
                if (!string.IsNullOrEmpty(newAssetPath))
                {
                    T asset = ScriptableObjectUtility.CreateAt<T>(newAssetPath);
                    property.objectReferenceValue = asset;
                    property.serializedObject.ApplyModifiedProperties();
                }
            }
            if (EditorGUI.EndChangeCheck())
            {
                property.serializedObject.ApplyModifiedProperties();
                UpdateEditor(property);
            }
        }

        void DestroyEditor()
        {
            if (m_Editor != null)
            {
                UnityEngine.Object.DestroyImmediate(m_Editor);
                m_Editor = null;
            }
        }
        
        void UpdateEditor(SerializedProperty property)
        {
            property.serializedObject.ApplyModifiedProperties();

            var target = property.objectReferenceValue;
            if (m_Editor != null && m_Editor.target != target)
            {
                if (m_EmbeddedInspectorElement != null)
                    m_EmbeddedInspectorElement.RemoveFromHierarchy();
                DestroyEditor();
            }
            if (target != null)
            {
                if (m_Editor == null)
                {
                    m_Editor = UnityEditor.Editor.CreateEditor(target);
                    if (OnCreateEditor != null)
                        OnCreateEditor(m_Editor);
                }
                if (m_EmbeddedInspectorParent != null)
                    m_EmbeddedInspectorElement = m_EmbeddedInspectorParent.AddChild(new InspectorElement(m_Editor));
            }
            if (m_UnassignedUx != null)
                m_UnassignedUx.SetVisible(target == null);
            if (m_AssignedUx != null)
                m_AssignedUx.SetVisible(target != null);
        }

        /// <summary>
        /// Call this to create the inspector GUI.  Will draw the asset reference field, and
        /// the embedded editor, or a Create Asset button, if no asset is set.
        /// </summary>
        /// GML todo: refactor to eliminate need for member variable
        public VisualElement CreateInspectorGUI(
            SerializedProperty property,
            string title, string defaultName, string extension, string message)
        {
            var ux = new VisualElement();

            // Asset field with create button
            m_UnassignedUx = ux.AddChild(new InspectorUtility.LeftRightContainer());
            m_UnassignedUx.Left.Add(new Label(property.displayName) 
                { tooltip = property.tooltip, style = { alignSelf = Align.Center, flexGrow = 0 }});
            m_UnassignedUx.Right.Add(new PropertyField(property, "") 
                { tooltip = property.tooltip, style = { alignSelf = Align.Center, flexGrow = 0, marginRight = 5 }});
            m_UnassignedUx.Right.Add(new Button(() =>
            {
                string newAssetPath = EditorUtility.SaveFilePanelInProject(
                        title, defaultName, extension, message);
                if (!string.IsNullOrEmpty(newAssetPath))
                {
                    T asset = ScriptableObjectUtility.CreateAt<T>(newAssetPath);
                    property.objectReferenceValue = asset;
                    property.serializedObject.ApplyModifiedProperties();
                }
            })
            {
                text = "Create Asset",
                tooltip = "Create a new shared settings asset"
            });

            var foldout = new Foldout() { text = property.displayName, tooltip = property.tooltip, value = s_CustomBlendsExpanded };
            foldout.RegisterValueChangedCallback((evt) => 
            {
                if (evt.target == foldout)
                {
                    s_CustomBlendsExpanded = evt.newValue;
                    evt.StopPropagation();
                }
            });
            m_EmbeddedInspectorParent = new VisualElement();
            m_AssignedUx = ux.AddChild(new InspectorUtility.FoldoutWithOverlay(
                foldout, new PropertyField(property, ""), null) { style = { flexGrow = 1 }});
            foldout.Add(new PropertyField(property, "Asset"));
            foldout.AddSpace();

            Color borderColor = Color.grey;
            float borderWidth = 2;
            float borderRadius = 5;
            m_EmbeddedInspectorParent = foldout.AddChild(new VisualElement() { 
            style = 
            { 
                borderTopColor = borderColor, borderTopWidth = borderWidth, borderTopLeftRadius = borderRadius,
                borderBottomColor = borderColor, borderBottomWidth = borderWidth, borderBottomLeftRadius = borderRadius,
                borderLeftColor = borderColor, borderLeftWidth = borderWidth, borderTopRightRadius = borderRadius,
                borderRightColor = borderColor, borderRightWidth = borderWidth, borderBottomRightRadius = borderRadius,
            }});

            m_EmbeddedInspectorParent.Add(new HelpBox(
                "This is a shared asset.  Changes made here will apply to all users of this asset.", 
                HelpBoxMessageType.Info));

            UpdateEditor(property);
            ux.TrackPropertyValue(property, (p) => UpdateEditor(p));

            return ux;
        }
    }
}
