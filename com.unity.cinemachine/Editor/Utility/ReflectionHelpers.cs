using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Unity.Cinemachine
{
    /// <summary>An ad-hoc collection of helpers for reflection, used by Cinemachine
    /// or its editor tools in various places</summary>
    static class ReflectionHelpers
    {
        /// <summary>Copy the fields from one object to another</summary>
        /// <param name="src">The source object to copy from</param>
        /// <param name="dst">The destination object to copy to</param>
        /// <param name="bindingAttr">The mask to filter the attributes.
        /// Only those fields that get caught in the filter will be copied</param>
        public static void CopyFields(
            object src, object dst,
            BindingFlags bindingAttr = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
        {
            if (src != null && dst != null)
            {
                Type type = src.GetType();
                FieldInfo[] fields = type.GetFields(bindingAttr);
                for (int i = 0; i < fields.Length; ++i)
                    if (!fields[i].IsStatic)
                        fields[i].SetValue(dst, fields[i].GetValue(src));
            }
        }

        /// <summary>Search all assemblies for all types that match a predicate</summary>
        /// <param name="type">The type or interface to look for</param>
        /// <param name="predicate">Additional conditions to test</param>
        /// <returns>A list of types found that inherit from the type and satisfy the predicate.</returns>
        public static List<Type> GetTypesDerivedFrom(Type type, Predicate<Type> predicate)
        {
            var list = new List<Type>();
            if (predicate(type))
                list.Add(type);
            var iter = TypeCache.GetTypesDerivedFrom(type).GetEnumerator();
            while (iter.MoveNext())
            {
                var t = iter.Current;
                if (t != null && predicate(t))
                    list.Add(t);
            }
            return list;
        }

        /// <summary>Cheater extension to access internal field of an object</summary>
        /// <typeparam name="T">The field type</typeparam>
        /// <param name="type">The type of the field</param>
        /// <param name="obj">The object to access</param>
        /// <param name="memberName">The string name of the field to access</param>
        /// <returns>The value of the field in the objects</returns>
        public static T AccessInternalField<T>(this Type type, object obj, string memberName)
        {
            if (string.IsNullOrEmpty(memberName) || (type == null))
                return default;

            BindingFlags bindingFlags = BindingFlags.NonPublic;
            if (obj != null)
                bindingFlags |= BindingFlags.Instance;
            else
                bindingFlags |= BindingFlags.Static;

            FieldInfo field = type.GetField(memberName, bindingFlags);
            if ((field != null) && (field.FieldType == typeof(T)))
                return (T)field.GetValue(obj);
            return default;
        }

        /// <summary>Get the object owner of a field.  This method processes
        /// the '.' separator to get from the object that owns the compound field
        /// to the object that owns the leaf field</summary>
        /// <param name="path">The name of the field, which may contain '.' separators</param>
        /// <param name="obj">the owner of the compound field</param>
        /// <returns>The object owner of the field</returns>
        public static object GetParentObject(string path, object obj)
        {
            var fields = path.Split('.');
            if (fields.Length <= 1)
                return obj;

            var type = obj.GetType();
            if (type.IsArray || typeof(IList).IsAssignableFrom(type))
            {
                var elements = fields[1].Split('[');
                if (elements.Length > 1)
                {
                    var index = Int32.Parse(elements[1].Trim(']'));
                    if (type.IsArray)
                    {
                        if (obj is not Array a || a.Length <= index)
                            return null;
                        obj = a.GetValue(index);
                    }
                    else
                    {
                        var list = obj as IList;
                        if (list != null || list.Count <= index)
                            return null;
                        obj = list[index];
                    }
                    if (fields.Length <= 3)
                        return obj;
                }
            }
            else
            {
                var info = type.GetField(fields[0], BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                obj = info.GetValue(obj);
            }
            return GetParentObject(string.Join(".", fields, 1, fields.Length - 1), obj);
        }

        /// <summary>Returns a string path from an expression - mostly used to retrieve serialized properties
        /// without hardcoding the field path. Safer, and allows for proper refactoring.</summary>
        /// <typeparam name="TType">Magic expression</typeparam>
        /// <typeparam name="TValue">Magic expression</typeparam>
        /// <param name="expr">Magic expression</param>
        /// <returns>The string version of the field path</returns>
        public static string GetFieldPath<TType, TValue>(Expression<Func<TType, TValue>> expr)
        {
            MemberExpression me;
            switch (expr.Body.NodeType)
            {
                case ExpressionType.MemberAccess:
                    me = expr.Body as MemberExpression;
                    break;
                default:
                    throw new InvalidOperationException();
            }

            var members = new List<string>();
            while (me != null)
            {
                members.Add(me.Member.Name);
                me = me.Expression as MemberExpression;
            }

            var sb = new StringBuilder();
            for (int i = members.Count - 1; i >= 0; i--)
            {
                sb.Append(members[i]);
                if (i > 0) sb.Append('.');
            }
            return sb.ToString();
        }

        public delegate MonoBehaviour ReferenceUpdater(Type expectedType, MonoBehaviour oldValue);

        /// <summary>
        /// Recursive scan that calls handler for all serializable fields that reference a MonoBehaviour
        /// </summary>
        public static bool RecursiveUpdateBehaviourReferences(GameObject go, ReferenceUpdater updater)
        {
            bool doneSomething = false;
            var components = go.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < components.Length; ++i)
            {
                var c = components[i];
                var obj = c as object;
                if (ScanFields(ref obj, updater))
                {
                    doneSomething = true;
                    if (UnityEditor.PrefabUtility.IsPartOfAnyPrefab(go))
                        UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(c);
                }
            }
            return doneSomething;

            // local function
            static bool ScanFields(ref object obj, ReferenceUpdater updater)
            {
                if (obj == null)
                    return false;

                bool changed = false;

                BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance;
                if (obj is MonoBehaviour)
                    bindingFlags |= BindingFlags.NonPublic; // you can inspect non-public fields if thy have the attribute

                var fields = obj.GetType().GetFields(bindingFlags);
                for (int j = 0; j < fields.Length; ++j)
                {
                    var f = fields[j];

                    if (!f.IsPublic && f.GetCustomAttribute(typeof(SerializeField)) == null)
                        continue;

                    // Process the field
                    var type = f.FieldType;
                    if (typeof(MonoBehaviour).IsAssignableFrom(type))
                    {
                        var fieldValue = f.GetValue(obj);
                        var mb = fieldValue as MonoBehaviour;
                        if (mb != null)
                        {
                            var newValue = updater(type, mb);
                            if (newValue != mb)
                            {
                                changed = true;
                                f.SetValue(obj, newValue);
                            }
                        }
                    }

                    // Handle arrays and nested types
                    else if (type.IsArray)
                    {
                        if (f.GetValue(obj) is Array fieldValue)
                        {
                            for (int i = 0; i < fieldValue.Length; ++i)
                            {
                                var element = fieldValue.GetValue(i);
                                if (ScanFields(ref element, updater))
                                {
                                    fieldValue.SetValue(element, i);
                                    changed = true;
                                }
                            }
                            if (changed)
                                f.SetValue(obj, fieldValue);
                        }
                    }
                    else if (typeof(IList).IsAssignableFrom(type))
                    {
                        if (f.GetValue(obj) is IList fieldValue)
                        {
                            for (int i = 0; i < fieldValue.Count; ++i)
                            {
                                var element = fieldValue[i];
                                if (ScanFields(ref element, updater))
                                {
                                    fieldValue[i] = element;
                                    changed = true;
                                }
                            }
                            if (changed)
                                f.SetValue(obj, fieldValue);
                        }
                    }
                    else
                    {
                        // If the field type has fields of its own, process them
                        var fieldValue = f.GetValue(obj);
                        if (ScanFields(ref fieldValue, updater))
                            changed = true;
                    }
                }
                return changed;
            }
        }
    }
}
