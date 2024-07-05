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
        public delegate T GetDefaultValueDelegate<T>();

        public const string ItemIndexTooltip = "The position on the Spline at which this data point will take effect.  "
            + "The value is interpreted according to the Index Unit setting.";

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
            enumField.AddToClassList(InspectorUtility.AlignFieldClassName);

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

        public static ListView CreateDataListField<T>(
            SplineData<T> splineData,
            SerializedProperty splineDataProp, 
            GetSplineDelegate getSpline,
            GetDefaultValueDelegate<T> getDefaultValue = null)
        {
            var sortMethod = splineData.GetType().GetMethod("ForceSort", BindingFlags.Instance | BindingFlags.NonPublic);
            var setDataPointMethod = splineData.GetType().GetMethod("SetDataPoint", BindingFlags.Instance | BindingFlags.Public);

            var pathUnitProp = splineDataProp.FindPropertyRelative("m_IndexUnit");
            var arrayProp = splineDataProp.FindPropertyRelative("m_DataPoints");

            var list = new ListView 
            {
                reorderable = false,
                showBorder = true,
                showFoldoutHeader = false,
                showBoundCollectionSize = false,
                showAddRemoveFooter = true,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                showAlternatingRowBackgrounds = AlternatingRowBackground.None
            };
            list.BindProperty(arrayProp);

            // When we add the first item, make sure to use the default value
            var button = list.Q<Button>("unity-list-view__add-button");
            button.clicked += () => 
            {
                if (arrayProp.arraySize == 1)
                {
                    T value = getDefaultValue != null ? getDefaultValue() : splineData.DefaultValue;
                    setDataPointMethod.Invoke(splineData, new object[] { 0, new DataPoint<T> () { Value = value } });
                    arrayProp.serializedObject.Update();
                }
            };

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
                    // Try to preserve the selected item through the sort
                    float index = 0;
                    T value = default;
                    var selected = list.selectedIndex;
                    if (selected >= 0)
                    {
                        index = splineData[selected].Index;
                        value = splineData[selected].Value;
                    }

                    Undo.RecordObject(p.serializedObject.targetObject, "Sort Spline Data");
                    sortMethod?.Invoke(splineData, null);
                    p.serializedObject.Update();

                    for (int i = 0; selected >= 0 && i < splineData.Count; ++i)
                    {
                        if (index == splineData[i].Index && splineData[i].Value.Equals(value))
                        {
                            list.selectedIndex = i;
                            EditorApplication.delayCall += () => list.ScrollToItem(i);
                            break;
                        }
                    }
                }
            });

            return list;
        }

        static bool SanitizePathUnit(SerializedProperty splineDataProp, ISplineContainer container, int splineIndex)
        {
            if (container == null || container.Splines.Count <= splineIndex)
                return false;

            bool changed = false;
            var arrayProp = splineDataProp.FindPropertyRelative("m_DataPoints");
            var pathUnitProp = splineDataProp.FindPropertyRelative("m_IndexUnit");
            var unit = (PathIndexUnit)Enum.GetValues(typeof(PathIndexUnit)).GetValue(pathUnitProp.enumValueIndex);

            var matrix = container is Component component ? component.transform.localToWorldMatrix : Matrix4x4.identity;
            using var nativeSpline = new NativeSpline(container.Splines[splineIndex], matrix);
            for (int i = 0, c = arrayProp.arraySize; i < c; ++i)
            {
                var point = arrayProp.GetArrayElementAtIndex(i);
                var index = point.FindPropertyRelative("m_Index");

                var newValue = nativeSpline.StandardizePostition(index.floatValue, unit);
                if (newValue != index.floatValue)
                {
                    index.floatValue = newValue;
                    changed = true;
                }
            }
            return changed;
        }

        /// <summary>
        /// Keeps position within the cacnonical range for the spline, removing wraps if spline is looped.
        /// </summary>
        static float StandardizePostition(this ISpline spline, float t, PathIndexUnit unit)
        {
            float maxPos = 1;
            switch (unit)
            {
                case PathIndexUnit.Distance: 
                    maxPos = spline.GetLength(); 
                    break;
                case PathIndexUnit.Knot: 
                {
                    var knotCount = spline.Count;
                    maxPos = (!spline.Closed || knotCount < 2) ? Mathf.Max(0, knotCount - 1) : knotCount;
                    break;
                }
            }

            if (float.IsNaN(t))
                return 0;
            if (!spline.Closed)
                return Mathf.Clamp(t, 0, maxPos);
            t %= maxPos;
            if (t < 0)
                t += maxPos;
            return t;
        }
    }
}
