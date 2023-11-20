#if !CINEMACHINE_NO_CM2_SUPPORT
using System;
using UnityEngine;

namespace Unity.Cinemachine
{
    /// <summary>Used to read in CM2 LensSettings</summary>
    [Obsolete("LegacyLensSettings is deprecated. Use LensSettings instead.")]
    [Serializable]
    public struct LegacyLensSettings
    {
        public float FieldOfView;
        public float OrthographicSize;
        public float NearClipPlane;
        public float FarClipPlane;
        public float Dutch;
        public LensSettings.OverrideModes ModeOverride;

        public Camera.GateFitMode GateFit;
        [HideInInspector] public Vector2 m_SensorSize;
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
            lens.PhysicalProperties.FocusDistance = FocusDistance;
            lens.PhysicalProperties.Iso = Iso;
            lens.PhysicalProperties.ShutterSpeed = ShutterSpeed;
            lens.PhysicalProperties.Aperture = Aperture;
            lens.PhysicalProperties.BladeCount = BladeCount;
            lens.PhysicalProperties.Curvature = Curvature;
            lens.PhysicalProperties.BarrelClipping = BarrelClipping;
            lens.PhysicalProperties.Anamorphism = Anamorphism;
            return lens;
        }

        public void SetFromLensSettings(LensSettings src)
        {
            FieldOfView = src.FieldOfView;
            OrthographicSize = src.OrthographicSize;
            NearClipPlane = src.NearClipPlane;
            FarClipPlane = src.FarClipPlane;
            Dutch = src.Dutch;
            ModeOverride = src.ModeOverride;

            GateFit = src.PhysicalProperties.GateFit;
            m_SensorSize = src.PhysicalProperties.SensorSize;
            LensShift = src.PhysicalProperties.LensShift;
            FocusDistance = src.PhysicalProperties.FocusDistance;
            Iso = src.PhysicalProperties.Iso;
            ShutterSpeed = src.PhysicalProperties.ShutterSpeed;
            Aperture = src.PhysicalProperties.Aperture;
            BladeCount = src.PhysicalProperties.BladeCount;
            Curvature = src.PhysicalProperties.Curvature;
            BarrelClipping = src.PhysicalProperties.BarrelClipping;
            Anamorphism = src.PhysicalProperties.Anamorphism;
        }
        
        /// <summary>Make sure lens settings are sane.  Call this from OnValidate().</summary>
        public void Validate()
        {
            FarClipPlane = Mathf.Max(FarClipPlane, NearClipPlane + 0.001f);
            FieldOfView = Mathf.Clamp(FieldOfView, 0.01f, 179f);
            FocusDistance = Mathf.Max(FocusDistance, 0.01f);
            ShutterSpeed = Mathf.Max(0, ShutterSpeed);
            Aperture = Mathf.Clamp(Aperture, Camera.kMinAperture, Camera.kMaxAperture);
            BladeCount = Mathf.Clamp(BladeCount, Camera.kMinBladeCount, Camera.kMaxBladeCount);
            BarrelClipping = Mathf.Clamp01(BarrelClipping);
            Curvature.x = Mathf.Clamp(Curvature.x, Camera.kMinAperture, Camera.kMaxAperture);
            Curvature.y = Mathf.Clamp(Curvature.y, Curvature.x, Camera.kMaxAperture);
            Anamorphism = Mathf.Clamp(Anamorphism, -1, 1);
        }

        /// <summary>Default Lens Settings</summary>
        public static LegacyLensSettings Default => new ()
        {
            FieldOfView = 40f,
            OrthographicSize = 10f,
            NearClipPlane = 0.1f,
            FarClipPlane = 5000f,
            Dutch = 0,
            ModeOverride = LensSettings.OverrideModes.None,
            m_SensorSize = new Vector2(21.946f, 16.002f),
            GateFit = Camera.GateFitMode.Horizontal,
            FocusDistance = 10,
            LensShift = Vector2.zero,
            Iso = 200,
            ShutterSpeed = 0.005f,
            Aperture = 16,
            BladeCount = 5,
            Curvature = new Vector2(2, 11),
            BarrelClipping = 0.25f,
            Anamorphism = 0
        };
    }
}
#endif
