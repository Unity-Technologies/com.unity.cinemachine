using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Unity.Cinemachine.Editor
{
    /// <summary>
    /// Inspector for CinemachineBrain
    /// </summary>
    [CustomEditor(typeof(CinemachineBrain))]
    [CanEditMultipleObjects]
    class CinemachineBrainEditor : UnityEditor.Editor
    {
        CinemachineBrain Target => target as CinemachineBrain;

        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();

            var brainModesWarning = ux.AddChild(new HelpBox("The <b>Fixed Update</b> setting of Blend Update Method is intended "
                + "to be used only when the Update Method is also <b>Fixed Update</b>, to address specific blending issues.\n"
                + "<b>Late Update</b> is usually the recommended setting.", HelpBoxMessageType.Warning));

            // Show the active camera and blend
            var row = ux.AddChild(new InspectorUtility.LeftRightRow());
            row.Left.Add(new Label("Live Camera")
                { tooltip = "The Cm Camera that is currently active", style = { alignSelf = Align.Center, flexGrow = 1 }});
            var liveCamera = row.Right.AddChild(new ObjectField()
                { objectType = typeof(CinemachineVirtualCameraBase), style = { flexGrow = 1, flexShrink = 1 }});
            row.SetEnabled(false);

            row = ux.AddChild(new InspectorUtility.LeftRightRow());
            row.Left.Add(new Label("Live Blend")
                { tooltip = "The state of currently active blend, if any", style = { alignSelf = Align.Center, flexGrow = 1 }});
            var liveBlend = row.Right.AddChild(new TextField() { style = { flexGrow = 1, flexShrink = 1, height = InspectorUtility.SingleLineHeight }});
            row.SetEnabled(false);

            var prop = serializedObject.GetIterator();
            if (prop.NextVisible(true))
                InspectorUtility.AddRemainingProperties(ux, prop);

            ux.ContinuousUpdate(() =>
            {
                if (target == null || liveCamera == null)
                    return;
                liveCamera.value = Target.ActiveVirtualCamera as CinemachineVirtualCameraBase;
                liveBlend.value = Target.ActiveBlend != null ? Target.ActiveBlend.Description : string.Empty;
            });
            ux.TrackAnyUserActivity(() =>
            {
                if (target == null)
                    return;
                brainModesWarning.SetVisible(Target.BlendUpdateMethod == CinemachineBrain.BrainUpdateMethods.FixedUpdate
                    && Target.UpdateMethod != CinemachineBrain.UpdateMethods.FixedUpdate);
            });

            return ux;
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
            const string kGizmoFileName = CinemachineCore.kPackageRoot + "/Editor/EditorResources/Icons/Light/CmCamera@256.png";

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
                CinemachineCore.IsLive(vcam)
                    ? CinemachineCorePrefs.ActiveGizmoColour.Value
                    : CinemachineCorePrefs.InactiveGizmoColour.Value);
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
