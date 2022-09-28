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
            /// <summary>Setting that control the way the input axis responds to user input</summary>
            [HideFoldout]
            public InputAxisControl Control;

            /// <summary>Controls automatic recentering of axis value.</summary>
            [FoldoutWithEnabledButton]
            public InputAxisRecenteringSettings Recentering;

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
        public List<Controller> Controllers = new List<Controller>();

        /// <summary>
        /// Axes are dynamically discovered by querying behaviours implementing <see cref="IInputAxisSource"/>
        /// </summary>
        List<IInputAxisSource.AxisDescriptor> m_Axes = new List<IInputAxisSource.AxisDescriptor>();
        List<IInputAxisSource> m_AxisOwners = new List<IInputAxisSource>();
        List<IInputAxisResetSource> m_AxisResetters = new List<IInputAxisResetSource>();

        void OnValidate()
        {
            for (var i = 0; i < Controllers.Count; ++i)
            {
                Controllers[i].Control.Validate();
                Controllers[i].Recentering.Validate();
            }
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
        internal bool ConrollersAreValid()
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
                        newControllers.Add(CreateDefaultControlForAxis(i, t));
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

        void OnResetInput()
        {
            for (int i = 0; i < Controllers.Count; ++i)
                Controllers[i].Driver.Reset(m_Axes[i].Axis, Controllers[i].Recentering);
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
#if ENABLE_LEGACY_INPUT_MANAGER
                if (!string.IsNullOrEmpty(c.LegacyInput) && GetInputAxisValue != null)
                    c.Control.InputValue = GetInputAxisValue(c.LegacyInput) * c.LegacyGain;
#endif
#if CINEMACHINE_UNITY_INPUTSYSTEM
                if (c.InputAction != null && c.InputAction.action != null)
                {
                    var axis = i < m_Axes.Count ? m_Axes[i].AxisIndex : 0;
                    c.Control.InputValue = ReadInputAction(c, axis) * c.Gain;
                }
#endif
                c.Driver.ProcessInput(deltaTime, m_Axes[i].Axis, ref c.Control);
                gotInput |= Mathf.Abs(c.Control.InputValue) > 0.001f;
            }

            // Do recentering
            for (int i = 0; i < Controllers.Count; ++i)
            {
                // If we got any input, cancel all recentering
                if (gotInput && Controllers[i].Recentering.Wait > 0.01f)
                    Controllers[i].Driver.CancelRecentering();
                Controllers[i].Driver.DoRecentering(deltaTime, m_Axes[i].Axis, Controllers[i].Recentering);
            }
        }

        Controller CreateDefaultControlForAxis(int axisIndex, IInputAxisSource owner)
        {
            var c = new Controller 
            {
                Name = m_Axes[axisIndex].Name,
                Owner = owner as UnityEngine.Object,
            };
            SetControlDefaults?.Invoke(m_Axes[axisIndex], ref c);
            return c;
        }

        /// <summary>Delegate for overriding the legacy input system with custom code</summary>
        public delegate float GetInputAxisValueDelegate(string axisName);

        /// <summary>Implement this delegate to locally override the legacy input system call</summary>
        public GetInputAxisValueDelegate GetInputAxisValue = ReadLegacyInput;

        /// <summary>
        /// Call this to trigger recentering immediately, regardless of whether recentering 
        /// is enabled or the wait time has elapsed.
        /// </summary>
        public void RecenterNow()
        {
            for (var i = 0; i < Controllers.Count; ++i)
                Controllers[i].Driver.RecenterNow();
        }

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
        float ReadInputAction(Controller c, int axis)
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

            if (c.m_CachedAction != null)
            {
                switch (axis)
                {
                    case 0: return c.m_CachedAction.ReadValue<Vector2>().x;
                    case 1: return c.m_CachedAction.ReadValue<Vector2>().y;
                    case 2: return c.m_CachedAction.ReadValue<float>();
                }
            }
            return 0;
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

