using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;

namespace Unity.Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineBlenderSettings))]
    class CinemachineBlenderSettingsEditor : UnityEditor.Editor
    {
        CinemachineBlenderSettings Target => target as CinemachineBlenderSettings;

        /// <summary>
        /// Called when building the Camera popup menus, to get the domain of possible
        /// cameras.  If no delegate is set, will find all top-level virtual cameras in the scene,
        /// i.e. vcams that are not feeding a specific mixer.
        /// </summary>
        public GetAllVirtualCamerasDelegate GetAllVirtualCameras = GetToplevelCameras;
        public delegate void GetAllVirtualCamerasDelegate(List<CinemachineVirtualCameraBase> list);

        // Get all top-level virtual cameras
        static void GetToplevelCameras(List<CinemachineVirtualCameraBase> list)
        {
            var candidates = Resources.FindObjectsOfTypeAll<CinemachineVirtualCameraBase>();
            for (var i = 0; i < candidates.Length; ++i)
                if (candidates[i].ParentCamera == null)
                    list.Add(candidates[i]);
        }

        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();

            var header = ux.AddChild(new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = -2 } });
            FormatElement(true,
                header.AddChild(new Label("From")), 
                header.AddChild(new Label("To")), 
                header.AddChild(new Label("Blend")));
            header.AddToClassList("unity-collection-view--with-border");

            var list = ux.AddChild(new ListView()
            {
                reorderable = true,
                reorderMode = ListViewReorderMode.Animated,
                showAddRemoveFooter = true,
                showBorder = true,
                showBoundCollectionSize = false,
                showFoldoutHeader = false,
                style = { borderTopWidth = 0, marginLeft = 0 },
            });
            var elements = serializedObject.FindProperty(() => Target.CustomBlends);
            list.BindProperty(elements);

            // Gather the camera candidates
            var availableCameras = new List<string>();
            Dictionary<string, int> cameraIndexLookup = new();
            Action onCamerasUpdated = null;
            list.TrackAnyUserActivity(() =>
            {
                var allCameras = new List<CinemachineVirtualCameraBase>();
                GetAllVirtualCameras(allCameras);
                availableCameras.Clear();
                availableCameras.Add(string.Empty);
                availableCameras.Add(CinemachineBlenderSettings.kBlendFromAnyCameraLabel);
                for (int i = 0; i < allCameras.Count; ++i)
                    if (allCameras[i] != null && !availableCameras.Contains(allCameras[i].Name))
                        availableCameras.Add(allCameras[i].Name);
                onCamerasUpdated?.Invoke();
            });

            list.makeItem = () => 
            {
                var def = new CinemachineBlenderSettings.CustomBlend();
                var row = new BindableElement { style = { flexDirection = FlexDirection.Row }};
                var from = row.AddChild(CreateCameraPopup(SerializedPropertyHelper.PropertyName(() => def.From)));
                var to = row.AddChild(CreateCameraPopup(SerializedPropertyHelper.PropertyName(() => def.To)));
                var blend = row.AddChild(new PropertyField(null, "") { bindingPath = SerializedPropertyHelper.PropertyName(() => def.Blend)});
                FormatElement(false, from, to, blend);
                return row;

                // Local function
                VisualElement CreateCameraPopup(string bindingPath)
                {
                    var container = new VisualElement { style = { flexDirection = FlexDirection.Row, flexGrow = 1 }};
                    var textField = container.AddChild(new TextField { bindingPath = bindingPath, isDelayed = true, style = { flexGrow = 1, flexBasis = 20 }});

                    var warning = container.AddChild(InspectorUtility.MiniHelpIcon($"No in-scene camera matches this name"));
                    textField.RegisterValueChangedCallback((evt) => OnCameraUpdated());
                    onCamerasUpdated += OnCameraUpdated;
                    void OnCameraUpdated()
                    {
                        warning.tooltip = $"No in-scene camera matches \"{textField.value}\"";
                        warning.SetVisible(availableCameras.FindIndex(x => x == textField.value) < 0);
                    };

                    var popup = container.AddChild(InspectorUtility.MiniDropdownButton(
                        "Choose from currently-available cameras", new ContextualMenuManipulator((evt) => 
                    {
                        for (int i = 0; i < availableCameras.Count; ++i)
                            evt.menu.AppendAction(availableCameras[i], (action) => textField.value = action.name);
                    })));
                    popup.style.marginRight = 5;
                    return container;
                }
            };

            return ux;

            // Local function
            static void FormatElement(bool isHeader, VisualElement e1, VisualElement e2, VisualElement e3)
            {
                e1.style.marginLeft = isHeader ? 2 * InspectorUtility.SingleLineHeight - 3: 0;
                e1.style.flexBasis = InspectorUtility.SingleLineHeight; 
                e1.style.flexGrow = 3;
                
                e2.style.flexBasis = 1; 
                e2.style.flexGrow = 3;

                e3.style.flexBasis = 1; 
                e3.style.flexGrow = 2;
            }
        }
    }
}
