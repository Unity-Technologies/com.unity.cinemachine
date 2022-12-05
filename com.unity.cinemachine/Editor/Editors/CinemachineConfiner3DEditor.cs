#if CINEMACHINE_PHYSICS 

using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineConfiner3D))]
    [CanEditMultipleObjects]
    class CinemachineConfiner3DEditor : UnityEditor.Editor
    {
        CinemachineConfiner3D Target => target as CinemachineConfiner3D;

        CmPipelineComponentInspectorUtility m_PipelineUtility;

        void OnEnable() => m_PipelineUtility = new (this);
        void OnDisable() => m_PipelineUtility.OnDisable();

        public override VisualElement CreateInspectorGUI()
        {
            var ux = new VisualElement();

            m_PipelineUtility.AddMissingCmCameraHelpBox(ux);

            var boundsHelp = ux.AddChild(new HelpBox(
                "Bounding Volume must be a BoxCollider, SphereCollider, CapsuleCollider, or convex MeshCollider.", 
                HelpBoxMessageType.Warning));

            var volumeProp = serializedObject.FindProperty(() => Target.BoundingVolume);
            ux.Add(new PropertyField(volumeProp));
            ux.Add(new PropertyField(serializedObject.FindProperty(() => Target.SlowingDistance)));
            m_PipelineUtility.UpdateState();

            TrackVolume(volumeProp);
            ux.TrackPropertyValue(volumeProp, TrackVolume);
            void TrackVolume(SerializedProperty p)
            {
                var c = p.objectReferenceValue;
                var mesh = c as MeshCollider;
                var isConvexMesh = mesh != null && mesh.convex;
                boundsHelp.SetVisible(!(isConvexMesh || c is BoxCollider || c is SphereCollider || c is CapsuleCollider));
            }
            return ux;
        }

        [DrawGizmo(GizmoType.Active | GizmoType.Selected, typeof(CinemachineConfiner3D))]
        private static void DrawColliderGizmos(CinemachineConfiner3D confiner, GizmoType type)
        {
            CinemachineVirtualCameraBase vcam = (confiner != null) ? confiner.VirtualCamera : null;
            if (vcam != null && confiner.IsValid)
            {
                var oldMatrix = Gizmos.matrix;
                var oldColor = Gizmos.color;
                Gizmos.color = Color.yellow;

                var t = confiner.BoundingVolume.transform;
                Gizmos.matrix = Matrix4x4.TRS(t.position, t.rotation, t.lossyScale);

                var colliderType = confiner.BoundingVolume.GetType();
                if (colliderType == typeof(BoxCollider))
                {
                    var c = confiner.BoundingVolume as BoxCollider;
                    Gizmos.DrawWireCube(c.center, c.size);
                }
                else if (colliderType == typeof(SphereCollider))
                {
                    var c = confiner.BoundingVolume as SphereCollider;
                    Gizmos.DrawWireSphere(c.center, c.radius);
                }
                else if (colliderType == typeof(CapsuleCollider))
                {
                    var c = confiner.BoundingVolume as CapsuleCollider;
                    var size = Vector3.one * c.radius * 2;
                    switch (c.direction)
                    {
                        case 0: size.x = c.height; break;
                        case 1: size.y = c.height; break;
                        case 2: size.z = c.height; break;
                    }
                    Gizmos.DrawWireCube(c.center, size);
                }
                else if (colliderType == typeof(MeshCollider))
                {
                    var c = confiner.BoundingVolume as MeshCollider;
                    Gizmos.DrawWireMesh(c.sharedMesh);
                }
                else
                {
                    // Just draw an AABB - not very nice!
                    Gizmos.matrix = oldMatrix;
                    var bounds = confiner.BoundingVolume.bounds;
                    Gizmos.DrawWireCube(t.position, bounds.extents * 2);
                }
                Gizmos.color = oldColor;
                Gizmos.matrix = oldMatrix;
            }
        }
    }
}
#endif
