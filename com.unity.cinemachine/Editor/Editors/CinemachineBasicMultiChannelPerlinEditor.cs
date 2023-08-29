using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineBasicMultiChannelPerlin))]
    [CanEditMultipleObjects]
    class CinemachineBasicMultiChannelPerlinEditor : UnityEditor.Editor
    {
        CinemachineBasicMultiChannelPerlin Target => target as CinemachineBasicMultiChannelPerlin;

        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();

            this.AddMissingCmCameraHelpBox(ux);

            var noProfile = ux.AddChild(new HelpBox(
                "A Noise Profile is required.  You may choose from among the NoiseSettings assets defined "
                + "in the project, or from one of the presets.", 
                HelpBoxMessageType.Warning));

            var profileProp = serializedObject.FindProperty(() => Target.NoiseProfile);
            ux.Add(new PropertyField(profileProp));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.PivotOffset)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.AmplitudeGain)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.FrequencyGain)));

            var row = ux.AddChild(new InspectorUtility.LeftRightRow());
            row.Right.Add(new Button(() =>
            {
                for (int i = 0; i < targets.Length; ++i)
                    (targets[i] as CinemachineBasicMultiChannelPerlin).ReSeed();
            }) { text = "New random seed", style = { flexGrow = 0 }});

            ux.TrackPropertyWithInitialCallback(profileProp, (p) => noProfile.SetVisible(p.objectReferenceValue == null));

            return ux;
        }
    }
}
