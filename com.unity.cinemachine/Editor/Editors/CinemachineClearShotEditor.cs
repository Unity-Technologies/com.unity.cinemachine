using UnityEditor;
using UnityEngine;

namespace Cinemachine.Editor
{
#if CINEMACHINE_PHYSICS
    [CustomEditor(typeof(CinemachineClearShot))]
    [CanEditMultipleObjects]
    class CinemachineClearShotEditor : CinemachineVirtualCameraBaseEditor<CinemachineClearShot>
    {
        EmbeddeAssetEditor<CinemachineBlenderSettings> m_BlendsEditor;
        ColliderState m_ColliderState;

        private UnityEditorInternal.ReorderableList m_ChildList;

        protected override void OnEnable()
        {
            base.OnEnable();
            m_BlendsEditor = new EmbeddeAssetEditor<CinemachineBlenderSettings>
            {
                OnChanged = (CinemachineBlenderSettings b) => InspectorUtility.RepaintGameView(),
                OnCreateEditor = (UnityEditor.Editor ed) =>
                {
                    var editor = ed as CinemachineBlenderSettingsEditor;
                    if (editor != null)
                        editor.GetAllVirtualCameras = (list) => list.AddRange(Target.ChildCameras);
                }
            };
            m_ChildList = null;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (m_BlendsEditor != null)
                m_BlendsEditor.OnDisable();
        }

        public override void OnInspectorGUI()
        {
            BeginInspector();
            if (m_ChildList == null)
                SetupChildList();

            m_ColliderState = GetColliderState();
            switch (m_ColliderState)
            {
                case ColliderState.ColliderOnParent:
                case ColliderState.ColliderOnAllChildren:
                    break;
                case ColliderState.NoCollider:
                    EditorGUILayout.HelpBox(
                        "ClearShot requires a Collider extension to rank the shots.  "
                            + "Either add one to the ClearShot itself, or to each of the child cameras.",
                        MessageType.Warning);
                    break;
                case ColliderState.ColliderOnSomeChildren:
                    EditorGUILayout.HelpBox(
                        "Some child cameras do not have a Collider extension.  ClearShot requires a "
                            + "Collider on all the child cameras, or alternatively on the ClearShot iself.",
                        MessageType.Warning);
                    break;
                case ColliderState.ColliderOnChildrenAndParent:
                    EditorGUILayout.HelpBox(
                        "There is a Collider extension on the ClearShot camera, and also on some "
                            + "of its child cameras.  You can't have both.",
                        MessageType.Error);
                    break;
            }

            var children = Target.ChildCameras; // force the child cache to rebuild

            DrawCameraStatusInInspector();
            DrawGlobalControlsInInspector();
            DrawPropertyInInspector(FindProperty(x => x.CameraPriority));
            DrawPropertyInInspector(FindProperty(x => x.DefaultTarget));
            DrawRemainingPropertiesInInspector();

            // Blends
            m_BlendsEditor.DrawEditorCombo(
                FindProperty(x => x.CustomBlends),
                "Create New Blender Asset",
                Target.gameObject.name + " Blends", "asset", string.Empty, false);

            // vcam children
            EditorGUILayout.Separator();

            if (Selection.objects.Length == 1)
            {
                EditorGUI.BeginChangeCheck();
                m_ChildList.DoLayoutList();
                if (EditorGUI.EndChangeCheck())
                    serializedObject.ApplyModifiedProperties();
            }
            else
            {
                EditorGUILayout.HelpBox(Styles.virtualCameraChildrenInfoMsg.text, MessageType.Info);
            }
            
            // Extensions
            DrawExtensionsWidgetInInspector();
        }

        enum ColliderState
        {
            NoCollider,
            ColliderOnAllChildren,
            ColliderOnSomeChildren,
            ColliderOnParent,
            ColliderOnChildrenAndParent
        }

        ColliderState GetColliderState()
        {
            int numColliderChildren = 0;
            bool colliderOnParent = ObjectHasCollider(Target);

            var children = Target.ChildCameras;
            var numChildren = children == null ? 0 : children.Count;
            for (int i = 0; i < numChildren; ++i)
                if (ObjectHasCollider(children[i]))
                    ++numColliderChildren;
            if (colliderOnParent)
                return (numColliderChildren > 0)
                    ? ColliderState.ColliderOnChildrenAndParent : ColliderState.ColliderOnParent;
            if (numColliderChildren > 0)
                return (numColliderChildren == numChildren)
                    ? ColliderState.ColliderOnAllChildren : ColliderState.ColliderOnSomeChildren;
            return ColliderState.NoCollider;
        }

        bool ObjectHasCollider(object obj)
        {
            CinemachineVirtualCameraBase vcam = obj as CinemachineVirtualCameraBase;
            var collider = (vcam == null) ? null : vcam.GetComponent<CinemachineCollider>();
            return (collider != null && collider.enabled);
        }

        void SetupChildList()
        {
            float vSpace = 2;
            float hSpace = 3;
            float floatFieldWidth = EditorGUIUtility.singleLineHeight * 2.5f;

            m_ChildList = new UnityEditorInternal.ReorderableList(
                    serializedObject, FindProperty(x => x.m_ChildCameras), true, true, true, true);

            m_ChildList.drawHeaderCallback = (Rect rect) =>
                {
                    EditorGUI.LabelField(rect, "Virtual Camera Children");
                    GUIContent priorityText = new GUIContent("Priority");
                    var textDimensions = GUI.skin.label.CalcSize(priorityText);
                    rect.x += rect.width - textDimensions.x;
                    rect.width = textDimensions.x;
                    EditorGUI.LabelField(rect, priorityText);
                };
            m_ChildList.drawElementCallback
                = (Rect rect, int index, bool isActive, bool isFocused) =>
                {
                    rect.y += vSpace;
                    rect.width -= floatFieldWidth + hSpace;
                    rect.height = EditorGUIUtility.singleLineHeight;
                    SerializedProperty element = m_ChildList.serializedProperty.GetArrayElementAtIndex(index);
                    if (m_ColliderState == ColliderState.ColliderOnSomeChildren
                        || m_ColliderState == ColliderState.ColliderOnChildrenAndParent)
                    {
                        bool hasCollider = ObjectHasCollider(element.objectReferenceValue);
                        if ((m_ColliderState == ColliderState.ColliderOnSomeChildren && !hasCollider)
                            || (m_ColliderState == ColliderState.ColliderOnChildrenAndParent && hasCollider))
                        {
                            float width = rect.width;
                            rect.width = rect.height;
                            GUIContent label = new GUIContent("");
                            label.image = EditorGUIUtility.IconContent("console.warnicon.sml").image;
                            EditorGUI.LabelField(rect, label);
                            width -= rect.width; rect.x += rect.width; rect.width = width;
                        }
                    }
                    GUI.enabled = false;
                    EditorGUI.PropertyField(rect, element, GUIContent.none);
                    GUI.enabled = true;

                    SerializedObject obj = new SerializedObject(element.objectReferenceValue);
                    rect.x += rect.width + hSpace; rect.width = floatFieldWidth;
                    SerializedProperty priorityProp = obj.FindProperty(() => Target.CameraPriority).FindPropertyRelative("Priority");
                    float oldWidth = EditorGUIUtility.labelWidth;
                    EditorGUIUtility.labelWidth = hSpace * 2;
                    EditorGUI.PropertyField(rect, priorityProp, new GUIContent(" "));
                    EditorGUIUtility.labelWidth = oldWidth;
                    obj.ApplyModifiedProperties();
                };
            m_ChildList.onChangedCallback = (UnityEditorInternal.ReorderableList l) =>
                {
                    if (l.index < 0 || l.index >= l.serializedProperty.arraySize)
                        return;
                    Object o = l.serializedProperty.GetArrayElementAtIndex(
                            l.index).objectReferenceValue;
                    CinemachineVirtualCameraBase vcam = (o != null)
                        ? (o as CinemachineVirtualCameraBase) : null;
                    if (vcam != null)
                        vcam.transform.SetSiblingIndex(l.index);
                };
            m_ChildList.onAddCallback = (UnityEditorInternal.ReorderableList l) =>
                {
                    var index = l.serializedProperty.arraySize;
                    var vcam = CinemachineMenu.CreateDefaultVirtualCamera(parentObject: Target.gameObject);
                    var collider = Undo.AddComponent<CinemachineCollider>(vcam.gameObject);
                    collider.AvoidObstacles = false;
                    Undo.RecordObject(collider, "create ClearShot child");
                    vcam.transform.SetSiblingIndex(index);
                };
            m_ChildList.onRemoveCallback = (UnityEditorInternal.ReorderableList l) =>
                {
                    Object o = l.serializedProperty.GetArrayElementAtIndex(
                            l.index).objectReferenceValue;
                    CinemachineVirtualCameraBase vcam = (o != null)
                        ? (o as CinemachineVirtualCameraBase) : null;
                    if (vcam != null)
                        Undo.DestroyObjectImmediate(vcam.gameObject);
                };
        }
    }
#endif
}
