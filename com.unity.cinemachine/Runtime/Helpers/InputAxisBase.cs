using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Users;

namespace Cinemachine
{
    public abstract class InputAxisBase : MonoBehaviour
    {
        [SerializeField] protected InputAxisData m_InputAxisData;

        internal InputAxisData InputAxisData
        {
            get { return m_InputAxisData; }
        }
        void OnValidate()
        {
            m_InputAxisData.Validate();
        }

        void Reset()
        {
            m_InputAxisData.Reset(gameObject, OnResetInput);
        }

        void OnEnable()
        {
            m_InputAxisData.CreateControllers(gameObject, OnResetInput);
        }

        void OnDisable()
        {
            m_InputAxisData.Disable(OnResetInput);
        }
        
        protected void OnResetInput()
        {
            m_InputAxisData.OnResetInput();
        }
        
#if UNITY_EDITOR
        static List<IInputAxisSource> s_AxisTargetsCache = new List<IInputAxisSource>();
        internal bool ControllersAreValid()
        {
            s_AxisTargetsCache.Clear();
            GetComponentsInChildren(s_AxisTargetsCache);
            var count = s_AxisTargetsCache.Count;
            bool isValid = count == m_InputAxisData.m_AxisOwners.Count;
            for (int i = 0; isValid && i < count; ++i)
                if (s_AxisTargetsCache[i] != m_InputAxisData.m_AxisOwners[i])
                    isValid = false;
            return isValid;
        }
        internal void SynchronizeControllers() => m_InputAxisData.CreateControllers(gameObject, OnResetInput);
#endif
    }
    
        public static class InputAxisBuilder
    {
        internal delegate void SetControlDefaultsForAxis(
            in IInputAxisSource.AxisDescriptor axis, ref Controller controller);
        internal static SetControlDefaultsForAxis SetControlDefaults;
        
        public static void Validate(this InputAxisData inputAxisData)
        {
            if (inputAxisData == null)
                inputAxisData = new InputAxisData();
            for (var i = 0; i < inputAxisData.Controllers.Count; ++i)
                if (inputAxisData.Controllers[i] != null)
                    inputAxisData.Controllers[i].Control.Validate();
        }

        public static void Update(this InputAxisData inputAxisData)
        {
            var deltaTime = Time.deltaTime;
            bool gotInput = false;
            for (int i = 0; i < inputAxisData.Controllers.Count; ++i)
            {
                var c = inputAxisData.Controllers[i];
                if (!c.Enabled)
                    continue;
#if ENABLE_LEGACY_INPUT_MANAGER
                float legacyInputValue = 0;
                if (!string.IsNullOrEmpty(c.LegacyInput) && GetInputAxisValue != null)
                    legacyInputValue = c.Control.InputValue = GetInputAxisValue(c.LegacyInput) * c.LegacyGain;
#endif
#if CINEMACHINE_UNITY_INPUTSYSTEM
                if (c.InputAction != null && c.InputAction.action != null)
                {
                    var hint = i < inputAxisData.m_Axes.Count ? inputAxisData.m_Axes[i].Hint : 0;
                    var inputValue = inputAxisData.ReadInputAction(c, hint) * c.Gain;
#if ENABLE_LEGACY_INPUT_MANAGER
                    if (legacyInputValue == 0 || inputValue != 0)
#endif
                    c.Control.InputValue = inputValue;
                }
#endif
                c.Driver.ProcessInput(deltaTime, ref inputAxisData.m_Axes[i].DrivenAxis(), ref c.Control);
                gotInput |= Mathf.Abs(c.Control.InputValue) > 0.001f;
            }
        }
        
        
        static void ResolveActionForPlayer(this InputAxisData inputAxisData, Controller c, int playerIndex)
        {
            if (c.m_CachedAction != null && c.InputAction.action.id != c.m_CachedAction.id)
                c.m_CachedAction = null;
            
            if (c.m_CachedAction == null)
            {
                c.m_CachedAction = c.InputAction.action;
                if (playerIndex != -1)
                    c.m_CachedAction = GetFirstMatch(InputUser.all[playerIndex], c.InputAction);
                if (inputAxisData.AutoEnableInputs && c.m_CachedAction != null)
                    c.m_CachedAction.Enable();
            }

            // local function to wrap the lambda which otherwise causes a tiny gc
            InputAction GetFirstMatch(in InputUser user, InputActionReference aRef) => 
                user.actions.First(x => x.id == aRef.action.id);
        }
        
        public static float ReadInputAction(this InputAxisData inputAxisData, Controller c, IInputAxisSource.AxisDescriptor.Hints hint)
        {
            ResolveActionForPlayer(inputAxisData, c, inputAxisData.PlayerIndex);

            // Update enabled status
            if (c.m_CachedAction != null && c.m_CachedAction.enabled != c.InputAction.action.enabled)
            {
                if (c.InputAction.action.enabled)
                    c.m_CachedAction.Enable();
                else
                    c.m_CachedAction.Disable();
            }

            return c.m_CachedAction != null ? ReadInput(c.m_CachedAction, hint) : 0f;
        }
        
        /// <summary>
        /// Definition of how we read input. Override this in your child classes to specify
        /// the InputAction's type to read if it is different from float or Vector2.
        /// </summary>
        /// <param name="action">The action being read.</param>
        /// <param name="hint">The axis hint of the action.</param>
        /// <returns>Returns the value of the input device.</returns>
        static float ReadInput(InputAction action, IInputAxisSource.AxisDescriptor.Hints hint)
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
                        return hint == IInputAxisSource.AxisDescriptor.Hints.Y ? value.y : value.x;
                    }
                    // Default: assume type is float
                    return action.ReadValue<float>(); 
                }
                catch (InvalidOperationException)
                {
                    Debug.LogError("An action is mapped to a "
                                   + control.valueType.Name + " control.  The default inmplementation of "
                                   + "InputAxisController.ReadInput can only handle float or Vector2 types. "
                                   + "To handle other types you can create a class inheriting "
                                   + "InputAxisController with the ReadInput method overridden.");
                }
            }
            return 0f;
        }

        public static void Reset(this InputAxisData inputAxisData, GameObject gameObject, IInputAxisResetSource.ResetHandler resetHandler)
        {
            if (inputAxisData == null)
                inputAxisData = new InputAxisData();
#if CINEMACHINE_UNITY_INPUTSYSTEM
            inputAxisData.PlayerIndex = -1;
            inputAxisData.AutoEnableInputs = true;
#endif
            inputAxisData.Controllers.Clear();
            CreateControllers(inputAxisData, gameObject, resetHandler);
        }

        public static void Disable(this InputAxisData inputAxisData, IInputAxisResetSource.ResetHandler resetHandler)
        {
            foreach (var t in inputAxisData.m_AxisResetters)
                if ((t as UnityEngine.Object) != null)
                    t.UnregisterResetHandler(resetHandler);
            inputAxisData.m_Axes.Clear();
            inputAxisData.m_AxisOwners.Clear();
            inputAxisData.m_AxisResetters.Clear();
        }

        public static void OnResetInput(this InputAxisData inputAxisData)
        {
            for (int i = 0; i < inputAxisData.Controllers.Count; ++i)
                inputAxisData.Controllers[i].Driver.Reset(ref inputAxisData.m_Axes[i].DrivenAxis());
        }
        
        public static void CreateDefaultControlForAxis(in IInputAxisSource.AxisDescriptor axis, Controller controller)
        { 
            SetControlDefaults?.Invoke(axis, ref controller);
        }

        public static void CreateControllers(this InputAxisData inputAxisData, GameObject gameObject, IInputAxisResetSource.ResetHandler resetHandler)
        {
            if (inputAxisData == null)
                inputAxisData = new InputAxisData();
            inputAxisData.m_Axes.Clear();
            inputAxisData.m_AxisOwners.Clear();
            gameObject.GetComponentsInChildren(inputAxisData.m_AxisOwners);

            // Trim excess controllers
            for (int i = inputAxisData.Controllers.Count - 1; i >= 0; --i)
                if (!inputAxisData.m_AxisOwners.Contains(inputAxisData.Controllers[i].Owner as IInputAxisSource))
                    inputAxisData.Controllers.RemoveAt(i);

            // Rebuild the controller list, recycling existing ones to preserve the settings
            List<Controller> newControllers = new();
            foreach (var t in inputAxisData.m_AxisOwners)
            {
                var startIndex = inputAxisData.m_Axes.Count;
                t.GetInputAxes(inputAxisData.m_Axes);
                for (int i = startIndex; i < inputAxisData.m_Axes.Count; ++i)
                {
                    int controllerIndex = GetControllerIndex(inputAxisData.Controllers, t, inputAxisData.m_Axes[i].Name);
                    if (controllerIndex < 0)
                    {
                        var c = new Controller 
                        {
                            Enabled = true,
                            Name = inputAxisData.m_Axes[i].Name,
                            Owner = t as UnityEngine.Object,
                        };
                        CreateDefaultControlForAxis(inputAxisData.m_Axes[i], c);
                        newControllers.Add(c);
                    }
                    else
                    {
                        newControllers.Add(inputAxisData.Controllers[controllerIndex]);
                        inputAxisData.Controllers.RemoveAt(controllerIndex);
                    }
                }
            }
            inputAxisData.Controllers = newControllers;

            // Rebuild the resetter list and register with them
            inputAxisData.m_AxisResetters.Clear();
            gameObject.GetComponentsInChildren(inputAxisData.m_AxisResetters);
            foreach (var t in inputAxisData.m_AxisResetters)
            {
                t.UnregisterResetHandler(resetHandler);
                t.RegisterResetHandler(resetHandler);
            }

            static int GetControllerIndex(List<Controller> list, IInputAxisSource owner, string axisName)
            {
                for (int i = 0; i < list.Count; ++i)
                    if (list[i].Owner as IInputAxisSource == owner && list[i].Name == axisName)
                        return i;
                return -1;
            }
        }
    }
    
    [Serializable]
    public class InputAxisData
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
        public bool AutoEnableInputs;
#endif

        /// <summary>This list is dynamically populated based on the discovered axes</summary>
        public List<Controller> Controllers = new ();

        /// <summary>
        /// Axes are dynamically discovered by querying behaviours implementing <see cref="IInputAxisSource"/>
        /// </summary>
        readonly internal List<IInputAxisSource.AxisDescriptor> m_Axes = new ();
        readonly internal List<IInputAxisSource> m_AxisOwners = new ();
        readonly internal List<IInputAxisResetSource> m_AxisResetters = new ();
    }
    
    /// <summary>
        /// Each discovered axis will get a Controller to drive it in Update().
        /// </summary>
        [Serializable]
        public class Controller
        {
            /// <summary>Identifies this axis in the inspector</summary>
            [HideInInspector] public string Name;

            /// <summary>Identifies this owner of the axis controlled by this controller</summary>
            [HideInInspector] public UnityEngine.Object Owner;

            /// <summary>
            /// When enabled, this controller will drive the input axis.
            /// </summary>
            [Tooltip("When enabled, this controller will drive the input axis")]
            public bool Enabled = true;

#if CINEMACHINE_UNITY_INPUTSYSTEM
            /// <summary>Action for the Input package (if used).</summary>
            [Tooltip("Action for the Input package (if used).")]
            public InputActionReference InputAction;

            /// <summary>The input value is multiplied by this amount prior to processing.
            /// Controls the input power.  Set it to a negative value to invert the input</summary>
            [Tooltip("The input value is multiplied by this amount prior to processing.  "
                + "Controls the input power.  Set it to a negative value to invert the input")]
            public float Gain;

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
            public float LegacyGain;
#endif

            /// <summary>Setting that control the way the input axis responds to user input</summary>
            [HideFoldout]
            public InputAxisControl Control;

            /// <summary>This object drives the axis value based on the control 
            /// and recentering settings</summary>
            internal InputAxisDriver Driver;
        }

}
