using System.Collections.Generic;
using Cinemachine;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Tests.HDRP
{
     /// <summary>Base class that handles creation and deletion of GameObjects.</summary>
    public class CinemachineHDRPFixtureBase
    {
        List<GameObject> m_GameObjectsToDestroy;
        
        [SetUp]
        public virtual void SetUp()
        {
            Unity.Collections.NativeLeakDetection.Mode = Unity.Collections.NativeLeakDetectionMode.EnabledWithStackTrace;
            m_GameObjectsToDestroy = new List<GameObject>();
        }
        
        [TearDown]
        public virtual void TearDown()
        {
            foreach (var go in m_GameObjectsToDestroy)
                RuntimeUtility.DestroyObject(go);

            m_GameObjectsToDestroy.Clear();
            m_GameObjectsToDestroy = null;
            Unity.Collections.NativeLeakDetection.Mode = Unity.Collections.NativeLeakDetectionMode.Disabled;
        }
        
        /// <summary>
        /// Creates gameObject and keeps track of it, so it is cleaned up properly at [TearDown].
        /// Uses appropriate method depending on whether the test is playing.
        /// </summary>
        /// <param name="name">Name of the gameObject.</param>
        /// <param name="components">Components to add to the gameObject.</param>
        /// <returns>GameObject created.</returns>
        protected GameObject CreateGameObject(string name, params System.Type[] components)
        {
#if UNITY_EDITOR
            var go = Application.isPlaying ? new GameObject(name) : ObjectFactory.CreateGameObject(name);
#else
            var go = new GameObject(name);
#endif
            m_GameObjectsToDestroy.Add(go);
        
            foreach(var c in components)
                if (c.IsSubclassOf(typeof(Component)))
                    AddComponent(go, c);
            
            return go;
        }

        protected Component AddComponent(GameObject go, System.Type c)
        {
#if UNITY_EDITOR
            return Application.isPlaying ? go.AddComponent(c) : Undo.AddComponent(go, c);
#else
            return go.AddComponent(c);
#endif
        }
    }
}
