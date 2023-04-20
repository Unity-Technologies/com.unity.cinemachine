#if CINEMACHINE_HDRP 
using NUnit.Framework;
using UnityEngine.Rendering;

namespace Unity.Cinemachine.Tests
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