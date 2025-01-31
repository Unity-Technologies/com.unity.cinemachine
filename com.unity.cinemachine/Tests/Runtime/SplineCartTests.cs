using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Splines;
using Unity.Mathematics;

namespace Unity.Cinemachine.Tests
{
    [TestFixture]
    public class CinemachineSplineCartTest : CinemachineRuntimeFixtureBase
    {
        CinemachineSplineCart m_CmSplineCart;
        SplineContainer m_SplineContainer;

        [SetUp]
        public void Setup()
        {
            base.SetUp();

            m_SplineContainer = CreateGameObject("Dolly Track", typeof(SplineContainer)).GetComponent<SplineContainer>();
            m_SplineContainer.Spline = SplineFactory.CreateLinear(
                new List<float3> { new(7, 1, -6), new(13, 1, -6), new(13, 1, 1), new(7, 1, 1) }, true);
            m_CmSplineCart = CreateGameObject("CM cart", typeof(CinemachineSplineCart)).GetComponent<CinemachineSplineCart>();
            m_CmSplineCart.Spline = m_SplineContainer;
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator CartPositionIsCorrect()
        {
            m_CmSplineCart.PositionUnits = PathIndexUnit.Distance;
            m_CmSplineCart.SplinePosition = 0;
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(
                Vector3.Distance(m_CmSplineCart.transform.position, new Vector3(7, 1, -6)), 0, 0.1f);

            m_CmSplineCart.PositionUnits = PathIndexUnit.Normalized;
            m_CmSplineCart.SplinePosition = 1;
            yield return null;
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(
                Vector3.Distance(m_CmSplineCart.transform.position, new Vector3(7, 1, -6)), 0, 0.1f);
        }
    }
}
