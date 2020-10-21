#if !UNITY_2019_3_OR_NEWER
#define CINEMACHINE_PHYSICS
#define CINEMACHINE_PHYSICS_2D
#endif

using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace Cinemachine.Editor
{
#if CINEMACHINE_PHYSICS || CINEMACHINE_PHYSICS_2D
    [CustomEditor(typeof(CinemachineConfiner2D))]
    [CanEditMultipleObjects]
    internal sealed class CinemachineConfiner2DEditor : BaseEditor<CinemachineConfiner2D>
    {
        private CinemachineConfiner2D m_target;
        void OnEnable()
        {
            m_target = (CinemachineConfiner2D) target;
        }
        
        public override void OnInspectorGUI()
        {
            DrawRemainingPropertiesInInspector();
            if (GUILayout.Button("InvalidateCache"))
            {
                m_target.InvalidatePathCache();
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}
