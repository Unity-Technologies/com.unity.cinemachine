#if CINEMACHINE_UNITY_SPLINES
using System;
using UnityEditor;
using UnityEngine;
namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineSplineDolly))]
    [CanEditMultipleObjects]
    sealed class CinemachineSplineDollyEditor : BaseEditor<CinemachineSplineDolly>
    {
        public override void OnInspectorGUI()
        {
            BeginInspector();
            bool needWarning = false;
            for (int i = 0; !needWarning && i < targets.Length; ++i)
                needWarning = (targets[i] as CinemachineSplineDolly).m_Spline == null;
            if (needWarning)
                EditorGUILayout.HelpBox("A Path is required", MessageType.Warning);

            needWarning = false;
            for (int i = 0; !needWarning && i < targets.Length; ++i)
                needWarning = (targets[i] as CinemachineSplineDolly).m_AutoDolly.m_Enabled 
                    && (targets[i] as CinemachineSplineDolly).FollowTarget == null;
            if (needWarning)
                EditorGUILayout.HelpBox("AutoDolly requires a Follow Target", MessageType.Warning);

            DrawRemainingPropertiesInInspector();
        }
    }
}
#endif
