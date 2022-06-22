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
    //  - Instantiates and invokes specific upgrader (e.g. cm3, cm4, etc)
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
    //  - Testable

    /// <summary>
    /// Upgrades cm2 to cm3
    /// </summary>
    class CinemachineUpgradeManager
    {
        const string k_UnupgradableTag = "(Clone) (Not fully upgradable)";
        SceneManager m_SceneManager;
        PrefabManager m_PrefabManager;

        public CinemachineUpgradeManager()
        {
            m_SceneManager = new SceneManager();
            m_PrefabManager = new PrefabManager();
        }
            
        /// <summary>
        /// Upgrades the input gameObject.  Legacy components are not deleted (exception: freeLook rigs).
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

        /// <summary>
        /// Data to link original prefab data to upgraded prefab data for restoring prefab modifications.
        /// </summary>
        struct ConversionLink
        {
            public string originalName;
            public string originalGUIDName;
            public string convertedGUIDName;
        }
        void UpgradePrefabs()
        {
            var prefabCount = m_PrefabManager.prefabCount; // asset prefabs, not instances
            for (var p = 0; p < prefabCount; ++p)
            {
                var prefabAsset = m_PrefabManager.GetPrefabAsset(p);
                var conversionLinks = new List<ConversionLink>();
                
                // In each scene, check each prefabInstance of prefabAsset and store their modifications in an upgraded copy of the prefab instance.
                var sceneCount = m_SceneManager.SceneCount;
                for (var s = 0; s < sceneCount; ++s)
                {
                    Debug.Log("Opening scene: " + m_SceneManager.GetScenePath(s));
                    var activeScene = EditorSceneManager.OpenScene(m_SceneManager.GetScenePath(s), OpenSceneMode.Single);
                    
                    var allPrefabInstances = 
                        PrefabManager.FindAllInstancesOfPrefabEvenInNestedPrefabs(activeScene, prefabAsset, m_PrefabManager.GetPrefabAssetPath(p));

                    var componentsList = new List<CinemachineVirtualCameraBase>();
                    foreach (var prefabInstance in allPrefabInstances)
                    {
                        componentsList.AddRange(prefabInstance.GetComponentsInChildren<CinemachineFreeLook>(true).ToList());
                    }
                    foreach (var prefabInstance in allPrefabInstances)
                    {
                        componentsList.AddRange(prefabInstance.GetComponentsInChildren<CinemachineVirtualCamera>(true).ToList());
                    }

                    var converted = new List<CinemachineVirtualCameraBase>();
                    foreach (var component in componentsList)
                    {
                        if (component == null || component.ParentCamera is CinemachineFreeLook)
                            continue; // ignore freelook rigs
                        if (converted.Contains(component))
                            continue; // ignore, if already converted (this can happen in nested prefabs)
                        converted.Add(component);
                        
                        var prefabInstance = component.gameObject;
                        var convertedCopy = Object.Instantiate(prefabInstance);
                        Upgrade(convertedCopy);

                        var conversionLink = new ConversionLink
                        {
                            originalName = prefabInstance.name,
                            originalGUIDName = GUID.Generate().ToString(),
                            convertedGUIDName = GUID.Generate().ToString(),
                        };
                        prefabInstance.name = conversionLink.originalGUIDName;
                        convertedCopy.name = conversionLink.convertedGUIDName;
                        conversionLinks.Add(conversionLink);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(prefabInstance); // record name change
                    }

                    EditorSceneManager.SaveScene(activeScene); // save changes (e.g. converted prefab instances)
                }

                // Upgrade Prefab Asset
                using (var editingScope = new PrefabUtility.EditPrefabContentsScope(m_PrefabManager.GetPrefabAssetPath(p)))
                {
                    var prefabContents= editingScope.prefabContentsRoot;
                    var freelooks = prefabContents.GetComponentsInChildren<CinemachineFreeLook>(true);
                    foreach (var freelook in freelooks)
                    {
                        Upgrade(freelook.gameObject);
                    }

                    var vcams = prefabContents.GetComponentsInChildren<CinemachineVirtualCamera>(true);
                    foreach (var vcam in vcams)
                    {
                        if (vcam.ParentCamera is not CinemachineFreeLook) // this check is needed in case a freelook was not fully upgradable
                            Upgrade(vcam.gameObject);
                    }
                }

                // In each scene, restore modifications in all prefab instances of prefab asset by copying data
                // from the linked converted copy of the prefab instance.
                for (int s = 0; s < sceneCount; ++s)
                {
                    Debug.Log("Opening scene: " + m_SceneManager.GetScenePath(s));
                    var activeScene = EditorSceneManager.OpenScene(m_SceneManager.GetScenePath(s), OpenSceneMode.Single);

                    var allGameObjectsInScene = GetAllGameObjects();
                    foreach (var conversionLink in conversionLinks)
                    {
                        var prefabInstance = Find(conversionLink.originalGUIDName, allGameObjectsInScene);
                        var convertedCopy = Find(conversionLink.convertedGUIDName, allGameObjectsInScene);
                        if (prefabInstance == null || convertedCopy == null)
                            continue; // ignore, instance is not in this scene
                        
                        var prefabInstanceComponents = 
                            prefabInstance.GetComponents<CinemachineComponentBase>();
                        var convertedComponents = 
                            convertedCopy.GetComponents<CinemachineComponentBase>();

                        // Delete stages (body, aim, noise, finalize) that are not on the upgraded copy. 
                        foreach (var prefabInstanceComponent in prefabInstanceComponents)
                        {
                            var found = convertedComponents.Any(
                                convertedComponent => prefabInstanceComponent.Stage == convertedComponent.Stage);
                            if (!found)
                            {
                                Object.DestroyImmediate(prefabInstanceComponent);
                            }
                        }

                        // Copy upgraded component and paste it to the prefab instance (as new if needed)
                        foreach (var convertedComponent in convertedComponents)
                        {
                            var didPasteValues = false;
                            UnityEditorInternal.ComponentUtility.CopyComponent(convertedComponent);
                            
                            foreach (var prefabInstanceComponent in prefabInstanceComponents)
                            {
                                if (prefabInstanceComponent.Stage == convertedComponent.Stage)
                                {
                                    if (prefabInstanceComponent.GetType() == convertedComponent.GetType())
                                    {
                                        // Same component type -> paste values
                                        UnityEditorInternal.ComponentUtility.PasteComponentValues(prefabInstanceComponent);
                                        didPasteValues = true;
                                    }
                                    else
                                    {
                                        // Same stage, but different component type -> delete component
                                        Object.DestroyImmediate(prefabInstanceComponent);
                                    }
                                    break;
                                }
                            }

                            if (!didPasteValues)
                            {
                                // No same stage component -> paste as new
                                UnityEditorInternal.ComponentUtility.PasteComponentAsNew(prefabInstance);
                            }
                        }

                        // Restore original scene state (prefab instance name, delete converted copies)
                        prefabInstance.name = conversionLink.originalName;
                        Object.DestroyImmediate(convertedCopy);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(prefabInstance);
                    }

                    EditorSceneManager.SaveScene(activeScene);
                }
            }
            
            // local functions
            List<GameObject> GetAllGameObjects()
            {
                var all = (GameObject[])Resources.FindObjectsOfTypeAll(typeof(GameObject));
                return all.Where(go => !EditorUtility.IsPersistent(go.transform.root.gameObject) && 
                    !(go.hideFlags == HideFlags.NotEditable || go.hideFlags == HideFlags.HideAndDontSave)).ToList();
            }
            static GameObject Find(string name, List<GameObject> gameobjects) => 
                gameobjects.FirstOrDefault(go => go.name.Equals(name));
        }

        void UpgradeInScenes()
        {
            var sceneCount = m_SceneManager.SceneCount;
            for (var s = 0; s < sceneCount; ++s)
            {
                Debug.Log("Opening scene: " + m_SceneManager.GetScenePath(s));
                var activeScene = EditorSceneManager.OpenScene(m_SceneManager.GetScenePath(s), OpenSceneMode.Single);
                
                var timelineManager = new TimelineManager(activeScene);
                    
                var componentsList = new List<CinemachineVirtualCameraBase>();
                var rootObjects = activeScene.GetRootGameObjects();
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
                    if (component == null || component.ParentCamera is CinemachineFreeLook)
                        continue; // ignore null components or freelook children vcams
                    
                    var go = component.gameObject;
                    if (go.name.Contains(k_UnupgradableTag))
                        continue; // ignore, because we created it

                    if (Upgrade(go))
                    {
                        modified = true;
                        timelineManager.UpdateTimelineReference(go.GetComponent<CinemachineVirtualCameraBase>());
                        EditorUtility.SetDirty(go);
                    }
                }

                if (modified)
                {
                    EditorSceneManager.MarkSceneDirty(activeScene);
                    EditorSceneManager.SaveScene(activeScene); // update scene
                }
            }
        }
        
        static List<string> s_IgnoreListGigaya = new()
        { 
            "Characters_Zoo.unity", 
            "VFXSample.unity", 
            "Scene_CharacterGym_Interactables.unity", 
            "Scene_Dev_Character_RigAndFace.unity",
            "Scene_Dev_DesignTestGameplayOverlayMap.unity",
            "Scene_Dev_Glyphs.unity",
            "Scene_Diorama.unity",
            "Scene_Material_And_Polish.unity",
            "Scene_02_OasisClimbIntoGigaya_Geo.unity",
            "Scene_02_OasisClimbIntoGigaya_Mechanics.unity",
            "Scene_Menu_Main.unity",
            "QA_TestScene.unity",
            "Scene_InnerHand_Geo.unity",
            "Scene_InnerHand_Mechanics.unity",
            "Scene_Oasis_Geo_Bckp.unity",
            "SampleScene.unity",
            "VFX_TestScene_v02.unity",
            "StartingArea_Geo.unity",
            "Logic.unity",
            "Lighting.unity"
        };

        class SceneManager
        {
            public int SceneCount { get; private set; }
            public string GetScenePath(int index) => m_AllScenePaths[index];

            List<string> m_AllScenePaths;
            public SceneManager()
            {
                var allSceneGuids = new List<string>();
                allSceneGuids.AddRange(AssetDatabase.FindAssets("t:scene", new[]
                {
                    "Assets"
                }));
                m_AllScenePaths = new List<string>();
                for (var i = allSceneGuids.Count - 1; i >= 0; i--)
                {
                    var sceneGuid = allSceneGuids[i];
                    var scenePath = AssetDatabase.GUIDToAssetPath(sceneGuid);
                    var add = true;
                    foreach (var ignore in s_IgnoreListGigaya)
                    {
                        if (scenePath.Contains(ignore))
                        {
                            add = false;
                            break;
                        }
                    }
                    if (add) {
                        m_AllScenePaths.Add(scenePath);
                    }
                }

                SceneCount = m_AllScenePaths.Count;

#if DEBUG_HELPERS
                Debug.Log("**** All scenes ****");
                m_AllSceneGuids.ForEach(guid => Debug.Log(AssetDatabase.GUIDToAssetPath(guid)));
                Debug.Log("********************");
#endif
            }
        }
            
        static class Cm2ToCm3Upgrader
        {
            public static bool Upgrade(GameObject old)
            {
#if DEBUG_HELPERS
                Debug.Log("Upgrading " + old.name + " to CM3!");
#endif
                old.TryGetComponent<CinemachineFreeLook>(out var freelook);
                if (freelook != null)
                    return UpgradeFreelook(freelook);

                old.TryGetComponent<CinemachineVirtualCamera>(out var vcam);
                if (vcam != null)
                    return UpgradeVcam(vcam);

                return true;
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
                    var clone = Object.Instantiate(go);
                    clone.name = go.name + k_UnupgradableTag;
                        
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
                var midAim = middleRig.GetCinemachineComponent(CinemachineCore.Stage.RotationControl);

                return
                    parentLookAt == topRig.LookAt && parentLookAt == middleRig.LookAt && parentLookAt == bottomRig.LookAt &&
                    IsEqualNoise(topNoise, middleNoise) &&
                    IsEqualNoise(middleNoise, bottomNoise) &&
                    IsEqualNoise(topNoise, bottomNoise) &&
                    PublicFieldsEqual(topRig.GetCinemachineComponent(CinemachineCore.Stage.RotationControl), midAim, s_IgnoreList) &&
                    PublicFieldsEqual(bottomRig.GetCinemachineComponent(CinemachineCore.Stage.RotationControl), midAim, s_IgnoreList);

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
                        && Mathf.Approximately(a.iso, b.iso)
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
                orbitalFollow.OrbitStyle = CinemachineOrbitalFollow.OrbitMode.ThreeRing;
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
                var template = freelook.GetRig(1).GetCinemachineComponent(CinemachineCore.Stage.RotationControl);
                if (template == null)
                    return;
                var newAim = (CinemachineComponentBase)go.AddComponent(template.GetType());
                CopyValues(template, newAim);
                
                // Add modifier if it is a composer
                var middle = newAim as CinemachineComposer;
                var top = freelook.GetRig(0).GetCinemachineComponent<CinemachineComposer>();
                var bottom = freelook.GetRig(2).GetCinemachineComponent<CinemachineComposer>();
                if (middle != null && top != null && bottom != null)
                {
                    if (!Mathf.Approximately(middle.m_ScreenX, top.m_ScreenX)
                        || !Mathf.Approximately(middle.m_ScreenY, top.m_ScreenY)
                        || !Mathf.Approximately(middle.m_ScreenX, bottom.m_ScreenX)
                        || !Mathf.Approximately(middle.m_ScreenY, bottom.m_ScreenY))
                    {
                        freeLookModifier.Modifiers.Add(new CinemachineFreeLookModifier.ScreenPositionModifier
                        {
                            Screen = new CinemachineFreeLookModifier.TopBottomRigs<Vector2>
                            {
                                Top = new Vector2(top.m_ScreenX, top.m_ScreenY),
                                Bottom = new Vector2(bottom.m_ScreenX, bottom.m_ScreenY),
                            }
                        });
                    }
                    if (!Mathf.Approximately(middle.m_BiasX, top.m_BiasX)
                        || !Mathf.Approximately(middle.m_BiasY, top.m_BiasY)
                        || !Mathf.Approximately(middle.m_BiasX, bottom.m_BiasX)
                        || !Mathf.Approximately(middle.m_BiasY, bottom.m_BiasY))
                    {
                        freeLookModifier.Modifiers.Add(new CinemachineFreeLookModifier.BiasPositionModifier
                        {
                            Bias = new CinemachineFreeLookModifier.TopBottomRigs<Vector2>
                            {
                                Top = new Vector2(top.m_BiasX, top.m_BiasY),
                                Bottom = new Vector2(bottom.m_BiasX, bottom.m_BiasY),
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

                var cmCamera = go.AddComponent<CmCamera>();
                if (cmCamera == null)
                {
                    go.TryGetComponent(out cmCamera); // this solves problems when RequireComponent already added CmCamera
                    cmCamera.enabled = true;
                }
                Assert.NotNull(cmCamera);
                CopyValues<CinemachineVirtualCameraBase>(vcam, cmCamera);
                cmCamera.Follow = vcam.Follow;
                cmCamera.LookAt = vcam.LookAt;
                cmCamera.Target.CustomLookAtTarget = vcam.Follow != vcam.LookAt;
                cmCamera.Lens = vcam.m_Lens;
                cmCamera.Transitions = vcam.m_Transitions;
                
                // Extensions are already in place, but we must connect them to the CmCamera
                var oldExtensions = go.GetComponents<CinemachineExtension>();
                foreach (var extension in oldExtensions)
                    cmCamera.AddExtension(extension);

                var oldPipeline = vcam.GetComponentPipeline();
                if (oldPipeline != null) {
                    foreach (var oldComponent in oldPipeline) 
                        UpgradeComponent(oldComponent, go);
                    
                    var pipelineHolder = go.GetComponentInChildren<CinemachinePipeline>().gameObject;
                    if (pipelineHolder != null)
                        Object.DestroyImmediate(pipelineHolder);
                }

                Object.DestroyImmediate(vcam);
                    
                return true;

                // local function
                void UpgradeComponent(CinemachineComponentBase oldComponent, GameObject gObject)
                {
                    if (oldComponent != null)
                    {
                        // Convert TrackedDolly to SplineDolly
                        if (oldComponent is CinemachineTrackedDolly trackedDolly)
                        {
                            var splineDolly = gObject.AddComponent<CinemachineSplineDolly>();
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
                                SearchIteration = 4,
                            };
                            splineDolly.CameraPosition = trackedDolly.m_PathPosition;
                            splineDolly.SplineOffset = trackedDolly.m_PathOffset;
                            var path = trackedDolly.m_Path;
                            if (path != null)
                            {
                                ConvertCinemachinePathToSpline(path, out splineDolly.Spline);
                                path.enabled = false;
                            }
                            Object.DestroyImmediate(trackedDolly);
                            return;
                        }

                        var newComponent = (CinemachineComponentBase)gObject.AddComponent(oldComponent.GetType());
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
                gameObject.TryGetComponent(out spline);
                if (spline != null)
                {
                    return; // already converted
                }
                
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
                        Assert.Fail("Path type (" + pathBase.GetType() + ") is not handled by the upgrader!");
                        break;
                }
            }
        }

        /// <summary>
        /// Terminology:
        /// - Prefab Asset: prefab source that lives with the assets.
        /// - Prefab Instance: instance of a Prefab Asset.
        /// </summary>
        class PrefabManager
        {
            public string GetPrefabAssetPath(int index)
            {
                return AssetDatabase.GetAssetPath(m_PrefabAssets[index]);
            }

            public GameObject GetPrefabAsset(int index)
            {
                return m_PrefabAssets[index];
            }

            public int prefabCount;
            List<GameObject> m_PrefabAssets;
            public PrefabManager()
            {
                // Get all Prefab Assets in the project
                var prefabGuids = AssetDatabase.FindAssets($"t:prefab", new [] { "Assets" });
                var allPrefabs = prefabGuids.Select(
                    g => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g))).ToList();
                for (var i = allPrefabs.Count - 1; i >= 0; i--)
                {
                    var prefab = allPrefabs[i];
                    var assetPath = AssetDatabase.GetAssetPath(prefab);
                    foreach (var ignore in s_IgnoreListGigaya)
                    {
                        if (assetPath.Contains("DesignPrefabs/ActionCamerasAndTriggers"))
                        {
                            allPrefabs.RemoveAt(i);
                            break;
                        }
                    }
                }

                // Select Prefab Assets containing CinemachineVirtualCameras or CinemachineFreeLooks.
                m_PrefabAssets = new List<GameObject>();
                foreach (var prefab in allPrefabs)
                {
                    var freelooks = prefab.GetComponentsInChildren<CinemachineFreeLook>(true);
                    var vcams = prefab.GetComponentsInChildren<CinemachineVirtualCamera>(true);
                    if ((freelooks != null && freelooks.Length > 0) || (vcams != null && vcams.Length > 0))
                    {
                        m_PrefabAssets.Add(prefab);
                    }
                }

                // Sort by dependency - none nested prefabs are first, and then nested prefabs
                m_PrefabAssets.Sort((a, b) =>
                {
                    // if they are not part of each other then we use the name to compare just for consistency.
                    return IsXPartOfY(a, b) ? -1 : IsXPartOfY(b, a) ? 1 : a.name.CompareTo(b.name);

                    static bool IsXPartOfY(GameObject x, GameObject y)
                    {
                        var prefab = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(y));
                        var componentsList = new List<CinemachineVirtualCameraBase>();
                        componentsList.AddRange(prefab.GetComponentsInChildren<CinemachineFreeLook>(true).ToList());
                        componentsList.AddRange(prefab.GetComponentsInChildren<CinemachineVirtualCamera>(true).ToList());

                        var result = false;
                        foreach (var component in componentsList)
                        {
                            if (component == null || component.ParentCamera is CinemachineFreeLook)
                                continue;

                            var prefabInstance = component.gameObject;
                            var r1 = PrefabUtility.GetCorrespondingObjectFromSource(prefabInstance);
                            if (x.Equals(r1))
                            {
                                result = true;
                                break;
                            }
                        }
                        PrefabUtility.UnloadPrefabContents(prefab);
                        return result;
                    }
                });
                
                prefabCount = m_PrefabAssets.Count;
                    
#if DEBUG_HELPERS
                Debug.Log("**** All prefabs ****");
                m_PrefabRoots.ForEach(prefab => Debug.Log(prefab.name));
                Debug.Log("*********************");
#endif
            }

            public static List<GameObject> FindAllInstancesOfPrefabEvenInNestedPrefabs(Scene activeScene, GameObject prefab, string prefabPath)
            {
                // find all freelooks and vcams in the current scene
                var rootObjects = activeScene.GetRootGameObjects();
                var componentsList = new List<CinemachineVirtualCameraBase>();
                foreach (var go in rootObjects)
                {
                    componentsList.AddRange(go.GetComponentsInChildren<CinemachineFreeLook>(true).ToList());
                }

                foreach (var go in rootObjects)
                {
                    componentsList.AddRange(go.GetComponentsInChildren<CinemachineVirtualCamera>(true).ToList());
                }

                var allInstances = new List<GameObject>();
                foreach (var component in componentsList)
                {
                    if (component.ParentCamera is CinemachineFreeLook)
                        continue; // ignore, because it is a freelook rig
                    
                    var prefabInstance = component.gameObject;
                    if (prefabInstance.name.Contains(k_UnupgradableTag))
                        continue; // ignore, because we created it
                    
                    var nearestPrefabRoot = PrefabUtility.GetNearestPrefabInstanceRoot(prefabInstance);
                    var nearestPrefabRootPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(prefabInstance);
                    // Check if prefabInstance is an instance of the input prefab
                    if (nearestPrefabRoot != null && nearestPrefabRootPath.Equals(prefabPath))
                    {
                        if (!allInstances.Contains(nearestPrefabRoot)) 
                            allInstances.Add(nearestPrefabRoot);
                    }
                }
                return allInstances;
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
