using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine.Utility;
using UnityEngine;

#if CINEMACHINE_UNITY_INPUTSYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;
#endif

namespace Cinemachine
{
    /// <summary>
    /// This is a behaviour that is used to drive behaviours with IInputAxisTarget interface, 
    /// which it discovers dynamically.  It is the bridge between the input system and 
    /// Cinemachine cameras that require user input.  Add it to a Cinemachine camera that needs it.
    /// </summary>
    [ExecuteAlways]
    public class InputAxisController : MonoBehaviour
    {
        public delegate float GetInputAxisValueDelegate(string axisName);
        public GetInputAxisValueDelegate GetInputAxisValue = ReadLegacyInput;

        /// <summary>This list is dynamically populated based AxisController the discovered axes</summary>
        public List<AxisController> Controllers = new List<AxisController>();

        /// <summary>
        /// Axes are dynamically discovered by querying behaviours implementing <see cref="IInputAxisTarget"/>
        /// </summary>
        List<IInputAxisTarget.AxisDescriptor> m_Axes = new List<IInputAxisTarget.AxisDescriptor>();

        /// <summary>
        /// Each discovered axis will get an AxisController to drive it in Update().
        /// </summary>
        [Serializable]
        public class AxisController
        {
            /// <summary>Identifies this axis in the inspector</summary>
            [HideInInspector] public string Name;

            /// <summary>Axis name for the Legacy Input system (if used).  
            /// CinemachineCore.GetInputAxis() will be called with this name.</summary>
            [InputAxisNameProperty]
            [Tooltip("Axis name for the Legacy Input system (if used).  "
                + "This value will be used to control the axis.")]
            public string InputName;

    #if CINEMACHINE_UNITY_INPUTSYSTEM
            /// <summary>Action for the Input package (if used).</summary>
            [Tooltip("Action for the Input package (if used).")]
            public InputActionReference InputAction;
    #endif
            /// <summary>The input value is multiplied by this amount prior to processing.
            /// Controls the input power.  Set it to a negative value to invert the input</summary>
            [Tooltip("The input value is multiplied by this amount prior to processing.  "
                + "Controls the input power.  Set it to a negative value to invert the input")]
            public float Gain;

            /// <summary>Setting that control the way the input axis responds to user input</summary>
            [HideFoldout]
            public InputAxisControl Control;

            /// <summary>This object drives the axis value based on the control 
            /// and recentering settings</summary>
            internal InputAxisDriver Driver;
        }

        void OnValidate()
        {
            for (int i = Controllers.Count; i < Controllers.Count; ++i)
                Controllers[i].Control.Validate();
        }

        void Reset()
        {
            Controllers.Clear();
            CreateControllers();
        }

        void OnEnable()
        {
            CreateControllers();
        }

        void OnDisable()
        {
            m_Axes.Clear();
            var targets = GetComponentsInChildren<IInputAxisTarget>();
            foreach (var t in targets)
                t.UnregisterResetHandler(OnResetInput);
        }

        void CreateControllers()
        {
            m_Axes.Clear();
            var targets = GetComponentsInChildren<IInputAxisTarget>();
            foreach (var t in targets)
            {
                t.GetInputAxes(m_Axes);
                t.UnregisterResetHandler(OnResetInput);
                t.RegisterResetHandler(OnResetInput);
            }
            // Trim excess controllers
            if (Controllers.Count > m_Axes.Count)
                Controllers.RemoveRange(m_Axes.Count, Controllers.Count - m_Axes.Count);

            // Add missing controllers - set up using defaults where possible
            for (int i = Controllers.Count; i < m_Axes.Count; ++i)
            {
                Controllers.Add(new AxisController {
                    Name = m_Axes[i].Name,
                    InputName = GetDefaultInputName(m_Axes[i].AxisIndex),
    #if CINEMACHINE_UNITY_INPUTSYSTEM
                    InputAction = null, // GML TODO
    #endif
                    Gain = (m_Axes[i].AxisIndex == 1) ? -1 : 1,
                    Control = new InputAxisControl { AccelTime = 0.2f, DecelTime = 0.2f }
                });
            }
        }

        string GetDefaultInputName(int axisIndex)
        {
            switch (axisIndex)
            {
                case 0: return "Mouse X";
                case 1: return "Mouse Y";
                case 2: return "Mouse ScrollWheel";
                default: return "";
            }
        }

        void OnResetInput()
        {
            for (int i = 0; i < Controllers.Count; ++i)
                Controllers[i].Driver.Reset(m_Axes[i].Axis);
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

                // GML TODO: new input handling
                if (!string.IsNullOrEmpty(c.InputName) && GetInputAxisValue != null)
                    c.Control.InputValue = GetInputAxisValue(c.InputName) * c.Gain;

                gotInput |= c.Driver.UpdateInput(deltaTime, m_Axes[i].Axis, ref c.Control);
            }
            for (int i = 0; i < Controllers.Count; ++i)
            {
                if (gotInput)
                    Controllers[i].Driver.CancelRecentering();
                Controllers[i].Driver.DoRecentering(deltaTime, m_Axes[i].Axis);
            }
        }

        static float ReadLegacyInput(string axisName)
        {
            float value = 0;
            {
                try { value = CinemachineCore.GetInputAxis(axisName); }
                catch (ArgumentException) {}
                //catch (ArgumentException e) { Debug.LogError(e.ToString()); }
            }
            return value;
        }
    }
}

