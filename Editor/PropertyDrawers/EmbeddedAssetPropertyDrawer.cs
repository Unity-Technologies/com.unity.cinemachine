using UnityEngine;
using UnityEditor;
using System;
using Cinemachine.Utility;
using System.Linq;

namespace Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(CinemachineEmbeddedAssetPropertyAttribute))]
    internal sealed class EmbeddedAssetPropertyDrawer : PropertyDrawer
    {
        const float vSpace = 2;
        const float kIndentAmount = 15;
        const float kBoxMargin = 3;
        float HelpBoxHeight { get { return EditorGUIUtility.singleLineHeight * 2.5f; } }
        bool mExpanded = false;

        bool WarnIfNull 
        {
            get
            {
                var attr = attribute as CinemachineEmbeddedAssetPropertyAttribute;
                return attr == null ? false : attr.WarnIfNull;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = base.GetPropertyHeight(property, label) + 2 * (kBoxMargin + vSpace);
            if (mExpanded)
            {
                height += HelpBoxHeight + kBoxMargin;
                ScriptableObject asset = property.objectReferenceValue as ScriptableObject;
                if (asset != null)
                {
                    SerializedObject so = new SerializedObject(asset);
                    var prop = so.GetIterator();
                    prop.NextVisible(true);
                    while (prop.NextVisible(prop.isExpanded))
                        height += EditorGUI.GetPropertyHeight(prop, label, prop.isExpanded) + vSpace;
                    height += kBoxMargin;
                }
            }
            return height;
        }

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            rect.y += vSpace; rect.height -= 2 * vSpace;
            GUI.Box(rect, GUIContent.none, GUI.skin.box);
            rect.y += kBoxMargin;

            rect.height = EditorGUIUtility.singleLineHeight;
            EditorGUIUtility.labelWidth -= kBoxMargin;
            AssetFieldWithCreateButton(
                new Rect(rect.x + kBoxMargin, rect.y, rect.width - 2 * kBoxMargin, rect.height),
                property, WarnIfNull,
                property.serializedObject.targetObject.name + " " + label.text);

            ScriptableObject asset = property.objectReferenceValue as ScriptableObject;
            if (asset != null)
            {
                mExpanded = EditorGUI.Foldout(rect, mExpanded, GUIContent.none);
                if (mExpanded)
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
                    while (prop.NextVisible(prop.isExpanded))
                    {
                        rect.height = EditorGUI.GetPropertyHeight(prop, label, prop.isExpanded);
                        EditorGUI.indentLevel = indent + prop.depth;
                        EditorGUI.PropertyField(rect, prop);
                        rect.y += rect.height + vSpace;
                    }

                    if (GUI.changed)
                        so.ApplyModifiedProperties();
                }
                EditorGUIUtility.labelWidth += kIndentAmount;
            }
            EditorGUIUtility.labelWidth += kBoxMargin;
        }


        Type[] mAssetTypes = null;

        static Type EmbeddedAssetType(SerializedProperty property)
        {
            return property.serializedObject.targetObject.GetType().GetField(property.propertyPath).FieldType;
        }

        void AssetFieldWithCreateButton(
            Rect r, SerializedProperty property, bool warnIfNull, string defaultName)
        {
            // Collect all the eligible asset types
            if (mAssetTypes == null)
                mAssetTypes = ReflectionHelpers.GetTypesInAllLoadedAssemblies(
                    (Type t) => t.IsSubclassOf(EmbeddedAssetType(property))).ToArray();

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
                            ScriptableObject asset = property.objectReferenceValue as ScriptableObject;
                            string fromPath = AssetDatabase.GetAssetPath(asset);
                            string toPath = AssetDatabase.GenerateUniqueAssetPath(fromPath);
                            if (AssetDatabase.CopyAsset(fromPath, toPath))
                            {
                                asset = AssetDatabase.LoadAssetAtPath(
                                    toPath, EmbeddedAssetType(property)) as ScriptableObject;
                                AssetDatabase.SaveAssets();
                                AssetDatabase.Refresh();
                                property.objectReferenceValue = asset;
                                property.serializedObject.ApplyModifiedProperties();
                            }
                        });
                    menu.AddItem(new GUIContent("Locate"), false, () 
                        => EditorGUIUtility.PingObject(property.objectReferenceValue));
                }

                foreach (var t in mAssetTypes)
                {
                    menu.AddItem(new GUIContent("New " + InspectorUtility.NicifyClassName(t.Name)), false, () => 
                        { 
                            string title = "Create New " + t.Name + " asset";
                            ScriptableObject asset = CreateAsset(t, defaultName, title);
                            AssetDatabase.SaveAssets();
                            AssetDatabase.Refresh();
                            property.objectReferenceValue = asset;
                            property.serializedObject.ApplyModifiedProperties();
                        });
                }
                menu.ShowAsContext();
            }
        }

        ScriptableObject CreateAsset(Type assetType, string defaultName, string dialogTitle)
        {
            ScriptableObject asset = null;
            string newAssetPath = EditorUtility.SaveFilePanelInProject(
                    dialogTitle, defaultName, "asset", string.Empty);
            if (!string.IsNullOrEmpty(newAssetPath))
            {
                asset = ScriptableObjectUtility.CreateAt(assetType, newAssetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            return asset;
        }
    }
}
