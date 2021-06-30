using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using Cinemachine;

public class AxisStateTests
{
    struct TestAxisProvider : AxisState.IInputAxisProvider
    {
        public float value;

        public TestAxisProvider(float value)
        {
            this.value = value;
        }
        
        public float GetAxisValue(int axis)
        {
            return value;
        }
    }
    
    static IEnumerable AxisStateTestCases
    {
        get
        {
            yield return new TestCaseData(-100f, 100f, false, 10f, 0.5f, 0.5f, false, 0.1f).Returns(1.0f);
            yield return new TestCaseData(-100f, 100f, false, 10f, 0.5f, 0.5f, false, 0.5f).Returns(5.0f);
            yield return new TestCaseData(-100f, 100f, false, 1f, 0.5f, 0.5f, false, 1.0f).Returns(1.0f);
            yield return new TestCaseData(-100f, 100f, false, 10f, 0.5f, 0.5f, false, 100.0f).Returns(10.0f);
            yield return new TestCaseData(-13f, 5f, false, 10f, 0.5f, 0.5f, false, 100.0f).Returns(5.0f);
            yield return new TestCaseData(-13f, 5f, true, 10f, 0.5f, 0.5f, false, 100.0f).Returns(-12.0f);

            yield return new TestCaseData(-100f, 100f, false, 10f, 0.5f, 0.5f, true, 0.1f).Returns(-1.0f);
            yield return new TestCaseData(-100f, 100f, false, 10f, 0.5f, 0.5f, true, 0.5f).Returns(-5.0f);
            yield return new TestCaseData(-100f, 100f, false, 1f, 0.5f, 0.5f, true, 1.0f).Returns(-1.0f);
            yield return new TestCaseData(-100f, 100f, false, 10f, 0.5f, 0.5f, true, 100.0f).Returns(-10.0f);
            yield return new TestCaseData(-13f, 5f, false, 10f, 0.5f, 0.5f, true, 100.0f).Returns(-13.0f);
            yield return new TestCaseData(-13f, 5f, true, 10f, 0.5f, 0.5f, true, 100.0f).Returns(4.0f);
        }
    }
    
    [Test, TestCaseSource(nameof(AxisStateTestCases))]
    public float TestAxisState(float minValue, float maxValue, bool wrap, 
        float maxSpeed, float accelTime, float decelTime,
        bool invert, float axisValue)
    {
        var axisState = new AxisState(minValue, maxValue, wrap, false, maxSpeed, accelTime, decelTime, null, invert);
        axisState.SetInputAxisProvider(0, new TestAxisProvider(axisValue));
        axisState.Validate();
        
        var success = axisState.Update(1.0f);
        Assert.IsTrue(success, "Update had no effect");

        return axisState.Value;
    }

    static IEnumerable RecenteringTestCases
    {
        get
        {
            yield return new TestCaseData(-100f, 100f, false, 10f, 0.5f, 0.5f, false, 0.1f, true, 0.01f).Returns(0.0f);
            yield return new TestCaseData(-100f, 100f, false, 10f, 0.5f, 0.5f, false, 0.1f, true, 5f).Returns(0.180375189f);

            yield return new TestCaseData(-100f, 100f, false, 10f, 0.5f, 0.5f, false, 0.1f, false, 0.01f).Returns(1.0f);
            yield return new TestCaseData(-100f, 100f, false, 10f, 0.5f, 0.5f, false, 0.1f, false, 5f).Returns(1.0f);

        }
    }

    [Test, TestCaseSource(nameof(RecenteringTestCases))]
    public float TestRecentering(float minValue, float maxValue, bool wrap,
        float maxSpeed, float accelTime, float decelTime, bool invert, float axisValue,
        bool enabled, float recenteringTime)
    {
        var axisState = new AxisState(minValue, maxValue, false, false, maxSpeed, accelTime, decelTime, null, invert);
        axisState.SetInputAxisProvider(0, new TestAxisProvider(axisValue));
        axisState.Validate();

        var success = axisState.Update(1.0f);
        Assert.IsTrue(success, "Update had no effect");

        var recentering = new AxisState.Recentering(enabled, 0.0f, recenteringTime);
        recentering.DoRecentering(ref axisState, 10.0f, 0.0f);

        return axisState.Value < Cinemachine.Utility.UnityVectorExtensions.Epsilon ? 0f : axisState.Value;
    }

    [TearDown]
    public static void TearDown()
    {
        Cinemachine.CinemachineCore.CurrentTimeOverride = -1;
    }
}
