#if CINEMACHINE_UNITY_ANIMATION
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor;
using UnityEditor.Animations;
using Cinemachine;

namespace Tests.Runtime
{
    [TestFixture]
    public class StateDrivenCameraTests : CinemachineFixtureBase
    {
        CinemachineStateDrivenCamera m_StateDrivenCamera;
        Animator m_Animator;
        CinemachineVirtualCamera m_Vcam1, m_Vcam2;

        [SetUp]
        public override void SetUp()
        {
            CreateGameObject("Camera", typeof(Camera), typeof(CinemachineBrain));

            // Create a minimal character controller
            var character = CreateGameObject("Character", typeof(Animator));
            var controller = AssetDatabase.LoadMainAssetAtPath("Packages/com.unity.cinemachine/Tests/Runtime/TestController.controller") as AnimatorController;
            character.GetComponent<Animator>().runtimeAnimatorController = controller;

            // Create a state-driven camera with two vcams 
            var stateDrivenCamera = CreateGameObject("CM StateDrivenCamera", typeof(CinemachineStateDrivenCamera)).GetComponent<CinemachineStateDrivenCamera>();
            stateDrivenCamera.m_AnimatedTarget = character.GetComponent<Animator>();

            var vcam1 = CreateGameObject("Vcam1", typeof(CinemachineVirtualCamera)).GetComponent<CinemachineVirtualCamera>();
            var vcam2 = CreateGameObject("Vcam1", typeof(CinemachineVirtualCamera)).GetComponent<CinemachineVirtualCamera>();
            vcam1.gameObject.transform.SetParent(stateDrivenCamera.gameObject.transform);
            vcam2.gameObject.transform.SetParent(stateDrivenCamera.gameObject.transform);

            // Map states to vcams
            stateDrivenCamera.m_Instructions = new[]
            {
                new CinemachineStateDrivenCamera.Instruction() {m_FullHash = controller.layers[0].stateMachine.states[0].GetHashCode(), m_VirtualCamera = vcam1},
                new CinemachineStateDrivenCamera.Instruction() {m_FullHash = controller.layers[0].stateMachine.states[1].GetHashCode(), m_VirtualCamera = vcam2}
            };

            m_StateDrivenCamera = stateDrivenCamera;
            m_Animator = character.GetComponent<Animator>();
            m_Vcam1 = vcam1;
            m_Vcam2 = vcam2;

            base.SetUp();
        }

        [UnityTest]
        public IEnumerator Test_StateDrivenCamera_Follows_State()
        {
            yield return null; // wait one frame

            Assert.That(m_StateDrivenCamera.LiveChild.Name, Is.EqualTo(m_Vcam1.Name));

            m_Animator.SetTrigger("DoTransitionToState2");

            yield return null; // wait one frame

            Assert.That(m_StateDrivenCamera.LiveChild.Name, Is.EqualTo(m_Vcam2.Name));

            m_Animator.SetTrigger("DoTransitionToState1");

            yield return null; // wait one frame

            Assert.That(m_StateDrivenCamera.LiveChild.Name, Is.EqualTo(m_Vcam1.Name));
        }
    }
}
#endif
