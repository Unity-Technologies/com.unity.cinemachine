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
#if UNITY_2023_3_OR_NEWER
            UnityEngine.Assertions.Assert.IsNotNull(GraphicsSettings.defaultRenderPipeline);
#else
            UnityEngine.Assertions.Assert.IsNotNull(GraphicsSettings.renderPipelineAsset);
#endif
        }
    }
}
#endif