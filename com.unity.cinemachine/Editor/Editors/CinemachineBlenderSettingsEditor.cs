using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

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
                list.RefreshItems();  // rebuild the list
            });

            list.makeItem = () => new BindableElement { style = { flexDirection = FlexDirection.Row }};
            list.bindItem = (row, index) =>
            {
                // Remove children - items get recycled
                for (int i = row.childCount - 1; i >= 0; --i)
                    row.RemoveAt(i);

                var def = new CinemachineBlenderSettings.CustomBlend();
                var element = index < elements.arraySize ? elements.GetArrayElementAtIndex(index) : null;
                if (!IsUnityNull(element))
                {
                    var from = row.AddChild(CreateCameraPopup(element.FindPropertyRelative(() => def.From)));
                    var to = row.AddChild(CreateCameraPopup(element.FindPropertyRelative(() => def.To)));
                    var blend = row.AddChild(new PropertyField(element.FindPropertyRelative(() => def.Blend), ""));
                    FormatElement(false, from, to, blend);

                    ((BindableElement)row).BindProperty(element); // bind must be done at the end
                }
            };

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

            // Local function
            VisualElement CreateCameraPopup(SerializedProperty p)
            {
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, flexGrow = 1 }};
                var textField = row.AddChild(new TextField { isDelayed = true, style = { flexGrow = 1, flexBasis = 20 }});
                textField.BindProperty(p);
                if (availableCameras.FindIndex(x => x == p.stringValue) < 0)
                    row.AddChild(InspectorUtility.MiniHelpIcon("No in-scene camera matches this name"));
                var popup = row.AddChild(InspectorUtility.MiniDropdownButton(
                    "Choose from currently-available cameras", new ContextualMenuManipulator((evt) => 
                {
                    for (int i = 0; i < availableCameras.Count; ++i)
                        evt.menu.AppendAction(availableCameras[i], 
                            (action) => 
                            {
                                p.stringValue = action.name;
                                p.serializedObject.ApplyModifiedProperties();
                            });
                })));
                popup.style.marginRight = 5;
                return row;
            }

            // Local function
            static bool IsUnityNull(object obj)
            {
                // Checks whether an object is null or Unity pseudo-null
                // without having to cast to UnityEngine.Object manually
                return obj == null || ((obj is UnityEngine.Object) && ((UnityEngine.Object)obj) == null);
            }
            return ux;
        }
    }
}
