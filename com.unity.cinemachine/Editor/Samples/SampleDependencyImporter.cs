using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEngine.UIElements;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Cinemachine.Editor
{
    [InitializeOnLoad]
    class SampleDependencyImporter : IPackageManagerExtension
    {
        /// <summary>
        /// An implementation of AssetPostProcessor which will raise an event when a new asset is imported.
        /// </summary>
        class SamplePostprocessor : AssetPostprocessor
        {
            public static event Action<string> AssetImported;

            static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
            {
                for (int i = 0; i < importedAssets.Length; i++)
                    AssetImported?.Invoke(importedAssets[i]);
            }
        }

        static SampleDependencyImporter()
        {
            PackageManagerExtensions.RegisterExtension(new SampleDependencyImporter());
        }

        const string k_CinemachinePackageName = "com.unity.cinemachine";
        PackageInfo m_PackageInfo;
        List<Sample> m_Samples;
        SampleConfiguration m_SampleConfiguration;

        VisualElement IPackageManagerExtension.CreateExtensionUI() => default;
        public void OnPackageAddedOrUpdated(PackageInfo packageInfo) {}
        public void OnPackageRemoved(PackageInfo packageInfo) {}

        /// <summary>
        /// Called when the package selection changes in the Package Manager window.
        /// The dependency importer will track the selected package and its sample configuration.
        /// </summary>
        void IPackageManagerExtension.OnPackageSelectionChange(PackageInfo packageInfo)
        {
            var isCmPackage = packageInfo != null && packageInfo.name.StartsWith(k_CinemachinePackageName);
            if (isCmPackage)
            {
                m_PackageInfo = packageInfo;
                m_Samples = GetSamples(packageInfo);
                if (TryLoadSampleConfiguration(m_PackageInfo, out m_SampleConfiguration))
                {
                    SamplePostprocessor.AssetImported += LoadAssetDependencies;
                }
            }
            else
            {
                m_PackageInfo = null;
                SamplePostprocessor.AssetImported -= LoadAssetDependencies;
            }
        }

        /// <summary>
        /// Load the sample configuration for the specified package, if one is available.
        /// </summary>
        static bool TryLoadSampleConfiguration(PackageInfo packageInfo, out SampleConfiguration configuration)
        {
            var configurationPath = $"{packageInfo.assetPath}/samples.json";

            if (File.Exists(configurationPath))
            {
                var configurationText = File.ReadAllText(configurationPath);
                configuration = JsonSerialization.Deserialize<SampleConfiguration>(configurationText);

                return true;
            }

            configuration = null;
            return false;
        }

        /// <summary>
        /// Handles loading common asset dependencies if required.
        /// </summary>
        void LoadAssetDependencies(string assetPath)
        {
            if (m_SampleConfiguration != null)
            {
                var assetsImported = false;

                for (int i = 0; i < m_Samples.Count; ++i)
                {
                    // Import dependencies if we are importing the root directory of the sample
                    var isSampleDirectory = assetPath.EndsWith(m_Samples[i].displayName);
                    if (isSampleDirectory)
                    {
                        var sampleEntry = m_SampleConfiguration.GetEntry(m_Samples[i]);
                        if (sampleEntry != null)
                        {
                            // Import the common asset dependencies
                            assetsImported = ImportDependencies(m_PackageInfo, m_SampleConfiguration.CommonAssetDependencies);

                            // Import the sample-specific dependencies
                            assetsImported |= ImportDependencies(m_PackageInfo, sampleEntry.AssetDependencies);
                        }
                    }
                }

                if (assetsImported)
                    AssetDatabase.Refresh();
            }
        }

        /// <summary>
        /// Imports specified dependencies from the package into the project.
        /// </summary>
        static bool ImportDependencies(PackageInfo packageInfo, string[] paths)
        {
            if (paths == null)
                return false;

            var assetsImported = false;
            for (int i = 0; i < paths.Length; ++i)
            {
                var dependencyPath = Path.GetFullPath($"Packages/{packageInfo.name}/Samples~/{paths[i]}");
                if (Directory.Exists(dependencyPath))
                {
                    CopyDirectory(dependencyPath, $"{Application.dataPath}/Samples/{packageInfo.displayName}/{packageInfo.version}/{paths[i]}");
                    assetsImported = true;
                }
            }

            return assetsImported;
        }

        /// <summary>
        /// Returns all samples part of the specified package.
        /// </summary>
        /// <param name="packageInfo"></param>
        /// <returns></returns>
        static List<Sample> GetSamples(PackageInfo packageInfo)
        {
            // Find all samples for the package
            var samples = Sample.FindByPackage(packageInfo.name, packageInfo.version);
            return new List<Sample>(samples);
        }

        /// <summary>
        /// Copies a directory from the source to target path. Overwrites existing directories.
        /// </summary>
        static void CopyDirectory(string sourcePath, string targetPath)
        {
            // Verify source directory
            var source = new DirectoryInfo(sourcePath);
            if (!source.Exists)
                throw new DirectoryNotFoundException($"{sourcePath}  directory not found");

            // Delete pre-existing directory at target path
            var target = new DirectoryInfo(targetPath);
            if (target.Exists)
                target.Delete(true);

            Directory.CreateDirectory(targetPath);

            // Copy all files to target path
            foreach (FileInfo file in source.GetFiles())
            {
                var newFilePath = Path.Combine(targetPath, file.Name);
                file.CopyTo(newFilePath);
            }

            // Recursively copy all subdirectories
            foreach (DirectoryInfo child in source.GetDirectories())
            {
                var newDirectoryPath = Path.Combine(targetPath, child.Name);
                CopyDirectory(child.FullName, newDirectoryPath);
            }
        }
    }
}
