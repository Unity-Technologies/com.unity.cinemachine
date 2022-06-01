#if UNITY_EDITOR
// #define DEBUG_HELPERS
// suppress obsolete warnings
#pragma warning disable CS0618

using System;
using System.Collections.Generic;
using System.Linq;
using Codice.CM.SemanticMerge.Gui;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Splines;
using UnityEngine.Timeline;
using Object = UnityEngine.Object;

namespace Cinemachine.Editor
{
    /// <summary>
    /// Upgrades cm2 to cm3
    /// </summary>
    class CinemachineUpgrader
    {
        SceneManager m_SceneManager;
        PrefabManager m_PrefabManager;

        public CinemachineUpgrader()
        {
            m_SceneManager = new SceneManager();
            m_PrefabManager = new PrefabManager();
        }
            
        /// <summary>
        /// Upgrades the input gameobject.
        /// </summary>
        /// <param name="go"></param>
        /// <returns></returns>
        public bool Upgrade(GameObject go)
        {
            return Cm2ToCm3Upgrader.Upgrade(go);
        }
        
        /// <summary>
        /// Upgrades all vcams in all scenes and prefabs
        /// </summary>
        public void UpgradeAll()
        {
            UpgradePrefabs();
            UpgradeInScenes();
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
                        timelineManager.UpdateTimelineReference(go.GetComponent<CinemachineVirtualCameraBase>());
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
                Debug.Log("********************");
#endif
            }
        }
            
        class Cm2ToCm3Upgrader
        {
            public static bool Upgrade(GameObject old)
            {
#if DEBUG_HELPERS
                Debug.Log("Upgrading " + old.name + " to CM3!");
#endif
                old.TryGetComponent<CinemachineFreeLook>(out var freeLook);
                if (freeLook != null)
                {
                    var freelookUpgrader = new FreelookUpgrader(freeLook);
                    return freelookUpgrader.Upgrade();
                }

                old.TryGetComponent<CinemachineVirtualCamera>(out var vcam);
                if (vcam != null)
                {
                    var virtualCameraUpgrader = new VirtualCameraUpgrader(vcam);
                    return virtualCameraUpgrader.Upgrade();
                }

                return true;
            }

            class FreelookUpgrader
            {
                CinemachineFreeLook m_Freelook;
                CinemachineVirtualCamera m_TopRig, m_MiddleRig, m_BottomRig;

                public FreelookUpgrader(CinemachineFreeLook freelook)
                {
                    m_Freelook = freelook;
                    if (freelook == null)
                        return;
                    m_TopRig = freelook.GetRig(0);
                    m_MiddleRig = freelook.GetRig(1);
                    m_BottomRig = freelook.GetRig(2);
                }
                
                public bool Upgrade()
                {
                    if (m_Freelook == null) 
                        return false;

                    var isUpgradable = IsFreelookUpgradable();
                    m_Freelook.enabled = false;

                    var go = m_Freelook.gameObject;
                    if (!isUpgradable)
                    {
                        var clone = Object.Instantiate(m_Freelook.gameObject);
                        clone.name = go.name + " (Not fully upgradable)";
                        
                        Debug.LogWarning("Freelook camera \"" + m_Freelook.name + "\" was not fully upgradable " +
                            "automatically! It was partially upgraded. Your data was saved to " + clone.name);
                    }
                    var oldExtensions = go.GetComponents<CinemachineExtension>();

                    var cmCamera = go.AddComponent<CmCamera>();
                    CopyValues<CinemachineVirtualCameraBase>(m_Freelook, cmCamera);
                    cmCamera.Follow = m_Freelook.Follow;
                    cmCamera.LookAt = m_Freelook.LookAt;
                    cmCamera.Transitions = m_Freelook.m_Transitions;
                    
                    var freeLookModifier = go.AddComponent<CinemachineFreeLookModifier>();
                    ConvertLens(cmCamera, freeLookModifier);
                    
                    ConvertFreelookBody(go, freeLookModifier);
                    ConvertFreelookAim(go, freeLookModifier);
                    ConvertFreelookNoise(go, freeLookModifier);

                    foreach (var extension in oldExtensions)
                    {
                        cmCamera.AddExtension(extension);
                    }
                    if (cmCamera.GetComponent<InputAxisController>() == null)
                    {
                        go.AddComponent<InputAxisController>();
                    }

                    var topRig = m_TopRig.gameObject;
                    var middleRig = m_MiddleRig.gameObject;
                    var bottomRig = m_BottomRig.gameObject;
                    Object.DestroyImmediate(topRig);
                    Object.DestroyImmediate(middleRig);
                    Object.DestroyImmediate(bottomRig);
                    Object.DestroyImmediate(m_Freelook);
                    
                    return true;
                }

                static string[] s_IgnoreList = { "m_ScreenX", "m_ScreenY", "m_BiasX", "m_BiasY" };
                bool IsFreelookUpgradable()
                {
                    // Freelook is not upgradable if it has:
                    // - look at override (parent lookat != child lookat)
                    // - different noise profiles on top, mid, bottom ||
                    // - different aim component types ||
                    // - same aim component types and with different parameters
                    var parentLookAt = m_Freelook.LookAt;
                    var topNoise = m_TopRig.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
                    var middleNoise = m_MiddleRig.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
                    var bottomNoise = m_BottomRig.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
                    var midAim = m_MiddleRig.GetCinemachineComponent(CinemachineCore.Stage.RotationControl);

                    return
                        parentLookAt == m_TopRig.LookAt && parentLookAt == m_MiddleRig.LookAt && parentLookAt == m_BottomRig.LookAt &&
                        IsEqualNoise(topNoise, middleNoise) &&
                        IsEqualNoise(middleNoise, bottomNoise) &&
                        IsEqualNoise(topNoise, bottomNoise) &&
                        PublicFieldsEqual(m_TopRig.GetCinemachineComponent(CinemachineCore.Stage.RotationControl), midAim, s_IgnoreList) &&
                        PublicFieldsEqual(m_BottomRig.GetCinemachineComponent(CinemachineCore.Stage.RotationControl), midAim, s_IgnoreList);

                    static bool IsEqualNoise(CinemachineBasicMultiChannelPerlin a, CinemachineBasicMultiChannelPerlin b)
                    {
                        return a == null || b == null ||
                            ((a.m_NoiseProfile == null || b.m_NoiseProfile == null || a.m_NoiseProfile == b.m_NoiseProfile)
                                && a.m_PivotOffset == b.m_PivotOffset);
                    }

                    static bool PublicFieldsEqual(CinemachineComponentBase a, CinemachineComponentBase b, params string[] ignoreList)
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
                            var name = pi.Name;
                            if (ignoreList.Contains(name))
                                continue; // ignore
                            
                            var field = aType.GetField(name);
                            if (!field.GetValue(a).Equals(field.GetValue(b))) 
                            {
#if DEBUG_HELPERS
                                Debug.Log("Rig values differ: " + name);
#endif
                                return false;
                            }
                        }

                        return true;
                    }
                }

                void ConvertLens(CmCamera cmCamera, CinemachineFreeLookModifier freeLookModifier)
                {
                    if (m_Freelook.m_CommonLens)
                    {
                        cmCamera.Lens = m_Freelook.m_Lens;
                    }
                    else
                    {
                        cmCamera.Lens = m_MiddleRig.m_Lens;
                        freeLookModifier.Modifiers.Add(new CinemachineFreeLookModifier.LensModifier
                        {
                            Lens = new CinemachineFreeLookModifier.TopBottomRigs<LensSettings>
                            {
                                Top = m_BottomRig.m_Lens,
                                Bottom = m_TopRig.m_Lens,
                            }
                        });
                    }
                }

                void ConvertFreelookBody(GameObject go, CinemachineFreeLookModifier freeLookModifier)
                {
                    var orbitalFollow = go.AddComponent<CinemachineOrbitalFollow>();
                    orbitalFollow.Orbits = ConvertFreelookSettings();
                    orbitalFollow.BindingMode = m_Freelook.m_BindingMode;
                        
                    var top = m_TopRig.GetCinemachineComponent<CinemachineOrbitalTransposer>();
                    var middle = m_MiddleRig.GetCinemachineComponent<CinemachineOrbitalTransposer>();
                    var bottom = m_BottomRig.GetCinemachineComponent<CinemachineOrbitalTransposer>();

                    orbitalFollow.PositionDamping = new Vector3(middle.m_XDamping, middle.m_YDamping, middle.m_ZDamping);
                    freeLookModifier.Modifiers.Add(new CinemachineFreeLookModifier.PositionDampingModifier
                    {
                        Damping = new CinemachineFreeLookModifier.TopBottomRigs<Vector3>
                        {
                            Top = new Vector3 { x = 0, y = top.m_YDamping, z = top.m_ZDamping },
                            Bottom = new Vector3 { x = 0, y = bottom.m_YDamping, z = bottom.m_ZDamping },
                        },
                    });
                        
                    Cinemachine3OrbitRig.Settings ConvertFreelookSettings()
                    {
                        var orbits = m_Freelook.m_Orbits;
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
                            SplineCurvature = m_Freelook.m_SplineCurvature,
                        };
                    }
                }

                void ConvertFreelookAim(GameObject go, CinemachineFreeLookModifier freeLookModifier)
                {
                    var middle = m_MiddleRig.GetCinemachineComponent(CinemachineCore.Stage.RotationControl);
                    if (middle == null)
                    {
                        return; // no need to return because all aims are null on this freelook - see IsFreelookUpgradable why
                    }

                    var newComponent = (CinemachineComponentBase)go.AddComponent(middle.GetType());
                    CopyValues(middle, newComponent);
                    
                    var topComposer = m_TopRig.GetCinemachineComponent<CinemachineComposer>();
                    var bottomComposer = m_BottomRig.GetCinemachineComponent<CinemachineComposer>();
                    freeLookModifier.Modifiers.Add(new CinemachineFreeLookModifier.ScreenPositionModifier
                    {
                        Screen = new CinemachineFreeLookModifier.TopBottomRigs<Vector2>
                        {
                            Top = topComposer == null
                                ? Vector3.zero
                                : new Vector2(topComposer.m_ScreenX, topComposer.m_ScreenY),
                            Bottom = bottomComposer == null
                                ? Vector3.zero
                                : new Vector2(bottomComposer.m_ScreenX, bottomComposer.m_ScreenY),
                        }
                    });
                    freeLookModifier.Modifiers.Add(new CinemachineFreeLookModifier.BiasPositionModifier
                    {
                        Bias = new CinemachineFreeLookModifier.TopBottomRigs<Vector2>
                        {
                            Top = topComposer == null
                                ? Vector3.zero
                                : new Vector2(topComposer.m_BiasX, topComposer.m_BiasY),
                            Bottom = bottomComposer == null
                                ? Vector3.zero
                                : new Vector2(bottomComposer.m_BiasX, bottomComposer.m_BiasY),
                        }
                    });
                }

                void ConvertFreelookNoise(GameObject go, CinemachineFreeLookModifier freeLookModifier)
                {
                    var top = m_TopRig.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
                    var middle = m_MiddleRig.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
                    var bottom = m_BottomRig.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
                    if (top == null && middle == null && bottom == null)
                    {
                        return;
                    }
                    
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

            class VirtualCameraUpgrader
            {
                CinemachineVirtualCamera m_Vcam;
                public VirtualCameraUpgrader(CinemachineVirtualCamera vcam)
                {
                    m_Vcam = vcam;
                }
                
                public bool Upgrade()
                {
                    if (m_Vcam == null)
                        return false;

                    m_Vcam.enabled = false;
                    
                    var go = m_Vcam.gameObject;
                    var oldExtensions = go.GetComponents<CinemachineExtension>();

                    var cmCamera = go.AddComponent<CmCamera>();
                    CopyValues<CinemachineVirtualCameraBase>(m_Vcam, cmCamera);
                    cmCamera.Follow = m_Vcam.Follow;
                    cmCamera.LookAt = m_Vcam.LookAt;
                    cmCamera.Lens = m_Vcam.m_Lens;
                    cmCamera.Transitions = m_Vcam.m_Transitions;

                    var oldPipeline = m_Vcam.GetComponentPipeline();
                    foreach (var oldComponent in oldPipeline)
                    {
                        UpgradeComponent(oldComponent, go);
                    }

                    foreach (var extension in oldExtensions)
                    {
                        cmCamera.AddExtension(extension);
                    }

                    var pipelineHolder = go.GetComponentInChildren<CinemachinePipeline>().gameObject;
                    
                    Object.DestroyImmediate(pipelineHolder);
                    Object.DestroyImmediate(m_Vcam);
                    
                    return true;

                    static void UpgradeComponent(CinemachineComponentBase oldComponent, GameObject go)
                    {
                        if (oldComponent != null)
                        {
                            if (oldComponent is CinemachineTrackedDolly trackedDolly)
                            {
                                var splineDolly = go.AddComponent<CinemachineSplineDolly>();
                                splineDolly.Damping = new Vector3(
                                    trackedDolly.m_XDamping, trackedDolly.m_YDamping, trackedDolly.m_ZDamping);
                                splineDolly.AngularDamping = Mathf.Max(trackedDolly.m_YawDamping,
                                    Mathf.Max(trackedDolly.m_RollDamping, trackedDolly.m_PitchDamping));
                                splineDolly.CameraUp = (CinemachineSplineDolly.CameraUpMode)trackedDolly.m_CameraUp;
                                splineDolly.DampingEnabled = true;
                                splineDolly.AutomaticDolly = new CinemachineSplineDolly.AutoDolly
                                {
                                    Enabled = trackedDolly.m_AutoDolly.m_Enabled,
                                    PositionOffset = trackedDolly.m_AutoDolly.m_PositionOffset,
                                    SearchResolution = trackedDolly.m_AutoDolly.m_SearchResolution,
                                };
                                splineDolly.CameraPosition = trackedDolly.m_PathPosition;
                                splineDolly.SplineOffset = trackedDolly.m_PathOffset;
                                var path = trackedDolly.m_Path;
                                if (path != null)
                                {
                                    ConvertCinemachinePathToSpline(path, out splineDolly.Spline);
                                    Object.DestroyImmediate(path);
                                }
                                Object.DestroyImmediate(trackedDolly);
                                return;
                            }

                            var newComponent = (CinemachineComponentBase)go.AddComponent(oldComponent.GetType());
                            CopyValues(oldComponent, newComponent);
                        }
                    }
                }
            }

            static void CopyValues<T>(T from, T to)
            {
                var json = JsonUtility.ToJson(from);
                JsonUtility.FromJsonOverwrite(json, to);
            }

            static void ConvertCinemachinePathToSpline(CinemachinePathBase pathBase, out SplineContainer spline)
            {
                var gameObject = pathBase.gameObject;
                switch (pathBase)
                {
                    case CinemachinePath path:
                    {
                        spline = gameObject.AddComponent<SplineContainer>();
                        var waypoints = path.m_Waypoints;
                        spline.Spline = new Spline(waypoints.Length, path.Looped);
            
                        var splineRoll = gameObject.AddComponent<CinemachineSplineRoll>();
                        splineRoll.Roll = new SplineData<float>();
                        for (var i = 0; i < waypoints.Length; i++)
                        {
                            spline.Spline.Add(new BezierKnot
                            {
                                Position = waypoints[i].position,
                                Rotation = Quaternion.identity,
                                TangentIn = -waypoints[i].tangent,
                                TangentOut = waypoints[i].tangent,
                            });
                            splineRoll.Roll.Add(new DataPoint<float>(i, waypoints[i].roll));
                        }

                        break;
                    }
                    case CinemachineSmoothPath smoothPath:
                    {
                        spline = gameObject.AddComponent<SplineContainer>();
                        var waypoints = smoothPath.m_Waypoints;
                        spline.Spline = new Spline(waypoints.Length, smoothPath.Looped)
                        {
                            EditType = SplineType.CatmullRom
                        };

                        var splineRoll = gameObject.AddComponent<CinemachineSplineRoll>();
                        splineRoll.Roll = new SplineData<float>();
                        for (var i = 0; i < waypoints.Length; i++)
                        {
                            spline.Spline.Add(new BezierKnot
                            {
                                Position = waypoints[i].position,
                                Rotation = Quaternion.identity,
                            });
                            splineRoll.Roll.Add(new DataPoint<float>(i, waypoints[i].roll));
                        }

                        break;
                    }
                    default:
                        Assert.True(false, "Path type (" + pathBase.GetType() + ") is not handled by the upgrader!");
                        spline = null;
                        break;
                }
            }
        }

        class PrefabManager
        {
            public GameObject LoadPrefabContents(int index)
            {
                return PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(m_PrefabVcams[index]));
            }

            public void SavePrefabContents(int index, GameObject contents)
            {
                PrefabUtility.SaveAsPrefabAsset(contents, AssetDatabase.GetAssetPath(m_PrefabVcams[index]));
                PrefabUtility.UnloadPrefabContents(contents);
            }
                
            public int prefabCount;
            List<GameObject> m_PrefabVcams;
            public PrefabManager()
            {
                var prefabGuids = AssetDatabase.FindAssets($"t:prefab");
                var allPrefabs = prefabGuids.Select(
                    g => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g))).ToList();

                // Ignore prefabs that are not CinemachineVirtualCameras or CinemachineFreeLooks.
                m_PrefabVcams = allPrefabs.Where(p =>
                    p.GetComponent<CinemachineVirtualCamera>() != null ||
                    p.GetComponent<CinemachineFreeLook>() != null).ToList();
                    
                // Sort by no variant prefabs first
                m_PrefabVcams.Sort((a, b) =>
                {
                    var aIsVariant = PrefabUtility.IsPartOfVariantPrefab(a);
                    var bIsVariant = PrefabUtility.IsPartOfVariantPrefab(b);

                    if (aIsVariant != bIsVariant)
                        return -1;

                    // if both no variants or both variants, we just use the name to compare just to be consistent.
                    return a.name.CompareTo(b.name);
                });

                prefabCount = m_PrefabVcams.Count;
                    
#if DEBUG_HELPERS
                Debug.Log("**** All prefabs ****");
                m_PrefabVcams.ForEach(prefab => Debug.Log(prefab.name));
                Debug.Log("*********************");
#endif
            }
        }

        class TimelineManager
        {
            Dictionary<PlayableDirector, List<CinemachineShot>> m_CmShotsToUpdate;
            public TimelineManager(Scene scene)
            {
                m_CmShotsToUpdate = new Dictionary<PlayableDirector, List<CinemachineShot>>();
                var playableDirectors = new List<PlayableDirector>();

                var rootObjects = scene.GetRootGameObjects();
                foreach (var go in rootObjects)
                {
                    playableDirectors.AddRange(go.GetComponentsInChildren<PlayableDirector>(true).ToList());
                }
                
                // collect all cmShots that may require a reference update
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
                                        if (vcam != null)
                                        {
                                            if (!m_CmShotsToUpdate.ContainsKey(playableDirector))
                                            {
                                                m_CmShotsToUpdate.Add(playableDirector, new List<CinemachineShot>());
                                            }
                                            m_CmShotsToUpdate[playableDirector].Add(cmShot);
                                        }
                                    }
                                }
                            }

                            if (track is AnimationTrack animationTrack)
                            {
                                if (animationTrack.inClipMode)
                                {
                                    var clips = animationTrack.GetClips();
                                    var animationClips = clips
                                        .Select(c => c.asset) //animation clip is stored in the clip's asset
                                        .OfType<AnimationPlayableAsset>() //need to cast to the correct asset type
                                        .Select(asset => asset.clip); //finally we get an animation clip!

                                    foreach (var animationClip in animationClips)
                                        ProcessAnimationClip(animationClip);
                                }
                                else //uses recorded clip
                                    ProcessAnimationClip(animationTrack.infiniteClip);
                            }
                        }
                    }
                }
            }
            
            static void ProcessAnimationClip(AnimationClip animationClip)
            {
                var existingEditorBindings = AnimationUtility.GetCurveBindings(animationClip);
                foreach (var previousBinding in existingEditorBindings)
                {
                    var newBinding = previousBinding;
                    var path = newBinding.path;
                    if (path.Contains("cm"))
                    {
                        //path is either cm only, or someParent/someOtherParent/.../cm. In the second case, we need to remove /cm.
                        var index = Mathf.Max(0, path.IndexOf("cm") - 1);
                        newBinding.path = path.Substring(0, index);
                        var curve = AnimationUtility.GetEditorCurve(animationClip, previousBinding); //keep existing curves
                        AnimationUtility.SetEditorCurve(animationClip, previousBinding, null); //remove previous binding
                        AnimationUtility.SetEditorCurve(animationClip, newBinding, curve); //set new binding
                    }
                }
            }

            /// <summary>
            /// Updates timeline reference with the upgraded vcam. This is called after each vcam is upgraded,
            /// because then it is easy to find the corresponding timeline reference; it is the one whose
            /// ExposedReference resolves to null.
            /// </summary>
            /// <param name="upgraded"></param>
            public void UpdateTimelineReference(CinemachineVirtualCameraBase upgraded)
            {
                if (upgraded == null)
                    return;

                foreach (var (director, cmShots) in m_CmShotsToUpdate)
                {
                    foreach (var cmShot in cmShots)
                    {
                        var exposedRef = cmShot.VirtualCamera;
                        var vcam = exposedRef.Resolve(director);
                        if (vcam == null && exposedRef.exposedName != 0)
                        {
                            director.SetReferenceValue(exposedRef.exposedName, upgraded);
                        }
                    }
                }
            }
        }
    }
}
#pragma warning restore CS0618
#endif
