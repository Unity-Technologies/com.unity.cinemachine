using System.Collections.Generic;
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
#if UNITY_EDITOR
                Undo.DestroyObjectImmediate(go);
#else
                Object.Destroy(go);
#endif

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
#if UNITY_EDITOR
            var go = ObjectFactory.CreateGameObject(name);
#else
            var go = new GameObject(name);
#endif
            m_GameObjectsToDestroy.Add(go);
        
            foreach(var c in components)
                if (c.IsSubclassOf(typeof(Component)))
#if UNITY_EDITOR
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
#if UNITY_EDITOR
            var go = ObjectFactory.CreatePrimitive(type);
#else
            var go = GameObject.CreatePrimitive(type);
#endif
            m_GameObjectsToDestroy.Add(go);
            return go;
        }
    }
}
