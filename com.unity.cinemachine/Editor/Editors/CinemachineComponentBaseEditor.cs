using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineComponentBase), true)]
    [CanEditMultipleObjects]
    class CinemachineComponentBaseEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();
            this.AddMissingCmCameraHelpBox(ux);

            // Add children
            var prop = serializedObject.GetIterator();
            if (prop.NextVisible(true))
                InspectorUtility.AddRemainingProperties(ux, prop);

            return ux;
        }
    }
}

