using System;
using System.Collections;
using Cinemachine;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
     public class UpgradeCm2ToCm3Tests : CinemachineFixtureBase
    {
        GameObject m_MainCamera;

        // CinemachineUpgrader m_Upgrader;
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            m_MainCamera = CreateGameObject("MainCamera", typeof(Camera), typeof(CinemachineBrain));
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator AddAllOneByOne()
        {
            yield return null;
        }
    }
}
