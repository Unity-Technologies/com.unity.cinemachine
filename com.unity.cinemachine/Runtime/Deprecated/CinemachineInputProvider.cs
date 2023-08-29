#if !CINEMACHINE_NO_CM2_SUPPORT
using System;
using UnityEngine;

#if CINEMACHINE_UNITY_INPUTSYSTEM
using System.Linq;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

namespace Unity.Cinemachine
{
    /// <summary>
    /// This is a deprecated component.  Use InputAxisController instead.
    /// </summary>
    [Obsolete("CinemachineInputProvider has been deprecated. Use InputAxisController instead.")]
    [AddComponentMenu("")] // Don't display in add component menu
    public class CinemachineInputProvider : MonoBehaviour, AxisState.IInputAxisProvider
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

        /// <summary>Vector2 action for XY movement</summary>
        [Tooltip("Vector2 action for XY movement")]
        public InputActionReference XYAxis;

        /// <summary>Float action for Z movement</summary>
        [Tooltip("Float action for Z movement")]
        public InputActionReference ZAxis;

        /// <summary>
        /// Implementation of AxisState.IInputAxisProvider.GetAxisValue().
        /// Axis index ranges from 0...2 for X, Y, and Z.
        /// Reads the action associated with the axis.
        /// </summary>
        /// <param name="axis"></param>
        /// <returns>The current axis value</returns>
        public virtual float GetAxisValue(int axis)
        {
            if (enabled)
            {
                var action = ResolveForPlayer(axis, axis == 2 ? ZAxis : XYAxis);
                if (action != null)
                {
                    switch (axis)
                    {
                        case 0: return action.ReadValue<Vector2>().x;
                        case 1: return action.ReadValue<Vector2>().y;
                        case 2: return action.ReadValue<float>();
                    }
                }
            }
            return 0;
        }

        const int NUM_AXES = 3;
        InputAction[] m_cachedActions;

        /// <summary>
        /// In a multi-player context, actions are associated with specific players
        /// This resolves the appropriate action reference for the specified player.
        /// 
        /// Because the resolution involves a search, we also cache the returned 
        /// action to make future resolutions faster.
        /// </summary>
        /// <param name="axis">Which input axis (0, 1, or 2)</param>
        /// <param name="actionRef">Which action reference to resolve</param>
        /// <returns>The cached action for the player specified in PlayerIndex</returns>
        protected InputAction ResolveForPlayer(int axis, InputActionReference actionRef)
        {
            if (axis < 0 || axis >= NUM_AXES)
                return null;
            if (actionRef == null || actionRef.action == null)
                return null;
            if (m_cachedActions == null || m_cachedActions.Length != NUM_AXES)
                m_cachedActions = new InputAction[NUM_AXES];
            if (m_cachedActions[axis] != null && actionRef.action.id != m_cachedActions[axis].id)
                m_cachedActions[axis] = null;
            
            if (m_cachedActions[axis] == null)
            {
                m_cachedActions[axis] = actionRef.action;
                if (PlayerIndex != -1)
                    m_cachedActions[axis] = GetFirstMatch(InputUser.all[PlayerIndex], actionRef);
        
                if (AutoEnableInputs && actionRef != null && actionRef.action != null)
                    actionRef.action.Enable();
            }
            // Update enabled status
            if (m_cachedActions[axis] != null && m_cachedActions[axis].enabled != actionRef.action.enabled)
            {
                if (actionRef.action.enabled)
                    m_cachedActions[axis].Enable();
                else
                    m_cachedActions[axis].Disable();
            }
            
            return m_cachedActions[axis];
            
            // local function to wrap the lambda which otherwise causes a tiny gc
            InputAction GetFirstMatch(in InputUser user, InputActionReference aRef) => 
                user.actions.First(x => x.id == aRef.action.id);
        }

        // Clean up
        protected virtual void OnDisable()
        {
            m_cachedActions = null;
        }
    }
}
#else
namespace Unity.Cinemachine
{
    /// <summary>
    /// This is an add-on to override the legacy input system and read input using the
    /// UnityEngine.Input package API.  Add this behaviour to any CinemachineVirtualCamera 
    /// or FreeLook that requires user input, and drag in the the desired actions.
    /// If the Input System Package is not installed, then this behaviour does nothing.
    /// </summary>
    [AddComponentMenu("")] // Hide in menu
    public class CinemachineInputProvider : MonoBehaviour {}
}
#endif

namespace Unity.Cinemachine
{
    /// <summary>
    /// IInputAxisProvider is deprecated.  Use InputAxis and InputAxisController instead.
    /// </summary>
    [Obsolete("IInputAxisProvider is deprecated.  Use InputAxis and InputAxisController instead")]
    public static class CinemachineInputProviderExtensions
    {
        /// <summary>
        /// Locate the first component that implements AxisState.IInputAxisProvider.
        /// </summary>
        /// <param name="vcam">The virtual camera in queston</param>
        /// <returns>The first AxisState.IInputAxisProvider or null if none</returns>
        public static AxisState.IInputAxisProvider GetInputAxisProvider(this CinemachineVirtualCameraBase vcam)
        {
            var components = vcam.GetComponentsInChildren<MonoBehaviour>();
            for (int i = 0; i < components.Length; ++i)
            {
                if (components[i] is AxisState.IInputAxisProvider provider)
                    return provider;
            }
            return null;
        }
    }
}
#endif
