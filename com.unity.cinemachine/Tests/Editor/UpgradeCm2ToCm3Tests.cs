using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Cinemachine;
using Cinemachine.Editor;
using Cinemachine.Utility;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    public class UpgradeCm2ToCm3Tests : CinemachineFixtureBase
    {
        static IEnumerable<Type> s_AllCinemachineComponents;
        CinemachineBrain m_Brain;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            m_Brain = CreateGameObject("MainCamera", typeof(Camera), typeof(CinemachineBrain))
                .GetComponent<CinemachineBrain>();
            m_Brain.UpdateMethod = CinemachineBrain.UpdateMethods.ManualUpdate;
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
        }
        
        static IEnumerable ConvertTestCases
        {
            get
            {
                s_AllCinemachineComponents = ReflectionHelpers.GetTypesInAllDependentAssemblies((Type t) => 
                    typeof(CinemachineComponentBase).IsAssignableFrom(t) && !t.IsAbstract 
                    && t.GetCustomAttribute<CameraPipelineAttribute>() != null
                    && t.GetCustomAttribute<ObsoleteAttribute>() != null);
                foreach (var cmComponent in s_AllCinemachineComponents)
                    yield return new TestCaseData(cmComponent).SetName(cmComponent.Name).Returns(null);
            }
        }

#pragma warning disable CS0618 // disable obsolete warnings
        [UnityTest, TestCaseSource(nameof(ConvertTestCases))]
        public IEnumerator ConvertAllDefaultOnes(Type type)
        {
            var vcamGo = CreateGameObject("TestVcam", typeof(CinemachineVirtualCamera));
            Assert.IsTrue(vcamGo.TryGetComponent(out CinemachineVirtualCamera vcam));
            vcam.InvalidateComponentPipeline();
            m_Brain.ManualUpdate(); // ensure pipeline is built
            var pipeline = vcam.GetComponentInChildren<CinemachinePipeline>();
            Undo.AddComponent(pipeline.gameObject, type);
            vcam.InvalidateComponentPipeline();
            CinemachineUpgradeManager.UpgradeSingleObject(vcamGo);
            yield return null;

            Assert.That(vcamGo.TryGetComponent(out CinemachineVirtualCamera _), Is.False);
            Assert.That(vcamGo.TryGetComponent(type, out _), Is.False);  // old component is deleted
            Assert.That(vcamGo.transform.childCount, Is.Zero);
            Assert.That(vcamGo.GetComponent<CmCamera>(), Is.Not.Null); 
            Assert.That(vcamGo.GetComponent(UpgradeObjectToCm3.ClassUpgradeMap[type]), Is.Not.Null); // new component is added
        }

        [UnityTest]
        public IEnumerator ConvertFreelook()
        {
            var freelookGo = CreateGameObject("TestVcam", typeof(CinemachineFreeLook));
            CinemachineUpgradeManager.UpgradeSingleObject(freelookGo);
            yield return null;

            Assert.That(freelookGo.TryGetComponent(out CinemachineFreeLook _), Is.False);
            Assert.That(freelookGo.transform.childCount, Is.Zero);
            Assert.That(freelookGo.GetComponent<CmCamera>(), Is.Not.Null);
            Assert.That(freelookGo.GetComponent<CinemachineOrbitalFollow>(), Is.Not.Null);
            Assert.That(freelookGo.GetComponent<CinemachineRotationComposer>(), Is.Not.Null);
            Assert.That(freelookGo.GetComponent<CinemachineFreeLookModifier>(), Is.Not.Null);
            Assert.That(freelookGo.GetComponent<InputAxisController>(), Is.Not.Null);
            Assert.That(freelookGo.GetComponents<MonoBehaviour>().Length, Is.EqualTo(5));
        }
#pragma warning restore CS0618
    }
}
