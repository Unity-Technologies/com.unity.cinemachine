#if !CINEMACHINE_NO_CM2_SUPPORT
//#define DEBUG_HELPERS
#pragma warning disable CS0618 // suppress obsolete warnings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
#if CINEMACHINE_TIMELINE
using UnityEngine.Timeline;
#endif

namespace Unity.Cinemachine.Editor
{
    /// <summary>
    /// Upgrades cm2 to cm3
    /// </summary>
    class CinemachineUpgradeManager
    {
        const string k_UnupgradableTag = " BACKUP - not fully upgradable by CM";
        const string k_RenamePrefix = "__CM__UPGRADER__RENAME__ ";
        const string k_CopyPrefix = "__CM__UPGRADER__COPY__ ";

        UpgradeObjectToCm3 m_ObjectUpgrader;
        SceneManager m_SceneManager;
        PrefabManager m_PrefabManager;

        // This gets set to help with more informative warning messages about objects
        string m_CurrentSceneOrPrefab;
        const string k_ProgressBarTitle = "Upgrade Progress";

        /// <summary>
        /// Upgrades the input gameObject.  Referenced objects (e.g. paths) may also get upgraded.
        /// Obsolete components are deleted.  Timeline references are not patched.
        /// Undo is supported.
        /// </summary>
        public static void UpgradeSingleObject(GameObject go)
        {
            var objectUpgrader = new UpgradeObjectToCm3();
            try 
            {
                var notUpgradable = objectUpgrader.UpgradeComponents(go);
                objectUpgrader.DeleteObsoleteComponents(go);
            
                // Report difficult cases
                if (notUpgradable != null)
                {
                    notUpgradable.name = go.name + k_UnupgradableTag;
                    Debug.LogWarning("Upgrader: " + go.name + " may not have been fully upgraded " +
                        "automatically.  A reference copy of the original was saved to " + notUpgradable.name);
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                OnUnsuccessfulUpgrade();
            }
        }

        /// <summary>
        /// Upgrades all the gameObjects in the current scene.  
        /// Obsolete components are deleted.  Timeline references are not patched.
        /// Undo is supported.
        /// </summary>
        public static void UpgradeObjectsInCurrentScene()
        {
            if (EditorUtility.DisplayDialog(
                "Upgrade objects in the current scene to Cinemachine 3",
                "This operation will not upgrade prefab instances or touch any timeline assets, "
                + "which can result in an incomplete upgrade.  To do a complete upgrade,  "
                + "you must choose the \"Upgrade Project\" option.\n\n"
                + "Upgrade scene?",
                "Upgrade", "Cancel"))
            {
                try 
                {
                    Thread.Sleep(1); // this is needed so the Display Dialog closes, and lets the progress bar open
                    EditorUtility.DisplayProgressBar(k_ProgressBarTitle, "Initializing...", 0);
                    var manager = new CinemachineUpgradeManager(false);
                    var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                    var rootObjects = scene.GetRootGameObjects();
                    var upgradable = manager.GetUpgradables(
                        rootObjects, manager.m_ObjectUpgrader.RootUpgradeComponentTypes, true);
                    var upgradedObjects = new HashSet<GameObject>();
                    EditorUtility.DisplayProgressBar(k_ProgressBarTitle, "Upgrading Scene...", 0.5f);
                    manager.UpgradeNonPrefabs(upgradable, upgradedObjects, null);
                    UpgradeObjectReferences(rootObjects);

                    EditorUtility.DisplayProgressBar(k_ProgressBarTitle, "Cleaning up...", 1f);
                    foreach (var go in upgradedObjects)
                        manager.m_ObjectUpgrader.DeleteObsoleteComponents(go);
                }
                catch (Exception e)
                {
                    Debug.LogError(e.Message);
                    OnUnsuccessfulUpgrade();
                }
                EditorUtility.ClearProgressBar();
            }
        }

        /// <summary>
        /// Upgrades all objects in all scenes and prefabs
        /// </summary>
        public static void UpgradeProject()
        {
            if (EditorUtility.DisplayDialog(
                "Upgrade Project to Cinemachine 3",
                "This project contains objects created with Cinemachine 2, "
                + "which can be upgraded to Cinemachine 3 equivalents.  "
                + "This can mostly be done automatically, but it is possible that "
                + "some objects might not be fully converted.\n\n"
                + "Any custom scripts in your project that reference the Cinemachine API will not be "
                + "automatically upgraded, and you may have to alter them manually.  "
                + "Please see the upgrade guide in the user manual.\n\n"
                + "NOTE: Undo is not supported for this operation.  You are strongly "
                + "advised to make a full backup of the project before proceeding.\n\n"
                + "If you prefer, you can cancel this operation and use the package manager to revert "
                + "Cinemachine to a 2.x version, which will continue to work as before.\n\n"
                + "Upgrade project?",
                "I made a backup, go ahead", "Cancel"))
            {
                var originalScenePath = EditorSceneManager.GetActiveScene().path;
                try 
                {
                    Thread.Sleep(1); // this is needed so the Display Dialog closes, and lets the progress bar open
                    EditorUtility.DisplayProgressBar(k_ProgressBarTitle, "Initializing...", 0.1f);
                    var manager = new CinemachineUpgradeManager(true);
                    manager.PrepareUpgrades(out var conversionLinksPerScene, out var timelineRenames);
                    EditorUtility.DisplayProgressBar(k_ProgressBarTitle, "Upgrading Prefabs...", 0.4f);
                    manager.UpgradePrefabAssets(true);
                    EditorUtility.DisplayProgressBar(k_ProgressBarTitle, "Upgrading Prefabs...", 0.5f);
                    manager.UpgradeReferencablePrefabInstances(conversionLinksPerScene);
                    EditorUtility.DisplayProgressBar(k_ProgressBarTitle, "Upgrading Prefabs...", 0.6f);
                    manager.UpgradePrefabAssets(false);
                    EditorUtility.DisplayProgressBar(k_ProgressBarTitle, "Upgrading Scenes...", 0.8f);
                    manager.UpgradeRemaining(conversionLinksPerScene, timelineRenames);
                    EditorUtility.DisplayProgressBar(k_ProgressBarTitle, "Cleaning up...", 1);
                    manager.CleanupPrefabAssets();
                }
                catch (Exception e)
                {
                    Debug.LogError(e.Message);
                    EditorUtility.ClearProgressBar();
                    OnUnsuccessfulUpgrade();
                    return;
                }
                EditorUtility.ClearProgressBar();
                EditorSceneManager.OpenScene(originalScenePath); // re-open scene where the user was before upgrading
            }
        }

        static void OnUnsuccessfulUpgrade()
        {
            EditorUtility.DisplayDialog(
                "Cinemachine Upgrader", 
                "The upgrade was unsuccessful, and your project may be corrupted. It would be wise to restore the backup.\n\n"
                + "Please see the console messages for details.", 
                "OK");
        }

        /// <summary>Returns true if any of the objects are prefab instances or prefabs.</summary>
        /// <param name="objects"></param>
        /// <returns></returns>
        public static bool ObjectsUsePrefabs(UnityEngine.Object[] objects)
        {
            for (int i = 0; i < objects.Length; ++i)
            {
                var go = objects[i] as GameObject;
                if (go == null)
                {
                    var b = objects[i] as MonoBehaviour;
                    if (b != null)
                        go = b.gameObject;
                }
                if (go != null && PrefabUtility.IsPartOfPrefabInstance(go) || PrefabUtility.IsPartOfPrefabAsset(go))
                    return true;
            }
            return false;
        }

        /// <summary>Returns true if any of the objects are prefab instances or prefabs.</summary>
        /// <param name="objects"></param>
        /// <returns></returns>
        public static bool CurrentSceneUsesPrefabs()
        {
            var manager = new CinemachineUpgradeManager(false);
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var rootObjects = scene.GetRootGameObjects();
            var upgradable = manager.GetUpgradables(
                rootObjects, manager.m_ObjectUpgrader.RootUpgradeComponentTypes, true).ToArray();
            return ObjectsUsePrefabs(upgradable);
        }

        
        /// <summary>
        /// For each scene:
        /// - Make names of timeline object unique - for unique referencing later on
        /// - Copy prefab instances (and upgrade the copy), build conversion link and collect timeline references
        /// </summary>
        /// <param name="conversionLinksPerScene">Key: scene index, Value: List of conversion links</param>
        /// <param name="renameMap">Timeline rename mapping</param>
        void PrepareUpgrades(
            out Dictionary<int, List<ConversionLink>> conversionLinksPerScene, 
            out Dictionary<string, string> renameMap)
        {
            conversionLinksPerScene = new ();
            renameMap = new ();

            for (var s = 0; s < m_SceneManager.SceneCount; ++s)
            {
                var scene = OpenScene(s);
                if (!scene.isLoaded)
                    continue;

                // Make timeline names unique
                var timelineManager = new TimelineManager(scene);
#if CINEMACHINE_TIMELINE
                foreach (var director in timelineManager.PlayableDirectors)
                {
                    var originalName = director.name;
                    if (!originalName.StartsWith(k_RenamePrefix))
                    {
                        director.name = k_RenamePrefix + originalName + " = " + GUID.Generate().ToString();
                        renameMap.Add(director.name, originalName); // key = guid, value = originalName
                    }
                }
#endif

                // CopyPrefabInstances, give unique names, create conversion links, collect timeline references
                // Upgrade prefab instance copies of referencables only
                var conversionLinks = new List<ConversionLink>();
                var allPrefabInstances = new List<GameObject>();

                for (var p = 0; p < m_PrefabManager.PrefabCount; ++p)
                {
                    allPrefabInstances.AddRange(
                        PrefabManager.FindAllInstancesOfPrefabEvenInNestedPrefabs(scene,
                            m_PrefabManager.GetPrefabAssetPath(p)));
                }

                var upgradedObjects = new HashSet<GameObject>();
                var upgradables = GetUpgradables(allPrefabInstances.ToArray(), m_ObjectUpgrader.RootUpgradeComponentTypes, true);
                foreach (var go in upgradables)
                {
                    if (upgradedObjects.Contains(go))
                        continue; // Ignore if already converted (this can happen in nested prefabs)
                    upgradedObjects.Add(go);
#if CINEMACHINE_TIMELINE
                    var originalVcam = go.GetComponent<CinemachineVirtualCameraBase>();
                    var timelineReferences = timelineManager.GetTimelineReferences(originalVcam);
#endif
                    var convertedCopy = UnityEngine.Object.Instantiate(go);
                    UpgradeObjectComponents(convertedCopy, null);

                    // Change the object name to something unique so we can find it later
                    var conversionLink = new ConversionLink
                    {
                        originalName = go.name,
                        originalGUIDName = GUID.Generate().ToString(),
                        convertedGUIDName = k_CopyPrefix + GUID.Generate().ToString(),
#if CINEMACHINE_TIMELINE
                        timelineReferences = timelineReferences,
#endif
                    };
                    go.name = conversionLink.originalGUIDName;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(go); // record name change

                    convertedCopy.name = conversionLink.convertedGUIDName;
                    conversionLinks.Add(conversionLink);
                }
                conversionLinksPerScene.Add(s, conversionLinks);

                EditorSceneManager.SaveScene(scene);
            }
            m_CurrentSceneOrPrefab = string.Empty;
        }
        
        /// <summary>
        /// For each prefab asset that has any component from filter
        /// - Upgrade the prefab asset, but do not delete obsolete components
        /// - Fix timeline references
        /// - Fix object references
        /// - Fix animation references
        /// </summary>
        /// <param name="upgradeReferencables">
        /// True, then only referencable prefab instances are upgraded.
        /// False, then only non-referencable prefab instances are upgraded.</param>
        void UpgradePrefabAssets(bool upgradeReferencables)
        {
            for (var p = 0; p < m_PrefabManager.PrefabCount; ++p)
            {
                m_CurrentSceneOrPrefab = m_PrefabManager.GetPrefabAssetPath(p);
#if DEBUG_HELPERS
                Debug.Log("Upgrading prefab asset: " + m_CurrentSceneOrPrefab);
#endif
                try
                {
                    using (var editingScope = new PrefabUtility.EditPrefabContentsScope(m_CurrentSceneOrPrefab))
                    {
                        var prefabContents = editingScope.prefabContentsRoot;

                        // Destroy any lingering invisible CM pipeline objects with invalid scripts, or prefab won't save
                        var removed = RemoveInvalidComponents(prefabContents.transform);
                        if (removed > 0)
                            EditorUtility.SetDirty(prefabContents);

                        if (upgradeReferencables ^ UpgradeObjectToCm3.HasReferencableComponent(prefabContents))
                           continue; 

                        var timelineManager = new TimelineManager(prefabContents, m_CurrentSceneOrPrefab);

                        // Note: this logic relies on the fact FreeLooks will be added first in the component list
                        var components = new List<Component>();
                        foreach (var type in m_ObjectUpgrader.RootUpgradeComponentTypes)
                            components.AddRange(prefabContents.GetComponentsInChildren(type, true).ToList());
                
                        // upgrade all
                        foreach (var c in components)
                        {
                            if (c == null || c.gameObject == null)
                                continue; // was a hidden rig
                            if (c.GetComponentInParent<CinemachineDoNotUpgrade>(true) != null)
                                continue; // is a backup copy

                            // Upgrade prefab and fix timeline references
                            UpgradeObjectComponents(c.gameObject, timelineManager);
                        }

                        // Fix object references
                        UpgradeObjectReferences(new[] { editingScope.prefabContentsRoot });

#if CINEMACHINE_TIMELINE
                        // Fix animation references
                        foreach (var playableDirector in timelineManager.PlayableDirectors)
                            UpdateAnimationBindings(playableDirector);
#endif
                    }  // Prefabs are automatically saved when exiting the scope
                }
                catch (Exception e) 
                {
                    Debug.LogAssertion("CinemachineUpgradeManager: Failed to open " + m_CurrentSceneOrPrefab + ": " + e.Message);
                    continue;
                }
            }
            m_CurrentSceneOrPrefab = string.Empty;

            static int RemoveInvalidComponents(Transform root)
            {
                int count = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(root.gameObject);
                for (int i = 0; i < root.childCount; ++i)
                    count += RemoveInvalidComponents(root.GetChild(i));
                return count;
            }
        }

        /// <summary>
        /// For each scene:
        /// - Upgrade referencable prefab instances without deleting obsolete components,
        /// - Sync referencable prefab instances with their linked copies (to regain lost prefab instance modifications)
        /// - Delete referencable prefab instance copies
        /// - Update timeline references
        /// </summary>
        /// <param name="conversionLinksPerScene">Key: scene index, Value: List of conversion links</param>
        void UpgradeReferencablePrefabInstances(Dictionary<int, List<ConversionLink>> conversionLinksPerScene)
        {
            for (var s = 0; s < m_SceneManager.SceneCount; ++s)
            {
                var scene = OpenScene(s);
                if (!scene.isLoaded) 
                    continue;
                var timelineManager = new TimelineManager(scene);
                var upgradedObjects = new HashSet<GameObject>();
                UpgradePrefabInstances(upgradedObjects, conversionLinksPerScene[s], timelineManager, true);
                EditorSceneManager.SaveScene(scene);
            }

            m_CurrentSceneOrPrefab = string.Empty;
        }
        
        /// <summary>
        /// For each scene:
        /// - Upgrade non-referencable prefab instances without deleting obsolete components, sync with their
        /// linked copies (to regain lost prefab instance modifications), delete copies, and update timeline references
        /// - Upgrade non-prefabs, and update timeline references
        /// - Fix animation references
        /// - Fix object references
        /// - Delete obsolete components
        /// - Restore timeline names to their original names
        /// </summary>
        /// <param name="conversionLinksPerScene">Key: scene index, Value: List of conversion links</param>
        /// <param name="timelineRenames">Timeline rename mapping</param>
        void UpgradeRemaining(
            Dictionary<int, List<ConversionLink>> conversionLinksPerScene,
            Dictionary<string, string> timelineRenames)
        {
            for (var s = 0; s < m_SceneManager.SceneCount; ++s)
            {
                var scene = OpenScene(s);
                if (!scene.isLoaded)
                    continue;
                var timelineManager = new TimelineManager(scene);
                var upgradedObjects = new HashSet<GameObject>();
                
                UpgradePrefabInstances(upgradedObjects, conversionLinksPerScene[s], timelineManager, false);
                
                var rootObjects = scene.GetRootGameObjects();
                var upgradables = GetUpgradables(rootObjects, m_ObjectUpgrader.RootUpgradeComponentTypes, true);
                UpgradeNonPrefabs(upgradables, upgradedObjects, timelineManager);
                
                // restore dolly references in prefab instances
                foreach (var go in upgradables) 
                    UpgradeObjectComponents(go, null);

#if CINEMACHINE_TIMELINE
                // Fix animation references
                foreach (var playableDirector in timelineManager.PlayableDirectors)
                    UpdateAnimationBindings(playableDirector);
#endif
                UpgradeObjectReferences(rootObjects);
                
                // Clean up all obsolete components
                foreach (var go in rootObjects)
                    m_ObjectUpgrader.DeleteObsoleteComponents(go);
                
#if CINEMACHINE_TIMELINE
                // Restore timeline names
                foreach (var director in timelineManager.PlayableDirectors)
                {
                    if (timelineRenames.ContainsKey(director.name)) // search based on guid name
                    {
                        director.name = timelineRenames[director.name]; // restore director name
                        if (PrefabUtility.IsPartOfAnyPrefab(director.gameObject)) 
                            PrefabUtility.RecordPrefabInstancePropertyModifications(director);
                    }
                }
#endif
                EditorSceneManager.SaveScene(scene);
            }
            m_CurrentSceneOrPrefab = string.Empty;
        }

        /// <summary>
        /// For each prefab asset
        /// - Delete obsolete components
        /// - Update manager camera caches
        /// </summary>
        void CleanupPrefabAssets()
        {
            for (var p = 0; p < m_PrefabManager.PrefabCount; ++p)
            {
                m_CurrentSceneOrPrefab = m_PrefabManager.GetPrefabAssetPath(p);
                try
                {
                    using var editingScope = new PrefabUtility.EditPrefabContentsScope(m_CurrentSceneOrPrefab);
                    var prefabContents = editingScope.prefabContentsRoot;
                    var components = new List<Component>();
                    foreach (var type in m_ObjectUpgrader.RootUpgradeComponentTypes)
                        components.AddRange(prefabContents.GetComponentsInChildren(type, true).ToList());
                
                    foreach (var c in components)
                    {
                        if (c == null)
                            continue; // ignore
                        if (c.GetComponentInParent<CinemachineDoNotUpgrade>(true) != null)
                            continue; // is a backup copy

                        m_ObjectUpgrader.DeleteObsoleteComponents(c.gameObject);
                    }
                    
                    var managers = prefabContents.GetComponentsInChildren<CinemachineCameraManagerBase>();
                    foreach (var manager in managers)
                    {
                        manager.InvalidateCameraCache();
                        var justToUpdateCache = manager.ChildCameras;
                    }
                }
                catch (Exception e) 
                {
                    Debug.LogAssertion("CinemachineUpgradeManager: Failed to open " + m_CurrentSceneOrPrefab + ": " + e.Message);
                    continue;
                }
            }
            m_CurrentSceneOrPrefab = string.Empty;
        }

        static void UpgradeObjectReferences(GameObject[] rootObjects)
        {
            foreach (var go in rootObjects) 
            {
                if (go == null)
                    continue; // ignore deleted objects (prefab instance copies)
                
                ReflectionHelpers.RecursiveUpdateBehaviourReferences(go, (expectedType, oldValue) =>
                {
                    var newType = UpgradeObjectToCm3.GetBehaviorReferenceUpgradeType(oldValue);
                    if (expectedType.IsAssignableFrom(newType))
                        return oldValue.GetComponent(newType) as MonoBehaviour;
                    return oldValue;
                });
            }
        }

        /// <summary>
        /// Data to link original prefab data to upgraded prefab data for restoring prefab modifications.
        /// timelineReferences are used to restore timelines that referenced vcams that needed to be upgraded
        /// </summary>
        struct ConversionLink
        {
            public string originalName; // not guaranteed to be unique
            public string originalGUIDName; // unique
            public string convertedGUIDName; // unique
            public List<UniqueExposedReference> timelineReferences;
        }

        struct UniqueExposedReference
        {
            public string directorName; // unique GUID based name
            public ExposedReference<CinemachineVirtualCameraBase> exposedReference;
        }

        static List<GameObject> GetAllGameObjects()
        {
            var all = (GameObject[])Resources.FindObjectsOfTypeAll(typeof(GameObject));
            return all.Where(go => !EditorUtility.IsPersistent(go.transform.root.gameObject)
                && (go.hideFlags & (HideFlags.NotEditable | HideFlags.HideAndDontSave)) == 0).ToList();
        }

        /// <summary>
        /// First, restores modifications in all prefab instances in the current scene by copying data from the linked
        /// converted copy of the prefab instance.
        /// Then, restores timeline references to any of these upgraded instances.
        /// </summary>
        /// <param name="conversionLinks">Conversion links for the current scene</param>
        /// <param name="timelineManager">Timeline manager for the current scene</param>
        /// <param name="upgradeReferencables">
        /// True, then only referencable prefab instances are upgraded.
        /// False, then only non-referencable prefab instances are upgraded.</param>
        /// <param name="upgradedObjects">Set of gameObject that have been converted</param>
        void UpgradePrefabInstances(HashSet<GameObject> upgradedObjects, List<ConversionLink> conversionLinks,
            TimelineManager timelineManager, bool upgradeReferencables)
        {
            var allGameObjectsInScene = GetAllGameObjects();

            foreach (var conversionLink in conversionLinks)
            {
                var prefabInstance = Find(conversionLink.originalGUIDName, allGameObjectsInScene);
                if (prefabInstance == null)
                    continue; // it has been upgraded already
                
#if DEBUG_HELPERS
                Debug.Log("Upgrading prefab instance: " + conversionLink.originalName);
#endif
                if (upgradeReferencables ^ UpgradeObjectToCm3.HasReferencableComponent(prefabInstance)) 
                {
    #if DEBUG_HELPERS
                    Debug.Log("SKIPPING because upgradeReferencables=" + upgradeReferencables);
    #endif
                    continue;
                }
                var convertedCopy = Find(conversionLink.convertedGUIDName, allGameObjectsInScene);

                // Prefab instance modification that added an old vcam needs to be upgraded,
                // all other prefab instances were indirectly upgraded when the Prefab Asset was upgraded
                UpgradeObjectComponents(prefabInstance, null);
                SynchronizeComponents(prefabInstance, convertedCopy, m_ObjectUpgrader.ObsoleteComponentTypesToDelete);
#if CINEMACHINE_TIMELINE
                timelineManager.UpdateTimelineReference(prefabInstance.GetComponent<CinemachineCamera>(), conversionLink);
                var playableDirectors = prefabInstance.GetComponentsInChildren<PlayableDirector>(true);
                for (int i = 0; i < playableDirectors.Length; ++i)
                    UpdateAnimationBindings(playableDirectors[i]);
#endif
                // Restore original scene state (prefab instance name, delete converted copies)
                prefabInstance.name = conversionLink.originalName;
                UnityEngine.Object.DestroyImmediate(convertedCopy);
                PrefabUtility.RecordPrefabInstancePropertyModifications(prefabInstance);

                upgradedObjects.Add(prefabInstance);
            }

            // local functions
            static GameObject Find(string name, List<GameObject> gos)
            {
                return gos.FirstOrDefault(go => go != null && go.name.Equals(name));
            }

            static void SynchronizeComponents(GameObject prefabInstance, GameObject convertedCopy, List<Type> noDeleteList)
            {
                // Transfer values from converted to the instance
                var components = convertedCopy.GetComponents<MonoBehaviour>();
                foreach (var c in components)
                {
                    UnityEditorInternal.ComponentUtility.CopyComponent(c);
                    var c2 = prefabInstance.GetComponent(c.GetType());
                    if (c2 == null)
                        UnityEditorInternal.ComponentUtility.PasteComponentAsNew(prefabInstance);
                    else
                        UnityEditorInternal.ComponentUtility.PasteComponentValues(c2);
                }

                // Delete instance components that are no longer applicable
                components = prefabInstance.GetComponents<MonoBehaviour>();
                foreach (var c in components)
                {
                    // We'll delete these later
                    if (noDeleteList.Contains(c.GetType()))
                        continue;
                    if (convertedCopy.GetComponent(c.GetType()) == null)
                        UnityEngine.Object.DestroyImmediate(c);
                }
            }
        }

        /// <summary>
        /// Upgrades all instances that are not part of any prefab and restores timeline references.
        /// </summary>
        /// <param name="gos">GameObjects to upgrade in the current scene</param>
        /// <param name="upgradedObjects">Object upgraded</param>
        /// <param name="timelineManager">Timeline manager for the current scene</param>
        void UpgradeNonPrefabs(
            List<GameObject> gos, HashSet<GameObject> upgradedObjects, TimelineManager timelineManager)
        {
            foreach (var go in gos)
            {
                // Skip prefab instances (they are done separately)
                if (PrefabUtility.GetPrefabInstanceStatus(go) != PrefabInstanceStatus.NotAPrefab)
                    continue;

                // Don't upgrade twice
                if (upgradedObjects.Contains(go))
                    continue;

                upgradedObjects.Add(go);
                UpgradeObjectComponents(go, timelineManager);
            }
        }
        

#if CINEMACHINE_TIMELINE
        void UpdateAnimationBindings(PlayableDirector playableDirector)
        {
            if (playableDirector == null)
                return;

            var playableAsset = playableDirector.playableAsset;
            if (playableAsset is TimelineAsset timelineAsset)
            {
                var tracks = timelineAsset.GetOutputTracks();
                foreach (var track in tracks)
                {
                    if (track is AnimationTrack animationTrack)
                    {
                        var binding = playableDirector.GetGenericBinding(track);
#if DEBUG_HELPERS
                        if (binding == null) 
                        {
                            Debug.Log("Binding is null for " 
                                + GetFullName(playableDirector.gameObject, m_CurrentSceneOrPrefab) + ", track:" + track.name 
                                + ", PlayableAsset=" + playableAsset.name);
                        }
#endif
                        if (binding is Animator trackAnimator)
                        {
                            if (!animationTrack.inClipMode)
                                m_ObjectUpgrader.ProcessAnimationClip(animationTrack.infiniteClip, trackAnimator); //uses recorded clip
                            else
                            {
                                var clips = animationTrack.GetClips();
                                var animationClips = clips
                                    .Select(c => c.asset) //animation clip is stored in the clip's asset
                                    .OfType<AnimationPlayableAsset>() //need to cast to the correct asset type
                                    .Select(asset => asset.clip); //finally we get an animation clip!
                                foreach (var animationClip in animationClips)
                                    m_ObjectUpgrader.ProcessAnimationClip(animationClip, trackAnimator);
                            }
                        }
                    }
                }
            }
        }
#endif

        CinemachineUpgradeManager(bool initPrefabManager)
        {
            m_ObjectUpgrader = new UpgradeObjectToCm3();
            m_SceneManager = new SceneManager();
            if (initPrefabManager) 
                m_PrefabManager = new PrefabManager(m_ObjectUpgrader.RootUpgradeComponentTypes);
        }

        Scene OpenScene(int sceneIndex)
        {
            m_CurrentSceneOrPrefab = m_SceneManager.GetScenePath(sceneIndex);
#if DEBUG_HELPERS
            Debug.Log("Opening scene: " + m_CurrentSceneOrPrefab);
#endif
            try
            {
                return EditorSceneManager.OpenScene(m_CurrentSceneOrPrefab, OpenSceneMode.Single);
            }
            catch (Exception e)
            {
                Debug.LogAssertion("CinemachineUpgradeManager: Failed to open " + m_CurrentSceneOrPrefab + ": " + e.Message);
                return default;
            }
        }

        static string GetFullName(GameObject go, string pathToRoot)
        {
            string path = go.name;
            while (go.transform.parent != null)
            {
                go = go.transform.parent.gameObject;
                path = go.name + "/" + path;
            }
            return pathToRoot + "/" + path;
        }
        
        /// <summary>
        /// First upgrades the input gameObject's components without deleting the obsolete components.
        /// Then upgrades timeline references (if timelineManager is not null) to the upgraded gameObject.
        /// If the object could not be fully upgraded, a pre-upgrade 
        /// copy is created and returned, else returns null.
        /// </summary>
        GameObject UpgradeObjectComponents(GameObject go, TimelineManager timelineManager)
        {
            // Grab the old component to update the CmShot timeline references
            var oldComponent = go.GetComponent<CinemachineVirtualCameraBase>();
            var notUpgradable = m_ObjectUpgrader.UpgradeComponents(go);

#if CINEMACHINE_TIMELINE
            // Patch the timeline shots
            if (timelineManager != null && oldComponent != null)
            {
                var newComponent = go.GetComponent<CinemachineCamera>();
                if (oldComponent != newComponent)
                    timelineManager.UpdateTimelineReference(oldComponent, newComponent);
            }
#endif

            // Report difficult cases
            if (notUpgradable != null)
            {
                notUpgradable.name = go.name + k_UnupgradableTag;
                Debug.LogWarning("Upgrader: " + GetFullName(go, m_CurrentSceneOrPrefab) + " may not have been fully upgraded " +
                    "automatically.  A reference copy of the original was saved to " + notUpgradable.name);
            }

            return notUpgradable;
        }

        /// <summary>
        /// Finds all gameObjects with components <paramref name="componentTypes"/>.
        /// </summary>
        /// <param name="rootObjects">GameObjects to check.</param>
        /// <param name="componentTypes">Find gameObjects that have these components on them or on their children.</param>
        /// <param name="sort">Sort output. First, candidates that may be referenced and then others.</param>
        /// <returns>Sorted list of candidates. First, candidates that may be referenced by others.</returns>
        List<GameObject> GetUpgradables(GameObject[] rootObjects, List<Type> componentTypes, bool sort)
        {
            var components = new List<Component>();
            if (rootObjects != null)
                foreach (var type in componentTypes)
                    foreach (var go in rootObjects)
                        components.AddRange(go.GetComponentsInChildren(type, true).ToList());
                    
            var upgradables = new List<GameObject>();
            foreach (var c in components)
            {
                // Ignore hidden-object freeLook rigs
                if (c == null || IsHiddenFreeLookRig(c))
                    continue;

                // Ignore backup copies that we may have created in a previous upgrade attempt
                if (c.GetComponentInParent<CinemachineDoNotUpgrade>(true) != null)
                    continue;

                upgradables.Add(c.gameObject);
            }

            return upgradables;
        }

        // Hack: ignore nested rigs of a freeLook (GML todo: how to remove this?)
        static bool IsHiddenFreeLookRig(Component c)
        {
            return c is not CinemachineFreeLook && c.GetComponentInParent<CinemachineFreeLook>(true) != null;
        }
        
        class SceneManager
        {
            List<string> m_AllScenePaths = new ();

            public List<string> s_IgnoreList = new() {}; // TODO: expose this to the user so they can ignore scenes they don't want to upgrade

            public int SceneCount => m_AllScenePaths.Count;
            public string GetScenePath(int index) => m_AllScenePaths[index];

            public SceneManager()
            {
                var allSceneGuids = new List<string>();
                allSceneGuids.AddRange(AssetDatabase.FindAssets("t:scene", new[] { "Assets" }));

                for (var i = allSceneGuids.Count - 1; i >= 0; --i)
                {
                    var sceneGuid = allSceneGuids[i];
                    var scenePath = AssetDatabase.GUIDToAssetPath(sceneGuid);
                    var add = true;
                    for (var j = 0; add && j < s_IgnoreList.Count; ++j)
                        if (scenePath.Contains(s_IgnoreList[j]))
                            add = false;
                    if (add)
                        m_AllScenePaths.Add(scenePath);
                }

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
            List<GameObject> m_PrefabAssets = new ();

            public int PrefabCount => m_PrefabAssets.Count;
            public GameObject GetPrefabAsset(int index) => m_PrefabAssets[index];
            public string GetPrefabAssetPath(int index) => AssetDatabase.GetAssetPath(m_PrefabAssets[index]);

            public PrefabManager(List<Type> upgradeComponentTypes)
            {
                // Get all Prefab Assets in the project
                var prefabGuids = AssetDatabase.FindAssets($"t:prefab", new [] { "Assets" });
                var allPrefabs = prefabGuids.Select(
                    g => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g))).ToList();

                // Select Prefab Assets containing any of the upgradeComponentTypes
                foreach (var prefab in allPrefabs)
                {
                    if (prefab.GetComponentInParent<CinemachineDoNotUpgrade>(true) != null)
                        continue;
                    foreach (var type in upgradeComponentTypes)
                    {
                        var c = prefab.GetComponentsInChildren(type, true);
                        if (c != null && c.Length > 0)
                        {
                            m_PrefabAssets.Add(prefab);
                            break;
                        }
                    }
                }

                // Sort by dependency - non-nested prefabs are first, and then nested prefabs
                if (m_PrefabAssets.Count > 1) m_PrefabAssets.Sort((a, b) =>
                {
                    // if they are not part of each other then we use the name to compare just for consistency.
                    return IsXPartOfY(a, b) ? -1 : IsXPartOfY(b, a) ? 1 : a.name.CompareTo(b.name);

                    bool IsXPartOfY(GameObject x, GameObject y)
                    {
                        GameObject prefab;
                        try 
                        {
                            prefab = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(y));
                        }
                        catch (Exception e) 
                        {
                            Debug.LogAssertion("CinemachineUpgradeManager: Failed to load prefab contents for " + y.name + ": " + e.Message);
                            return false;
                        }
                        var components = new List<Component>();
                        foreach (var type in upgradeComponentTypes)
                            components.AddRange(prefab.GetComponentsInChildren(type, true).ToList());

                        var result = false;
                        foreach (var c in components)
                        {
                            if (c == null || IsHiddenFreeLookRig(c))
                                continue;

                            var prefabInstance = c.gameObject;
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
                    
#if DEBUG_HELPERS
                Debug.Log("**** All prefabs ****");
                m_PrefabAssets.ForEach(prefab => Debug.Log(prefab.name));
                Debug.Log("*********************");
#endif
            }

            public static List<GameObject> FindAllInstancesOfPrefabEvenInNestedPrefabs(
                Scene scene, string prefabPath)
            {
                var allInstances = new List<GameObject>();
                foreach (var go in scene.GetRootGameObjects())
                {
                    // Check if prefabInstance is an instance of the input prefab
                    var nearestPrefabRoot = PrefabUtility.GetNearestPrefabInstanceRoot(go);
                    if (nearestPrefabRoot != null)
                    {
                        var nearestPrefabRootPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
                        if (nearestPrefabRootPath.Equals(prefabPath) && !allInstances.Contains(nearestPrefabRoot)) 
                            allInstances.Add(nearestPrefabRoot);
                    }
                }
                return allInstances;
            }
        }

        class TimelineManager
        {
#if !CINEMACHINE_TIMELINE
            public TimelineManager(GameObject root, string pathToRoot) {} 
            public TimelineManager(Scene scene) {} 
#else
            struct Item { public string Name; public List<CinemachineShot> Shots; }
            Dictionary<PlayableDirector, Item> m_ShotsToUpdate;
            List<PlayableDirector> m_PlayableDirectors;
            string m_PathToRootObject;

            public List<PlayableDirector> PlayableDirectors => m_PlayableDirectors;

            public TimelineManager(Scene scene)
            {
                m_PathToRootObject = scene.path;
                m_PlayableDirectors = new List<PlayableDirector>();
                foreach (var go in scene.GetRootGameObjects())
                    m_PlayableDirectors.AddRange(go.GetComponentsInChildren<PlayableDirector>(true).ToList());
                PruneDuplicates();
                CollectShotsToUpdate();
            }

            public TimelineManager(GameObject root, string pathToRoot)
            {
                m_PathToRootObject = pathToRoot;
                m_PlayableDirectors = root.GetComponentsInChildren<PlayableDirector>(true).ToList();
                PruneDuplicates();
                CollectShotsToUpdate();
            }

            void PruneDuplicates()
            {
                for (int i = m_PlayableDirectors.Count - 1; i >= 0; --i)
                    if (m_PlayableDirectors[i].name.StartsWith(k_CopyPrefix))
                        m_PlayableDirectors.RemoveAt(i);
            }

            void CollectShotsToUpdate()
            {
                m_ShotsToUpdate = new ();

                // collect all cmShots that may require a reference update
                foreach (var playableDirector in m_PlayableDirectors)
                {
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
                                        if (!m_ShotsToUpdate.ContainsKey(playableDirector))
                                            m_ShotsToUpdate.Add(playableDirector, new () 
                                                { Name = playableDirector.name, Shots = new () });
                                        m_ShotsToUpdate[playableDirector].Shots.Add(cmShot);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            /// <summary>
            /// Updates timeline reference with the upgraded vcam. This is called after each 
            /// vcam is upgraded, but before the obsolete component is deleted.
            /// </summary>
            public void UpdateTimelineReference(
                CinemachineVirtualCameraBase oldComponent, 
                CinemachineVirtualCameraBase upgraded)
            {
                foreach (var (director, items) in m_ShotsToUpdate)
                {
                    foreach (var cmShot in items.Shots)
                    {
                        var exposedRef = cmShot.VirtualCamera;
                        var vcam = exposedRef.Resolve(director);
                        if (vcam == oldComponent)
                            director.SetReferenceValue(exposedRef.exposedName, upgraded);
                    }
                }
            }
            
            public void UpdateTimelineReference(CinemachineVirtualCameraBase upgraded, ConversionLink link)
            {
                foreach (var (director, items) in m_ShotsToUpdate)
                {
                    if (director == null)
                        continue;
                    var references = link.timelineReferences;
                    foreach (var reference in references)
                    {
                        if (reference.directorName != director.name) 
                        {
#if DEBUG_HELPERS
                            Debug.Log("Skipping reference for " + reference.directorName + " because it is not for " + director.name);
#endif
                            continue; // ignore references that are not for this director
                        }                        
                        foreach (var cmShot in items.Shots)
                        {
                            var exposedRef = cmShot.VirtualCamera;
                            if (exposedRef.exposedName == reference.exposedReference.exposedName) 
                                director.SetReferenceValue(exposedRef.exposedName, upgraded);
                        }
                    }
                }
            }

            public List<UniqueExposedReference> GetTimelineReferences(CinemachineVirtualCameraBase vcam)
            {
                var references = new List<UniqueExposedReference>();
                foreach (var (director, items) in m_ShotsToUpdate)
                {
                    foreach (var cmShot in items.Shots)
                    {
                        var exposedRef = cmShot.VirtualCamera;
                        if (vcam == exposedRef.Resolve(director))
                            references.Add(new UniqueExposedReference
                            {
                                directorName = director.name,
                                exposedReference = exposedRef
                            });
                    }
                }
                return references;
            }
#endif
        }
    }
}
#pragma warning restore CS0618
#endif
