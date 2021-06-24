using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine.TestTools;

using Cinemachine;
using UnityEditor.VersionControl;
using Object = System.Object;

[TestFixture]
public class FreeLookTests
{
    private const float Epsilon = 0.001f;

    private List<GameObject> m_GameObjectsToDestroy = new List<GameObject>();
    private GameObject CreateGameObject(string name, params System.Type[] components)
    {
        var go = new GameObject();
        m_GameObjectsToDestroy.Add(go);
        go.name = name;
        
        foreach(var c in components)
            if (c.IsSubclassOf(typeof(Component)))
                go.AddComponent(c);
        
        return go;
    }

    class TestAxisProvider : AxisState.IInputAxisProvider
    {
        private float x, y;

        public TestAxisProvider()
        {
            x = 0f;
            y = 0f;
        }
        
        public void SetAxisValues(float x, float y)
        {
            this.x = x;
            this.y = y;
        }
        
        public float GetAxisValue(int axis)
        {
            return axis == 0 ? x : y;
        }
    }

    private CinemachineFreeLook m_FreeLook;
    private TestAxisProvider m_AxisProvider;
    
    [SetUp]
    public void SetUp()
    {
        CreateGameObject("Camera", typeof(Camera), typeof(CinemachineBrain));

        // create a "character"
        var character = CreateGameObject("Character").GetComponent<Transform>();
        character.position.Set(0f, 0f, 0f);
        var body = CreateGameObject("Body").GetComponent<Transform>();
        body.position.Set(0f, 0f, 0f);
        body.parent = character;
        var head = CreateGameObject("Head").GetComponent<Transform>();
        head.position.Set(0f, 1f, 0f);
        head.parent = body;
        
        // Create a free-look camera 
        m_FreeLook = CreateGameObject("CinemachineFreeLook", typeof(CinemachineFreeLook)).GetComponent<CinemachineFreeLook>();
        m_AxisProvider = new TestAxisProvider();
        m_FreeLook.m_XAxis.SetInputAxisProvider(0, m_AxisProvider);
        m_FreeLook.m_YAxis.SetInputAxisProvider(1, m_AxisProvider);
        m_FreeLook.Follow = body;
        m_FreeLook.LookAt = head;
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var go in m_GameObjectsToDestroy)
            UnityEngine.Object.Destroy(go);

        m_GameObjectsToDestroy.Clear();
    }
    
    private static IEnumerable FreeLookTestCases
    {
        get
        {
            yield return new TestCaseData(-45f, 0.0f, new Vector3(2.5f, 2.5f, -1.6f)).SetName("Left X Bottom Y").Returns(null);
            yield return new TestCaseData(-45f, 0.5f, new Vector3(-1.4f, 4.0f, 2.0f)).SetName("Left X Center Y").Returns(null);
            yield return new TestCaseData(-45f, 1.0f, new Vector3(-0.9f, 4.5f, 1.5f)).SetName("Left X Top Y").Returns(null);

            yield return new TestCaseData(0f, 0f, new Vector3(1.3f, 4.5f, -1.2f)).SetName("Center X Bottom Y").Returns(null);
            yield return new TestCaseData(0f, 0.5f, new Vector3(0f, 2.5f, -3.0f)).SetName("Center X Center Y").Returns(null);
            yield return new TestCaseData(0f, 1.0f, new Vector3(0f, 4.5f, -1.8f)).SetName("Center X Top Y").Returns(null);

            yield return new TestCaseData(45f, 0f, new Vector3(0f, 4.5f, -1.8f)).SetName("Right X Bottom Y").Returns(null);
            yield return new TestCaseData(45f, 0.5f, new Vector3(-2.5f, 2.5f, -1.7f)).SetName("Right X Center Y").Returns(null);
            yield return new TestCaseData(45f, 1.0f, new Vector3(-1.3f, 4.5f, -1.1f)).SetName("Right X Top Y").Returns(null);
        }
    }

    [UnityTest, TestCaseSource(nameof(FreeLookTestCases))]
    public IEnumerator TestAxisStateChangeMovesCamera(float axisX, float axisY, Vector3 expectedPosition)
    {
        m_AxisProvider.SetAxisValues(axisX, axisY);

        yield return new WaitForSeconds(1.0f);

        Assert.That(m_FreeLook.transform.position, Is.EqualTo(expectedPosition).Within(Epsilon));
        // Assert.That(m_FreeLook.transform.position.x, Is.EqualTo(expectedPosition.x).Within(Epsilon));
        // Assert.That(m_FreeLook.transform.position.y, Is.EqualTo(expectedPosition.y).Within(Epsilon));
        // Assert.That(m_FreeLook.transform.position.z, Is.EqualTo(expectedPosition.z).Within(Epsilon));
    }
}