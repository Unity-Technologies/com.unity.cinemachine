using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Cinemachine.Utility;
using System.IO;

namespace Cinemachine.Editor
{
    /// <summary>
    /// Inspector for CinemachineBrain
    /// </summary>
    [CustomEditor(typeof(CinemachineBrain))]
    [CanEditMultipleObjects]
    public sealed class CinemachineBrainEditor : BaseEditor<CinemachineBrain>
    {
        EmbeddeAssetEditor<CinemachineBlenderSettings> m_BlendsEditor;
        bool m_EventsExpanded = false;

        /// <summary>Obsolete, do not use.  Use the overload, which is more performant</summary>
        /// <returns>List of property names to exclude</returns>
        protected override List<string> GetExcludedPropertiesInInspector() 
            { return base.GetExcludedPropertiesInInspector(); }

        /// <summary>Get the property names to exclude in the inspector.</summary>
        /// <param name="excluded">Add the names to this list</param>
        protected override void GetExcludedPropertiesInInspector(List<string> excluded)
        {
            base.GetExcludedPropertiesInInspector(excluded);
            excluded.Add(FieldPath(x => x.m_CameraCutEvent));
            excluded.Add(FieldPath(x => x.m_CameraActivatedEvent));
            excluded.Add(FieldPath(x => x.m_CustomBlends));
        }

        void OnEnable()
        {
            m_BlendsEditor = new EmbeddeAssetEditor<CinemachineBlenderSettings>(FieldPath(x => x.m_CustomBlends), this)
            {
                OnChanged = (CinemachineBlenderSettings b) => { InspectorUtility.RepaintGameView(); }
            };
        }

        void OnDisable()
        {
            if (m_BlendsEditor != null)
                m_BlendsEditor.OnDisable();
        }

        /// <summary>Create the contents of the inspector panel</summary>
        public override void OnInspectorGUI()
        {
            BeginInspector();

            // Show the active camera and blend
            GUI.enabled = false;
            var vcam = Target.ActiveVirtualCamera as CinemachineVirtualCameraBase;
            Transform activeCam = vcam != null ? vcam.transform : null;
            EditorGUILayout.ObjectField("Live Camera", activeCam, typeof(Transform), true);
            EditorGUILayout.DelayedTextField(
                "Live Blend", Target.ActiveBlend != null
                ? Target.ActiveBlend.Description : string.Empty);
            GUI.enabled = true;

            // Normal properties
            DrawRemainingPropertiesInInspector();

            if (targets.Length == 1)
            {
                // Blender
                m_BlendsEditor.DrawEditorCombo(
                    "Create New Blender Asset",
                    Target.gameObject.name + " Blends", "asset", string.Empty,
                    "Custom Blends", false);

                m_EventsExpanded = EditorGUILayout.Foldout(m_EventsExpanded, "Events", true);
                if (m_EventsExpanded)
                {
                    EditorGUILayout.PropertyField(FindProperty(x => x.m_CameraCutEvent));
                    EditorGUILayout.PropertyField(FindProperty(x => x.m_CameraActivatedEvent));
                }
                serializedObject.ApplyModifiedProperties(); 
            }
        }

        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected, typeof(CinemachineBrain))]
        static void DrawBrainGizmos(CinemachineBrain brain, GizmoType drawType)
        {
            if (brain.OutputCamera != null && brain.m_ShowCameraFrustum && brain.isActiveAndEnabled)
            {
                DrawCameraFrustumGizmo(
                    brain, LensSettings.FromCamera(brain.OutputCamera),
                    brain.transform.localToWorldMatrix,
                    Color.white); // GML why is this color hardcoded?
            }
        }

        internal static void DrawCameraFrustumGizmo(
            CinemachineBrain brain, LensSettings lens,
            Matrix4x4 transform, Color color)
        {
            float aspect = 1;
            bool ortho = false;
            if (brain != null)
            {
                aspect = brain.OutputCamera.aspect;
                ortho = brain.OutputCamera.orthographic;
            }

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
                Gizmos.DrawWireCube(
                    new Vector3(0, 0, (size.z / 2) + lens.NearClipPlane), size);
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

        /// <summary>Draw the gizmo for a virtual camera in the scene view</summary>
        /// <param name="vcam">The virtual camera</param>
        /// <param name="selectionType">How the object is selected</param>
        [DrawGizmo(GizmoType.Active | GizmoType.InSelectionHierarchy | GizmoType.Pickable, typeof(CinemachineVirtualCameraBase))]
        public static void DrawVirtualCameraBaseGizmos(CinemachineVirtualCameraBase vcam, GizmoType selectionType)
        {
            // Don't draw gizmos on hidden stuff
            if ((vcam.gameObject.hideFlags & (HideFlags.HideInHierarchy | HideFlags.HideInInspector)) != 0)
                return;

            if (vcam.ParentCamera != null && (selectionType & GizmoType.Active) == 0)
                return;

            CameraState state = vcam.State;
            Gizmos.DrawIcon(state.FinalPosition, s_GizmoFileName, true);

            DrawCameraFrustumGizmo(
                CinemachineCore.Instance.FindPotentialTargetBrain(vcam),
                state.Lens,
                Matrix4x4.TRS(
                    state.FinalPosition,
                    UnityQuaternionExtensions.Normalized(state.FinalOrientation), Vector3.one),
                CinemachineCore.Instance.IsLive(vcam)
                    ? CinemachineSettings.CinemachineCoreSettings.ActiveGizmoColour
                    : CinemachineSettings.CinemachineCoreSettings.InactiveGizmoColour);
        }

#if UNITY_2019_1_OR_NEWER
        static string s_GizmoFileName = "Packages/com.unity.cinemachine/Gizmos/cm_logo.png";
#else
        static string s_GizmoFileName = "Cinemachine/cm_logo_lg.png";
        [InitializeOnLoad]
        static class InstallGizmos
        {
            static InstallGizmos()
            {
                string srcFile = ScriptableObjectUtility.CinemachineInstallPath + "/Gizmos/" + s_GizmoFileName;
                if (File.Exists(srcFile))
                {
                    string dstFile = Application.dataPath + "/Gizmos";
                    if (!Directory.Exists(dstFile))
                        Directory.CreateDirectory(dstFile);
                    dstFile += "/" + s_GizmoFileName;
                    if (!File.Exists(dstFile)
                        || (File.GetLastWriteTime(dstFile) < File.GetLastWriteTime(srcFile)
                            && (File.GetAttributes(dstFile) & FileAttributes.ReadOnly) == 0))
                    {
                        if (!Directory.Exists(Path.GetDirectoryName(dstFile)))
                            Directory.CreateDirectory(Path.GetDirectoryName(dstFile));
                        File.Copy(srcFile, dstFile, true);
                        File.SetAttributes(
                            dstFile, File.GetAttributes(dstFile) & ~FileAttributes.ReadOnly);
                    }
                }
            }
        }
#endif
    }
}
