#if CINEMACHINE_UGUI
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Unity.Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineStoryboard))]
    [CanEditMultipleObjects]
    class CinemachineStoryboardEditor : UnityEditor.Editor
    {
        CinemachineStoryboard Target => target as CinemachineStoryboard;
        static bool s_AdvancedFoldout;

        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();
            this.AddMissingCmCameraHelpBox(ux);

            // Global mute
            var muteToggle = ux.AddChild(new Toggle(CinemachineCorePrefs.s_StoryboardGlobalMuteLabel.text)
            {
                tooltip = CinemachineCorePrefs.s_StoryboardGlobalMuteLabel.tooltip,
                value = CinemachineStoryboard.s_StoryboardGlobalMute
            });
            muteToggle.AddToClassList(InspectorUtility.AlignFieldClassName);
            muteToggle.RegisterValueChangedCallback((evt) =>
            {
                CinemachineStoryboard.s_StoryboardGlobalMute = CinemachineCorePrefs.StoryboardGlobalMute.Value = evt.newValue;
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            });

            // Image
            var showImageProp = serializedObject.FindProperty(() => Target.ShowImage);
            var row = ux.AddChild(new InspectorUtility.LabeledRow("Show Image", showImageProp.tooltip));
            var imageToggle = row.Contents.AddChild(new Toggle("")
                { style = { flexGrow = 0, marginTop = 3, marginLeft = 3, alignSelf = Align.Center }});
            imageToggle.BindProperty(showImageProp);
            var imageField = row.Contents.AddChild(new PropertyField(serializedObject.FindProperty(() => Target.Image), "")
                { style = { flexGrow = 1, marginLeft = 4 }});
            imageField.TrackPropertyWithInitialCallback(showImageProp, (p) => imageField.SetVisible(p.boolValue));

            ux.AddChild(new PropertyField(serializedObject.FindProperty(() => Target.Aspect)));
            ux.AddChild(new PropertyField(serializedObject.FindProperty(() => Target.Alpha)));
            ux.AddChild(new PropertyField(serializedObject.FindProperty(() => Target.Center)));
            ux.AddChild(new PropertyField(serializedObject.FindProperty(() => Target.Rotation)));

            // Scale
            var scaleProp = serializedObject.FindProperty(() => Target.Scale);
            row = ux.AddChild(new InspectorUtility.LabeledRow(scaleProp.displayName, scaleProp.tooltip));
            var scaleField = row.Contents.AddChild(new PropertyField(scaleProp, "") { style = { flexBasis = 50, flexGrow = 2 }});
            var syncProp = serializedObject.FindProperty(() => Target.SyncScale);
            var syncField = row.Contents.AddChild(new Toggle("")
                { text = " Sync", style = { flexBasis = 20, flexGrow = 1, paddingLeft = 1, paddingTop = 2 }});
            syncField.BindProperty(syncProp);
            scaleField.TrackPropertyWithInitialCallback(syncProp, (p) =>
            {
                var f = scaleField.Q<FloatField>("unity-y-input");
                f?.SetVisible(!p.boolValue);
            });

            ux.AddChild(new PropertyField(serializedObject.FindProperty(() => Target.MuteCamera)));
            var splitField = ux.AddChild(new PropertyField(serializedObject.FindProperty(() => Target.SplitView)));
            splitField.RegisterValueChangeCallback((evt) => WaveformWindow.RefreshNow());

            var waveformRow = ux.AddChild(new InspectorUtility.LeftRightRow() { style = { marginTop = 2 }});
            waveformRow.Left.Add(new Label("Waveform Monitor"));
            waveformRow.Right.AddChild(new Button(() => WaveformWindow.OpenWindow()) { text ="Open", style = { flexBasis = 100 }});

            // Advanced
            var advanced = ux.AddChild(new Foldout() { text = "Advanced", value = s_AdvancedFoldout });
            advanced.RegisterValueChangedCallback((evt) => s_AdvancedFoldout = evt.newValue);
            var renderModeProp = serializedObject.FindProperty(() => Target.RenderMode);
            advanced.AddChild(new PropertyField(renderModeProp));
            advanced.AddChild(new PropertyField(serializedObject.FindProperty(() => Target.SortingOrder)));
            var planeDistacneField = advanced.AddChild(new PropertyField(serializedObject.FindProperty(() => Target.PlaneDistance)));
            planeDistacneField.TrackPropertyWithInitialCallback(
                renderModeProp, (p) => planeDistacneField.SetVisible(p.intValue == (int)RenderMode.ScreenSpaceCamera));

            return ux;
        }
    }
}
#endif
