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
        PackageChecker m_PackageChecker = new ();

        static SampleDependencyImporter() => PackageManagerExtensions.RegisterExtension(new SampleDependencyImporter());
        VisualElement IPackageManagerExtension.CreateExtensionUI() => default;

        public void OnPackageAddedOrUpdated(PackageInfo packageInfo) => m_PackageChecker.RefreshPackageCache();
        public void OnPackageRemoved(PackageInfo packageInfo) => m_PackageChecker.RefreshPackageCache();

        /// <summary>
        /// Called when the package selection changes in the Package Manager window.
        /// It loads Cinemachine package info and configuration, when Cinemachine is selected.
        /// </summary>
        void IPackageManagerExtension.OnPackageSelectionChange(PackageInfo packageInfo)
        {
            var cmPackageInfo = packageInfo != null && packageInfo.name.StartsWith(k_CinemachinePackageName);
            if (m_PackageInfo == null && cmPackageInfo)
            {
                m_PackageInfo = packageInfo;
                m_Samples = Sample.FindByPackage(packageInfo.name, packageInfo.version);
                if (TryLoadSampleConfiguration(m_PackageInfo, out m_SampleConfiguration)) 
                    SamplePostprocessor.AssetImported += LoadAssetDependencies;
            }
            else if (!cmPackageInfo)
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
        
        const string k_SampleName = "Samples/Cinemachine/";
        AddRequest m_PackageAddRequest;
        int m_PackageDependencyIndex;
        List<string> m_PackageDependencies = new ();
        void LoadAssetDependencies(string assetPath)
        {
            if (m_SampleConfiguration != null)
            {
                var sharedAssetsImported = false;
                var localAssetsImported = false;
                foreach (var sample in m_Samples)
                {
                    if (assetPath.EndsWith(sample.displayName))
                    {
                        var sampleEntry = m_SampleConfiguration.GetEntry(sample);
                        if (sampleEntry != null)
                        {
                            // Import common asset dependencies
                            sharedAssetsImported = ImportAssetDependencies(
                                m_PackageInfo, m_SampleConfiguration.SharedAssetDependencies, 
                                out bool sharedAssetsReimported);
                            
                            // Import sample-specific asset dependencies
                            localAssetsImported = ImportAssetDependencies(
                                m_PackageInfo, sampleEntry.AssetDependencies, 
                                out bool localAssetsReimported);
                            
                            // Import common amd sample specific package dependencies
                            m_PackageDependencyIndex = 0;
                            m_PackageDependencies = new List<string>(m_SampleConfiguration.SharedPackageDependencies);
                            m_PackageDependencies.AddRange(sampleEntry.PackageDependencies);
                            
                            if (m_PackageDependencies.Count != 0 && 
                                DoDependenciesNeedToBeImported(out var dependenciesToImport))
                            {
                                switch (PromptUserConfirmation(dependenciesToImport))
                                {
                                    case UserChoice.ImportPackageDependencies:
                                        // only import dependencies that are missing
                                        m_PackageDependencies = dependenciesToImport;
                                        // Import package dependencies using the editor update loop, because
                                        // adding packages need to be done in sequence one after the other
                                        EditorApplication.update += ImportPackageDependencies;
                                        break;
                                    case UserChoice.AbortImport:
                                        // if samples were already imported, don't delete them
                                        if (!localAssetsReimported)
                                            DeleteSampleImported(sample.importPath, 
                                                !sharedAssetsReimported, m_SampleConfiguration.SharedAssetDependencies,
                                                localAssetsImported, sampleEntry.AssetDependencies);
                                        break;
                                    default:
                                    case UserChoice.ImportSampleButNotPackageDependencies:
                                        break;
                                }
                            }
                        }
                        break;
                    }
                } 

                if (sharedAssetsImported || localAssetsImported)
                    AssetDatabase.Refresh();
            }
            
            // local functions
            bool DoDependenciesNeedToBeImported(out List<string> packagesToImport)
            {
                packagesToImport = new List<string>();
                foreach (var packageName in m_PackageDependencies)
                {
                    if (!m_PackageChecker.ContainsPackage(packageName)) 
                        packagesToImport.Add(packageName);
                }

                return packagesToImport.Count != 0;
            }
            
            void ImportPackageDependencies()
            {
                if (m_PackageAddRequest != null && !m_PackageAddRequest.IsCompleted)
                    return; // wait while we have a request pending

                if (m_PackageDependencyIndex < m_PackageDependencies.Count)
                    m_PackageAddRequest = Client.Add(m_PackageDependencies[m_PackageDependencyIndex++]);
                else
                {
                    m_PackageDependencies.Clear();
                    m_PackageAddRequest = null;
                    EditorApplication.update -= ImportPackageDependencies;
                }
            }
            
            static bool ImportAssetDependencies(PackageInfo packageInfo, string[] paths, out bool reimport)
            {
                reimport = false;
                if (paths == null)
                    return false;

                var assetsImported = false;
                foreach (var path in paths)
                {
                    var dependencyPath = Path.GetFullPath($"Packages/{packageInfo.name}/Samples~/{path}");
                    if (Directory.Exists(dependencyPath))
                    {
                        var copyTo = 
                            $"{Application.dataPath}/Samples/{packageInfo.displayName}/{packageInfo.version}/{path}";
                        reimport = Directory.Exists(copyTo);
                        CopyDirectory(dependencyPath, copyTo);
                        assetsImported = true;
                    }
                }

                return assetsImported;
            }

            static void DeleteSampleImported(string importPath, 
                bool sharedAssetsImported, string[] sharedAssetDependencies, 
                bool localAssetsImported, string[] localAssetDependencies)
            {
                AssetDatabase.Refresh();
                var relativePath = importPath.Substring(importPath.IndexOf(k_SampleName));
                var version = relativePath.Substring(k_SampleName.Length);
                version = version.Substring(0, version.IndexOf("/") + 1);

                var instructions = new List<string>();
                if (sharedAssetsImported)
                    foreach (var folder in sharedAssetDependencies) 
                        instructions.Add("Assets/" + k_SampleName + version + folder);
                                                        
                if (localAssetsImported)
                    foreach (var folder in localAssetDependencies)
                        instructions.Add("Assets/" + k_SampleName + version + folder);
                                                        
                instructions.Add("Assets/" + relativePath);

                AssetDatabase.DeleteAssets(instructions.ToArray(), new List<string>());
                AssetDatabase.Refresh();
            }
        }

        enum UserChoice
        {
            ImportPackageDependencies = 0,
            ImportSampleButNotPackageDependencies = 1,
            AbortImport = 2
        }
        static UserChoice PromptUserConfirmation(List<string> dependencies)
        {
            return (UserChoice) EditorUtility.DisplayDialogComplex(
                "Import Sample Package Dependencies",
                "These samples contain package dependencies that your project does not have: \n" +
                dependencies.Aggregate("", (current, dependency) => current + (dependency + "\n")),
                "Import samples and its dependencies", 
                "Import samples but not its dependencies",
                "Abort importing samples");
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
            public string[] SharedPackageDependencies;
            public SampleEntry[] SampleEntries;

            public SampleEntry GetEntry(Sample sample) =>
                SampleEntries?.FirstOrDefault(t => sample.resolvedPath.EndsWith(t.Path));
        }
        
        class PackageChecker
        {
            ListRequest m_Request;
            PackageCollection m_Packages;

            public PackageChecker()
            {
                RefreshPackageCache();
            }

            public void RefreshPackageCache()
            {
                if (m_Request != null && !m_Request.IsCompleted)
                    return; // need to wait for previous request to finish
                
                m_Request = Client.List(offlineMode: true);
                EditorApplication.update += WaitForRequestToComplete;
            }
 
            void WaitForRequestToComplete()
            {
                if (m_Request.IsCompleted)
                {
                    if (m_Request.Status == StatusCode.Success) 
                        m_Packages = m_Request.Result;
                    EditorApplication.update -= WaitForRequestToComplete;
                }
            }
 
            public bool ContainsPackage(string packageName)
            {
                // Check each package and package dependency for packageName
                foreach (var package in m_Packages)
                {
                    if (string.Compare(package.name, packageName) == 0)
                        return true;

                    if (package.dependencies.Any(dependencyInfo => string.Compare(dependencyInfo.name, packageName) == 0))
                        return true;
                }
 
                return false;
            }
        }
    }
}
