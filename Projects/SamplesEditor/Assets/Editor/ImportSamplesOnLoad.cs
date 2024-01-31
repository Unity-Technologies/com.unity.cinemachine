using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class ImportSamplesOnLoad
{
    
    [MenuItem("Cinemachine Samples/Load Samples")]
    // Start is called before the first frame update
    static void ImportSamples()
    {
        var samplesName = GetAllSamplesNames();
        ImportSamples(samplesName);
    }
    
    [MenuItem("Cinemachine Samples/Save Samples")]
    static void SaveSamples()
    {
        var paths = GetAllSamplesNames();
        SaveSamples(paths);
    }

    static List<string> GetAllSamplesNames()
    {
        var samplesName = new List<string>();
        samplesName.Add("Shared Assets");
        samplesName.Add("3D Samples");
        samplesName.Add("2D Samples");
        samplesName.Add("Input System Samples");
        return samplesName;
    }

    static List<string> GetAllSamplesFoldersInProjects()
    {
        var samplesFolders = new List<string>();
        samplesFolders.Add( $"{Application.dataPath}/Samples/Cinemachine/SamplesEditor/Shared Assets");
        samplesFolders.Add( $"{Application.dataPath}/Samples/Cinemachine/SamplesEditor/3D Samples");
        samplesFolders.Add( $"{Application.dataPath}/Samples/Cinemachine/SamplesEditor/2D Samples");
        samplesFolders.Add( $"{Application.dataPath}/Samples/Cinemachine/SamplesEditor/Input System Samples");
        return samplesFolders;
    }

    static void SaveSamples(List<string> paths)
    {
        foreach (var path in paths)
        {
            var dependencyPath = $"{Application.dataPath}/Samples/Cinemachine/SamplesEditor/{path}";
            if (Directory.Exists(dependencyPath))
            {
                var copyTo = Path.GetFullPath($"Packages/com.unity.cinemachine/Samples~/{path}");
                CopyDirectory(dependencyPath, copyTo);
            }
        }
        AssetDatabase.Refresh();
    }
    
    
    
    static void ImportSamples(List<string> paths)
    {
        SpecificAssets InputSystem = (SpecificAssets)AssetDatabase.LoadAssetAtPath("Assets/SepcialAssetsInputSystem.asset", typeof(SpecificAssets));
        InputSystem.specialSampleAssets.Clear();
        foreach (var path in paths)
        {
            var dependencyPath = Path.GetFullPath($"Packages/com.unity.cinemachine/Samples~/{path}");
            if (Directory.Exists(dependencyPath))
            {
                var copyTo = 
                    $"{Application.dataPath}/Samples/Cinemachine/SamplesEditor/{path}";
                CopyDirectory(dependencyPath, copyTo);
            }
        }
        AssetDatabase.Refresh();
        FixAssets(GetAllSamplesFoldersInProjects());
    }

    static void FixAssets(List<string> folders)
        {
            foreach (var folder in folders)
            {
                var inputSystemFixPath = Path.GetFullPath($"Packages/com.unity.cinemachine/Samples~/InputSystem~/");
                ReplaceAssets(inputSystemFixPath, folder);
            }
            
#if CINEMACHINE_HDRP
            foreach (var folder in folders)
            {
                var hdrpFixPath = Path.GetFullPath($"Packages/{packageInfo.name}/Samples~/HDRP~/");
                ReplaceAssets(hdrpFixPath, folder);
            }
#endif
            
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
                        FileUtil.CopyFileOrDirectory(brokenPrefab, Application.dataPath + $"/Temp/{prefab.Name}");
                        SpecificAssets InputSystem = (SpecificAssets)AssetDatabase.LoadAssetAtPath("Assets/SepcialAssetsInputSystem.asset", typeof(SpecificAssets));
                        var fixedPrefabContents = PrefabUtility.LoadPrefabContents(fixPath + prefab.Name);
                        InputSystem.specialSampleAssets.Add(new SpecialPrefab()
                        {
                            oldPrefab = null, 
                            newPrefab = PrefabUtility.SaveAsPrefabAsset(fixedPrefabContents, brokenPrefab),
                        });
                        PrefabUtility.UnloadPrefabContents(fixedPrefabContents);
                        AssetDatabase.Refresh();
                    }
                }

                // fix other assets
                var assets = fixDirectory.GetFiles("*.asset*"); // assets and their meta files
                foreach (var asset in assets)
                    asset.CopyTo(prefabFolder + "/" + asset.Name);
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
}
