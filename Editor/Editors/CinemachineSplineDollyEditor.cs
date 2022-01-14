using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineSplineDolly))]
    [CanEditMultipleObjects]
    internal sealed class CinemachineSplineDollyEditor : BaseEditor<CinemachineSplineDolly>
    {
        /// <summary>Get the property names to exclude in the inspector.</summary>
        /// <param name="excluded">Add the names to this list</param>
        protected override void GetExcludedPropertiesInInspector(List<string> excluded)
        {
            base.GetExcludedPropertiesInInspector(excluded);
        }

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
