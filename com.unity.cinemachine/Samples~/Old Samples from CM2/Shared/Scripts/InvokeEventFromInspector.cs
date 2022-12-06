using UnityEngine;
using UnityEngine.Events;

namespace Cinemachine.OldExamples
{
    public class InvokeEventFromInspector : MonoBehaviour
    {
        public UnityEvent Event = new UnityEvent();
        public void Invoke() { Event.Invoke(); }
    }

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(InvokeEventFromInspector))]
    public class GenerateEventEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            Rect rect = UnityEditor.EditorGUILayout.GetControlRect(true);
            if (GUI.Button(rect, "Invoke", "Button"))
                (target as InvokeEventFromInspector).Invoke();
            UnityEditor.EditorGUI.BeginChangeCheck();
            DrawPropertiesExcluding(serializedObject, "m_Script");
            if (UnityEditor.EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}
