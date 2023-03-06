using System;
using UnityEngine;

namespace Cinemachine
{
    /// <summary>Used to read in CM2 LensSettings</summary>
    [Serializable]
    internal struct LegacyLensSettings
    {
        public float FieldOfView;
        public float OrthographicSize;
        public float NearClipPlane;
        public float FarClipPlane;
        public float Dutch;
        public LensSettings.OverrideModes ModeOverride;

        public Camera.GateFitMode GateFit;
        public Vector2 m_SensorSize;
        public Vector2 LensShift;
        public float FocusDistance;
        public int Iso;
        public float ShutterSpeed;
        public float Aperture;
        public int BladeCount;
        public Vector2 Curvature;
        public float BarrelClipping;
        public float Anamorphism;

        public LensSettings ToLensSettings()
        {
            var lens = new LensSettings
            {
                FieldOfView = FieldOfView,
                OrthographicSize = OrthographicSize,
                NearClipPlane = NearClipPlane,
                FarClipPlane = FarClipPlane,
                Dutch = Dutch,
                ModeOverride = ModeOverride,
                PhysicalProperties = LensSettings.Default.PhysicalProperties
            };
            lens.PhysicalProperties.GateFit = GateFit;
            lens.PhysicalProperties.SensorSize = m_SensorSize;
            lens.PhysicalProperties.LensShift = LensShift;
#if CINEMACHINE_HDRP
            lens.PhysicalProperties.FocusDistance = FocusDistance;
            lens.PhysicalProperties.Iso = Iso;
            lens.PhysicalProperties.ShutterSpeed = ShutterSpeed;
            lens.PhysicalProperties.Aperture = Aperture;
            lens.PhysicalProperties.BladeCount = BladeCount;
            lens.PhysicalProperties.Curvature = Curvature;
            lens.PhysicalProperties.BarrelClipping = BarrelClipping;
            lens.PhysicalProperties.Anamorphism = Anamorphism;
#endif
            return lens;
        }
    }
}

