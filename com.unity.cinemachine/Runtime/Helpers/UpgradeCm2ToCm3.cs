#if UNITY_EDITOR
#define DEBUG_HELPERS
// suppress obsolete warnings
#pragma warning disable CS0618 

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;

namespace Cinemachine.Upgrader
{
    [ExecuteInEditMode]
    public class UpgradeCm2ToCm3 : MonoBehaviour
    {
        public bool triggerUpgrade = false;
        public bool showHiddenGameObjects = true;
        CinemachineUpgrader m_Upgrader;

        void Update()
        {
            CinemachineCore.sShowHiddenObjects = showHiddenGameObjects;
            if (triggerUpgrade)
            {
                triggerUpgrade = false;
                m_Upgrader = new CinemachineUpgrader();
                m_Upgrader.UpgradeAll();
            }
        }

        class Cm2ToCm3Upgrader
        {
            public bool Upgrade(GameObject old)
            {
#if DEBUG_HELPERS
                Debug.Log("Upgrading " + old.name + " to CM3!");
#endif
                old.TryGetComponent<CinemachineFreeLook>(out var freeLook);
                if (freeLook != null)
                {
                    return UpgradeFreelook(freeLook);
                }
                old.TryGetComponent<CinemachineVirtualCamera>(out var vcam);
                if (vcam != null)
                {
                    return UpgradeCmVirtualCamera(vcam);
                }

                return true;
            }
            
            bool UpgradeFreelook(CinemachineFreeLook freelook)
            {
                if (!IsFreelookUpgradable(freelook))
                {
                    Debug.LogWarning("Freelook camera (" + freelook.name + ") is not upgradable automatically. Please upgrade manually!");
                    return false;
                }

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

                var topRig = freelook.GetRig(0).gameObject;
                var middleRig = freelook.GetRig(1).gameObject;
                var bottomRig = freelook.GetRig(2).gameObject;
                DestroyImmediate(topRig);
                DestroyImmediate(middleRig);
                DestroyImmediate(bottomRig);
                DestroyImmediate(freelook);
                return true;
                
                // local functions
                static bool IsFreelookUpgradable(CinemachineFreeLook freelook)
                {
                    // Freelook is not upgradable if it has:
                    // - look at override (parent lookat != child lookat)
                    // - different noise profiles on top, mid, bottom ||
                    // - different aim component types ||
                    // - same aim component types and with different parameters
                    var parentLookAt = freelook.LookAt;
                    var top = freelook.GetRig(0);
                    var middle = freelook.GetRig(1);
                    var bottom = freelook.GetRig(2);
                    var topNoise = top.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
                    var middleNoise = middle.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
                    var bottomNoise = bottom.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
                    var midAim = middle.GetCinemachineComponent(CinemachineCore.Stage.Aim);
            
                    return 
                        parentLookAt == top.LookAt && parentLookAt == middle.LookAt && parentLookAt == bottom.LookAt &&
                        IsEqualNoise(topNoise, middleNoise) && 
                        IsEqualNoise(middleNoise, bottomNoise) &&
                        IsEqualNoise(topNoise, bottomNoise) &&
                        PublicFieldsEqual(top.GetCinemachineComponent(CinemachineCore.Stage.Aim), midAim) &&
                        PublicFieldsEqual(bottom.GetCinemachineComponent(CinemachineCore.Stage.Aim), midAim);

                    static bool IsEqualNoise(CinemachineBasicMultiChannelPerlin a, CinemachineBasicMultiChannelPerlin b)
                    {
                        return a == null || b == null || 
                            ((a.m_NoiseProfile == null || b.m_NoiseProfile == null || a.m_NoiseProfile == b.m_NoiseProfile) 
                                && a.m_PivotOffset == b.m_PivotOffset);
                    }
            
                    static bool PublicFieldsEqual(CinemachineComponentBase a, CinemachineComponentBase b)
                    {
                        if (a == null && b == null)
                            return true;

                        if (a == null || b == null)
                            return false;
                
                        var aType = a.GetType();

                        if (aType != b.GetType())
                            return false;

                        var publicFields = aType.GetFields();
                        foreach (var pi in publicFields)
                        {
                            var field = aType.GetField(pi.Name);
                            if (!field.GetValue(a).Equals(field.GetValue(b)))
                                return false;
                        }
                        return true;
                    }
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
                        return; // no need to return because all aims are null on this freelook - see IsFreelookUpgradable why
                    }
                    var newComponent = (CinemachineComponentBase)go.AddComponent(middle.GetType());
                    CopyValues(middle, newComponent);
                }

                static void ConvertFreelookNoise(CinemachineFreeLook freelook, 
                    GameObject go, CinemachineFreeLookModifier freeLookModifier)
                {
                    var top = freelook.GetRig(0).GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
                    var middle = freelook.GetRig(1).GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
                    var bottom = freelook.GetRig(2).GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
                    if (middle == null)
                    {
                        var middleNoise = go.AddComponent<CinemachineBasicMultiChannelPerlin>();
                        middleNoise.m_NoiseProfile = 
                            (top != null && top.m_NoiseProfile != null) ? top.m_NoiseProfile : 
                            (bottom != null && bottom.m_NoiseProfile != null) ? bottom.m_NoiseProfile : null;
                        middleNoise.m_AmplitudeGain = 0;
                        middleNoise.m_FrequencyGain = 0;
                    }
                    else
                    {
                        var middleNoise = go.AddComponent<CinemachineBasicMultiChannelPerlin>();
                        CopyValues(middle, middleNoise);
                    }

            
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
            
            bool UpgradeCmVirtualCamera(CinemachineVirtualCamera vcam)
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
                    UpgradeComponent(oldComponent, go);
                }

                foreach (var extension in oldExtensions)
                {
                    cmCamera.AddExtension(extension);
                }

                var pipelineHolder = vcam.gameObject.GetComponentInChildren<CinemachinePipeline>().gameObject;
                DestroyImmediate(pipelineHolder);
                DestroyImmediate(vcam);
                return true;
                
                // local functions
                static void UpgradeComponent(CinemachineComponentBase oldComponent, GameObject go)
                {
                    if (oldComponent != null)
                    {
                        if (oldComponent is CinemachineTrackedDolly trackedDolly)
                        {
                            if (trackedDolly.Convertable())
                            {
                                var splineDolly = (CinemachineSplineDolly)go.AddComponent<CinemachineSplineDolly>();
                                trackedDolly.CopyTo(splineDolly);
                                DestroyImmediate(trackedDolly);
                                return;
                            }
                            Debug.LogWarning("CinemachineTrackedDolly (" + go.name + ") is not upgradable automatically. Please upgrade manually!");
                        }
                        
                        var newComponent = (CinemachineComponentBase)go.AddComponent(oldComponent.GetType());
                        CopyValues(oldComponent, newComponent);
                    }
                }
            }
            
            static void CopyValues<T>(T from, T to) {
                var json = JsonUtility.ToJson(from);
                JsonUtility.FromJsonOverwrite(json, to);
            }
        }

        class CinemachineUpgrader
        {
            SceneManager m_SceneManager;
            PrefabManager m_PrefabManager;
            Cm2ToCm3Upgrader m_Cm2ToCm3;

            public CinemachineUpgrader()
            {
                m_SceneManager = new SceneManager();
                m_PrefabManager = new PrefabManager();
                m_Cm2ToCm3 = new Cm2ToCm3Upgrader();
            }
            
            bool Upgrade(GameObject go)
            {
                return m_Cm2ToCm3.Upgrade(go);
            }
            
            public void UpgradeAll()
            {
                UpgradePrefabs();
                UpgradeInScenes();
            }

            void UpdateTimelineReference(Scene scene, GameObject upgraded)
            {
                var playableDirectors = new List<PlayableDirector>();

                var rootObjects = scene.GetRootGameObjects();
                foreach (var go in rootObjects)
                {
                    playableDirectors.AddRange(go.GetComponentsInChildren<PlayableDirector>(true).ToList());
                }

                foreach (var playableDirector in playableDirectors)
                {
                    if (playableDirector == null) continue;

                    var playableAsset = playableDirector.playableAsset;
                    if (playableAsset is TimelineAsset timelineAsset)
                    {
                        var tracks = timelineAsset.GetOutputTracks();
                        foreach (var track in tracks)
                        {
                            if (track is CinemachineTrack cmTrack)
                            {
                                var clips = cmTrack.GetClips();
                                foreach (var clip in clips)
                                {
                                    if (clip.asset is CinemachineShot cmShot)
                                    {
                                        var exposedRef = cmShot.VirtualCamera;
                                        var vcam = exposedRef.Resolve(playableDirector);
                                        if (vcam == null)
                                        {
                                            var vcamToAdd = upgraded.GetComponent<CinemachineVirtualCameraBase>();
                                            playableDirector.SetReferenceValue(exposedRef.exposedName, vcamToAdd);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            void UpgradePrefabs()
            {
                var prefabCount = m_PrefabManager.prefabCount;

                for (var i = 0; i < prefabCount; i++)
                {
                    var contents = m_PrefabManager.LoadPrefabContents(i);
                    Upgrade(contents); // TODO: maybe I should pass the upgrade function into prefab maanger -> cm3 -> cm4, we just need to change this function
                    m_PrefabManager.SavePrefabContents(i, contents);
                }
            }

            void UpgradeInScenes()
            {
                int sceneCount = m_SceneManager.sceneCount;
                for (int i = 0; i < sceneCount; ++i)
                {
                    var scene = m_SceneManager.LoadScene(i);
                    var timelineManager = new TimelineManager(scene);
                    
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
                        if (component == null) continue;
                        
                        var go = component.gameObject;
                        if (Upgrade(go))
                        {
                            modified = true;
                            timelineManager.UpdateTimelineReference(go);
                            EditorUtility.SetDirty(go);
                        }
                    }

                    if (modified)
                    {
                        m_SceneManager.UpdateScene(scene);
                    }
                }
            }

            class SceneManager
            {
                public Scene LoadScene(int index)
                {
                    var sceneGuid = m_AllSceneGuids[index];
                    var scenePath = AssetDatabase.GUIDToAssetPath(sceneGuid);
                    return EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                }

                public void UpdateScene(Scene scene)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
                
                public int sceneCount;
                List<string> m_AllSceneGuids;
                public SceneManager()
                {
                    m_AllSceneGuids = new List<string>();
                    m_AllSceneGuids.AddRange(AssetDatabase.FindAssets("t:scene", new[]
                    {
                        "Assets"
                    }));
                    sceneCount = m_AllSceneGuids.Count;
                    
#if DEBUG_HELPERS
                    Debug.Log("**** All scenes ****");
                    m_AllSceneGuids.ForEach(guid => Debug.Log(AssetDatabase.GUIDToAssetPath(guid)));
#endif
                }
            }

            class PrefabManager
            {
                public GameObject LoadPrefabContents(int index)
                {
                    return PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(m_prefabVcams[index]));
                }

                public void SavePrefabContents(int index, GameObject contents)
                {
                    PrefabUtility.SaveAsPrefabAsset(contents, AssetDatabase.GetAssetPath(m_prefabVcams[index]));
                    PrefabUtility.UnloadPrefabContents(contents);
                }
                
                public int prefabCount;
                List<GameObject> m_prefabVcams;
                public PrefabManager()
                {
                    var prefabGuids = AssetDatabase.FindAssets($"t:prefab");
                    var allPrefabs = prefabGuids.Select(
                        g => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g))).ToList();

                    // Ignore prefabs that are not CinemachineVirtualCameras or CinemachineFreeLooks.
                    m_prefabVcams = allPrefabs.Where(p =>
                        p.GetComponent<CinemachineVirtualCamera>() != null ||
                        p.GetComponent<CinemachineFreeLook>() != null).ToList();
                    
                    // Sort by no variant prefabs first
                    m_prefabVcams.Sort((a, b) =>
                    {
                        var aIsVariant = PrefabUtility.IsPartOfVariantPrefab(a);
                        var bIsVariant = PrefabUtility.IsPartOfVariantPrefab(b);

                        if (aIsVariant != bIsVariant)
                            return -1;

                        // if both no variants or both variants, we just use the name to compare just to be consistent.
                        return a.name.CompareTo(b.name);
                    });
                    
#if DEBUG_HELPERS
                    Debug.Log("**** All prefabs ****");
                    m_prefabVcams.ForEach(prefab => Debug.Log(prefab.name));
#endif
                }
            }

            class TimelineManager
            {
                List<PlayableDirector> m_PlayableDirectors;
                public TimelineManager(Scene scene)
                {
                    m_PlayableDirectors = new List<PlayableDirector>();

                    var rootObjects = scene.GetRootGameObjects();
                    foreach (var go in rootObjects)
                    {
                        m_PlayableDirectors.AddRange(go.GetComponentsInChildren<PlayableDirector>(true).ToList());
                    }
                }

                public void UpdateTimelineReference(GameObject upgraded)
                {
                    // TODO: currently I use null check to check if I need to fill a vcam
                    // but user might have cmshot's that are null before upgrade, we don't want to fill them
                    foreach (var playableDirector in m_PlayableDirectors)
                    {
                        if (playableDirector == null) continue;

                        var playableAsset = playableDirector.playableAsset;
                        if (playableAsset is TimelineAsset timelineAsset)
                        {
                            var tracks = timelineAsset.GetOutputTracks();
                            foreach (var track in tracks)
                            {
                                if (track is CinemachineTrack cmTrack)
                                {
                                    var clips = cmTrack.GetClips();
                                    foreach (var clip in clips)
                                    {
                                        if (clip.asset is CinemachineShot cmShot)
                                        {
                                            var exposedRef = cmShot.VirtualCamera;
                                            var vcam = exposedRef.Resolve(playableDirector);
                                            if (vcam == null)
                                            {
                                                var vcamToAdd = upgraded.GetComponent<CinemachineVirtualCameraBase>();
                                                playableDirector.SetReferenceValue(exposedRef.exposedName, vcamToAdd);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
#pragma warning restore CS0618
#endif
