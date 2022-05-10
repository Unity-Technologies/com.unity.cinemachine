#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
// suppress obsolete warnings
#pragma warning disable CS0618 

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
                allPrefabs.Where(p =>
                        p.GetComponent<CinemachineVirtualCamera>() != null ||
                        p.GetComponent<CinemachineFreeLook>() != null)
                    .ToList();
            //var freelookPrefabs = allPrefabs.Where(p => p.GetComponent<CinemachineFreeLook>() != null).ToList();

            // Sort by no variant prefabs first
            vcamPrefabs.Sort((a, b) =>
            {
                var aIsVariant = PrefabUtility.IsPartOfVariantPrefab(a);
                var bIsVariant = PrefabUtility.IsPartOfVariantPrefab(b);

                if (aIsVariant != bIsVariant)
                    return -1;

                // if both no variants or both variants, we just use the name to compare just to be consistent.
                return a.name.CompareTo(b.name);
            });

            vcamPrefabs.ForEach(o => Debug.Log(o.name));

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
                    var componentsList = new List<CinemachineVirtualCameraBase>();

                    var rootObjects = scene.GetRootGameObjects();
                    foreach (var go in rootObjects)
                    {
                        var freelooks = go.GetComponentsInChildren<CinemachineFreeLook>(true);
                        componentsList.AddRange(freelooks.ToList());
                    }
                    foreach (var go in rootObjects)
                    {
                        var vcams = go.GetComponentsInChildren<CinemachineVirtualCamera>(true);
                        componentsList.AddRange(vcams.ToList());
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
            old.TryGetComponent<CinemachineFreeLook>(out var freeLook);
            if (freeLook != null)
            {
                freeLook.enabled = false;
                return UpgradeFreelook(freeLook);
            }
            old.TryGetComponent<CinemachineVirtualCamera>(out var vcam);
            if (vcam != null)
            {
                vcam.enabled = false;
                return UpgradeCmVirtualCamera(vcam);
            }

            return true;
        }

        static bool UpgradeCmVirtualCamera(CinemachineVirtualCamera vcam)
        {
            var go = vcam.gameObject;
            vcam.enabled = false;
            var oldExtensions = go.GetComponents<CinemachineExtension>();
            
            var cmCamera = go.AddComponent<CmCamera>();
            CopyValues<CinemachineVirtualCameraBase>(vcam, cmCamera);
            cmCamera.Follow = vcam.Follow;
            cmCamera.LookAt = vcam.LookAt;
            cmCamera.m_Lens = vcam.m_Lens;
            cmCamera.m_Transitions = vcam.m_Transitions;

            var oldPipeline = vcam.GetComponentPipeline();
            foreach (var oldComponent in oldPipeline)
            {
                if (oldComponent != null)
                {
                    var newComponent = (CinemachineComponentBase)go.AddComponent(oldComponent.GetType());
                    CopyValues(oldComponent, newComponent);
                }
            }

            foreach (var extension in oldExtensions)
            {
                cmCamera.AddExtension(extension);
            }

            var pipelineHolder = vcam.gameObject.GetComponentInChildren<CinemachinePipeline>().gameObject;
            DestroyImmediate(pipelineHolder);
            
            DestroyImmediate(vcam);
            return true;
        }

        static bool UpgradeFreelook(CinemachineFreeLook freelook)
        {
            // TODO: need to rule out freelooks that are not convertible
            // - Different noise profiles on top, mid, bottom
            // - Different Aim components or same Aim components but different parameters
            
            var go = freelook.gameObject;
            freelook.enabled = false;
            var oldExtensions = go.GetComponents<CinemachineExtension>();
            
            var cmCamera = go.AddComponent<CmCamera>();
            CopyValues<CinemachineVirtualCameraBase>(freelook, cmCamera);
            cmCamera.Follow = freelook.Follow;
            cmCamera.LookAt = freelook.LookAt;
            cmCamera.m_Lens = freelook.m_Lens;
            cmCamera.m_Transitions = freelook.m_Transitions;
            
            var orbitalFollow = go.AddComponent<CinemachineOrbitalFollow>();
            orbitalFollow.Orbits = ConvertFreelookSettings(freelook);
            orbitalFollow.BindingMode = freelook.m_BindingMode;

            var freeLookModifier = go.AddComponent<CinemachineFreeLookModifier>();
            ConvertFreelookBody(freelook, orbitalFollow, freeLookModifier);
            ConvertFreelookAim(freelook, go);
            ConvertFreelookNoise(freelook, go, freeLookModifier);
            
            foreach (var extension in oldExtensions)
            {
                cmCamera.AddExtension(extension);
            }
            
            DestroyImmediate(freelook);
            return true;
        }
        
        static void CopyValues<T>(T from, T to) {
            var json = JsonUtility.ToJson(from);
            JsonUtility.FromJsonOverwrite(json, to);
        }

        static Cinemachine3OrbitRig.Settings ConvertFreelookSettings(CinemachineFreeLook freelook)
        {
            var orbits = freelook.m_Orbits;
            return new Cinemachine3OrbitRig.Settings
            {
                Top = new Cinemachine3OrbitRig.Orbit
                {
                    Height = orbits[0].m_Height,
                    Radius = orbits[0].m_Radius,
                },
                Center = new Cinemachine3OrbitRig.Orbit
                {
                    Height = orbits[1].m_Height,
                    Radius = orbits[1].m_Radius,
                },
                Bottom = new Cinemachine3OrbitRig.Orbit
                {
                    Height = orbits[2].m_Height,
                    Radius = orbits[2].m_Radius,
                },
                SplineCurvature = freelook.m_SplineCurvature,
            };
        }

        static void ConvertFreelookBody(CinemachineFreeLook freelook, 
            CinemachineOrbitalFollow orbitalFollow, CinemachineFreeLookModifier freeLookModifier)
        {
            var top = freelook.GetRig(0).GetCinemachineComponent<CinemachineOrbitalTransposer>();
            var middle = freelook.GetRig(1).GetCinemachineComponent<CinemachineOrbitalTransposer>();
            var bottom = freelook.GetRig(2).GetCinemachineComponent<CinemachineOrbitalTransposer>();

            orbitalFollow.PositionDamping = new Vector3(middle.m_XDamping, middle.m_YDamping, middle.m_ZDamping);
            freeLookModifier.Modifiers.Add(new CinemachineFreeLookModifier.PositionDampingModifier 
            {
                Damping = new CinemachineFreeLookModifier.TopBottomRigs<Vector3>
                {
                    Top = new Vector3 { x = 0, y = top.m_YDamping, z = top.m_ZDamping },
                    Bottom = new Vector3 { x = 0, y = bottom.m_YDamping, z = bottom.m_ZDamping },
                },
            });
        }

        static void ConvertFreelookAim(CinemachineFreeLook freelook,
            GameObject go)
        {
            var middle = freelook.GetRig(1).GetCinemachineComponent(CinemachineCore.Stage.Aim);
            if (middle == null)
            {
                return;
            }
            var newComponent = (CinemachineComponentBase)go.AddComponent(middle.GetType());
            CopyValues(middle, newComponent);
        }

        static void ConvertFreelookNoise(CinemachineFreeLook freelook, 
            GameObject go, CinemachineFreeLookModifier freeLookModifier)
        {
            var middle = freelook.GetRig(1).GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
            if (middle == null)
            {
                return;
            }
            
            var middleNoise = go.AddComponent<CinemachineBasicMultiChannelPerlin>();
            CopyValues(middle, middleNoise);
            
            var top = freelook.GetRig(0).GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
            var bottom = freelook.GetRig(2).GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
            
            freeLookModifier.Modifiers.Add(new CinemachineFreeLookModifier.NoiseModifier
            {
                Noise = new CinemachineFreeLookModifier.TopBottomRigs<CinemachineFreeLookModifier.NoiseModifier.NoiseSettings>
                {
                    Top = new CinemachineFreeLookModifier.NoiseModifier.NoiseSettings
                    {
                        Amplitude = top == null ? 0 : top.m_AmplitudeGain,
                        Frequency = top == null ? 0 : top.m_FrequencyGain,
                    },
                    Bottom = new CinemachineFreeLookModifier.NoiseModifier.NoiseSettings
                    {
                        Amplitude = bottom == null ? 0 : bottom.m_AmplitudeGain,
                        Frequency = bottom == null ? 0 : bottom.m_FrequencyGain,
                    }
                }
            });
        }
    }
}
#endif