using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Cinemachine;
using Cinemachine.Editor;
using Cinemachine.Utility;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Tests.Editor
{
     public class UpgradeCm2ToCm3Tests : CinemachineFixtureBase
    {
        CinemachineUpgrader m_Upgrader;
        static IEnumerable<Type> s_AllCinemachineComponents;
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
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
                    && t.GetCustomAttribute<ObsoleteAttribute>() == null);
                foreach (var cmComponent in s_AllCinemachineComponents)
                {
                    yield return new TestCaseData(cmComponent).SetName(cmComponent.Name).Returns(null);
                }
            }
        }

#pragma warning disable CS0618
        [UnityTest, TestCaseSource(nameof(ConvertTestCases))]
        public IEnumerator ConvertAllDefaultOnes(Type type)
        {
            var vcamGo = CreateGameObject("TestVcam", typeof(CinemachineVirtualCamera));

            yield return null;
            
            var vcam = vcamGo.GetComponent<CinemachineVirtualCamera>();
            AddCinemachineComponent(vcam, type);

            yield return null;
            
            var upgrader = new CinemachineUpgrader();
            upgrader.Upgrade(vcamGo);

            yield return null;

            Assert.That(vcamGo.GetComponent<CinemachineVirtualCamera>(), Is.Null);
            Assert.That(vcamGo.transform.childCount, Is.Zero);
            Assert.That(vcamGo.GetComponent<CmCamera>(), Is.Not.Null);
            Assert.That(vcamGo.GetComponent(type), Is.Not.Null);
        }
        
        static void AddCinemachineComponent(CinemachineVirtualCamera vcam, Type t)
        {
            var pipeline = vcam.GetComponentInChildren<CinemachinePipeline>();
            pipeline.gameObject.AddComponent(t);
            vcam.InvalidateComponentPipeline();
        }
#pragma warning restore CS0618
    }
}
