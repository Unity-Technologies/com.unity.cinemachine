using UnityEngine;
using UnityEditor;

namespace Unity.Cinemachine.Editor
{
    class CinemachineCameraViewpoint : Viewpoint<CinemachineCamera>, ICameraLensData
    {
        public CinemachineCameraViewpoint(CinemachineCamera target) : base(target)
        {
        }

        public Camera camera
        {
            get
            {
                return null;
            }
        }

        public float fieldOfView
        {
            get => target.Lens.FieldOfView;
            set
            {
                var currentLens = target.Lens;
                currentLens.FieldOfView = value;
                target.Lens = currentLens;
            }
        }

        public float focalLength
        {
            get
            {
                return Camera.FieldOfViewToFocalLength(target.Lens.FieldOfView, target.Lens.PhysicalProperties.SensorSize.y);
            }
            set
            {
                var currentLens = target.Lens;
                currentLens.FieldOfView = Camera.FocalLengthToFieldOfView(value, target.Lens.PhysicalProperties.SensorSize.y);
                target.Lens = currentLens;
            }
        }

        public bool orthographic
        {
            get => target.Lens.Orthographic;
            set
            {
                var currentLens = target.Lens;
                currentLens.ModeOverride = (value) ? LensSettings.OverrideModes.Orthographic : LensSettings.OverrideModes.None;
                target.Lens = currentLens;
            }
        }

        public float orthographicSize
        {
            get => target.Lens.OrthographicSize;
            set
            {
                var currentLens = target.Lens;
                currentLens.OrthographicSize = value;
                target.Lens = currentLens;
            }
        }

        public float nearPlane => target.Lens.NearClipPlane;

        public float farPlane => target.Lens.FarClipPlane;

        public bool usePhysicalProperties => target.Lens.IsPhysicalCamera;

        public Vector2 sensorSize => target.Lens.PhysicalProperties.SensorSize;

        public Vector2 lensShift => target.Lens.PhysicalProperties.LensShift;

        public Camera.GateFitMode gateFit => target.Lens.PhysicalProperties.GateFit;

        public override bool IsAvailable()
        {
            return true;
        }
    }
}