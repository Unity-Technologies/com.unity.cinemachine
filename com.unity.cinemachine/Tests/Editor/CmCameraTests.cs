using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Cinemachine;
using Cinemachine.Utility;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    public class CmCameraTests
    {
        GameObject m_MainCamera;
        CmCamera m_CmCamera;
        static IEnumerable<Type> s_AllCinemachineComponents;

        [SetUp]
        public void SetUp()
        {
            m_MainCamera = new GameObject("MainCamera");
            m_MainCamera.AddComponent<Camera>();
            m_MainCamera.AddComponent<CinemachineBrain>();
            var cmCameraGo = new GameObject("CmCamera");
            m_CmCamera = cmCameraGo.AddComponent<CmCamera>();
            m_CmCamera.Priority = 100;
            
            s_AllCinemachineComponents = ReflectionHelpers.GetTypesInAllDependentAssemblies((Type t) => 
                typeof(CinemachineComponentBase).IsAssignableFrom(t) && !t.IsAbstract 
                && t.GetCustomAttribute<CameraPipelineAttribute>() != null
                && t.GetCustomAttribute<ObsoleteAttribute>() == null);
        }

        [TearDown]
        public void TearDown()
        {
            RuntimeUtility.DestroyObject(m_CmCamera.gameObject);
            RuntimeUtility.DestroyObject(m_MainCamera);
        }

        [UnityTest]
        public IEnumerator ProceduralBehaviourCache_AddAllOneByOne()
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
        public IEnumerator ProceduralBehaviourCache_AddAndDestroyAllOneByOne()
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
        public IEnumerator ProceduralBehaviourCache_AddAllAtTheSameTimeThenRemoveAllAtTheSameTime()
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
            
            foreach (var toRemove in finalComponentsAdded)
            {
                if (toRemove == null) 
                    continue;
                
                RuntimeUtility.DestroyObject(m_CmCamera.gameObject.GetComponent(toRemove));
            }
            
            yield return null;
            
            for (var i = 0; i < finalComponentsAdded.Length; ++i)
            {
                Assert.True(m_CmCamera.m_Pipeline[i] == null);
            }
        }
    }
}