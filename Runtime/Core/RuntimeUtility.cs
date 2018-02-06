using System;
using UnityEngine;

namespace Cinemachine
{
    /// <summary>An ad-hoc collection of helpers, used by Cinemachine
    /// or its editor tools in various places</summary>
    [DocumentationSorting(DocumentationSortingAttribute.Level.Undoc)]
    public static class RuntimeUtility
    {
        public static void DestroyObject(UnityEngine.Object obj)
        {
            if (obj != null)
            {
#if UNITY_EDITOR
                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(obj);
                else
                    UnityEngine.Object.DestroyImmediate(obj);
#else
                UnityEngine.Object.Destroy(obj);
#endif
            }
        }
    }
}

