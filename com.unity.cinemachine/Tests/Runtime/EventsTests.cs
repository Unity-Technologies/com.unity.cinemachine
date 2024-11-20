using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Unity.Cinemachine.Tests
{
    [TestFixture]
    public class EventsTests : CinemachineRuntimeTimeInvariantFixtureBase
    {
        const float k_BlendingTime = 2;

        CinemachineCamera A, B, C;

        Dictionary<string, int> m_ActivatedCount = new ();
        Dictionary<string, int> m_DeactivatedCount = new ();
        Dictionary<string, int> m_BlendCreatedCount = new ();
        Dictionary<string, int> m_BlendFinishedCount = new ();

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            m_Brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Linear, k_BlendingTime);

            m_ActivatedCount.Clear();
            m_DeactivatedCount.Clear();
            m_BlendCreatedCount.Clear();
            m_BlendFinishedCount.Clear();
            
            A = SetupCamera("A", 30);
            B = SetupCamera("B", 20);
            C = SetupCamera("C", 10);

            CinemachineCamera SetupCamera(string name, int priority)
            {
                var cam = CreateGameObject(name, typeof(CinemachineCamera)).GetComponent<CinemachineCamera>();
                cam.Priority = priority;

                var events = cam.gameObject.AddComponent<CinemachineCameraEvents>();
                m_ActivatedCount.Add(name, 0);
                events.CameraActivatedEvent.AddListener((m, c) => CountEvent(m_ActivatedCount, c.Name, name));

                m_DeactivatedCount.Add(name, 0);
                events.CameraDeactivatedEvent.AddListener((m, c) => CountEvent(m_DeactivatedCount, c.Name, name));

                m_BlendCreatedCount.Add(name, 0);
                events.BlendCreatedEvent.AddListener((b) => CountEvent(m_BlendCreatedCount, b.Blend.CamB.Name, name));

                m_BlendFinishedCount.Add(name, 0);
                events.BlendFinishedEvent.AddListener((m, c) => CountEvent(m_BlendFinishedCount, c.Name, name));

                return cam;
            }

            void CountEvent(Dictionary<string, int> dic, string name, string expectedName)
            {
                dic[name] += 1;
                Assert.AreEqual(expectedName, name); // make sure we get events only for the relevant camera
            }
        }

        [UnityTest]
        public IEnumerator GetBlendEvents()
        {
            // Check that A is active
            yield return UpdateCinemachine();
            Assert.That(ReferenceEquals(m_Brain.ActiveVirtualCamera, A));
            Assert.AreEqual(m_ActivatedCount["A"], 1);
            Assert.AreEqual(m_ActivatedCount["B"], 0);
            Assert.AreEqual(m_ActivatedCount["C"], 0);

            // Activate B
            yield return WaitForSeconds(k_BlendingTime / 4);
            B.Priority = 100;
            yield return WaitForSeconds(k_BlendingTime / 4);
            Assert.AreEqual(m_ActivatedCount["B"], 1);
            Assert.AreEqual(m_BlendCreatedCount["B"], 1);
            Assert.AreEqual(m_DeactivatedCount["A"], 0); // still blending

            // Activate C before blend is finished - interrupting the first blend
            C.Priority = 200;
            yield return WaitForSeconds(k_BlendingTime / 4);
            Assert.AreEqual(m_ActivatedCount["C"], 1);
            Assert.AreEqual(m_BlendCreatedCount["C"], 1);
            Assert.AreEqual(m_DeactivatedCount["A"], 0);
            Assert.AreEqual(m_DeactivatedCount["B"], 0);

            // Wait until first blend is finished, but not the second
            yield return WaitForSeconds(k_BlendingTime / 2);
            Assert.AreEqual(m_DeactivatedCount["A"], 1);
            Assert.AreEqual(m_DeactivatedCount["B"], 0);
            Assert.AreEqual(m_BlendFinishedCount["B"], 0); // blend was interrupted, so never finishes

            // Wait until second blend is finished
            yield return WaitForSeconds(k_BlendingTime);
            Assert.AreEqual(m_DeactivatedCount["A"], 1);
            Assert.AreEqual(m_DeactivatedCount["B"], 1);
            Assert.AreEqual(m_DeactivatedCount["C"], 0);
            Assert.AreEqual(m_BlendFinishedCount["A"], 0);
            Assert.AreEqual(m_BlendFinishedCount["B"], 0);
            Assert.AreEqual(m_BlendFinishedCount["C"], 1); // this blend is allowed to finish
        }
    }
}
