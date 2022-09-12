    using UnityEditor;
    using System.Collections.Generic;

namespace Cinemachine.PostFX.Editor
{
    [CustomEditor(typeof(CinemachineAutoFocus))]
    sealed class CinemachineAutoFocusEditor : Cinemachine.Editor.BaseEditor<CinemachineAutoFocus>
    {
#if !CINEMACHINE_HDRP
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox(
                "This component is only valid for HDRP projects.",
                MessageType.Warning);
        }
#else
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox(
                "Note: focus control requires an active Volume containing a Depth Of Field override "
                    + "having Focus Mode activated and set to Physical Camera, "
                    + "and Focus Distance Mode activated and set to Camera",
                MessageType.Info);

            base.OnInspectorGUI();
        }

        /// <summary>Get the property names to exclude in the inspector.</summary>
        /// <param name="excluded">Add the names to this list</param>
        protected override void GetExcludedPropertiesInInspector(List<string> excluded)
        {
            base.GetExcludedPropertiesInInspector(excluded);
            var focusTargetProp  = FindProperty(x => x.FocusTarget);
            var mode = (CinemachineAutoFocus.FocusTrackingMode)focusTargetProp.intValue;
            if (mode != CinemachineAutoFocus.FocusTrackingMode.CustomTarget)
                excluded.Add(FieldPath(x => x.CustomTarget));
            if (mode != CinemachineAutoFocus.FocusTrackingMode.ScreenCenter)
                excluded.Add(FieldPath(x => x.AutoDetectionRadius));
            if (mode == CinemachineAutoFocus.FocusTrackingMode.None)
            {
                excluded.Add(FieldPath(x => x.FocusOffset));
                excluded.Add(FieldPath(x => x.Damping));
            }
        }
#endif
    }
}
