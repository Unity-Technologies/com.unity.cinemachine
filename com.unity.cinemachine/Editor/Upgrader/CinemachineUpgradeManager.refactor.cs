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
        // foreach scene
        // {
        //     open scene x
        //     MakeTimelineNamesUnique x
        //     CopyPrefabInstances(no upgrade), give unique names x
        //     Extra: convert copy
        //     collect timeline references, collect animation references, create conversion links x
        //     Upgrade prefab instance copies of referencables only x
        //     save scene x
        // }
        void Step1(out Dictionary<int, List<ConversionLink>> conversionLinksPerScene, out Dictionary<string, string> renames)
        {
            conversionLinksPerScene = new Dictionary<int, List<ConversionLink>>();
            renames = new Dictionary<string, string>();
            for (var s = 0; s < m_SceneManager.SceneCount; ++s)
            {
                var scene = OpenScene(s);

                // MakeTimelineNamesUnique
                {
#if CINEMACHINE_TIMELINE
                    var directors = TimelineManager.GetPlayableDirectors(scene);
                    foreach (var director in directors)
                    {
                        var originalName = director.name;
                        director.name = GUID.Generate().ToString();
                        renames.Add(director.name, originalName); // key = guid, value = originalName
                    }
#endif
                }
                
                // CopyPrefabInstances(no upgrade), give unique names, create conversion links, collect timeline references
                // Upgrade prefab instance copies of referencables only
                {
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
                }

                EditorSceneManager.SaveScene(scene);
            }

            m_CurrentSceneOrPrefab = string.Empty;
        }

        // foreach referencable prefab asset
        // {
        //     upgrade, do not delete obsolete components x
        //     Fixup animation and timeline references within prefab x
        //     FixupObjectReferences within prefab x
        // }
        void Step2()
        {
            for (var p = 0; p < m_PrefabManager.PrefabCount; ++p)
            {
                m_CurrentSceneOrPrefab = m_PrefabManager.GetPrefabAssetPath(p);
                using (var editingScope = new PrefabUtility.EditPrefabContentsScope(m_CurrentSceneOrPrefab))
                {
                    var prefabContents = editingScope.prefabContentsRoot;
                    if (!UpgradeObjectToCm3.HasReferencableComponent(prefabContents))
                        continue; // ignore not referencable prefab assets
                    
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

        //foreach scene
        //{
        //    open scene x
        //    Upgrade referencable prefabs instances only, sync copies (prefab instance modifications), delete copies, do not delete obsolete components x
        //    extra step: timeline references are updated
        //    save scene x
        //}
        void Step3(Dictionary<int, List<ConversionLink>> conversionLinksPerScene)
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

        // foreach non-referencable prefab asset
        // {
        //     upgrade, do not delete obsolete components x
        //     Fixup animation and timeline references within prefab x
        //     FixupObjectReferences within prefab x
        // }
        void Step4()
        {
            for (var p = 0; p < m_PrefabManager.PrefabCount; ++p)
            {
                m_CurrentSceneOrPrefab = m_PrefabManager.GetPrefabAssetPath(p);
                using (var editingScope = new PrefabUtility.EditPrefabContentsScope(m_CurrentSceneOrPrefab))
                {
                    var prefabContents = editingScope.prefabContentsRoot;
                    if (UpgradeObjectToCm3.HasReferencableComponent(prefabContents))
                        continue; // ignore referencable prefab assets
                    
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

        //foreach scene
        //{
        //    open scene x
        //    Upgrade non-referencable prefab instances only, upgrade and sync copies (prefab instance modifications), delete copies, do not delete obsolete components x
        //    Upgrade non-prefabs x
        //    Fixup timeline references x
        //    Fixup animation references x
        //    FixupObjectReferences x
        //    delete obsolete components x
        //    restore names (using conversionLink.originalName) x (in UpgradePrefabInstances)
        //    restore timeline names x
        //    save scene x
        //}
        void Step5(Dictionary<int, List<ConversionLink>> conversionLinksPerScene, Dictionary<string, string> timelineRenames)
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
                
                // TODO: maybe I should delete after prefabs
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

        // delete obsolete components for prefab assets
        // update caches of manager cameras that are part of any prefab assets
        void Step6()
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
                        var children = manager.ChildCameras; // UpdateCameraCache
                    }
                }
            }
            m_CurrentSceneOrPrefab = string.Empty;
        }
    }
}
