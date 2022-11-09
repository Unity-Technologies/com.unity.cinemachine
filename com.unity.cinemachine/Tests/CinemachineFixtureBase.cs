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
        /// </summary>
        /// <param name="name">Name of the gameObject</param>
        /// <param name="components">Components to add to the gameObject</param>
        /// <returns></returns>
        protected GameObject CreateGameObject(string name, params System.Type[] components)
        {
            GameObject go;
#if UNITY_EDITOR
            if (Application.isPlaying)
                go = new GameObject(name);
            else
                go = ObjectFactory.CreateGameObject(name);
#else
            go = new GameObject(name);
#endif
            m_GameObjectsToDestroy.Add(go);
        
            foreach(var c in components)
                if (c.IsSubclassOf(typeof(Component)))
#if UNITY_EDITOR
                    if (Application.isPlaying)
                        go.AddComponent(c);
                    else
                        Undo.AddComponent(go, c);
#else
                    go.AddComponent(c);
#endif
                    
        
            return go;
        }

        /// <summary>
        /// Creates the specified primitive and keeps track of it, so it is cleaned up properly at [TearDown].
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        protected GameObject CreatePrimitive(PrimitiveType type)
        {
            GameObject go;
#if UNITY_EDITOR
            if (Application.isPlaying) 
                go = GameObject.CreatePrimitive(type);
            else
                go = ObjectFactory.CreatePrimitive(type);
#else
            go = GameObject.CreatePrimitive(type);
#endif
            m_GameObjectsToDestroy.Add(go);
            return go;
        }
    }
}
