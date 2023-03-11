using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Cinemachine
{
    /// <summary>
    /// This interface identifies a behaviour that can drive IInputAxisOwners.
    /// </summary>
    public interface IInputAxisController {}

    /// <summary>
    /// This is a base class for a behaviour that is used to drive IInputAxisOwner behviours, 
    /// which it discovers dynamically.  It is the bridge between the input system and 
    /// Cinemachine cameras that require user input.  Add it to a Cinemachine camera that needs it.
    /// If you want to read inputs from a third-party source, then you must specialize this class 
    /// with an appropriate implementation of IInputAxisReader.
    /// </summary>
    [ExecuteAlways]
    [SaveDuringPlay]
    public abstract class InputAxisControllerBase<T> : MonoBehaviour, IInputAxisController where T : IInputAxisReader, new ()
    {
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

            /// <summary>The input axis reader to read the value from the user</summary>
            [HideFoldout]
            public T Input;

            /// <summary>The current value of the input</summary>
            public float InputValue;

            /// <summary>Drives the input axis value based on input value</summary>
            [HideFoldout]
            public DefaultInputAxisDriver Driver;
        }
        
        /// <summary>This list is dynamically populated based on the discovered axes</summary>
        public List<Controller> Controllers = new ();

        /// <summary>
        /// Axes are dynamically discovered by querying behaviours implementing <see cref="IInputAxisOwner"/>
        /// </summary>
        readonly List<IInputAxisOwner.AxisDescriptor> m_Axes = new ();
        readonly List<IInputAxisOwner> m_AxisOwners = new ();
        readonly List<IInputAxisResetSource> m_AxisResetters = new ();

        void OnValidate()
        {
            for (var i = 0; i < Controllers.Count; ++i)
                if (Controllers[i] != null)
                    Controllers[i].Driver.Validate();
        }

        void Reset()
        {
            Controllers.Clear();
            CreateControllers();
            PlayerIndex = -1;
            AutoEnableInputs = true;
        }

        void OnEnable()
        {
            CreateControllers();
        }

        void OnDisable()
        {
            foreach (var t in m_AxisResetters)
                if ((t as UnityEngine.Object) != null)
                    t.UnregisterResetHandler(OnResetInput);
            m_Axes.Clear();
            m_AxisOwners.Clear();
            m_AxisResetters.Clear();
        }

#if UNITY_EDITOR
        static List<IInputAxisOwner> s_AxisTargetsCache = new ();
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

        void CreateControllers()
        {
            m_Axes.Clear();
            m_AxisOwners.Clear();
            GetComponentsInChildren(m_AxisOwners);

            // Trim excess controllers
            for (int i = Controllers.Count - 1; i >= 0; --i)
                if (!m_AxisOwners.Contains(Controllers[i].Owner as IInputAxisOwner))
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
                            Input = new T()
                        };
                        InitializeControllerDefaultsForAxis(m_Axes[i], c);
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
            if (enabled)
            {
                GetComponentsInChildren(m_AxisResetters);
                foreach (var t in m_AxisResetters)
                {
                    t.UnregisterResetHandler(OnResetInput);
                    t.RegisterResetHandler(OnResetInput);
                }
            }
            static int GetControllerIndex(List<Controller> list, IInputAxisOwner owner, string axisName)
            {
                for (int i = 0; i < list.Count; ++i)
                    if (list[i].Owner as IInputAxisOwner == owner && list[i].Name == axisName)
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
        protected virtual void InitializeControllerDefaultsForAxis(
            in IInputAxisOwner.AxisDescriptor axis, Controller controller)
        { 
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
            //bool gotInput = false;
            for (int i = 0; i < Controllers.Count; ++i)
            {
                var c = Controllers[i];
                if (!c.Enabled || c.Input == null)
                    continue;
                var hint = i < m_Axes.Count ? m_Axes[i].Hint : 0;
                if (c.Input != null)
                    c.InputValue = c.Input.GetValue(this, PlayerIndex, AutoEnableInputs, hint);
                c.Driver.ProcessInput(ref m_Axes[i].DrivenAxis(), c.InputValue, deltaTime);
                //gotInput |= Mathf.Abs(c.InputValue) > 0.001f;
            }
            // GML todo: handle synching of recentering across multiple axes
        }
    }
}

