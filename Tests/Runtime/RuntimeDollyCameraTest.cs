// using System.Collections;
// using System.Collections.Generic;
// using NUnit.Framework;
// using UnityEngine;
// using UnityEngine.TestTools;
// using UnityEngine.Splines;
// using Unity.Mathematics;
// using Cinemachine;
// using Cinemachine.Editor;
//
// public class RuntimeDollyCameraTest
// {
//     CinemachineVirtualCamera m_Vcam;
//     CinemachineTrackedDolly m_Dolly;
//     CinemachineSplinePath m_Path;
//
//     [SetUp]
//     public void Setup()
//     {
//         m_Vcam = CinemachineMenu.InternalCreateVirtualCamera("CM vcam", true, typeof(CinemachineComposer), typeof(CinemachineTrackedDolly));
//         m_Path = new GameObject().AddComponent<CinemachineSplinePath>();
//         m_Path.Spline = SplineFactory.CreateLinear(
//             new List<float3> { new float3(7, 1, -6), new float3(13, 1, -6), new float3(13, 1, 1), new float3(7, 1, 1) }, 
//             true);
//         m_Dolly = m_Vcam.GetCinemachineComponent<CinemachineTrackedDolly>();
//         m_Dolly.m_Path = m_Path;
//         m_Dolly.m_ZDamping = 0;
//     }
//
//     [UnityTest]
//     public IEnumerator LinearInterpolationTestDistance()
//     {
//         m_Dolly.m_PositionUnits = CinemachinePathBase.PositionUnits.Distance;
//         m_Dolly.m_PathPosition = 0;
//         m_Vcam.InternalUpdateCameraState(Vector3.up, 0);
//         yield return null;
//         UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_Vcam.State.FinalPosition, new Vector3(7, 1, -6)), 0, 0.1f);
//         
//         m_Dolly.m_PathPosition = 0.5f;
//         m_Vcam.InternalUpdateCameraState(Vector3.up, 0);
//         yield return null;
//         UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_Vcam.State.FinalPosition, new Vector3(7.5f, 1, -6)), 0, 0.1f);
//         
//         m_Dolly.m_PathPosition = 6;
//         m_Vcam.InternalUpdateCameraState(Vector3.up, 0);
//         yield return null;
//         UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_Vcam.State.FinalPosition, new Vector3(12.98846f, 1, -6)), 0, 0.1f);
//         
//         m_Dolly.m_PathPosition = 10;
//         m_Vcam.InternalUpdateCameraState(Vector3.up, 0);
//         yield return null;
//         UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_Vcam.State.FinalPosition, new Vector3(13, 1, -2)), 0, 0.1f);
//         
//         m_Dolly.m_PathPosition = 15;
//         m_Vcam.InternalUpdateCameraState(Vector3.up, 0);
//         yield return null;
//         UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_Vcam.State.FinalPosition, new Vector3(11, 1, 1)), 0, 0.1f);
//         
//         m_Dolly.m_PathPosition = 20;
//         m_Vcam.InternalUpdateCameraState(Vector3.up, 0);
//         yield return null;
//         UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_Vcam.State.FinalPosition, new Vector3(7, 1, 0)), 0, 0.1f);
//         
//         m_Dolly.m_PathPosition = 26;
//         m_Vcam.InternalUpdateCameraState(Vector3.up, 0);
//         yield return null;
//         UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_Vcam.State.FinalPosition, new Vector3(7, 1, -6)), 0, 0.1f);
//     }
//
//     [UnityTest]
//     public IEnumerator LinearInterpolationTestNormalized()
//     {
//         m_Dolly.m_PositionUnits = CinemachinePathBase.PositionUnits.Normalized;
//         m_Dolly.m_PathPosition = 0;
//         m_Vcam.InternalUpdateCameraState(Vector3.up, 0);
//         yield return null;
//         UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_Vcam.State.FinalPosition, new Vector3(7, 1, -6)), 0, 0.1f);
//         
//         m_Dolly.m_PathPosition = 0.125f;
//         m_Vcam.InternalUpdateCameraState(Vector3.up, 0);
//         yield return null;
//         UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_Vcam.State.FinalPosition, new Vector3(10.25f, 1, -6)), 0, 0.1f);
//         
//         m_Dolly.m_PathPosition = 0.5f;
//         m_Vcam.InternalUpdateCameraState(Vector3.up, 0);
//         yield return null;
//         UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_Vcam.State.FinalPosition, new Vector3(13, 1, 1)), 0, 0.1f);
//         
//         m_Dolly.m_PathPosition = 0.75f;
//         m_Vcam.InternalUpdateCameraState(Vector3.up, 0);
//         yield return null;
//         UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_Vcam.State.FinalPosition, new Vector3(7, 1, 0.5f)), 0, 0.1f);
//         
//         m_Dolly.m_PathPosition = 1;
//         m_Vcam.InternalUpdateCameraState(Vector3.up, 0);
//         yield return null;
//         UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_Vcam.State.FinalPosition, new Vector3(7, 1, -6)), 0, 0.1f);
//     }
//
//     [UnityTest]
//     public IEnumerator LinearInterpolationTestPathUnits()
//     {
//         m_Dolly.m_PositionUnits = CinemachinePathBase.PositionUnits.PathUnits;
//         m_Dolly.m_PathPosition = 0;
//         m_Vcam.InternalUpdateCameraState(Vector3.up, 0);
//         yield return null;
//         UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_Vcam.State.FinalPosition, new Vector3(7, 1, -6)), 0, 0.1f);
//         
//         m_Dolly.m_PathPosition = 0.125f;
//         m_Vcam.InternalUpdateCameraState(Vector3.up, 0);
//         yield return null;
//         UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_Vcam.State.FinalPosition, new Vector3(7.75f, 1, -6)), 0, 0.1f);
//         
//         m_Dolly.m_PathPosition = 0.5f;
//         m_Vcam.InternalUpdateCameraState(Vector3.up, 0);
//         yield return null;
//         UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_Vcam.State.FinalPosition, new Vector3(10, 1, -6)), 0, 0.1f);
//         
//         m_Dolly.m_PathPosition = 1;
//         m_Vcam.InternalUpdateCameraState(Vector3.up, 0);
//         yield return null;
//         UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_Vcam.State.FinalPosition, new Vector3(13, 1, -6)), 0, 0.1f);
//         
//         m_Dolly.m_PathPosition = 1.5f;
//         m_Vcam.InternalUpdateCameraState(Vector3.up, 0);
//         yield return null;
//         UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_Vcam.State.FinalPosition, new Vector3(13, 1, -2.5f)), 0, 0.1f);
//         
//         m_Dolly.m_PathPosition = 1.75f;
//         m_Vcam.InternalUpdateCameraState(Vector3.up, 0);
//         yield return null;
//         UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_Vcam.State.FinalPosition, new Vector3(13, 1, -0.75f)), 0, 0.1f);
//         
//         m_Dolly.m_PathPosition = 3;
//         m_Vcam.InternalUpdateCameraState(Vector3.up, 0);
//         yield return null;
//         UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_Vcam.State.FinalPosition, new Vector3(7, 1, 1)), 0, 0.1f);
//         
//         m_Dolly.m_PathPosition = 3.5f;
//         m_Vcam.InternalUpdateCameraState(Vector3.up, 0);
//         yield return null;
//         UnityEngine.Assertions.Assert.AreApproximatelyEqual(Vector3.Distance(m_Vcam.State.FinalPosition, new Vector3(7, 1, -2.5f)), 0, 0.1f);
//     }
//
// }
