﻿using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Linq.Expressions;

namespace Unity.Cinemachine.Editor
{
    /// <summary>
    /// Collection of tools and helpers for drawing inspectors
    /// </summary>
    [InitializeOnLoad]
    static partial class InspectorUtility
    {
        /// <summary>
        /// Callback that happens whenever something undoable happens, either with 
        /// objects or with selection.  This is a good way to track user activity.
        /// </summary>
        public static EditorApplication.CallbackFunction UserDidSomething;

        static InspectorUtility()
        {
            ObjectChangeEvents.changesPublished -= OnUserDidSomethingStream;
            ObjectChangeEvents.changesPublished += OnUserDidSomethingStream;
            Selection.selectionChanged -= OnUserDidSomething;
            Selection.selectionChanged += OnUserDidSomething;

            static void OnUserDidSomething() => UserDidSomething?.Invoke();
            static void OnUserDidSomethingStream(ref ObjectChangeEventStream stream) => UserDidSomething?.Invoke();
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
                path = CinemachineCore.kPackageRoot + "/" + path;
                var info = new DirectoryInfo(path);
                path += "/";
                var fileInfo = info.GetFiles();
                for (int i = 0; i < fileInfo.Length; ++i)
                {
                    var file = fileInfo[i];
                    if (file.Extension != ".asset")
                        continue;
                    var name = path + file.Name;
                    var a = AssetDatabase.LoadAssetAtPath(name, type) as ScriptableObject;
                    if (a != null)
                        assets.Add(a);
                }
            }
            catch
            {
            }
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
            var name = NicifyClassName(type.Name);
            if (type.GetCustomAttribute<ObsoleteAttribute>() != null) 
                name += " (Deprecated)";
            return name;
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

        static Dictionary<Type, string> s_AssignableTypes = new ();
        public const string s_NoneString = "(none)";

        public static string GetAssignableBehaviourNames(Type inputType)
        {
            if (inputType == null)
                return "(none)";
            if (!s_AssignableTypes.ContainsKey(inputType))
            {
                var allSources = ReflectionHelpers.GetTypesDerivedFrom(inputType,
                    (t) => !t.IsAbstract && typeof(MonoBehaviour).IsAssignableFrom(t)
                        && t.GetCustomAttribute<ObsoleteAttribute>() == null);
                var s = string.Empty;
                var iter = allSources.GetEnumerator();
                while (iter.MoveNext())
                {
                    var sep = (s.Length == 0) ? string.Empty : ", ";
                    s += sep + iter.Current.Name;
                }
                if (s.Length == 0)
                    s = s_NoneString;
                s_AssignableTypes[inputType] = s;
            }
            return s_AssignableTypes[inputType];
        }

        /// <summary>Aligns fields created by UI toolkit the unity inspector standard way.</summary>
        public static string AlignFieldClassName => BaseField<bool>.alignedFieldUssClassName;

        // this is a hack to get around some vertical alignment issues in UITK
        public static float SingleLineHeight => EditorGUIUtility.singleLineHeight - EditorGUIUtility.standardVerticalSpacing;

        /// <summary>
        /// Convenience extension for UserDidSomething callbacks, making it easier to use lambdas.
        /// Cleans itself up when the owner is undisplayed.  Works in inspectors and PropertyDrawers.
        /// </summary>
        public static void TrackAnyUserActivity(
            this VisualElement owner, EditorApplication.CallbackFunction callback)
        {
            owner.RegisterCallback<AttachToPanelEvent>(_ =>
            {
                UserDidSomething += callback;
                owner.OnInitialGeometry(callback); 
                owner.RegisterCallback<DetachFromPanelEvent>(_ => UserDidSomething -= callback);
            });
        }

        /// <summary>
        /// Convenience extension for EditorApplication.update callbacks, making it easier to use lambdas.
        /// Cleans itself up when the owner is undisplayed.  Works in inspectors and PropertyDrawers.
        /// </summary>
        public static void ContinuousUpdate(
            this VisualElement owner, EditorApplication.CallbackFunction callback)
        {
            owner.RegisterCallback<AttachToPanelEvent>(_ =>
            {
                owner.OnInitialGeometry(callback); 
                EditorApplication.update += callback;
                owner.RegisterCallback<DetachFromPanelEvent>(_ => EditorApplication.update -= callback);
            });
        }
        
        /// <summary>
        /// Convenience extension to get a callback after initial geometry creation, making it easier to use lambdas.
        /// Callback will only be called once.  Works in inspectors and PropertyDrawers.
        /// </summary>
        public static void OnInitialGeometry(
            this VisualElement owner, EditorApplication.CallbackFunction callback)
        {
            owner.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            void OnGeometryChanged(GeometryChangedEvent _)
            {
                owner.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged); // call only once
                callback();
            }
        }
        
        /// <summary>
        /// Convenience extension to track a property value change plus an initial callback at creation time.  
        /// This simplifies logic for the caller, allowing use of lambda callback.
        /// </summary>
        public static void TrackPropertyWithInitialCallback(
            this VisualElement owner, SerializedProperty property, Action<SerializedProperty> callback)
        {
            owner.OnInitialGeometry(() => callback(property));
            owner.TrackPropertyValue(property, callback);
        }
        
        /// <summary>Control the visibility of a widget</summary>
        /// <param name="e">The widget</param>
        /// <param name="show">Whether it should be visible</param>
        public static void SetVisible(this VisualElement e, bool show) 
            => e.style.display = show ? StyleKeyword.Null : DisplayStyle.None;

        /// <summary>Is the widgte visible?</summary>
        /// <param name="e">The widget</param>
        /// <returns>True if visible</returns>
        public static bool IsVisible(this VisualElement e) => e.style.display != DisplayStyle.None;

        /// <summary>Convenience method: calls e.Add(child) and returns child./// </summary>
        public static T AddChild<T>(this VisualElement e, T child) where T : VisualElement
        {
            e.Add(child);
            return child;
        }

        /// <summary>
        /// Tries to set isDelayed of a FloatField, IntField, or TextField child, if it exists.
        /// </summary>
        /// <param name="e">Parent widget</param>
        /// <param name="name">name of child (or null)</param>
        public static void SafeSetIsDelayed(this VisualElement e, string name = null) 
        {
            var f = e.Q<FloatField>(name);
            if (f != null)
                f.isDelayed = true;
            var i = e.Q<IntegerField>(name);
            if (i != null)
                i.isDelayed = true;
            var t = e.Q<TextField>(name);
            if (t != null)
                t.isDelayed = true;
        }

        /// <summary>
        /// Draw a bold header in the inspector - hack to get around missing UITK functionality
        /// </summary>
        /// <param name="ux">Container in which to put the header</param>
        /// <param name="text">The text of the header</param>
        /// <param name="tooltip">optional tooltip for the header</param>
        public static void AddHeader(this VisualElement ux, string text, string tooltip = "")
        {
            var verticalPad = SingleLineHeight / 2;
            var row = ux.AddChild(new LabeledRow($"<b>{text}</b>", tooltip, new VisualElement { style = { flexBasis = 0} }));
            row.focusable = false;
            row.Label.style.flexGrow = 1;
            row.Label.style.paddingTop = verticalPad;
            row.Label.style.paddingBottom = EditorGUIUtility.standardVerticalSpacing - 2;
        }

        /// <summary>
        /// Create a space between inspector sections
        /// </summary>
        /// <param name="ux">Container in which to add the space</param>
        public static void AddSpace(this VisualElement ux)
        {
            ux.Add(new VisualElement { style = { height = SingleLineHeight / 2 }});
        }
        
        /// <summary>
        /// Add a property dragger to a float or int label, so that dragging it changes the property value.
        /// </summary>
        public static void AddDelayedFriendlyPropertyDragger(
            this Label label, SerializedProperty p, VisualElement field, 
            Action<IDelayedFriendlyDragger> OnDraggerCreated = null)
        {
            if (p.propertyType == SerializedPropertyType.Float || p.propertyType == SerializedPropertyType.Integer)
            {
                label.AddToClassList("unity-base-field__label--with-dragger");
                label.OnInitialGeometry(() =>
                {
                    if (p.propertyType == SerializedPropertyType.Float)
                    {
                        var dragger = new DelayedFriendlyFieldDragger<float>(field.Q<FloatField>());
                        dragger.SetDragZone(label);
                        OnDraggerCreated?.Invoke(dragger);
                    }
                    else if (p.propertyType == SerializedPropertyType.Integer)
                    {
                        var dragger = new DelayedFriendlyFieldDragger<int>(field.Q<IntegerField>());
                        dragger.SetDragZone(label);
                        OnDraggerCreated?.Invoke(dragger);
                    }
                });
            }
        }

        public static VisualElement CreateDraggableField(Expression<Func<object>> exp, Label label, out IDelayedFriendlyDragger dragger)
        {
            var bindingPath = SerializedPropertyHelper.PropertyName(exp);
            var tooltip = SerializedPropertyHelper.PropertyTooltip(exp);
            return CreateDraggableField(SerializedPropertyHelper.PropertyType(exp), bindingPath, tooltip, label, out dragger);
        }

        public static VisualElement CreateDraggableField(Type type, string bindingPath, string tooltip, Label label, out IDelayedFriendlyDragger dragger)
        {
            VisualElement field;
            label.AddToClassList("unity-base-field__label--with-dragger");
            label.tooltip = tooltip;
            label.style.alignSelf = Align.Center;
            if (type == typeof(float))
            {
                field = new FloatField { bindingPath = bindingPath, tooltip = tooltip };
                dragger = new DelayedFriendlyFieldDragger<float>((FloatField)field);
            }
            else if (type == typeof(int))
            {
                field = new IntegerField { bindingPath = bindingPath, tooltip = tooltip };
                dragger = new DelayedFriendlyFieldDragger<int>((IntegerField)field);
            }
            else
            {
                field = new PropertyField(null, "") { bindingPath = bindingPath, tooltip = tooltip };
                dragger = null;
            }
            var d = dragger as BaseFieldMouseDragger;
            d?.SetDragZone(label);
            return field;
        }

        
        /// <summary>A small warning sybmol, suitable for embedding in an inspector row</summary>
        /// <param name="tooltip">The tooltip text</param>
        /// <param name="iconType">The little picture: error, warning, or info</param>
        public static Label MiniHelpIcon(string tooltip, HelpBoxMessageType iconType = HelpBoxMessageType.Warning)
        {
            string icon = iconType switch
            {
                HelpBoxMessageType.Warning => "console.warnicon.sml",
                HelpBoxMessageType.Error => "console.erroricon.sml",
                _ => "console.infoicon.sml",
            };
            return new Label 
            { 
                tooltip = tooltip,
                style = 
                { 
                    flexGrow = 0,
                    flexBasis = SingleLineHeight,
                    backgroundImage = (StyleBackground)EditorGUIUtility.IconContent(icon).image,
                    width = SingleLineHeight, height = SingleLineHeight,
                    alignSelf = Align.Center
                }
            };
        }

        /// <summary>A small popup context menu, suitable for embedding in an inspector row</summary>
        /// <param name="tooltip">The tooltip text</param>
        /// <param name="contextMenu">The context menu to show when the button is pressed</param>
        public static Button MiniPopupButton(string tooltip = null, ContextualMenuManipulator contextMenu = null)
        {
            var button = new Button { tooltip = tooltip, style = 
            {
                flexGrow = 0,
                flexBasis = SingleLineHeight,
                backgroundImage = (StyleBackground)EditorGUIUtility.IconContent("_Popup").image,
                width = SingleLineHeight, height = SingleLineHeight,
                alignSelf = Align.Center,
                paddingRight = 0, marginRight = 0
            }};
            if (contextMenu != null)
            {
                contextMenu.activators.Clear();
                contextMenu.activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
                button.AddManipulator(contextMenu);
            }
            return button;
        }

        /// <summary>A small dropdown context menu, suitable for embedding in an inspector row</summary>
        /// <param name="tooltip">The tooltip text</param>
        /// <param name="contextMenu">The context menu to show when the button is pressed</param>
        public static Button MiniDropdownButton(string tooltip = null, ContextualMenuManipulator contextMenu = null)
        {
            var button = new Button { tooltip = tooltip, style = 
            {
                flexGrow = 0,
                flexBasis = SingleLineHeight,
                backgroundImage = (StyleBackground)EditorGUIUtility.IconContent("dropdown").image,
                width = SingleLineHeight, height = SingleLineHeight,
                alignSelf = Align.Center,
                paddingRight = 0, marginRight = 0
            }};
            if (contextMenu != null)
            {
                contextMenu.activators.Clear();
                contextMenu.activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
                button.AddManipulator(contextMenu);
            }
            return button;
        }

        /// <summary>
        /// This is an inspector container with 2 side-by-side rows. The Left row's width is 
        /// locked to the inspector field label size, for proper alignment.
        /// </summary>
        public class LeftRightRow : VisualElement
        {
            public VisualElement Left;
            public VisualElement Right;

            /// <summary>
            /// Set this to offset the Left/Right division from the inspector's Label/Content line
            /// </summary>
            public float DivisionOffset = 0;

            /// <summary>
            /// Set this to zero the left margin, useful for foldouts that control the margin themselves.
            /// </summary>
            public bool KillLeftMargin;

            public LeftRightRow(VisualElement left = null, VisualElement right = null)
            {
                // This is to peek at the resolved label width
                Add(new AlignFieldSizer { OnLabelWidthChanged = (w) => 
                {
                    if (KillLeftMargin)
                        style.marginLeft = 0;
                    Left.style.width = w + DivisionOffset;
                }});

                // Actual contents will live in this row
                var row = AddChild(this, new VisualElement { style = { flexDirection = FlexDirection.Row }});
                style.marginLeft = 3;

                left ??= new VisualElement();
                Left = row.AddChild(left);
                Left.style.flexDirection = FlexDirection.Row;
                Left.style.flexGrow = 0;

                right ??= new VisualElement();
                Right = row.AddChild(right);
                Right.style.flexDirection = FlexDirection.Row;
                Right.style.flexGrow = 1;
                Right.style.marginLeft = 2;
            }

            // This is a hacky thing to create custom inspector rows with labels that are the correct size
            class AlignFieldSizer : BaseField<bool> // bool is just a dummy because it has to be something
            {
                public Action<float> OnLabelWidthChanged;
                public AlignFieldSizer() : base (" ", new VisualElement()) 
                {
                    focusable = false;
                    style.flexDirection = FlexDirection.Row;
                    style.flexGrow = 1;
                    style.height = 1;
                    style.marginTop = -2;
                    AddToClassList(AlignFieldClassName);
                    labelElement.RegisterCallback<GeometryChangedEvent>((_) 
                        => OnLabelWidthChanged?.Invoke(labelElement.resolvedStyle.width));
                }
            }
        }

        /// <summary>
        /// This creates a row with a properly-sized label in front of it.
        /// The label's width is locked to the inspector field label size, for proper alignment.
        /// </summary>
        public class LabeledRow : LeftRightRow
        {
            public Label Label { get; private set; }
            public VisualElement Contents { get; private set; } 

            public LabeledRow(string label, string tooltip = "", VisualElement contents = null) 
                : base(new Label(label) { tooltip = tooltip, style = { alignSelf = Align.Center, flexGrow = 1 }}, contents)
            {
                Label = Left as Label;
                Contents = Right;
                style.marginRight = 0;
                style.flexGrow = 1;
                Contents.tooltip = tooltip;
                Contents.style.marginRight = 0;
                Contents.style.flexGrow = 1;
            }
        }

        /// <summary>
        /// A row containing a property field.  Suitable for adding widgets next to the property field.
        /// </summary>
        public static LabeledRow PropertyRow(
            SerializedProperty property, out PropertyField propertyField, string label = null)
        {
            var row = new LabeledRow(label ?? property.displayName, property.tooltip);
            propertyField = row.Contents.AddChild(new PropertyField(property, "")
                { style = { flexGrow = 1, flexBasis = SingleLineHeight * 5 }});
            AddDelayedFriendlyPropertyDragger(row.Label, property, propertyField, (d) => d.CancelDelayedWhenDragging = true);
            return row;
        }
        
        /// <summary>
        /// A property field with a minimally-sized label that does not respect inspector sizing.
        /// Suitable for embedding in a row within the right-hand side of the inspector.
        /// </summary>
        public class CompactPropertyField : VisualElement
        {
            public Label Label;
            public PropertyField Field;

            public CompactPropertyField(SerializedProperty property) : this(property, property.displayName) {}

            public CompactPropertyField(SerializedProperty property, string label, float minLabelWidth = 0)
            {
                style.flexDirection = FlexDirection.Row;
                if (!string.IsNullOrEmpty(label))
                    Label = AddChild(this, new Label(label) 
                        { tooltip = property?.tooltip, style = { alignSelf = Align.Center, minWidth = minLabelWidth }});
                Field = AddChild(this, new PropertyField(property, "") { style = { flexGrow = 1, flexBasis = 50 } });
                Field.style.marginLeft = Field.style.marginLeft.value.value - 1;
                if (Label != null && property != null)
                    AddDelayedFriendlyPropertyDragger(Label, property, Field, (d) => d.CancelDelayedWhenDragging = true);
            }
        }

        /// <summary>A foldout that displays an overlay in the right-hand column when closed.
        /// The overlay can optionally have a label of its own (use with caution).</summary>
        public class FoldoutWithOverlay : VisualElement
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

                Add(foldout);

                // There are 2 modes for this element: foldout closed and foldout open.
                // When closed, we cheat the layout system, and to implement this we do a switcheroo
                var closedContainer = AddChild(this, new LeftRightRow() { KillLeftMargin = true, style = { flexGrow = 1 }});

                var closedFoldout = new Foldout { text = foldout.text, tooltip = foldout.tooltip, value = false };
                ClosedFoldout = closedFoldout;
                ClosedFoldout = closedContainer.Left.AddChild(ClosedFoldout);
                if (overlayLabel != null)
                    closedContainer.Right.Add(overlayLabel);
                closedContainer.Right.Add(overlay);

                // Outdent the label
                if (overlayLabel != null)
                    closedContainer.Right.OnInitialGeometry(() =>
                        closedContainer.Right.style.marginLeft = -overlayLabel.resolvedStyle.width);

                // Swap the open and closed foldouts when the foldout is opened or closed
                foldout.SetVisible(foldout.value);
                closedFoldout.RegisterValueChangedCallback((evt) =>
                {
                    if (evt.target == closedFoldout)
                    {
                        if (evt.newValue && evt.target == closedFoldout)
                        {
                            closedContainer.SetVisible(false);
                            foldout.SetVisible(true);
                            foldout.value = true;
                            closedFoldout.SetValueWithoutNotify(false);
                            foldout.Q<Toggle>().Focus();
                        }
                        evt.StopPropagation();
                    }
                });

                closedContainer.SetVisible(!foldout.value);
                foldout.RegisterValueChangedCallback((evt) =>
                {
                    if (evt.target == foldout)
                    {
                        if (!evt.newValue)
                        {
                            closedContainer.SetVisible(true);
                            foldout.SetVisible(false);
                            closedFoldout.SetValueWithoutNotify(false);
                            foldout.value = false;
                            closedFoldout.Q<Toggle>().Focus();
                        }
                        evt.StopPropagation();
                    }
                });
            }
        }

        public static VisualElement HelpBoxWithButton(
            string message, HelpBoxMessageType messageType, 
            string buttonText, Action onClicked, ContextualMenuManipulator contextMenu = null)
        {
            var box = new VisualElement { style = 
            { 
                flexDirection = FlexDirection.Row, 
                paddingTop = 8, paddingBottom = 8, paddingLeft = 8, paddingRight = 8 
            }};
            box.AddToClassList("unity-help-box");
            var innerBox = box.AddChild(new VisualElement { style = { flexDirection = FlexDirection.Column, flexGrow = 1 }});

            var row = innerBox.AddChild(new VisualElement { style = { flexDirection = FlexDirection.Row, flexGrow = 1 }});
            var icon = row.AddChild(MiniHelpIcon("", messageType));
            icon.style.alignSelf = Align.Auto;
            icon.style.marginRight = 6;
            var text = row.AddChild(new Label(message) 
                { style = { flexGrow = 1, flexBasis = 100, alignSelf = Align.Center, whiteSpace = WhiteSpace.Normal }});

            var buttons = innerBox.AddChild(new VisualElement { style = { flexDirection = FlexDirection.Row, flexGrow = 1, marginTop = 6 }});
            buttons.Add(new VisualElement { style = { flexGrow = 1 }});
            var button = buttons.AddChild(new Button(onClicked) { text = buttonText });
            if (contextMenu != null)
            {
                contextMenu.activators.Clear();
                contextMenu.activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
                button.AddManipulator(contextMenu);
            }
            return box;
        }

        public static void AddRemainingProperties(VisualElement ux, SerializedProperty property)
        {
            if (property != null)
            {
                var p = property.Copy();
                do
                {
                    if (p.name != "m_Script")
                        ux.Add(new PropertyField(p));
                }
                while (p.NextVisible(false));
            }
        }

        public static bool IsAncestorOf(this Transform p, Transform other)
        {
            while (other != null && p != null)
            {
                if (other == p)
                    return true;
                other = other.parent;
            }
            return false;
        }
    }
}
