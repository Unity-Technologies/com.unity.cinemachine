using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

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

    /// <summary>Use special property drawer for a list of InputAxisControllerBase.Controller objects</summary>
    internal class InputAxisControllerManagerAttribute : PropertyAttribute {}

    [Serializable]
    internal class InputAxisControllerManager<T> where T : IInputAxisReader, new ()
    {
        [NonReorderable]
        public List<InputAxisControllerBase<T>.Controller> Controllers = new ();

        /// Axes are dynamically discovered by querying behaviours implementing <see cref="IInputAxisOwner"/>
        /// </summary>
        readonly List<IInputAxisOwner.AxisDescriptor> m_Axes = new ();
        readonly List<IInputAxisOwner> m_AxisOwners = new ();
        readonly List<IInputAxisResetSource> m_AxisResetters = new ();

        /// Call from owner's OnValidate()
        public void Validate()
        {
            for (int i = 0; i < Controllers.Count; ++i)
                if (Controllers[i] != null)
                    Controllers[i].Driver.Validate();
        }

        /// Call from owner's OnDisable() to shut down <summary>
        public void OnDisable()
        {
            for (int i = 0; i < m_AxisResetters.Count; ++i)
                if ((m_AxisResetters[i] as UnityEngine.Object) != null)
                    m_AxisResetters[i].UnregisterResetHandler(OnResetInput);
            m_Axes.Clear();
            m_AxisOwners.Clear();
            m_AxisResetters.Clear();
        }

        /// Call from owner's OnDisable() to shut down <summary>
        public void Reset()
        {
            OnDisable();
            Controllers.Clear();
        }

        void OnResetInput()
        {
            for (int i = 0; i < Controllers.Count; ++i)
                Controllers[i].Driver.Reset(ref m_Axes[i].DrivenAxis());
        }

#if UNITY_EDITOR
        public bool ControllersAreValid(GameObject root, bool scanRecursively)
        {
            s_AxisTargetsCache.Clear();
            if (scanRecursively)
                root.GetComponentsInChildren(s_AxisTargetsCache);
            else
                root.GetComponents(s_AxisTargetsCache);
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
        /// Creates default controllers for an axis.
        /// Override this if the default axis controllers do not fit your axes.
        /// </summary>
        /// <param name="axis">Description of the axis whose default controller needs to be set.</param>
        /// <param name="controller">Controller to drive the axis.</param>
        public delegate void DefaultInitializer(
            in IInputAxisOwner.AxisDescriptor axis, InputAxisControllerBase<T>.Controller controller);

        /// <summary>
        /// Create missing controllers (in their default state) and remove any that
        /// are no longer relevant.
        /// </summary>
        public void CreateControllers(
            GameObject root, bool scanRecursively, bool enabled, DefaultInitializer defaultInitializer)
        {
            OnDisable();
            if (scanRecursively)
                root.GetComponentsInChildren(m_AxisOwners);
            else
                root.GetComponents(m_AxisOwners);

            // Trim excess controllers
            for (int i = Controllers.Count - 1; i >= 0; --i)
                if (!m_AxisOwners.Contains(Controllers[i].Owner as IInputAxisOwner))
                    Controllers.RemoveAt(i);

            // Rebuild the controller list, recycling existing ones to preserve the settings
            List<InputAxisControllerBase<T>.Controller> newControllers = new();
            for (int j = 0; j < m_AxisOwners.Count; ++j)
            {
                var t = m_AxisOwners[j];
                var startIndex = m_Axes.Count;
                t.GetInputAxes(m_Axes);
                for (int i = startIndex; i < m_Axes.Count; ++i)
                {
                    int controllerIndex = GetControllerIndex(Controllers, t, m_Axes[i].Name);
                    if (controllerIndex < 0)
                    {
                        var c = new InputAxisControllerBase<T>.Controller
                        {
                            Enabled = true,
                            Name = m_Axes[i].Name,
                            Owner = t as UnityEngine.Object,
                            Input = new T()
                        };
                        defaultInitializer?.Invoke(m_Axes[i], c);
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

            if (enabled)
                RegisterResetHandlers(root, scanRecursively);

            static int GetControllerIndex(
                List<InputAxisControllerBase<T>.Controller> list, IInputAxisOwner owner, string axisName)
            {
                for (int i = 0; i < list.Count; ++i)
                    if (list[i].Owner as IInputAxisOwner == owner && list[i].Name == axisName)
                        return i;
                return -1;
            }
        }

        void RegisterResetHandlers(GameObject root, bool scanRecursively)
        {
            // Rebuild the resetter list and register with them
            m_AxisResetters.Clear();
            if (scanRecursively)
                root.GetComponentsInChildren(m_AxisResetters);
            else
                root.GetComponents(m_AxisResetters);
            for (int i = 0; i < m_AxisResetters.Count; ++i)
            {
                m_AxisResetters[i].UnregisterResetHandler(OnResetInput);
                m_AxisResetters[i].RegisterResetHandler(OnResetInput);
            }
        }

        /// <summary>Read all the controllers and process their input.</summary>
        public void UpdateControllers(UnityEngine.Object context, float deltaTime)
        {
            for (int i = 0; i < Controllers.Count; ++i)
            {
                var c = Controllers[i];
                if (!c.Enabled || c.Input == null)
                    continue;
                var hint = i < m_Axes.Count ? m_Axes[i].Hint : 0;
                if (c.Input != null)
                    c.InputValue = c.Input.GetValue(context, hint);

                c.Driver.ProcessInput(ref m_Axes[i].DrivenAxis(), c.InputValue, deltaTime);
            }
        }
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
        /// If set, then input will be processed using unscaled deltaTime, and not scaled deltaTime.
        /// This allows input to continue even when the timescale is set to 0.
        /// </summary>
        public bool IgnoreTimeScale;

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

        [Header("Driven Axes")]
        [InputAxisControllerManager]
        [SerializeField, NoSaveDuringPlay] internal InputAxisControllerManager<T> m_ControllerManager = new ();

        /// <summary>This list is dynamically populated based on the discovered axes</summary>
        public List<Controller> Controllers
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_ControllerManager.Controllers;
        }

        /// <summary>Editor only: Called by Unity when the component is serialized
        /// or the inspector is changed.</summary>
        protected virtual void OnValidate() => m_ControllerManager.Validate();

        /// <summary>Called by Unity when the component is reset.</summary>
        protected virtual void Reset()
        {
            ScanRecursively = true;
            SuppressInputWhileBlending = true;
            m_ControllerManager.Reset();
            SynchronizeControllers();
        }

        /// <summary>Called by Unity when the inspector component is enabled</summary>
        protected virtual void OnEnable() => SynchronizeControllers();

        /// <summary>Called by Unity when the inspector component is disabled</summary>
        protected virtual void OnDisable() => m_ControllerManager.OnDisable();

#if UNITY_EDITOR
        /// <inheritdoc />
        public bool ControllersAreValid() => m_ControllerManager.ControllersAreValid(gameObject, ScanRecursively);
#endif

        /// <summary>
        /// Normally we should have one controller per IInputAxisOwner axis.
        /// This will create missing controllers (in their default state) and remove any that
        /// are no longer relevant.  This is costly - do not call it every frame.
        /// </summary>
        public void SynchronizeControllers() => m_ControllerManager.CreateControllers(
            gameObject, ScanRecursively, enabled, InitializeControllerDefaultsForAxis);

        /// <summary>
        /// Creates default controllers for an axis.
        /// Override this if the default axis controllers do not fit your axes.
        /// </summary>
        /// <param name="axis">Description of the axis whose default controller needs to be set.</param>
        /// <param name="controller">Controller to drive the axis.</param>
        protected virtual void InitializeControllerDefaultsForAxis(
            in IInputAxisOwner.AxisDescriptor axis, Controller controller) {}

        /// <summary>Read all the controllers and process their input.
        /// Default implementation calls UpdateControllers(IgnoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime)</summary>
        protected void UpdateControllers()
        {
            UpdateControllers(IgnoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime);
        }

        /// <summary>Read all the controllers and process their input.</summary>
        /// <param name="deltaTime">The time interval for which to process the input</param>
        protected void UpdateControllers(float deltaTime)
        {
            if (SuppressInputWhileBlending
                && TryGetComponent<CinemachineVirtualCameraBase>(out var vcam)
                && vcam.IsParticipatingInBlend())
                return;

            m_ControllerManager.UpdateControllers(this, deltaTime);
        }
    }
}

