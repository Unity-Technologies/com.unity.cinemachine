using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Unity.Cinemachine.Samples
{
    public class CursorLockManager : MonoBehaviour, IInputAxisOwner
    {
        public InputAxis CursorLockAxis = InputAxis.DefaultMomentary;
        
        public UnityEvent OnCursorLocked = new ();
        public UnityEvent OnCursorUnlocked = new ();
        
        bool m_CheckInput = true;
        void Update()
        {
            if (m_CheckInput && CursorLockAxis.Value > 0)
            {
                m_CheckInput = false;
                if (Cursor.lockState == CursorLockMode.None)
                    LockCursor();
                else
                    UnlockCursor();
            }
            else if (CursorLockAxis.Value == 0)
                m_CheckInput = true;
        }

        public void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            OnCursorLocked.Invoke();
            Debug.Log("Lock Cursor");
        }

        public void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            OnCursorUnlocked.Invoke();
            Debug.Log("Unlock Cursor");
        }

        public void GetInputAxes(List<IInputAxisOwner.AxisDescriptor> axes)
        {
            axes.Add(new () { DrivenAxis = () => ref CursorLockAxis, Name = "CursorLock", Hint = IInputAxisOwner.AxisDescriptor.Hints.X });
        }
    }
}
