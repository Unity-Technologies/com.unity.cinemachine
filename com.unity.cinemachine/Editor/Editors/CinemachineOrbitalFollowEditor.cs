using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineOrbitalFollow))]
    [CanEditMultipleObjects]
    internal class CinemachineOrbitalFollowEditor : BaseEditor<CinemachineOrbitalFollow>
    {
        /// <summary>Get the property names to exclude in the inspector.</summary>
        /// <param name="excluded">Add the names to this list</param>
        protected override void GetExcludedPropertiesInInspector(List<string> excluded)
        {
            base.GetExcludedPropertiesInInspector(excluded);
            switch (Target.BindingMode)
            {
                default:
                case CinemachineTransposer.BindingMode.LockToTarget:
                    excluded.Add(Target.RotationDampingMode == CinemachineTransposer.AngularDampingMode.Euler 
                        ? FieldPath(x => x.QuaternionDamping) 
                        : FieldPath(x => x.RotationDamping));
                    break;
                case CinemachineTransposer.BindingMode.WorldSpace:
                case CinemachineTransposer.BindingMode.SimpleFollowWithWorldUp:
                    excluded.Add(FieldPath(x => x.RotationDampingMode));
                    excluded.Add(FieldPath(x => x.RotationDamping));
                    excluded.Add(FieldPath(x => x.QuaternionDamping));
                    break;
            }

            excluded.Add(Target.OrbitStyle == CinemachineOrbitalFollow.OrbitMode.Sphere 
                ? FieldPath(x => x.Orbits) 
                : FieldPath(x => x.Radius));
        }

        public override void OnInspectorGUI()
        {
            BeginInspector();
            bool noFollow = false;
            bool noHandler = false;
            for (int i = 0; i < targets.Length; ++i)
            {
                var t = (CinemachineOrbitalFollow)targets[i];
                noFollow |= t.FollowTarget == null;
                noHandler |= !t.HasInputHandler;
            }

            if (noFollow)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(
                    "Orbital Follow requires a Follow target.",
                    MessageType.Warning);
            }
            if (noHandler)
            {
                InspectorUtility.HelpBoxWithButton(
                    "Orbital Follow has no input axis controller behaviour.", MessageType.Info,
                    new GUIContent("Add Input\nController"), () =>
                    {
                        Undo.SetCurrentGroupName("Add Input Axis Controller");
                        for (int i = 0; i < targets.Length; ++i)
                        {
                            var t = (CinemachineOrbitalFollow)targets[i];
                            if (!t.HasInputHandler)
                            {
                                var controller = t.VirtualCamera.GetComponent<InputAxisController>();
                                if (controller == null)
                                    Undo.AddComponent<InputAxisController>(t.VirtualCamera.gameObject);
                                else if (!controller.enabled)
                                {
                                    Undo.RecordObject(controller, "enable controller");
                                    controller.enabled = true;
                                }
                            }
                        }
                    });
            }

            int flags = 0;
            if (Target.BindingMode == CinemachineTransposer.BindingMode.SimpleFollowWithWorldUp)
                flags |= (int)InputAxis.Flags.HideRecentering | (int)InputAxis.Flags.RangeIsDriven;
            var flagsProp = FindProperty(x => x.HorizontalAxis).FindPropertyRelative("InspectorFlags");
            if (flagsProp.intValue != flags)
            {
                flagsProp.intValue = flags;
                serializedObject.ApplyModifiedProperties();
            }
            DrawRemainingPropertiesInInspector();
        }
        
        static GUIContent[] s_OrbitNames = 
        {
            new GUIContent("Top"), 
            new GUIContent("Center"), 
            new GUIContent("Bottom")
        };
        internal static GUIContent[] orbitNames => s_OrbitNames;
        
        protected virtual void OnEnable()
        {
#if UNITY_2021_2_OR_NEWER
            CinemachineSceneToolUtility.RegisterTool(typeof(FollowOffsetTool));
            CinemachineSceneToolUtility.RegisterTool(typeof(OrbitalFollowOrbitSelection));
#endif
        }
        
        protected virtual void OnDisable()
        {
#if UNITY_2021_2_OR_NEWER
            CinemachineSceneToolUtility.UnregisterTool(typeof(FollowOffsetTool));
            CinemachineSceneToolUtility.UnregisterTool(typeof(OrbitalFollowOrbitSelection));
#endif
        }
   
#if UNITY_2021_2_OR_NEWER     
        void OnSceneGUI()
        {
            DrawSceneTools();
        }

        bool m_UpdateCache = true;
        float m_VerticalAxisCache;
        void DrawSceneTools()
        {
            var orbitalFollow = Target;
            if (orbitalFollow == null || !orbitalFollow.IsValid)
            {
                return;
            }
            
            var originalColor = Handles.color;
            Handles.color = Handles.preselectionColor;
            if (CinemachineSceneToolUtility.IsToolActive(typeof(FollowOffsetTool))) 
            {
                switch (orbitalFollow.OrbitStyle)
                {
                    case CinemachineOrbitalFollow.OrbitMode.Sphere:
                        {
                            EditorGUI.BeginChangeCheck();
                            var camPos = orbitalFollow.VcamState.RawPosition;
                            var camTransform = orbitalFollow.VirtualCamera.transform;
                            var camRight = camTransform.right;
                            var followPos = orbitalFollow.FollowTargetPosition;
                            var handlePos = followPos + camRight * orbitalFollow.Radius;
                            var rHandleId = GUIUtility.GetControlID(FocusType.Passive);
                            var newHandlePosition = Handles.Slider(rHandleId, handlePos, -camRight,
                                CinemachineSceneToolHelpers.CubeHandleCapSize(camPos), Handles.CubeHandleCap, 0.5f);
                            if (EditorGUI.EndChangeCheck())
                            {
                                // Modify via SerializedProperty for OnValidate to get called automatically, and scene repainting too
                                var so = new SerializedObject(orbitalFollow);
                                var prop = so.FindProperty(() => orbitalFollow.Radius);
                                prop.floatValue -= CinemachineSceneToolHelpers.SliderHandleDelta(
                                    newHandlePosition, handlePos, -camRight);
                                so.ApplyModifiedProperties();
                            }

                            var orbitRadiusHandleIsDragged = GUIUtility.hotControl == rHandleId;
                            var orbitRadiusHandleIsUsedOrHovered = orbitRadiusHandleIsDragged ||
                                HandleUtility.nearestControl == rHandleId;
                            if (orbitRadiusHandleIsUsedOrHovered)
                            {
                                CinemachineSceneToolHelpers.DrawLabel(camPos,
                                    "Radius (" + orbitalFollow.Radius.ToString("F1") + ")");
                            }
                            
                            Handles.color = orbitRadiusHandleIsUsedOrHovered ? 
                                Handles.selectedColor : CinemachineSceneToolHelpers.HelperLineDefaultColor;
                            Handles.DrawLine(camPos, followPos);
                            Handles.DrawWireDisc(followPos, camTransform.up, orbitalFollow.Radius);
                            
                            CinemachineSceneToolHelpers.SoloOnDrag(
                                orbitRadiusHandleIsDragged, orbitalFollow.VirtualCamera, rHandleId);

                            Handles.color = originalColor;
                        }
                        break;
                    case CinemachineOrbitalFollow.OrbitMode.ThreeRing:
                        if (m_UpdateCache)
                            m_VerticalAxisCache = orbitalFollow.VerticalAxis.Value;
                        
                        var draggedRig = CinemachineSceneToolHelpers.OrbitControlHandleOrbitalFollow(orbitalFollow.VirtualCamera, 
                            new SerializedObject(orbitalFollow).FindProperty(() => orbitalFollow.Orbits));
                        m_UpdateCache = draggedRig < 0 || draggedRig > 2;
                        orbitalFollow.VerticalAxis.Value = draggedRig switch
                        {
                            0 => orbitalFollow.VerticalAxis.Range.y,
                            1 => orbitalFollow.VerticalAxis.Center,
                            2 => orbitalFollow.VerticalAxis.Range.x,
                            _ => m_VerticalAxisCache
                        };
                        break;
                    default:
                        Debug.LogError("OrbitStyle has no associated handle");
                        throw new System.ArgumentOutOfRangeException();
                }
                
            }
            Handles.color = originalColor;
        }
#endif

        // TODO: ask swap's opinion on this. Do we want to always draw this or only when follow offset handle is not selected
        // TODO: what color? when follow offset handle is selected, do we want to draw CameraPath.
        [DrawGizmo(GizmoType.Active | GizmoType.Selected, typeof(CinemachineOrbitalFollow))]
        static void DrawOrbitalGizmos(CinemachineOrbitalFollow orbital, GizmoType selectionType)
        {
            var vcam = orbital.VirtualCamera;
            if (vcam != null && vcam.Follow != null)
            {
                if (orbital.OrbitStyle == CinemachineOrbitalFollow.OrbitMode.ThreeRing)
                {
                    var prevColor = Gizmos.color;
                    Gizmos.color = CinemachineCore.Instance.IsLive(vcam)
                        ? CinemachineSettings.CinemachineCoreSettings.BoundaryObjectGizmoColour
                        : CinemachineSettings.CinemachineCoreSettings.InactiveGizmoColour;

                    var orient = orbital.GetReferenceOrientation();
                    var up = orient * Vector3.up;
                    var rotation = orbital.HorizontalAxis.Value;
                    orient = Quaternion.AngleAxis(rotation, up);
                    var pos = orbital.FollowTargetPosition;
                    var scale = orbital.RadialAxis.Value;

                    DrawCircleAtPointWithRadius(
                        pos + up * orbital.Orbits.Top.Height * scale, 
                        orient, orbital.Orbits.Top.Radius * scale);
                    DrawCircleAtPointWithRadius(
                        pos + up * orbital.Orbits.Center.Height * scale, orient, 
                        orbital.Orbits.Center.Radius * scale);
                    DrawCircleAtPointWithRadius(
                        pos + up * orbital.Orbits.Bottom.Height * scale, 
                        orient, orbital.Orbits.Bottom.Radius * scale);

                    DrawCameraPath(pos, orient, scale, orbital);

                    Gizmos.color = prevColor;
                }
            }
        }

        static void DrawCircleAtPointWithRadius(Vector3 point, Quaternion orient, float radius)
        {
            var prevMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(point, orient, radius * Vector3.one);

            const int kNumPoints = 25;
            var currPoint = Vector3.forward;
            var rot = Quaternion.AngleAxis(360f / (float)kNumPoints, Vector3.up);
            for (int i = 0; i < kNumPoints + 1; ++i)
            {
                var nextPoint = rot * currPoint;
                Gizmos.DrawLine(currPoint, nextPoint);
                currPoint = nextPoint;
            }
            Gizmos.matrix = prevMatrix;
        }
        
        static void DrawCameraPath(
            Vector3 pos, Quaternion orient, float scale, CinemachineOrbitalFollow freelook)
        {
            var prevMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(pos, orient, scale * Vector3.one);

            const float stepSize = 0.1f;
            var lastPos = freelook.GetCameraOffsetForNormalizedAxisValue(-1);
            var max = 1 + stepSize/2;
            for (float t = -1 + stepSize; t < max; t += stepSize)
            {
                var p = freelook.GetCameraOffsetForNormalizedAxisValue(t);
                Gizmos.DrawLine(lastPos, p);
                lastPos = p;
            }
            Gizmos.matrix = prevMatrix;
        }
    }
}
