using NUnit.Framework;
using UnityEngine.Rendering;

namespace Tests.HDRP.Runtime
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
