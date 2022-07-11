// #define DEBUG_HELPERS
#pragma warning disable CS0618 // suppress obsolete warnings

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;
using Object = UnityEngine.Object;

namespace Cinemachine.Editor
{
    /// <summary>
    /// Upgrades cm2 to cm3
    /// </summary>
    class CinemachineUpgradeManager
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
        /// Obsolete components are deleted.  Timeline referencs are not patched.
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
        /// Upgrades all objects in all scenes and prefabs
        /// </summary>
        public static void UpgradeProject()
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
                var manager = new CinemachineUpgradeManager();

                manager.UpgradeInScenes(); // non-prefabs first
                manager.UpgradePrefabsAndPrefabInstances();
                manager.DeleteObsoleteComponentsInScenes();
            }
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
        /// Upgrades the input gameObject's components without deleting the obsolete components.
        /// If the object could not be fully upgraded, a pre-upgrade 
        /// copy is created and returned, else returns null.
        /// </summary>
        GameObject UpgradeObjectComponents(GameObject go, TimelineManager timelineManager)
        {
            // Grab the old component to update the CmShot timeline references
            var oldComponent = go.GetComponent<CinemachineVirtualCameraBase>();
            var notUpgradable = m_ObjectUpgrader.UpgradeComponents(go);

            // Patch the timeline shots
            if (timelineManager != null && oldComponent != null)
            {
                var newComponent = go.GetComponent<CmCamera>();
                if (oldComponent != newComponent)
                    timelineManager.UpdateTimelineReference(oldComponent, newComponent);
            }

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
        /// Last conversion stage, after all objects have been converted.  Deletes the obsolete components.
        /// </summary>
        void DeleteObsoleteComponentsInScenes()
        {
            for (var s = 0; s < m_SceneManager.SceneCount; ++s)
            {
                var scene = OpenScene(s);
                var upgradable = GetUpgradableObjects(scene.GetRootGameObjects());
                foreach (var go in upgradable)
                    m_ObjectUpgrader.DeleteObsoleteComponents(go);
                if (upgradable.Count > 0)
                    EditorSceneManager.SaveScene(scene); 
            }
        }

        List<GameObject> GetUpgradableObjects(GameObject[] rootObjects)
        {
            var components = new List<Component>();
            if (rootObjects != null)
                foreach (var go in rootObjects)
                    foreach (var type in m_ObjectUpgrader.RootUpgradeComponentTypes)
                        components.AddRange(go.GetComponentsInChildren(type, true).ToList());
                    
            var upgradable = new List<GameObject>();
            foreach (var c in components)
            {
                // Ignore hidden-object freeLook rigs
                if (c == null || IsHiddenFreeLookRig(c))
                    continue;

                // Ignore backup copies that we may have created in a previous upgrade attempt
                if (c.GetComponentInParent<CinemachineDoNotUpgrade>(true) != null)
                    continue;

                upgradable.Add(c.gameObject);
            }
            return upgradable;
        }

        // Hack: ignore nested rigs of a freeLook (GML todo: how to remove this?)
        static bool IsHiddenFreeLookRig(Component c)
        {
            return !(c is CinemachineFreeLook) && c.GetComponentInParent<CinemachineFreeLook>(true) != null;
        }

        void UpgradeInScenes()
        {
            for (var s = 0; s < m_SceneManager.SceneCount; ++s)
            {
                var scene = OpenScene(s);
                var timelineManager = new TimelineManager(scene);
                    
                var upgradedObjects = new HashSet<GameObject>();
                var upgradable = GetUpgradableObjects(scene.GetRootGameObjects());
                foreach (var go in upgradable)
                {
                    // Skip prefab instances (we'll do them later)
                    if (PrefabUtility.GetPrefabInstanceStatus(go) != PrefabInstanceStatus.NotAPrefab)
                        continue;

                    // Don't upgrade twice
                    if (upgradedObjects.Contains(go))
                        continue;

                    upgradedObjects.Add(go);
                    UpgradeObjectComponents(go, timelineManager);
                }

                // Update scene
                if (upgradedObjects.Count > 0)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
            }
        }
        
        /// <summary>
        /// Data to link original prefab data to upgraded prefab data for restoring prefab modifications.
        /// timelineReferences are used to restore timelines that referenced vcams that needed to be upgraded
        /// </summary>
        struct ConversionLink
        {
            public string originalName;
            public string originalGUIDName;
            public string convertedGUIDName;
            public Dictionary<KeyValuePair<string, int>, List<ExposedReference<CinemachineVirtualCameraBase>>> timelineReferences;
        }
        
        void UpgradePrefabsAndPrefabInstances()
        {
            var upgradeComponentTypes = m_ObjectUpgrader.RootUpgradeComponentTypes;

            // First copy all prefab instances and upgrade the copy, leaving the original intact.
            // This is to capture local prefab override settings.
            // We temporarily rename the original with a GUID so we can find it later.
            var conversionLinksPerScene = new Dictionary<string, List<ConversionLink>>();
            for (var s = 0; s < m_SceneManager.SceneCount; ++s)
            {
                var scene = OpenScene(s);
                var timelineManager = new TimelineManager(scene);

                var conversionLinks = new List<ConversionLink>();
                conversionLinksPerScene.Add(m_SceneManager.GetScenePath(s), conversionLinks);
                    
                List<GameObject> allPrefabInstances = new List<GameObject>();
                for (var p = 0; p < m_PrefabManager.PrefabCount; ++p)
                    allPrefabInstances.AddRange(m_PrefabManager.FindAllInstancesOfPrefabEvenInNestedPrefabs(
                        scene, m_PrefabManager.GetPrefabAssetPath(p)));

                var upgradedObjects = new HashSet<GameObject>();
                var upgradable = GetUpgradableObjects(allPrefabInstances.ToArray());
                foreach (var go in upgradable)
                {
                    // Ignore if already converted (this can happen in nested prefabs)
                    if (upgradedObjects.Contains(go))
                        continue; 
                    upgradedObjects.Add(go);
                    
                    var originalVcam = go.GetComponent<CinemachineVirtualCameraBase>();
                    var timelineReferences = timelineManager.GetTimelineReferences(originalVcam);
                    
                    var convertedCopy = Object.Instantiate(go);
                    UpgradeObjectComponents(convertedCopy, null);
                    var conversionLink = new ConversionLink
                    {
                        originalName = go.name,
                        originalGUIDName = GUID.Generate().ToString(),
                        convertedGUIDName = GUID.Generate().ToString(),
                        timelineReferences = timelineReferences,
                    };
                    go.name = conversionLink.originalGUIDName;
                    convertedCopy.name = conversionLink.convertedGUIDName;
                    conversionLinks.Add(conversionLink);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(go); // record name change
                }

                // save changes (i.e. converted prefab instances)
                EditorSceneManager.SaveScene(scene); 
            }

            // Upgrade Prefab Assets
            for (var p = 0; p < m_PrefabManager.PrefabCount; ++p)
            {
                m_CurrentSceneOrPrefab = m_PrefabManager.GetPrefabAssetPath(p);
                using (var editingScope = new PrefabUtility.EditPrefabContentsScope(m_CurrentSceneOrPrefab))
                {
                    var prefabContents = editingScope.prefabContentsRoot;
                    var timelineManager = new TimelineManager(prefabContents.GetComponentsInChildren<PlayableDirector>(true).ToList());
                    // Note: this logic relies on the fact FreeLooks will be added first in the component list
                    var components = new List<Component>();
                    foreach (var type in upgradeComponentTypes)
                        components.AddRange(prefabContents.GetComponentsInChildren(type, true).ToList());
                    foreach (var c in components)
                    {
                        if (c == null || c.gameObject == null)
                            continue; // was a hidden rig
                        if (c.GetComponentInParent<CinemachineDoNotUpgrade>(true) != null)
                            continue; // is a backup copy

                        UpgradeObjectComponents(c.gameObject, timelineManager);
                        m_ObjectUpgrader.DeleteObsoleteComponents(c.gameObject);
                    }
                }
            }
            m_CurrentSceneOrPrefab = string.Empty;

            // In each scene, restore modifications in all prefab instances by copying data
            // from the linked converted copy of the prefab instance.
            
            Debug.Log("Prefab instance modification sync stage");
            for (int s = 0; s < m_SceneManager.SceneCount; ++s)
            {
                var scene = OpenScene(s);
                Debug.Log("scene:"+ s);
                var timelineManager = new TimelineManager(scene);
                var conversionLinks = conversionLinksPerScene[m_SceneManager.GetScenePath(s)];
                var allGameObjectsInScene = GetAllGameObjects();

                foreach (var conversionLink in conversionLinks)
                {
                    foreach (var (director, references) in conversionLink.timelineReferences)
                    {
                        Debug.Log("conversionLink:" +director.Key + "-" + director.Value);
                    }
                    
                    var prefabInstance = Find(conversionLink.originalGUIDName, allGameObjectsInScene);
                    var convertedCopy = Find(conversionLink.convertedGUIDName, allGameObjectsInScene);
                 
                    // GML todo: do we need to do this recursively for child GameObjects?
                    SynchronizeComponents(prefabInstance, convertedCopy, m_ObjectUpgrader.ObsoleteComponentTypesToDelete);
                    var converted = prefabInstance.GetComponent<CmCamera>();
                    timelineManager.UpdateTimelineReference(converted, conversionLink);

                    // Restore original scene state (prefab instance name, delete converted copies)
                    prefabInstance.name = conversionLink.originalName;
                    Object.DestroyImmediate(convertedCopy);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(prefabInstance);
                }
                if (conversionLinks.Count > 0)
                    EditorSceneManager.SaveScene(scene);
            }

            // local functions
            static List<GameObject> GetAllGameObjects()
            {
                var all = (GameObject[])Resources.FindObjectsOfTypeAll(typeof(GameObject));
                return all.Where(go => !EditorUtility.IsPersistent(go.transform.root.gameObject)
                    && (go.hideFlags & (HideFlags.NotEditable | HideFlags.HideAndDontSave)) == 0).ToList();
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
                        Object.DestroyImmediate(c);
                }
            }
        }

        class SceneManager
        {
            List<string> m_AllScenePaths = new ();

            public List<string> s_IgnoreList = new() {}; // TODO: expose this to the user

            public int SceneCount => m_AllScenePaths.Count;
            public string GetScenePath(int index) => m_AllScenePaths[index];

            public SceneManager()
            {
                var allSceneGuids = new List<string>();
                allSceneGuids.AddRange(AssetDatabase.FindAssets("t:scene", new[] { "Assets" }));

                for (int i = allSceneGuids.Count - 1; i >= 0; --i)
                {
                    var sceneGuid = allSceneGuids[i];
                    var scenePath = AssetDatabase.GUIDToAssetPath(sceneGuid);
                    var add = true;
                    for (int j = 0; add && j < s_IgnoreList.Count; ++j)
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

            public List<GameObject> FindAllInstancesOfPrefabEvenInNestedPrefabs(
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
            Dictionary<PlayableDirector, List<CinemachineShot>> m_CmShotsToUpdate;

            public TimelineManager(List<PlayableDirector> playableDirectors)
            {
                Initialize(playableDirectors);
            }
            
            public TimelineManager(Scene scene)
            {
                var playableDirectors = new List<PlayableDirector>();

                var rootObjects = scene.GetRootGameObjects();
                foreach (var go in rootObjects)
                    playableDirectors.AddRange(go.GetComponentsInChildren<PlayableDirector>(true).ToList());

                Initialize(playableDirectors);
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
                                        
                                        // var exposedRef = cmShot.VirtualCamera;
                                        // var vcam = exposedRef.Resolve(playableDirector);
                                        // if (vcam != null)
                                        {
                                            if (!m_CmShotsToUpdate.ContainsKey(playableDirector))
                                                m_CmShotsToUpdate.Add(playableDirector, new List<CinemachineShot>());
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
            /// Updates timeline reference with the upgraded vcam. This is called after each 
            /// vcam is upgraded, but before the obsolete component is deleted.
            /// </summary>
            /// <param name="upgraded"></param>
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
                    var directorId = director.GetInstanceID();
                    var key = new KeyValuePair<string, int>(director.name, directorId);
                    Debug.Log("UpdateTimelineReference:" + key.Key + "-" + key.Value);
                    if (!link.timelineReferences.ContainsKey(key))
                        continue;
                    
                    var references = link.timelineReferences[key];
                    foreach (var cmShot in cmShots)
                    {
                        var exposedRef = cmShot.VirtualCamera;
                        foreach (var reference in references)
                        {
                            if (exposedRef.exposedName == reference.exposedName)
                            {
                                director.SetReferenceValue(exposedRef.exposedName, upgraded);
                            }
                        }
                    }
                }
            }

            public Dictionary<KeyValuePair<string, int>, List<ExposedReference<CinemachineVirtualCameraBase>>> 
                GetTimelineReferences(CinemachineVirtualCameraBase vcam)
            {
                var d = new Dictionary<KeyValuePair<string, int>, List<ExposedReference<CinemachineVirtualCameraBase>>>();
                Debug.Log("GetTimelineReferences:");
                foreach (var (director, cmShots) in m_CmShotsToUpdate)
                {
                    var id = director.GetInstanceID();
                    Debug.Log(director.name + "-" + id);
                    var references = new List<ExposedReference<CinemachineVirtualCameraBase>>();
                    foreach (var cmShot in cmShots)
                    {
                        var exposedRef = cmShot.VirtualCamera;
                        if (vcam == exposedRef.Resolve(director))
                            references.Add(exposedRef);
                    }
                    d.Add(new KeyValuePair<string, int>(director.name, id), references);
                }

                return d;
            }
        }
    }
}
#pragma warning restore CS0618
