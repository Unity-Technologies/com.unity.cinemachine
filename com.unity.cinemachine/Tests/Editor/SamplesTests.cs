using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEngine.TestTools;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Unity.Cinemachine.Tests.Editor
{
    public class SamplesTests
    {
        [SerializeField] bool s_NoExistingSamples; // Serialize to restore after domain reload.

        [SetUp]
        public void CheckExistingSamples()
        {
            s_NoExistingSamples = s_NoExistingSamples 
                                  || !Directory.Exists("Assets/Samples") && !File.Exists("Assets/Samples.meta");
        }

        [TearDown]
        public void DeleteImportedSamples()
        {
            if (s_NoExistingSamples)
            {
                Directory.Delete("Assets/Samples", recursive: true);
                File.Delete("Assets/Samples.meta");
            }
        }

        [UnityTest]
        public IEnumerator ImportSamples()
        {
            Assume.That(s_NoExistingSamples, Is.True, "Samples already imported");

            var packageInfo = PackageInfo.FindForAssetPath("Packages/com.unity.cinemachine");
            var version = packageInfo.version;
            
            // Import Shared Assets manually if not already present
            var sharedAssetsSource = Path.Combine(packageInfo.resolvedPath, "Samples~", "Shared Assets");
            var sharedAssetsDest = Path.Combine("Assets/Samples/Cinemachine", version, "Shared Assets");
            if (Directory.Exists(sharedAssetsSource) && !Directory.Exists(sharedAssetsDest))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(sharedAssetsDest));
                CopyDirectory(sharedAssetsSource, sharedAssetsDest);
            }

            // Determine if project name contains "Input"
            var projectName = Application.productName;
            var projectHasInput = projectName.ToLower().Contains("input");

            // Import samples using Package Manager API
            var samples = Sample.FindByPackage(packageInfo.name, version);
            foreach (var sample in samples)
            {
                // Skip importing "Input System Samples" if project name does not contain "input"
                bool isInputSample = sample.displayName.ToLower().Contains("input");
                if (isInputSample && !projectHasInput)
                    continue;
                
                sample.Import(Sample.ImportOptions.OverridePreviousImports | Sample.ImportOptions.HideImportWindow);
            }
            yield return new WaitForDomainReload();
        }

        // Recursively copy a directory
        static void CopyDirectory(string sourceDir, string destDir)
        {
            foreach (var dir in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dir.Replace(sourceDir, destDir));
            }
            foreach (var file in Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories))
            {
                File.Copy(file, file.Replace(sourceDir, destDir), true);
            }
        }
    }
}