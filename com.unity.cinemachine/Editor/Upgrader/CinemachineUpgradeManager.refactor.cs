using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using Object = UnityEngine.Object;

namespace Cinemachine.Editor
{
    
    partial class CinemachineUpgradeManager
    {
        /// <summary>
        /// For each scene:
        /// - Make names of timeline object unique - for unique referencing later on
        /// - Copy prefab instances (and upgrade the copy), build conversion link and collect timeline references
        /// </summary>
        /// <param name="conversionLinksPerScene">Key: scene index, Value: List of conversion links</param>
        /// <param name="renameMap">Timeline rename mapping</param>
        void PrepareUpgrades(out Dictionary<int, List<ConversionLink>> conversionLinksPerScene, out Dictionary<string, string> renameMap)
        {
            conversionLinksPerScene = new Dictionary<int, List<ConversionLink>>();
            renameMap = new Dictionary<string, string>();
            for (var s = 0; s < m_SceneManager.SceneCount; ++s)
            {
                var scene = OpenScene(s);

                // Make timeline names unique
#if CINEMACHINE_TIMELINE
                var directors = TimelineManager.GetPlayableDirectors(scene);
                foreach (var director in directors)
                {
                    var originalName = director.name;
                    director.name = GUID.Generate().ToString();
                    renameMap.Add(director.name, originalName); // key = guid, value = originalName
                }
#endif

                // CopyPrefabInstances, give unique names, create conversion links, collect timeline references
                // Upgrade prefab instance copies of referencables only
                var conversionLinks = new List<ConversionLink>();
                var timelineManager = new TimelineManager(scene);
                var allPrefabInstances = new List<GameObject>();

                for (var p = 0; p < m_PrefabManager.PrefabCount; ++p)
                {
                    allPrefabInstances.AddRange(
                        PrefabManager.FindAllInstancesOfPrefabEvenInNestedPrefabs(scene,
                            m_PrefabManager.GetPrefabAssetPath(p)));
                }

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
                    var convertedCopy = Object.Instantiate(go);
                    UpgradeObjectComponents(convertedCopy, null);
                    m_ObjectUpgrader.DeleteObsoleteComponents(convertedCopy);

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
                using (var editingScope = new PrefabUtility.EditPrefabContentsScope(m_CurrentSceneOrPrefab))
                {
                    var prefabContents = editingScope.prefabContentsRoot;
                    var hasReferencable = UpgradeObjectToCm3.HasReferencableComponent(prefabContents);
                    if ((!upgradeReferencables || !hasReferencable) && (upgradeReferencables || hasReferencable))
                        continue;
                    
#if CINEMACHINE_TIMELINE
                    var playableDirectors = prefabContents.GetComponentsInChildren<PlayableDirector>(true).ToList();
                    var timelineManager = new TimelineManager(playableDirectors);
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

                        // Upgrade prefab and fix timeline references
                        UpgradeObjectComponents(c.gameObject, timelineManager);
                    }

                    // Fix object references
                    UpgradeObjectReferences(new[] { editingScope.prefabContentsRoot });
#if CINEMACHINE_TIMELINE
                    // Fix animation references
                    UpdateAnimationReferences(playableDirectors);
#endif
                }
            }
            m_CurrentSceneOrPrefab = string.Empty;
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
                var timelineManager = new TimelineManager(scene);
                var upgradedObjects = new HashSet<GameObject>();
                
                UpgradePrefabInstances(upgradedObjects, conversionLinksPerScene[s], timelineManager, false);
                
                var rootObjects = scene.GetRootGameObjects();
                var upgradables = GetUpgradables(rootObjects, m_ObjectUpgrader.RootUpgradeComponentTypes, true);
                UpgradeNonPrefabs(upgradables, upgradedObjects, timelineManager);
                
#if CINEMACHINE_TIMELINE
                var playableDirectors = TimelineManager.GetPlayableDirectors(scene);
                UpdateAnimationReferences(playableDirectors);
#endif
                UpgradeObjectReferences(rootObjects);
                
                // Clean up all obsolete components
                foreach (var go in rootObjects)
                    m_ObjectUpgrader.DeleteObsoleteComponents(go);
                
#if CINEMACHINE_TIMELINE
                // Restore timeline names
                foreach (var director in playableDirectors)
                {
                    if (timelineRenames.ContainsKey(director.name)) // search based on guid name
                    {
                        director.name = timelineRenames[director.name]; // restore director name
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
                using (var editingScope = new PrefabUtility.EditPrefabContentsScope(m_CurrentSceneOrPrefab))
                {
                    var prefabContents = editingScope.prefabContentsRoot;
                    var components = new List<Component>();
                    foreach (var type in m_ObjectUpgrader.RootUpgradeComponentTypes)
                        components.AddRange(prefabContents.GetComponentsInChildren(type, true).ToList());
                    foreach (var c in components)
                    {
                        if (c == null || c.gameObject == null)
                            continue; // was a hidden rig
                        if (c.GetComponentInParent<CinemachineDoNotUpgrade>(true) != null)
                            continue; // is a backup copy

                        m_ObjectUpgrader.DeleteObsoleteComponents(c.gameObject);
                    }
                    
                    var managers =
                        prefabContents.GetComponentsInChildren<CinemachineCameraManagerBase>();
                    foreach (var manager in managers)
                    {
                        manager.InvalidateCameraCache();
                        var justToUpdateCache = manager.ChildCameras;
                    }
                }
            }
            m_CurrentSceneOrPrefab = string.Empty;
        }
    }
}
