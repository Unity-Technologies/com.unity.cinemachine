using System.Collections.Generic;
using Cinemachine;
using NUnit.Framework;
using UnityEngine;

namespace Tests
{
    public class CinemachineFixtureBase
    {
        readonly List<GameObject> m_GameObjectsToDestroy = new();

        /// <summary>
        /// Creates gameObject and keeps track of it, so it is cleaned up properly at [TearDown].
        /// </summary>
        /// <param name="name">Name of the gameObject</param>
        /// <param name="components">Components to add to the gameObject</param>
        /// <returns></returns>
        protected GameObject CreateGameObject(string name, params System.Type[] components)
        {
            var go = new GameObject();
            m_GameObjectsToDestroy.Add(go);
            go.name = name;
        
            foreach(var c in components)
                if (c.IsSubclassOf(typeof(Component)))
                    go.AddComponent(c);
        
            return go;
        }

        /// <summary>
        /// Creates the specified primitive and keeps track of it, so it is cleaned up properly at [TearDown].
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        protected GameObject CreatePrimitive(PrimitiveType type)
        {
            var go = GameObject.CreatePrimitive(type);
            m_GameObjectsToDestroy.Add(go);
            return go;
        }

        [SetUp]
        public virtual void SetUp()
        {
        }
        
        [TearDown]
        public virtual void TearDown()
        {
            foreach (var go in m_GameObjectsToDestroy)
            {
                RuntimeUtility.DestroyObject(go);
            }

            m_GameObjectsToDestroy.Clear();
        }
    }
}
