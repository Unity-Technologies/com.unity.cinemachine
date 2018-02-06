using UnityEngine;
using UnityEditor;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineOverlayImage))]
    public sealed class CinemachineOverlayImageEditor : BaseEditor<CinemachineOverlayImage>
    {
        public override void OnInspectorGUI()
        {
            BeginInspector();
            EditorGUI.BeginChangeCheck();
            {
                Rect rect = EditorGUILayout.GetControlRect(true);
                float width = rect.width;
                rect.width = EditorGUIUtility.labelWidth + rect.height;
                EditorGUI.PropertyField(rect, FindProperty(x => x.m_ShowImage));

                rect.x += rect.width; rect.width = width - rect.width;
                EditorGUI.PropertyField(rect, FindProperty(x => x.m_Image), GUIContent.none);

                EditorGUILayout.PropertyField(FindProperty(x => x.m_Aspect));
                EditorGUILayout.PropertyField(FindProperty(x => x.m_Alpha));
                EditorGUILayout.PropertyField(FindProperty(x => x.m_Center));
                EditorGUILayout.PropertyField(FindProperty(x => x.m_Rotation));

                rect = EditorGUILayout.GetControlRect(true);
                EditorGUI.LabelField(rect, "Scale");
                rect.x += EditorGUIUtility.labelWidth; rect.width -= EditorGUIUtility.labelWidth;
                rect.width /= 3;
                var prop = FindProperty(x => x.m_SyncScale);
                GUIContent syncLabel = new GUIContent("Sync", prop.tooltip);
                prop.boolValue = EditorGUI.ToggleLeft(rect, syncLabel, prop.boolValue);
                rect.x += rect.width; 
                if (prop.boolValue)
                {
                    prop = FindProperty(x => x.m_Scale);
                    float[] values = new float[1] { prop.vector2Value.x };
                    EditorGUI.MultiFloatField(rect, new GUIContent[1] { new GUIContent("X") }, values);
                    prop.vector2Value = new Vector2(values[0], values[0]);
                }
                else
                {
                    rect.width *= 2;
                    EditorGUI.PropertyField(rect, FindProperty(x => x.m_Scale), GUIContent.none);
                }
                EditorGUILayout.PropertyField(FindProperty(x => x.m_MuteCamera));
                EditorGUILayout.PropertyField(FindProperty(x => x.m_Wipe));
            }
            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();
        }
    }
}
