using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineClearShot))]
    [CanEditMultipleObjects]
    class CinemachineClearShotEditor : CinemachineVirtualCameraBaseEditor
    {
        CinemachineClearShot Target => target as CinemachineClearShot;
        EvaluatorState m_EvaluatorState;

        static string GetAvailableQualityEvaluatorNames()
        {
            var names = InspectorUtility.GetAssignableBehaviourNames(typeof(IShotQualityEvaluator));
            if (names == InspectorUtility.s_NoneString)
                return "No Shot Quality Evaluator extensions are available.  This might be because the "
                    + "physics module is disabled and all Shot Quality Evaluator implementations "
                    + "depend on physics raycasts";
            return "Available Shot Quality Evaluators are: " + names;
        }

        protected override void AddInspectorProperties(VisualElement ux)
        {
            ux.AddHeader("Global Settings");
            this.AddGlobalControls(ux);

            var helpBox = ux.AddChild(new HelpBox());

            ux.AddHeader("Clear Shot");
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.DefaultTarget)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.ActivateAfter)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.MinDuration)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.RandomizeChoice)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.DefaultBlend)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.CustomBlends)));

            ux.TrackAnyUserActivity(() =>
            {
                if (Target == null)
                    return; // object deleted
                m_EvaluatorState = GetEvaluatorState();
                switch (m_EvaluatorState)
                {
                    case EvaluatorState.EvaluatorOnParent:
                    case EvaluatorState.EvaluatorOnAllChildren:
                        helpBox.SetVisible(false);
                        break;
                    case EvaluatorState.NoEvaluator:
                        helpBox.text = "ClearShot requires a Shot Quality Evaluator extension to rank the shots.  "
                            + "Either add one to the ClearShot itself, or to each of the child cameras.  "
                            + GetAvailableQualityEvaluatorNames();
                        helpBox.messageType = HelpBoxMessageType.Warning;
                        helpBox.SetVisible(true);
                        break;
                    case EvaluatorState.EvaluatorOnSomeChildren:
                        helpBox.text = "Some child cameras do not have a Shot Quality Evaluator extension.  "
                            + "ClearShot requires a Shot Quality Evaluator on all the child cameras, or "
                            + "alternatively on the ClearShot iself.  "
                            + GetAvailableQualityEvaluatorNames();
                        helpBox.messageType = HelpBoxMessageType.Warning;
                        helpBox.SetVisible(true);
                        break;
                    case EvaluatorState.EvaluatorOnChildrenAndParent:
                        helpBox.text = "There is a Shot Quality Evaluator extension on the ClearShot camera, "
                            + "and also on some of its child cameras.  You can't have both.  " 
                            + GetAvailableQualityEvaluatorNames();
                        helpBox.messageType = HelpBoxMessageType.Error;
                        helpBox.SetVisible(true);
                        break;
                }
            });
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
