using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace Cinemachine.Utility
{
    /// <summary>An ad-hoc collection of helpers for reflection, used by Cinemachine
    /// or its editor tools in various places</summary>
    [DocumentationSorting(DocumentationSortingAttribute.Level.Undoc)]
    public static class ReflectionHelpers
    {
        /// <summary>Copy the fields from one object to another</summary>
        /// <param name="src">The source object to copy from</param>
        /// <param name="dst">The destination object to copy to</param>
        /// <param name="bindingAttr">The mask to filter the attributes.
        /// Only those fields that get caught in the filter will be copied</param>
        public static void CopyFields(
            System.Object src, System.Object dst,
            System.Reflection.BindingFlags bindingAttr 
                = System.Reflection.BindingFlags.Public 
                | System.Reflection.BindingFlags.NonPublic 
                | System.Reflection.BindingFlags.Instance)
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

#if UNITY_EDITOR
        /// <summary>Search the assembly for all types that match a predicate</summary>
        /// <param name="assembly">The assembly to search</param>
        /// <param name="predicate">The type to look for</param>
        /// <returns>A list of types found in the assembly that inherit from the predicate</returns>
        public static IEnumerable<Type> GetTypesInAssembly(
            Assembly assembly, Predicate<Type> predicate)
        {
            if (assembly == null)
                return null;

            Type[] types = new Type[0];
            try
            {
                types = assembly.GetTypes();
            }
            catch (Exception)
            {
                // Can't load the types in this assembly
            }
            types = (from t in types
                     where t != null && predicate(t)
                     select t).ToArray();
            return types;
        }

        /// <summary>Get a type from a name</summary>
        /// <param name="typeName">The name of the type to search for</param>
        /// <returns>The type matching the name, or null if not found</returns>
        public static Type GetTypeInAllLoadedAssemblies(string typeName)
        {
            foreach (Type type in GetTypesInAllLoadedAssemblies(t => t.Name == typeName))
                return type;
            return null;
        }

        /// <summary>Search all assemblies for all types that match a predicate</summary>
        /// <param name="predicate">The type to look for</param>
        /// <returns>A list of types found in the assembly that inherit from the predicate</returns>
        public static IEnumerable<Type> GetTypesInAllLoadedAssemblies(Predicate<Type> predicate)
        {
            Assembly[] assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            List<Type> foundTypes = new List<Type>(100);
            foreach (Assembly assembly in assemblies)
            {
                foreach (Type foundType in GetTypesInAssembly(assembly, predicate))
                    foundTypes.Add(foundType);
            }
            return foundTypes;
        }

        /// <summary>call GetTypesInAssembly() for all assemblies that match a predicate</summary>
        /// <param name="assemblyPredicate">Which assemblies to search</param>
        /// <param name="predicate">What type to look for</param>
        public static IEnumerable<Type> GetTypesInLoadedAssemblies(
            Predicate<Assembly> assemblyPredicate, Predicate<Type> predicate)
        {
            Assembly[] assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            assemblies = assemblies.Where((Assembly assembly)
                    => { return assemblyPredicate(assembly); }).OrderBy((Assembly ass)
                    => { return ass.FullName; }).ToArray();

            List<Type> foundTypes = new List<Type>(100);
            foreach (Assembly assembly in assemblies)
            {
                foreach (Type foundType in GetTypesInAssembly(assembly, predicate))
                    foundTypes.Add(foundType);
            }

            return foundTypes;
        }

        /// <summary>Is a type defined and visible</summary>
        /// <param name="fullname">Fullly-qualified type name</param>
        /// <returns>true if the type exists</returns>
        public static bool TypeIsDefined(string fullname)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies)
            {
                try 
                {
                    foreach (var type in assembly.GetTypes())
                        if (type.FullName == fullname)
                            return true;
                }
                catch (System.Exception) {} // Just skip uncooperative assemblies
            }
            return false;
        }
#endif

        /// <summary>Cheater extension to access internal field of an object</summary>
        /// <param name="type">The type of the field</param>
        /// <param name="obj">The object to access</param>
        /// <param name="memberName">The string name of the field to access</param>
        /// <returns>The value of the field in the objects</returns>
        public static T AccessInternalField<T>(this Type type, object obj, string memberName)
        {
            if (string.IsNullOrEmpty(memberName) || (type == null))
                return default(T);

            System.Reflection.BindingFlags bindingFlags = System.Reflection.BindingFlags.NonPublic;
            if (obj != null)
                bindingFlags |= System.Reflection.BindingFlags.Instance;
            else
                bindingFlags |= System.Reflection.BindingFlags.Static;

            FieldInfo field = type.GetField(memberName, bindingFlags);
            if ((field != null) && (field.FieldType == typeof(T)))
                return (T)field.GetValue(obj);
            else
                return default(T);
        }

        /// <summary>Get the object owner of a field.  This method processes
        /// the '.' separator to get from the object that owns the compound field
        /// to the object that owns the leaf field</summary>
        /// <param name="path">The name of the field, which may contain '.' separators</param>
        /// <param name="obj">the owner of the compound field</param>
        public static object GetParentObject(string path, object obj)
        {
            var fields = path.Split('.');
            if (fields.Length == 1)
                return obj;

            var info = obj.GetType().GetField(
                    fields[0], System.Reflection.BindingFlags.Public 
                        | System.Reflection.BindingFlags.NonPublic 
                        | System.Reflection.BindingFlags.Instance);
            obj = info.GetValue(obj);

            return GetParentObject(string.Join(".", fields, 1, fields.Length - 1), obj);
        }

        /// <summary>Returns a string path from an expression - mostly used to retrieve serialized properties
        /// without hardcoding the field path. Safer, and allows for proper refactoring.</summary>
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


        class GameObjectFieldScanner
        {
            public OnFieldFoundDelegate OnFieldFound;
            public delegate bool OnFieldFoundDelegate(string fullName, FieldInfo field, ref object owner);

            /// <summary>Which fields will be scanned</summary>
            public BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance;

            public bool ScanFields(string fullName, ref object obj)
            {
                bool doneSomething = false;
                FieldInfo[] fields = obj.GetType().GetFields(bindingFlags);
                if (fields.Length > 0)
                {
                    for (int i = 0; i < fields.Length; ++i)
                    {
                        string name = fullName + "." + fields[i].Name;
                        object fieldValue = fields[i].GetValue(obj);
                        if (fieldValue != null && OnFieldFound != null && OnFieldFound(name, fields[i], ref obj))
                            doneSomething = true;

                        // Check if it's a complex type
                        Type type = fields[i].FieldType;
                        if (fieldValue != null
                            && !type.IsSubclassOf(typeof(Component))
                            && !type.IsSubclassOf(typeof(GameObject)))
                        {
                            if (ScanFields(name, ref fieldValue))
                                doneSomething = true;

                            // Is it an array?
                            if (type.IsArray)
                            {
                                Array array = fieldValue as Array;
                                for (int j = 0; j < array.Length; ++j)
                                {
                                    object element = array.GetValue(j);
                                    if (element != null && ScanFields(fullName + "[" + j + "]", ref element))
                                        doneSomething = true;
                                }
                            }
                        }
                    }
                }
                return doneSomething;
            }

            /// <summary>
            /// Recursively scan the MonoBehaviours of a GameObject.
            /// For each leaf field found, call the OnFieldValue delegate.
            /// </summary>
            public bool ScanFields(GameObject go)
            {
                bool doneSomething = false;
                MonoBehaviour[] components = go.GetComponents<MonoBehaviour>();
                for (int i = 0; i < components.Length; ++i)
                {
                    object c = components[i] as object;
                    if (c != null && ScanFields(c.GetType().Name, ref c))
                        doneSomething = true;
                }
                return doneSomething;
            }
        };

        public static List<string> GetAllFieldOfType(Type type, GameObject go)
        {
            List<string> fields = new List<string>();
            GameObjectFieldScanner scanner = new GameObjectFieldScanner();
            scanner.OnFieldFound = (string fullName, FieldInfo field, ref object owner) =>
                {
                    //Debug.Log(fullName);
                    if (!field.FieldType.IsSubclassOf(type))
                        return false;
                    fields.Add(fullName);
                    return true;
                };

            scanner.ScanFields(go);
            return fields;
        }

        public static FieldInfo FieldFromPath(GameObject go, string path, out object outObj)
        {
            List<string> fields = new List<string>();
            GameObjectFieldScanner scanner = new GameObjectFieldScanner();
            FieldInfo resultFI = null;
            object resultObj = null;
            scanner.OnFieldFound = (string fullName, FieldInfo field, ref object owner) =>
                {
                    if (resultFI != null && fullName == path)
                    {
                        resultFI = field;
                        resultObj = owner;
                        return true;
                    }
                    return false;
                };

            scanner.ScanFields(go);
            outObj = resultObj;
            return resultFI;
        }

        public static T FieldValueFromPath<T>(GameObject go, string path, T defaultValue)
        {
            object owner;
            FieldInfo field = FieldFromPath(go, path, out owner);
            if (field != null)
            {
                object value = field.GetValue(owner);
                if (value != null && value.GetType() == typeof(T))
                    return (T)value;
            }
            return defaultValue;
        }
    }
}
