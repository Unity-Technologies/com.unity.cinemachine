using NUnit.Framework;
using UnityEngine;

public class RuntimeDampingTest
{
    [Test]
	public void DampFloat()
    {
        const float dampTime = 10f;
        const float initial = 100f;
        float[] fixedFactor = new float[3] { 0.79f, 0f, 1.07f };
        for (int f = 0; f < fixedFactor.Length; ++f)
        {
            float t = 0;
            float r = Cinemachine.Utility.Damper.Damp(initial, dampTime, t);
            Assert.AreEqual(0, r);
            Assert.Less(r, initial);
            const int iterations = 10;
            for (int i = 0; i < iterations; ++i)
            {
                t += dampTime / iterations;
                float fdt = fixedFactor[f] * t;
                string msg = "i = " + i + ", t = " + t + ", fdt = " + fdt;
                if (i != iterations-1)
                    Assert.Less(t, dampTime, msg);
                else
                    t = dampTime;
                float r2 = Cinemachine.Utility.Damper.Damp(initial, dampTime, t);
                Assert.Less(r, r2, msg);
                r = r2;
            }
            //Assert.AreEqual(initial * (1 - MathHelpers.kNegligibleResidual), r, "f = " + f);
        }
	}

/*
	// A UnityTest behaves like a coroutine in PlayMode
	// and allows you to yield null to skip a frame in EditMode
	[UnityTest]
	public IEnumerator PlayModeSampleTestWithEnumeratorPasses() {
		// Use the Assert class to test conditions.
		// yield to skip a frame
		yield return null;
	}
*/
}
