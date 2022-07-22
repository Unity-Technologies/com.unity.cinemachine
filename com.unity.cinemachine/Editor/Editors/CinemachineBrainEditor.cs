﻿using UnityEngine;
using UnityEditor;
using Cinemachine.Utility;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Cinemachine.Editor
{
    /// <summary>
    /// Inspector for CinemachineBrain
    /// </summary>
    [CustomEditor(typeof(CinemachineBrain))]
    [CanEditMultipleObjects]
    class CinemachineBrainEditor : UnityEditor.Editor
    {
        CinemachineBrain Target => target as CinemachineBrain;

        EmbeddeAssetEditor<CinemachineBlenderSettings> m_BlendsEditor;
        ObjectField m_LiveCamera;
        TextField m_LiveBlend;
        bool m_EventsExpanded = false;

        void OnEnable()
        {
            m_BlendsEditor = new EmbeddeAssetEditor<CinemachineBlenderSettings>();
            EditorApplication.update += UpdateVisibility;
        }

        void OnDisable()
        {
            if (m_BlendsEditor != null)
                m_BlendsEditor.OnDisable();
            EditorApplication.update -= UpdateVisibility;
        }

        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();

            // Show the active camera and blend
            var row = ux.AddChild(new InspectorUtility.LeftRightContainer());
            row.Left.Add(new Label("Live Camera")
                { tooltip = "The Cm Camera that is currently active", style = { alignSelf = Align.Center, flexGrow = 1 }});
            m_LiveCamera = row.Right.AddChild(new ObjectField("") 
                { objectType = typeof(CinemachineVirtualCameraBase), style = { flexGrow = 1 }});
            row.SetEnabled(false);

            row = ux.AddChild(new InspectorUtility.LeftRightContainer());
            row.Left.Add(new Label("Live Blend")
                { tooltip = "The state of currently active blend, if any", style = { alignSelf = Align.Center, flexGrow = 1 }});
            m_LiveBlend = row.Right.AddChild(new TextField("") { style = { flexGrow = 1 }});
            row.SetEnabled(false);

            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.ShowDebugText)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.ShowCameraFrustum)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.IgnoreTimeScale)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.WorldUpOverride)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.UpdateMethod)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.BlendUpdateMethod)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.LensModeOverride)));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.DefaultBlend)));
            ux.Add(m_BlendsEditor.CreateInspectorGUI(
                serializedObject.FindProperty(() => Target.CustomBlends),
                "Create New Blender Asset", Target.gameObject.name + " Blends", "asset", string.Empty));

            var foldout = ux.AddChild(new Foldout 
            { 
                text = "Events", 
                tooltip = "Add handlers for these events, if desired", 
                value = m_EventsExpanded 
            });
            foldout.RegisterValueChangedCallback((evt) => 
            {
                m_EventsExpanded = evt.newValue;
                evt.StopPropagation();
            });
            foldout.Add(new PropertyField(serializedObject.FindProperty(() => Target.CameraCutEvent)));
            foldout.Add(new PropertyField(serializedObject.FindProperty(() => Target.CameraActivatedEvent)));

            return ux;
        }

        void UpdateVisibility()
        {
            if (Target == null || m_LiveCamera == null)
                return;
            m_LiveCamera.value = Target.ActiveVirtualCamera as CinemachineVirtualCameraBase;
            m_LiveBlend.value = Target.ActiveBlend != null ? Target.ActiveBlend.Description : string.Empty;
        }

        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected, typeof(CinemachineBrain))]
        static void DrawBrainGizmos(CinemachineBrain brain, GizmoType drawType)
        {
            if (brain.OutputCamera != null && brain.ShowCameraFrustum && brain.isActiveAndEnabled)
            {
                DrawCameraFrustumGizmo(
                    LensSettings.FromCamera(brain.OutputCamera),
                    brain.transform.localToWorldMatrix,
                    Color.white); // GML why is this color hardcoded?
            }
        }

        /// <summary>Draw the gizmo for a virtual camera in the scene view</summary>
        /// <param name="vcam">The virtual camera</param>
        /// <param name="selectionType">How the object is selected</param>
        [DrawGizmo(GizmoType.Active | GizmoType.InSelectionHierarchy | GizmoType.Pickable, typeof(CinemachineVirtualCameraBase))]
        public static void DrawVirtualCameraBaseGizmos(CinemachineVirtualCameraBase vcam, GizmoType selectionType)
        {
            const string kGizmoFileName = "Packages/com.unity.cinemachine/Gizmos/cm_logo.png";

            // Don't draw gizmos on hidden stuff
            if ((vcam.gameObject.hideFlags & (HideFlags.HideInHierarchy | HideFlags.HideInInspector)) != 0)
                return;

            if (vcam.ParentCamera != null && (selectionType & GizmoType.Active) == 0)
                return;

            var state = vcam.State;
            Gizmos.DrawIcon(state.GetFinalPosition(), kGizmoFileName, true);

            DrawCameraFrustumGizmo(
                state.Lens,
                Matrix4x4.TRS(state.GetFinalPosition(), state.GetFinalOrientation().normalized, Vector3.one),
                CinemachineCore.Instance.IsLive(vcam)
                    ? CinemachineSettings.CinemachineCoreSettings.ActiveGizmoColour
                    : CinemachineSettings.CinemachineCoreSettings.InactiveGizmoColour);
        }

        public static void DrawCameraFrustumGizmo(LensSettings lens, Matrix4x4 transform, Color color)
        {
            var aspect = lens.Aspect;
            var ortho = lens.Orthographic;

            var originalMatrix = Gizmos.matrix;
            var originalGizmoColour = Gizmos.color;
            
            Gizmos.color = color;
            Gizmos.matrix = transform;
            if (ortho)
            {
                var size = new Vector3(
                    aspect * lens.OrthographicSize * 2,
                    lens.OrthographicSize * 2,
                    lens.FarClipPlane - lens.NearClipPlane);
                Gizmos.DrawWireCube(new Vector3(0, 0, (size.z / 2) + lens.NearClipPlane), size);
            }
            else
            {
                Gizmos.DrawFrustum(
                        Vector3.zero, lens.FieldOfView,
                        lens.FarClipPlane, lens.NearClipPlane, aspect);
            }
            Gizmos.matrix = originalMatrix;
            Gizmos.color = originalGizmoColour;
        }
    }
}
