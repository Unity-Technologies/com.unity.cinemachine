using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Cinemachine.Editor
{
    /// <summary>
    /// Helper for drawing embedded asset editors
    /// </summary>
    static class EmbeddedAssetEditorUtility
    {
        /// <summary>
        /// Called after the asset editor is created, in case it needs to be customized
        /// </summary>
        public delegate void OnCreateEditorDelegate(UnityEditor.Editor editor);

        static bool s_CustomBlendsExpanded;

        // This is to enable lambda capture of read/write variables
        class EmbeddedEditorContext
        {
            public UnityEditor.Editor Editor = null;
            public InspectorElement Inspector = null;
        }

        /// <summary>
        /// Call this to add the embedded inspector UX to an inspector.  
        /// Will draw the asset reference field, and the embedded editor, or a Create Asset button if no asset is set.
        /// </summary>
        public static VisualElement AddEmbeddedAssetInspector<T>(
            this UnityEditor.Editor owner, VisualElement ux,
            SerializedProperty property, OnCreateEditorDelegate onCreateEditor, 
            string saveAssetTitle, string defaultName, string extension, string saveAssetMessage) where T : ScriptableObject
        {
            // Asset field with create button
            var unassignedUx = ux.AddChild(new InspectorUtility.LeftRightContainer());
            unassignedUx.Left.Add(new Label(property.displayName) 
                { tooltip = property.tooltip, style = { alignSelf = Align.Center, flexGrow = 0 }});
            unassignedUx.Right.Add(new PropertyField(property, "") 
                { tooltip = property.tooltip, style = { alignSelf = Align.Center, flexGrow = 0, marginRight = 5 }});
            unassignedUx.Right.Add(new Button(() =>
            {
                string newAssetPath = EditorUtility.SaveFilePanelInProject(
                    saveAssetTitle, defaultName, extension, saveAssetMessage);
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
            var embeddedInspectorParent = new VisualElement();
            var assignedUx = ux.AddChild(new InspectorUtility.FoldoutWithOverlay(
                foldout, new PropertyField(property, ""), null) { style = { flexGrow = 1 }});
            foldout.Add(new PropertyField(property, "Asset"));
            foldout.AddSpace();

            Color borderColor = Color.grey;
            float borderWidth = 1;
            float borderRadius = 5;
            embeddedInspectorParent = foldout.AddChild(new VisualElement() { style = 
            { 
                borderTopColor = borderColor, borderTopWidth = borderWidth, borderTopLeftRadius = borderRadius,
                borderBottomColor = borderColor, borderBottomWidth = borderWidth, borderBottomLeftRadius = borderRadius,
                borderLeftColor = borderColor, borderLeftWidth = borderWidth, borderTopRightRadius = borderRadius,
                borderRightColor = borderColor, borderRightWidth = borderWidth, borderBottomRightRadius = borderRadius,
            }});

            embeddedInspectorParent.Add(new HelpBox(
                "This is a shared asset.  Changes made here will apply to all users of this asset.", 
                HelpBoxMessageType.Info));
    
            EmbeddedEditorContext context = new ();
            OnAssetChanged(property, context);
            ux.TrackPropertyValue(property, (p) => OnAssetChanged(p, context));
            embeddedInspectorParent.RegisterCallback<DetachFromPanelEvent>((e) => DestroyEditor(context));

            return ux;

            // Local function
            void OnAssetChanged(SerializedProperty property, EmbeddedEditorContext context)
            {
                property.serializedObject.ApplyModifiedProperties();

                var target = property.objectReferenceValue;
                if (context.Editor != null && context.Editor.target != target)
                {
                    context.Inspector?.RemoveFromHierarchy();
                    DestroyEditor(context);
                }
                if (target != null)
                {
                    if (context.Editor == null)
                    {
                        UnityEditor.Editor.CreateCachedEditor(target, null, ref context.Editor);
                        onCreateEditor?.Invoke(context.Editor);
                    }
                    if (embeddedInspectorParent != null)
                        context.Inspector = embeddedInspectorParent.AddChild(new InspectorElement(context.Editor));
                }
                if (unassignedUx != null)
                    unassignedUx.SetVisible(target == null);
                if (assignedUx != null)
                    assignedUx.SetVisible(target != null);
            }

            // Local function
            void DestroyEditor(EmbeddedEditorContext context)
            {
                if (context.Editor != null)
                {
                    Object.DestroyImmediate(context.Editor);
                    context.Editor = null;
                }
            }
        }
    }
}
