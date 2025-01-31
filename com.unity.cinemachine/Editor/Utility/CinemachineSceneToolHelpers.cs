using System;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Cinemachine.Editor
{
    static class CinemachineSceneToolHelpers
    {
        public const float LineThickness = 2f;
        public static readonly Color HelperLineDefaultColor = new Color(255, 255, 255, 25);
        const float k_DottedLineSpacing = 4f;

        static GUIStyle s_LabelStyle = new ()
        {
            normal =
            {
                background = AssetDatabase.LoadAssetAtPath<Texture2D>($"{ResourcePath}/SceneToolsLabelBackground.png"),
                textColor = Handles.selectedColor,
            },
            fontStyle = FontStyle.Bold,
            padding = new RectOffset(5, 0, 5, 0)
        };

        static string SkinSuffix => EditorGUIUtility.isProSkin ? "Dark" : "Light";
        static string ResourcePath => $"{CinemachineCore.kPackageRoot}/Editor/EditorResources";
        public static string IconPath => $"{ResourcePath}/Icons/{SkinSuffix}";

        static Color s_NormalBkgColor = Color.black;

        public static float SliderHandleDelta(Vector3 newPos, Vector3 oldPos, Vector3 forward)
        {
            var delta = newPos - oldPos;
            return Mathf.Sign(Vector3.Dot(delta, forward)) * delta.magnitude;
        }

        /// <summary>
        /// Calculate delta and discard imprecision.
        /// </summary>
        public static Vector3 PositionHandleDelta(Quaternion rot, Vector3 newPos, Vector3 oldPos)
        {
            var delta =
                Quaternion.Inverse(rot) * (newPos - oldPos);
            delta = new Vector3(
                Mathf.Abs(delta.x) < UnityVectorExtensions.Epsilon ? 0 : delta.x,
                Mathf.Abs(delta.y) < UnityVectorExtensions.Epsilon ? 0 : delta.y,
                Mathf.Abs(delta.z) < UnityVectorExtensions.Epsilon ? 0 : delta.z);
            return delta;
        }

        public static void DrawLabel(Vector3 position, string text)
        {
            var labelOffset = HandleUtility.GetHandleSize(position) / 2f;
            Handles.Label(position + new Vector3(0, -labelOffset, 0), text, s_LabelStyle);
        }

        public static float CubeHandleCapSize(Vector3 position) => HandleUtility.GetHandleSize(position) / 10f;

        static int s_ScaleSliderHash = "ScaleSliderHash".GetHashCode();
        static float s_FOVAfterLastToolModification;

        public static void DoFovToolHandle(
            CinemachineVirtualCameraBase vcam, SerializedProperty lensProperty,
            in LensSettings lens, bool isLensHorizontal)
        {
            var orthographic = lens.Orthographic;
            if (GUIUtility.hotControl == 0)
                s_FOVAfterLastToolModification = orthographic ? lens.OrthographicSize : lens.FieldOfView;

            var originalColor = Handles.color;
            Handles.color = Handles.preselectionColor;

            var camPos = vcam.State.GetFinalPosition();
            var camRot = vcam.State.GetFinalOrientation();
            var camForward = camRot * Vector3.forward;

            EditorGUI.BeginChangeCheck();
            var fovHandleId = GUIUtility.GetControlID(s_ScaleSliderHash, FocusType.Passive);
            var newFov = Handles.ScaleSlider(fovHandleId, s_FOVAfterLastToolModification,
                camPos, camForward, camRot, HandleUtility.GetHandleSize(camPos), 0.1f);

            if (EditorGUI.EndChangeCheck())
            {
                if (orthographic)
                {
                    lensProperty.FindPropertyRelative("OrthographicSize").floatValue +=
                        (s_FOVAfterLastToolModification - newFov);
                }
                else
                {
                    lensProperty.FindPropertyRelative("FieldOfView").floatValue +=
                        (s_FOVAfterLastToolModification - newFov);
                    lensProperty.FindPropertyRelative("FieldOfView").floatValue =
                        Mathf.Clamp(lensProperty.FindPropertyRelative("FieldOfView").floatValue, 1f, 179f);
                }
                lensProperty.serializedObject.ApplyModifiedProperties();
            }
            s_FOVAfterLastToolModification = newFov;

            var fovHandleDraggedOrHovered =
                GUIUtility.hotControl == fovHandleId || HandleUtility.nearestControl == fovHandleId;
            if (fovHandleDraggedOrHovered)
            {
                var labelPos = camPos + camForward * HandleUtility.GetHandleSize(camPos);
                if (lens.IsPhysicalCamera)
                {
                    DrawLabel(labelPos, "Focal Length (" +
                        Camera.FieldOfViewToFocalLength(lens.FieldOfView, lens.PhysicalProperties.SensorSize.y).ToString("F1") + ")");
                }
                else if (orthographic)
                {
                    DrawLabel(labelPos, "Orthographic Size (" +
                        lens.OrthographicSize.ToString("F1") + ")");
                }
                else if (isLensHorizontal)
                {
                    DrawLabel(labelPos, "Horizontal FOV (" +
                        Camera.VerticalToHorizontalFieldOfView(lens.FieldOfView, lens.Aspect).ToString("F1") + ")");
                }
                else
                {
                    DrawLabel(labelPos, "Vertical FOV (" +
                        lens.FieldOfView.ToString("F1") + ")");
                }
            }

            Handles.color = fovHandleDraggedOrHovered ? Handles.selectedColor : HelperLineDefaultColor;
            var vcamLocalToWorld = Matrix4x4.TRS(camPos, camRot, Vector3.one);
            DrawFrustum(vcamLocalToWorld, lens);

            SoloOnDrag(GUIUtility.hotControl == fovHandleId, vcam, fovHandleId);

            Handles.color = originalColor;
        }

        public static void DoNearFarClipHandle(CinemachineVirtualCameraBase vcam, SerializedProperty lens)
        {
            var originalColor = Handles.color;
            Handles.color = Handles.preselectionColor;

            var vcamState = vcam.State;
            var camPos = vcamState.GetFinalPosition();
            var camRot = vcamState.GetFinalOrientation();
            var camForward = camRot * Vector3.forward;
            var nearClipPlane = lens.FindPropertyRelative("NearClipPlane");
            var farClipPlane = lens.FindPropertyRelative("FarClipPlane");
            var nearClipPos = camPos + camForward * nearClipPlane.floatValue;
            var farClipPos = camPos + camForward * farClipPlane.floatValue;
            var vcamLens = vcamState.Lens;

            EditorGUI.BeginChangeCheck();
            var ncHandleId = GUIUtility.GetControlID(FocusType.Passive);
            var newNearClipPos = Handles.Slider(ncHandleId, nearClipPos, camForward,
                CubeHandleCapSize(nearClipPos), Handles.CubeHandleCap, 0.5f);
            var fcHandleId = GUIUtility.GetControlID(FocusType.Passive);
            var newFarClipPos = Handles.Slider(fcHandleId, farClipPos, camForward,
                CubeHandleCapSize(farClipPos), Handles.CubeHandleCap, 0.5f);
            if (EditorGUI.EndChangeCheck())
            {
                nearClipPlane.floatValue +=
                    SliderHandleDelta(newNearClipPos, nearClipPos, camForward);
                if (!vcamLens.Orthographic)
                {
                    nearClipPlane.floatValue = Mathf.Max(0.01f, nearClipPlane.floatValue);
                }
                farClipPlane.floatValue +=
                    SliderHandleDelta(newFarClipPos, farClipPos, camForward);
                lens.serializedObject.ApplyModifiedProperties();
            }

            var vcamLocalToWorld = Matrix4x4.TRS(camPos, camRot, Vector3.one);
            Handles.color = HelperLineDefaultColor;
            DrawFrustum(vcamLocalToWorld, vcamLens);
            if (GUIUtility.hotControl == ncHandleId || HandleUtility.nearestControl == ncHandleId)
            {
                DrawPreFrustum(vcamLocalToWorld, vcamLens);
                DrawLabel(nearClipPos, nearClipPlane.displayName + " (" + nearClipPlane.floatValue.ToString("F1") + ")");
            }
            if (GUIUtility.hotControl == fcHandleId || HandleUtility.nearestControl == fcHandleId)
            {
                DrawPreFrustum(vcamLocalToWorld, vcamLens);
                DrawLabel(farClipPos, farClipPlane.displayName + " (" + farClipPlane.floatValue.ToString("F1") + ")");
            }

            SoloOnDrag(GUIUtility.hotControl == ncHandleId || GUIUtility.hotControl == fcHandleId,
                vcam, Mathf.Min(ncHandleId, fcHandleId));

            Handles.color = originalColor;
        }

        static void DrawPreFrustum(Matrix4x4 transform, LensSettings lens)
        {
            if (!lens.Orthographic && lens.NearClipPlane >= 0)
            {
                DrawPerspectiveFrustum(transform, lens.FieldOfView,
                    lens.NearClipPlane, 0, lens.Aspect, true);
            }
        }

        static void DrawFrustum(Matrix4x4 transform, LensSettings lens)
        {
            if (lens.Orthographic)
            {
                DrawOrthographicFrustum(transform, lens.OrthographicSize,
                    lens.FarClipPlane, lens.NearClipPlane, lens.Aspect);
            }
            else
            {
                DrawPerspectiveFrustum(transform, lens.FieldOfView,
                    lens.FarClipPlane, lens.NearClipPlane, lens.Aspect, false);
            }
        }

        static void DrawOrthographicFrustum(Matrix4x4 transform,
            float orthographicSize, float farClipPlane, float nearClipRange, float aspect)
        {
            var originalMatrix = Handles.matrix;
            Handles.matrix = transform;

            var size = new Vector3(aspect * orthographicSize * 2,
                orthographicSize * 2, farClipPlane - nearClipRange);
            Handles.DrawWireCube(new Vector3(0, 0, (size.z / 2) + nearClipRange), size);

            Handles.matrix = originalMatrix;
        }

        static void DrawPerspectiveFrustum(Matrix4x4 transform,
            float fov, float farClipPlane, float nearClipRange, float aspect, bool dottedLine)
        {
            var originalMatrix = Handles.matrix;
            Handles.matrix = transform;

            fov = fov * 0.5f * Mathf.Deg2Rad;
            var tanfov = Mathf.Tan(fov);
            var farEnd = new Vector3(0, 0, farClipPlane);
            var endSizeX = new Vector3(farClipPlane * tanfov * aspect, 0, 0);
            var endSizeY = new Vector3(0, farClipPlane * tanfov, 0);

            Vector3 s1, s2, s3, s4;
            var e1 = farEnd + endSizeX + endSizeY;
            var e2 = farEnd - endSizeX + endSizeY;
            var e3 = farEnd - endSizeX - endSizeY;
            var e4 = farEnd + endSizeX - endSizeY;
            if (nearClipRange <= 0.0f)
            {
                s1 = s2 = s3 = s4 = Vector3.zero;
            }
            else
            {
                var startSizeX = new Vector3(nearClipRange * tanfov * aspect, 0, 0);
                var startSizeY = new Vector3(0, nearClipRange * tanfov, 0);
                var startPoint = new Vector3(0, 0, nearClipRange);
                s1 = startPoint + startSizeX + startSizeY;
                s2 = startPoint - startSizeX + startSizeY;
                s3 = startPoint - startSizeX - startSizeY;
                s4 = startPoint + startSizeX - startSizeY;

                if (dottedLine)
                {
                    Handles.DrawDottedLine(s1, s2, k_DottedLineSpacing);
                    Handles.DrawDottedLine(s2, s3, k_DottedLineSpacing);
                    Handles.DrawDottedLine(s3, s4, k_DottedLineSpacing);
                    Handles.DrawDottedLine(s4, s1, k_DottedLineSpacing);
                }
                else
                {
                    Handles.DrawLine(s1, s2);
                    Handles.DrawLine(s2, s3);
                    Handles.DrawLine(s3, s4);
                    Handles.DrawLine(s4, s1);
                }
            }

            if (dottedLine)
            {
                Handles.DrawDottedLine(e1, e2, k_DottedLineSpacing);
                Handles.DrawDottedLine(e2, e3, k_DottedLineSpacing);
                Handles.DrawDottedLine(e3, e4, k_DottedLineSpacing);
                Handles.DrawDottedLine(e4, e1, k_DottedLineSpacing);

                Handles.DrawDottedLine(s1, e1, k_DottedLineSpacing);
                Handles.DrawDottedLine(s2, e2, k_DottedLineSpacing);
                Handles.DrawDottedLine(s3, e3, k_DottedLineSpacing);
                Handles.DrawDottedLine(s4, e4, k_DottedLineSpacing);
            }
            else
            {
                Handles.DrawLine(e1, e2);
                Handles.DrawLine(e2, e3);
                Handles.DrawLine(e3, e4);
                Handles.DrawLine(e4, e1);

                Handles.DrawLine(s1, e1);
                Handles.DrawLine(s2, e2);
                Handles.DrawLine(s3, e3);
                Handles.DrawLine(s4, e4);
            }

            Handles.matrix = originalMatrix;
        }

        public static void DoTrackedObjectOffsetTool(
            CinemachineVirtualCameraBase vcam, SerializedProperty trackedObjectOffset, CinemachineCore.Stage stage)
        {
            var target = vcam.LookAt;
            if (target == null)
                return;

            var originalColor = Handles.color;

            var lookAtPos = target.position;
            var lookAtRot = target.rotation;
            var trackedObjectPos = lookAtPos + lookAtRot * trackedObjectOffset.vector3Value;

            EditorGUI.BeginChangeCheck();
            var tooHandleIds = Handles.PositionHandleIds.@default;
            var newTrackedObjectPos = Handles.PositionHandle(tooHandleIds, trackedObjectPos, lookAtRot);
            var tooHandleMinId = tooHandleIds.x - 1;
            var tooHandleMaxId = tooHandleIds.xyz + 1;

            if (EditorGUI.EndChangeCheck())
            {
                trackedObjectOffset.vector3Value +=
                    PositionHandleDelta(lookAtRot, newTrackedObjectPos, trackedObjectPos);
                trackedObjectOffset.serializedObject.ApplyModifiedProperties();
            }

            var isDragged =
                tooHandleMinId < GUIUtility.hotControl && GUIUtility.hotControl < tooHandleMaxId;
            var isDraggedOrHovered = isDragged ||
                tooHandleMinId < HandleUtility.nearestControl && HandleUtility.nearestControl < tooHandleMaxId;
            if (isDraggedOrHovered)
            {
                DrawLabel(trackedObjectPos, "(" + stage + ") " + trackedObjectOffset.displayName + " "
                    + trackedObjectOffset.vector3Value.ToString("F1"));
            }

            Handles.color = isDraggedOrHovered ? Handles.selectedColor : HelperLineDefaultColor;
            Handles.DrawDottedLine(lookAtPos, trackedObjectPos, k_DottedLineSpacing);
            Handles.DrawLine(trackedObjectPos, vcam.State.GetFinalPosition());

            SoloOnDrag(isDragged, vcam, tooHandleMaxId);

            Handles.color = originalColor;
        }

        public static void DoFollowOffsetTool(
            CinemachineVirtualCameraBase vcam, SerializedProperty offsetProperty,
            Vector3 camPos, Vector3 targetPosition, Quaternion targetRotation,
            Action OnChanged = null)
        {
            var originalColor = Handles.color;

            EditorGUI.BeginChangeCheck();
            var foHandleIds = Handles.PositionHandleIds.@default;
            var newPos = Handles.PositionHandle(foHandleIds, camPos, targetRotation);
            var foHandleMinId = foHandleIds.x - 1;
            var foHandleMaxId = foHandleIds.xyz + 1;

            if (EditorGUI.EndChangeCheck())
            {
                offsetProperty.vector3Value += PositionHandleDelta(targetRotation, newPos, camPos);
                offsetProperty.serializedObject.ApplyModifiedProperties();
                OnChanged?.Invoke();
            }

            var offset = offsetProperty.vector3Value;
            var isDragged = foHandleMinId < GUIUtility.hotControl && GUIUtility.hotControl < foHandleMaxId;
            var isDraggedOrHovered = isDragged ||
                foHandleMinId < HandleUtility.nearestControl && HandleUtility.nearestControl < foHandleMaxId;
            if (isDraggedOrHovered)
                DrawLabel(camPos, offsetProperty.displayName + " " + offset.ToString("F1"));

            Handles.color = isDraggedOrHovered ? Handles.selectedColor : HelperLineDefaultColor;
            Handles.DrawDottedLine(targetPosition, camPos, k_DottedLineSpacing);

            SoloOnDrag(isDragged, vcam, foHandleMaxId);

            Handles.color = originalColor;
        }

        /// <summary>
        /// Draws Orbit handles for OrbitalFollow
        /// </summary>
        /// <returns>Index of the rig being edited, or -1 if none</returns>
        public static int DoThreeOrbitRigHandle(
            CinemachineVirtualCameraBase vcam, Quaternion rotationFrame,
            SerializedProperty orbitSetting, Vector3 orbitCenter)
        {
            Cinemachine3OrbitRig.Settings def = new();

            var originalColor = Handles.color;
            var draggedRig = -1;
            var minIndex = 1;
            SerializedProperty[] orbits =
            {
                orbitSetting.FindPropertyRelative(() => def.Top),
                orbitSetting.FindPropertyRelative(() => def.Center),
                orbitSetting.FindPropertyRelative(() => def.Bottom),
            };
            for (var rigIndex = 0; rigIndex < orbits.Length; ++rigIndex)
            {
                var orbit = orbits[rigIndex];
                var orbitHeight = orbit.FindPropertyRelative(() => def.Top.Height);
                var orbitRadius = orbit.FindPropertyRelative(() => def.Top.Radius);

                if (OrbitHandles(
                    orbitSetting.serializedObject, orbitHeight, orbitRadius,
                    orbitCenter, rotationFrame,
                    out var heightHandleId, out var radiusHandleId))
                {
                    draggedRig = rigIndex;
                    minIndex = Mathf.Min(heightHandleId, radiusHandleId);
                }
            }
            SoloOnDrag(draggedRig != -1, vcam, minIndex);

            Handles.color = originalColor;
            return draggedRig;
        }

        static bool OrbitHandles(
            SerializedObject orbit,
            SerializedProperty orbitHeight, SerializedProperty orbitRadius,
            Vector3 followPos, Quaternion rotationFrame,
            out int heightHandleId, out int radiusHandleId)
        {
            var oldMatrix = Handles.matrix;
            Handles.matrix = Matrix4x4.TRS(followPos, rotationFrame, Vector3.one);

            Handles.color = Handles.preselectionColor;
            EditorGUI.BeginChangeCheck();

            heightHandleId = GUIUtility.GetControlID(FocusType.Passive);

            var height = Vector3.up * orbitHeight.floatValue;
            var newHeight = Handles.Slider(
                heightHandleId, height, Vector3.up, CubeHandleCapSize(height), Handles.CubeHandleCap, 0.5f);

            radiusHandleId = GUIUtility.GetControlID(FocusType.Passive);
            var radius = Vector3.up * orbitHeight.floatValue + Vector3.right * orbitRadius.floatValue;
            var newRadius = Handles.Slider(
                radiusHandleId, radius, Vector3.right, CubeHandleCapSize(radius), Handles.CubeHandleCap, 0.5f);

            if (EditorGUI.EndChangeCheck())
            {
                orbitHeight.floatValue += SliderHandleDelta(newHeight, height, Vector3.up);
                orbitRadius.floatValue += SliderHandleDelta(newRadius, radius, Vector3.right);
                orbit.ApplyModifiedProperties();
            }

            var isDragged = GUIUtility.hotControl == heightHandleId || GUIUtility.hotControl == radiusHandleId;
            Handles.color = isDragged || HandleUtility.nearestControl == heightHandleId
                || HandleUtility.nearestControl == radiusHandleId ? Handles.selectedColor : HelperLineDefaultColor;
            if (GUIUtility.hotControl == heightHandleId || HandleUtility.nearestControl == heightHandleId)
                DrawLabel(height, orbitHeight.displayName + ": " + orbitHeight.floatValue);
            if (GUIUtility.hotControl == radiusHandleId || HandleUtility.nearestControl == radiusHandleId)
                DrawLabel(radius, orbitRadius.displayName + ": " + orbitRadius.floatValue);
            Handles.DrawWireDisc(newHeight, Vector3.up, orbitRadius.floatValue);
            Handles.matrix = oldMatrix;
            return isDragged;
        }

        static bool s_IsDragging;
        static ICinemachineCamera s_UserSolo;
        public static void SoloOnDrag(bool isDragged, CinemachineVirtualCameraBase vcam, int handleMaxId)
        {
            if (isDragged)
            {
                if (!s_IsDragging)
                {
                    s_UserSolo = CinemachineCore.SoloCamera;
                    s_IsDragging = true;
                }
                CinemachineCore.SoloCamera = vcam;
            }
            else if (s_IsDragging && handleMaxId != -1) // Handles sometimes return -1 as id, ignore those frames
            {
                CinemachineCore.SoloCamera = s_UserSolo;
                InspectorUtility.RepaintGameView();
                s_IsDragging = false;
                s_UserSolo = null;
            }
        }
    }
}
