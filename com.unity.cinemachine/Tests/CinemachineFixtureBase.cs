using System.Collections.Generic;
using Cinemachine;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Tests
{
    /// <summary>Base class that handles creation and deletion of GameObjects.</summary>
    public class CinemachineFixtureBase
    {
        readonly List<GameObject> m_GameObjectsToDestroy = new();
        
        [SetUp]
        public virtual void SetUp()
        {
        }
        
        [TearDown]
        public virtual void TearDown()
        {
            foreach (var go in m_GameObjectsToDestroy)
                RuntimeUtility.DestroyObject(go);

            m_GameObjectsToDestroy.Clear();
        }

        /// <summary>
        /// Creates gameObject and keeps track of it, so it is cleaned up properly at [TearDown].
        /// Uses appropriate method depending on being in Editor (playmode, editmode) or in Build.
        /// </summary>
        /// <param name="name">Name of the gameObject.</param>
        /// <param name="components">Components to add to the gameObject.</param>
        /// <returns>GameObject created.</returns>
        protected GameObject CreateGameObjectSafe(string name, params System.Type[] components)
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

        /// <summary>
        /// Creates the specified primitive and keeps track of it, so it is cleaned up properly at [TearDown].
        /// Uses appropriate method depending on being in Editor (playmode, editmode) or in Build.
        /// </summary>
        /// <param name="type">Type of primitive.</param>
        /// <returns>GameObject created.</returns>
        protected virtual GameObject CreatePrimitiveSafe(PrimitiveType type)
        {
#if UNITY_EDITOR
            var go = Application.isPlaying ? GameObject.CreatePrimitive(type) : ObjectFactory.CreatePrimitive(type);
#else
            var go = GameObject.CreatePrimitive(type);
#endif
            m_GameObjectsToDestroy.Add(go);
            return go;
        }
    }
}
