using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Cinemachine;
using Cinemachine.Editor;
using Cinemachine.Utility;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Runtime
{
    public class UpgradeCm2ToCm3Tests : CinemachineFixtureBase
    {
        static IEnumerable<Type> s_AllCinemachineComponents;
        CinemachineBrain m_Brain;
        
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            var mainCamera = CreateGameObject("MainCamera", typeof(Camera), typeof(CinemachineBrain));
            m_Brain = mainCamera.GetComponent<CinemachineBrain>();
            m_Brain.UpdateMethod = CinemachineBrain.UpdateMethods.ManualUpdate;
            CinemachineCore.UniformDeltaTimeOverride = 0.1f;
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

#pragma warning disable CS0618
        [UnityTest, TestCaseSource(nameof(ConvertTestCases))]
        public IEnumerator ConvertAllDefaultOnes(Type type)
        {
            var vcamGo = CreateGameObject("TestVcam", typeof(CinemachineVirtualCamera));
            yield return null;
            
            var vcam = vcamGo.GetComponent<CinemachineVirtualCamera>();
            vcam.InvalidateComponentPipeline();
            yield return null;
            
            m_Brain.ManualUpdate(); // ensure pipeline is built
            yield return null;
            
            var pipeline = vcam.GetComponentInChildren<CinemachinePipeline>();
            pipeline.gameObject.AddComponent(type);
            vcam.InvalidateComponentPipeline();
            yield return null;
            
            CinemachineUpgradeManager.UpgradeSingleObject(vcamGo);
            yield return null;

            Assert.That(vcamGo.GetComponent<CinemachineVirtualCamera>(), Is.Null);
            Assert.That(vcamGo.transform.childCount, Is.Zero);
            Assert.That(vcamGo.GetComponent<CmCamera>(), Is.Not.Null); 
            Assert.That(vcamGo.GetComponent(type), Is.Null);  // old component is deleted
            Assert.That(vcamGo.GetComponent(UpgradeObjectToCm3.ClassUpgradeMap[type]), Is.Not.Null); // new component is added
        }

        [UnityTest]
        public IEnumerator ConvertTrackedDolly()
        {
            var vcamGo = CreateGameObject("TestVcam", typeof(CinemachineVirtualCamera));

            yield return null;
            
            var vcam = vcamGo.GetComponent<CinemachineVirtualCamera>();
            vcam.AddCinemachineComponent<CinemachineTrackedDolly>();
            vcam.InvalidateComponentPipeline();

            yield return null;
            
            CinemachineUpgradeManager.UpgradeSingleObject(vcamGo);

            yield return null;

            Assert.That(vcamGo.GetComponent<CinemachineVirtualCamera>(), Is.Null);
            Assert.That(vcamGo.GetComponent<CinemachineTrackedDolly>(), Is.Null);
            Assert.That(vcamGo.transform.childCount, Is.Zero);
            Assert.That(vcamGo.GetComponent<CmCamera>(), Is.Not.Null);
            Assert.That(vcamGo.GetComponent<CinemachineSplineDolly>(), Is.Not.Null);
            Assert.That(vcamGo.GetComponents<MonoBehaviour>().Length, Is.EqualTo(2));
        } 

        [UnityTest]
        public IEnumerator ConvertFreelook()
        {
            var freelookGo = CreateGameObject("TestVcam", typeof(CinemachineFreeLook));

            yield return null;
            
            CinemachineUpgradeManager.UpgradeSingleObject(freelookGo);
            yield return null;

            Assert.That(freelookGo.GetComponent<CinemachineFreeLook>(), Is.Null);
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
