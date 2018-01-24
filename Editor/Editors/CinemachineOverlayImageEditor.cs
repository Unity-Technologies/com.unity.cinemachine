using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineOverlayImage))]
    public sealed class CinemachineOverlayImageEditor : BaseEditor<CinemachineOverlayImage>
    {
        protected override List<string> GetExcludedPropertiesInInspector()
        {
            List<string> excluded = base.GetExcludedPropertiesInInspector();
            excluded.Add(FieldPath(x => x.VirtualCamera));
            excluded.Add(FieldPath(x => x.m_ShowImage));
            excluded.Add(FieldPath(x => x.m_Image));
            return excluded;
        }

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
            }
            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();

            DrawRemainingPropertiesInInspector();
        }


#if UNITY_EDITOR
        [InitializeOnLoad]
        class EditorInitialize 
        { 
            static EditorInitialize() { CinemachineOverlayImage.InitializeModule(); } 
        }
#endif
    }
}
