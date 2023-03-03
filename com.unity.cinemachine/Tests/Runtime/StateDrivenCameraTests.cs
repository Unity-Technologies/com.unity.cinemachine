#if UNITY_EDITOR // AssetDatabase.LoadMainAssetAtPath
#if CINEMACHINE_UNITY_ANIMATION
using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine.TestTools;

using Unity.Cinemachine;

namespace Tests.Runtime
{
    [TestFixture]
    public class StateDrivenCameraTests : CinemachineRuntimeFixtureBase
    {
        CinemachineStateDrivenCamera m_StateDrivenCamera;
        Animator m_Animator;
        CinemachineCamera m_Vcam1, m_Vcam2;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            // Create a minimal character controller
            var character = CreateGameObject("Character", typeof(Animator));
            AnimatorController controller = null;
            foreach (var asset in AssetDatabase.FindAssets("t:AnimatorController TestController"))
            {
                var path = AssetDatabase.GUIDToAssetPath(asset);
                controller = AssetDatabase.LoadMainAssetAtPath(path) as AnimatorController;
            }

            if (controller == null)
            {
                throw new ArgumentNullException("controller", "FindAssets did not find the TestController in the project.");
            }
            character.GetComponent<Animator>().runtimeAnimatorController = controller;

            // Create a state-driven camera with two vcams 
            var stateDrivenCamera = CreateGameObject("CM StateDrivenCamera", typeof(CinemachineStateDrivenCamera)).GetComponent<CinemachineStateDrivenCamera>();
            stateDrivenCamera.AnimatedTarget = character.GetComponent<Animator>();

            var vcam1 = CreateGameObject("Vcam1", typeof(CinemachineCamera)).GetComponent<CinemachineCamera>();
            var vcam2 = CreateGameObject("Vcam1", typeof(CinemachineCamera)).GetComponent<CinemachineCamera>();
            vcam1.gameObject.transform.SetParent(stateDrivenCamera.gameObject.transform);
            vcam2.gameObject.transform.SetParent(stateDrivenCamera.gameObject.transform);

            // Map states to vcams
            stateDrivenCamera.Instructions = new[]
            {
                new CinemachineStateDrivenCamera.Instruction() {FullHash = controller.layers[0].stateMachine.states[0].GetHashCode(), Camera = vcam1},
                new CinemachineStateDrivenCamera.Instruction() {FullHash = controller.layers[0].stateMachine.states[1].GetHashCode(), Camera = vcam2}
            };

            m_StateDrivenCamera = stateDrivenCamera;
            m_Animator = character.GetComponent<Animator>();
            m_Vcam1 = vcam1;
            m_Vcam2 = vcam2;
        }

        [UnityTest]
        public IEnumerator Test_StateDrivenCamera_Follows_State()
        {
            yield return null; // wait one frame

            Assert.That(m_StateDrivenCamera.LiveChild.Name, Is.EqualTo(m_Vcam1.Name));

            m_Animator.SetTrigger("DoTransitionToState2");

            yield return null; // wait one frame

            Assert.That(m_StateDrivenCamera.LiveChild.Name, Is.EqualTo(m_Vcam2.Name));

            m_Animator.SetTrigger(("DoTransitionToState1"));

            yield return null; // wait one frame

            Assert.That(m_StateDrivenCamera.LiveChild.Name, Is.EqualTo(m_Vcam1.Name));
        }
    }
}
#endif
#endif
