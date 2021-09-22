// using System;
// using Cinemachine.Utility;
// using UnityEditor;
// using UnityEngine;
// using UnityEngine.UIElements;
//
// namespace Cinemachine.Editor
// {
//     public static class CinemachineVirtualCameraToolbarHandleDrawer
//     {
//         static GUIStyle m_LabelStyle;
//         static bool m_HandleIsBeingDragged;
//         static bool FoVToolIsOn, FarNearClipToolIsOn, FollowOffsetToolIsOn, TrackedObjectOffsetToolIsOn;
//         static CinemachineVirtualCameraToolbarHandleDrawer()
//         {
//             m_LabelStyle = new GUIStyle();
//             FoVToolIsOn = FarNearClipToolIsOn = FollowOffsetToolIsOn = TrackedObjectOffsetToolIsOn = false;
//         }
//
//         public static void TrackedObjectOffsetToolSelection(ChangeEvent<bool> evt)
//         {
//             TrackedObjectOffsetToolIsOn = evt.newValue;
//         }
//         
//         public static void FollowOffsetToolSelection(ChangeEvent<bool> evt)
//         {
//             FollowOffsetToolIsOn = evt.newValue;
//         }
//
//         /// <summary>
//         /// Draws handles for vcam.
//         /// </summary>
//         /// <param name="target"></param>
//         public static void DrawSceneTools(CinemachineVirtualCamera target)
//         {
//             foreach(CinemachineSceneTool sceneTool in Enum.GetValues(typeof(CinemachineSceneTool)))
//             {
//                 if (target.CanBeControllerBySceneTool(sceneTool))
//                 {
//                     DrawSceneTool(sceneTool, target);
//                 }
//             }
//             
//             // if (Tools.current == Tool.Move)
//             // {
//             // }
//
//             // target.CanBeControllerBySceneTool(Cinemachine.Utility.CinemachineSceneTool.FollowOffset);
//             //
//             // DrawTransposerToolHandles(target.GetCinemachineComponent<CinemachineTransposer>());
//             // DrawFramingTransposerToolHandles(target.GetCinemachineComponent<CinemachineFramingTransposer>());
//         }
//
//         static void DrawSceneTool(CinemachineSceneTool tool, CinemachineVirtualCamera vcam)
//         {
//             
//         }
//
//         static void DrawTransposerToolHandles(CinemachineTransposer target)
//         {
//             if (target == null || !target.IsValid)
//             {
//                 return;
//             }
//
//             if (FollowOffsetToolIsOn)
//             {
//                 var up = Vector3.up;
//                 var brain = CinemachineCore.Instance.FindPotentialTargetBrain(target.VirtualCamera);
//                 if (brain != null)
//                     up = brain.DefaultWorldUp;
//                 var followTargetPosition = target.FollowTargetPosition;
//                 var cameraPosition = target.GetTargetCameraPosition(up);
//
//                 var originalColor = Handles.color;
//
//                 Handles.color = m_LabelStyle.normal.textColor = m_HandleIsBeingDragged
//                     ? CinemachineSettings.CinemachineCoreSettings.k_vcamActiveToolColor
//                     : CinemachineSettings.CinemachineCoreSettings.k_vcamToolsColor;
//
//
//                 EditorGUI.BeginChangeCheck();
//                 var newPos = Handles.PositionHandle(cameraPosition, Quaternion.identity);
//                 if (EditorGUI.EndChangeCheck())
//                 {
//                     m_HandleIsBeingDragged = true;
//                     target.m_FollowOffset = newPos;
//                     InspectorUtility.RepaintGameView();
//
//                     Undo.RecordObject(target, "Change Follow Offset Position using handle in scene view.");
//                 }
//                 else
//                 {
//                     m_HandleIsBeingDragged = false;
//                 }
//
//
//                 Handles.DrawDottedLine(followTargetPosition, cameraPosition, 5f);
//                 Handles.Label(cameraPosition, "Follow offset " + target.m_FollowOffset.ToString("F1"), m_LabelStyle);
//
//                 Handles.color = originalColor;
//             }
//         }
//
//         static void DrawFramingTransposerToolHandles(CinemachineFramingTransposer target)
//         {
//             
//             
//             // if (target == null || !target.IsValid)
//             // {
//             //     return;
//             // }
//             //
//             // if (FollowOffsetToolIsOn)
//             // {
//             //     var up = Vector3.up;
//             //     var brain = CinemachineCore.Instance.FindPotentialTargetBrain(target.VirtualCamera);
//             //     if (brain != null)
//             //         up = brain.DefaultWorldUp;
//             //     var followTargetPosition = target.FollowTargetPosition;
//             //     var cameraPosition = target.GetTargetCameraPosition(up);
//             //
//             //     var originalColor = Handles.color;
//             //
//             //     Handles.color = m_LabelStyle.normal.textColor = m_HandleIsBeingDragged
//             //         ? CinemachineSettings.CinemachineCoreSettings.k_vcamActiveToolColor
//             //         : CinemachineSettings.CinemachineCoreSettings.k_vcamToolsColor;
//             //
//             //
//             //     EditorGUI.BeginChangeCheck();
//             //     var newPos = Handles.PositionHandle(cameraPosition, Quaternion.identity);
//             //     if (EditorGUI.EndChangeCheck())
//             //     {
//             //         m_HandleIsBeingDragged = true;
//             //         target.m_FollowOffset = newPos;
//             //         InspectorUtility.RepaintGameView();
//             //
//             //         Undo.RecordObject(target, "Change Follow Offset Position using handle in scene view.");
//             //     }
//             //     else
//             //     {
//             //         m_HandleIsBeingDragged = false;
//             //     }
//             //
//             //
//             //     Handles.DrawDottedLine(followTargetPosition, cameraPosition, 5f);
//             //     Handles.Label(cameraPosition, "Follow offset " + target.m_FollowOffset.ToString("F1"), m_LabelStyle);
//             //
//             //     Handles.color = originalColor;
//             // }
//         }
//     }
// }
