using NUnit.Framework;
using System.IO;

namespace Unity.Cinemachine.Tests.Editor
{
    [TestFixture]
    public class ScriptableObjectUtilityTests
    {
        [Test]
        public void CinemachineInstallPathIsValid()
        {
            var pathToCmLogo = Path.Combine(CinemachineCore.kPackageRoot + 
                "/Editor/EditorResources/Icons/CmCamera@256.png");
            Assert.That(File.Exists(pathToCmLogo));
        }
        
        [Test]
        public void CinemachineInstallRelativePathIsValid()
        {
            var relativePathToCmLogo = Path.Combine(CinemachineCore.kPackageRoot + 
                "/Editor/EditorResources/Icons/CmCamera@256.png");
            var pathToCmLogo = Path.GetFullPath(relativePathToCmLogo);
            Assert.That(File.Exists(pathToCmLogo));
        }
    }
}