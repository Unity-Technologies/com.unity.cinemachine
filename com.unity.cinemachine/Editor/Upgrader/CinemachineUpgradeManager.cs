// #define DEBUG_HELPERS
#pragma warning disable CS0618 // suppress obsolete warnings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cinemachine.Utility;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
#if CINEMACHINE_TIMELINE
using UnityEngine.Timeline;
#endif

namespace Cinemachine.Editor
{
    /// <summary>
    /// Upgrades cm2 to cm3
    /// </summary>
    partial class CinemachineUpgradeManager
    {
        const string k_UnupgradableTag = " BACKUP - not fully upgradable by CM";

        UpgradeObjectToCm3 m_ObjectUpgrader;
        SceneManager m_SceneManager;
        PrefabManager m_PrefabManager;

        // This gets set to help with more informative warning messages about objects
        string m_CurrentSceneOrPrefab;

        /// <summary>
        /// GML Temporary helper method for testing.
        /// Upgrades the input gameObject.  Referenced objects (e.g. paths) may also get upgraded.
        /// Obsolete components are deleted.  Timeline references are not patched.
        /// Undo is supported.
        /// </summary>
        public static void UpgradeSingleObject(GameObject go)
        {
            var objectUpgrader = new UpgradeObjectToCm3();
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

        /// <summary>
        /// GML Temporary helper method for testing.
        /// Upgrades the input gameObject.  Referenced objects (e.g. paths) may also get upgraded.
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
                var manager = new CinemachineUpgradeManager();
                var scene = EditorSceneManager.GetActiveScene();
                var rootObjects = scene.GetRootGameObjects();
                var upgradable = manager.GetUpgradables(
                    rootObjects, manager.m_ObjectUpgrader.RootUpgradeComponentTypes, true);
                var upgradedObjects = new HashSet<GameObject>();
                manager.UpgradeNonPrefabs(upgradable, upgradedObjects, null);
                manager.UpgradeObjectReferences(rootObjects);

                foreach (var go in upgradedObjects)
                    manager.m_ObjectUpgrader.DeleteObsoleteComponents(go);
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
                + "Please see the upgrade guide <here>.\n\n"
                + "NOTE: Undo is not supported for this operation.  You are strongly "
                + "advised to make a full backup of the project before proceeding.\n\n"
                + "If you prefer, you can cancel this operation and use the package manager to revert "
                + "Cinemachine to a 2.x version, which will continue to work as before.\n\n"
                + "Upgrade project?",
                "I made a backup, go ahead", "Cancel"))
            {
                var manager = new CinemachineUpgradeManager();

                manager.Step1(out var conversionLinksPerScene, out var timelineRenames);
                manager.Step2();
                manager.Step3(conversionLinksPerScene);
                manager.Step4();
                manager.Step5(conversionLinksPerScene, timelineRenames);
                manager.Step6();

                // var renames = manager.MakeTimelineNamesUnique();
                // manager.UpgradeNonPrefabReferencables();
                // var conversionLinksPerScene = manager.CopyPrefabInstances();
                // manager.UpgradePrefabAssets();
                // manager.UpgradeAllScenes(conversionLinksPerScene);
                // manager.RestoreTimelineNames(renames);
            }
        }

        void UpgradeObjectReferences(GameObject[] rootObjects)
        {
            var map = m_ObjectUpgrader.ClassUpgradeMap;
            foreach (var go in rootObjects) 
            {
                if (go == null)
                    continue; // ignore deleted objects (prefab instance copies)
                
                ReflectionHelpers.RecursiveUpdateBehaviourReferences(go, (expectedType, oldValue) =>
                {
                    var oldType = oldValue.GetType();
                    if (map.ContainsKey(oldType))
                    {
                        var newType = map[oldType];
                        if (expectedType.IsAssignableFrom(newType))
                            return oldValue.GetComponent(newType) as MonoBehaviour;
                    }
                    return oldValue;
                });
            }
        }
        
        void UpgradeNonPrefabReferencables()
        {
            for (var s = 0; s < m_SceneManager.SceneCount; ++s)
            {
                var scene = OpenScene(s);
                var upgradables = 
                    GetUpgradables(scene.GetRootGameObjects(), UpgradeObjectToCm3.Referencables, false);
                foreach (var upgradable in upgradables)
                {
                    if (PrefabUtility.IsPartOfAnyPrefab(upgradable)) 
                        continue;
                    
                    UpgradeObjectComponents(upgradable, null);
                }
                EditorSceneManager.SaveScene(scene);
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

        /// <summary>
        /// Iterates through all scenes, and copies and upgrades all prefab instances, leaving the original intact.
        /// Creates a link between original and upgraded copy.
        /// This is to capture local prefab override settings before the prefab asset is upgraded, which can cause
        /// the prefab instances to lose their overrides.
        /// </summary>
        /// <returns>
        /// A per scene (scene index) list of links between prefab instances and
        /// their upgraded copies that contain the original prefab modifications.
        /// </returns>
        Dictionary<int, List<ConversionLink>> CopyPrefabInstances()
        {
            var conversionLinksPerScene = new Dictionary<int, List<ConversionLink>>();
            for (var s = 0; s < m_SceneManager.SceneCount; ++s)
            {
                var scene = OpenScene(s);
                var timelineManager = new TimelineManager(scene);

                var conversionLinks = new List<ConversionLink>();
                    
                var allPrefabInstances = new List<GameObject>();
                for (var p = 0; p < m_PrefabManager.PrefabCount; ++p)
                    allPrefabInstances.AddRange(PrefabManager.FindAllInstancesOfPrefabEvenInNestedPrefabs(
                        scene, m_PrefabManager.GetPrefabAssetPath(p)));

                var upgradedObjects = new HashSet<GameObject>();
                var upgradable = 
                    GetUpgradables(allPrefabInstances.ToArray(), m_ObjectUpgrader.RootUpgradeComponentTypes, true);
                foreach (var go in upgradable)
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
                    m_ObjectUpgrader.DeleteObsoleteComponents(convertedCopy);
                    RestoreConvertedCopyReferences(go, convertedCopy, conversionLinks);
                    
                    var conversionLink = new ConversionLink
                    {
                        originalName = go.name,
                        originalGUIDName = GUID.Generate().ToString(),
                        convertedGUIDName = GUID.Generate().ToString(),
#if CINEMACHINE_TIMELINE
                        timelineReferences = timelineReferences,
#endif
                    };
                    go.name = conversionLink.originalGUIDName;
                    convertedCopy.name = conversionLink.convertedGUIDName;
                    conversionLinks.Add(conversionLink);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(go); // record name change
                }
                conversionLinksPerScene.Add(s, conversionLinks);
                // save changes (i.e. converted prefab instances)
                EditorSceneManager.SaveScene(scene); 
            }

            return conversionLinksPerScene;
        }
        
        static List<GameObject> GetAllGameObjects()
        {
            var all = (GameObject[])Resources.FindObjectsOfTypeAll(typeof(GameObject));
            return all.Where(go => !EditorUtility.IsPersistent(go.transform.root.gameObject)
                && (go.hideFlags & (HideFlags.NotEditable | HideFlags.HideAndDontSave)) == 0).ToList();
        }

        /// <summary>
        /// Temporary hack until the reflection based reference updater is ready! TODO: remove!
        /// </summary>
        static void RestoreConvertedCopyReferences(GameObject original, GameObject converted, List<ConversionLink> conversionLinks)
        {
            // TODO: if we'll have more components that can reference upgradables, then we'll need to
            // TODO: create some Data in UpgradeObjectToCM3.Data to make this generic and loopable
            var dolly = original.GetComponentInChildren<CinemachineTrackedDolly>();
            if (dolly != null &&
                converted.TryGetComponent(out CinemachineSplineDolly splineDolly) &&
                dolly.m_Path != null)
            {
                var gameObjects = GetAllGameObjects();
                foreach (var link in conversionLinks)
                {
                    if (link.originalGUIDName == dolly.m_Path.gameObject.name)
                    {
                        var target = Find(link.convertedGUIDName, gameObjects);
                        target.TryGetComponent(out splineDolly.Spline);
                    }
                }
            }

            // local function
            static GameObject Find(string name, List<GameObject> gos)
            {
                return gos.FirstOrDefault(go => go != null && go.name.Equals(name));
            }
        }
        
        /// <summary>Upgrades all prefab assets.</summary>
        void UpgradePrefabAssets()
        {
            for (var p = 0; p < m_PrefabManager.PrefabCount; ++p)
            {
                m_CurrentSceneOrPrefab = m_PrefabManager.GetPrefabAssetPath(p);
                using (var editingScope = new PrefabUtility.EditPrefabContentsScope(m_CurrentSceneOrPrefab))
                {
                    var prefabContents = editingScope.prefabContentsRoot;
#if CINEMACHINE_TIMELINE
                    var timelineManager = new TimelineManager(prefabContents.GetComponentsInChildren<PlayableDirector>(true).ToList());
#else
                    var timelineManager = new TimelineManager();
#endif
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

                        UpgradeObjectComponents(c.gameObject, timelineManager);
                    }

                    UpgradeObjectReferences(new GameObject[] { editingScope.prefabContentsRoot });

                    // clean-up now, because we needed obsolete components to restore references
                    foreach (var c in components)
                    {
                        if (c == null || c.gameObject == null)
                            continue; // was a hidden rig
                        if (c.GetComponentInParent<CinemachineDoNotUpgrade>(true) != null)
                            continue; // is a backup copy

                        m_ObjectUpgrader.DeleteObsoleteComponents(c.gameObject);
                    }
                }
            }
            
            m_CurrentSceneOrPrefab = string.Empty;
        }

        /// <summary>
        /// Opens every scene in the project and upgrades:
        /// 1. prefab instances using Conversion link,
        /// 2. non-prefabs,
        /// 3. cleans obsolete components,
        /// 4. updates animation references.
        /// </summary>
        /// <param name="conversionLinksPerScene">A per scene (scene index) list of links between prefab instances
        /// and their upgraded copies that contain the original prefab modifications.</param>
        void UpgradeAllScenes(Dictionary<int, List<ConversionLink>> conversionLinksPerScene)
        {
            for (var s = 0; s < m_SceneManager.SceneCount; ++s)
            {
                // 0. Open scene
                var scene = OpenScene(s);
                var rootObjects = scene.GetRootGameObjects();
                var timelineManager = new TimelineManager(scene);
                var upgradables = GetUpgradables(rootObjects, m_ObjectUpgrader.RootUpgradeComponentTypes, true);
                var upgradedObjects = new HashSet<GameObject>();
                
                // 1. Prefab instances
                UpgradePrefabInstances(upgradedObjects, conversionLinksPerScene[s], timelineManager, true);
                UpgradePrefabInstances(upgradedObjects, conversionLinksPerScene[s], timelineManager, false);

                // 2. Non-prefabs
                UpgradeNonPrefabs(upgradables, upgradedObjects, timelineManager);
                
                // 3. Fix up object references
                UpgradeObjectReferences(rootObjects);

                // 4. Clean up obsolete components
                foreach (var go in upgradedObjects)
                    m_ObjectUpgrader.DeleteObsoleteComponents(go);

                // 4. AnimationReferences
#if CINEMACHINE_TIMELINE
                UpdateAnimationReferences(TimelineManager.GetPlayableDirectors(scene));
#endif

                // 5. Save scene
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }

       
        /// <summary>
        /// First, restores modifications in all prefab instances in the current scene by copying data from the linked
        /// converted copy of the prefab instance.
        /// Then, restores timeline references to any of these upgraded instances.
        /// </summary>
        /// <param name="conversionLinks">Conversion links for the current scene</param>
        /// <param name="timelineManager">Timeline manager for the current scene</param>
        void UpgradePrefabInstances(
            HashSet<GameObject> upgradedObjects, 
            List<ConversionLink> conversionLinks, TimelineManager timelineManager, bool upgradeReferencables)
        {
            var allGameObjectsInScene = GetAllGameObjects();

            foreach (var conversionLink in conversionLinks)
            {
                var prefabInstance = Find(conversionLink.originalGUIDName, allGameObjectsInScene);
                var hasReferencable = UpgradeObjectToCm3.HasReferencableComponent(prefabInstance);
                if ((!upgradeReferencables || !hasReferencable) && (upgradeReferencables || hasReferencable))
                    continue;

                var convertedCopy = Find(conversionLink.convertedGUIDName, allGameObjectsInScene);

                // Prefab instance modification that added an old vcam needs to be upgraded,
                // all other prefab instances were indirectly upgraded when the Prefab Asset was upgraded
                UpgradeObjectComponents(prefabInstance, null); 

                // GML todo: do we need to do this recursively for child GameObjects?
                SynchronizeComponents(prefabInstance, convertedCopy, m_ObjectUpgrader.ObsoleteComponentTypesToDelete);
#if CINEMACHINE_TIMELINE
                if (timelineManager != null)
                    timelineManager.UpdateTimelineReference(prefabInstance.GetComponent<CmCamera>(), conversionLink);
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
        /// <param name="gos">Gameobjects to upgrade in the current scene</param>
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
        
        /// <summary>Updates all playable directors' animation track's references in the current scene.</summary>
        /// <param name="playableDirectors">Playable directors in the current scene</param>
        void UpdateAnimationReferences(List<PlayableDirector> playableDirectors)
        {
#if CINEMACHINE_TIMELINE
            foreach (var playableDirector in playableDirectors)
            {
                if (playableDirector == null)
                    continue;

                var playableAsset = playableDirector.playableAsset;
                if (playableAsset is TimelineAsset timelineAsset)
                {
                    var tracks = timelineAsset.GetOutputTracks();
                    foreach (var track in tracks)
                    {
                        if (track is AnimationTrack animationTrack)
                        {
                            var trackAnimator = playableDirector.GetGenericBinding(track) as Animator;
                            if (animationTrack.inClipMode)
                            {
                                var clips = animationTrack.GetClips();
                                var animationClips = clips
                                    .Select(c => c.asset) //animation clip is stored in the clip's asset
                                    .OfType<AnimationPlayableAsset>() //need to cast to the correct asset type
                                    .Select(asset => asset.clip); //finally we get an animation clip!

                                foreach (var animationClip in animationClips)
                                    m_ObjectUpgrader.ProcessAnimationClip(animationClip, trackAnimator);
                            }
                            else //uses recorded clip
                                m_ObjectUpgrader.ProcessAnimationClip(animationTrack.infiniteClip, trackAnimator);
                        }
                    }
                }
            }
#endif
        }
        

        /// <summary>
        /// We need to be able to tell timelines apart, and their name may not be unique. So we make them
        /// unique here, and restore their original name when we are done with RestoreTimelineNames().
        /// </summary>
        /// <returns>Mapping from the GUID name to the original name.</returns>
        Dictionary<string, string> MakeTimelineNamesUnique()
        {
            var renames = new Dictionary<string, string>();
#if CINEMACHINE_TIMELINE
            for (var s = 0; s < m_SceneManager.SceneCount; ++s)
            {
                var scene = OpenScene(s);
                var directors = TimelineManager.GetPlayableDirectors(scene);
                foreach (var director in directors)
                {
                    var originalName = director.name;
                    director.name = GUID.Generate().ToString();
                    renames.Add(director.name, originalName); // key = guid, value = originalName
                }
                EditorSceneManager.SaveScene(scene);
            }
#endif
            return renames;
        }
        
        /// <summary>Restore timelines' names to the original name.</summary>
        /// <param name="renames">Mapping from the GUID name to the original name.</param>
        void RestoreTimelineNames(Dictionary<string, string> renames)
        {
#if CINEMACHINE_TIMELINE
            for (var s = 0; s < m_SceneManager.SceneCount; ++s)
            {
                var scene = OpenScene(s);
                var directors = TimelineManager.GetPlayableDirectors(scene);
                foreach (var director in directors)
                {
                    if (renames.ContainsKey(director.name)) // search based on guid name
                    {
                        director.name = renames[director.name]; // restore director name
                    }
                }
                EditorSceneManager.SaveScene(scene);
            }
#endif
        }

        CinemachineUpgradeManager()
        {
            m_ObjectUpgrader = new UpgradeObjectToCm3();
            m_SceneManager = new SceneManager();
            m_PrefabManager = new PrefabManager(m_ObjectUpgrader.RootUpgradeComponentTypes);
        }

        Scene OpenScene(int sceneIndex)
        {
            var path = m_SceneManager.GetScenePath(sceneIndex);
#if DEBUG_HELPERS
            Debug.Log("Opening scene: " + m_SceneManager.GetScenePath(s));
#endif
            m_CurrentSceneOrPrefab = path;
            return EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        }

        string GetFullName(GameObject go)
        {
            string path = go.name;
            while (go.transform.parent != null)
            {
                go = go.transform.parent.gameObject;
                path = go.name + "/" + path;
            }
            return m_CurrentSceneOrPrefab + "/" + path;
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
                var newComponent = go.GetComponent<CmCamera>();
                if (oldComponent != newComponent)
                    timelineManager.UpdateTimelineReference(oldComponent, newComponent);
            }
#endif

            // Report difficult cases
            if (notUpgradable != null)
            {
                notUpgradable.name = go.name + k_UnupgradableTag;
                Debug.LogWarning("Upgrader: " + GetFullName(go) + " may not have been fully upgraded " +
                    "automatically.  A reference copy of the original was saved to " + notUpgradable.name);
            }

            return notUpgradable;
        }

        /// <summary>
        /// Finds all gameObjects with components <paramref name="rootUpgradeComponentTypes"/>.
        /// </summary>
        /// <param name="rootObjects">GameObjects to check.</param>
        /// <param name="componentTypes">Find gameObjects that have these components on them or on their children.</param>
        /// <param name="sort">Sort output. First, candidates that may be referenced and then others.</param>
        /// <returns>Sorted list of candidates. First, candidates that may be referenced by others.</returns>
        List<GameObject> GetUpgradables(GameObject[] rootObjects, List<Type> componentTypes, bool sort)
        {
            var components = new List<Component>();
            if (rootObjects != null)
                foreach (var go in rootObjects)
                    foreach (var type in componentTypes)
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

            if (sort)
            {
                upgradables.Sort((x, y) => -(HasReferencable(x).CompareTo(HasReferencable(y))));
                
                // local function
                static bool HasReferencable(GameObject go) => 
                    UpgradeObjectToCm3.Referencables.Any(referencable => go.GetComponent(referencable) != null);
            }
            
            return upgradables;
        }

        // Hack: ignore nested rigs of a freeLook (GML todo: how to remove this?)
        static bool IsHiddenFreeLookRig(Component c)
        {
            return !(c is CinemachineFreeLook) && c.GetComponentInParent<CinemachineFreeLook>(true) != null;
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
                        var prefab = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(y));
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
            public TimelineManager() {} 
            public TimelineManager(Scene scene) {} 
#else
            Dictionary<PlayableDirector, List<CinemachineShot>> m_CmShotsToUpdate;

            public TimelineManager(List<PlayableDirector> playableDirectors)
            {
                Initialize(playableDirectors);
            }

            public TimelineManager(Scene scene)
            {
                Initialize(GetPlayableDirectors(scene));
            }

            public static List<PlayableDirector> GetPlayableDirectors(Scene scene)
            {
                var playableDirectors = new List<PlayableDirector>();

                var rootObjects = scene.GetRootGameObjects();
                foreach (var go in rootObjects)
                    playableDirectors.AddRange(go.GetComponentsInChildren<PlayableDirector>(true).ToList());

                return playableDirectors;
            }

            void Initialize(List<PlayableDirector> playableDirectors)
            {
                m_CmShotsToUpdate = new Dictionary<PlayableDirector, List<CinemachineShot>>();

                // collect all cmShots that may require a reference update
                foreach (var playableDirector in playableDirectors)
                {
                    if (playableDirector == null)
                        continue;

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
                                        if (!m_CmShotsToUpdate.ContainsKey(playableDirector))
                                            m_CmShotsToUpdate.Add(playableDirector, new List<CinemachineShot>());
                                        
                                        m_CmShotsToUpdate[playableDirector].Add(cmShot);
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
            public void UpdateTimelineReference(CinemachineVirtualCameraBase oldComponent, 
                CinemachineVirtualCameraBase upgraded)
            {
                foreach (var (director, cmShots) in m_CmShotsToUpdate)
                {
                    foreach (var cmShot in cmShots)
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
                foreach (var (director, cmShots) in m_CmShotsToUpdate)
                {
                    var references = link.timelineReferences;
                    foreach (var reference in references)
                    {
                        if (reference.directorName != director.name) 
                            continue; // ignore references that are not for this director
                        
                        foreach (var cmShot in cmShots)
                        {
                            var exposedRef = cmShot.VirtualCamera;
                            if (exposedRef.exposedName == reference.exposedReference.exposedName)
                            {
                                // update reference if it needs to be updated <=> null
                                if (exposedRef.Resolve(director) == null)
                                    director.SetReferenceValue(exposedRef.exposedName, upgraded);
                            }
                        }
                    }
                }
            }

            public List<UniqueExposedReference> GetTimelineReferences(CinemachineVirtualCameraBase vcam)
            {
                var references = new List<UniqueExposedReference>();
                foreach (var (director, cmShots) in m_CmShotsToUpdate)
                {
                    foreach (var cmShot in cmShots)
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
