using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

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
    public class InputAxisController : InputAxisBehaviour<Controller> {}
    
     /// <summary>
        /// Each discovered axis will get a Controller to drive it in Update().
        /// </summary>
        [Serializable]
        public class Controller : LazyController, IController<Controller>
        {
#if CINEMACHINE_UNITY_INPUTSYSTEM
            /// <summary>Action for the Input package (if used).</summary>
            [Tooltip("Action for the Input package (if used).")]
            public InputActionReference InputAction;

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
            
            public bool IsValid()
            {
                return InputAction != null && InputAction.action != null;
            }
            
            void ResolveActionForPlayer(int playerIndex)
            {
                if (m_CachedAction != null && InputAction.action.id != m_CachedAction.id)
                    m_CachedAction = null;
            
                if (m_CachedAction == null)
                {
                    m_CachedAction = InputAction.action;
                    if (playerIndex != -1)
                        m_CachedAction = GetFirstMatch(InputUser.all[playerIndex], InputAction);
                    if (/*AutoEnableInputs*/ true && m_CachedAction != null)
                        m_CachedAction.Enable();
                }

                // local function to wrap the lambda which otherwise causes a tiny gc
                InputAction GetFirstMatch(in InputUser user, InputActionReference aRef) => 
                    user.actions.First(x => x.id == aRef.action.id);
            }

            public float Read(IInputAxisSource.AxisDescriptor.Hints hint)
            {
                ResolveActionForPlayer(-1);
                // Update enabled status
                if (m_CachedAction != null && m_CachedAction.enabled != InputAction.action.enabled)
                {
                    if (InputAction.action.enabled)
                        m_CachedAction.Enable();
                    else
                        m_CachedAction.Disable();
                }

                return m_CachedAction != null ? m_CachedAction.ReadInput(IInputAxisSource.AxisDescriptor.Hints.X) : 0f;
            }
        }
}

