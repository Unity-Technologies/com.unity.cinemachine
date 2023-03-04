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

#if CINEMACHINE_HDRP
        public int Iso;
        public float ShutterSpeed;
        public float Aperture;
        public int BladeCount;
        public Vector2 Curvature;
        public float BarrelClipping;
        public float Anamorphism;
#endif
        public LensSettings ToLensSettings()
        {
            return new LensSettings
            {
                FieldOfView = FieldOfView,
                OrthographicSize = OrthographicSize,
                NearClipPlane = NearClipPlane,
                FarClipPlane = FarClipPlane,
                Dutch = Dutch,
                ModeOverride = ModeOverride,
                PhysicalProperties = new ()
                {
                    GateFit = GateFit,
                    SensorSize = m_SensorSize,
                    LensShift = LensShift,
                    FocusDistance = FocusDistance,
#if CINEMACHINE_HDRP
                    Iso = Iso,
                    ShutterSpeed = ShutterSpeed,
                    Aperture = Aperture,
                    BladeCount = BladeCount,
                    Curvature = Curvature,
                    BarrelClipping = BarrelClipping,
                    Anamorphism = Anamorphism
#endif
                }
            };
        }
    }
}

