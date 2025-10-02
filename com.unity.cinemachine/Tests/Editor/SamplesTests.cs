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
        
        readonly string[] m_supportedProjectNames =
            { "HDRP", "HDRPInputSystem", "Standalone", "StandaloneInputSystem", "URP", "URPInputSystem" };
        readonly string m_projectName = Application.productName;

        [SetUp]
        public void CheckExistingSamplesAndValidProjects()
        {
            m_NoExistingSamples = m_NoExistingSamples
                                  || !Directory.Exists("Assets/Samples") && !File.Exists("Assets/Samples.meta");

            foreach (var name in m_supportedProjectNames)
            {
                if (name == m_projectName)
                {
                    m_IsSupportedProject = true;
                    break;
                }
            }
        }

        [TearDown]
        public void DeleteImportedSamples()
        {
            if (m_NoExistingSamples || !m_IsSupportedProject)
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

            // Determine if project name contains "Input"
            var projectNameLower = m_projectName.ToLower();
            var projectHasInput = projectNameLower.Contains("input");

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