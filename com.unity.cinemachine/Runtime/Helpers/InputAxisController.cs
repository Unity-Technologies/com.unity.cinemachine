using UnityEngine;

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
    public class InputAxisController : InputAxisBase
    {

        void Update()
        {
            if (!Application.isPlaying)
                return;

            m_InputAxisData.Update();
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

#if CINEMACHINE_UNITY_INPUTSYSTEM
#endif
    }
}

