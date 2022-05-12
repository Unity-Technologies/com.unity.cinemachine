#if UNITY_EDITOR
#define DEBUG_HELPERS

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
    public class UpgradeCm2ToCm3
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
                CollectTimelineReferences();
                UpgradePrefabs();
                UpgradeInScenes();
                RestoreTimelineReferences();
            }

            List<KeyValuePair<GameObject, ExposedReference<CinemachineVirtualCameraBase>>> m_TimelineReferences = new();
            void CollectTimelineReferences()
            {
                var allScenesCount = m_SceneManager.sceneCount;
                for (var i = 0; i < allScenesCount; ++i)
                {
                    var scene = m_SceneManager.LoadScene(i);
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
                                            var vcam = cmShot.VirtualCamera.Resolve(playableDirector);
                                            if (vcam == null) continue;
                                            m_TimelineReferences.Add(
                                                new KeyValuePair<GameObject,
                                                    ExposedReference<CinemachineVirtualCameraBase>>(
                                                    vcam.gameObject,
                                                    cmShot.VirtualCamera));
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
        }
    }
}
#endif
