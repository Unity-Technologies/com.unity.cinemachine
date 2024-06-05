using System;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Collections.Generic;
using System.Reflection;
using Object = UnityEngine.Object;

namespace Unity.Cinemachine.Editor
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
        /// Call this to create an embedded inspector.  
        /// Will draw the asset reference field, and the embedded editor, or a Create 
        /// Asset button if no asset is set.
        /// </summary>
        public static VisualElement EmbeddedAssetInspector<T>(
            SerializedProperty property, OnCreateEditorDelegate onCreateEditor) where T : ScriptableObject
        {
            var ux = new VisualElement();

            // Asset field with create button
            var unassignedUx = ux.AddChild(AssetSelectorWithPresets<T>(property));

            var foldout = new Foldout() { text = property.displayName, tooltip = property.tooltip, value = s_CustomBlendsExpanded };
            foldout.RegisterValueChangedCallback((evt) => 
            {
                if (evt.target == foldout)
                {
                    s_CustomBlendsExpanded = evt.newValue;
                    evt.StopPropagation();
                }
            });
            var assignedUx = ux.AddChild(new InspectorUtility.FoldoutWithOverlay(
                foldout, AssetSelectorWithPresets<T>(property, ""), null) { style = { flexGrow = 1 }});
            foldout.Add(AssetSelectorWithPresets<T>(property, "Asset"));
            foldout.AddSpace();

            var borderColor = Color.grey;
            const float borderWidth = 1f;
            const float borderRadius = 5f;
            var embeddedInspectorParent = foldout.AddChild(new VisualElement() { style = 
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
            void OnAssetChanged(SerializedProperty sProp, EmbeddedEditorContext eContext)
            {
                if (sProp.serializedObject == null)
                    return; // object deleted
                sProp.serializedObject.ApplyModifiedProperties();

                var target = sProp.objectReferenceValue;
                if (eContext.Editor != null && eContext.Editor.target != target)
                {
                    eContext.Inspector?.RemoveFromHierarchy();
                    DestroyEditor(eContext);
                }
                if (target != null)
                {
                    if (eContext.Editor == null)
                    {
                        UnityEditor.Editor.CreateCachedEditor(target, null, ref eContext.Editor);
                        onCreateEditor?.Invoke(eContext.Editor);
                    }
                    if (embeddedInspectorParent != null)
                        eContext.Inspector = embeddedInspectorParent.AddChild(new InspectorElement(eContext.Editor));
                }
                unassignedUx?.SetVisible(target == null);
                if (assignedUx != null)
                    assignedUx.SetVisible(target != null);
            }

            // Local function
            void DestroyEditor(EmbeddedEditorContext eContext)
            {
                if (eContext.Editor != null)
                {
                    Object.DestroyImmediate(eContext.Editor);
                    eContext.Editor = null;
                }
            }
        }

        /// <summary>
        /// Create an asset selector widget with a presets popup.
        /// </summary>
        public static VisualElement AssetSelectorWithPresets<T>(
            SerializedProperty property, string label = null, 
            string presetsPath = null, string warningTextIfNull = null) where T : ScriptableObject
        {
            VisualElement ux;
            VisualElement contents;
            if (label == string.Empty)
            {
                contents = new VisualElement { style = { flexGrow = 1, flexDirection = FlexDirection.Row }};
                ux = contents;
            }
            else 
            {
                var row = new InspectorUtility.LabeledRow(label ?? property.displayName, property.tooltip);
                contents = row.Contents;
                ux = row;
            }

            var selector = contents.AddChild(new PropertyField(property, "") { style = { flexGrow = 1 }});

            Label warningIcon = null;
            if (!string.IsNullOrEmpty(warningTextIfNull))
            {
                warningIcon = InspectorUtility.MiniHelpIcon(warningTextIfNull);
                contents.Insert(0, warningIcon);
            }

            var presetName = contents.AddChild(new TextField 
            { 
                isReadOnly = true,
                tooltip = property.tooltip, 
                style = { alignSelf = Align.Center, flexBasis = 40, flexGrow = 1, marginLeft = 0 }
            });

            var defaultName = property.serializedObject.targetObject.name + " " + property.displayName;
            var assetTypes = GetAssetTypes(typeof(T));
            var presetAssets = GetPresets(assetTypes, presetsPath, out var presetNames);

            contents.Add(InspectorUtility.MiniPopupButton(null, new ContextualMenuManipulator((evt) => 
            {
                evt.menu.AppendAction("Clear", 
                    (action) => 
                    {
                        property.objectReferenceValue = null;
                        property.serializedObject.ApplyModifiedProperties();
                    }, 
                    (status) => 
                    {
                        var copyFrom = property.objectReferenceValue as ScriptableObject;
                        return copyFrom == null ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal;
                    }
                );
                for (int i = 0; i < presetAssets.Count; ++i)
                {
                    var a = presetAssets[i];
                    evt.menu.AppendAction(presetNames[i], 
                        (action) => 
                        {
                            property.objectReferenceValue = a;
                            property.serializedObject.ApplyModifiedProperties();
                        }
                    );
                }
                evt.menu.AppendAction("Clone", 
                    (action) => 
                    {
                        var copyFrom = property.objectReferenceValue as ScriptableObject;
                        if (copyFrom != null)
                        {
                            string title = "Create New " + copyFrom.GetType().Name + " asset";
                            var asset = CreateAsset(copyFrom.GetType(), copyFrom, defaultName, title);
                            if (asset != null)
                            {
                                property.objectReferenceValue = asset;
                                property.serializedObject.ApplyModifiedProperties();
                            }
                        }
                    }, 
                    (status) => 
                    {
                        var copyFrom = property.objectReferenceValue as ScriptableObject;
                        return copyFrom == null ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal;
                    }
                );
                for (int i = 0; i < assetTypes.Count; ++i)
                {
                    var t = assetTypes[i];
                    evt.menu.AppendAction("New " + InspectorUtility.NicifyClassName(t), 
                        (action) => 
                        {
                            var asset = CreateAsset(t, null, defaultName, "Create New " + t.Name + " asset");
                            if (asset != null)
                            {
                                property.objectReferenceValue = asset;
                                property.serializedObject.ApplyModifiedProperties();
                            }
                        }
                    );  
                }
            })));

            ux.TrackPropertyWithInitialCallback(property, (p) =>
            {
                if (p.serializedObject == null)
                    return; // object deleted
                var target = p.objectReferenceValue as ScriptableObject;
                warningIcon?.SetVisible(target == null);

                // Is it a preset?
                int presetIndex;
                for (presetIndex = presetAssets.Count - 1; presetIndex >= 0; --presetIndex)
                    if (target == presetAssets[presetIndex])
                        break;

                if (presetIndex >= 0)
                    presetName.value = presetNames[presetIndex];
                presetName.SetVisible(presetIndex >= 0);
                selector.SetVisible(presetIndex < 0);
            });

            return ux;

            // Local function
            static List<Type> GetAssetTypes(Type baseType)
            {
                var allTypes = ReflectionHelpers.GetTypesDerivedFrom(baseType,
                    (t) => !t.IsAbstract && t.GetCustomAttribute<ObsoleteAttribute>() == null);
                var list = new List<Type>();
                var iter = allTypes.GetEnumerator();
                while (iter.MoveNext())
                    list.Add(iter.Current);
                return list;
            }

            // Local function
            static List<ScriptableObject> GetPresets(
                List<Type> assetTypes, string presetPath, out List<string> presetNames)
            {
                presetNames = new List<string>();
                var presetAssets = new List<ScriptableObject>();

                if (!string.IsNullOrEmpty(presetPath))
                {
                    for (int i = 0; i < assetTypes.Count; ++i)
                        InspectorUtility.AddAssetsFromPackageSubDirectory(assetTypes[i], presetAssets, presetPath);
                    for (int i = 0; i < presetAssets.Count; ++i)
                        presetNames.Add("Presets/" + presetAssets[i].name);
                }
                return presetAssets;
            }

            // Local function
            static ScriptableObject CreateAsset(
                Type assetType, ScriptableObject copyFrom, string defaultName, string dialogTitle)
            {
                ScriptableObject asset = null;
                string path = EditorUtility.SaveFilePanelInProject(
                        dialogTitle, defaultName, "asset", string.Empty);
                if (!string.IsNullOrEmpty(path))
                {
                    if (copyFrom == null)
                        asset = ScriptableObjectUtility.CreateAt(assetType, path);
                    else
                    {
                        string fromPath = AssetDatabase.GetAssetPath(copyFrom);
                        if (AssetDatabase.CopyAsset(fromPath, path))
                            asset = AssetDatabase.LoadAssetAtPath(path, assetType) as ScriptableObject;
                    }
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
                return asset;
            }
        }
    }
}
