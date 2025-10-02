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
        [SerializeField] bool m_NoExistingSamples; // Serialize to restore after domain reload.
        bool m_IsSupportedProject;

        [SetUp]
        public void CheckExistingSamplesAndValidProjects()
        {
            string[] supportedProjectNames =
                { "HDRP", "HDRPInputSystem", "Standalone", "StandaloneInputSystem", "URP", "URPInputSystem" };
            string projectName = Application.productName;
            
            foreach (var name in supportedProjectNames)
            {
                if (name == projectName)
                {
                    m_IsSupportedProject = true;
                    break;
                }
            }
            
            m_NoExistingSamples = m_NoExistingSamples
                                  || !Directory.Exists("Assets/Samples") && !File.Exists("Assets/Samples.meta");
        }

        [TearDown]
        public void DeleteImportedSamples()
        {
            if (m_NoExistingSamples && m_IsSupportedProject)
            {
                if (Directory.Exists("Assets/Samples"))
                {
                    Directory.Delete("Assets/Samples", recursive: true);
                }
                if (File.Exists("Assets/Samples.meta"))
                {
                    File.Delete("Assets/Samples.meta");
                }
            }
        }

        [UnityTest]
        public IEnumerator ImportSamples()
        {
            // Skip test if project name is not in list of supported project names and/or samples not already imported.
            if (!m_IsSupportedProject)
            {
                Assert.Ignore($"Project not valid for Project Testing. Skipping sample import test.");
            }
            Assume.That(m_NoExistingSamples, Is.True, "Samples already imported");

            var packageInfo = PackageInfo.FindForAssetPath("Packages/com.unity.cinemachine");
            var version = packageInfo.version;

            // Import Shared Assets manually since Package Manager API cannot do it.
            var sharedAssetsSource = Path.Combine(packageInfo.resolvedPath, "Samples~", "Shared Assets");
            var sharedAssetsDest = Path.Combine("Assets/Samples/Cinemachine", version, "Shared Assets");
            if (Directory.Exists(sharedAssetsSource) && !Directory.Exists(sharedAssetsDest))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(sharedAssetsDest));
                CopyDirectory(sharedAssetsSource, sharedAssetsDest);
            }

            // Determine if project has "Input System" package installed
            var projectHasInput = PackageInfo.FindForAssetPath("Packages/com.unity.inputsystem") != null;

            // Import samples using Package Manager API
            var samples = Sample.FindByPackage(packageInfo.name, version);
            foreach (var sample in samples)
            {
                // Skip importing "Input System Samples" if project doesn't have "Input System" package installed
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