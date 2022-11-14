using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEngine.UIElements;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Cinemachine.Editor
{
    [InitializeOnLoad]
    class SampleDependencyImporter : IPackageManagerExtension
    {
        const string k_CinemachinePackageName = "com.unity.cinemachine";
        PackageInfo m_PackageInfo;
        IEnumerable<Sample> m_Samples;
        SampleConfiguration m_SampleConfiguration;
        AddRequest m_PackageAddRequest;
        int m_PackageDependencyIndex;
        string[] m_PackageDependencies;

        static SampleDependencyImporter() => PackageManagerExtensions.RegisterExtension(new SampleDependencyImporter());
        VisualElement IPackageManagerExtension.CreateExtensionUI() => default;
        public void OnPackageAddedOrUpdated(PackageInfo packageInfo) {}
        public void OnPackageRemoved(PackageInfo packageInfo) {}

        /// <summary>
        /// Called when the package selection changes in the Package Manager window.
        /// It loads Cinemachine package info and configuration, when Cinemachine is selected.
        /// </summary>
        void IPackageManagerExtension.OnPackageSelectionChange(PackageInfo packageInfo)
        {
            if (packageInfo != null && packageInfo.name.StartsWith(k_CinemachinePackageName))
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

        /// <summary>Load the sample configuration for the specified package, if one is available.</summary>
        static bool TryLoadSampleConfiguration(PackageInfo packageInfo, out SampleConfiguration configuration)
        {
            var configurationPath = $"{packageInfo.assetPath}/Samples~/samples.json";

            if (File.Exists(configurationPath))
            {
                var configurationText = File.ReadAllText(configurationPath);
                configuration = JsonUtility.FromJson<SampleConfiguration>(configurationText);

                return true;
            }

            configuration = null;
            return false;
        }

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
                            assetsImported = ImportAssetDependencies(m_PackageInfo, m_SampleConfiguration.SharedAssetDependencies);

                            // Import sample-specific dependencies
                            assetsImported |= ImportAssetDependencies(m_PackageInfo, sampleEntry.AssetDependencies);
                            
                            // Import sample-specific package dependencies using the editor update loop, because
                            // adding package dependencies need to be done in sequence one after the other
                            m_PackageDependencyIndex = 0;
                            m_PackageDependencies = sampleEntry.PackageDependencies;
                            EditorApplication.update += ImportPackageDependencies;
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
                        CopyDirectory(dependencyPath, 
                            $"{Application.dataPath}/Samples/{packageInfo.displayName}/{packageInfo.version}/{path}");
                        assetsImported = true;
                    }
                }

                return assetsImported;
            }

            void ImportPackageDependencies()
            {
                if (m_PackageAddRequest != null && !m_PackageAddRequest.IsCompleted)
                    return; // wait while we have a request pending

                if (m_PackageDependencyIndex < m_PackageDependencies.Length)
                    m_PackageAddRequest = Client.Add(m_PackageDependencies[m_PackageDependencyIndex++]);
                else
                {
                    m_PackageDependencies = null;
                    m_PackageAddRequest = null;
                    EditorApplication.update -= ImportPackageDependencies;
                }
            }
        }

        /// <summary>Copies a directory from the source to target path. Overwrites existing directories.</summary>
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
        
        /// <summary>An AssetPostProcessor which will raise an event when a new asset is imported.</summary>
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
        
        /// <summary>A configuration class defining information related to samples for the package.</summary>
        [Serializable]
        class SampleConfiguration
        {
            /// <summary>This class defines the path and dependencies for a specific sample.</summary>
            [Serializable]
            public class SampleEntry
            {
                public string Path;
                public string[] AssetDependencies;
                public string[] PackageDependencies;
            }

            public string[] SharedAssetDependencies;

            public SampleEntry[] SampleEntries;

            public SampleEntry GetEntry(Sample sample) =>
                SampleEntries?.FirstOrDefault(t => sample.resolvedPath.EndsWith(t.Path));
        }
    }
}
