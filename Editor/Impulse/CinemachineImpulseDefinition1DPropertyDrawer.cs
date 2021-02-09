using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(CinemachineImpulseDefinition1D))]
    internal sealed class CinemachineImpulseDefinition1DPropertyDrawer : PropertyDrawer
    {
        const int vSpace = 2;
        const int kGraphHeight = 8; // lines

        #pragma warning disable 649 // variable never used
        CinemachineImpulseDefinition1D m_MyClass;
        #pragma warning restore 649

        GUIContent m_TimeText = null;
        Vector2 m_TimeTextDimensions;

        SerializedProperty m_ShapeProperty;
        float m_ShapePropertyHeight;

        SerializedProperty m_SpatialRangeProperty;
        
        SerializedProperty m_DissipationRateProperty;
        float m_SpreadPropertyHeight;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            int lines = 3;

            m_ShapePropertyHeight = EditorGUIUtility.singleLineHeight + vSpace;
            m_ShapeProperty = property.FindPropertyRelative(() => m_MyClass.m_ImpulseShape);
            if (m_ShapeProperty.isExpanded)
            {
                lines += kGraphHeight;
                m_ShapePropertyHeight *= 1 + kGraphHeight;
            }
            m_ShapePropertyHeight -= vSpace;

            m_DissipationRateProperty = property.FindPropertyRelative(() => m_MyClass.m_DissipationRate);
            m_SpatialRangeProperty = property.FindPropertyRelative(() => m_MyClass.m_SpatialRange);
            var mode = (CinemachineImpulseDefinition1D.PropagationModes)m_SpatialRangeProperty.intValue;
            if (mode != CinemachineImpulseDefinition1D.PropagationModes.Uniform)
            {
                m_SpreadPropertyHeight = EditorGUIUtility.singleLineHeight + vSpace;
                if (m_DissipationRateProperty.isExpanded)
                {
                    lines += kGraphHeight;
                    m_SpreadPropertyHeight *= 1 + kGraphHeight;
                }
                m_SpreadPropertyHeight -= vSpace;
                lines += 2;
                if (mode == CinemachineImpulseDefinition1D.PropagationModes.Propagating)
                    ++lines;
            }
            return lines * (EditorGUIUtility.singleLineHeight + vSpace);
        }

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            // Using BeginProperty / EndProperty on the parent property means that
            // prefab override logic works on the entire property.
            EditorGUI.BeginProperty(rect, label, property);

            rect.height = EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => m_MyClass.m_ImpulseChannel));
                rect.y += rect.height + vSpace;

            // Spatial mode
            EditorGUI.PropertyField(rect, m_SpatialRangeProperty);
            rect.y += rect.height + vSpace;
            var mode = (CinemachineImpulseDefinition1D.PropagationModes)m_SpatialRangeProperty.intValue;
            if (mode != CinemachineImpulseDefinition1D.PropagationModes.Uniform)
            {
                EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => m_MyClass.m_EffectRadius));
                rect.y += rect.height + vSpace;

                // Spread combo
                rect.height = m_SpreadPropertyHeight;
                DrawSpreadCombo(rect, property);
                rect.y += rect.height + vSpace; rect.height = EditorGUIUtility.singleLineHeight;

                // Propaation speed
                if (mode == CinemachineImpulseDefinition1D.PropagationModes.Propagating)
                {
                    EditorGUI.PropertyField(rect, property.FindPropertyRelative(() => m_MyClass.m_PropagationSpeed));
                    rect.y += rect.height + vSpace;
                }
            }
            // Impulse Shape combo
            rect.height = m_ShapePropertyHeight;
            DrawImpulseShapeCombo(rect, property);
            rect.y += rect.height + vSpace; rect.height = EditorGUIUtility.singleLineHeight;

            EditorGUI.EndProperty();
        }

        void DrawImpulseShapeCombo(Rect fullRect, SerializedProperty property)
        {
            float floatFieldWidth = EditorGUIUtility.singleLineHeight * 3f;

            SerializedProperty timeProp = property.FindPropertyRelative(() => m_MyClass.m_ImpulseDuration);
            if (m_TimeText == null)
            {
                m_TimeText = new GUIContent(" s", timeProp.tooltip);
                m_TimeTextDimensions = GUI.skin.label.CalcSize(m_TimeText);
            }

            var graphRect = fullRect; 
            graphRect.y += EditorGUIUtility.singleLineHeight + vSpace;
            graphRect.height -= EditorGUIUtility.singleLineHeight + vSpace;

            var indentLevel = EditorGUI.indentLevel;

            Rect r = fullRect; r.height = EditorGUIUtility.singleLineHeight;
            r = EditorGUI.PrefixLabel(r, EditorGUI.BeginProperty(
                r, new GUIContent(m_ShapeProperty.displayName, m_ShapeProperty.tooltip), property));
            m_ShapeProperty.isExpanded =  EditorGUI.Foldout(r, m_ShapeProperty.isExpanded, GUIContent.none);

            r.width -= floatFieldWidth + m_TimeTextDimensions.x;
            if (m_ShapeProperty.intValue != (int)CinemachineImpulseDefinition1D.ImpulseShapes.Custom)
            {
                EditorGUI.BeginChangeCheck();
                    EditorGUI.PropertyField(r, m_ShapeProperty, GUIContent.none);
                if (EditorGUI.EndChangeCheck())
                    InvalidateImpulseGraphSample();
                if (Event.current.type == EventType.Repaint && m_ShapeProperty.isExpanded)
                    DrawImpulseGraph(graphRect, CinemachineImpulseDefinition1D.GetStandardCurve(
                        (CinemachineImpulseDefinition1D.ImpulseShapes)m_ShapeProperty.intValue));
            }
            else
            {
                SerializedProperty curveProp = property.FindPropertyRelative(() => m_MyClass.m_CustomImpulseShape);
                r.width -= r.height;
                r.height -= 1;
                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(r, curveProp, GUIContent.none);
                r.x += r.width; r.width = r.height; ++r.height;
                EditorGUI.PropertyField(r, m_ShapeProperty, GUIContent.none);
                if (EditorGUI.EndChangeCheck())
                {
                    curveProp.animationCurveValue = RuntimeUtility.NormalizeCurve(curveProp.animationCurveValue, true, false);
                    curveProp.serializedObject.ApplyModifiedProperties();
                    InvalidateImpulseGraphSample();
                }
                if (Event.current.type == EventType.Repaint && m_ShapeProperty.isExpanded)
                    DrawImpulseGraph(graphRect, curveProp.animationCurveValue);
            }

            // Time
            float oldWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = m_TimeTextDimensions.x;
            r.x += r.width; r.width = floatFieldWidth + EditorGUIUtility.labelWidth;
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(r, timeProp, m_TimeText);
            if (EditorGUI.EndChangeCheck())
                timeProp.floatValue = Mathf.Max(timeProp.floatValue, 0);
            EditorGUIUtility.labelWidth = oldWidth;
            
            EditorGUI.indentLevel = indentLevel;
        }

        const int kNumSamples = 100;
        Vector3[] m_ImpulseGraphSnapshot = new Vector3[kNumSamples+1];
        float m_ImpulseGraphZero;
        Vector2 m_ImpulseGraphSize;
        void InvalidateImpulseGraphSample() { m_ImpulseGraphSize = Vector2.zero; }

        void DrawImpulseGraph(Rect rect, AnimationCurve curve)
        {
            // Resample if necessary
            if (m_ImpulseGraphSize != rect.size)
            {
                m_ImpulseGraphSize = rect.size;
                float minY = 0;
                float maxY = 0.1f;
                for (int i = 0; i <= kNumSamples; ++i)
                {
                    var x = (float)i / kNumSamples;
                    var y = curve.Evaluate(x);
                    minY = Mathf.Min(y, minY);
                    maxY = Mathf.Max(y, maxY);
                    m_ImpulseGraphSnapshot[i] = new Vector2(x * rect.width, y);
                }
                var range = maxY - minY;
                m_ImpulseGraphZero = -minY / range;
                m_ImpulseGraphZero = rect.height * (1f - m_ImpulseGraphZero);

                // Apply scale
                for (int i = 0; i <= kNumSamples; ++i)
                    m_ImpulseGraphSnapshot[i].y = rect.height * (1f - (m_ImpulseGraphSnapshot[i].y - minY) / range);
            }
            EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f, 1));
            var oldMatrix = Handles.matrix;
            Handles.matrix = Handles.matrix * Matrix4x4.Translate(rect.position);
            Handles.color = new Color(0, 0, 0, 1); 
            Handles.DrawLine(new Vector3(0, m_ImpulseGraphZero, 0), new Vector3(rect.width, m_ImpulseGraphZero, 0));
            Handles.color = new Color(1, 0.8f, 0, 1); 
            Handles.DrawPolyLine(m_ImpulseGraphSnapshot);
            Handles.matrix = oldMatrix;
        }

        void DrawSpreadCombo(Rect fullRect, SerializedProperty property)
        {
            var graphRect = fullRect; 
            graphRect.y += EditorGUIUtility.singleLineHeight + vSpace;
            graphRect.height -= EditorGUIUtility.singleLineHeight + vSpace;

            var indentLevel = EditorGUI.indentLevel;

            Rect r = fullRect; r.height = EditorGUIUtility.singleLineHeight;
            r = EditorGUI.PrefixLabel(r, EditorGUI.BeginProperty(
                r, new GUIContent(m_DissipationRateProperty.displayName, m_DissipationRateProperty.tooltip), property));
            m_DissipationRateProperty.isExpanded =  EditorGUI.Foldout(r, m_DissipationRateProperty.isExpanded, GUIContent.none);

            EditorGUI.BeginChangeCheck();
                EditorGUI.Slider(r, m_DissipationRateProperty, 0, 1, GUIContent.none);
            if (EditorGUI.EndChangeCheck())
                InvalidateSpreadGraphSample();
            if (Event.current.type == EventType.Repaint && m_DissipationRateProperty.isExpanded)
                DrawSpreadGraph(graphRect, m_DissipationRateProperty.floatValue);
            EditorGUI.indentLevel = indentLevel;
        }

        Vector3[] m_SpreadGraphSnapshot = new Vector3[kNumSamples+1];
        Vector2 m_SpreadGraphSize;
        void InvalidateSpreadGraphSample() { m_SpreadGraphSize = Vector2.zero; }

        void DrawSpreadGraph(Rect rect, float spread)
        {
            // Resample if necessary
            if (m_SpreadGraphSize != rect.size)
            {
                m_SpreadGraphSize = rect.size;
                for (int i = 0; i <= kNumSamples >> 1; ++i)
                {
                    var x = (float)i / kNumSamples;
                    var y = CinemachineImpulseManager.EvaluateDissipationScale(spread, Mathf.Abs(1 - x * 2));
                    m_SpreadGraphSnapshot[i] = new Vector2(x * rect.width, rect.height * (1 - y));
                    m_SpreadGraphSnapshot[kNumSamples - i] = new Vector2((1 - x) * rect.width, rect.height * (1 - y));
                }
            }
            EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f, 1));
            var oldMatrix = Handles.matrix;
            Handles.matrix = Handles.matrix * Matrix4x4.Translate(rect.position);
            Handles.color = new Color(0, 0, 0, 1); 
            Handles.DrawLine(new Vector3(rect.width * 0.5f, 0, 0), new Vector3(rect.width * 0.5f, rect.height, 0));
            Handles.color = new Color(0, 0.6f, 1, 1); 
            Handles.DrawPolyLine(m_SpreadGraphSnapshot);
            Handles.matrix = oldMatrix;
        }
    }
}
