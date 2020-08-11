using UnityEditor;
using UnityEngine;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineAdvanced2DConfiner))]
    public class CinemachineAdvanced2DConfinerEditor : BaseEditor<CinemachineAdvanced2DConfiner>
    {
        protected static bool ShowOffsetSettings = true;
 
        public override void OnInspectorGUI()
        {
            DrawRemainingPropertiesInInspector();
            
            ShowOffsetSettings = EditorGUILayout.Foldout(ShowOffsetSettings, "Advanced Settings", true);
            if (ShowOffsetSettings)
            {
                var shriokToSkeletomGUI = new GUIContent("Shrink to Skeleton",
                    "If this is true, then the confiner is going to shrink down to the polygon skeleton and not further. " +
                    "If this is false, then Confiner will locally continue to shrink bones of the skeleton to a point");
                EditorGUILayout.PropertyField(FindProperty(x => x.ShrinkUntilSkeleton), shriokToSkeletomGUI);
                EditorGUILayout.PropertyField(FindProperty(x => x.DrawGizmosDebug));
                EditorGUILayout.PropertyField(FindProperty(x => x.SkipTrimming));
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}