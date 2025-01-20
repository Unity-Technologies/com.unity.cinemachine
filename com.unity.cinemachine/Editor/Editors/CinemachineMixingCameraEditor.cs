using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Unity.Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineMixingCamera))]
    class CinemachineMixingCameraEditor : CinemachineVirtualCameraBaseEditor
    {
        CinemachineMixingCamera Target => target as CinemachineMixingCamera;

        GUIStyle m_MixResultStyle;

        static string WeightPropertyName(int i) => "Weight" + i;

        protected override void AddInspectorProperties(VisualElement ux)
        {
            ux.AddHeader("Global Settings");
            this.AddGlobalControls(ux);

            ux.AddSpace();
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.DefaultTarget)));

            ux.AddHeader("Child Camera Weights");
            List<PropertyField> weights = new ();
            for (int i = 0; i < CinemachineMixingCamera.MaxCameras; ++i)
                weights.Add(ux.AddChild(new PropertyField(serializedObject.FindProperty(WeightPropertyName(i)))));

            var noChildrenHelp = ux.AddChild(new HelpBox("There are no CinemachineCamera children", HelpBoxMessageType.Warning));
            var noWeightHelp = ux.AddChild(new HelpBox("No input channels are active", HelpBoxMessageType.Warning));

            var container = ux.AddChild(new VisualElement());
            container.AddHeader("Mix Result");
            container.Add(new IMGUIContainer(() =>
            {
                float totalWeight = 0;
                var children = Target.ChildCameras;
                int numCameras = Mathf.Min(CinemachineMixingCamera.MaxCameras, children.Count);
                for (int i = 0; i < numCameras; ++i)
                    if (children[i].isActiveAndEnabled)
                        totalWeight += Target.GetWeight(i);
                DrawProportionIndicator(children, numCameras, totalWeight);
            }));

            ux.TrackAnyUserActivity(() =>
            {
                if (Target == null)
                    return; // object deleted
                var children = Target.ChildCameras;
                int numCameras = Mathf.Min(CinemachineMixingCamera.MaxCameras, children.Count);
                noChildrenHelp.SetVisible(numCameras == 0);
                float totalWeight = 0;
                for (int i = 0; totalWeight == 0 && i < numCameras; ++i)
                    if (children[i].isActiveAndEnabled)
                        totalWeight += Target.GetWeight(i);
                noWeightHelp.SetVisible(numCameras > 0 && totalWeight == 0);
                container.SetVisible(totalWeight > 0);

                for (int i = 0; i < weights.Count; ++i)
                    weights[i].SetVisible(i < numCameras);
            });
        }

        void DrawProportionIndicator(
            List<CinemachineVirtualCameraBase> children, int numCameras, float totalWeight)
        {
            if (m_MixResultStyle == null)
            {
                m_MixResultStyle = new GUIStyle(EditorStyles.miniLabel);
                m_MixResultStyle.alignment = TextAnchor.MiddleCenter;
            }

            Color bkg = new Color(0.27f, 0.27f, 0.27f); // ack! no better way than this?
            Color fg = Color.Lerp(CinemachineCore.SoloGUIColor(), bkg, 0.8f);
            float totalHeight = (m_MixResultStyle.lineHeight + m_MixResultStyle.margin.vertical) * numCameras;
            Rect r = EditorGUILayout.GetControlRect(true, totalHeight);
            r.height /= numCameras; r.height -= 1;
            float fullWidth = r.width;
            for (int i = 0; i < numCameras; ++i)
            {
                float p = 0;
                string label = children[i].Name;
                if (totalWeight > UnityVectorExtensions.Epsilon)
                {
                    if (children[i].isActiveAndEnabled)
                        p = Target.GetWeight(i) / totalWeight;
                    else
                        label += " (disabled)";
                }
                r.width = fullWidth * p;
                EditorGUI.DrawRect(r, fg);

                Rect r2 = r;
                r2.x += r.width;
                r2.width = fullWidth - r.width;
                EditorGUI.DrawRect(r2, bkg);

                r.width = fullWidth;
                EditorGUI.LabelField(r, label, m_MixResultStyle);

                r.y += r.height + 1;
            }
        }
    }
}
