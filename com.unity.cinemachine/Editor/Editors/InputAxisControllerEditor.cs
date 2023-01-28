using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Linq;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(InputAxisController))]
    class InputAxisControllerEditor : UnityEditor.Editor
    {
        InputAxisController Target => target as InputAxisController;

        VisualElement m_ListIsEmptyMessage;
        int m_ListCount;

        void OnDisable()
        {
            EditorApplication.update -= UpdateControllersStatus;
        }

        void UpdateControllersStatus()
        {
            if (Target != null && !Target.ConrollersAreValid())
            {
                Undo.RecordObject(Target, "SynchronizeControllers");
                Target.SynchronizeControllers();
            }
        }
        
        public override VisualElement CreateInspectorGUI()
        {
            EditorApplication.update -= UpdateControllersStatus;
            EditorApplication.update += UpdateControllersStatus;
            
            var ux = new VisualElement();

#if CINEMACHINE_UNITY_INPUTSYSTEM
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.PlayerIndex)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.AutoEnableInputs)));
#endif
            ux.AddHeader("Driven Axes");
            var list = ux.AddChild(new ListView()
            {
                reorderable = false,
                showAddRemoveFooter = false,
                showBorder = false,
                showBoundCollectionSize = false,
                showFoldoutHeader = false,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight
            });
            var controllersProperty = serializedObject.FindProperty(() => Target.Controllers);
            list.BindProperty(controllersProperty);

            m_ListIsEmptyMessage = ux.AddChild(new HelpBox("No applicable CM components found.  Must have one of: "
                    + InspectorUtility.GetAssignableBehaviourNames(typeof(IInputAxisSource)), HelpBoxMessageType.Warning));

            m_ListCount = controllersProperty.arraySize;
            TrackControllerCount(controllersProperty);
            list.TrackPropertyValue(controllersProperty, TrackControllerCount);

            return ux;
        }

        void TrackControllerCount(SerializedProperty property)
        {
            m_ListIsEmptyMessage.SetVisible(property.arraySize == 0);

            // New elements should be collapsed.  GML todo: how to do this less intrusively?
            if (property.arraySize != m_ListCount)
            {
                m_ListCount = property.arraySize;
                for (int i = 0; i < m_ListCount; ++i)
                    property.GetArrayElementAtIndex(i).isExpanded = false;
            }
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

    [CustomPropertyDrawer(typeof(InputAxisController.Controller))]
    sealed class InputAxisControllerItemPropertyDrawer : PropertyDrawer
    {
        InputAxisController.Controller m_def = new ();

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var overlay = new VisualElement { style = { flexDirection = FlexDirection.Row, flexGrow = 1 }};
            overlay.Add(new PropertyField(property.FindPropertyRelative(() => m_def.Enabled), "") 
                { style = {flexGrow = 0, flexBasis = InspectorUtility.SingleLineHeight, alignSelf = Align.Center}} );

            // Draw the input value on the same line as the foldout, for convenience
#if CINEMACHINE_UNITY_INPUTSYSTEM
            overlay.Add(new PropertyField(property.FindPropertyRelative(() => m_def.InputAction), "") 
                { style = {flexGrow = 1, flexBasis = 5 * InspectorUtility.SingleLineHeight}} );
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            overlay.Add(new PropertyField(property.FindPropertyRelative(() => m_def.LegacyInput), "") 
                { style = {flexGrow = 1, flexBasis = 5 * InspectorUtility.SingleLineHeight, marginLeft = 4}} );
#endif
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
    }
}
