using UnityEngine;
using UnityEditor;

namespace Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(Cinemachine3OrbitRig.Orbit))]
    internal sealed class FreeLookOrbitPropertyDrawer : PropertyDrawer
    {
        Cinemachine3OrbitRig.Orbit def = new Cinemachine3OrbitRig.Orbit();

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            InspectorUtility.MultiPropertyOnLine(
                rect, label,
                new [] { property.FindPropertyRelative(() => def.Height),
                        property.FindPropertyRelative(() => def.Radius) },
                null);
        }
    }
}
