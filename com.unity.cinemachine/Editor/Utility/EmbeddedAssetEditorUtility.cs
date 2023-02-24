using System;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Object = UnityEngine.Object;

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
        static public VisualElement AddEmbeddedAssetInspector<T>(
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

        /// <summary>
        /// Add an asset selector widget with a presets popup.
        /// </summary>
        static public void AddAssetSelectorWithPresets<T>(
            this UnityEditor.Editor owner, VisualElement ux, SerializedProperty property, 
            string presetsPath, string warningTextIfNull) where T : ScriptableObject
        {
            var row = ux.AddChild(new InspectorUtility.LabeledContainer(property.displayName) { tooltip = property.tooltip });
            var contents = row.Input;

            Label warningIcon = null;
            if (!string.IsNullOrEmpty(warningTextIfNull))
            {
                warningIcon = contents.AddChild(new Label 
                { 
                    tooltip = warningTextIfNull,
                    style = 
                    { 
                        backgroundImage = (StyleBackground)EditorGUIUtility.IconContent("console.warnicon.sml").image,
                        width = InspectorUtility.SingleLineHeight, height = InspectorUtility.SingleLineHeight,
                        alignSelf = Align.Center
                    }
                });
            }

            var presetName = contents.AddChild(new TextField 
            { 
                isReadOnly = true,
                tooltip = property.tooltip, 
                style = { alignSelf = Align.Center, flexBasis = 40, flexGrow = 1 }
            });
            presetName.SetEnabled(false);

            var selector = contents.AddChild(new PropertyField(property, "") { style = { flexGrow = 1 }});
            selector.RemoveFromClassList(InspectorUtility.kAlignFieldClass);

            var button = contents.AddChild(new Button { style = 
            { 
                backgroundImage = (StyleBackground)EditorGUIUtility.IconContent("_Popup").image,
                width = InspectorUtility.SingleLineHeight, height = InspectorUtility.SingleLineHeight,
                alignSelf = Align.Center,
                paddingRight = 0, borderRightWidth = 0, marginRight = 0
            }});

            var defaultName = property.serializedObject.targetObject.name + " " + property.displayName;
            var assetTypes = GetAssetTypes(typeof(T));
            var presetAssets = GetPresets(assetTypes, presetsPath, out var presetNames);

            var manipulator = new ContextualMenuManipulator((evt) => 
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
                int i = 0;
                foreach (var a in presetAssets)
                {
                    evt.menu.AppendAction(presetNames[i++], 
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
                foreach (var t in assetTypes)
                {
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
            });
            manipulator.activators.Clear();
            manipulator.activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
            button.AddManipulator(manipulator);

            UpdateUX(property);
            row.TrackPropertyValue(property, UpdateUX);
            void UpdateUX(SerializedProperty p)
            {
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
            }

            // Local function
            static List<Type> GetAssetTypes(Type baseType)
            {
                // GML todo: optimize with TypeCache
                return Utility.ReflectionHelpers.GetTypesInAllDependentAssemblies(
                    (Type t) => baseType.IsAssignableFrom(t) && !t.IsAbstract 
                        && t.GetCustomAttribute<ObsoleteAttribute>() == null).ToList();
            }

            // Local function
            static List<ScriptableObject> GetPresets(
                List<Type> assetTypes, string presetPath, out List<string> presetNames)
            {
                presetNames = new List<string>();
                var presetAssets = new List<ScriptableObject>();

                if (!string.IsNullOrEmpty(presetPath))
                {
                    foreach (var t in assetTypes)
                        InspectorUtility.AddAssetsFromPackageSubDirectory(t, presetAssets, presetPath);
                    foreach (var n in presetAssets)
                        presetNames.Add("Presets/" + n.name);
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
