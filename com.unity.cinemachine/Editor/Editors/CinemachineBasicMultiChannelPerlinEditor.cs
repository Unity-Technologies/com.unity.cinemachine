using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineBasicMultiChannelPerlin))]
    [CanEditMultipleObjects]
    class CinemachineBasicMultiChannelPerlinEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();
            this.AddMissingCmCameraHelpBox(ux);

            var noProfile = ux.AddChild(new HelpBox(
                "A Noise Profile is required.  You may choose from among the NoiseSettings assets defined "
                + "in the project, or from one of the presets.",
                HelpBoxMessageType.Warning));

            var perlin = target as CinemachineBasicMultiChannelPerlin;
            var profileProp = serializedObject.FindProperty(() => perlin.NoiseProfile);
            InspectorUtility.AddRemainingProperties(ux, profileProp);

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
