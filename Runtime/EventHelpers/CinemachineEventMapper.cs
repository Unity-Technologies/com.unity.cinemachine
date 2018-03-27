using Cinemachine.Utility;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Cinemachine
{
    /// <summary>
    /// This is a way to map UnityEvents to Cinemachine Viretual Cameras
    /// allowing you to associate specific vcams with specific events.  
    /// When that event is fired, then the associated camera will be activated 
    /// or priority boosted, depending on the setting.
    /// </summary>
    [DocumentationSorting(DocumentationSortingAttribute.Level.UserRef)]
    [AddComponentMenu("Cinemachine/CinemachineEventMapper")]
    public class CinemachineEventMapper : MonoBehaviour
    {
        /// <summary>This represents a single event mapping. It associates
        /// an event with a virtual camera, and describes the action to be taken 
        /// when that event occurs.</summary>
        [Serializable]
        public class Instruction
        {
            /// <summary>The GameObject that owns the UnityEvent</summary>
            [Tooltip("The GameObject that owns the UnityEvent")]
            public GameObject m_Object;

            /// <summary>The UnityEvent that will trigger the activation of this camera</summary>
            [Tooltip("The UnityEvent that will trigger the activation of this camera")]
            public string m_Event;

            /// <summary>The virtual camera to activate whrn the animation state becomes active</summary>
            [Tooltip("The virtual camera to activate whrn the animation state becomes active")]
            public CinemachineVirtualCameraBase m_VirtualCamera;

            /// <summary>How much the priority will be temporarily boosted until the 
            /// next activation event. If 0 then the vcam will be moved to the top 
            /// of its priority subqueue</summary>
            [Tooltip("How much the priority will be temporarily boosted until the next activation event. If 0 then the vcam will be moved to the top of its priority subqueue")]
            public int m_PriorityBoost;

            /// <summary>How long to wait (in seconds) before activating the virtual camera. 
            /// This filters out very short event intervals</summary>
            [Tooltip("How long to wait (in seconds) before activating the virtual camera. This filters out very short event intervals")]
            public float m_ActivateAfter;

            /// <summary>The minimum length of time (in seconds) to keep a virtual camera active.</summary>
            [Tooltip("The minimum length of time (in seconds) to keep a virtual camera active")]
            public float m_MinDuration;

            /// <summary>The camera will automatically deactivate after this time (in seconds).  0 means it does not automatically deactivate.</summary>
            [Tooltip("The camera will automatically deactivate after this time (in seconds).  0 means it does not automatically deactivate.")]
            public float m_MaxDuration;

            /// <summary>Make sure the field values are sane</summary>
            public void Validate()
            {
                m_Event = m_Event.Trim();
                m_ActivateAfter = Mathf.Max(0, m_ActivateAfter);
                m_MinDuration = Mathf.Max(0, m_MinDuration);
                m_MaxDuration = Mathf.Max(0, m_MaxDuration);
            }
        };

        /// <summary>The set of mappings associating virtual cameras with events</summary>
        [Tooltip("The set of mappings associating virtual cameras with events.")]
        public Instruction[] m_Instructions;

        void Start()
        {
            InvalidateCache();
        }

        void OnEnable()
        {
            InvalidateCache();
            ValidateCache();
        }

        private void OnValidate()
        {
            if (m_Instructions != null)
                for (int i = 0; i < m_Instructions.Length; ++i)
                    m_Instructions[i].Validate();
        }

        /// <summary>Call this when instruction list is changed</summary>
        public void InvalidateCache() { mCacheValid = false; }

        void ValidateCache()
        {
            if (mEventHandlers == null)
                mEventHandlers = new List<EventHandler>();
            if (mCacheValid)
                return;
            for (int i = 0; i < mEventHandlers.Count; ++i)
                mEventHandlers[i].Disconnect();
            mEventHandlers.Clear();

            if (m_Instructions == null)
                m_Instructions = new Instruction[0];
            for (int i = 0; i < m_Instructions.Length; ++i)
            {
                EventHandler h = new EventHandler 
                {
                    mEventMapper = this,
                    mInstruction = m_Instructions[i]
                };
                h.Connect();
                mEventHandlers.Add(h);
            }
            mCacheValid = true;

            // Zap the cached current state
            mActivationTime = mPendingActivationTime = 0;
            mActiveInstruction = mPendingInstruction = null;
            mWasDisabled = false;
        }

        class EventHandler
        {
            public CinemachineEventMapper mEventMapper;
            public Instruction mInstruction;
            public GameObject m_Object;
            public string m_Event;
            public void Handler() { mEventMapper.OnInstructionEvent(mInstruction); }
            public void Connect()
            {
                UnityEvent e = ReflectionHelpers.FieldValueFromPath<UnityEvent>(
                    mInstruction.m_Object, mInstruction.m_Event, null);
                if (e != null)
                {
                    e.RemoveListener(Handler);
                    e.AddListener(Handler);
                    m_Object = mInstruction.m_Object;
                    m_Event = mInstruction.m_Event;
                }
            }
            public void Disconnect()
            {
                UnityEvent e = ReflectionHelpers.FieldValueFromPath<UnityEvent>(
                    m_Object, m_Event, null);
                if (e != null)
                    e.RemoveListener(Handler);
            }
        }
        List<EventHandler> mEventHandlers;
        bool mCacheValid = false;

        float mActivationTime = 0;
        Instruction mActiveInstruction;
        bool mWasDisabled = false;

        float mPendingActivationTime = 0;
        Instruction mPendingInstruction;

        // Callback for instruction events
        private void OnInstructionEvent(Instruction instruction)
        {
            mPendingActivationTime = Time.time + instruction.m_ActivateAfter;
            mPendingInstruction = instruction;
        }

        private void Update()
        {
            ValidateCache();

            // Is there a minimum activation tme to respect?
            float now = Time.time;
            if (mActivationTime == 0 || mActiveInstruction == null 
                || now >= mActivationTime + mActiveInstruction.m_MinDuration)
            {
                // Did we get an event?
                if (mPendingActivationTime != 0 && mPendingInstruction != null 
                    && mPendingActivationTime <= now)
                {
                    DeactivateCurrentInstruction();

                    mActivationTime = now;
                    mActiveInstruction = mPendingInstruction;

                    mPendingInstruction = null;
                    mPendingActivationTime = 0;

                    CinemachineVirtualCameraBase vcam = mActiveInstruction.m_VirtualCamera;
                    if (vcam != null)
                    {
                        if (!vcam.gameObject.activeSelf)
                        {
                            mWasDisabled = true;
                            vcam.gameObject.SetActive(true);
                        }
                        if (mActiveInstruction.m_PriorityBoost != 0)
                            mActiveInstruction.m_VirtualCamera.Priority += mActiveInstruction.m_PriorityBoost;
                        else
                            vcam.MoveToTopOfPrioritySubqueue();
                    }
                }
            }
            // Has the current instruction expired?
            if (mActiveInstruction != null && mActiveInstruction.m_MaxDuration > 0
                && (now - mActivationTime) > mActiveInstruction.m_MaxDuration)
            {
                DeactivateCurrentInstruction();
            }
        }

        void DeactivateCurrentInstruction()
        {
            if (mActiveInstruction != null && mActiveInstruction.m_VirtualCamera != null)
            {
                mActiveInstruction.m_VirtualCamera.Priority -= mActiveInstruction.m_PriorityBoost;
                if (mWasDisabled)
                    mActiveInstruction.m_VirtualCamera.gameObject.SetActive(false);
            }
            mWasDisabled = false;
            mActivationTime = 0;
            mActiveInstruction = null;
        }
    }
}
