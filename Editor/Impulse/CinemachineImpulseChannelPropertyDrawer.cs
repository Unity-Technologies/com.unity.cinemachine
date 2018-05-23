using UnityEngine;
using UnityEditor;

namespace Cinemachine.Editor
{
    [CustomPropertyDrawer(typeof(CinemachineImpulseChannelPropertyAttribute))]
    internal sealed class CinemachineImpulseChannelPropertyDrawer : PropertyDrawer
    {
        const float hSpace = 2;
        GUIContent mAddLabel = new GUIContent("Add...", "Add, remove, or rename channels");
        string[] mLayerList = null;
        int mRangeMask;

        void UpdateLayerList()
        {
            CinemachineImpulseEditorSettings settings = CinemachineImpulseEditorSettings.Instance;
            int numLayers = settings.ImpulseChannels == null ? 0 : settings.ImpulseChannels.Length;
            numLayers = Mathf.Min(31, numLayers);
            if (mLayerList == null || mLayerList.Length != numLayers)
                mLayerList = new string[numLayers];
            mRangeMask = 0;
            for (int i = 0; i < numLayers; ++i)
            {
                mRangeMask |= (1 << i);
                mLayerList[i] = string.Format("{0}: {1}", i, settings.ImpulseChannels[i]);
            }
        }

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            UpdateLayerList();
            float addWidth = GUI.skin.button.CalcSize(mAddLabel).x;
            rect.width -= addWidth + hSpace;
            int value = EditorGUI.MaskField(rect, label, property.intValue & mRangeMask, mLayerList);
            if (value != property.intValue)
            {
                property.intValue  = value;
                property.serializedObject.ApplyModifiedProperties();
            }

            rect.x += rect.width + hSpace; rect.width = addWidth; rect.height -= 1;
            if (GUI.Button(rect, mAddLabel))
                Selection.activeObject = CinemachineImpulseEditorSettings.Instance;
        }
    }
}
