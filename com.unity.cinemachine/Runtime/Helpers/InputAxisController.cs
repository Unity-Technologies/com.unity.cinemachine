using System;
using System.Collections.Generic;
using UnityEngine;

#if CINEMACHINE_UNITY_INPUTSYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;
using System.Linq;
#endif

namespace Cinemachine
{
    /// <summary>
    /// This is a behaviour that is used to drive behaviours with IInputAxisSource interface, 
    /// which it discovers dynamically.  It is the bridge between the input system and 
    /// Cinemachine cameras that require user input.  Add it to a Cinemachine camera that needs it.
    /// </summary>
    [ExecuteAlways]
    [SaveDuringPlay]
    [AddComponentMenu("Cinemachine/Helpers/Cinemachine Input Axis Controller")]
    [HelpURL(Documentation.BaseURL + "manual/InputAxisController.html")]
    public class InputAxisController : MonoBehaviour
    {
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
        readonly List<IInputAxisSource.AxisDescriptor> m_Axes = new ();
        readonly List<IInputAxisSource> m_AxisOwners = new ();
        readonly List<IInputAxisResetSource> m_AxisResetters = new ();

        void OnValidate()
        {
            for (var i = 0; i < Controllers.Count; ++i)
                if (Controllers[i] != null)
                    Controllers[i].Control.Validate();
        }

        void Reset()
        {
#if CINEMACHINE_UNITY_INPUTSYSTEM
            PlayerIndex = -1;
            AutoEnableInputs = true;
#endif
            Controllers.Clear();
            CreateControllers();
        }

        void OnEnable()
        {
            CreateControllers();
        }

#if UNITY_EDITOR
        static List<IInputAxisSource> s_AxisTargetsCache = new List<IInputAxisSource>();
        internal bool ControllersAreValid()
        {
            s_AxisTargetsCache.Clear();
            GetComponentsInChildren(s_AxisTargetsCache);
            var count = s_AxisTargetsCache.Count;
            bool isValid = count == m_AxisOwners.Count;
            for (int i = 0; isValid && i < count; ++i)
                if (s_AxisTargetsCache[i] != m_AxisOwners[i])
                    isValid = false;
            return isValid;
        }
        internal void SynchronizeControllers() => CreateControllers();
#endif

        void OnDisable()
        {
            m_Axes.Clear();
            foreach (var t in m_AxisResetters)
                if ((t as UnityEngine.Object) != null)
                    t.UnregisterResetHandler(OnResetInput);
        }

        void CreateControllers()
        {
            m_Axes.Clear();
            m_AxisOwners.Clear();
            GetComponentsInChildren(m_AxisOwners);

            // Trim excess controllers
            for (int i = Controllers.Count - 1; i >= 0; --i)
                if (!m_AxisOwners.Contains(Controllers[i].Owner as IInputAxisSource))
                    Controllers.RemoveAt(i);

            // Rebuild the controller list, recycling existing ones to preserve the settings
            List<Controller> newControllers = new();
            foreach (var t in m_AxisOwners)
            {
                var startIndex = m_Axes.Count;
                t.GetInputAxes(m_Axes);
                for (int i = startIndex; i < m_Axes.Count; ++i)
                {
                    int controllerIndex = GetControllerIndex(Controllers, t, m_Axes[i].Name);
                    if (controllerIndex < 0)
                    {
                        var c = new Controller 
                        {
                            Enabled = true,
                            Name = m_Axes[i].Name,
                            Owner = t as UnityEngine.Object,
                        };
                        CreateDefaultControlForAxis(m_Axes[i], c);
                        newControllers.Add(c);
                    }
                    else
                    {
                        newControllers.Add(Controllers[controllerIndex]);
                        Controllers.RemoveAt(controllerIndex);
                    }
                }
            }
            Controllers = newControllers;

            // Rebuild the resetter list and register with them
            m_AxisResetters.Clear();
            GetComponentsInChildren(m_AxisResetters);
            foreach (var t in m_AxisResetters)
            {
                t.UnregisterResetHandler(OnResetInput);
                t.RegisterResetHandler(OnResetInput);
            }

            static int GetControllerIndex(List<Controller> list, IInputAxisSource owner, string axisName)
            {
                for (int i = 0; i < list.Count; ++i)
                    if (list[i].Owner as IInputAxisSource == owner && list[i].Name == axisName)
                        return i;
                return -1;
            }
        }

        /// <summary>
        /// Creates default controllers for an axis.
        /// Override this if the default axis controllers do not fit your axes.
        /// </summary>
        /// <param name="axis">Description of the axis whose default controller needs to be set.</param>
        /// <param name="controller">Controller to drive the axis.</param>
        protected virtual void CreateDefaultControlForAxis(
            in IInputAxisSource.AxisDescriptor axis, Controller controller)
        { 
            SetControlDefaults?.Invoke(axis, ref controller);
        }

        void OnResetInput()
        {
            for (int i = 0; i < Controllers.Count; ++i)
                Controllers[i].Driver.Reset(ref m_Axes[i].DrivenAxis());
        }

        void Update()
        {
            if (!Application.isPlaying)
                return;

            var deltaTime = Time.deltaTime;
            bool gotInput = false;
            for (int i = 0; i < Controllers.Count; ++i)
            {
                var c = Controllers[i];
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
                    var hint = i < m_Axes.Count ? m_Axes[i].Hint : 0;
                    var inputValue = ReadInputAction(c, hint) * c.Gain;
#if ENABLE_LEGACY_INPUT_MANAGER
                    if (legacyInputValue == 0 || inputValue != 0)
#endif
                        c.Control.InputValue = inputValue;
                }
#endif
                c.Driver.ProcessInput(deltaTime, ref m_Axes[i].DrivenAxis(), ref c.Control);
                gotInput |= Mathf.Abs(c.Control.InputValue) > 0.001f;
            }
        }

        /// <summary>Delegate for overriding the legacy input system with custom code</summary>
        public delegate float GetInputAxisValueDelegate(string axisName);

        /// <summary>Implement this delegate to locally override the legacy input system call</summary>
        public GetInputAxisValueDelegate GetInputAxisValue = ReadLegacyInput;
        
        static float ReadLegacyInput(string axisName)
        {
            float value = 0;
#if ENABLE_LEGACY_INPUT_MANAGER
            {
                try { value = CinemachineCore.GetInputAxis(axisName); }
                catch (ArgumentException) {}
                //catch (ArgumentException e) { Debug.LogError(e.ToString()); }
            }
#endif
            return value;
        }

        internal delegate void SetControlDefaultsForAxis(
            in IInputAxisSource.AxisDescriptor axis, ref Controller controller);
        internal static SetControlDefaultsForAxis SetControlDefaults;

#if CINEMACHINE_UNITY_INPUTSYSTEM
        /// <summary>
        /// Definition of how we read input. Override this in your child classes to specify
        /// the InputAction's type to read if it is different from float or Vector2.
        /// </summary>
        /// <param name="action">The action being read.</param>
        /// <param name="hint">The axis hint of the action.</param>
        /// <returns>Returns the value of the input device.</returns>
        protected virtual float ReadInput(InputAction action, IInputAxisSource.AxisDescriptor.Hints hint)
        {
#if true
            // GML Temporary fix until this issue is sorted out
            switch (hint)
            {
                case IInputAxisSource.AxisDescriptor.Hints.X: return action.ReadValue<Vector2>().x;
                case IInputAxisSource.AxisDescriptor.Hints.Y: return action.ReadValue<Vector2>().y;
                default: return action.ReadValue<float>();
            }
#else
            var activeControl = action.activeControl;
            if (activeControl == null)
                return 0f;
            
            var actionControlType = activeControl.valueType;
            if (actionControlType == typeof(float))
                return action.ReadValue<float>();
            if (actionControlType == typeof(Vector2))
                return hint == IInputAxisSource.AxisDescriptor.Hints.Y
                    ? action.ReadValue<Vector2>().y
                    : action.ReadValue<Vector2>().x;

            Debug.LogError("The valueType of InputAction provided to " + name + " is not handled by default. " +
                "You need to create a class inheriting InputAxisController and you need to override the " +
                "ReadInput method to handle your case.");
            return 0f;
#endif
        }
        
        float ReadInputAction(Controller c, IInputAxisSource.AxisDescriptor.Hints hint)
        {
            ResolveActionForPlayer(c, PlayerIndex);

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

        void ResolveActionForPlayer(Controller c, int playerIndex)
        {
            if (c.m_CachedAction != null && c.InputAction.action.id != c.m_CachedAction.id)
                c.m_CachedAction = null;
            
            if (c.m_CachedAction == null)
            {
                c.m_CachedAction = c.InputAction.action;
                if (playerIndex != -1)
                    c.m_CachedAction = GetFirstMatch(InputUser.all[playerIndex], c.InputAction);
                if (AutoEnableInputs && c.m_CachedAction != null)
                    c.m_CachedAction.Enable();
            }

            // local function to wrap the lambda which otherwise causes a tiny gc
            InputAction GetFirstMatch(in InputUser user, InputActionReference aRef) => 
                user.actions.First(x => x.id == aRef.action.id);
        }
#endif
    }
}

