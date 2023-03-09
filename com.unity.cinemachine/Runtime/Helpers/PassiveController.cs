using System;
using UnityEngine;

namespace Unity.Cinemachine
{
    [Serializable]
    public class PassiveController: LazyController, IController<PassiveController>
    {
        private float m_Value;

        public float controlValue
        {
            set { m_Value = value; }
        }
        
        public float Read(IInputAxisSource.AxisDescriptor.Hints hint)
        {
            return m_Value;
        }

        public bool IsValid()
        {
            return true;
        }
    }
}
