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
        const string k_UnupgradableTag = " - Not fully upgradable by CM";
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
        public void Upgrade(GameObject go, Scene? scene = null)
        {
            var upgrader = new UpgradeObjectToCm3();
            var notUpgradable = upgrader.UpgradeComponents(go);
            upgrader.DeleteObsoleteComponents(go);

            // Report difficult cases
            if (notUpgradable != null)
            {
                notUpgradable.name += k_UnupgradableTag;
                var location = scene == null ? "(prefab asset)" : "(in " + scene.Value.name + ")";
                Debug.LogWarning("Freelook camera \"" + go.name + "\" " + location + " may not have been fully upgraded " +
                    "automatically.  A reference copy of the original was sved to " + notUpgradable.name);
            }
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
#if DEBUG_HELPERS
                    Debug.Log("Opening scene: " + m_SceneManager.GetScenePath(s));
#endif
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
#if DEBUG_HELPERS
                    Debug.Log("Opening scene: " + m_SceneManager.GetScenePath(s));
#endif
                    var activeScene = EditorSceneManager.OpenScene(m_SceneManager.GetScenePath(s), OpenSceneMode.Single);
                    var allGameObjectsInScene = GetAllGameObjects();
                    foreach (var conversionLink in conversionLinks)
                    {
                        var prefabInstance = Find(conversionLink.originalGUIDName, allGameObjectsInScene);
                        var convertedCopy = Find(conversionLink.convertedGUIDName, allGameObjectsInScene);
                        if (prefabInstance == null || convertedCopy == null)
                            continue; // ignore, instance is not in this scene
                        
                        // copy cm camera related properties (such as targets and lens)
                        UnityEditorInternal.ComponentUtility.CopyComponent(convertedCopy.GetComponent<CmCamera>());
                        UnityEditorInternal.ComponentUtility.PasteComponentValues(prefabInstance.GetComponent<CmCamera>());
                        
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

            static GameObject Find(string name, List<GameObject> gameobjects)
            {
                foreach (var go in gameobjects)
                {
                    if (go != null && go.name.Equals(name))
                        return go;
                }
                return null;
            }
        }

        void UpgradeInScenes()
        {
            var sceneCount = m_SceneManager.SceneCount;
            for (var s = 0; s < sceneCount; ++s)
            {
#if DEBUG_HELPERS
                Debug.Log("Opening scene: " + m_SceneManager.GetScenePath(s));
#endif
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

                    Upgrade(go, activeScene);
                    modified = true;
                    timelineManager.UpdateTimelineReference(go.GetComponent<CinemachineVirtualCameraBase>());
                    EditorUtility.SetDirty(go);
                }

                if (modified)
                {
                    EditorSceneManager.MarkSceneDirty(activeScene);
                    EditorSceneManager.SaveScene(activeScene); // update scene
                }
            }
        }
        

        class SceneManager
        {
            public List<string> s_IgnoreList = new() {}; // TODO: expose this to the user
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
                    foreach (var ignore in s_IgnoreList)
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
                m_AllScenePaths.ForEach(path => Debug.Log(path));
                Debug.Log("********************");
#endif
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
                m_PrefabAssets.ForEach(prefab => Debug.Log(prefab.name));
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
