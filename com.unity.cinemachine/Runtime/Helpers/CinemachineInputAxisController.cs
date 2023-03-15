using System;
using UnityEngine;

#if CINEMACHINE_UNITY_INPUTSYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;
using System.Linq;
#endif

namespace Unity.Cinemachine
{
    /// <summary>
    /// This is a behaviour that is used to drive other behaviours that implement IInputAxisOwner, 
    /// which it discovers dynamically.  It is the bridge between the input system and 
    /// Cinemachine cameras that require user input.  Add it to a Cinemachine camera that needs it.
    /// 
    /// This implementation can read input from the Input package, or from the legacy input system, 
    /// or both, depending on what is installed in the project.
    /// </summary>
    [ExecuteAlways]
    [SaveDuringPlay]
    [AddComponentMenu("Cinemachine/Helpers/Cinemachine Input Axis Controller")]
    [HelpURL(Documentation.BaseURL + "manual/InputAxisController.html")]
    public class CinemachineInputAxisController : InputAxisControllerBase<DefaultInputAxisReader> 
    {
        /// <summary>
        /// This is a mechanism to allow the inspector to set up default values 
        /// when the component is reset.
        /// </summary>
        /// <param name="axis">The information of the input axis.</param>
        /// <param name="controller">Reference to the controller to change.</param>
        internal delegate void SetControlDefaultsForAxis(
            in IInputAxisOwner.AxisDescriptor axis, ref Controller controller);
        internal static SetControlDefaultsForAxis SetControlDefaults;
        
        /// <summary>
        /// Creates default controllers for an axis.
        /// Override this if the default axis controllers do not fit your axes.
        /// </summary>
        /// <param name="axis">Description of the axis whose default controller needs to be set.</param>
        /// <param name="controller">Controller to drive the axis.</param>
        protected override void InitializeControllerDefaultsForAxis(
            in IInputAxisOwner.AxisDescriptor axis, Controller controller)
        { 
            SetControlDefaults?.Invoke(axis, ref controller);
        }
    }


    /// <summary>Read an input value from legacy input or from and Input Action</summary>
    [Serializable]
    public class DefaultInputAxisReader : IInputAxisReader
    {
#if CINEMACHINE_UNITY_INPUTSYSTEM
        /// <summary>Action for the Input package (if used).</summary>
        [Tooltip("Action for the Input package (if used).")]
        public InputActionReference InputAction;

        /// <summary>The input value is multiplied by this amount prior to processing.
        /// Controls the input power.  Set it to a negative value to invert the input</summary>
        [Tooltip("The input value is multiplied by this amount prior to processing.  "
            + "Controls the input power.  Set it to a negative value to invert the input")]
        public float Gain = 1;

        /// <summary>The actual action, resolved for player</summary>
        internal InputAction m_CachedAction;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        /// <summary>Axis name for the Legacy Input system (if used).  
        /// CinemachineCore.GetInputAxis() will be called with this name.</summary>
        [InputAxisNameProperty]
        [Tooltip("Axis name for the Legacy Input system (if used).  "
            + "This value will be used to control the axis.")]
        public string LegacyInput;

        /// <summary>The LegacyInput value is multiplied by this amount prior to processing.
        /// Controls the input power.  Set it to a negative value to invert the input</summary>
        [Tooltip("The LegacyInput value is multiplied by this amount prior to processing.  "
            + "Controls the input power.  Set it to a negative value to invert the input")]
        public float LegacyGain = 1;
#endif

        /// <summary>Get the current value of the axis.</summary>
        /// <param name="context">The context GameObject, can be used for logging diagnostics</param>
        /// <param name="playerIndex">For multiplayer games the player index if applicable, or -1 for default</param>
        /// <param name="autoEnableInput">If true, then disabled controls should be automatically enabled on first reading</param>
        /// <param name="hint">A hint for converting a Vector2 value to a float</param>
        /// <returns>The axis value</returns>
        public float GetValue(
            UnityEngine.Object context, 
            int playerIndex,
            bool autoEnableInput,
            IInputAxisOwner.AxisDescriptor.Hints hint)
        {
            float inputValue = 0;
#if CINEMACHINE_UNITY_INPUTSYSTEM
            if (InputAction != null)
                inputValue = ResolveAndReadInputAction(context, playerIndex, autoEnableInput, hint) * Gain;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (inputValue == 0 && !string.IsNullOrEmpty(LegacyInput))
            {
                try { inputValue = CinemachineCore.GetInputAxis(LegacyInput) * LegacyGain; }
                catch (ArgumentException) {}
                //catch (ArgumentException e) { Debug.LogError(e.ToString()); }
            }
#endif
            return inputValue;
        }

#if CINEMACHINE_UNITY_INPUTSYSTEM
        float ResolveAndReadInputAction(
            UnityEngine.Object context,
            int playerIndex, bool autoEnableInput, 
            IInputAxisOwner.AxisDescriptor.Hints hint)
        {
            // Resolve Action for player
            if (m_CachedAction != null && InputAction.action.id != m_CachedAction.id)
                m_CachedAction = null;
            if (m_CachedAction == null)
            {
                m_CachedAction = InputAction.action;
                if (playerIndex != -1)
                    m_CachedAction = GetFirstMatch(InputUser.all[playerIndex], InputAction);
                if (autoEnableInput && m_CachedAction != null)
                    m_CachedAction.Enable();

                // local function to wrap the lambda which otherwise causes a tiny gc
                InputAction GetFirstMatch(in InputUser user, InputActionReference aRef) => 
                    user.actions.First(x => x.id == aRef.action.id);
            }

            // Update enabled status
            if (m_CachedAction != null && m_CachedAction.enabled != InputAction.action.enabled)
            {
                if (InputAction.action.enabled)
                    m_CachedAction.Enable();
                else
                    m_CachedAction.Disable();
            }

            // Read the value
            return m_CachedAction != null ? ReadInput(m_CachedAction, hint, context) : 0f;
        }

        /// <summary>
        /// Definition of how we read input. Override this in your child classes to specify
        /// the InputAction's type to read if it is different from float or Vector2.
        /// </summary>
        /// <param name="action">The action being read.</param>
        /// <param name="hint">The axis hint of the action.</param>
        /// <param name="context">The owner GameObject, can be used for logging diagnostics</param>
        /// <returns>Returns the value of the input device.</returns>
        protected virtual float ReadInput(
            InputAction action, IInputAxisOwner.AxisDescriptor.Hints hint, UnityEngine.Object context)
        {
            var control = action.activeControl;
            if (control != null)
            {
                try 
                {
                    // If we can read as a Vector2, do so
                    if (control.valueType == typeof(Vector2) || action.expectedControlType == "Vector2")
                    {
                        var value = action.ReadValue<Vector2>();
                        return hint == IInputAxisOwner.AxisDescriptor.Hints.Y ? value.y : value.x;
                    }
                    // Default: assume type is float
                    return action.ReadValue<float>(); 
                }
                catch (InvalidOperationException)
                {
                    Debug.LogError($"An action in {context.name} is mapped to a {control.valueType.Name} "
                        + "control.  DefaultInputAxisReader can only handle float or Vector2 types.  "
                        + "To handle other types you can create a class inheriting "
                        + "DefaultInputAxisReader and override the ReadInput method.");
                }
            }
            return 0f;
        }
#endif
    }
}
