using NUnit.Framework;
using UnityEngine.TestTools.Constraints;
using Is = NUnit.Framework.Is;

namespace Unity.Cinemachine.Tests
{
    [TestFixture]
    public class CinemachineInputAxisControllerTests : CinemachineRuntimeFixtureBase
    {
        private CinemachineInputAxisController m_Controller;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            var camGo = CreateGameObject("CinemachineCamera", new[]
            {
                typeof(CinemachineCamera),
                typeof(CinemachinePanTilt),
                typeof(CinemachineInputAxisController)
            });
            m_Controller = camGo.GetComponent<CinemachineInputAxisController>();
        }

#if CINEMACHINE_UNITY_INPUTSYSTEM
        [Test]
        public void ReadControlValueOverride_DoesNot_Allocate()
        {
            // Empty ReadControlValueOverride delegate.
            m_Controller.ReadControlValueOverride = (action, hint, context, reader) => 0f;

            m_Controller.m_ControllerManager.UpdateControllers(m_Controller, 0.1f);

            for (int i = 0; i < 10; i++)
            {
                Assert.That(() =>
                {
                    m_Controller.m_ControllerManager.UpdateControllers(m_Controller, 0.1f);
                }, Is.Not.AllocatingGCMemory());
            }
        }
#endif
    }
}