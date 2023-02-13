using UnityEditor;
using Cinemachine.Editor;
using System.Collections.Generic;
using Cinemachine.Utility;
using System;
using System.Reflection;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Cinemachine
{
    [CustomEditor(typeof(CinemachineFreeLookModifier))]
    class CinemachineFreeLookModifierEditor : UnityEditor.Editor
    {
        CinemachineFreeLookModifier Target => target as CinemachineFreeLookModifier;

        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();

            var invalidSrcMsg = ux.AddChild(
                new HelpBox("No applicable components found.  Must have one of: "
                    + InspectorUtility.GetAssignableBehaviourNames(
                        typeof(CinemachineFreeLookModifier.IModifierValueSource)), 
                    HelpBoxMessageType.Warning));

            ux.AddHeader("Modifiers");
            var list = ux.AddChild(new ListView()
            {
                reorderable = true,
                showAddRemoveFooter = true,
                showBorder = true,
                showBoundCollectionSize = false,
                showFoldoutHeader = false,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight
            });
            var controllersProperty = serializedObject.FindProperty(() => Target.Modifiers);
            list.BindProperty(controllersProperty);

            // Convert the add button to a popup selector
            var button = list.Q<Button>("unity-list-view__add-button");
            var manipulator = new ContextualMenuManipulator((evt) => 
            {
                for (int i = 0; i < ModifierMenuItems.s_ModifierNames.Count; ++i)
                {
                    var type = ModifierMenuItems.s_AllModifiers[i];
                    evt.menu.AppendAction(ModifierMenuItems.s_ModifierNames[i], 
                        (action) =>
                        {
                            Undo.RecordObject(Target, "add modifier");
                            var m = (CinemachineFreeLookModifier.Modifier)Activator.CreateInstance(type);
                            m.RefreshCache(Target.ComponentOwner);
                            m.Reset(Target.ComponentOwner);
                            Target.Modifiers.Add(m);
                        }, 
                        (status) =>
                        {
                            // Enable item if not already present
                            for (int m = 0; m < Target.Modifiers.Count; ++m)
                                if (Target.Modifiers[m] != null && Target.Modifiers[m].GetType() == type)
                                    return DropdownMenuAction.Status.Disabled;
                            return DropdownMenuAction.Status.Normal;
                        });
                }
            });
            manipulator.activators.Clear();
            manipulator.activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
            button.AddManipulator(manipulator);
            button.clickable = null;

            TrackSourceValidity();
            ux.schedule.Execute(TrackSourceValidity).Every(250); // GML todo: is there a better way to do this?
            void TrackSourceValidity() => invalidSrcMsg.SetVisible(Target != null && !Target.HasValueSource());

            return ux;
        }

        [CustomPropertyDrawer(typeof(CinemachineFreeLookModifier.Modifier), true)]
        class InputAxisControllerItemPropertyDrawer : PropertyDrawer
        {
            public override VisualElement CreatePropertyGUI(SerializedProperty property)
            {
                var m = property.managedReferenceValue as CinemachineFreeLookModifier.Modifier;
                if (m == null)
                    return new Label("invalid item");

                var typeName = ModifierMenuItems.GetModifierName(m.GetType());

                var warningText = "No applicable components found.  Must have one of: "
                    + InspectorUtility.GetAssignableBehaviourNames(m.CachedComponentType);

                var overlay = new VisualElement { style = { flexDirection = FlexDirection.Row, flexGrow = 1 }};
                var warningSymbol = overlay.AddChild(new Label 
                { 
                    tooltip = warningText,
                    style = 
                    { 
                        backgroundImage = (StyleBackground)EditorGUIUtility.IconContent("console.warnicon.sml").image,
                        width = InspectorUtility.SingleLineHeight, height = InspectorUtility.SingleLineHeight,
                        alignSelf = Align.Center,
                        //paddingRight = 0, borderRightWidth = 0, marginRight = 0
                    }
                });

                var foldout = new Foldout() { text = typeName, tooltip = property.tooltip };
                foldout.BindProperty(property);

                var noComponentsMsg = foldout.AddChild(new HelpBox(warningText, 
                    HelpBoxMessageType.Warning));

                var childProperty = property.Copy();
                var endProperty = childProperty.GetEndProperty();
                childProperty.NextVisible(true);
                while (!SerializedProperty.EqualContents(childProperty, endProperty))
                {
                    foldout.Add(new PropertyField(childProperty));
                    childProperty.NextVisible(false);
                }

                TrackSourceValidity();
                foldout.schedule.Execute(TrackSourceValidity).Every(250); // GML todo: is there a better way to do this?
                void TrackSourceValidity() 
                {
                    var showWarning = m != null && !m.HasRequiredComponent;
                    noComponentsMsg.SetVisible(showWarning);
                    warningSymbol.SetVisible(showWarning);
                };

                return new InspectorUtility.FoldoutWithOverlay(foldout, overlay, null);
            }
        }

        [InitializeOnLoad]
        static class ModifierMenuItems
        {
            public static string GetModifierName(Type type)
            {
                for (int j = 0; j < s_AllModifiers.Count; ++j)
                    if (s_AllModifiers[j] == type)
                        return s_ModifierNames[j];
                return "(none)"; // should never get here
            }
        
            // These lists are synchronized
            public static List<Type> s_AllModifiers = new ();
            public static List<string> s_ModifierNames = new ();

            // This code dynamically discovers eligible classes and builds the menu data 
            static ModifierMenuItems()
            {
                // Get all Modifier types
                var allTypes
                    = ReflectionHelpers.GetTypesInAllDependentAssemblies(
                        (Type t) => typeof(CinemachineFreeLookModifier.Modifier).IsAssignableFrom(t) 
                        && !t.IsAbstract && t.GetCustomAttribute<ObsoleteAttribute>() == null);

                s_AllModifiers.Clear();
                s_ModifierNames.Clear();
                s_AllModifiers.AddRange(allTypes);
                for (int i = 0; i < s_AllModifiers.Count; ++i)
                {
                    var name = s_AllModifiers[i].Name;
                    var index = name.LastIndexOf("Modifier", StringComparison.OrdinalIgnoreCase);
                    if (index >= 0)
                        name = name.Remove(index);
                    s_ModifierNames.Add(InspectorUtility.NicifyClassName(name));
                }
            }
        }
    }
}

