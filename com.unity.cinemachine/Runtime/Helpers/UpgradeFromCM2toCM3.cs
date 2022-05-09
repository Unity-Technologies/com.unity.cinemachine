#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Cinemachine
{
// [InitializeOnLoad] use this for checking project load (also triggers on script reload)
    [ExecuteInEditMode]
    public class UpgradeFromCM2toCM3 : MonoBehaviour
    {
        public bool triggerUpgrade = false;
        public bool showHiddenGameObjects = true;

        void Update()
        {
            CinemachineCore.sShowHiddenObjects = showHiddenGameObjects;
            if (triggerUpgrade)
            {
                triggerUpgrade = false;
                Upgrade();
            }
        }

        static void Upgrade()
        {
            Debug.Log("UpgradeFromCM2toCM3 started!");

            var prefabGuids = AssetDatabase.FindAssets($"t:prefab");
            var allPrefabs = prefabGuids.Select(g => AssetDatabase.LoadAssetAtPath<GameObject>(
                AssetDatabase.GUIDToAssetPath(g))).ToList();

            // Ignore prefabs that are not CinemachineVirtualCameras or CinemachineFreeLooks.
            var vcamPrefabs =
                allPrefabs.Where(p => p.GetComponent<CinemachineVirtualCamera>() != null).ToList();

            //TODO: var freelookPrefabs = allPrefabs.Where(p => p.GetComponent<CinemachineFreeLook>() != null).ToList();

            // Sort by no variant prefabs first
            vcamPrefabs.Sort(delegate(GameObject a, GameObject b)
            {
                var aIsVariant = PrefabUtility.IsPartOfVariantPrefab(a);
                var bIsVariant = PrefabUtility.IsPartOfVariantPrefab(b);

                if (aIsVariant != bIsVariant)
                    return -1;

                // if both no variants or both variants, we just use the name to compare just to be consistent.
                return a.name.CompareTo(b.name);
            });

            vcamPrefabs.ForEach(delegate(GameObject o)
            {
                Debug.Log(o.name);
            });

            try
            {
                var total = vcamPrefabs.Count;
                var vcamType = typeof(CinemachineVirtualCamera);

                EditorUtility.DisplayProgressBar($"Refactoring {total} prefabs with {vcamType.Name}", "Start", 0);

                for (var i = 0; i < vcamPrefabs.Count; i++)
                {
                    var prefab = vcamPrefabs[i];
                    EditorUtility.DisplayProgressBar($"Refactoring {vcamPrefabs.Count} assets of type {vcamType.Name}",
                        prefab.name,
                        i / (float)total);

                    var contents = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(prefab));

                    var result = UpgradeCmVirtualCameraToCmCamera(contents);

                    // Just to break the loop if something is wrong...
                    if (!result)
                    {
                        PrefabUtility.UnloadPrefabContents(contents);
                        break;
                    }

                    PrefabUtility.SaveAsPrefabAsset(contents, AssetDatabase.GetAssetPath(prefab));
                    PrefabUtility.UnloadPrefabContents(contents);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            // Get all scenes under the Assets folder (to avoid touching scenes in packages)
            var allScenesGuids = new List<string>();
            allScenesGuids.AddRange(AssetDatabase.FindAssets("t:scene", new[]
            {
                "Assets"
            }));

            EditorUtility.DisplayProgressBar($"Refactoring {allScenesGuids.Count} scenes", "Starting...", 0);

            var allScenesCount = allScenesGuids.Count;
            for (var i = 0; i < allScenesCount; ++i)
            {
                var sceneGuid = allScenesGuids[i];
                var scenePath = AssetDatabase.GUIDToAssetPath(sceneGuid);

                try
                {
                    EditorUtility.DisplayProgressBar($"Refactoring {allScenesGuids.Count} scenes", scenePath, i / (float)allScenesCount);

                    var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                    var componentsList = new List<CinemachineVirtualCamera>();

                    // TODO: don't forget freelooks, and do them first

                    var rootObjects = scene.GetRootGameObjects();
                    foreach (var go in rootObjects)
                    {
                        var components = go.GetComponentsInChildren<CinemachineVirtualCamera>(true);
                        componentsList.AddRange(components.ToList());
                    }

                    var modified = false;
                    foreach (var component in componentsList)
                    {
                        var go = component.gameObject;
                        if (UpgradeCmVirtualCameraToCmCamera(go))
                        {
                            modified = true;
                            EditorUtility.SetDirty(go);
                        }
                    }

                    if (modified)
                    {
                        EditorSceneManager.MarkSceneDirty(scene);
                        EditorSceneManager.SaveScene(scene);
                    }

                }
                finally
                {
                    EditorUtility.ClearProgressBar();
                }
            }

            Debug.Log("UpgradeFromCM2toCM3 end!");
        }

        /// <summary>
        /// Upgrades gameobject using cm2 to use cm3.
        /// </summary>
        /// <param name="old">Gameobject that needs to be upgraded</param>
        /// <returns>True, if upgrade was successful. False, otherwise.</returns>
        static bool UpgradeCmVirtualCameraToCmCamera(GameObject old)
        {
            Debug.Log("Upgrading " + old.name + " to CM3!");
            old.TryGetComponent<CinemachineVirtualCamera>(out var vcam);
            if (vcam != null)
            {
                vcam.enabled = false;
            }

            var cmCamera = old.AddComponent<CmCamera>();

            // TODO: need to convert data, not just delete
            if (cmCamera != null)
            {
                cmCamera.enabled = true;
            }
            DestroyImmediate(vcam);

            return true;
        }
    }
}
#endif