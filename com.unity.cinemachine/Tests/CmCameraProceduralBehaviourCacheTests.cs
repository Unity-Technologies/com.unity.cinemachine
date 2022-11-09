using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Cinemachine;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class CmCameraProceduralBehaviourCacheTests : CinemachineFixtureBase
    {
        CmCamera m_CmCamera;
        static IEnumerable<Type> s_AllCinemachineComponents;

        void DestroyCinemachineComponents()
        {
            foreach (var c in m_CmCamera.GetComponents<CinemachineComponentBase>())
                RuntimeUtility.DestroyObject(c);
        }

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            CreateGameObject("MainCamera", typeof(Camera), typeof(CinemachineBrain));
            m_CmCamera = CreateGameObject("CmCamera", typeof(CmCamera)).GetComponent<CmCamera>();
            m_CmCamera.Priority = 100;
            
            s_AllCinemachineComponents = Cinemachine.Utility.ReflectionHelpers.GetTypesInAllDependentAssemblies((Type t) => 
                typeof(CinemachineComponentBase).IsAssignableFrom(t) && !t.IsAbstract 
                && t.GetCustomAttribute<CameraPipelineAttribute>() != null
                && t.GetCustomAttribute<ObsoleteAttribute>() == null);
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator AddAllOneByOne()
        {
            DestroyCinemachineComponents();
            yield return null;
            Assert.False(m_CmCamera.PipelineCacheInvalidated); // pipeline is rebuilt

            foreach (var cmComponent in s_AllCinemachineComponents)
            {
                var stage = cmComponent.GetCustomAttribute<CameraPipelineAttribute>().Stage;
                Assert.True(m_CmCamera.PeekPipelineCacheType(stage) != cmComponent);
                AddComponent(m_CmCamera.gameObject, cmComponent);
                Assert.True(m_CmCamera.PipelineCacheInvalidated); // invalid pipeline after add
            
                // We expect that the last one added should be in the pipeline
                Assert.True(m_CmCamera.GetCinemachineComponent(stage).GetType() == cmComponent); // pipeline is rebuilt correctly
            }
        }
        
        [UnityTest]
        public IEnumerator AddAndDestroyAllOneByOne()
        {
            DestroyCinemachineComponents();
            yield return null;
            Assert.False(m_CmCamera.PipelineCacheInvalidated); // pipeline is rebuilt 

            foreach (var cmComponent in s_AllCinemachineComponents)
            {
                var stage = cmComponent.GetCustomAttribute<CameraPipelineAttribute>().Stage;
                Assert.True(m_CmCamera.PeekPipelineCacheType(stage) != cmComponent);
                AddComponent(m_CmCamera.gameObject, cmComponent);
                Assert.True(m_CmCamera.PipelineCacheInvalidated); // invalid pipeline after add
            
                Assert.True(m_CmCamera.GetCinemachineComponent(stage).GetType() == cmComponent); // pipeline is rebuilt correctly
                
                var component = m_CmCamera.gameObject.GetComponent(cmComponent);
                RuntimeUtility.DestroyObject(component);
                Assert.True(m_CmCamera.PipelineCacheInvalidated); // invalid pipeline after destroy
                yield return null;
            
                Assert.True(m_CmCamera.GetCinemachineComponent(stage) == null); // pipeline is rebuilt correctly
            }
        }
    }
}