using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Utils;
using Cinemachine;
using UnityEngine.SceneManagement;
using Object = System.Object;

namespace Tests.Runtime
{
    [TestFixture]
    public class FreeLookTests : CinemachineFixtureBase
    {
        class TestAxisProvider : AxisState.IInputAxisProvider
        {
            private float x, y;

            public TestAxisProvider()
            {
                x = 0f;
                y = 0f;
            }

            public void SetAxisValues(float x, float y)
            {
                this.x = x;
                this.y = y;
            }

            public float GetAxisValue(int axis)
            {
                return axis == 0 ? x : y;
            }
        }

        private CinemachineFreeLook m_FreeLook;
        private TestAxisProvider m_AxisProvider;

        [SetUp]
        public override void SetUp()
        {
            CreateGameObject("Camera", typeof(Camera), typeof(CinemachineBrain));

            // create a "character"
            var character = CreateGameObject("Character").GetComponent<Transform>();
            character.position.Set(0f, 0f, 0f);
            var body = CreateGameObject("Body").GetComponent<Transform>();
            body.position.Set(0f, 0f, 0f);
            body.parent = character;
            var head = CreateGameObject("Head").GetComponent<Transform>();
            head.position.Set(0f, 1f, 0f);
            head.parent = body;

            // Create a free-look camera 
            m_FreeLook = CreateGameObject("CinemachineFreeLook", typeof(CinemachineFreeLook)).GetComponent<CinemachineFreeLook>();
            m_AxisProvider = new TestAxisProvider();
            m_FreeLook.m_XAxis.SetInputAxisProvider(0, m_AxisProvider);
            m_FreeLook.m_YAxis.SetInputAxisProvider(1, m_AxisProvider);
            m_FreeLook.Follow = body;
            m_FreeLook.LookAt = head;

            base.SetUp();
        }

        private static IEnumerable FreeLookTestCases
        {
            get
            {
                yield return new TestCaseData(-10f, 0.25f, new Vector3(-2.604343f, 2.633411f, -1.503618f)).SetName("Left X Bottom Y").Returns(null);
                yield return new TestCaseData(-10f, 0.5f, new Vector3(-2.598455f, 2.766761f, -1.500219f)).SetName("Left X Center Y").Returns(null);
                yield return new TestCaseData(-10f, 0.75f, new Vector3(-2.58138f, 2.899473f, -1.49036f)).SetName("Left X Top Y").Returns(null);

                yield return new TestCaseData(0f, 0.25f, new Vector3(0f, 2.633411f, -3.007236f)).SetName("Center X Bottom Y").Returns(null);
                yield return new TestCaseData(0f, 0.5f, new Vector3(0f, 2.766761f, -3.000437f)).SetName("Center X Center Y").Returns(null);
                yield return new TestCaseData(0f, 0.75f, new Vector3(0f, 2.899473f, -2.980721f)).SetName("Center X Top Y").Returns(null);

                yield return new TestCaseData(10f, 0.25f, new Vector3(2.604343f, 2.633411f, -1.503618f)).SetName("Right X Bottom Y").Returns(null);
                yield return new TestCaseData(10f, 0.5f, new Vector3(2.598455f, 2.766761f, -1.500219f)).SetName("Right X Center Y").Returns(null);
                yield return new TestCaseData(10f, 0.75f, new Vector3(2.58138f, 2.899473f, -1.49036f)).SetName("Right X Top Y").Returns(null);
            }
        }

        [UnityTest, TestCaseSource(nameof(FreeLookTestCases))]
        public IEnumerator TestAxisStateChangeMovesCamera(float axisX, float axisY, Vector3 expectedPosition)
        {
            // apply a constant "force"
            m_AxisProvider.SetAxisValues(axisX, axisY);

            // wait two frames (constant deltaTime forced in SetUp)
            yield return null;
            yield return null;

            // check the resulting position of the freelook
            Assert.That(m_FreeLook.transform.position, Is.EqualTo(expectedPosition).Using(Vector3EqualityComparer.Instance),
                $"Actual was: ({m_FreeLook.transform.position.x}f, {m_FreeLook.transform.position.y}f, {m_FreeLook.transform.position.z}f)");
        }
    }
}