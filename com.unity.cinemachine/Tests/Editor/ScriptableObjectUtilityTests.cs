using NUnit.Framework;
using Cinemachine.Editor;
using System.IO;
using UnityEditor;

namespace Tests.Editor
{
    [TestFixture]
    public class ScriptableObjectUtilityTests
    {
        [Test]
        public void CinemachineInstallPathIsValid()
        {
            var pathToCmLogo = ScriptableObjectUtility.CinemachineInstallPath +
                "/Editor/EditorResources/Icons/CmCamera/" + (EditorGUIUtility.isProSkin ? "Dark/" : "Light/") +
                "VirtualCamera@256.png";
            Assert.That(File.Exists(pathToCmLogo));
        }
        
        [Test]
        public void CinemachineInstallRelativePathIsValid()
        {
            var relativePath = ScriptableObjectUtility.CinemachineRelativeInstallPath +
                "/Editor/EditorResources/Icons/CmCamera/" + (EditorGUIUtility.isProSkin ? "Dark/" : "Light/") +
                "VirtualCamera@256.png";
            var pathToCmLogo = Path.GetFullPath(relativePath);
            Assert.That(File.Exists(pathToCmLogo));
        }
    }
}