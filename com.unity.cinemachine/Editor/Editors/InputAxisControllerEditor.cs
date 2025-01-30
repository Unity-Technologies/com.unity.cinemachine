using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Unity.Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineInputAxisController))]
    class InputAxisControllerEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();
            var prop = serializedObject.GetIterator();
            if (prop.NextVisible(true))
                InspectorUtility.AddRemainingProperties(ux, prop);
            return ux;
        }
        
        [InitializeOnLoad]
        class DefaultControlInitializer
        {
            static DefaultControlInitializer()
            {
                CinemachineInputAxisController.SetControlDefaults 
                    = (in IInputAxisOwner.AxisDescriptor axis, ref CinemachineInputAxisController.Controller controller) => 
                {
#pragma warning disable CS0219 // Variable is assigned but its value is never used
                    var actionName = "";
#pragma warning restore CS0219
                    var inputName = "";
                    var invertY = false;
                    bool isMomentary = (axis.DrivenAxis().Restrictions & InputAxis.RestrictionFlags.Momentary) != 0;

                    if (axis.Name.Contains("Look"))
                    {
                        actionName = "CM Default/Look";
                        inputName = axis.Hint switch
                        {
                           IInputAxisOwner.AxisDescriptor.Hints.X => "Mouse X",
                           IInputAxisOwner.AxisDescriptor.Hints.Y => "Mouse Y",
                           _ => ""
                        };
                        invertY = axis.Hint == IInputAxisOwner.AxisDescriptor.Hints.Y;
                        controller.Driver = DefaultInputAxisDriver.Default;
                    }
                    if (axis.Name.Contains("Zoom") || axis.Name.Contains("Scale"))
                    {
                        actionName = "CM Default/Zoom";
                        inputName = "Mouse ScrollWheel";
                        invertY = true;
                    }
                    if (axis.Name.Contains("Move"))
                    {
                        actionName = "CM Default/Move";
                        inputName = axis.Hint switch
                        {
                           IInputAxisOwner.AxisDescriptor.Hints.X => "Horizontal",
                           IInputAxisOwner.AxisDescriptor.Hints.Y => "Vertical",
                           _ => ""
                        };
                    }
                    if (axis.Name.Contains("Fire"))
                    {
                        actionName = "CM Default/Fire";
                        inputName = "Fire1";
                    }
                    if (axis.Name.Contains("Jump"))
                    {
                        actionName = "CM Default/Jump";
                        inputName = "Jump";
                    }
                    if (axis.Name.Contains("Sprint"))
                    {
                        actionName = "CM Default/Sprint";
                        inputName = "Fire3"; // best we can do
                    }

#if CINEMACHINE_UNITY_INPUTSYSTEM
                    if (actionName.Length != 0)
                    {
                        var assetPath = CinemachineCore.kPackageRoot 
                            + "/Runtime/Input/CinemachineDefaultInputActions.inputactions";
                        var assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
                        for (int i = 0; controller.Input.InputAction == null && i < assets.Length; ++i)
                            if (assets[i].name == actionName)
                                controller.Input.InputAction = (UnityEngine.InputSystem.InputActionReference)assets[i];
                    }
                    controller.Input.Gain = invertY ? -1 : 1;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER && !CINEMACHINE_UNITY_INPUTSYSTEM
                    controller.Input.LegacyInput = inputName;
                    controller.Input.LegacyGain = isMomentary ? 1 : 100 * (invertY ? -1 : 1);
#endif
                    controller.Enabled = true;
                };
            }
        }
    }

    [CustomPropertyDrawer(typeof(CinemachineInputAxisController.Controller), true)]
    class InputAxisControllerItemPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            CinemachineInputAxisController.Controller def = new ();

            var overlay = new VisualElement { style = { flexDirection = FlexDirection.Row, flexGrow = 1 }};
            overlay.Add(new PropertyField(property.FindPropertyRelative(() => def.Enabled), "") 
                { style = { marginLeft = 2, flexGrow = 0, flexBasis = InspectorUtility.SingleLineHeight, alignSelf = Align.Center}} );

            // Draw the input value on the same line as the foldout, for convenience
            var inputProperty = property.FindPropertyRelative(() => def.Input);
#if CINEMACHINE_UNITY_INPUTSYSTEM
            overlay.Add(new PropertyField(inputProperty.FindPropertyRelative(() => def.Input.InputAction), "") 
                { style = { marginLeft = -3, flexGrow = 1, flexBasis = 5 * InspectorUtility.SingleLineHeight}} );
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            overlay.Add(new PropertyField(inputProperty.FindPropertyRelative(() => def.Input.LegacyInput), "") 
                { style = {flexGrow = 1, flexBasis = 5 * InspectorUtility.SingleLineHeight }} );
#endif
            var foldout = new Foldout() { text = property.displayName, tooltip = property.tooltip };
            foldout.BindProperty(property);

            var childProperty = property.Copy();
            var endProperty = childProperty.GetEndProperty();
            childProperty.NextVisible(true);
            while (!SerializedProperty.EqualContents(childProperty, endProperty))
            {
                foldout.Add(new PropertyField(childProperty));
                childProperty.NextVisible(false);
            }
            return new InspectorUtility.FoldoutWithOverlay(foldout, overlay, null) { style = { marginLeft = 12, marginRight = 3 }};
        }
    }

    [CustomPropertyDrawer(typeof(InputAxisControllerManagerAttribute))]
    class InputAxisControllerListPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            // Why not make a PropertyDrawer for the list directly?  Because
            // of a bug in ListView - PropertyDrawers directly on Lists don't work.
            // This is a workaround for that bug.
            property = property.FindPropertyRelative("Controllers");

            var ux = new VisualElement();
            var list = ux.AddChild(new ListView()
            {
                reorderable = false,
                showAddRemoveFooter = false,
                showBorder = false,
                showBoundCollectionSize = false,
                showFoldoutHeader = false,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                style = { marginLeft = -12, marginRight = -3 }
            });
            list.BindProperty(property);

            var isEmptyMessage = ux.AddChild(new HelpBox(
                "<b>This component will be ignored because no applicable target components are present.</b>\n\n"
                    + "Applicable target components include: "
                    + InspectorUtility.GetAssignableBehaviourNames(typeof(IInputAxisOwner)), 
                HelpBoxMessageType.Warning));
            list.TrackPropertyWithInitialCallback(
                property, (p) => isEmptyMessage.SetVisible(p.serializedObject != null && p.arraySize == 0));

            // Synchronize the controller list
            ux.TrackAnyUserActivity(() =>
            {
                if (property.serializedObject == null)
                    return; // object deleted
                var targets = property.serializedObject.targetObjects;
                for (int i = 0; i < targets.Length; ++i)
                {
                    if (targets[i] is IInputAxisController target && !target.ControllersAreValid())
                    {
                        Undo.RecordObject(targets[i], "SynchronizeControllers");
                        target.SynchronizeControllers();
                    }
                }
            });
            return ux;
        }
    }
}
