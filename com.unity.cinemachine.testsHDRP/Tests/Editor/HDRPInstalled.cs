using NUnit.Framework;
using Cinemachine.Editor;
using System.IO;
using UnityEngine.Rendering;

namespace Tests.Editor
{
    [TestFixture]
    public class HDRPInstalled
    {
        [Test]
        public void HDRPIsInstalled()
        {
            Assert.That(GraphicsSettings.renderPipelineAsset != null);
        }
    }
}