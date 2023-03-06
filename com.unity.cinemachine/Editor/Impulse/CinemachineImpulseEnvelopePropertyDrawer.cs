using UnityEngine;
using UnityEditor;

namespace Unity.Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(CinemachineImpulseManager.EnvelopeDefinition))]
    class CinemachineImpulseEnvelopePropertyDrawer : PropertyDrawer
    {
        const int vSpace = 2;
        static bool mExpanded = true;

        #pragma warning disable 649 // variable never used
        CinemachineImpulseManager.EnvelopeDefinition myClass; // to access name strings
        #pragma warning restore 649

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;
            rect.height = height;
            mExpanded = EditorGUI.Foldout(rect, mExpanded, label, true);
            if (mExpanded)
            {
                const float indentAmount = 15;
                rect.width -= indentAmount; rect.x += indentAmount;
                float oldWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth -= indentAmount;

                rect.y += EditorGUIUtility.singleLineHeight + vSpace;
                DrawCurveTimeProperty(
                    rect, new GUIContent("Attack", "The custom shape of the attack curve.  Leave it blank for a default shape"),
                    property.FindPropertyRelative(() => myClass.AttackShape),
                    property.FindPropertyRelative(() => myClass.AttackTime));

                rect.y += EditorGUIUtility.singleLineHeight + vSpace;
#if false // with "forever" button... dangerous because signal never goes away!
                var holdProp = property.FindPropertyRelative(() => myClass.m_SustainTime);
                InspectorUtility.MultiPropertyOnLine(
                    rect, new GUIContent(holdProp.displayName, holdProp.tooltip),
                    new SerializedProperty[] { holdProp, property.FindPropertyRelative(() => myClass.m_HoldForever) },
                    new GUIContent[] { GUIContent.none, new GUIContent("forever") });
#else
                EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => myClass.SustainTime));
#endif
                rect.y += EditorGUIUtility.singleLineHeight + vSpace;
                DrawCurveTimeProperty(
                    rect, new GUIContent("Decay", "The custom shape of the decay curve.  Leave it blank for a default shape"),
                    property.FindPropertyRelative(() => myClass.DecayShape),
                    property.FindPropertyRelative(() => myClass.DecayTime));

                rect.y += EditorGUIUtility.singleLineHeight + vSpace;
                EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => myClass.ScaleWithImpact));

                EditorGUIUtility.labelWidth = oldWidth;
            }
        }

        void DrawCurveTimeProperty(
            Rect rect, GUIContent label,
            SerializedProperty curveProp, SerializedProperty timeProp)
        {
            float floatFieldWidth = EditorGUIUtility.singleLineHeight * 2.5f;

            GUIContent timeText = new GUIContent(" s", timeProp.tooltip);
            var textDimensions = GUI.skin.label.CalcSize(timeText);

            rect = EditorGUI.PrefixLabel(rect, label);

            rect.height = EditorGUIUtility.singleLineHeight;
            rect.width -= floatFieldWidth + textDimensions.x;

            Rect r = rect; r.height += 1; r.y -= 1;
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(r, curveProp, GUIContent.none);
            if (EditorGUI.EndChangeCheck())
            {
                curveProp.animationCurveValue = InspectorUtility.NormalizeCurve(curveProp.animationCurveValue);
                if (curveProp.animationCurveValue.length < 1)
                    curveProp.animationCurveValue = new AnimationCurve();
                curveProp.serializedObject.ApplyModifiedProperties();
            }

            float oldWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = textDimensions.x;
            rect.x += rect.width; rect.width = floatFieldWidth + EditorGUIUtility.labelWidth;
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(rect, timeProp, timeText);
            if (EditorGUI.EndChangeCheck())
                timeProp.floatValue = Mathf.Max(timeProp.floatValue, 0);
            EditorGUIUtility.labelWidth = oldWidth;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight + vSpace;
            if (mExpanded)
                height *= 5;
            return height;
        }
    }
}
