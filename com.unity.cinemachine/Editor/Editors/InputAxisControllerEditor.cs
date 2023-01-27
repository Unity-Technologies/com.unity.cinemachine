using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(InputAxisController))]
    class InputAxisControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var Target = target as InputAxisController;
            if (Target != null && !Target.ConrollersAreValid())
            {
                Undo.RecordObject(Target, "SynchronizeControllers");
                Target.SynchronizeControllers();
            }

            serializedObject.Update();
            var controllers = serializedObject.FindProperty("Controllers");

            EditorGUI.BeginChangeCheck();

#if CINEMACHINE_UNITY_INPUTSYSTEM
            EditorGUILayout.PropertyField(serializedObject.FindProperty("PlayerIndex"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("AutoEnableInputs"));
#endif

            int numElements = controllers.arraySize;
            if (numElements == 0)
                EditorGUILayout.HelpBox("No applicable CM components found.  Must have one of: "
                    + InspectorUtility.GetAssignableBehaviourNames(typeof(IInputAxisSource)), 
                    MessageType.Warning);
            else for (int i = 0; i < numElements; ++i)
            {
                var element = controllers.GetArrayElementAtIndex(i);

                var rect = EditorGUILayout.GetControlRect();
                float height = EditorGUIUtility.singleLineHeight;
                rect.height = height;

                element.isExpanded = EditorGUI.Foldout(
                    new Rect(rect.x, rect.y, EditorGUIUtility.labelWidth - height, rect.height),
                    element.isExpanded, 
                    new GUIContent(element.displayName, element.tooltip), true);

                if (element.isExpanded)
                {
                    ++EditorGUI.indentLevel;
                    rect = EditorGUILayout.GetControlRect(true, InspectorUtility.PropertyHeightOfChidren(element));
                    InspectorUtility.DrawChildProperties(rect, element);
                    --EditorGUI.indentLevel;
                }
                else
                {
                    // Draw the input value on the same line as the foldout, for convenience
                    SerializedProperty actionProperty = null;
                    SerializedProperty legacyProperty = null;
                    int numFields = 0;
#if CINEMACHINE_UNITY_INPUTSYSTEM
                    actionProperty = element.FindPropertyRelative("InputAction");
                    ++numFields;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
                    legacyProperty = element.FindPropertyRelative("LegacyInput");
                    ++numFields;
#endif
                    if (numFields > 0)
                    {
                        var blankLabelWidth = height / numFields;
                        rect.x += EditorGUIUtility.labelWidth - blankLabelWidth;
                        rect.width -= EditorGUIUtility.labelWidth - blankLabelWidth;
                        rect.width /= numFields;

                        int oldIndent = EditorGUI.indentLevel;
                        float oldLabelWidth = EditorGUIUtility.labelWidth;

                        EditorGUI.indentLevel = 0;
                        EditorGUIUtility.labelWidth = blankLabelWidth;

                        if (actionProperty != null)
                        {
                            EditorGUI.PropertyField(rect, actionProperty, new GUIContent(" ", actionProperty.tooltip));
                            rect.x += rect.width;
                        }
                        if (legacyProperty != null)
                            EditorGUI.PropertyField(rect, legacyProperty, new GUIContent(" ", legacyProperty.tooltip));

                        EditorGUI.indentLevel = oldIndent;
                        EditorGUIUtility.labelWidth = oldLabelWidth;
                    }
                }
            }
            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();
        }

        [InitializeOnLoad]
        class DefaultControlInitializer
        {
            static DefaultControlInitializer()
            {
                InputAxisController.SetControlDefaults 
                    = (in IInputAxisSource.AxisDescriptor axis, ref InputAxisController.Controller controller) => 
                {
#pragma warning disable CS0219 // Variable is assigned but its value is never used
                    var actionName = "";
#pragma warning restore CS0219 // Variable is assigned but its value is never used
                    var inputName = "";
                    var invertY = false;
                    bool isMomentary = (axis.DrivenAxis().Restrictions & InputAxis.RestrictionFlags.Momentary) != 0;

                    if (axis.Name.Contains("Look"))
                    {
                        actionName = "Player/Look";
                        inputName = axis.Hint == IInputAxisSource.AxisDescriptor.Hints.X ? "Mouse X" 
                            : (axis.Hint == IInputAxisSource.AxisDescriptor.Hints.Y ? "Mouse Y" : "");
                        invertY = axis.Hint == IInputAxisSource.AxisDescriptor.Hints.Y;
                        controller.Control = new InputAxisControl { AccelTime = 0.2f, DecelTime = 0.2f };
                    }
#if false
                    if (axis.Name.Contains("Zoom") || axis.Name.Contains("Scale"))
                    {
                        //actionName = "UI/ScrollWheel"; // best we can do - actually it doean't work because it'a Vector2 type
                        inputName = "Mouse ScrollWheel";
                    }
#endif
                    if (axis.Name.Contains("Move"))
                    {
                        actionName = "Player/Move";
                        inputName = axis.Hint == IInputAxisSource.AxisDescriptor.Hints.X ? "Horizontal" 
                            : (axis.Hint == IInputAxisSource.AxisDescriptor.Hints.Y ? "Vertical" : "");
                    }
                    if (axis.Name.Contains("Fire"))
                    {
                        actionName = "Player/Fire";
                        inputName = "Fire";
                    }
                    if (axis.Name.Contains("Jump"))
                    {
                        actionName = "UI/RightClick"; // best we can do
                        inputName = "Jump";
                    }

#if CINEMACHINE_UNITY_INPUTSYSTEM
                    if (actionName.Length != 0)
                    {
                        controller.InputAction = (UnityEngine.InputSystem.InputActionReference)AssetDatabase.LoadAllAssetsAtPath(
                            "Packages/com.unity.inputsystem/InputSystem/Plugins/PlayerInput/DefaultInputActions.inputactions").FirstOrDefault(
                                x => x.name == actionName);
                    }
                    controller.Gain = isMomentary ? 1 : 4f * (invertY ? -1 : 1);
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
                    controller.LegacyInput = inputName;
                    controller.LegacyGain = isMomentary ? 1 : 200 * (invertY ? -1 : 1);
#endif
                    controller.Enabled = true;
                };
            }
        }
    }



#if false // GML incomplete code.  This is not working yet in UITK - stay in IMGUI for now
        public override VisualElement CreateInspectorGUI()
        {
            var Target = target as InputAxisController;

            var ux = new VisualElement();

            ux.Add(new PropertyField(serializedObject.FindProperty("PlayerIndex")));
#if CINEMACHINE_UNITY_INPUTSYSTEM
            ux.Add(new PropertyField(serializedObject.FindProperty("AutoEnableInputs"));
#endif
            ux.AddSpace();

            //var list = new PropertyField(serializedObject.FindProperty("Controllers"))
            var list = new ListView()
            {
                reorderable = false,
                showAddRemoveFooter = false,
                showBorder = false,
                showBoundCollectionSize = false,
                showFoldoutHeader = false,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight
            };
            list.BindProperty(serializedObject.FindProperty("Controllers"));
            ux.Add(list);

            return ux;
        }
    }

    [CustomPropertyDrawer(typeof(InputAxisController.Controller))]
    sealed class InputAxisControllerItemPropertyDrawer : PropertyDrawer
    {
        InputAxisController.Controller m_def = new InputAxisController.Controller();

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            // Draw the input value on the same line as the foldout, for convenience
            SerializedProperty inputProp = null;
#if CINEMACHINE_UNITY_INPUTSYSTEM
            inputProp = property.FindPropertyRelative(() => m_def.InputAction);
#elif ENABLE_LEGACY_INPUT_MANAGER
            inputProp = property.FindPropertyRelative(() => m_def.InputName);
#endif
            var overlay = new PropertyField(inputProp, "") { style = {flexGrow = 1}};

            var foldout = new Foldout()
            {
                text = property.displayName,
                tooltip = property.tooltip,
                value = property.isExpanded
            };
            var childProperty = property.Copy();
            var endProperty = childProperty.GetEndProperty();
            childProperty.NextVisible(true);
            while (!SerializedProperty.EqualContents(childProperty, endProperty))
            {
                foldout.Add(new PropertyField(childProperty));
                childProperty.NextVisible(false);
            }
            return new InspectorUtility.FoldoutWithOverlay(foldout, overlay, null);
        }

        //VisualElement CreateInputAxisNameControl(
    }
#endif
}
