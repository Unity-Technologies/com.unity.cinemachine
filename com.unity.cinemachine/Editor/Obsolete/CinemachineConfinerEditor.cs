#if !CINEMACHINE_NO_CM2_SUPPORT
#if CINEMACHINE_PHYSICS || CINEMACHINE_PHYSICS_2D

using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace Unity.Cinemachine.Editor
{
    [System.Obsolete]
    [CustomEditor(typeof(CinemachineConfiner))]
    [CanEditMultipleObjects]
    class CinemachineConfinerEditor : BaseEditor<CinemachineConfiner>
    {
        /// <summary>Get the property names to exclude in the inspector.</summary>
        /// <param name="excluded">Add the names to this list</param>
        protected override void GetExcludedPropertiesInInspector(List<string> excluded)
        {
            base.GetExcludedPropertiesInInspector(excluded);
            CinemachineBrain brain = CinemachineCore.FindPotentialTargetBrain(Target.ComponentOwner);
            bool ortho = brain != null ? brain.OutputCamera.orthographic : false;
            if (!ortho)
                excluded.Add(FieldPath(x => x.m_ConfineScreenEdges));
#if CINEMACHINE_PHYSICS && CINEMACHINE_PHYSICS_2D
            if (Target.m_ConfineMode == CinemachineConfiner.Mode.Confine2D)
                excluded.Add(FieldPath(x => x.m_BoundingVolume));
            else
                excluded.Add(FieldPath(x => x.m_BoundingShape2D));
#endif
        }

        public override void OnInspectorGUI()
        {
            BeginInspector();
            this.IMGUI_DrawMissingCmCameraHelpBox();

#if CINEMACHINE_PHYSICS && CINEMACHINE_PHYSICS_2D
            if (Target.m_ConfineMode == CinemachineConfiner.Mode.Confine2D)
            {
#endif
#if CINEMACHINE_PHYSICS_2D
                if (Target.m_BoundingShape2D == null)
                    EditorGUILayout.HelpBox("A Bounding Shape is required.", MessageType.Warning);
                else if (Target.m_BoundingShape2D.GetType() != typeof(PolygonCollider2D)
                    && Target.m_BoundingShape2D.GetType() != typeof(CompositeCollider2D))
                {
                    EditorGUILayout.HelpBox(
                        "Must be a PolygonCollider2D or CompositeCollider2D.",
                        MessageType.Warning);
                }
                else if (Target.m_BoundingShape2D.GetType() == typeof(CompositeCollider2D))
                {
                    CompositeCollider2D poly = Target.m_BoundingShape2D as CompositeCollider2D;
                    if (poly.geometryType != CompositeCollider2D.GeometryType.Polygons)
                    {
                        EditorGUILayout.HelpBox(
                            "CompositeCollider2D geometry type must be Polygons",
                            MessageType.Warning);
                    }
                }
#endif
#if CINEMACHINE_PHYSICS && CINEMACHINE_PHYSICS_2D
            }
            else
            {
#endif
#if CINEMACHINE_PHYSICS
                if (Target.m_BoundingVolume == null)
                    EditorGUILayout.HelpBox("A Bounding Volume is required.", MessageType.Warning);
                else if (Target.m_BoundingVolume.GetType() != typeof(BoxCollider)
                    && Target.m_BoundingVolume.GetType() != typeof(SphereCollider)
                    && Target.m_BoundingVolume.GetType() != typeof(CapsuleCollider))
                {
                    EditorGUILayout.HelpBox(
                        "Must be a BoxCollider, SphereCollider, or CapsuleCollider.",
                        MessageType.Warning);
                }
#endif
#if CINEMACHINE_PHYSICS && CINEMACHINE_PHYSICS_2D
            }
#endif
            DrawRemainingPropertiesInInspector();
        }
    }
}
#endif
#endif
