#if UNITY_EDITOR
// #define DEBUG_HELPERS
// suppress obsolete warnings
#pragma warning disable CS0618

using System;
using System.Collections.Generic;
using System.Linq;
using Cinemachine.Utility;
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
    // Consider refactoring this along these lines:
    //
    // UpgradeManager:
    //  - Scans project for scenes and prefabs and timelines, maintains lists of these things
    //  - This should be a framework only.  Has no knowledge of upgrade specifics (cm3 vs cm4 etc).  Does not know which classes are getting upgraded.
    //  - Instantiates and invokes specific upgraders (e.g. cm3, cm4, etc)
    //  - Provides public API for launching an upgrade
    //
    // Cm2ToCm3Upgrader:
    //  - Knows which classes need to be upgraded
    //  - Uses the services of UpgradeManager to access objects in the project
    //  - Upgrading objects:
    //      - Don't delete old vcam components - just disable them and maintain a list of (old, new) component pairs.
    //      - Use the list later for upgrading timeline references.
    //      - Keeping the old components will help the client upgrade their scripts because references will be maintained
    //      - UpgradeManager can expose a separate API to delete all obsolete components after the client has finished upgrading custom stuff manually
    //  - Be minimally destructive.  Keep things as much as possible, delete them at the end so information is not lost during the process.
    //  - The prefab upgrade process should be something like this:
    //      - find all prefab instances in the project, record which prefab they're instances of
    //      - unpack completely all prefab instances
    //      - upgrade all prefabs
    //      - upgrade all prefab instances
    //      - re-connect the prefab instances to their upgraded prefabs
    //  - Objects that are not upgradable should be tracked (keep a list) and a report generated at the end.  Maybe it makes sense to keep copies of them in the project, but maybe not


    /// <summary>
    /// Upgrades cm2 to cm3
    /// </summary>
    class CinemachineUpgradeManager
    {
        SceneManager m_SceneManager;
        PrefabManager m_PrefabManager;

        public CinemachineUpgradeManager()
        {
            m_SceneManager = new SceneManager();
            m_PrefabManager = new PrefabManager();
        }
            
        /// <summary>
        /// Upgrades the input gameobject.  Legacy components are not deleted (exception: freeLook rigs).
        /// Instead, they are disabled and new components are created alongside.
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
            if (EditorUtility.DisplayDialog(
                "Upgrade Project to Cinemachine 3",
                "This project contains objects created with Cinemachine 2, "
                + "which need to be upgraded to Cinemachine 3 equivalents.  "
                + "This can mostly be done automatically, but it is possible that "
                + "some objects might not be fully converted.\n\n"
                + "NOTE: Undo is not supported for this operation.  You are strongly "
                + "advised to make a full backup of the project before proceeding.\n\n"
                + "Upgrade project?",
                "I made a backup, go ahead", "Cancel"))
            {
                UpgradePrefabs();
                UpgradeInScenes();
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
                bool result = true;
                old.TryGetComponent<CinemachineFreeLook>(out var freelook);
                if (freelook != null)
                    result = UpgradeFreelook(freelook);
                else
                {
                    old.TryGetComponent<CinemachineVirtualCamera>(out var vcam);
                    if (vcam != null)
                        result = UpgradeVcam(vcam);
                }

                // Cinemachine pipeline components (there will be more of these...)
                old.TryGetComponent<CinemachineComposer>(out var composer);
                if (composer != null)
                    UpgradeComposer(composer);
                old.TryGetComponent<CinemachineFramingTransposer>(out var ft);
                if (ft != null)
                    UpgradeFramingTransposer(ft);

                return result;
            }
            
            static void UpgradeComposer(CinemachineComposer c)
            {
                var rc = c.gameObject.AddComponent<CinemachineRotationComposer>();
                rc.TrackedObjectOffset = c.m_TrackedObjectOffset;
                rc.Lookahead = new LookaheadSettings
                {
                    Enabled = c.m_LookaheadTime > 0,
                    Time = c.m_LookaheadTime,
                    Smoothing = c.m_LookaheadSmoothing,
                    IgnoreY = c.m_LookaheadIgnoreY
                };
                rc.Damping = new Vector2(c.m_HorizontalDamping, c.m_VerticalDamping);
                rc.Composition = ScreenComposerSettingsFromLegacyComposer(c);
                rc.CenterOnActivate = c.m_CenterOnActivate;
                Object.DestroyImmediate(c);
            }

            static void UpgradeFramingTransposer(CinemachineFramingTransposer c)
            {
                var pc = c.gameObject.AddComponent<CinemachinePositionComposer>();
                pc.TrackedObjectOffset = c.m_TrackedObjectOffset;
                pc.Lookahead = new LookaheadSettings
                {
                    Enabled = c.m_LookaheadTime > 0,
                    Time = c.m_LookaheadTime,
                    Smoothing = c.m_LookaheadSmoothing,
                    IgnoreY = c.m_LookaheadIgnoreY
                };
                pc.CameraDistance = c.m_CameraDistance;
                pc.DeadZoneDepth = c.m_DeadZoneDepth;
                pc.Damping = new Vector3(c.m_XDamping, c.m_YDamping, c.m_ZDamping);
                pc.Composition = new ScreenComposerSettings
                {
                    ScreenPosition = new Vector2(c.m_ScreenX, c.m_ScreenY) - new Vector2(0.5f, 0.5f),
                    DeadZoneSize = new Vector2(c.m_DeadZoneWidth, c.m_DeadZoneHeight),
                    SoftZoneSize = new Vector2(c.m_SoftZoneWidth, c.m_SoftZoneHeight),
                    Bias = new Vector2(c.m_BiasX, c.m_BiasY)
                };
                pc.UnlimitedSoftZone = c.m_UnlimitedSoftZone;
                pc.CenterOnActivate = c.m_CenterOnActivate;
                pc.GroupFramingMode = pc.GroupFramingMode == CinemachinePositionComposer.FramingModes.None 
                    ? CinemachinePositionComposer.FramingModes.None : (CinemachinePositionComposer.FramingModes)((int)c.m_GroupFramingMode + 1);
                pc.AdjustmentMode = (CinemachinePositionComposer.AdjustmentModes)c.m_AdjustmentMode;
                pc.GroupFramingSize = c.m_GroupFramingSize;
                pc.DollyRange = new Vector2(-c.m_MaxDollyIn, c.m_MaxDollyOut);
                pc.TargetDistanceRange = new Vector2(c.m_MinimumDistance, c.m_MaximumDistance);
                pc.FovRange = new Vector2(c.m_MinimumFOV, c.m_MaximumFOV);
                pc.OrthoSizeRange = new Vector2(c.m_MinimumOrthoSize, c.m_MaximumOrthoSize);
                Object.DestroyImmediate(c);
            }
            
            static ScreenComposerSettings ScreenComposerSettingsFromLegacyComposer(CinemachineComposer c)
            {
                return new ScreenComposerSettings
                {
                    ScreenPosition = new Vector2(c.m_ScreenX, c.m_ScreenY) - new Vector2(0.5f, 0.5f),
                    DeadZoneSize = new Vector2(c.m_DeadZoneWidth, c.m_DeadZoneHeight),
                    SoftZoneSize = new Vector2(c.m_SoftZoneWidth, c.m_SoftZoneHeight),
                    Bias = new Vector2(c.m_BiasX, c.m_BiasY)
                };
            }
            
            static bool UpgradeFreelook(CinemachineFreeLook freelook)
            {
                var topRig = freelook.GetRig(0);
                var middleRig = freelook.GetRig(1);
                var bottomRig = freelook.GetRig(2);
                if (topRig == null || middleRig == null || bottomRig == null)
                    return false;

                var isUpgradable = IsFreelookUpgradable(freelook);
                freelook.enabled = false;

                var go = freelook.gameObject;
                if (!isUpgradable)
                {
                    var clone = Object.Instantiate(freelook.gameObject);
                    clone.name = go.name + " (Not fully upgradable)";
                        
                    Debug.LogWarning("Freelook camera \"" + freelook.name + "\" was not fully upgradable " +
                        "automatically! It was partially upgraded. Your data was saved to " + clone.name);
                }
                var oldExtensions = go.GetComponents<CinemachineExtension>();

                var cmCamera = go.AddComponent<CmCamera>();
                CopyValues<CinemachineVirtualCameraBase>(freelook, cmCamera);
                cmCamera.Follow = freelook.Follow;
                cmCamera.LookAt = freelook.LookAt;
                cmCamera.Target.CustomLookAtTarget = freelook.Follow != freelook.LookAt;
                cmCamera.Transitions = freelook.m_Transitions;
                    
                var freeLookModifier = go.AddComponent<CinemachineFreeLookModifier>();
                ConvertLens(freelook, cmCamera, freeLookModifier);
                ConvertFreelookBody(freelook, go, freeLookModifier);
                ConvertFreelookAim(freelook, go, freeLookModifier);
                ConvertFreelookNoise(freelook, go, freeLookModifier);

                foreach (var extension in oldExtensions)
                    cmCamera.AddExtension(extension);

                cmCamera.TryGetComponent<InputAxisController>(out var inputAxisController);
                if (inputAxisController == null)
                    go.AddComponent<InputAxisController>();

                Object.DestroyImmediate(topRig.gameObject);
                Object.DestroyImmediate(middleRig.gameObject);
                Object.DestroyImmediate(bottomRig.gameObject);
                Object.DestroyImmediate(freelook);
                    
                return true;
            }

            static string[] s_IgnoreList = { "m_ScreenX", "m_ScreenY", "m_BiasX", "m_BiasY", "m_AmplitudeGain", "m_FrequencyGain" };
            static bool IsFreelookUpgradable(CinemachineFreeLook freelook)
            {
                // Freelook is not upgradable if it has:
                // - look at override (parent lookat != child lookat)
                // - different noise profiles on top, mid, bottom ||
                // - different aim component types ||
                // - same aim component types and with different parameters
                var topRig = freelook.GetRig(0);
                var middleRig = freelook.GetRig(1);
                var bottomRig = freelook.GetRig(2);
                var parentLookAt = freelook.LookAt;
                var topNoise = topRig.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
                var middleNoise = middleRig.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
                var bottomNoise = bottomRig.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
                var midAim = middleRig.GetCinemachineComponent(CinemachineCore.Stage.Aim);

                return
                    parentLookAt == topRig.LookAt && parentLookAt == middleRig.LookAt && parentLookAt == bottomRig.LookAt &&
                    PublicFieldsEqual(topNoise, middleNoise) &&
                    PublicFieldsEqual(middleNoise, bottomNoise) &&
                    PublicFieldsEqual(topRig.GetCinemachineComponent(CinemachineCore.Stage.Aim), midAim, s_IgnoreList) &&
                    PublicFieldsEqual(bottomRig.GetCinemachineComponent(CinemachineCore.Stage.Aim), midAim, s_IgnoreList);

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

            static void ConvertLens(CinemachineFreeLook freelook, CmCamera cmCamera, CinemachineFreeLookModifier freeLookModifier)
            {
                if (freelook.m_CommonLens)
                {
                    cmCamera.Lens = freelook.m_Lens;
                }
                else
                {
                    cmCamera.Lens = freelook.GetRig(1).m_Lens;
                    if (!LensesAreEqual(ref freelook.GetRig(1).m_Lens, ref freelook.GetRig(0).m_Lens)
                        || !LensesAreEqual(ref freelook.GetRig(1).m_Lens, ref freelook.GetRig(2).m_Lens))
                    {
                        freeLookModifier.Modifiers.Add(new CinemachineFreeLookModifier.LensModifier
                        {
                            Lens = new CinemachineFreeLookModifier.TopBottomRigs<LensSettings>
                            {
                                Top = freelook.GetRig(0).m_Lens,
                                Bottom = freelook.GetRig(2).m_Lens,
                            }
                        });
                    }
                }

                bool LensesAreEqual(ref LensSettings a, ref LensSettings b)
                {
                    return Mathf.Approximately(a.NearClipPlane, b.NearClipPlane)
                        && Mathf.Approximately(a.FarClipPlane, b.FarClipPlane)
                        && Mathf.Approximately(a.OrthographicSize, b.OrthographicSize)
                        && Mathf.Approximately(a.FieldOfView, b.FieldOfView)
                        && Mathf.Approximately(a.Dutch, b.Dutch)
                        && Mathf.Approximately(a.LensShift.x, b.LensShift.x)
                        && Mathf.Approximately(a.LensShift.y, b.LensShift.y)

                        && Mathf.Approximately(a.SensorSize.x, b.SensorSize.x)
                        && Mathf.Approximately(a.SensorSize.y, b.SensorSize.y)
                        && a.GateFit == b.GateFit
#if CINEMACHINE_HDRP
                        && Mathf.Approximately(a.Iso, b.Iso)
                        && Mathf.Approximately(a.ShutterSpeed, b.ShutterSpeed)
                        && Mathf.Approximately(a.Aperture, b.Aperture)
                        && a.BladeCount == b.BladeCount
                        && Mathf.Approximately(a.Curvature.x, b.Curvature.x)
                        && Mathf.Approximately(a.Curvature.y, b.Curvature.y)
                        && Mathf.Approximately(a.BarrelClipping, b.BarrelClipping)
                        && Mathf.Approximately(a.Anamorphism, b.Anamorphism)
#endif
                        ;
                }
            }

            static void ConvertFreelookBody(CinemachineFreeLook freelook, GameObject go, CinemachineFreeLookModifier freeLookModifier)
            {
                var orbitalFollow = go.AddComponent<CinemachineOrbitalFollow>();
                orbitalFollow.OrbitStyle = CinemachineOrbitalFollow.OrbitStyles.ThreeRing;
                orbitalFollow.Orbits = new Cinemachine3OrbitRig.Settings
                {
                    Top = new Cinemachine3OrbitRig.Orbit
                    {
                        Height = freelook.m_Orbits[0].m_Height,
                        Radius = freelook.m_Orbits[0].m_Radius,
                    },
                    Center = new Cinemachine3OrbitRig.Orbit
                    {
                        Height = freelook.m_Orbits[1].m_Height,
                        Radius = freelook.m_Orbits[1].m_Radius,
                    },
                    Bottom = new Cinemachine3OrbitRig.Orbit
                    {
                        Height = freelook.m_Orbits[2].m_Height,
                        Radius = freelook.m_Orbits[2].m_Radius,
                    },
                    SplineCurvature = freelook.m_SplineCurvature,
                };
                orbitalFollow.BindingMode = freelook.m_BindingMode;
                        
                var top = freelook.GetRig(0).GetCinemachineComponent<CinemachineOrbitalTransposer>();
                var middle = freelook.GetRig(1).GetCinemachineComponent<CinemachineOrbitalTransposer>();
                var bottom = freelook.GetRig(2).GetCinemachineComponent<CinemachineOrbitalTransposer>();

                orbitalFollow.PositionDamping = new Vector3(middle.m_XDamping, middle.m_YDamping, middle.m_ZDamping);
                var topDamping = new Vector3(top.m_XDamping, top.m_YDamping, top.m_ZDamping);
                var bottomDamping = new Vector3(bottom.m_XDamping, bottom.m_YDamping, bottom.m_ZDamping);
                if (!(orbitalFollow.PositionDamping - topDamping).AlmostZero()
                    || !(orbitalFollow.PositionDamping - bottomDamping).AlmostZero())
                {
                    freeLookModifier.Modifiers.Add(new CinemachineFreeLookModifier.PositionDampingModifier
                    {
                        Damping = new CinemachineFreeLookModifier.TopBottomRigs<Vector3> 
                        {
                            Top = topDamping,
                            Bottom = bottomDamping,
                        },
                    });
                }
            }

            static void ConvertFreelookAim(CinemachineFreeLook freelook, GameObject go, CinemachineFreeLookModifier freeLookModifier)
            {
                // We assume that the middle aim is a suitable template
                var template = freelook.GetRig(1).GetCinemachineComponent(CinemachineCore.Stage.Aim);
                if (template == null)
                    return;
                var newAim = (CinemachineComponentBase)go.AddComponent(template.GetType());
                CopyValues(template, newAim);

                // Add modifier if it is a composer
                var middle = newAim as CinemachineRotationComposer;
                var top = freelook.GetRig(0).GetComponentInChildren<CinemachineComposer>();
                var bottom = freelook.GetRig(2).GetComponentInChildren<CinemachineComposer>();
                if (middle != null && top != null && bottom != null)
                {
                    var topComposition = ScreenComposerSettingsFromLegacyComposer(top);
                    var bottomComposition = ScreenComposerSettingsFromLegacyComposer(bottom);

                    if (!ScreenComposerSettings.Approximately(middle.Composition, topComposition)
                        || !ScreenComposerSettings.Approximately(middle.Composition, bottomComposition))
                    {
                        freeLookModifier.Modifiers.Add(new CinemachineFreeLookModifier.CompositionModifier
                        {
                            Composition = new CinemachineFreeLookModifier.TopBottomRigs<ScreenComposerSettings>
                            {
                                Top = topComposition,
                                Bottom = bottomComposition
                            }
                        });
                    }
                }
            }

            static void ConvertFreelookNoise(CinemachineFreeLook freelook, GameObject go, CinemachineFreeLookModifier freeLookModifier)
            {
                // Noise can be on any subset of the rigs
                var top = freelook.GetRig(0).GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
                var middle = freelook.GetRig(1).GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
                var bottom = freelook.GetRig(2).GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
                var template = middle != null && middle.m_NoiseProfile != null 
                    ? middle : (top != null && top.m_NoiseProfile != null? top : bottom);
                if (template == null || template.m_NoiseProfile == null)
                    return;

                var middleNoise = go.AddComponent<CinemachineBasicMultiChannelPerlin>();
                CopyValues(template, middleNoise);

                var middleSettings = GetNoiseSettings(middle);
                middleNoise.m_AmplitudeGain = middleSettings.Amplitude;
                middleNoise.m_FrequencyGain = middleSettings.Frequency;

                var topSettings = GetNoiseSettings(top);
                var bottomSettings = GetNoiseSettings(bottom);
                if (!Mathf.Approximately(topSettings.Amplitude, middleSettings.Amplitude)
                    || !Mathf.Approximately(topSettings.Frequency, middleSettings.Frequency)
                    || !Mathf.Approximately(bottomSettings.Amplitude, middleSettings.Amplitude)
                    || !Mathf.Approximately(bottomSettings.Frequency, middleSettings.Frequency))
                {
                    freeLookModifier.Modifiers.Add(new CinemachineFreeLookModifier.NoiseModifier
                    {
                        Noise = new CinemachineFreeLookModifier.TopBottomRigs<CinemachineFreeLookModifier.NoiseModifier.NoiseSettings>
                        {
                            Top = topSettings,
                            Bottom = bottomSettings
                        }
                    });
                }

                CinemachineFreeLookModifier.NoiseModifier.NoiseSettings GetNoiseSettings(CinemachineBasicMultiChannelPerlin noise)
                {
                    var settings = new CinemachineFreeLookModifier.NoiseModifier.NoiseSettings();
                    if (noise != null)
                    {
                        settings.Amplitude = noise.m_AmplitudeGain;
                        settings.Frequency = noise.m_FrequencyGain;
                    }
                    return settings;
                }
            }

            static bool UpgradeVcam(CinemachineVirtualCamera vcam)
            {
                vcam.enabled = false;
                    
                var go = vcam.gameObject;
                var oldExtensions = go.GetComponents<CinemachineExtension>();

                var cmCamera = go.AddComponent<CmCamera>();
                CopyValues<CinemachineVirtualCameraBase>(vcam, cmCamera);
                cmCamera.Follow = vcam.Follow;
                cmCamera.LookAt = vcam.LookAt;
                cmCamera.Target.CustomLookAtTarget = vcam.Follow != vcam.LookAt;
                cmCamera.Lens = vcam.m_Lens;
                cmCamera.Transitions = vcam.m_Transitions;

                var oldPipeline = vcam.GetComponentPipeline();
                foreach (var oldComponent in oldPipeline)
                    UpgradeComponent(oldComponent, go);

                // Extensions are already in place, but we must connect them to the CmCamera
                foreach (var extension in oldExtensions)
                    cmCamera.AddExtension(extension);

                var pipelineHolder = go.GetComponentInChildren<CinemachinePipeline>().gameObject;
                if (pipelineHolder != null)
                    Object.DestroyImmediate(pipelineHolder);
                Object.DestroyImmediate(vcam);
                    
                return true;

                void UpgradeComponent(CinemachineComponentBase oldComponent, GameObject go)
                {
                    if (oldComponent != null)
                    {
                        // Convert TrackedDolly to SplineDolly
                        if (oldComponent is CinemachineTrackedDolly trackedDolly)
                        {
                            var splineDolly = go.AddComponent<CinemachineSplineDolly>();
                            splineDolly.Damping.Position = new Vector3(
                                trackedDolly.m_XDamping, trackedDolly.m_YDamping, trackedDolly.m_ZDamping);
                            splineDolly.Damping.Angular = Mathf.Max(trackedDolly.m_YawDamping,
                                Mathf.Max(trackedDolly.m_RollDamping, trackedDolly.m_PitchDamping));
                            splineDolly.CameraUp = (CinemachineSplineDolly.CameraUpMode)trackedDolly.m_CameraUp;
                            splineDolly.Damping.Enabled = true;
                            splineDolly.AutomaticDolly.Enabled = trackedDolly.m_AutoDolly.m_Enabled;
                            splineDolly.AutomaticDolly.PositionOffset = trackedDolly.m_AutoDolly.m_PositionOffset;
                            splineDolly.CameraPosition = trackedDolly.m_PathPosition;
                            splineDolly.SplineOffset = trackedDolly.m_PathOffset;
                            var path = trackedDolly.m_Path;
                            if (path != null)
                            {
                                ConvertCinemachinePathToSpline(path, out splineDolly.Spline);
                                Object.DestroyImmediate(path); // GML todo: what if multiple objects are referencing path?  Must convert all paths first
                            }
                            Object.DestroyImmediate(trackedDolly);
                            return;
                        }

                        var newComponent = (CinemachineComponentBase)go.AddComponent(oldComponent.GetType());
                        CopyValues(oldComponent, newComponent);
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

                        // cmShots collected had to have a not null vcam
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
