#define LISTVIEW_BUG_WORKAROUND // GML hacking because of another ListView bug

using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Linq;

namespace Unity.Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineInputAxisController))]
    class InputAxisControllerEditor : UnityEditor.Editor
    {
        CinemachineInputAxisController Target => target as CinemachineInputAxisController;
        
        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();
            
#if CINEMACHINE_UNITY_INPUTSYSTEM
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.PlayerIndex)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.AutoEnableInputs)));
#endif
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.ScanRecursively)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.SuppressInputWhileBlending)));
#if LISTVIEW_BUG_WORKAROUND
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.m_ControllerList)));
#else
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.Controllers)));
#endif
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
#pragma warning restore CS0219 // Variable is assigned but its value is never used
                    var inputName = "";
                    var invertY = false;
                    bool isMomentary = (axis.DrivenAxis().Restrictions & InputAxis.RestrictionFlags.Momentary) != 0;

                    if (axis.Name.Contains("Look"))
                    {
                        actionName = "Player/Look";
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
                        actionName = "Player/Zoom";
                        inputName = "Mouse ScrollWheel";
                    }
                    if (axis.Name.Contains("Move"))
                    {
                        actionName = "Player/Move";
                        inputName = axis.Hint switch
                        {
                           IInputAxisOwner.AxisDescriptor.Hints.X => "Horizontal",
                           IInputAxisOwner.AxisDescriptor.Hints.Y => "Vertical",
                           _ => ""
                        };
                    }
                    if (axis.Name.Contains("Fire"))
                    {
                        actionName = "Player/Fire";
                        inputName = "Fire1";
                    }
                    if (axis.Name.Contains("Jump"))
                    {
                        actionName = "Player/Jump";
                        inputName = "Jump";
                    }
                    if (axis.Name.Contains("Sprint"))
                    {
                        actionName = "Player/Sprint";
                        inputName = "Fire3"; // best we can do
                    }

#if CINEMACHINE_UNITY_INPUTSYSTEM
                    if (actionName.Length != 0)
                    {
                        var asset = ScriptableObjectUtility.kPackageRoot 
                            + "/Runtime/Input/CinemachineDefaultInputActions.inputactions";
                        controller.Input.InputAction = (UnityEngine.InputSystem.InputActionReference)
                            AssetDatabase.LoadAllAssetsAtPath(asset).FirstOrDefault(x => x.name == actionName);
                    }
                    controller.Input.Gain = invertY ? -1 : 1;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
                    controller.Input.LegacyInput = inputName;
                    controller.Input.LegacyGain = isMomentary ? 1 : 200 * (invertY ? -1 : 1);
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
                { style = {flexGrow = 0, flexBasis = InspectorUtility.SingleLineHeight, alignSelf = Align.Center}} );

            // Draw the input value on the same line as the foldout, for convenience
            var inputProperty = property.FindPropertyRelative(() => def.Input);
#if CINEMACHINE_UNITY_INPUTSYSTEM
            overlay.Add(new PropertyField(inputProperty.FindPropertyRelative(() => def.Input.InputAction), "") 
                { style = {flexGrow = 1, flexBasis = 5 * InspectorUtility.SingleLineHeight}} );
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            overlay.Add(new PropertyField(inputProperty.FindPropertyRelative(() => def.Input.LegacyInput), "") 
                { style = {flexGrow = 1, flexBasis = 5 * InspectorUtility.SingleLineHeight, marginLeft = 6}} );
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
            return new InspectorUtility.FoldoutWithOverlay(foldout, overlay, null);
        }
    }

    [CustomPropertyDrawer(typeof(InputAxisControllerListAttribute))]
    class InputAxisControllerListPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
#if LISTVIEW_BUG_WORKAROUND
            property = property.FindPropertyRelative("Controllers");
#endif
            var ux = new VisualElement();
            var list = ux.AddChild(new ListView()
            {
                reorderable = false,
                showAddRemoveFooter = false,
                showBorder = false,
                showBoundCollectionSize = false,
                showFoldoutHeader = false,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight
            });
            list.BindProperty(property);

            var isEmptyMessage = ux.AddChild(new HelpBox(
                "No applicable components found.  Must have one of: "
                    + InspectorUtility.GetAssignableBehaviourNames(typeof(IInputAxisOwner)), 
                HelpBoxMessageType.Warning));
            list.TrackPropertyWithInitialCallback(
                property, (p) => isEmptyMessage.SetVisible(p.arraySize == 0));

            // Synchronize the controller list
            ux.TrackAnyUserActivity(() =>
            {
                var targets = property.serializedObject.targetObjects;
                for (int i = 0; i < targets.Length; ++i)
                {
                    var target = targets[i] as IInputAxisController;
                    if (!target.ControllersAreValid())
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
