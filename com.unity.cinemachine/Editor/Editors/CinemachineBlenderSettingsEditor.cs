#define USE_IMGUI_INSTRUCTION_LIST // We use IMGUI while we wait for UUM-27687 and UUM-27688 to be fixed

using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineBlenderSettings))]
    class CinemachineBlenderSettingsEditor : UnityEditor.Editor
    {
        CinemachineBlenderSettings Target => target as CinemachineBlenderSettings;
        const string k_NoneLabel = "(none)";

        /// <summary>
        /// Called when building the Camera popup menus, to get the domain of possible
        /// cameras.  If no delegate is set, will find all top-level (non-slave)
        /// virtual cameras in the scene.
        /// </summary>
        public GetAllVirtualCamerasDelegate GetAllVirtualCameras = GetToplevelCameras;
        public delegate void GetAllVirtualCamerasDelegate(List<CinemachineVirtualCameraBase> list);

        // Get all top-level (i.e. non-slave) virtual cameras
        static void GetToplevelCameras(List<CinemachineVirtualCameraBase> list)
        {
            var candidates = Resources.FindObjectsOfTypeAll<CinemachineVirtualCameraBase>();
            for (var i = 0; i < candidates.Length; ++i)
                if (candidates[i].ParentCamera == null)
                    list.Add(candidates[i]);
        }

#if USE_IMGUI_INSTRUCTION_LIST
        Color m_warningColor = new (1, 193.0f/255.0f, 7.0f/255.0f); // the "warning" icon color
        ReorderableList m_BlendList;
        string[] m_CameraCandidates;
        Dictionary<string, int> m_CameraIndexLookup;
        List<CinemachineVirtualCameraBase> m_AllCameras = new();

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
            GetAllVirtualCameras?.Invoke(m_AllCameras);
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
                GUI.color = m_warningColor;
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
#else
        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();

            var vcam = Target;
            var header = ux.AddChild(new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = -2 } });
            FormatElement(
                header.AddChild(new Label("From")), 
                header.AddChild(new Label("To")), 
                header.AddChild(new Label("Blend")));
            header.AddToClassList("unity-collection-view--with-border");

            var list = ux.AddChild(new ListView()
            {
                reorderable = true,
                reorderMode = ListViewReorderMode.Animated,
                showAddRemoveFooter = true,
                showBorder = true,
                showBoundCollectionSize = false,
                showFoldoutHeader = false,
                style = { borderTopWidth = 0, marginLeft = 0 },
            });
            var elements = serializedObject.FindProperty(() => Target.CustomBlends);
            list.BindProperty(elements);

            // Gather the camera candidates
            var availableCameras = new List<string>();
            Dictionary<string, int> cameraIndexLookup = new();
            list.TrackAnyUserActivity(() =>
            {
                var allCameras = new List<CinemachineVirtualCameraBase>();
                GetAllVirtualCameras(allCameras);
                availableCameras.Clear();
                availableCameras.Add(string.Empty);
                availableCameras.Add(CinemachineBlenderSettings.kBlendFromAnyCameraLabel);
                foreach (var c in allCameras)
                    if (c != null && !availableCameras.Contains(c.Name))
                        availableCameras.Add(c.Name);
                list.RefreshItems();
            });

            // Delay to work around a bug in ListView (UUM-27687 and UUM-27688)
            list.OnInitialGeometry(() =>
            {
                list.makeItem = () => new BindableElement { style = { flexDirection = FlexDirection.Row }};
                list.bindItem = (row, index) =>
                {
                    // Remove children - items get recycled
                    for (int i = row.childCount - 1; i >= 0; --i)
                        row.RemoveAt(i);

                    var def = new CinemachineBlenderSettings.CustomBlend();
                    var element = elements.GetArrayElementAtIndex(index);
                    ((BindableElement)row).BindProperty(element);

                    var from = row.AddChild(CreateCameraPopup(element.FindPropertyRelative(() => def.From)));
                    var to = row.AddChild(CreateCameraPopup(element.FindPropertyRelative(() => def.To)));
                    var blend = row.AddChild(new PropertyField(element.FindPropertyRelative(() => def.Blend), ""));
                    
                    FormatElement(from, to, blend);
                };
            });

            // Local function
            static void FormatElement(VisualElement e1, VisualElement e2, VisualElement e3)
            {
                e1.style.marginLeft = 3;
                e1.style.flexBasis = 1; 
                e1.style.flexGrow = 3;
                
                e2.style.flexBasis = 1; 
                e2.style.flexGrow = 3;

                e3.style.flexBasis = 1; 
                e3.style.flexGrow = 2;
            }

            // Local function
            VisualElement CreateCameraPopup(SerializedProperty p)
            {
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, flexGrow = 1 }};
                if (availableCameras.FindIndex(x => x == p.stringValue) < 0)
                    row.AddChild(InspectorUtility.MiniHelpIcon("No available camera has this name"));
                var popup = row.AddChild(new PopupField<string>()
                    { style = { marginLeft = 0, marginRight = 3, flexGrow = 1 }});
                popup.BindProperty(p);
                popup.choices = availableCameras;
                popup.formatListItemCallback = (s) => string.IsNullOrEmpty(s) ? k_NoneLabel : s;
                return row;
            }

            return ux;
        }
#endif
    }
}
