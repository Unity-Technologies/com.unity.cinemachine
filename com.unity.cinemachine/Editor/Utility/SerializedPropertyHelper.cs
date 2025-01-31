using System;
using System.Linq.Expressions;
using System.Reflection;
using UnityEditor;

namespace Unity.Cinemachine.Editor
{
    /// <summary>
    /// Helpers for the editor relating to SerializedProperties
    /// </summary>
    static class SerializedPropertyHelper
    {
        /// <summary>
        /// This is a way to get a field name string in such a manner that the compiler will
        /// generate errors for invalid fields.  Much better than directly using strings.
        /// Usage: instead of
        /// <code>
        /// "m_MyField";
        /// </code>
        /// do this:
        /// <code>
        /// MyClass myclass = null;
        /// SerializedPropertyHelper.PropertyName( () => myClass.m_MyField);
        /// </code>
        /// </summary>
        /// <param name="exp">Magic expression that resolves to a field: () => myClass.m_MyField</param>
        /// <returns>string name of field</returns>
        public static MemberInfo MemberInfo(Expression<Func<object>> exp)
        {
            if (exp.Body is not MemberExpression body)
            {
                var ubody = (UnaryExpression)exp.Body;
                body = ubody.Operand as MemberExpression;
            }
            return body.Member;
        }

        /// <summary>
        /// This is a way to get a field name string in such a manner that the compiler will
        /// generate errors for invalid fields.  Much better than directly using strings.
        /// Usage: instead of
        /// <code>
        /// "m_MyField";
        /// </code>
        /// do this:
        /// <code>
        /// MyClass myclass = null;
        /// SerializedPropertyHelper.PropertyName( () => myClass.m_MyField);
        /// </code>
        /// </summary>
        /// <param name="exp">Magic expression that resolves to a field: () => myClass.m_MyField</param>
        /// <returns>string name of field</returns>
        public static string PropertyName(Expression<Func<object>> exp) => MemberInfo(exp).Name;

        /// <summary>
        /// This is a way to get a field tooltip string in such a manner that the compiler will
        /// generate errors for invalid fields.  Much better than directly using strings.
        /// </summary>
        /// <param name="exp">Magic expression that resolves to a field: () => myClass.m_MyField</param>
        /// <returns>Tooltip text</returns>
        public static string PropertyTooltip(Expression<Func<object>> exp)
        {
            var attrs = MemberInfo(exp).GetCustomAttributes(typeof(UnityEngine.TooltipAttribute), false);
            return attrs.Length > 0 ? ((UnityEngine.TooltipAttribute)attrs[0]).tooltip : string.Empty;
        }

        /// <summary>
        /// This is a way to get a field name string in such a manner that the compiler will
        /// generate errors for invalid fields.  Much better than directly using strings.
        /// </summary>
        /// <param name="exp">Magic expression that resolves to a field: () => myClass.m_MyField</param>
        /// <returns>Type of field</returns>
        public static Type PropertyType(Expression<Func<object>> exp)
        {
            var member = MemberInfo(exp);
            if (member.MemberType == MemberTypes.Field)
                return ((FieldInfo)member).FieldType;
            throw new ArgumentException ( "Input MemberInfo must be of type FieldInfo" );
        }

        /// <summary>
        /// A compiler-assisted (non-string-based) way to call SerializedProperty.FindProperty
        /// </summary>
        /// <param name="obj">The serialized object to search</param>
        /// <param name="exp">Magic expression that resolves to a field: () => myClass.m_MyField</param>
        /// <returns>The resulting SerializedProperty, or null</returns>
        public static SerializedProperty FindProperty(this SerializedObject obj, Expression<Func<object>> exp)
        {
            return obj.FindProperty(PropertyName(exp));
        }

        /// <summary>
        /// A compiler-assisted (non-string-based) way to call SerializedProperty.FindPropertyRelative
        /// </summary>
        /// <param name="obj">The serialized object to search</param>
        /// <param name="exp">Magic expression that resolves to a field: () => myClass.m_MyField</param>
        /// <returns>The resulting SerializedProperty, or null</returns>
        public static SerializedProperty FindPropertyRelative(this SerializedProperty obj, Expression<Func<object>> exp)
        {
            return obj.FindPropertyRelative(PropertyName(exp));
        }

        /// <summary>Get the value of a property, as an object</summary>
        /// <param name="property">The property to query</param>
        /// <returns>The object value of the property</returns>
        public static object GetPropertyValue(SerializedProperty property)
        {
            var targetObject = property.serializedObject.targetObject;
            var field = targetObject.GetType().GetField(property.propertyPath);
            if (field != null)
                return field.GetValue(targetObject);

            var paths = property.propertyPath.Split('.');
            if (paths.Length > 1)
            {
                var fieldOwner = ReflectionHelpers.GetParentObject(property.propertyPath, targetObject);
                field = fieldOwner?.GetType().GetField(paths[paths.Length-1]);
                if (field != null)
                    return field.GetValue(fieldOwner);
            }
            return null;
        }
    }
}
