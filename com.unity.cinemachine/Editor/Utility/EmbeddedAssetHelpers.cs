using UnityEngine;
using UnityEditor;
using UnityEditor.VersionControl;
using System;
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
        /// Called after the asset editor is created, in case it needs
        /// to be customized
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

        GUIContent m_CreateButtonGUIContent 
            = new GUIContent("Create Asset", "Create a new shared settings asset");

        UnityEditor.Editor m_Editor = null;
        InspectorUtility.LeftRightContainer m_UnassignedUx;
        InspectorElement m_EmbeddedInspectorElement;
        VisualElement m_AssignedUx;

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
                DestroyEditor();
            if (target != null)
            {
                m_Editor = UnityEditor.Editor.CreateEditor(target);
                if (OnCreateEditor != null)
                    OnCreateEditor(m_Editor);
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
        public VisualElement CreateInspectorGUI(
            SerializedProperty property,
            string title, string defaultName, string extension, string message,
            bool indent)
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

            m_AssignedUx = ux.AddChild(new VisualElement());

            // GML todo: surround with a nice box
            //EditorGUILayout.BeginVertical(GUI.skin.box);

            m_AssignedUx.Add(new PropertyField(property));

            // GML todo: how to draw an embedded editor?
            //m_EmbeddedInspectorElement = m_AssignedUx.AddChild(new InspectorElement(m_Editor));
            //m_EmbeddedInspectorElement.Bind(null);

#if false // GML todo: how to draw an embedded editor?
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
#endif
            UpdateEditor(property);
            ux.TrackPropertyValue(property, (p) => UpdateEditor(p));

            return ux;
        }
    }
}
