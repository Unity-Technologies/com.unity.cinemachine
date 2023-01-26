using System;
using System.Collections;
using NUnit.Framework;
using Cinemachine;
using UnityEngine;

namespace Tests.Editor
{    
    [TestFixture]
    public class InputAxisTests
    {
        const float k_DeltaTime = 0.1f;
        [SetUp] public void Setup() { CinemachineCore.CurrentUnscaledTimeTimeOverride = 0f; }
        [TearDown] public void TearDown() { CinemachineCore.CurrentUnscaledTimeTimeOverride = -1; }
        
        static IEnumerable AxisDriverAccelTestCases
        {
            get
            {
                yield return new TestCaseData(0);
                yield return new TestCaseData(0.1f);
                yield return new TestCaseData(0.2f);
                yield return new TestCaseData(0.3f);
                yield return new TestCaseData(0.4f);
                yield return new TestCaseData(0.5f);
            }
        }

        [Test, TestCaseSource(nameof(AxisDriverAccelTestCases))]
        public void TestAxisDriverAccel(float accelTime)
        {
            var axis = new InputAxis { Range = new Vector2(-100f, 100f), Value = 0, Center = 0, Wrap = false };
            var control = new InputAxisControl { InputValue = 1, AccelTime = accelTime, DecelTime = accelTime };
            axis.Validate();
            control.Validate();
            var driver = new InputAxisDriver();
            var prevValue = axis.Value;
            var prevDelta = 0f;

            // Accelerate to speed
            for (int i = 0; i < 20; ++i)
            {
                driver.ProcessInput(k_DeltaTime, ref axis, ref control);
                var delta = axis.Value - prevValue;
                UnityEngine.Assertions.Assert.IsTrue(delta > prevDelta); // must be speeding up
                prevValue = axis.Value;
                prevDelta = delta;
                if (k_DeltaTime * i >= accelTime)
                    break;
                UnityEngine.Assertions.Assert.IsTrue(delta < control.InputValue * k_DeltaTime); // must be not at target speed
            }
            // Must have reached the target speed
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(prevDelta, control.InputValue * k_DeltaTime, 0.001f);

            // Decelerate to zero
            control.InputValue = 0;
            for (int i = 0; i < 20; ++i)
            {
                driver.ProcessInput(k_DeltaTime, ref axis, ref control);
                var delta = axis.Value - prevValue;
                UnityEngine.Assertions.Assert.IsTrue(delta < prevDelta); // must be slowing down
                prevValue = axis.Value;
                prevDelta = delta;
                if (k_DeltaTime * i >= accelTime)
                    break;
                UnityEngine.Assertions.Assert.IsTrue(delta > 0); // must be not at target speed
            }
            // Must have reached zero speed
            UnityEngine.Assertions.Assert.AreApproximatelyEqual(prevDelta, 0, 0.001f);
        }

        static IEnumerable AxisStateTestCases
        {
            get
            {
                yield return new TestCaseData(new Vector2(-100f, 100f), false, 0.5f, 0.5f, 0.1f, 
                    new[]{0.006018929f, 0.01443404f, 0.02380308f});
                yield return new TestCaseData(new Vector2(-100f, 100f), false, 0.5f, 0.5f, 0.5f,
                    new[]{0.03009464f, 0.07217018f, 0.1190154f});
                yield return new TestCaseData(new Vector2(-100f, 100f), false, 0.5f, 0.5f, 1.0f,
                    new[]{0.06018928f, 0.1443404f, 0.2380308f});
                yield return new TestCaseData(new Vector2(-100f, 100f), false, 0.5f, 0.5f, 100.0f,
                    new[]{6.018928f, 14.43403f, 23.80308f});
                yield return new TestCaseData(new Vector2(-13f, 5f), false, 0.5f, 0.5f, 100.0f,
                    new[]{3.009464f, 4.207553f, 4.684521f});
                yield return new TestCaseData(new Vector2(-13f, 5f), true, 0.5f, 0.5f, 100.0f,
                    new[]{-11.98107f, -3.565965f, -12.19692f});
            }
        }

        [Test, TestCaseSource(nameof(AxisStateTestCases))]
        public void TestInputAxis(
            Vector2 range, bool wrap, float accelTime, float decelTime, float inputValue, float[] expectedResults)
        {
            var axis = new InputAxis
            {
                Range = range,
                Center = 0,
                Wrap = wrap,
            };
            axis.Validate();
            
            var control = new InputAxisControl
            {
                InputValue = inputValue,
                AccelTime = accelTime,
                DecelTime = decelTime,
            };
            control.Validate();

            var driver = new InputAxisDriver();
            foreach (var result in expectedResults)
            {
                driver.ProcessInput(k_DeltaTime, ref axis, ref control);
                UnityEngine.Assertions.Assert.AreApproximatelyEqual(axis.Value, result);
            }
        }

        static IEnumerable RecenteringTestCases
        {
            get
            {
                yield return new TestCaseData(new Vector2(-100f, 100f), false, 0.5f, 0.5f, 0.1f, true, 0.01f, 
                    new[]{0.006018929f, 2.176916E-05f, 0});
                yield return new TestCaseData(new Vector2(-100f, 100f), false, 0.5f, 0.5f, 0.1f, true, 2.5f, 
                    new[]{0.006018929f, 0.008320068f, 0.009012762f});
                yield return new TestCaseData(new Vector2(-100f, 100f), false, 0.5f, 0.5f, 0.1f, true, 5f, 
                    new[]{0.006018929f, 0.008390307f, 0.009270803f});

                yield return new TestCaseData(new Vector2(-100f, 100f), false, 0.5f, 0.5f, 0.1f, false, 0.01f, 
                    new[]{0.006018929f, 0.008415108f, 0.009369044f});
                yield return new TestCaseData(new Vector2(-100f, 100f), false, 0.5f, 0.5f, 0.1f, false, 5f, 
                    new[]{0.006018929f, 0.008415108f, 0.009369044f});

            }
        }

        [Test, TestCaseSource(nameof(RecenteringTestCases))]
        public void TestInputAxisRecentering(Vector2 range, bool wrap,
            float accelTime, float decelTime, float inputValue,
            bool enabled, float recenteringTime, float[] expectedResults)
        {
            var axis = new InputAxis
            {
                Range = range,
                Center = 0,
                Wrap = wrap,
                Recentering = new () { Enabled = enabled, Time = recenteringTime, Wait = 0 }
            };
            axis.Validate();
            
            var control = new InputAxisControl
            {
                InputValue = inputValue,
                AccelTime = accelTime,
                DecelTime = decelTime,
            };

            control.Validate();

            var driver = new InputAxisDriver();
            foreach (var result in expectedResults)
            {
                driver.ProcessInput(k_DeltaTime, ref axis, ref control);
                axis.Recentering.DoRecentering(k_DeltaTime, ref axis);
                control.InputValue = 0; // cancel input, so recentering can start
                CinemachineCore.CurrentUnscaledTimeTimeOverride += k_DeltaTime; // control time for deterministic tests
                UnityEngine.Assertions.Assert.AreApproximatelyEqual(axis.Value, result);
            }
        }
    }
}