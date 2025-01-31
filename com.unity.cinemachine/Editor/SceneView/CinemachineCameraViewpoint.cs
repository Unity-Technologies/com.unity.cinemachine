#if UNITY_2023_2_OR_NEWER
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Cinemachine.Editor
{
    class CinemachineCameraViewpoint : Viewpoint<CinemachineCamera>, ICameraLensData
    {
        public CinemachineCameraViewpoint(CinemachineCamera target) : base(target)
        {
        }

        public override Quaternion Rotation
        {
            get => base.Rotation;
            set
            {
                base.Rotation = value;
                Target.InternalUpdateCameraState(Vector3.up, 0f);
            }
        }

        public override Vector3 Position
        {
            get => base.Position;
            set
            {
                base.Position = value;
                Target.InternalUpdateCameraState(Vector3.up, 0f);
            }
        }

        public float FieldOfView
        {
            get => Target.Lens.FieldOfView;
            set
            {
                var currentLens = Target.Lens;
                currentLens.FieldOfView = value;
                Target.Lens = currentLens;
            }
        }

        public float FocalLength
        {
            get
            {
                return Camera.FieldOfViewToFocalLength(Target.Lens.FieldOfView, Target.Lens.PhysicalProperties.SensorSize.y);
            }
            set
            {
                var currentLens = Target.Lens;
                currentLens.FieldOfView = Camera.FocalLengthToFieldOfView(value, Target.Lens.PhysicalProperties.SensorSize.y);
                Target.Lens = currentLens;
            }
        }

        public bool Orthographic
        {
            get => Target.Lens.Orthographic;
            set
            {
                var currentLens = Target.Lens;
                currentLens.ModeOverride = (value) ? LensSettings.OverrideModes.Orthographic : LensSettings.OverrideModes.None;
                Target.Lens = currentLens;
            }
        }

        public float OrthographicSize
        {
            get => Target.Lens.OrthographicSize;
            set
            {
                var currentLens = Target.Lens;
                currentLens.OrthographicSize = value;
                Target.Lens = currentLens;
            }
        }

        public float NearClipPlane => Target.Lens.NearClipPlane;

        public float FarClipPlane => Target.Lens.FarClipPlane;

        public bool UsePhysicalProperties => Target.Lens.IsPhysicalCamera;

        public Vector2 SensorSize => Target.Lens.PhysicalProperties.SensorSize;

        public Vector2 LensShift => Target.Lens.PhysicalProperties.LensShift;

        public Camera.GateFitMode GateFit => Target.Lens.PhysicalProperties.GateFit;

        // TODO: Surface text message through a Label to the user to tell when a constraint is being changed.
        //public override VisualElement CreateVisualElement()
        //{
        //    return new VisualElement();
        //}
    }
}
#endif