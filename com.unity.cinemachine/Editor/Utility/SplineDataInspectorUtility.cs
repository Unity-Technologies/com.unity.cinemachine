using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.UIElements;

namespace Unity.Cinemachine.Editor
{
    static class SplineDataInspectorUtility
    {
        public delegate ISplineContainer GetSplineDelegate();

        public static VisualElement CreatePathUnitField(SerializedProperty splineDataProp, GetSplineDelegate getSpline)
        {
            var indexUnitProp = splineDataProp.FindPropertyRelative("m_IndexUnit");
            var enumField = new EnumField(indexUnitProp.displayName, (PathIndexUnit)indexUnitProp.enumValueIndex)
                { tooltip = "Defines how to interpret the Index field for each data point.  "
                    + "Knot is the recommended value because it remains robust if the spline points change." };
            enumField.RegisterValueChangedCallback((evt) =>
            {
                var newIndexUnit = (PathIndexUnit)evt.newValue;
                var spline = getSpline?.Invoke();
                if (spline != null && newIndexUnit != (PathIndexUnit)indexUnitProp.intValue)
                {
                    Undo.RecordObject(splineDataProp.serializedObject.targetObject, "Change Index Unit");
                    ConvertPathUnit(splineDataProp, spline, 0, newIndexUnit);
                    indexUnitProp.intValue = (int)newIndexUnit;
                    splineDataProp.serializedObject.ApplyModifiedProperties();
                }
            });
            enumField.TrackPropertyValue(indexUnitProp, (p) => enumField.value = (PathIndexUnit)indexUnitProp.enumValueIndex);
            enumField.TrackAnyUserActivity(() => enumField.SetEnabled(getSpline?.Invoke() != null));

            return enumField;
        }

        static void ConvertPathUnit(
            SerializedProperty splineDataProp,
            ISplineContainer container, int splineIndex, PathIndexUnit newIndexUnit)
        {
            if (container == null || container.Splines.Count == 0)
                return;
            var spline = container.Splines[splineIndex];
            var transform = container is Component component ? component.transform.localToWorldMatrix : Matrix4x4.identity;

            using var native = new NativeSpline(spline, transform);
            var arrayProp = splineDataProp.FindPropertyRelative("m_DataPoints");
            var pathUnitProp = splineDataProp.FindPropertyRelative("m_IndexUnit");
            var from = (PathIndexUnit)Enum.GetValues(typeof(PathIndexUnit)).GetValue(pathUnitProp.enumValueIndex);

            for (int i = 0, c = arrayProp.arraySize; i < c; ++i)
            {
                var point = arrayProp.GetArrayElementAtIndex(i);
                var index = point.FindPropertyRelative("m_Index");
                index.floatValue = native.ConvertIndexUnit(index.floatValue, from, newIndexUnit);
            }

            pathUnitProp.enumValueIndex = (int)newIndexUnit;
        }

        public static ListView CreateDataListField<T>(
            SplineData<T> splineData,
            SerializedProperty splineDataProp, 
            GetSplineDelegate getSpline)
        {
            var sortMethod = splineData.GetType().GetMethod("ForceSort", BindingFlags.Instance | BindingFlags.NonPublic);
            var dirtyMethod = splineData.GetType().GetMethod("SetDirty", BindingFlags.Instance | BindingFlags.NonPublic);

            var pathUnitProp = splineDataProp.FindPropertyRelative("m_IndexUnit");
            var arrayProp = splineDataProp.FindPropertyRelative("m_DataPoints");
            //var array = arrayProp.objectReferenceValue as IList;

            var list = new ListView() 
            { 
                reorderable = false, 
                showFoldoutHeader = false, 
                showBoundCollectionSize = false, 
                showAddRemoveFooter = true
            };
            list.BindProperty(arrayProp);

            list.TrackPropertyValue(arrayProp, (p) => 
            {
                Undo.RecordObject(p.serializedObject.targetObject, "Sort Spline Data");
                //p.serializedObject.ApplyModifiedProperties();

                // Make sure the indexes are properly wrapped around at the bondaries of a loop
                SanitizePathUnit(splineDataProp, getSpline?.Invoke(), 0);
                p.serializedObject.ApplyModifiedProperties();

                // Sort the array
                sortMethod?.Invoke(splineData, null);
                p.serializedObject.Update();
                dirtyMethod?.Invoke(splineData, null);
                p.serializedObject.ApplyModifiedProperties();
            });
            return list;
        }

        static void SanitizePathUnit(SerializedProperty splineDataProp, ISplineContainer container, int splineIndex)
        {
            if (container == null || container.Splines.Count == 0)
                return;
            var spline = container.Splines[splineIndex];
            var splineLength = spline.GetLength();
            var arrayProp = splineDataProp.FindPropertyRelative("m_DataPoints");
            var pathUnitProp = splineDataProp.FindPropertyRelative("m_IndexUnit");
            var unit = (PathIndexUnit)Enum.GetValues(typeof(PathIndexUnit)).GetValue(pathUnitProp.enumValueIndex);

            for (int i = 0, c = arrayProp.arraySize; i < c; ++i)
            {
                var point = arrayProp.GetArrayElementAtIndex(i);
                var index = point.FindPropertyRelative("m_Index");
                index.floatValue = spline.StandardizePosition(index.floatValue, unit, splineLength);
            }
        }
    }
}
