using UnityEditor;

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
                EditorGUILayout.PropertyField(FindProperty(x => x.ShrinkSubgraphsToPoint));
                EditorGUILayout.PropertyField(FindProperty(x => x.DrawGizmosDebug));
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}