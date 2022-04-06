using UnityEngine;
using UnityEditor;

namespace Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(CinemachineNewFreeLook.Orbit))]
    internal sealed class FreeLookOrbitPropertyDrawer : PropertyDrawer
    {
        CinemachineFreeLook.Orbit def = new CinemachineFreeLook.Orbit();

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            InspectorUtility.MultiPropertyOnLine(
                rect, label,
                new [] { property.FindPropertyRelative(() => def.m_Height),
                        property.FindPropertyRelative(() => def.m_Radius) },
                null);
        }
    }
}
