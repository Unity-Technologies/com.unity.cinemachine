using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineBlenderSettings))]
    class CinemachineBlenderSettingsEditor : UnityEditor.Editor
    {
        CinemachineBlenderSettings Target => target as CinemachineBlenderSettings;

        ReorderableList m_BlendList;
        const string k_NoneLabel = "(none)";
        string[] m_CameraCandidates;
        Dictionary<string, int> m_CameraIndexLookup;
        List<CinemachineVirtualCameraBase> m_AllCameras = new();

        /// <summary>
        /// Called when building the Camera popup menus, to get the domain of possible
        /// cameras.  If no delegate is set, will find all top-level (non-slave)
        /// virtual cameras in the scene.
        /// </summary>
        public GetAllVirtualCamerasDelegate GetAllVirtualCameras;
        public delegate void GetAllVirtualCamerasDelegate(List<CinemachineVirtualCameraBase> list);

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (m_BlendList == null)
                SetupBlendList();

            UpdateCameraCandidates();
            m_BlendList.DoLayoutList();
            serializedObject.ApplyModifiedProperties();
        }

        void UpdateCameraCandidates()
        {
            var vcams = new List<string>();
            m_CameraIndexLookup = new Dictionary<string, int>();

            m_AllCameras.Clear();
            if (GetAllVirtualCameras != null)
                GetAllVirtualCameras(m_AllCameras);
            else
            {
                // Get all top-level (i.e. non-slave) virtual cameras
                var candidates = Resources.FindObjectsOfTypeAll<CinemachineVirtualCameraBase>();
                for (var i = 0; i < candidates.Length; ++i)
                    if (candidates[i].ParentCamera == null)
                        m_AllCameras.Add(candidates[i]);
            }
            vcams.Add(k_NoneLabel);
            vcams.Add(CinemachineBlenderSettings.kBlendFromAnyCameraLabel);
            foreach (var c in m_AllCameras)
                if (c != null && !vcams.Contains(c.Name))
                    vcams.Add(c.Name);

            m_CameraCandidates = vcams.ToArray();
            for (int i = 0; i < m_CameraCandidates.Length; ++i)
                m_CameraIndexLookup[m_CameraCandidates[i]] = i;
        }

        void DrawVcamSelector(Rect r, SerializedProperty prop)
        {
            r.width -= EditorGUIUtility.singleLineHeight;
            int current = GetCameraIndex(prop.stringValue);
            var oldColor = GUI.color;
            if (current == 0)
                GUI.color = new Color(1, 193.0f/255.0f, 7.0f/255.0f); // the "warning" icon color
            EditorGUI.PropertyField(r, prop, GUIContent.none);
            r.x += r.width; r.width = EditorGUIUtility.singleLineHeight;
            int sel = EditorGUI.Popup(r, current, m_CameraCandidates);
            if (current != sel)
                prop.stringValue = (m_CameraCandidates[sel] == k_NoneLabel) 
                    ? string.Empty : m_CameraCandidates[sel];
            GUI.color = oldColor;
            
            int GetCameraIndex(string propName)
            {
                if (propName == null || m_CameraIndexLookup == null)
                    return 0;
                if (!m_CameraIndexLookup.ContainsKey(propName))
                    return 0;
                return m_CameraIndexLookup[propName];
            }
        }
        
        void SetupBlendList()
        {
            m_BlendList = new ReorderableList(serializedObject,
                    serializedObject.FindProperty(() => Target.CustomBlends),
                    true, true, true, true);

            // Needed for accessing string names of fields
            var def = new CinemachineBlenderSettings.CustomBlend();
            var def2 = new CinemachineBlendDefinition();

            const float vSpace = 2f;
            const float hSpace = 3f;
            float floatFieldWidth = EditorGUIUtility.singleLineHeight * 2.5f;
            m_BlendList.drawHeaderCallback = (Rect rect) =>
                {
                    rect.width -= EditorGUIUtility.singleLineHeight + 2 * hSpace;
                    rect.width /= 3;
                    rect.x += EditorGUIUtility.singleLineHeight;
                    EditorGUI.LabelField(rect, "From");

                    rect.x += rect.width + hSpace;
                    EditorGUI.LabelField(rect, "To");

                    rect.x += rect.width + hSpace; rect.width -= floatFieldWidth + hSpace;
                    EditorGUI.LabelField(rect, "Style");

                    rect.x += rect.width + hSpace; rect.width = floatFieldWidth;
                    EditorGUI.LabelField(rect, "Time");
                };

            m_BlendList.drawElementCallback
                = (Rect rect, int index, bool isActive, bool isFocused) =>
                {
                    SerializedProperty element
                        = m_BlendList.serializedProperty.GetArrayElementAtIndex(index);

                    rect.y += vSpace;
                    rect.height = EditorGUIUtility.singleLineHeight;
                    rect.width -= 2 * hSpace; rect.width /= 3;
                    DrawVcamSelector(rect, element.FindPropertyRelative(() => def.From));

                    rect.x += rect.width + hSpace;
                    DrawVcamSelector(rect, element.FindPropertyRelative(() => def.To));

                    SerializedProperty blendProp = element.FindPropertyRelative(() => def.Blend);
                    rect.x += rect.width + hSpace;
                    EditorGUI.PropertyField(rect, blendProp, GUIContent.none);
                };

            m_BlendList.onAddCallback = (ReorderableList l) =>
                {
                    var index = l.serializedProperty.arraySize;
                    ++l.serializedProperty.arraySize;
                    SerializedProperty blendProp = l.serializedProperty.GetArrayElementAtIndex(
                            index).FindPropertyRelative(() => def.Blend);

                    blendProp.FindPropertyRelative(() => def2.Style).enumValueIndex
                        = (int)CinemachineBlendDefinition.Styles.EaseInOut;
                    blendProp.FindPropertyRelative(() => def2.Time).floatValue = 2f;
                };
        }
    }
}
