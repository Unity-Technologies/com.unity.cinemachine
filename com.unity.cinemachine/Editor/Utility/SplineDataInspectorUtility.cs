using System;
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
                    splineDataProp.serializedObject.Update();
                    ConvertPathUnit(splineDataProp, spline, 0, newIndexUnit);
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
            var arrayProp = splineDataProp.FindPropertyRelative("m_DataPoints");
            var pathUnitProp = splineDataProp.FindPropertyRelative("m_IndexUnit");
            var from = (PathIndexUnit)Enum.GetValues(typeof(PathIndexUnit)).GetValue(pathUnitProp.enumValueIndex);

            var spline = container.Splines[splineIndex];
            var transform = container is Component component ? component.transform.localToWorldMatrix : Matrix4x4.identity;
            using var native = new NativeSpline(spline, transform);
            for (int i = 0, c = arrayProp.arraySize; i < c; ++i)
            {
                var point = arrayProp.GetArrayElementAtIndex(i);
                var index = point.FindPropertyRelative("m_Index");
                index.floatValue = native.ConvertIndexUnit(index.floatValue, from, newIndexUnit);
            }
            pathUnitProp.enumValueIndex = (int)newIndexUnit;
        }

        public static PropertyField CreateDataListField<T>(
            SplineData<T> splineData,
            SerializedProperty splineDataProp, 
            GetSplineDelegate getSpline)
        {
            var sortMethod = splineData.GetType().GetMethod("ForceSort", BindingFlags.Instance | BindingFlags.NonPublic);
            var setDataPointMethod = splineData.GetType().GetMethod("SetDataPoint", BindingFlags.Instance | BindingFlags.Public);

            var pathUnitProp = splineDataProp.FindPropertyRelative("m_IndexUnit");
            var arrayProp = splineDataProp.FindPropertyRelative("m_DataPoints");

            var list = new PropertyField(arrayProp);
            list.OnInitialGeometry(() => 
            {
                var listView = list.Q<ListView>();
                listView.reorderable = false;
                listView.showFoldoutHeader = false;
                listView.showBoundCollectionSize = false;
                listView.showAddRemoveFooter = true;
                listView.showAlternatingRowBackgrounds = AlternatingRowBackground.None;

                var button = list.Q<Button>("unity-list-view__add-button");
                button.clicked += () => 
                {
                    if (arrayProp.arraySize == 1)
                    {
                        setDataPointMethod.Invoke(splineData, new object[] { 0, new DataPoint<T> () { Value = splineData.DefaultValue } });
                        arrayProp.serializedObject.Update();
                    }
                };
            });

            list.TrackPropertyValue(arrayProp, (p) => 
            {
                p.serializedObject.ApplyModifiedProperties();

                // Make sure the indexes are properly wrapped around at the boundaries of a loop
                if (SanitizePathUnit(splineDataProp, getSpline?.Invoke(), 0))
                    p.serializedObject.ApplyModifiedProperties();

                // Sort the array
                bool needsSort = false;
                for (int i = 1; !needsSort && i < splineData.Count; ++i)
                    needsSort = splineData[i].Index < splineData[i - 1].Index;
                if (needsSort)
                {
                    Undo.RecordObject(p.serializedObject.targetObject, "Sort Spline Data");
                    sortMethod?.Invoke(splineData, null);
                    p.serializedObject.Update();
                }
            });
            return list;
        }

        static bool SanitizePathUnit(SerializedProperty splineDataProp, ISplineContainer container, int splineIndex)
        {
            if (container == null || container.Splines.Count == 0)
                return false;

            bool changed = false;
            var arrayProp = splineDataProp.FindPropertyRelative("m_DataPoints");
            var pathUnitProp = splineDataProp.FindPropertyRelative("m_IndexUnit");
            var unit = (PathIndexUnit)Enum.GetValues(typeof(PathIndexUnit)).GetValue(pathUnitProp.enumValueIndex);

            var spline = container.Splines[splineIndex];
            var transform = container is Component component ? component.transform : null;
            var scaledSpline = new CachedScaledSpline(spline, transform, Collections.Allocator.Temp);
            for (int i = 0, c = arrayProp.arraySize; i < c; ++i)
            {
                var point = arrayProp.GetArrayElementAtIndex(i);
                var index = point.FindPropertyRelative("m_Index");
                var newValue = scaledSpline.StandardizePosition(index.floatValue, unit, out _);
                if (newValue != index.floatValue)
                {
                    index.floatValue = newValue;
                    changed = true;
                }
                index.floatValue = scaledSpline.StandardizePosition(index.floatValue, unit, out _);
            }
            return changed;
        }
    }
}
