using System.Collections.Generic;
using Cinemachine.Utility;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Events;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineEventMapper))]
    internal sealed class CinemachineEventMapperEditor : BaseEditor<CinemachineEventMapper>
    {
        protected override List<string> GetExcludedPropertiesInInspector()
        {
            List<string> excluded = base.GetExcludedPropertiesInInspector();
            excluded.Add(FieldPath(x => x.m_Instructions));
            return excluded;
        }

        private ReorderableList mInstructionList;

        void OnEnable()
        {
            mInstructionList = null;
        }

        public override void OnInspectorGUI()
        {
            BeginInspector();
            if (mInstructionList == null)
                SetupInstructionList();

            // Ordinary properties
            DrawRemainingPropertiesInInspector();

            // Instructions
            UpdateCameraCandidates();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.Separator();
            mInstructionList.DoLayoutList();
            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();
        }

        private string[] mCameraCandidateNames;
        private CinemachineVirtualCameraBase[] mCameraCandidates;

        private Dictionary<CinemachineVirtualCameraBase, int> mCameraIndexLookup;
        private void UpdateCameraCandidates()
        {
            List<CinemachineVirtualCameraBase> vcams = new List<CinemachineVirtualCameraBase>();
            List<string> vcamNames = new List<string>();
            mCameraIndexLookup = new Dictionary<CinemachineVirtualCameraBase, int>();

            // Get all top-level (i.e. non-slave) virtual cameras
            mCameraCandidates = Resources.FindObjectsOfTypeAll(
                    typeof(CinemachineVirtualCameraBase)) as CinemachineVirtualCameraBase[];

            vcams.Add(null);
            vcamNames.Add("(none)");
            foreach (CinemachineVirtualCameraBase c in mCameraCandidates)
            {
                if (c.ParentCamera == null)
                {
                    mCameraIndexLookup[c] = vcams.Count;
                    vcams.Add(c);
                    vcamNames.Add(c.Name);
                }
            }
            mCameraCandidates = vcams.ToArray();
            mCameraCandidateNames = vcamNames.ToArray();
        }

        private int GetCameraIndex(Object obj)
        {
            if (obj == null || mCameraIndexLookup == null)
                return 0;
            CinemachineVirtualCameraBase vcam = obj as CinemachineVirtualCameraBase;
            if (vcam == null)
                return 0;
            if (!mCameraIndexLookup.ContainsKey(vcam))
                return 0;
            return mCameraIndexLookup[vcam];
        }

        void SetupInstructionList()
        {
            const float kVeryWideView = 550;
            const float vSpace = 2;
            const float hSpace = 3;
            float floatFieldWidth = EditorGUIUtility.singleLineHeight * 2.5f;
            float hBigSpace = EditorGUIUtility.singleLineHeight * 2 / 3;
            
            mInstructionList = new ReorderableList(serializedObject,
                    serializedObject.FindProperty(() => Target.m_Instructions),
                    true, true, true, true);
            mInstructionList.elementHeight = 2 * (EditorGUIUtility.singleLineHeight + vSpace) + vSpace;

            // Needed for accessing field names as strings
            CinemachineEventMapper.Instruction def = new CinemachineEventMapper.Instruction();

            mInstructionList.elementHeightCallback = (int index) =>
                {
                    return EditorGUIUtility.currentViewWidth >= kVeryWideView
                        ? EditorGUIUtility.singleLineHeight + vSpace
                        : 2 * (EditorGUIUtility.singleLineHeight + vSpace) + vSpace;
                };

            mInstructionList.drawHeaderCallback = (Rect rect) =>
                {
                    rect.width = rect.width - EditorGUIUtility.singleLineHeight 
                        - 4 * (hBigSpace + floatFieldWidth);
                    rect.x += EditorGUIUtility.singleLineHeight;
                    if (EditorGUIUtility.currentViewWidth < kVeryWideView)
                        EditorGUI.LabelField(rect, "Event/Camera");
                    else
                    {
                        EditorGUI.LabelField(rect, "Event");
                        Rect r = rect; r.width /= 3; r.x += 2 * r.width;
                        EditorGUI.LabelField(r, "Camera");
                    }
                    rect.x += rect.width + hBigSpace; rect.width = floatFieldWidth;
                    EditorGUI.LabelField(rect, "Boost");

                    rect.x += rect.width + hBigSpace;
                    EditorGUI.LabelField(rect, "Wait");

                    rect.x += rect.width + hBigSpace;
                    EditorGUI.LabelField(rect, "Min");

                    rect.x += rect.width + hBigSpace;
                    EditorGUI.LabelField(rect, "Max");
                };

            mInstructionList.drawElementCallback
                = (Rect rect, int index, bool isActive, bool isFocused) =>
                {
                    SerializedProperty element = mInstructionList.serializedProperty.GetArrayElementAtIndex(index);
                    rect.height = EditorGUIUtility.singleLineHeight;

                    // Assume view is wide enough for one line
                    Rect rWait = new Rect(rect); 
                    rWait.width = 4 * (hBigSpace + floatFieldWidth); 
                    rWait.x = rect.x + rect.width - rWait.width;
                    Rect rObj = rect; rObj.width -= rWait.width + 2 * hSpace; rObj.width /= 3; 
                    Rect rEvent = rObj; rEvent.x += rObj.width + hSpace;
                    Rect rCam = rEvent; rCam.x += rEvent.width + hSpace;

                    // If too narrow, split onto 2 lines
                    if (EditorGUIUtility.currentViewWidth < kVeryWideView)
                    {
                        rObj.width = rect.width * 2 / 5;
                        rEvent = rObj; rEvent.x += rObj.width + hSpace; 
                        rEvent.width = rect.width - rObj.width - hSpace;
                        rCam = rObj; rCam.y += rect.height + vSpace;
                        rCam.width = rect.width - rWait.width;
                        rWait.y += rect.height + vSpace;
                    }

                    SerializedProperty property = element.FindPropertyRelative(() => def.m_Object);
                    EditorGUI.PropertyField(rObj, property, GUIContent.none);
                    GameObject eventObj = property.objectReferenceValue as GameObject;
                    if (eventObj)
                    {
                        property = element.FindPropertyRelative(() => def.m_Event);
                        if (DrawEventPopup(rEvent, property, eventObj))
                            Target.InvalidateCache();
                    }

                    property = element.FindPropertyRelative(() => def.m_VirtualCamera);
                    int currentVcam = GetCameraIndex(property.objectReferenceValue);
                    int vcamSelection = EditorGUI.Popup(rCam, currentVcam, mCameraCandidateNames);
                    if (currentVcam != vcamSelection)
                    {
                        property.objectReferenceValue = (vcamSelection < 0)
                            ? null : mCameraCandidates[vcamSelection];
                        Target.InvalidateCache();
                    }

                    float oldWidth = EditorGUIUtility.labelWidth;
                    rWait.width /= 4;
                    EditorGUIUtility.labelWidth = hBigSpace;
                    property = element.FindPropertyRelative(() => def.m_PriorityBoost);
                    EditorGUI.PropertyField(rWait, property, new GUIContent(" ", property.tooltip));
                    rWait.x += rWait.width;
                    property = element.FindPropertyRelative(() => def.m_ActivateAfter);
                    EditorGUI.PropertyField(rWait, property, new GUIContent("s", property.tooltip));
                    rWait.x += rWait.width;
                    property = element.FindPropertyRelative(() => def.m_MinDuration);
                    EditorGUI.PropertyField(rWait, property, new GUIContent("s", property.tooltip));
                    rWait.x += rWait.width;
                    property = element.FindPropertyRelative(() => def.m_MaxDuration);
                    EditorGUI.PropertyField(rWait, property, new GUIContent("s", property.tooltip));
                    EditorGUIUtility.labelWidth = oldWidth;
                };

            mInstructionList.onAddCallback = (ReorderableList l) =>
                {
                    var index = l.serializedProperty.arraySize;
                    ++l.serializedProperty.arraySize;
                    Target.InvalidateCache();
                };

            mInstructionList.onRemoveCallback = (ReorderableList l) =>
                {
                    l.serializedProperty.DeleteArrayElementAtIndex(l.index);
                    Target.InvalidateCache();
                };
        }

        class PopupLists
        {
            public List<string> eventNames;
            public List<string> displayNames;
        }
        Dictionary<GameObject, PopupLists> mPopupCache = new Dictionary<GameObject, PopupLists>();

        PopupLists GetPopupLists(GameObject obj)
        {
            PopupLists lists = null;
            if (!mPopupCache.TryGetValue(obj, out lists))
            {
                List<string> events = ReflectionHelpers.GetAllFieldOfType(typeof(UnityEvent), obj);
                events.Insert(0, string.Empty);
                List<string> displayNames = new List<string>();
                displayNames.InsertRange(0, events);
                for (int i = 1; i < displayNames.Count; ++i)
                {
                    string[] parts = displayNames[i].Split('.');
                    for (int j = 0; j < parts.Length; ++j)
                        parts[j] = InspectorUtility.NicifyClassName(parts[j]);
                    displayNames[i] = string.Join("/", parts);
                }
                displayNames[0] = "(none)";
                lists = new PopupLists { eventNames = events, displayNames = displayNames };
            }
            return lists;
        }

        bool DrawEventPopup(Rect r, SerializedProperty eventProp, GameObject eventObj)
        {
            bool changed = false;
            PopupLists lists = GetPopupLists(eventObj);
            int current = lists.eventNames.IndexOf(eventProp.stringValue);
            int selection = EditorGUI.Popup(r, current, lists.displayNames.ToArray());
            if (selection != current)
            {
                eventProp.stringValue = selection < 0 ? string.Empty : lists.eventNames[selection];
                eventProp.serializedObject.ApplyModifiedProperties();
                changed = true;
            }
            return changed;
        }
    }
}
