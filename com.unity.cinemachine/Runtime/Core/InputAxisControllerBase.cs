#define LISTVIEW_BUG_WORKAROUND // GML hacking because of another ListView bug

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Unity.Cinemachine
{
    /// <summary>
    /// This interface identifies a behaviour that can drive IInputAxisOwners.
    /// </summary>
    public interface IInputAxisController 
    {
        /// <summary>
        /// Called by editor only.  Normally we should have one controller per 
        /// IInputAxisOwner axis.  This will scan the object for IInputAxisOwner
        /// behaviours, create missing controllers (in their 
        /// default state), and remove any that are no longer relevant.
        /// </summary>
        void SynchronizeControllers();

#if UNITY_EDITOR
        /// <summary>
        /// Available in Editor only.  Used to check if a controller synchronization is necessary.
        /// Normally we should have one controller per IInputAxisOwner axis.
        /// </summary>
        /// <returns>True if there is one controller defined per IInputAxisOwner axis, 
        /// false if there is a mismatch</returns>
        bool ControllersAreValid();
#endif
    }

    /// <summary>
    /// This is a base class for a behaviour that is used to drive IInputAxisOwner behaviours, 
    /// which it discovers dynamically.  It is the bridge between the input system and 
    /// Cinemachine cameras that require user input.  Add it to a Cinemachine camera that needs it.
    /// If you want to read inputs from a third-party source, then you must specialize this class 
    /// with an appropriate implementation of IInputAxisReader.
    /// </summary>
    /// <typeparam name="T">The axis reader that will read the inputs.</typeparam>
    [ExecuteAlways]
    [SaveDuringPlay]
    public abstract class InputAxisControllerBase<T> : MonoBehaviour, IInputAxisController where T : IInputAxisReader, new ()
    {
        /// <summary>If set, a recursive search for IInputAxisOwners behaviours will be performed.  
        /// Otherwise, only behaviours attached directly to this GameObject will be considered, 
        /// and child objects will be ignored.</summary>
        [Tooltip("If set, a recursive search for IInputAxisOwners behaviours will be performed.  "
            + "Otherwise, only behaviours attached directly to this GameObject will be considered, "
            + "and child objects will be ignored")]
        public bool ScanRecursively = true;
        
        /// <summary>If set, input will not be processed while the Cinemachine Camera is 
        /// participating in a blend.</summary>
        [HideIfNoComponent(typeof(CinemachineVirtualCameraBase))]
        [Tooltip("If set, input will not be processed while the Cinemachine Camera is "
            + "participating in a blend.")]
        public bool SuppressInputWhileBlending = true;

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

#if LISTVIEW_BUG_WORKAROUND
        // This class is a hack to work around a ListView bug
        [Serializable] internal class ControllerList { public List<Controller> Controllers = new (); }

        /// <summary>This list is dynamically populated based on the discovered axes</summary>
        [Header("Driven Axes")]
        [InputAxisControllerList]
        [SerializeField] internal ControllerList m_ControllerList = new ();

        // GML hacking: Legacy support until our sample scenes are all upgraded
        [SerializeField, HideInInspector, FormerlySerializedAs("Controllers")] List<Controller> m_LegacyControllers;

        /// <summary>This list is dynamically populated based on the discovered axes</summary>
        public List<Controller> Controllers => m_ControllerList.Controllers;
#else
        /// <summary>This list is dynamically populated based on the discovered axes</summary>
        [Header("Driven Axes")]
        [InputAxisControllerList]
        public List<Controller> Controllers = new ();
#endif
        /// <summary>
        /// Axes are dynamically discovered by querying behaviours implementing <see cref="IInputAxisOwner"/>
        /// </summary>
        readonly List<IInputAxisOwner.AxisDescriptor> m_Axes = new ();
        readonly List<IInputAxisOwner> m_AxisOwners = new ();
        readonly List<IInputAxisResetSource> m_AxisResetters = new ();


        /// <summary>Editor only: Called by Unity when the component is serialized 
        /// or the inspector is changed.</summary>
        protected virtual void OnValidate()
        {
            for (var i = 0; i < Controllers.Count; ++i)
                if (Controllers[i] != null)
                    Controllers[i].Driver.Validate();
        }

        /// <summary>Called by Unity when the component is reset.</summary>
        protected virtual void Reset()
        {
            Controllers.Clear();
            CreateControllers();
            ScanRecursively = true;
            SuppressInputWhileBlending = true;
        }

        /// <summary>Called by Unity when the inspector component is enabled</summary>
        protected virtual void OnEnable()
        {
#if LISTVIEW_BUG_WORKAROUND
            // Legacy support: upgrade the saved data
            if (m_LegacyControllers.Count > 0)
            {
                m_ControllerList.Controllers.Clear();
                m_ControllerList.Controllers.AddRange(m_LegacyControllers);
                m_LegacyControllers.Clear();
            }
#endif
            CreateControllers();
        }

        /// <summary>Called by Unity when the inspector component is disabled</summary>
        protected virtual void OnDisable()
        {
            foreach (var t in m_AxisResetters)
                if ((t as UnityEngine.Object) != null)
                    t.UnregisterResetHandler(OnResetInput);
            m_Axes.Clear();
            m_AxisOwners.Clear();
            m_AxisResetters.Clear();
        }

#if UNITY_EDITOR
        /// <inheritdoc />
        public bool ControllersAreValid()
        {
            s_AxisTargetsCache.Clear();
            if (ScanRecursively)
                GetComponentsInChildren(s_AxisTargetsCache);
            else
                GetComponents(s_AxisTargetsCache);
            var count = s_AxisTargetsCache.Count;
            bool isValid = count == m_AxisOwners.Count;
            for (int i = 0; isValid && i < count; ++i)
                if (s_AxisTargetsCache[i] != m_AxisOwners[i])
                    isValid = false;
            return isValid;
        }
        static readonly List<IInputAxisOwner> s_AxisTargetsCache = new ();
#endif

        /// <summary>
        /// Called by editor only.  Normally we should have one controller per 
        /// IInputAxisOwner axis.  This will create missing controllers (in their 
        /// default state) and remove any that are no longer releant.
        /// </summary>
        public void SynchronizeControllers() => CreateControllers();

        void CreateControllers()
        {
            m_Axes.Clear();
            m_AxisOwners.Clear();
            if (ScanRecursively)
                GetComponentsInChildren(m_AxisOwners);
            else
                GetComponents(m_AxisOwners);

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
#if LISTVIEW_BUG_WORKAROUND
            m_ControllerList.Controllers = newControllers;
#else
            Controllers = newControllers;
#endif

            // Rebuild the resetter list and register with them
            m_AxisResetters.Clear();
            if (enabled)
            {
                if (ScanRecursively)
                    GetComponentsInChildren(m_AxisResetters);
                else
                    GetComponents(m_AxisResetters);
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
        
        //TODO Support fixed update as well. Input system has a setting to update inputs only during fixed update.
        //TODO This won't work accuratly if this setting is enabled.
        void Update() 
        {
            if (Application.isPlaying)
                UpdateControllers();
        }
           
        /// <summary>Read all the controllers and process their input.</summary>
        protected void UpdateControllers()
        {
            if (SuppressInputWhileBlending 
                && TryGetComponent<CinemachineVirtualCameraBase>(out var vcam)
                && vcam.IsParticipatingInBlend())
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
                    c.InputValue = c.Input.GetValue(this, hint);

                c.Driver.ProcessInput(ref m_Axes[i].DrivenAxis(), c.InputValue, deltaTime);
                //gotInput |= Mathf.Abs(c.InputValue) > 0.001f;
            }
            // GML todo: handle synching of recentering across multiple axes
        }
    }
}

