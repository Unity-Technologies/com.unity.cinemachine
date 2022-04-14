using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using Cinemachine.Utility;

namespace Cinemachine.Editor
{
    /// <summary>
    /// Collection of tools and helpers for drawing inspectors
    /// </summary>
    public class InspectorUtility
    {
        /// <summary>
        /// Aligns fields create by UI toolkit the unity standard way.
        /// </summary>
        internal const string alignFieldClass = "unity-base-field__aligned";
        
        /// <summary>Put multiple properties on a single inspector line, with
        /// optional label overrides.  Passing null as a label (or sublabel) override will
        /// cause the property's displayName to be used as a label.  For no label at all,
        /// pass GUIContent.none.</summary>
        /// <param name="rect">Rect in which to draw</param>
        /// <param name="label">Main label</param>
        /// <param name="props">Properties to place on the line</param>
        /// <param name="subLabels">Sublabels for the properties</param>
        public static void MultiPropertyOnLine(
            Rect rect,
            GUIContent label,
            SerializedProperty[] props, GUIContent[] subLabels)
        {
            if (props == null || props.Length == 0)
                return;

            const int hSpace = 2;
            int indentLevel = EditorGUI.indentLevel;
            float labelWidth = EditorGUIUtility.labelWidth;

            float totalSubLabelWidth = 0;
            int numBoolColumns = 0;
            List<GUIContent> actualLabels = new List<GUIContent>();
            for (int i = 0; i < props.Length; ++i)
            {
                GUIContent sublabel = new GUIContent(props[i].displayName, props[i].tooltip);
                if (subLabels != null && subLabels.Length > i && subLabels[i] != null)
                    sublabel = subLabels[i];
                actualLabels.Add(sublabel);
                totalSubLabelWidth += GUI.skin.label.CalcSize(sublabel).x;
                if (i > 0)
                    totalSubLabelWidth += hSpace;
                // Special handling for toggles, or it looks stupid
                if (props[i].propertyType == SerializedPropertyType.Boolean)
                {
                    totalSubLabelWidth += rect.height + hSpace;
                    ++numBoolColumns;
                }
            }

            float subFieldWidth = rect.width - labelWidth - totalSubLabelWidth;
            float numCols = props.Length - numBoolColumns;
            float colWidth = numCols == 0 ? 0 : subFieldWidth / numCols;

            // Main label.  If no first sublabel, then main label must take on that
            // role, for mouse dragging value-scrolling support
            int subfieldStartIndex = 0;
            if (label == null)
                label = new GUIContent(props[0].displayName, props[0].tooltip);
            if (actualLabels[0] != GUIContent.none)
                rect = EditorGUI.PrefixLabel(rect, label);
            else
            {
                rect.width = labelWidth + colWidth;
                EditorGUI.PropertyField(rect, props[0], label);
                rect.x += rect.width + hSpace;
                subfieldStartIndex = 1;
            }

            for (int i = subfieldStartIndex; i < props.Length; ++i)
            {
                EditorGUI.indentLevel = 0;
                EditorGUIUtility.labelWidth = GUI.skin.label.CalcSize(actualLabels[i]).x;
                if (props[i].propertyType == SerializedPropertyType.Boolean)
                {
                    rect.x += hSpace;
                    rect.width = EditorGUIUtility.labelWidth + rect.height;
                    EditorGUI.BeginProperty(rect, actualLabels[i], props[i]);
                    props[i].boolValue = EditorGUI.ToggleLeft(rect, actualLabels[i], props[i].boolValue);
                }
                else
                {
                    rect.width = EditorGUIUtility.labelWidth + colWidth;
                    EditorGUI.BeginProperty(rect, actualLabels[i], props[i]);
                    EditorGUI.PropertyField(rect, props[i], actualLabels[i]);
                }
                EditorGUI.EndProperty();
                rect.x += rect.width + hSpace;
            }

            EditorGUIUtility.labelWidth = labelWidth;
            EditorGUI.indentLevel = indentLevel;
        }

        /// <summary>
        /// Normalize a curve so that each of X and Y axes ranges from 0 to 1
        /// </summary>
        /// <param name="curve">Curve to normalize</param>
        /// <returns>The normalized curve</returns>
        public static AnimationCurve NormalizeCurve(AnimationCurve curve)
        {
            return RuntimeUtility.NormalizeCurve(curve, true, true);
        }

        /// <summary>
        /// Remove the "Cinemachine" prefix, then call the standard Unity Nicify.
        /// </summary>
        /// <param name="name">The name to nicify</param>
        /// <returns>The nicified name</returns>
        public static string NicifyClassName(string name)
        {
            if (name.StartsWith("Cinemachine"))
                name = name.Substring(11); // Trim the prefix
            return ObjectNames.NicifyVariableName(name);
        }

        /// <summary>
        /// Add to a list all assets of a given type found in a given location
        /// </summary>
        /// <param name="type">The asset type to look for</param>
        /// <param name="assets">The list to add found assets to</param>
        /// <param name="path">The location in which to look.  Path is relative to package root.</param>
        public static void AddAssetsFromPackageSubDirectory(
            Type type, List<ScriptableObject> assets, string path)
        {
            try
            {
                path = "/" + path;
                var info = new DirectoryInfo(ScriptableObjectUtility.CinemachineInstallPath + path);
                path = ScriptableObjectUtility.kPackageRoot + path + "/";
                var fileInfo = info.GetFiles();
                foreach (var file in fileInfo)
                {
                    if (file.Extension != ".asset")
                        continue;
                    string name = path + file.Name;
                    ScriptableObject a = AssetDatabase.LoadAssetAtPath(name, type) as ScriptableObject;
                    if (a != null)
                        assets.Add(a);
                }
            }
            catch
            {
            }
        }

        // Temporarily here
        /// <summary>
        /// Creates a new GameObject.
        /// </summary>
        /// <param name="name">Name to give the object.</param>
        /// <param name="types">Optional components to add.</param>
        /// <returns>The GameObject that was created.</returns>
        [Obsolete("Use ObjectFactory.CreateGameObject(string name, params Type[] types) instead.")]
        public static GameObject CreateGameObject(string name, params Type[] types)
        {
            return ObjectFactory.CreateGameObject(name, types);
        }

        /// <summary>
        /// Force a repaint of the Game View
        /// </summary>
        /// <param name="unused">Like it says</param>
        public static void RepaintGameView(UnityEngine.Object unused = null)
        {
            EditorApplication.QueuePlayerLoopUpdate();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        /// <summary>
        /// Try to get the name of the owning virtual camera oibject.  If none then use
        /// the object's name
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        // GML TODO: get rid of this
        internal static string GetVirtualCameraObjectName(SerializedProperty property)
        {
            // A little hacky here, as we favour virtual cameras...
            var obj = property.serializedObject.targetObject;
            GameObject go = obj as GameObject;
            if (go == null)
            {
                var component = obj as Component;
                if (component != null)
                    go = component.gameObject;
            }
            if (go != null)
            {
                var vcam = go.GetComponentInParent<CinemachineVirtualCameraBase>();
                if (vcam != null)
                    return vcam.Name;
                return go.name;
            }
            return obj.name;
        }

        internal static float PropertyHeightOfChidren(SerializedProperty property)
        {
            float height = 0;
            var childProperty = property.Copy();
            var endProperty = childProperty.GetEndProperty();
            childProperty.NextVisible(true);
            while (!SerializedProperty.EqualContents(childProperty, endProperty))
            {
                height += EditorGUI.GetPropertyHeight(childProperty)
                    + EditorGUIUtility.standardVerticalSpacing;
                childProperty.NextVisible(false);
            }
            return height - EditorGUIUtility.standardVerticalSpacing;
        }

        internal static void DrawChildProperties(Rect position, SerializedProperty property)
        {
            var childProperty = property.Copy();
            var endProperty = childProperty.GetEndProperty();
            childProperty.NextVisible(true);
            while (!SerializedProperty.EqualContents(childProperty, endProperty))
            {
                position.height = EditorGUI.GetPropertyHeight(childProperty);
                EditorGUI.PropertyField(position, childProperty, true);
                position.y += position.height + EditorGUIUtility.standardVerticalSpacing;
                childProperty.NextVisible(false);
            }
        }

        internal static void HelpBoxWithButton(
            string message, MessageType messageType, 
            GUIContent buttonContent, Action onClicked)
        {
            float verticalPadding = 3 * EditorGUIUtility.standardVerticalSpacing;
            float lineHeight = EditorGUIUtility.singleLineHeight;
            var buttonSize = GUI.skin.label.CalcSize(buttonContent);
            buttonSize.x += + lineHeight;

            var rect = EditorGUILayout.GetControlRect(false, verticalPadding);
            rect = EditorGUI.IndentedRect(rect);

            var boxContent = new GUIContent(message);
            var boxWidth = rect.width - buttonSize.x;
            var boxHeight = GUI.skin.GetStyle("helpbox").CalcHeight(boxContent, boxWidth - 3 * lineHeight) + verticalPadding;

            var height = Mathf.Max(Mathf.Max(buttonSize.y, lineHeight * 1.5f), boxHeight);
            rect = EditorGUILayout.GetControlRect(false, height + verticalPadding);
            rect = EditorGUI.IndentedRect(rect);
            rect.width = boxWidth; rect.height = height;
            EditorGUI.HelpBox(rect, message, messageType);
            rect.x += rect.width; rect.width = buttonSize.x;
            if (GUI.Button(rect, buttonContent))
                onClicked();
        }

        internal static float EnabledFoldoutHeight(SerializedProperty property, string enabledPropertyName)
        {
            var enabledProp = property.FindPropertyRelative(enabledPropertyName);
            if (enabledProp == null)
                return EditorGUI.GetPropertyHeight(property);
            if (!enabledProp.boolValue)
                return EditorGUIUtility.singleLineHeight;
            return PropertyHeightOfChidren(property);
        }

        internal static bool EnabledFoldout(
            Rect rect, SerializedProperty property, string enabledPropertyName,
            GUIContent label = null)
        {
            var enabledProp = property.FindPropertyRelative(enabledPropertyName);
            if (enabledProp == null)
            {
                EditorGUI.PropertyField(rect, property, true);
                rect.x += EditorGUIUtility.labelWidth;
                EditorGUI.LabelField(rect, new GUIContent($"unknown field `{enabledPropertyName}`"));
                return property.isExpanded;
            }
            rect.height = EditorGUIUtility.singleLineHeight;
            if (label == null)
                label = new GUIContent(property.displayName, enabledProp.tooltip);
            EditorGUI.PropertyField(rect, enabledProp, label);
            rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            if (enabledProp.boolValue)
            {
                ++EditorGUI.indentLevel;
                var childProperty = property.Copy();
                var endProperty = childProperty.GetEndProperty();
                childProperty.NextVisible(true);
                while (!SerializedProperty.EqualContents(childProperty, endProperty))
                {
                    if (!SerializedProperty.EqualContents(childProperty, enabledProp))
                    {
                        rect.height = EditorGUI.GetPropertyHeight(childProperty);
                        EditorGUI.PropertyField(rect, childProperty, true);
                        rect.y += rect.height + EditorGUIUtility.standardVerticalSpacing;
                    }
                    childProperty.NextVisible(false);
                }
                --EditorGUI.indentLevel;
            }
            return enabledProp.boolValue;
        }

        static Dictionary<Type, string> s_AssignableTypes = new Dictionary<Type, string>();
        internal static string GetAssignableBehaviourNames(Type inputType)
        {
            if (!s_AssignableTypes.ContainsKey(inputType))
            {
                var allSources = ReflectionHelpers.GetTypesInAllDependentAssemblies(
                    (Type t) => inputType.IsAssignableFrom(t) && !t.IsAbstract 
                        && typeof(MonoBehaviour).IsAssignableFrom(t));
                var s = string.Empty;
                foreach (var t in allSources)
                {
                    var sep = (s.Length == 0) ? string.Empty : ", ";
                    s += sep + t.Name;
                }
                if (s.Length == 0)
                    s = "(none)";
                s_AssignableTypes[inputType] = s;
            }
            return s_AssignableTypes[inputType];
        }

    }
}
