using System;

namespace Unity.Cinemachine.Tests.Editor
{
    [Serializable]
    class Return1Reader : IInputAxisReader
    {
        public float GetValue(
            UnityEngine.Object context,
            int playerIndex,
            bool autoEnableInput,
            IInputAxisOwner.AxisDescriptor.Hints hint)
        {
            return 1;
        }
    }

    class InputController1AllAxis : InputAxisControllerBase<Return1Reader>
    {
    }
}