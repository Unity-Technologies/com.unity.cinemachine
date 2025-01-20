using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace Unity.Cinemachine.Editor
{
    // GML todo: remove this class, replace with EmbeddedAssetEditorUtility.AssetSelectorWithPresets
    // Currently only used by CinemachineImpulseDefinition editor

    [CustomPropertyDrawer(typeof(CinemachineEmbeddedAssetPropertyAttribute))]
    class EmbeddedAssetPropertyDrawer : PropertyDrawer
    {
        const float vSpace = 2;
        const float kIndentAmount = 15;
        const float kBoxMargin = 0; //3;
        float HelpBoxHeight => EditorGUIUtility.singleLineHeight * 2.5f;
        bool m_Expanded = false;

        bool WarnIfNull => attribute is CinemachineEmbeddedAssetPropertyAttribute attr && attr.WarnIfNull;

        float HeaderHeight => EditorGUIUtility.singleLineHeight * 1.5f; 
        float DrawHeader(Rect rect, string text)
        {
            float delta = HeaderHeight - EditorGUIUtility.singleLineHeight;
            rect.y += delta; rect.height -= delta;
            EditorGUI.LabelField(rect, new GUIContent(text), EditorStyles.boldLabel);
            return HeaderHeight;
        }

        string HeaderText(SerializedProperty property)
        {
            var attrs = property.serializedObject.targetObject.GetType()
                .GetCustomAttributes(typeof(HeaderAttribute), false);
            if (attrs != null && attrs.Length > 0)
                return ((HeaderAttribute)attrs[0]).header;
            return null;
        }

        bool AssetHasCustomEditor(SerializedProperty property)
        {
            ScriptableObject asset = property.objectReferenceValue as ScriptableObject;
            if (asset != null)
                return ActiveEditorTracker.HasCustomEditor(asset);
            return false;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            bool hasCustomEditor = AssetHasCustomEditor(property);
            float height = base.GetPropertyHeight(property, label);
            height += + 2 * (kBoxMargin + vSpace);
            if (m_Expanded && !hasCustomEditor)
            {
                height += HelpBoxHeight + kBoxMargin;
                ScriptableObject asset = property.objectReferenceValue as ScriptableObject;
                if (asset != null)
                {
                    SerializedObject so = new SerializedObject(asset);
                    var prop = so.GetIterator();
                    prop.NextVisible(true);
                    do
                    {
                        if (prop.name == "m_Script")
                            continue;
                        string header = HeaderText(prop);
                        if (header != null)
                            height += HeaderHeight + vSpace;
                        height += EditorGUI.GetPropertyHeight(prop, false) + vSpace;
                    }
                    while (prop.NextVisible(prop.isExpanded));
                    height += kBoxMargin;
                }
            }
            return height;
        }

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            bool hasCustomEditor = AssetHasCustomEditor(property);
            rect.y += vSpace; rect.height -= 2 * vSpace;
            //GUI.Box(rect, GUIContent.none, GUI.skin.box);
            rect.y += kBoxMargin;

            rect.height = EditorGUIUtility.singleLineHeight;
            EditorGUIUtility.labelWidth -= kBoxMargin;
            AssetFieldWithCreateButton(
                new Rect(rect.x + kBoxMargin, rect.y, rect.width - 2 * kBoxMargin, rect.height),
                property, WarnIfNull,
                property.serializedObject.targetObject.name + " " + label.text);

            ScriptableObject asset = property.objectReferenceValue as ScriptableObject;
            if (asset != null && !hasCustomEditor)
            {
                m_Expanded = EditorGUI.Foldout(rect, m_Expanded, GUIContent.none, true);
                if (m_Expanded)
                {
                    rect.y += rect.height + kBoxMargin + vSpace;
                    rect.x += kIndentAmount + kBoxMargin;
                    rect.width -= kIndentAmount + 2 * kBoxMargin;
                    EditorGUIUtility.labelWidth -= kIndentAmount;

                    EditorGUI.HelpBox(
                        new Rect(rect.x, rect.y, rect.width, HelpBoxHeight),
                        "This is a shared asset.\n"
                            + "Changes made here will apply to all users of this asset",
                        MessageType.Info);

                    rect.y += HelpBoxHeight + kBoxMargin;
                    SerializedObject so = new SerializedObject(property.objectReferenceValue);
                    var prop = so.GetIterator();
                    prop.NextVisible(true);

                    var indent = EditorGUI.indentLevel;
                    do
                    {
                        if (prop.name == "m_Script")
                            continue;
                        string header = HeaderText(prop);
                        if (header != null)
                        {
                            DrawHeader(rect, header);
                            rect.y += HeaderHeight + vSpace;
                            rect.height -= HeaderHeight + vSpace;
                        }
                        rect.height = EditorGUI.GetPropertyHeight(prop, false);
                        EditorGUI.indentLevel = indent + prop.depth;
                        EditorGUI.PropertyField(rect, prop);
                        rect.y += rect.height + vSpace;
                    } while (prop.NextVisible(prop.isExpanded));

                    if (GUI.changed)
                        so.ApplyModifiedProperties();
                }
                EditorGUIUtility.labelWidth += kIndentAmount;
            }
            EditorGUIUtility.labelWidth += kBoxMargin;
        }

        Type EmbeddedAssetType(SerializedProperty property)
        {
            Type type = property.serializedObject.targetObject.GetType();
            var a = property.propertyPath.Split('.');
            for (int i = 0; i < a.Length; ++i)
            {
                var field = type.GetField(a[i],
                    System.Reflection.BindingFlags.Public
                    | System.Reflection.BindingFlags.NonPublic
                    | System.Reflection.BindingFlags.Instance);
                if (field == null) continue;
                type = field.FieldType;
            }
            return type;
        }

        Type[] mAssetTypes = null;
        List<ScriptableObject> mAssetPresets;
        GUIContent[] mAssetPresetNames;

        void RebuildPresetList()
        {
            if (mAssetPresets != null && mAssetPresetNames != null)
                return;

            mAssetPresets = new List<ScriptableObject>();
            if (mAssetTypes != null)
            {
                for (int i = 0; i < mAssetTypes.Length; ++i)
                    InspectorUtility.AddAssetsFromPackageSubDirectory(
                        mAssetTypes[i], mAssetPresets, "Presets/Noise");
            }
            List<GUIContent> presetNameList = new ();
            for (int i = 0; i < mAssetPresets.Count; ++i)
                presetNameList.Add(new GUIContent("Presets/" + mAssetPresets[i].name));
            mAssetPresetNames = presetNameList.ToArray();
        }

        void AssetFieldWithCreateButton(
            Rect r, SerializedProperty property, bool warnIfNull, string defaultName)
        {
            // Collect all the eligible asset types
            Type type = EmbeddedAssetType(property);
            mAssetTypes ??= ReflectionHelpers.GetTypesDerivedFrom(type, 
                (t) => !t.IsAbstract && t.GetCustomAttribute<ObsoleteAttribute>() == null).ToArray();

            float iconSize = r.height + 4;
            r.width -= iconSize;

            GUIContent label = new GUIContent(property.displayName, property.tooltip);
            if (warnIfNull && property.objectReferenceValue == null)
                label.image = EditorGUIUtility.IconContent("console.warnicon.sml").image;
            EditorGUI.PropertyField(r, property, label);

            r.x += r.width; r.width = iconSize; r.height = iconSize;
            if (GUI.Button(r, EditorGUIUtility.IconContent("_Popup"), GUI.skin.label))
            {
                GenericMenu menu = new GenericMenu();
                if (property.objectReferenceValue != null)
                {
                    menu.AddItem(new GUIContent("Edit"), false, ()
                        => Selection.activeObject = property.objectReferenceValue);
                    menu.AddItem(new GUIContent("Clone"), false, () =>
                        {
                            ScriptableObject copyFrom = property.objectReferenceValue as ScriptableObject;
                            if (copyFrom != null)
                            {
                                string title = "Create New " + copyFrom.GetType().Name + " asset";
                                ScriptableObject asset = CreateAsset(
                                    copyFrom.GetType(), copyFrom, defaultName, title);
                                if (asset != null)
                                {
                                    property.objectReferenceValue = asset;
                                    property.serializedObject.ApplyModifiedProperties();
                                }
                            }
                        });
                    menu.AddItem(new GUIContent("Locate"), false, ()
                        => EditorGUIUtility.PingObject(property.objectReferenceValue));
                }

                RebuildPresetList();
                int index = 0;
                for (int i = 0; i < mAssetPresets.Count; ++i)
                {
                    var a = mAssetPresets[i];
                    menu.AddItem(mAssetPresetNames[index++], false, () =>
                        {
                            property.objectReferenceValue = a;
                            property.serializedObject.ApplyModifiedProperties();
                        });
                }

                for (int i = 0; i < mAssetTypes.Length; ++i)
                {
                    var t = mAssetTypes[i];
                    menu.AddItem(new GUIContent("New " + InspectorUtility.NicifyClassName(t)), false, () =>
                        {
                            string title = "Create New " + t.Name + " asset";
                            ScriptableObject asset = CreateAsset(t, null, defaultName, title);
                            if (asset != null)
                            {
                                property.objectReferenceValue = asset;
                                property.serializedObject.ApplyModifiedProperties();
                            }
                        });
                }
                menu.ShowAsContext();
            }
        }

        ScriptableObject CreateAsset(
            Type assetType, ScriptableObject copyFrom, string defaultName, string dialogTitle)
        {
            ScriptableObject asset = null;
            string path = EditorUtility.SaveFilePanelInProject(
                    dialogTitle, defaultName, "asset", string.Empty);
            if (!string.IsNullOrEmpty(path))
            {
                if (copyFrom != null)
                {
                    string fromPath = AssetDatabase.GetAssetPath(copyFrom);
                    if (AssetDatabase.CopyAsset(fromPath, path))
                        asset = AssetDatabase.LoadAssetAtPath(path, assetType) as ScriptableObject;
                }
                else
                {
                    asset = ScriptableObjectUtility.CreateAt(assetType, path);
                }
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            return asset;
        }
    }
}
