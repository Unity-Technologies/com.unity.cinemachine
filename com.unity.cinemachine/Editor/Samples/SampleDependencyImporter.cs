using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager.UI;
#if CINEMACHINE_URP || CINEMACHINE_HDRP
using UnityEditor.Rendering;
#endif
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Unity.Cinemachine.Editor
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
        UnityEngine.UIElements.VisualElement IPackageManagerExtension.CreateExtensionUI() => default;

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
            var configurationPath = $"{packageInfo.assetPath}/Samples~/sampleDependencies.json";
            if (File.Exists(configurationPath))
            {
                var configurationText = File.ReadAllText(configurationPath);
                configuration = JsonUtility.FromJson<SampleConfiguration>(configurationText);
                return true;
            }
            configuration = null;
            return false;
        }

        AddRequest m_PackageAddRequest;
        int m_PackageDependencyIndex;
        List<string> m_PackageDependencies = new ();
        void LoadAssetDependencies(string assetPath)
        {
            if (m_SampleConfiguration != null)
            {
                var assetsImported = false;
                foreach (var sample in m_Samples)
                {
                    if (assetPath.EndsWith(sample.displayName))
                    {
                        var sampleEntry = m_SampleConfiguration.GetEntry(sample);
                        if (sampleEntry != null)
                        {
                            // Import common asset dependencies
                            assetsImported = ImportAssetDependencies(
                                m_PackageInfo, m_SampleConfiguration.SharedAssetDependencies, out var sharedDestinations);
                            
                            // Import sample-specific asset dependencies
                            assetsImported |= ImportAssetDependencies(
                                m_PackageInfo, sampleEntry.AssetDependencies, out var localDestinations);


                            if (assetsImported)
                            {
                                AssetDatabase.Refresh();
                                var uniqueFolders = new HashSet<string>(sharedDestinations.Concat(localDestinations));
                                ConvertMaterials(uniqueFolders);
                                FixAssets(uniqueFolders, m_PackageInfo);
                            }
                            
                            // Import common amd sample specific package dependencies
                            m_PackageDependencyIndex = 0;
                            m_PackageDependencies = new List<string>(m_SampleConfiguration.SharedPackageDependencies);
                            m_PackageDependencies.AddRange(sampleEntry.PackageDependencies);
                            
                            if (m_PackageDependencies.Count != 0 && 
                                DoDependenciesNeedToBeImported(out var dependenciesToImport))
                            {
                                if (PromptUserImportDependencyConfirmation(dependenciesToImport))
                                {
                                    // only import dependencies that are missing
                                    m_PackageDependencies = dependenciesToImport;
                                    // Import package dependencies using the editor update loop, because
                                    // adding packages need to be done in sequence one after the other
                                    EditorApplication.update += ImportPackageDependencies;
                                }
                            }
                        }
                        break;
                    }
                } 

                if (assetsImported)
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
            
            static bool ImportAssetDependencies(PackageInfo packageInfo, string[] paths, out List<string> destinations)
            {
                destinations = new List<string>();
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
                        CopyDirectory(dependencyPath, copyTo);
                        destinations.Add(copyTo);
                        assetsImported = true;
                    }
                }

                return assetsImported;
            }

            static bool PromptUserImportDependencyConfirmation(List<string> dependencies)
            {
                return EditorUtility.DisplayDialog(
                    "Import Sample Package Dependencies",
                    "These samples contain package dependencies that your project does not have: \n" +
                    dependencies.Aggregate("", (current, dependency) => current + (dependency + "\n")),
                    "Import samples and their dependencies", 
                    "Import samples without their dependencies");
            }
        }

        static void ConvertMaterials(IEnumerable<string> folders)
        {
#if CINEMACHINE_URP || CINEMACHINE_HDRP
            const string materialFolder = "Materials";
            foreach (var folder in folders)
            {
                var materialDir = new DirectoryInfo(folder + "/" + materialFolder);
                if (!materialDir.Exists)
                    continue;
                var materialInfos = materialDir.GetFiles("*.mat");
                foreach (var matInfo in materialInfos)
                {
                    var localPath = matInfo.FullName[Application.dataPath.Length..];
                    var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/" + localPath);

                    MaterialUpgrader.Upgrade(material, 
#if CINEMACHINE_URP
                    new UnityEditor.Rendering.Universal.StandardUpgrader(material.shader.name), 
#elif CINEMACHINE_HDRP
                    UnityEditor.Rendering.HighDefinition.MaterialUpgradeHelper.GetHDRPMaterialUpgraders(),
#endif
                    MaterialUpgrader.UpgradeFlags.None);
                }
            }
#endif
        }
        
        static void FixAssets(IEnumerable<string> folders, PackageInfo packageInfo)
        {
#if CINEMACHINE_UNITY_INPUTSYSTEM
            foreach (var folder in folders)
            {
                var inputSystemFixPath = Path.GetFullPath($"Packages/{packageInfo.name}/Samples~/InputSystem~/");
                ReplaceAssets(inputSystemFixPath, folder);
            }
#endif
#if CINEMACHINE_HDRP
            foreach (var folder in folders)
            {
                var hdrpFixPath = Path.GetFullPath($"Packages/{packageInfo.name}/Samples~/HDRP~/");
                ReplaceAssets(hdrpFixPath, folder);
            }
#endif
            
#if CINEMACHINE_HDRP || CINEMACHINE_UNITY_INPUTSYSTEM
            // local function
            static void ReplaceAssets(string fixPath, string prefabFolder)
            {
                var fixDirectory = new DirectoryInfo(fixPath);
                var prefabPath = prefabFolder + "/Prefabs/";
                var prefabDir = new DirectoryInfo(prefabPath);
                if (!fixDirectory.Exists || !prefabDir.Exists)
                    return;
                
                // fix prefab assets
                var fixPrefabs = fixDirectory.GetFiles("*.prefab");
                foreach (var prefab in fixPrefabs)
                {
                    var brokenPrefab = prefabPath + prefab.Name;
                    if (File.Exists(brokenPrefab))
                    {
                        var fixedPrefabContents = PrefabUtility.LoadPrefabContents(fixPath + prefab.Name);
                        PrefabUtility.SaveAsPrefabAsset(fixedPrefabContents, brokenPrefab);
                        PrefabUtility.UnloadPrefabContents(fixedPrefabContents);
                    }
                }

                // fix other assets
                var assets = fixDirectory.GetFiles("*.asset*"); // assets and their meta files
                foreach (var asset in assets)
                    asset.CopyTo(prefabFolder + "/" + asset.Name);
            }
#endif
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
                
                m_Request = Client.List(true);
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
