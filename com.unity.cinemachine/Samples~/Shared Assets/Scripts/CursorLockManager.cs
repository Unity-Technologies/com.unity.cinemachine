using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Unity.Cinemachine.Samples
{
    public class CursorLockManager : MonoBehaviour, Unity.Cinemachine.IInputAxisOwner
    {
        public InputAxis CursorLock = InputAxis.DefaultMomentary;

        public UnityEvent OnCursorLocked = new ();
        public UnityEvent OnCursorUnlocked = new ();

        bool m_IsTriggered;

        public void GetInputAxes(List<IInputAxisOwner.AxisDescriptor> axes)
        {
            axes.Add(new()
            {
                DrivenAxis = () => ref CursorLock, Name = "CursorLock",
                Hint = IInputAxisOwner.AxisDescriptor.Hints.X
            });
        }

        void OnEnable() => UnlockCursor();
        void OnDisable() => UnlockCursor();

        void Update()
        {
            if (CursorLock.Value == 0)
                m_IsTriggered = false;
            else if (!m_IsTriggered)
            {
                m_IsTriggered = true;
                if (Cursor.lockState == CursorLockMode.None)
                    LockCursor();
                else
                    UnlockCursor();
            }
        }

        public void LockCursor()
        {
            if (enabled)
            {
                Cursor.lockState = CursorLockMode.Locked;
                OnCursorLocked.Invoke();
            }
        }

        public void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            OnCursorUnlocked.Invoke();
        }
    }
}
