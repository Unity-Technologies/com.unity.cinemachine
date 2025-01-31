using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.Cinemachine.Editor
{
    /// <summary>
    /// Inspector for the CinemachineVirtualCameraBase class.  It will be chosen by default
    /// by Unity.  You can inherit from it and customize it.
    /// </summary>
    [CustomEditor(typeof(CinemachineVirtualCameraBase), true, isFallback = true)]
    [CanEditMultipleObjects]
    public class CinemachineVirtualCameraBaseEditor : UnityEditor.Editor
    {
        //protected virtual void OnEnable() => Undo.undoRedoPerformed += ResetTarget;
        //protected virtual void OnDisable() => Undo.undoRedoPerformed -= ResetTarget;

        /// <inheritdoc/>
        public override VisualElement CreateInspectorGUI()
        {
            var vcam = target as CinemachineVirtualCameraBase;
            var ux = new VisualElement();

            this.AddCameraStatus(ux);
            ux.Add(new PropertyField(serializedObject.FindProperty(() => vcam.Priority)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => vcam.OutputChannel)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => vcam.StandbyUpdate)));

            AddInspectorProperties(ux);

            if (vcam is CinemachineCameraManagerBase manager && targets.Length == 1)
            {
                ux.AddSpace();
                this.AddChildCameras(ux, null);
            }
            ux.AddSpace();
            this.AddExtensionsDropdown(ux);
            return ux;
        }

        /// <summary>
        /// Called by the default implementation of CreateInspectorGUI() to populate the inspector with the items that
        /// are not specific to the base class.  Default implementation iterates over the serialized properties and
        /// adds an item for each.
        /// </summary>
        /// <param name="ux">The VisualElement container to which to add items</param>
        protected virtual void AddInspectorProperties(VisualElement ux)
        {
            var p = serializedObject.FindProperty(() => (target as CinemachineVirtualCameraBase).StandbyUpdate);
            if (p.NextVisible(false))
                InspectorUtility.AddRemainingProperties(ux, p);
        }
    }
}
