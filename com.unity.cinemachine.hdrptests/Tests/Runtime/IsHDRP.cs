using NUnit.Framework;
using UnityEngine.Rendering;

namespace Tests.Runtime.HDRP
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
