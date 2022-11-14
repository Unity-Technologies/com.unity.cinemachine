using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.PackageManager;
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

            static void OnPostprocessAllAssets(string[] importedAssets, 
                string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
            {
                foreach (var importedAsset in importedAssets)
                    AssetImported?.Invoke(importedAsset);
            }
        }

        static SampleDependencyImporter()
        {
            PackageManagerExtensions.RegisterExtension(new SampleDependencyImporter());
        }

        const string k_CinemachinePackageName = "com.unity.cinemachine";
        PackageInfo m_PackageInfo;
        IEnumerable<Sample> m_Samples;
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
                m_Samples = Sample.FindByPackage(packageInfo.name, packageInfo.version);
                if (TryLoadSampleConfiguration(m_PackageInfo, out m_SampleConfiguration)) 
                    SamplePostprocessor.AssetImported += LoadAssetDependencies;
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
            var configurationPath = $"{packageInfo.assetPath}/Samples~/samples.json";

            if (File.Exists(configurationPath))
            {
                var configurationText = File.ReadAllText(configurationPath);
                configuration = Newtonsoft.Json.JsonConvert.DeserializeObject<SampleConfiguration>(configurationText);

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

                foreach (var t in m_Samples)
                {
                    // Import dependencies if we are importing the root directory of the sample
                    var isSampleDirectory = assetPath.EndsWith(t.displayName);
                    if (isSampleDirectory)
                    {
                        var sampleEntry = m_SampleConfiguration.GetEntry(t);
                        if (sampleEntry != null)
                        {
                            // Import common asset dependencies
                            assetsImported = ImportAssetDependencies(m_PackageInfo, m_SampleConfiguration.CommonAssetDependencies);

                            // Import sample-specific dependencies
                            assetsImported |= ImportAssetDependencies(m_PackageInfo, sampleEntry.AssetDependencies);
                            
                            // Import sample-specific package dependencies
                            assetsImported |= ImportPackageDependencies(sampleEntry.PackageDependencies);
                        }
                    }
                } 

                if (assetsImported)
                    AssetDatabase.Refresh();
            }
            
            // local functions
            static bool ImportAssetDependencies(PackageInfo packageInfo, string[] paths)
            {
                if (paths == null)
                    return false;

                var assetsImported = false;
                foreach (var path in paths)
                {
                    var dependencyPath = Path.GetFullPath($"Packages/{packageInfo.name}/Samples~/{path}");
                    if (Directory.Exists(dependencyPath))
                    {
                        CopyDirectory(dependencyPath, $"{Application.dataPath}/Samples/{packageInfo.displayName}/{packageInfo.version}/{path}");
                        assetsImported = true;
                    }
                }

                return assetsImported;
            }

            static bool ImportPackageDependencies(string[] packages)
            {
                foreach (var package in packages) 
                    Client.Add(package);
            
                return packages.Length != 0;
            }
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
