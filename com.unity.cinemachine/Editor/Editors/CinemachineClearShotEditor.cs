using UnityEditor;
using UnityEngine;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineClearShot))]
    [CanEditMultipleObjects]
    class CinemachineClearShotEditor : CinemachineVirtualCameraBaseEditor<CinemachineClearShot>
    {
        EmbeddeAssetEditor<CinemachineBlenderSettings> m_BlendsEditor;
        ChildListInspectorHelper m_ChildListHelper = new();
        EvaluatorState m_EvaluatorState;

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
            m_ChildListHelper.OnEnable();
            m_ChildListHelper.GetChildWarningMessage = GetChildWarningMessage;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (m_BlendsEditor != null)
                m_BlendsEditor.OnDisable();
        }


        static string GetAvailableQualityEvaluatorNames()
        {
            var names = InspectorUtility.GetAssignableBehaviourNames(typeof(IShotQualityEvaluator));
            if (names == InspectorUtility.s_NoneString)
                return "No Shot Quality Evaluator extensions are available.  This might be because the "
                    + "physics module is disabled and all Shot Quality Evaluator implementations "
                    + "depend on physics raycasts";
            return "Available Shot Quality Evaluators are: " + names;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            m_EvaluatorState = GetEvaluatorState();
            switch (m_EvaluatorState)
            {
                case EvaluatorState.EvaluatorOnParent:
                case EvaluatorState.EvaluatorOnAllChildren:
                    break;
                case EvaluatorState.NoEvaluator:
                    EditorGUILayout.HelpBox(
                        "ClearShot requires a Shot Quality Evaluator extension to rank the shots.  "
                            + "Either add one to the ClearShot itself, or to each of the child cameras.  "
                            + GetAvailableQualityEvaluatorNames(),
                        MessageType.Warning);
                    break;
                case EvaluatorState.EvaluatorOnSomeChildren:
                    EditorGUILayout.HelpBox(
                        "Some child cameras do not have a Shot Quality Evaluator extension.  ClearShot requires a "
                            + "Shot Quality Evaluator on all the child cameras, or alternatively on the ClearShot iself.  "
                            + GetAvailableQualityEvaluatorNames(),
                        MessageType.Warning);
                    break;
                case EvaluatorState.EvaluatorOnChildrenAndParent:
                    EditorGUILayout.HelpBox(
                        "There is a Shot Quality Evaluator extension on the ClearShot camera, and also on some "
                            + "of its child cameras.  You can't have both.  " + GetAvailableQualityEvaluatorNames(),
                        MessageType.Error);
                    break;
            }

            var children = Target.ChildCameras; // force the child cache to rebuild

            DrawStandardInspectorTopSection();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serializedObject.FindProperty(() => Target.DefaultTarget));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(() => Target.ActivateAfter));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(() => Target.MinDuration));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(() => Target.RandomizeChoice));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(() => Target.DefaultBlend));
            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();

            // Blends
            m_BlendsEditor.DrawEditorCombo(
                serializedObject.FindProperty(() => Target.CustomBlends),
                "Create New Blender Asset",
                Target.gameObject.name + " Blends", "asset", string.Empty, false);

            // vcam children
            EditorGUILayout.Separator();
            m_ChildListHelper.OnInspectorGUI(serializedObject.FindProperty(() => Target.m_ChildCameras));

            // Extensions
            DrawExtensionsWidgetInInspector();
        }

        enum EvaluatorState
        {
            NoEvaluator,
            EvaluatorOnAllChildren,
            EvaluatorOnSomeChildren,
            EvaluatorOnParent,
            EvaluatorOnChildrenAndParent
        }

        EvaluatorState GetEvaluatorState()
        {
            int numEvaluatorChildren = 0;
            bool colliderOnParent = ObjectHasEvaluator(Target);

            var children = Target.ChildCameras;
            var numChildren = children == null ? 0 : children.Count;
            for (var i = 0; i < numChildren; ++i)
                if (ObjectHasEvaluator(children[i]))
                    ++numEvaluatorChildren;
            if (colliderOnParent)
                return numEvaluatorChildren > 0
                    ? EvaluatorState.EvaluatorOnChildrenAndParent : EvaluatorState.EvaluatorOnParent;
            if (numEvaluatorChildren > 0)
                return numEvaluatorChildren == numChildren
                    ? EvaluatorState.EvaluatorOnAllChildren : EvaluatorState.EvaluatorOnSomeChildren;
            return EvaluatorState.NoEvaluator;
        }

        bool ObjectHasEvaluator(object obj)
        {
            var vcam = obj as CinemachineVirtualCameraBase;
            if (vcam != null && vcam.TryGetComponent<IShotQualityEvaluator>(out var evaluator))
            {
                var b = evaluator as MonoBehaviour;
                return b != null && b.enabled;
            }
            return false;
        }

        string GetChildWarningMessage(object obj)
        {
            if (m_EvaluatorState == EvaluatorState.EvaluatorOnSomeChildren
                || m_EvaluatorState == EvaluatorState.EvaluatorOnChildrenAndParent)
            {
                bool hasEvaluator = ObjectHasEvaluator(obj);
                if (m_EvaluatorState == EvaluatorState.EvaluatorOnSomeChildren && !hasEvaluator)
                    return "This camera has no shot quality evaluator";
                if (m_EvaluatorState == EvaluatorState.EvaluatorOnChildrenAndParent && hasEvaluator)
                    return "There are multiple shot quality evaluators on this camera";
            }
            return "";
        }
    }
}
