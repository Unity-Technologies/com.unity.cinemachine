using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Cinemachine.Utility;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Utils;

namespace Tests.Runtime
{
    public class CmCameraTests : CinemachineFixtureBase
    {
        CmCamera m_CmCamera;

        [SetUp]
        public override void SetUp()
        {
            CreateGameObject("MainCamera", typeof(Camera), typeof(CinemachineBrain)).GetComponent<Camera>();
            var cmCameraGo = CreateGameObject("CmCamera", typeof(CmCamera));
            m_CmCamera = cmCameraGo.GetComponent<CmCamera>();
            m_CmCamera.Priority = 100;
            base.SetUp();
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
        }

        private static IEnumerable ColliderTestCases
        {
            get
            {
                yield return new TestCaseData(new[] {Vector2.left, Vector2.up, Vector2.right, Vector2.down}).SetName("Clockwise").Returns(null);
                yield return new TestCaseData(new[] {Vector2.left, Vector2.down, Vector2.right, Vector2.up}).SetName("Counter-Clockwise").Returns(null);
            }
        }

        [UnityTest, TestCaseSource(nameof(ColliderTestCases))]
        public IEnumerator TestCacheValidity(Vector2[] testPoints)
        {
            Undo.AddComponent(m_CmCamera.gameObject, typeof(Cinemachine3rdPersonFollow));
            
            yield return null;
            Assert.True(m_CmCamera.m_Pipeline[(int) CinemachineCore.Stage.Body] is Cinemachine3rdPersonFollow);
        }
    }
}