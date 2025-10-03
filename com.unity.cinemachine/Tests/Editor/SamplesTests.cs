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
        // We only want to run this test on our test projects, which we assume to have no samples imported yet.
        bool IsSupportedProject() => Application.productName == "CinemachineTestProject" && Application.companyName == "Unity";

        void DeleteSamplesIfExisting()
        {
            if (Directory.Exists("Assets/Samples/Cinemachine"))
                Directory.Delete("Assets/Samples/Cinemachine", recursive: true);
            if (File.Exists("Assets/Samples/Cinemachine.meta"))
                File.Delete("Assets/Samples/Cinemachine.meta");
        }

        [UnityTest]
        public IEnumerator TestImportSamples()
        {
            // Skip test if project name is not in list of supported projects.
            if (!IsSupportedProject())
                Assert.Ignore($"Project is not valid for Samples Testing. Skipping sample import test.");

            DeleteSamplesIfExisting();

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
                Directory.CreateDirectory(dir.Replace(sourceDir, destDir));

            foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
                File.Copy(file, file.Replace(sourceDir, destDir), true);
        }
    }
}