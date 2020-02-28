using UnityEditor;
using System;
using System.Collections.Generic;
using Cinemachine.Utility;
using System.Linq.Expressions;

namespace Cinemachine.Editor
{
    /// <summary>
    /// A convenience base class for making inspector editors.
    /// </summary>
    /// <typeparam name="T">The class we're editing</typeparam>
    public class BaseEditor<T> : UnityEditor.Editor where T : class
    {
        protected T Target { get { return target as T; } }

        protected SerializedProperty FindAndExcludeProperty<TValue>(Expression<Func<T, TValue>> expr)
        {
            SerializedProperty p = FindProperty(expr);
            ExcludeProperty(p.name);
            return p;
        }

        protected SerializedProperty FindProperty<TValue>(Expression<Func<T, TValue>> expr)
        {
            return serializedObject.FindProperty(FieldPath(expr));
        }

        protected string FieldPath<TValue>(Expression<Func<T, TValue>> expr)
        {
            return ReflectionHelpers.GetFieldPath(expr);
        }

        /// <summary>Obsolete, do not use</summary>
        protected virtual List<string> GetExcludedPropertiesInInspector() { return mExcluded; }

        protected virtual void GetExcludedPropertiesInInspector(List<string> excluded)
        {
            excluded.Add("m_Script");
        }

        protected void ExcludeProperty(string propertyName)
        {
            mExcluded.Add(propertyName);
        }

        protected bool IsPropertyExcluded(string propertyName)
        {
            return mExcluded.Contains(propertyName);
        }

        public override void OnInspectorGUI()
        {
            BeginInspector();
            DrawRemainingPropertiesInInspector();
        }

        List<string> mExcluded = new List<string>();
        protected virtual void BeginInspector()
        {
            serializedObject.Update();
            mExcluded.Clear();
            GetExcludedPropertiesInInspector(mExcluded);
        }

        protected virtual void DrawPropertyInInspector(SerializedProperty p)
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

        protected void DrawRemainingPropertiesInInspector()
        {
            EditorGUI.BeginChangeCheck();
            DrawPropertiesExcluding(serializedObject, mExcluded.ToArray());
            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();
        }
    }
}
