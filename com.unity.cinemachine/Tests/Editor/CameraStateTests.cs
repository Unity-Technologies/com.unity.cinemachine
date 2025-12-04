using NUnit.Framework;
using UnityEngine.TestTools.Utils;
using BlendHints = Unity.Cinemachine.CameraState.BlendHints;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace Unity.Cinemachine.Tests.Editor
{
    [TestFixture]
    public class CameraStateTests
    {
        QuaternionEqualityComparer QuaternionComparer = new QuaternionEqualityComparer(1e-5f);
       
        static TestCaseData[] s_IgnoreLookAtTarget_TestCases =
        {
            new TestCaseData(BlendHints.Nothing, BlendHints.Nothing)
                .SetName("Without blend hints"),
            new TestCaseData(BlendHints.IgnoreLookAtTarget, BlendHints.Nothing)
                .SetName("State A ignores lookAt"),
            new TestCaseData(BlendHints.Nothing, BlendHints.IgnoreLookAtTarget)
                .SetName("State B ignores lookAt"),
        };
        
        [TestCaseSource(nameof(s_IgnoreLookAtTarget_TestCases))]
        public void CameraState_IgnoreLookAtTarget_In_Lerp_Provides_ExpectedReferenceLookAt(
            BlendHints blendHintsStateA,
            BlendHints blendHintsStateB)
        {

            var stateA = CameraState.Default;
            stateA.ReferenceLookAt = Vector3.forward;
            stateA.BlendHint = blendHintsStateA;

            var stateB = CameraState.Default;
            stateB.ReferenceLookAt = Vector3.right;
            stateB.BlendHint = blendHintsStateB;

            var state = CameraState.Lerp(stateA, stateB, 0.5f);

            var expectedHasLookAt =
                (blendHintsStateA & BlendHints.IgnoreLookAtTarget) == 0 && 
                (blendHintsStateB & BlendHints.IgnoreLookAtTarget) == 0;       
            var expectedReferenceLookAt = new Vector3(0.5f, 0f, 0.5f);
            
            Assert.That(state.HasLookAt(), Is.EqualTo(expectedHasLookAt));
            if (state.HasLookAt())
            {
                Assert.That(state.ReferenceLookAt, Is.EqualTo(expectedReferenceLookAt));
            }
        }
        
        [TestCaseSource(nameof(s_IgnoreLookAtTarget_TestCases))]
        public void CameraState_IgnoreLookAtTarget_In_Lerp_Provides_ExpectedRawOrientation(
            BlendHints blendHintsStateA,
            BlendHints blendHintsStateB)
        {
            var stateA = CameraState.Default;
            stateA.RawOrientation = Quaternion.identity;
            stateA.ReferenceLookAt = new Vector3(0f, 1f, 0f);
            stateA.BlendHint = blendHintsStateA;

            var stateB = CameraState.Default;
            stateB.RawOrientation = Quaternion.Euler(0f, 45f, 0f);
            stateB.ReferenceLookAt = new Vector3(1f, 0f, 0f);
            stateB.BlendHint = blendHintsStateB;

            var state = CameraState.Lerp(stateA, stateB, 0.5f);

            var expectedUsesSlerp =
                (blendHintsStateA & BlendHints.IgnoreLookAtTarget) != 0 || 
                (blendHintsStateB & BlendHints.IgnoreLookAtTarget) != 0;
            var expectedSlerpOrientation = Quaternion.Euler(0f, 22.5f, 0f);
            
            
            if (expectedUsesSlerp)
            {
                Assert.That(state.RawOrientation, Is.EqualTo(expectedSlerpOrientation).Using(QuaternionComparer));
            }
            else
            {
                Assert.That(state.RawOrientation, Is.Not.EqualTo(expectedSlerpOrientation).Using(QuaternionComparer));
            }
        }
    }
}