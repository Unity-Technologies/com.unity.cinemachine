using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Cinemachine.Utility;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Cinemachine.Editor
{
    /// <summary>
    /// Collection of tools and helpers for drawing inspectors
    /// </summary>
    static partial class InspectorUtility
    {
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
        /// Remove the "Cinemachine" prefix, then call the standard Unity Nicify,
        /// and add (Deprecated) to types with Obsolete attributes.
        /// </summary>
        /// <param name="type">The type to nicify as a string</param>
        /// <returns>The nicified name</returns>
        public static string NicifyClassName(Type type)
        {
            var name = type.Name;
            if (name.StartsWith("Cinemachine"))
                name = name.Substring(11); // Trim the prefix
            
            name = ObjectNames.NicifyVariableName(name);
            
            if (type.GetCustomAttribute<ObsoleteAttribute>() != null) 
                name += " (Deprecated)";

            return name;
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
        
        private static int m_lastRepaintFrame;

        /// <summary>
        /// Force a repaint of the Game View
        /// </summary>
        /// <param name="unused">Like it says</param>
        public static void RepaintGameView(UnityEngine.Object unused = null)
        {
            if (m_lastRepaintFrame == Time.frameCount)
                return;
            m_lastRepaintFrame = Time.frameCount;

            EditorApplication.QueuePlayerLoopUpdate();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        /// <summary>
        /// Try to get the name of the owning virtual camera object.  If none then use
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
        internal const string s_NoneString = "(none)";

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
                    s = s_NoneString;
                s_AssignableTypes[inputType] = s;
            }
            return s_AssignableTypes[inputType];
        }


        ///==============================================================================================
        ///==============================================================================================
        /// UI Elements utilities
        ///==============================================================================================
        ///==============================================================================================


        /// <summary>Aligns fields created by UI toolkit the unity inspector standard way.</summary>
        internal static string kAlignFieldClass => BaseField<bool>.alignedFieldUssClassName;

        // this is a hack to get around some vertical alignment issues in UITK
        internal static float SingleLineHeight => EditorGUIUtility.singleLineHeight - EditorGUIUtility.standardVerticalSpacing;

        /// <summary>
        /// Draw a bold header in the inspector - hack to get around missing UITK functionality
        /// </summary>
        /// <param name="ux">Container in which to put the header</param>
        /// <param name="text">The text of the header</param>
        /// <param name="tooltip">optional tooltip for the header</param>
        internal static void AddHeader(this VisualElement ux, string text, string tooltip = "")
        {
            var verticalPad = SingleLineHeight / 2;
            var row = new LabeledContainer($"<b>{text}</b>", tooltip, new VisualElement { style = { flexBasis = 0} });
            row.focusable = false;
            row.labelElement.style.flexGrow = 1;
            row.labelElement.style.paddingTop = verticalPad;
            row.labelElement.style.paddingBottom = EditorGUIUtility.standardVerticalSpacing;
            ux.Add(row);
        }

        /// <summary>
        /// Create a space between inspector sections
        /// </summary>
        /// <param name="ux">Container in which to add the space</param>
        internal static void AddSpace(this VisualElement ux)
        {
            ux.Add(new VisualElement { style = { height = SingleLineHeight / 2 }});
        }
        
        /// <summary>
        /// This is a hack to get proper layout.  There seems to be no sanctioned way to 
        /// get the current inspector label width.
        /// </summary>
        internal class LabeledContainer : BaseField<bool> // bool is just a dummy because it has to be something
        {
            public Label Label => labelElement;
            public VisualElement Input { get; }

            public LabeledContainer(string label, string tooltip = "") : this (label, tooltip, new VisualElement()) 
            {
                Input.style.flexDirection = FlexDirection.Row;
            }

            public LabeledContainer(string label, string tooltip, VisualElement input) : base(label, input)
            {
                Input = input;
                AddToClassList(alignedFieldUssClassName);
                this.tooltip = tooltip;
                Label.tooltip = tooltip;
            }

            public T AddInput<T>(T input) where T : VisualElement
            {
                Input.Add(input);
                return input;
            }
        }

        /// <summary>
        /// This is an inspector container with 2 side-by-side rows. The Left row's width is 
        /// locked to the inspector field label size, for proper alignment.
        /// </summary>
        internal class LeftRightContainer : VisualElement
        {
            public VisualElement Left;
            public VisualElement Right;

            public static float kLeftMarginHack = 3;

            /// <summary>
            /// Set this to offset the Left/Right division from the inspector's Label/Content line
            /// </summary>
            public float DivisionOffset = 0;

            public LeftRightContainer()
            {
                // This is to peek at the resolved label width
                var hack = AddChild(this,  new LabeledContainer(" ") { style = { height = 1, marginTop = -2 }});

                var row = AddChild(this, new VisualElement 
                    { style = { flexDirection = FlexDirection.Row }});
                Left = row.AddChild(new VisualElement 
                    { style = { flexDirection = FlexDirection.Row, flexGrow = 0, marginLeft = kLeftMarginHack }});
                Right = row.AddChild(new VisualElement 
                    { style = { flexDirection = FlexDirection.Row, flexGrow = 1 }});

                hack.Label.RegisterCallback<GeometryChangedEvent>(
                    (evt) => Left.style.width = hack.Label.resolvedStyle.width + DivisionOffset);
            }
        }

        /// <summary>A foldout that displays an overlay in the right-hand column when closed</summary>
        internal class FoldoutWithOverlay : VisualElement
        {
            public readonly Foldout OpenFoldout;
            public readonly Foldout ClosedFoldout;
            public readonly VisualElement Overlay;
            public readonly Label OverlayLabel;

            public FoldoutWithOverlay(Foldout foldout, VisualElement overlay, Label overlayLabel)
            {
                OpenFoldout = foldout;
                Overlay = overlay;
                OverlayLabel = overlayLabel;

                // There are 2 modes for this element: foldout closed and foldout open.
                // When closed, we cheat the layout system, and to implement this we do a switcheroo
                var closedContainer = AddChild(this, new LeftRightContainer() 
                    { style = { flexGrow = 1, marginLeft = -LeftRightContainer.kLeftMarginHack }});
                Add(foldout);

                var closedFoldout = new Foldout { text = foldout.text, tooltip = foldout.tooltip, value = false };
                ClosedFoldout = closedFoldout;
                ClosedFoldout = closedContainer.Left.AddChild(ClosedFoldout);
                if (overlayLabel != null)
                    closedContainer.Right.Add(overlayLabel);
                closedContainer.Right.Add(overlay);

                // Outdent the label
                if (overlayLabel != null)
                {
                    closedContainer.Right.RegisterCallback<GeometryChangedEvent>(
                        (evt) => closedContainer.Right.style.marginLeft = -overlayLabel.resolvedStyle.width);
                }

                // Swap the open and closed foldouts when the foldout is opened or closed
                closedContainer.SetVisible(!foldout.value);
                closedFoldout.RegisterValueChangedCallback((evt) =>
                {
                    if (evt.newValue)
                    {
                        closedContainer.SetVisible(false);
                        foldout.SetVisible(true);
                        foldout.value = true;
                        closedFoldout.SetValueWithoutNotify(false);
                        foldout.Focus(); // GML why doesn't this work?
                        evt.StopPropagation();
                    }
                });
                foldout.SetVisible(foldout.value);
                foldout.RegisterValueChangedCallback((evt) =>
                {
                    if (!evt.newValue)
                    {
                        closedContainer.SetVisible(true);
                        foldout.SetVisible(false);
                        closedFoldout.SetValueWithoutNotify(false);
                        foldout.value = false;
                        closedFoldout.Focus(); // GML why doesn't this work?
                        evt.StopPropagation();
                    }
                });
            }
        }

        internal class CompactPropertyField : VisualElement
        {
            public Label Label;
            public PropertyField Field;

            public CompactPropertyField(SerializedProperty property) : this(property, property.displayName) {}

            public CompactPropertyField(SerializedProperty property, string label, float minLabelWidth = 0)
            {
                style.flexDirection = FlexDirection.Row;
                if (label.Length != 0)
                    Label = AddChild(this, new Label(label) 
                        { tooltip = property.tooltip, style = { alignSelf = Align.Center, minWidth = minLabelWidth }});
                Field = AddChild(this, new PropertyField(property, "") { style = { flexGrow = 1, flexBasis = 0 } });
                if (Label != null)
                    Label.AddPropertyDragger(property, Field);
            }
        }

        internal static void AddPropertyDragger(this Label label, SerializedProperty p, VisualElement field)
        {
            if (p.propertyType == SerializedPropertyType.Float 
                || p.propertyType == SerializedPropertyType.Integer)
            {
                label.RegisterCallback<GeometryChangedEvent>(AddDragger);
                label.AddToClassList("unity-base-field__label--with-dragger");
            }

            void AddDragger(GeometryChangedEvent evt) 
            {
                label.UnregisterCallback<GeometryChangedEvent>(AddDragger);

                if (p.propertyType == SerializedPropertyType.Float)
                    new FieldMouseDragger<float>(field.Q<FloatField>()).SetDragZone(label);
                else if (p.propertyType == SerializedPropertyType.Integer)
                    new FieldMouseDragger<int>(field.Q<IntegerField>()).SetDragZone(label);
            }
        }

        internal static LeftRightContainer CreatePropertyRow(
            SerializedProperty property, out VisualElement propertyField)
        {
            var row = new LeftRightContainer();

            var label = row.Left.AddChild(new Label(property.displayName) 
                { tooltip = property.tooltip, style = { alignSelf = Align.Center, flexGrow = 1 }});
            propertyField = row.Right.AddChild(new PropertyField(property, "") 
                { tooltip = property.tooltip, style = { flexGrow = 1, flexBasis = 0 }});
            AddPropertyDragger(label, property, propertyField);

            return row;
        }

        internal static VisualElement CreateHelpBoxWithButton(
            string message, HelpBoxMessageType messageType, 
            string buttonText, Action onClicked)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row }};
            row.Add(new HelpBox(message, messageType) { style = { flexGrow = 1 }});
            row.Add(new Button(onClicked) { text = buttonText, style = { marginLeft = 0 }});
            return row;
        }

        internal static void SetVisible(this VisualElement e, bool show) 
            => e.style.display = show ? StyleKeyword.Null : DisplayStyle.None;

        internal static bool IsVisible(this VisualElement e) => e.style.display != DisplayStyle.None;

        internal static T AddChild<T>(this VisualElement e, T child) where T : VisualElement
        {
            e.Add(child);
            return child;
        }
    }
}
