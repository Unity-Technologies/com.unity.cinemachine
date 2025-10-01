using System;
using UnityEngine;

#if CINEMACHINE_UNITY_INPUTSYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;
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
    [HelpURL(Documentation.BaseURL + "manual/CinemachineInputAxisController.html")]
    public class CinemachineInputAxisController
        : InputAxisControllerBase<CinemachineInputAxisController.Reader>
    {
#if CINEMACHINE_UNITY_INPUTSYSTEM
        /// <summary>
        /// Leave this at -1 for single-player games.
        /// For multi-player games, set this to be the player index, and the actions will
        /// be read from that player's controls
        /// </summary>
        [Tooltip("Leave this at -1 for single-player games.  "
            + "For multi-player games, set this to be the player index, and the actions will "
            + "be read from that player's controls")]
        public int PlayerIndex = -1;

        /// <summary>If set, Input Actions will be auto-enabled at start</summary>
        [Tooltip("If set, Input Actions will be auto-enabled at start")]
        public bool AutoEnableInputs = true;
#endif
        /// <summary>
        /// This is a mechanism to allow the inspector to set up default values
        /// when the component is reset.
        /// </summary>
        /// <param name="axis">The information of the input axis.</param>
        /// <param name="controller">Reference to the controller to change.</param>
        internal delegate void SetControlDefaultsForAxis(
            in IInputAxisOwner.AxisDescriptor axis, ref Controller controller);
        internal static SetControlDefaultsForAxis SetControlDefaults;

#if CINEMACHINE_UNITY_INPUTSYSTEM
        /// <summary>
        /// CinemachineInputAxisController.Reader can only handle float or Vector2 InputAction types.
        /// To handle other types you can install a handler to read InputActions of a different type.
        /// </summary>
        /// <param name="action">The action to read</param>
        /// <param name="hint">The axis hint of the action.</param>
        /// <param name="context">The owner object, can be used for logging diagnostics</param>
        /// <param name="defaultReadValue">The default reader to call if you don't handle the type</param>
        /// <returns>The value of the control</returns>
        public Reader.ControlValueReader ReadControlValueOverride;

        /// <inheritdoc />
        protected override void Reset()
        {
            base.Reset();
            PlayerIndex = -1;
            AutoEnableInputs = true;
        }
#endif

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

        //TODO Support fixed update as well. Input system has a setting to update inputs only during fixed update.
        //TODO This won't work accuratly if this setting is enabled.
        void Update()
        {
            if (Application.isPlaying)
                UpdateControllers();
        }

        /// <summary>Read an input value from legacy input or from and Input Action</summary>
        [Serializable]
        public sealed class Reader : IInputAxisReader
        {
#if CINEMACHINE_UNITY_INPUTSYSTEM
            /// <summary>Action mapping for the Input package (if used).</summary>
            [Tooltip("Action mapping for the Input package.")]
            public InputActionReference InputAction;

            /// <summary>The input value is multiplied by this amount prior to processing.
            /// Controls the input power.  Set it to a negative value to invert the input</summary>
            [Tooltip("The input value is multiplied by this amount prior to processing.  "
                + "Controls the input power.  Set it to a negative value to invert the input")]
            public float Gain = 1;

            /// <summary>The actual action, resolved for player</summary>
            [NonSerialized] internal InputAction m_CachedAction;

            /// <summary>
            /// CinemachineInputAxisController.Reader can only handle float or Vector2 InputAction types.
            /// To handle other types you can install a handler to read InputActions of a different type.
            /// </summary>
            /// <param name="action">The action to read</param>
            /// <param name="hint">The axis hint of the action.</param>
            /// <param name="context">The owner object, can be used for logging diagnostics</param>
            /// <param name="defaultReader">The default reader to call if you don't handle the type</param>
            /// <returns>The value of the control</returns>
            public delegate float ControlValueReader(
                InputAction action, IInputAxisOwner.AxisDescriptor.Hints hint, UnityEngine.Object context,
                ControlValueReader defaultReader);
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

            /// <summary>Enable this if the input value is inherently dependent on frame time.
            /// For example, mouse deltas will naturally be bigger for longer frames, so
            /// should not normally be scaled by deltaTime.</summary>
            [Tooltip("Enable this if the input value is inherently dependent on frame time.  "
                + "For example, mouse deltas will naturally be bigger for longer frames, so "
                + "in this case the default deltaTime scaling should be canceled.")]
            public bool CancelDeltaTime = false;

            /// <inheritdoc />
            public float GetValue(
                UnityEngine.Object context,
                IInputAxisOwner.AxisDescriptor.Hints hint)
            {
                float inputValue = 0;
#if CINEMACHINE_UNITY_INPUTSYSTEM
                if (InputAction != null)
                {
                    if (context is CinemachineInputAxisController c)
                        inputValue = ResolveAndReadInputAction(c, hint) * Gain;
                }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
                if (inputValue == 0 && !string.IsNullOrEmpty(LegacyInput))
                {
                    try { inputValue = CinemachineCore.GetInputAxis(LegacyInput) * LegacyGain; }
                    catch (ArgumentException) {}
                    //catch (ArgumentException e) { Debug.LogError(e.ToString()); }
                }
#endif
                return (Time.deltaTime > 0 && CancelDeltaTime) ? inputValue / Time.deltaTime : inputValue;
            }

#if CINEMACHINE_UNITY_INPUTSYSTEM
            float ResolveAndReadInputAction(
                CinemachineInputAxisController context,
                IInputAxisOwner.AxisDescriptor.Hints hint)
            {
                // Resolve Action for player
                if (m_CachedAction != null && InputAction.action.id != m_CachedAction.id)
                    m_CachedAction = null;
                if (m_CachedAction == null)
                {
                    m_CachedAction = InputAction.action;
                    if (context.PlayerIndex != -1)
                        m_CachedAction = GetFirstMatch(InputUser.all[context.PlayerIndex], InputAction);
                    if (context.AutoEnableInputs && m_CachedAction != null)
                        m_CachedAction.Enable();

                    // local function to wrap the lambda which otherwise causes a tiny gc
                    static InputAction GetFirstMatch(in InputUser user, InputActionReference aRef)
                    {
                        var iter = user.actions.GetEnumerator();
                        while (iter.MoveNext())
                            if (iter.Current.id == aRef.action.id)
                                return iter.Current;
                        return null;
                    }
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
                if (m_CachedAction != null)
                {
                    // If client installed an override, use it
                    if (context.ReadControlValueOverride != null)
                        return context.ReadControlValueOverride.Invoke(m_CachedAction, hint, context, ReadInput);
                    return ReadInput(m_CachedAction, hint, context, null);
                }
                return 0;
            }

            /// <summary>
            /// Definition of how we read input. Override this in your child classes to specify
            /// the InputAction's type to read if it is different from float or Vector2.
            /// </summary>
            /// <param name="action">The action being read.</param>
            /// <param name="hint">The axis hint of the action.</param>
            /// <param name="context">The owner object, can be used for logging diagnostics</param>
            /// <param name="defaultReader">Not used</param>
            /// <returns>Returns the value of the input device.</returns>
            float ReadInput(
                InputAction action, IInputAxisOwner.AxisDescriptor.Hints hint,
                UnityEngine.Object context, ControlValueReader defaultReader)
            {
                if (action.activeValueType != null)
                {
                    if (action.activeValueType == typeof(Vector2))
                    {
                        var value = action.ReadValue<Vector2>();
                        return hint == IInputAxisOwner.AxisDescriptor.Hints.Y ? value.y : value.x;
                    }
                    if (action.activeValueType == typeof(float))
                        return action.ReadValue<float>();
            
                    Debug.LogError($"{context.name} - {action.name}: CinemachineInputAxisController.Reader can only read "
                        + "actions of type float or Vector2.  To read other types you can install a custom handler for "
                        + "CinemachineInputAxisController.ReadControlValueOverride.");
                }
                return 0f;
            }
#endif
        }
    }
}
