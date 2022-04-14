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
        public IEnumerator TestProceduralBehaviourCache_AddAllOneByOne()
        {
            foreach (var cmComponent in s_AllCinemachineComponents)
            {
                var stage = (int) cmComponent.GetCustomAttribute<CameraPipelineAttribute>().Stage;
                Assert.True(m_CmCamera.m_Pipeline[stage] == null || m_CmCamera.m_Pipeline[stage].GetType() != cmComponent);
                m_CmCamera.gameObject.AddComponent(cmComponent);
                Assert.True(m_CmCamera.m_Pipeline == null); // invalid pipeline after add
            
                yield return null;
            
                Assert.True(m_CmCamera.m_Pipeline[stage].GetType() == cmComponent); // pipeline is rebuilt correctly
            }
        }
        
        [UnityTest]
        public IEnumerator TestProceduralBehaviourCache_AddAndDestroyAllOneByOne()
        {
            foreach (var cmComponent in s_AllCinemachineComponents)
            {
                var stage = (int) cmComponent.GetCustomAttribute<CameraPipelineAttribute>().Stage;
                Assert.True(m_CmCamera.m_Pipeline[stage] == null || m_CmCamera.m_Pipeline[stage].GetType() != cmComponent);
                m_CmCamera.gameObject.AddComponent(cmComponent);
                Assert.True(m_CmCamera.m_Pipeline == null); // invalid pipeline after add
            
                yield return null;
            
                Assert.True(m_CmCamera.m_Pipeline[stage].GetType() == cmComponent); // pipeline is rebuilt correctly
                
                var component = m_CmCamera.gameObject.GetComponent(cmComponent);
                RuntimeUtility.DestroyObject(component);
                Assert.True(m_CmCamera.m_Pipeline == null); // invalid pipeline after add
            
                yield return null;
                
                Assert.True(m_CmCamera.m_Pipeline[stage] == null); // pipeline is rebuilt correctly
            }
        }
        
        [UnityTest]
        public IEnumerator TestProceduralBehaviourCache_AddAllAtTheSameTime()
        {
            var finalComponentsAdded = new Type[Enum.GetValues(typeof(CinemachineCore.Stage)).Length];
            foreach (var cmComponent in s_AllCinemachineComponents)
            {
                var stage = (int) cmComponent.GetCustomAttribute<CameraPipelineAttribute>().Stage;
                finalComponentsAdded[stage] = cmComponent;
                m_CmCamera.gameObject.AddComponent(cmComponent);
            }
            
            yield return null;
            
            for (var i = 0; i < finalComponentsAdded.Length; ++i)
            {
                Assert.True((m_CmCamera.m_Pipeline[i] == null && finalComponentsAdded[i] == null) ||
                    m_CmCamera.m_Pipeline[i].GetType() == finalComponentsAdded[i]);
            }
        }
    }
}