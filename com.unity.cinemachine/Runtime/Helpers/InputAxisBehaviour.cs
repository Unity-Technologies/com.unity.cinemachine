using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

namespace Cinemachine
{
    public abstract class InputAxisBehaviour<T>: MonoBehaviour where T: IController<T>, new()
    {
        [SerializeField] protected InputAxisData<T> m_InputAxisData;
        internal InputAxisData<T> InputAxisData
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
            m_InputAxisData.DestroyControllers(OnResetInput);
        }
        
        protected void OnResetInput()
        {
            m_InputAxisData.OnResetInput();
        }
        
        void Update()
        {
            if (!Application.isPlaying)
                return;
            
            m_InputAxisData.Update();
        }
        
#if UNITY_EDITOR
        static List<IInputAxisSource> m_AxisTargetsCache = new List<IInputAxisSource>();
        internal bool ControllersAreValid()
        {
            m_AxisTargetsCache.Clear();
            GetComponentsInChildren(m_AxisTargetsCache);
            var count = m_AxisTargetsCache.Count;
            bool isValid = count == m_InputAxisData.m_AxisOwners.Count;
            for (int i = 0; isValid && i < count; ++i)
                if (m_AxisTargetsCache[i] != m_InputAxisData.m_AxisOwners[i])
                    isValid = false;
            return isValid;
        }
        internal void SynchronizeControllers() => m_InputAxisData.CreateControllers(gameObject, OnResetInput);
#endif
    }

    internal static class InputAxisEvents<T> where T: IController<T>, new()
    {
        internal delegate void SetControlDefaultsForAxis(in IInputAxisSource.AxisDescriptor axis, IController<T> controller);
        internal static SetControlDefaultsForAxis SetControlDefaults;   
    }
    
    public static class InputAxisBuilder
    {
        public static void Validate<T>(this InputAxisData<T> inputAxisData) where T: IController<T>, new()
        {
            if (inputAxisData == null)
                inputAxisData = new InputAxisData<T>();
            for (var i = 0; i < inputAxisData.Controllers.Count; ++i)
                if (inputAxisData.Controllers[i] != null)
                    inputAxisData.Controllers[i].control.Validate();
        }
        
                    
        public static float ReadInput(this InputAction action, IInputAxisSource.AxisDescriptor.Hints hint)
        {
            var actionActiveControl = action.activeControl;
            if (actionActiveControl != null)
            {
                try 
                {
                    // If we can read as a Vector2, do so
                    if (actionActiveControl.valueType == typeof(Vector2) || action.expectedControlType == "Vector2")
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
                                   + actionActiveControl.valueType.Name + " control.  The default inmplementation of "
                                   + "InputAxisController.ReadInput can only handle float or Vector2 types. "
                                   + "To handle other types you can create a class inheriting "
                                   + "InputAxisController with the ReadInput method overridden.");
                }
            }
            return 0f;
        }

        public static void Update<T>(this InputAxisData<T> inputAxisData) where T: IController<T>, new()
        {
            var deltaTime = Time.deltaTime;
            bool gotInput = false;
            for (int i = 0; i < inputAxisData.Controllers.Count; ++i)
            {
                var c = inputAxisData.Controllers[i];
                if (!c.enabled)
                    continue;
#if ENABLE_LEGACY_INPUT_MANAGER
                float legacyInputValue = 0;
                if (!string.IsNullOrEmpty(c.LegacyInput) && GetInputAxisValue != null)
                    legacyInputValue = c.Control.InputValue = GetInputAxisValue(c.LegacyInput) * c.LegacyGain;
#endif
#if CINEMACHINE_UNITY_INPUTSYSTEM
                if (c.IsValid())
                {
                    var hint = i < inputAxisData.m_Axes.Count ? inputAxisData.m_Axes[i].Hint : 0;
                    var inputValue = inputAxisData.ReadInputAction(c, hint) * c.gain;
#if ENABLE_LEGACY_INPUT_MANAGER
                    if (legacyInputValue == 0 || inputValue != 0)
#endif
                    c.control.InputValue = inputValue;
                }
#endif
                c.driver.ProcessInput(deltaTime, ref inputAxisData.m_Axes[i].DrivenAxis(), ref c.control);
                gotInput |= Mathf.Abs(c.control.InputValue) > 0.001f;
            }
        }

        public static float ReadInputAction<T>(this InputAxisData<T> inputAxisData, T c, IInputAxisSource.AxisDescriptor.Hints hint) where T : IController<T>, new()
        {
            return c.Read(hint);
        }

        public static void Reset<T>(this InputAxisData<T> inputAxisData, GameObject gameObject, IInputAxisResetSource.ResetHandler resetHandler) where T : IController<T>, new()
        {
            if (inputAxisData == null)
                inputAxisData = new InputAxisData<T>();
#if CINEMACHINE_UNITY_INPUTSYSTEM
            inputAxisData.PlayerIndex = -1;
            inputAxisData.AutoEnableInputs = true;
#endif
            inputAxisData.Controllers.Clear();
            CreateControllers(inputAxisData, gameObject, resetHandler);
        }

        public static void DestroyControllers<T>(this InputAxisData<T> inputAxisData, IInputAxisResetSource.ResetHandler resetHandler) where T : IController<T>, new()
        {
            foreach (var t in inputAxisData.m_AxisResetters)
                if ((t as UnityEngine.Object) != null)
                    t.UnregisterResetHandler(resetHandler);
            inputAxisData.m_Axes.Clear();
            inputAxisData.m_AxisOwners.Clear();
            inputAxisData.m_AxisResetters.Clear();
        }

        public static void OnResetInput<T>(this InputAxisData<T> inputAxisData) where T : IController<T>, new()
        {
            for (int i = 0; i < inputAxisData.Controllers.Count; ++i)
                inputAxisData.Controllers[i].driver.Reset(ref inputAxisData.m_Axes[i].DrivenAxis());
        }
        
        public static void CreateDefaultControlForAxis<T>(in IInputAxisSource.AxisDescriptor axis, T controller) where T : IController<T>, new()
        {
            InputAxisEvents<T>.SetControlDefaults?.Invoke(axis, controller);
        }

        public static void CreateControllers<T>(this InputAxisData<T> inputAxisData, GameObject gameObject, IInputAxisResetSource.ResetHandler resetHandler) where T : IController<T>, new()
        {
            if (inputAxisData == null)
                inputAxisData = new InputAxisData<T>();
            inputAxisData.m_Axes.Clear();
            inputAxisData.m_AxisOwners.Clear();
            gameObject.GetComponentsInChildren(inputAxisData.m_AxisOwners);

            // Trim excess controllers
            for (int i = inputAxisData.Controllers.Count - 1; i >= 0; --i)
                if (!inputAxisData.m_AxisOwners.Contains(inputAxisData.Controllers[i].owner as IInputAxisSource))
                    inputAxisData.Controllers.RemoveAt(i);

            // Rebuild the controller list, recycling existing ones to preserve the settings
            List<T> newControllers = new();
            foreach (var t in inputAxisData.m_AxisOwners)
            {
                var startIndex = inputAxisData.m_Axes.Count;
                t.GetInputAxes(inputAxisData.m_Axes);
                for (int i = startIndex; i < inputAxisData.m_Axes.Count; ++i)
                {
                    int controllerIndex = GetControllerIndex(inputAxisData.Controllers, t, inputAxisData.m_Axes[i].Name);
                    if (controllerIndex < 0)
                    {
                        var c = new T 
                        {
                            enabled = true,
                            name = inputAxisData.m_Axes[i].Name,
                            owner = t as UnityEngine.Object,
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

            static int GetControllerIndex<T>(List<T> list, IInputAxisSource owner, string axisName) where T: IController<T>
            {
                for (int i = 0; i < list.Count; ++i)
                    if (list[i].owner as IInputAxisSource == owner && list[i].name == axisName)
                        return i;
                return -1;
            }
        }
    }
    
    [Serializable]
    public class InputAxisData <T> where T : IController<T>, new()
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
        public List<T> Controllers = new ();

        /// <summary>
        /// Axes are dynamically discovered by querying behaviours implementing <see cref="IInputAxisSource"/>
        /// </summary>
        readonly internal List<IInputAxisSource.AxisDescriptor> m_Axes = new ();
        readonly internal List<IInputAxisSource> m_AxisOwners = new ();
        readonly internal List<IInputAxisResetSource> m_AxisResetters = new ();
    }
    
    [Serializable]
    public abstract class LazyController
    {
        /// <summary>Identifies this axis in the inspector</summary>
        [SerializeField][HideInInspector] private string Name;
        
        public string name
        {
            set { Name = value;}
            get { return Name; }
        }

        /// <summary>Identifies this owner of the axis controlled by this controller</summary>
        [SerializeField][HideInInspector] private UnityEngine.Object Owner;

        public UnityEngine.Object owner
        {
            set { Owner = value;}
            get { return Owner; }
        }
            
        /// <summary>
        /// When enabled, this controller will drive the input axis.
        /// </summary>
        [Tooltip("When enabled, this controller will drive the input axis")][SerializeField]
        private bool Enabled = true;

        public bool enabled
        {
            set { Enabled = value; }
            get { return Enabled; }
        }
        
        /// <summary>The input value is multiplied by this amount prior to processing.
        /// Controls the input power.  Set it to a negative value to invert the input</summary>
        [SerializeField][Tooltip("The input value is multiplied by this amount prior to processing.  "
                                 + "Controls the input power.  Set it to a negative value to invert the input")]
        private float Gain;

        public float gain
        {
            get { return Gain; }
            set => Gain = value;
        }
        
        /// <summary>Setting that control the way the input axis responds to user input</summary>
        [SerializeField][HideFoldout]
        private InputAxisControl Control;

        public ref InputAxisControl control
        {
            get { return ref Control; }
        }
        
        /// <summary>This object drives the axis value based on the control 
        /// and recentering settings</summary>
        internal InputAxisDriver Driver;
            
        public ref InputAxisDriver driver
        {
            get { return ref Driver; }
        }
    }
    
    public interface IController<T>
    {
        string name
        {
            set;
            get;
        }

        bool enabled
        {
            set;
            get;
        }

        UnityEngine.Object owner
        {
            set;
            get;
        }

        float gain
        {
            get;
        }

        ref InputAxisControl control
        {
            get;
        }
        
        ref InputAxisDriver driver
        {
            get;
        }

        float Read(IInputAxisSource.AxisDescriptor.Hints hint);

        public bool IsValid();
    }
}
