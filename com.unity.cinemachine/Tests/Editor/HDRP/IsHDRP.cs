#if TEST_CINEMACHINE_HDRP 
using NUnit.Framework;
using UnityEngine.Rendering;

namespace Unity.Cinemachine.Tests.HDRP.Editor
{
    [TestFixture]
    public class IsHDRPTests
    {
        [Test]
        public void IsHDRP()
        {
            UnityEngine.Assertions.Assert.IsNotNull(GraphicsSettings.renderPipelineAsset);
        }
    }
}
#endif
