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

namespace Tests.Editor
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

        [UnityTest]
        public IEnumerator TestProceduralBehaviourCache_Adds()
        {
            foreach (var testType in s_AllCinemachineComponents)
            {
                var stage = (int) testType.GetCustomAttribute<CameraPipelineAttribute>().Stage;
                Assert.True(m_CmCamera.m_Pipeline[stage] == null || m_CmCamera.m_Pipeline[stage].GetType() != testType);
                m_CmCamera.gameObject.AddComponent(testType);
                Assert.True(m_CmCamera.m_Pipeline == null);
            
                yield return null;
            
                Assert.True(m_CmCamera.m_Pipeline[stage].GetType() == testType);
            }
        }
        
        [UnityTest]
        public IEnumerator TestProceduralBehaviourCache_AddAndDestroy()
        {
            foreach (var testType in s_AllCinemachineComponents)
            {
                var stage = (int) testType.GetCustomAttribute<CameraPipelineAttribute>().Stage;
                Assert.True(m_CmCamera.m_Pipeline[stage] == null || m_CmCamera.m_Pipeline[stage].GetType() != testType);
                m_CmCamera.gameObject.AddComponent(testType);
                Assert.True(m_CmCamera.m_Pipeline == null);
            
                yield return null;
            
                Assert.True(m_CmCamera.m_Pipeline[stage].GetType() == testType);
                
                var component = m_CmCamera.gameObject.GetComponent(testType);
                RuntimeUtility.DestroyObject(component);
                Assert.True(m_CmCamera.m_Pipeline == null);
            
                yield return null;
                
                Assert.True(m_CmCamera.m_Pipeline[stage] == null);
            }
        }
    }
}