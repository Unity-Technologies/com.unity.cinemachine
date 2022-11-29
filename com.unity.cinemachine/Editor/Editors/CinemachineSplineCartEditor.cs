#if CINEMACHINE_PHYSICS

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineSplineCart))]
    [CanEditMultipleObjects]
    class CinemachineSplineCartEditor : UnityEditor.Editor
    {
        CinemachineSplineCart Target => target as CinemachineSplineCart;

        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.Spline)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.UpdateMethod)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.PositionUnits)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.AutomaticDolly)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.TrackingTarget)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.SplinePosition)));
            return ux;
        }
    }
}
#endif
