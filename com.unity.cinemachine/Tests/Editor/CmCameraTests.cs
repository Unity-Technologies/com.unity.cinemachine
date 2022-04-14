using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Cinemachine;
using Cinemachine.Utility;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Utils;

namespace Tests.Runtime
{
    public class CmCameraTests
    {
        CmCamera m_CmCamera;
        static IEnumerable<Type> s_AllCinemachineComponents;

        [SetUp]
        public void SetUp()
        {
            var mainCameraGo = new GameObject("MainCamera");
            mainCameraGo.AddComponent<Camera>();
            mainCameraGo.AddComponent<CinemachineBrain>();
            var cmCameraGo = new GameObject("CmCamera");
            m_CmCamera = cmCameraGo.AddComponent<CmCamera>();
            m_CmCamera.Priority = 100;
            
            s_AllCinemachineComponents = ReflectionHelpers.GetTypesInAllDependentAssemblies((Type t) => 
                typeof(CinemachineComponentBase).IsAssignableFrom(t) && !t.IsAbstract 
                && t.GetCustomAttribute<CameraPipelineAttribute>() != null
                && t.GetCustomAttribute<ObsoleteAttribute>() == null);
        }

        private static IEnumerable CacheTestCases
        {
            get
            {
                yield return new TestCaseData(s_AllCinemachineComponents).SetName("AllCinemachineComponents").Returns(null);
            }
        }

        [UnityTest, TestCaseSource(nameof(CacheTestCases))]
        public IEnumerator TestCacheValidity(Type testType)
        {
            var stage = (int) testType.GetCustomAttribute<CameraPipelineAttribute>().Stage;
            Assert.True(m_CmCamera.m_Pipeline[stage] is null);
            Undo.AddComponent(m_CmCamera.gameObject, typeof(Cinemachine3rdPersonFollow));
            Assert.True(m_CmCamera.m_Pipeline[stage] is null);
            
            yield return null;
            
            Assert.True(m_CmCamera.m_Pipeline[stage] is Cinemachine3rdPersonFollow);
        }
    }
}