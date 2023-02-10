using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace Cinemachine.Editor
{
    /// <summary>
    /// Base class for virtual camera editors.
    /// Handles drawing the header and the basic properties.
    /// </summary>
    /// <typeparam name="T">The type of CinemachineVirtualCameraBase being edited</typeparam>
    [Obsolete]
    class CinemachineLegacyVcamBaseEditor<T> : CinemachineVirtualCameraBaseEditor<T> where T : CinemachineVirtualCameraBase
    {
        List<string> m_ExcludedProperties = new List<string>();
        protected virtual void GetExcludedPropertiesInInspector(List<string> excluded) => excluded.Add("m_Script");
        protected void ExcludeProperty(string propertyName) => m_ExcludedProperties.Add(propertyName);
        protected bool IsPropertyExcluded(string propertyName) => m_ExcludedProperties.Contains(propertyName);

        /// <summary>
        /// Clients should call this at the start of OnInspectorGUI.  
        /// Updates the serialized object and Sets up for excluded properties.
        /// </summary>
        protected virtual void BeginInspector()
        {
            serializedObject.Update();
            m_ExcludedProperties.Clear();
            GetExcludedPropertiesInInspector(m_ExcludedProperties);
        }

        /// <summary>
        /// Draw a property in the inspector, if it is not excluded.  
        /// Property is marked as drawn, so will not be drawn again 
        /// by DrawRemainingPropertiesInInspector()
        /// </summary>
        /// <param name="p">The property to draw</param>
        protected virtual void DrawNonExcludedPropertyInInspector(SerializedProperty p)
        {
            if (!IsPropertyExcluded(p.name))
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(p);
                if (EditorGUI.EndChangeCheck())
                    serializedObject.ApplyModifiedProperties();
                ExcludeProperty(p.name);
            }
        }

        /// <summary>
        /// Draw all remaining unexcluded undrawn properties in the inspector.
        /// </summary>
        protected void DrawRemainingPropertiesInInspector()
        {
            EditorGUI.BeginChangeCheck();
            DrawPropertiesExcluding(serializedObject, m_ExcludedProperties.ToArray());
            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Draw the LookAt and Follow targets in the inspector
        /// </summary>
        /// <param name="followTarget">Follow target SerializedProperty</param>
        /// <param name="lookAtTarget">LookAt target SerializedProperty</param>
        protected void DrawNonExcludedTargetsInInspector(
            SerializedProperty followTarget, SerializedProperty lookAtTarget)
        {
            EditorGUI.BeginChangeCheck();
            if (!IsPropertyExcluded(followTarget.name))
            {
                if (Target.ParentCamera == null || Target.ParentCamera.Follow == null)
                    EditorGUILayout.PropertyField(followTarget);
                else
                    EditorGUILayout.PropertyField(followTarget,
                        new GUIContent(followTarget.displayName + " Override"));
                ExcludeProperty(followTarget.name);
            }
            if (!IsPropertyExcluded(lookAtTarget.name))
            {
                if (Target.ParentCamera == null || Target.ParentCamera.LookAt == null)
                    EditorGUILayout.PropertyField(lookAtTarget);
                else
                    EditorGUILayout.PropertyField(lookAtTarget,
                        new GUIContent(lookAtTarget.displayName + " Override"));
                ExcludeProperty(lookAtTarget.name);
            }
            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();
        }
    }
}

