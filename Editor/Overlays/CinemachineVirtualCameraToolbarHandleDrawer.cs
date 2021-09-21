using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Cinemachine.Editor
{
    public class CinemachineVirtualCameraToolbarHandleDrawer
    {
        GUIStyle m_LabelStyle;
        bool m_HandleIsBeingDragged;
        static bool FoVToolIsOn, FarNearClipToolIsOn, FollowOffsetToolIsOn, TrackedObjectOffsetToolIsOn;
        public CinemachineVirtualCameraToolbarHandleDrawer()
        {
            m_LabelStyle = new GUIStyle();
            FoVToolIsOn = FarNearClipToolIsOn = FollowOffsetToolIsOn = TrackedObjectOffsetToolIsOn = false;
        }

        public static void TrackedObjectOffsetToolSelection(ChangeEvent<bool> evt)
        {
            TrackedObjectOffsetToolIsOn = evt.newValue;
        }
        
        public static void FollowOffsetToolSelection(ChangeEvent<bool> evt)
        {
            FollowOffsetToolIsOn = evt.newValue;
        }

        /// <summary>
        /// Draws handles for vcam.
        /// </summary>
        /// <param name="target"></param>
        /// <returns>True, if any handle has been drawn.</returns>
        public bool DrawHandles(CinemachineVirtualCamera target)
        {
            // TODO: KGB add -> || no tool is selected in the toolbar
            if (Tools.current == Tool.Move)
            {
                return false;
            }

            DrawTransposerToolHandles(target.GetCinemachineComponent<CinemachineTransposer>());
            DrawFramingTransposerToolHandles(target.GetCinemachineComponent<CinemachineFramingTransposer>());

            return false;
        }

        void DrawTransposerToolHandles(CinemachineTransposer target)
        {
            if (target == null || !target.IsValid)
            {
                return;
            }

            if (FollowOffsetToolIsOn)
            {
                var up = Vector3.up;
                var brain = CinemachineCore.Instance.FindPotentialTargetBrain(target.VirtualCamera);
                if (brain != null)
                    up = brain.DefaultWorldUp;
                var followTargetPosition = target.FollowTargetPosition;
                var cameraPosition = target.GetTargetCameraPosition(up);

                var originalColor = Handles.color;

                Handles.color = m_LabelStyle.normal.textColor = m_HandleIsBeingDragged
                    ? CinemachineSettings.CinemachineCoreSettings.k_vcamActiveToolColor
                    : CinemachineSettings.CinemachineCoreSettings.k_vcamToolsColor;


                EditorGUI.BeginChangeCheck();
                var newPos = Handles.PositionHandle(cameraPosition, Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    m_HandleIsBeingDragged = true;
                    target.m_FollowOffset = newPos;
                    InspectorUtility.RepaintGameView();

                    Undo.RecordObject(target, "Change Follow Offset Position using handle in scene view.");
                }
                else
                {
                    m_HandleIsBeingDragged = false;
                }


                Handles.DrawDottedLine(followTargetPosition, cameraPosition, 5f);
                Handles.Label(cameraPosition, "Follow offset " + target.m_FollowOffset.ToString("F1"), m_LabelStyle);

                Handles.color = originalColor;
            }
        }
        
        void DrawFramingTransposerToolHandles(CinemachineFramingTransposer target)
        {
            
            
            // if (target == null || !target.IsValid)
            // {
            //     return;
            // }
            //
            // if (FollowOffsetToolIsOn)
            // {
            //     var up = Vector3.up;
            //     var brain = CinemachineCore.Instance.FindPotentialTargetBrain(target.VirtualCamera);
            //     if (brain != null)
            //         up = brain.DefaultWorldUp;
            //     var followTargetPosition = target.FollowTargetPosition;
            //     var cameraPosition = target.GetTargetCameraPosition(up);
            //
            //     var originalColor = Handles.color;
            //
            //     Handles.color = m_LabelStyle.normal.textColor = m_HandleIsBeingDragged
            //         ? CinemachineSettings.CinemachineCoreSettings.k_vcamActiveToolColor
            //         : CinemachineSettings.CinemachineCoreSettings.k_vcamToolsColor;
            //
            //
            //     EditorGUI.BeginChangeCheck();
            //     var newPos = Handles.PositionHandle(cameraPosition, Quaternion.identity);
            //     if (EditorGUI.EndChangeCheck())
            //     {
            //         m_HandleIsBeingDragged = true;
            //         target.m_FollowOffset = newPos;
            //         InspectorUtility.RepaintGameView();
            //
            //         Undo.RecordObject(target, "Change Follow Offset Position using handle in scene view.");
            //     }
            //     else
            //     {
            //         m_HandleIsBeingDragged = false;
            //     }
            //
            //
            //     Handles.DrawDottedLine(followTargetPosition, cameraPosition, 5f);
            //     Handles.Label(cameraPosition, "Follow offset " + target.m_FollowOffset.ToString("F1"), m_LabelStyle);
            //
            //     Handles.color = originalColor;
            // }
        }
    }
}
