using UnityEngine;
using System;

namespace Unity.Cinemachine
{
    /// <summary>
    /// Describes the FOV and clip planes for a camera.  This generally mirrors the Unity Camera's
    /// lens settings, and will be used to drive the Unity camera when the vcam is active.
    /// </summary>
    [Serializable]
    public struct LensSettings
    {
        /// <summary>
        /// This is the camera vertical field of view in degrees. Display will be in vertical degress, unless the
        /// associated camera has its FOV axis setting set to Horizontal, in which case display will 
        /// be in horizontal degress.  Internally, it is always vertical degrees.  
        /// For cinematic people, a 50mm lens on a super-35mm sensor would equal a 19.6 degree FOV.
        /// </summary>
        [Tooltip("This setting controls the Field of View or Local Length of the lens, depending "
            + "on whether the camera mode is physical or nonphysical.  Field of View can be either horizontal "
            + "or vertical, depending on the setting in the Camera component.")]
        public float FieldOfView;

        /// <summary>
        /// When using an orthographic camera, this defines the half-height, in world
        /// co-ordinates, of the camera view.
        /// </summary>
        [Tooltip("When using an orthographic camera, this defines the half-height, in world "
            + "coordinates, of the camera view.")]
        public float OrthographicSize;

        /// <summary>
        /// The near clip plane for this LensSettings
        /// </summary>
        [Tooltip("This defines the near region in the renderable range of the camera frustum. "
            + "Raising this value will stop the game from drawing things near the camera, which "
            + "can sometimes come in handy.  Larger values will also increase your shadow resolution.")]
        public float NearClipPlane;

        /// <summary>
        /// The far clip plane for this LensSettings
        /// </summary>
        [Tooltip("This defines the far region of the renderable range of the camera frustum. Typically "
            + "you want to set this value as low as possible without cutting off desired distant objects")]
        public float FarClipPlane;

        /// <summary>
        /// The dutch (tilt) to be applied to the camera. In degrees
        /// </summary>
        [Tooltip("Camera Z roll, or tilt, in degrees.")]
        public float Dutch;

        /// <summary>
        /// This enum controls how the Camera settings are driven.  Some settings
        /// can be pulled from the main camera, or pushed to it, depending on these values.
        /// </summary>
        public enum OverrideModes
        {
            /// <summary> Perspective/Ortho, IsPhysical 
            /// will not be changed in Unity Camera.  This is the default setting.</summary>
            None = 0,
            /// <summary>Orthographic projection mode will be pushed to the Unity Camera</summary>
            Orthographic,
            /// <summary>Perspective projection mode will be pushed to the Unity Camera</summary>
            Perspective,
            /// <summary>A physically-modeled Perspective projection type will be pushed 
            /// to the Unity Camera</summary>
            Physical
        }

        /// <summary>
        /// Allows you to select a different camera mode to apply to the Camera component
        /// when Cinemachine activates this Virtual Camera. 
        /// </summary>
        [Tooltip("Allows you to select a different camera mode to apply to the Camera component "
            + "when Cinemachine activates this Virtual Camera.")]
        public OverrideModes ModeOverride;

        /// <summary>These are settings that are used only if IsPhysicalCamera is true.</summary>
        [Serializable]
        [Tooltip("These are settings that are used only if IsPhysicalCamera is true")]
        public struct PhysicalSettings
        {
            /// <summary>How the image is fitted to the sensor if the aspect ratios differ</summary>
            [Tooltip("How the image is fitted to the sensor if the aspect ratios differ")]
            public Camera.GateFitMode GateFit;

            /// <summary>This is the actual size of the image sensor (in mm).</summary>
            [SensorSizeProperty]
            [Tooltip("This is the actual size of the image sensor (in mm)")]
            public Vector2 SensorSize;

            /// <summary>Position of the gate relative to the film back</summary>
            [Tooltip("Position of the gate relative to the film back")]
            public Vector2 LensShift;

            /// <summary>Distance from the camera lens at which focus is sharpest.  
            /// The Depth of Field Volume override uses this value if you set FocusDistanceMode to Camera</summary>
            [Tooltip("Distance from the camera lens at which focus is sharpest.  The Depth of Field Volume "
                + "override uses this value if you set FocusDistanceMode to Camera")]
            public float FocusDistance;

            /// <summary>The sensor sensitivity (ISO)</summary>
            [Tooltip("The sensor sensitivity (ISO)")]
            public int Iso;

            /// <summary>The exposure time, in seconds</summary>
            [Tooltip("The exposure time, in seconds")]
            public float ShutterSpeed;

            /// <summary>The aperture number, in f-stop</summary>
            [Tooltip("The aperture number, in f-stop")]
            [Range(Camera.kMinAperture, Camera.kMaxAperture)]
            public float Aperture;

            /// <summary>The number of diaphragm blades</summary>
            [Tooltip("The number of diaphragm blades")]
            [Range(Camera.kMinBladeCount, Camera.kMaxBladeCount)]
            public int BladeCount;

            /// <summary>Maps an aperture range to blade curvature</summary>
            [Tooltip("Maps an aperture range to blade curvature")]
            [MinMaxRangeSlider(Camera.kMinAperture, Camera.kMaxAperture)]
            public Vector2 Curvature;

            /// <summary>The strength of the "cat-eye" effect on bokeh (optical vignetting)</summary>
            [Tooltip("The strength of the \"cat-eye\" effect on bokeh (optical vignetting)")]
            [Range(0, 1)]
            public float BarrelClipping;

            /// <summary>Stretches the sensor to simulate an anamorphic look.  Positive values distort 
            /// the camera vertically, negative values distore the camera horizontally</summary>
            [Tooltip("Stretches the sensor to simulate an anamorphic look.  Positive values distort the "
                + "camera vertically, negative values distort the camera horizontally")]
            [Range(-1, 1)]
            public float Anamorphism;
        }

        /// <summary>
        /// The physical settings of the lens, valid only when camera is set to Physical mode.
        /// </summary>
        public PhysicalSettings PhysicalProperties;

        bool m_OrthoFromCamera;
        bool m_PhysicalFromCamera;
        float m_AspectFromCamera;

#if UNITY_EDITOR
        // Needed for knowing how to display FOV (horizontal or vertical)
        // This should really be a global Unity setting, but for now there is no better way than this!
        Camera m_SourceCamera;
        internal bool UseHorizontalFOV
        {
            get
            {
                if (m_SourceCamera == null)
                    return false;
                var p = new UnityEditor.SerializedObject(m_SourceCamera).FindProperty("m_FOVAxisMode");
                return p != null && p.intValue == (int)Camera.FieldOfViewAxis.Horizontal;
            }
        }
#endif

        /// <summary>
        /// This is set every frame by the virtual camera, based on the value found in the
        /// currently associated Unity camera.
        /// Do not set this property.  Instead, use the ModeOverride field to set orthographic mode.
        /// </summary>
        public bool Orthographic => ModeOverride == OverrideModes.Orthographic 
            || (ModeOverride == OverrideModes.None && m_OrthoFromCamera);

        /// <summary>
        /// This property will be true if the camera mode is set, either directly or 
        /// indirectly, to Physical Camera
        /// Do not set this property.  Instead, use the ModeOverride field to set physical mode.
        /// </summary>
        public bool IsPhysicalCamera => ModeOverride == OverrideModes.Physical 
            || (ModeOverride == OverrideModes.None && m_PhysicalFromCamera);
                
        /// <summary>
        /// For physical cameras, this is the Sensor aspect.  
        /// For nonphysical cameras, this is the screen aspect pulled from the camera, if any.
        /// </summary>
        public float Aspect => IsPhysicalCamera 
            ? PhysicalProperties.SensorSize.x / PhysicalProperties.SensorSize.y : m_AspectFromCamera;

        /// <summary>Default Lens Settings</summary>
        public static LensSettings Default => new ()
        {
            FieldOfView = 40f,
            OrthographicSize = 10f,
            NearClipPlane = 0.1f,
            FarClipPlane = 5000f,
            Dutch = 0,
            ModeOverride = OverrideModes.None,

            PhysicalProperties = new ()
            {
                SensorSize = new Vector2(21.946f, 16.002f),
                GateFit = Camera.GateFitMode.Horizontal,
                FocusDistance = 10,
                LensShift = Vector2.zero,
                Iso = 200,
                ShutterSpeed = 0.005f,
                Aperture = 16,
                BladeCount = 5,
                Curvature = new Vector2(2, 11),
                BarrelClipping = 0.25f,
                Anamorphism = 0,
            },
            m_AspectFromCamera = 1
        };

        /// <summary>
        /// Creates a new LensSettings, copying the values from the
        /// supplied Camera
        /// </summary>
        /// <param name="fromCamera">The Camera from which the FoV, near
        /// and far clip planes will be copied.</param>
        /// <returns>The LensSettings as extracted from the supplied Camera</returns>
        public static LensSettings FromCamera(Camera fromCamera)
        {
            LensSettings lens = Default;
            if (fromCamera != null)
            {
                lens.PullInheritedPropertiesFromCamera(fromCamera);

                lens.FieldOfView = fromCamera.fieldOfView;
                lens.OrthographicSize = fromCamera.orthographicSize;
                lens.NearClipPlane = fromCamera.nearClipPlane;
                lens.FarClipPlane = fromCamera.farClipPlane;

                if (lens.IsPhysicalCamera)
                {
                    lens.FieldOfView = Camera.FocalLengthToFieldOfView(
                        Mathf.Max(0.01f, fromCamera.focalLength), fromCamera.sensorSize.y);
                    lens.PhysicalProperties.SensorSize = fromCamera.sensorSize;
                    lens.PhysicalProperties.LensShift = fromCamera.lensShift;
                    lens.PhysicalProperties.GateFit = fromCamera.gateFit;
                    lens.PhysicalProperties.FocusDistance = fromCamera.focusDistance;
                    lens.PhysicalProperties.Iso = fromCamera.iso;
                    lens.PhysicalProperties.ShutterSpeed = fromCamera.shutterSpeed;
                    lens.PhysicalProperties.Aperture = fromCamera.aperture;
                    lens.PhysicalProperties.BladeCount = fromCamera.bladeCount;
                    lens.PhysicalProperties.Curvature = fromCamera.curvature;
                    lens.PhysicalProperties.BarrelClipping = fromCamera.barrelClipping;
                    lens.PhysicalProperties.Anamorphism = fromCamera.anamorphism;
                }
            }
            return lens;
        }

        /// <summary>
        /// In the event that there is no camera mode override, camera mode is driven
        /// by the Camera's state.
        /// </summary>
        /// <param name="camera">The Camera from which we will take the info</param>
        public void PullInheritedPropertiesFromCamera(Camera camera)
        {
            if (ModeOverride == OverrideModes.None)
            {
                m_OrthoFromCamera = camera.orthographic;
                m_PhysicalFromCamera = camera.usePhysicalProperties;
            }
            m_AspectFromCamera = camera.aspect;
#if UNITY_EDITOR
            m_SourceCamera = camera; // hack because of missing Unity API to get horizontal or vertical fov mode
#endif
        }

        /// <summary>
        /// Copy the properties controlled by camera mode.  If ModeOverride is None, then
        /// some internal state information must be transferred.
        /// </summary>
        /// <param name="fromLens">The LensSettings from which we will take the info</param>
        public void CopyCameraMode(ref LensSettings fromLens)
        {
            ModeOverride = fromLens.ModeOverride;
            if (ModeOverride == OverrideModes.None)
            {
                m_OrthoFromCamera = fromLens.Orthographic;
                m_PhysicalFromCamera = fromLens.IsPhysicalCamera;
            }
            m_AspectFromCamera = fromLens.m_AspectFromCamera;
        }

        /// <summary>
        /// Linearly blends the fields of two LensSettings and returns the result
        /// </summary>
        /// <param name="lensA">The LensSettings to blend from</param>
        /// <param name="lensB">The LensSettings to blend to</param>
        /// <param name="t">The interpolation value. Internally clamped to the range [0,1]</param>
        /// <returns>Interpolated settings</returns>
        public static LensSettings Lerp(LensSettings lensA, LensSettings lensB, float t)
        {
            t = Mathf.Clamp01(t);
            // non-lerpable settings taken care of here
            if (t < 0.5f)
            {
                var blendedLens = lensA; 
                blendedLens.Lerp(lensB, t);
                return blendedLens;
            }
            else
            {
                var blendedLens = lensB; 
                blendedLens.Lerp(lensA, 1 - t);
                return blendedLens;
            }
        }

        /// <summary>
        /// Lerp the interpolatable values. Values that can't be interpolated remain intact.
        /// </summary>
        /// <param name="lensB">The lens containing the values to combine with this one</param>
        /// <param name="t">The weight of LensB's values.</param>
        public void Lerp(in LensSettings lensB, float t)
        {
            FarClipPlane = Mathf.Lerp(FarClipPlane, lensB.FarClipPlane, t);
            NearClipPlane = Mathf.Lerp(NearClipPlane, lensB.NearClipPlane, t);
            FieldOfView = Mathf.Lerp(FieldOfView, lensB.FieldOfView, t);
            OrthographicSize = Mathf.Lerp(OrthographicSize, lensB.OrthographicSize, t);
            Dutch = Mathf.Lerp(Dutch, lensB.Dutch, t);
            PhysicalProperties.SensorSize = Vector2.Lerp(PhysicalProperties.SensorSize, lensB.PhysicalProperties.SensorSize, t);
            PhysicalProperties.LensShift = Vector2.Lerp(PhysicalProperties.LensShift, lensB.PhysicalProperties.LensShift, t);
            PhysicalProperties.FocusDistance = Mathf.Lerp(PhysicalProperties.FocusDistance, lensB.PhysicalProperties.FocusDistance, t);
            PhysicalProperties.Iso = Mathf.RoundToInt(Mathf.Lerp((float)PhysicalProperties.Iso, (float)lensB.PhysicalProperties.Iso, t));
            PhysicalProperties.ShutterSpeed = Mathf.Lerp(PhysicalProperties.ShutterSpeed, lensB.PhysicalProperties.ShutterSpeed, t);
            PhysicalProperties.Aperture = Mathf.Lerp(PhysicalProperties.Aperture, lensB.PhysicalProperties.Aperture, t);
            PhysicalProperties.BladeCount = Mathf.RoundToInt(Mathf.Lerp(PhysicalProperties.BladeCount, lensB.PhysicalProperties.BladeCount, t));;
            PhysicalProperties.Curvature = Vector2.Lerp(PhysicalProperties.Curvature, lensB.PhysicalProperties.Curvature, t);
            PhysicalProperties.BarrelClipping = Mathf.Lerp(PhysicalProperties.BarrelClipping, lensB.PhysicalProperties.BarrelClipping, t);
            PhysicalProperties.Anamorphism = Mathf.Lerp(PhysicalProperties.Anamorphism, lensB.PhysicalProperties.Anamorphism, t);
        }

        /// <summary>Make sure lens settings are sane.  Call this from OnValidate().</summary>
        public void Validate()
        {
            FarClipPlane = Mathf.Max(FarClipPlane, NearClipPlane + 0.001f);
            FieldOfView = Mathf.Clamp(FieldOfView, 0.01f, 179f);
            PhysicalProperties.SensorSize.x = Mathf.Max(PhysicalProperties.SensorSize.x, 0.1f);
            PhysicalProperties.SensorSize.y = Mathf.Max(PhysicalProperties.SensorSize.y, 0.1f);
            PhysicalProperties.FocusDistance = Mathf.Max(PhysicalProperties.FocusDistance, 0.01f);
            if (m_AspectFromCamera == 0)
                m_AspectFromCamera = 1;
            PhysicalProperties.ShutterSpeed = Mathf.Max(0, PhysicalProperties.ShutterSpeed);
            PhysicalProperties.Aperture = Mathf.Clamp(PhysicalProperties.Aperture, Camera.kMinAperture, Camera.kMaxAperture);
            PhysicalProperties.BladeCount = Mathf.Clamp(PhysicalProperties.BladeCount, Camera.kMinBladeCount, Camera.kMaxBladeCount);
            PhysicalProperties.BarrelClipping = Mathf.Clamp01(PhysicalProperties.BarrelClipping);
            PhysicalProperties.Curvature.x = Mathf.Clamp(PhysicalProperties.Curvature.x, Camera.kMinAperture, Camera.kMaxAperture);
            PhysicalProperties.Curvature.y = Mathf.Clamp(PhysicalProperties.Curvature.y, PhysicalProperties.Curvature.x, Camera.kMaxAperture);
            PhysicalProperties.Anamorphism = Mathf.Clamp(PhysicalProperties.Anamorphism, -1, 1);
        }

        /// <summary>
        /// Compare two lens settings objects for approximate equality
        /// </summary>
        /// <param name="a">First LensSettings</param>
        /// <param name="b">Second Lens Settings</param>
        /// <returns>True if the two lenses are approximately equal</returns>
        public static bool AreEqual(ref LensSettings a, ref LensSettings b)
        {
            return Mathf.Approximately(a.NearClipPlane, b.NearClipPlane)
                && Mathf.Approximately(a.FarClipPlane, b.FarClipPlane)
                && Mathf.Approximately(a.OrthographicSize, b.OrthographicSize)
                && Mathf.Approximately(a.FieldOfView, b.FieldOfView)
                && Mathf.Approximately(a.Dutch, b.Dutch)
                && Mathf.Approximately(a.PhysicalProperties.LensShift.x, b.PhysicalProperties.LensShift.x)
                && Mathf.Approximately(a.PhysicalProperties.LensShift.y, b.PhysicalProperties.LensShift.y)

                && Mathf.Approximately(a.PhysicalProperties.SensorSize.x, b.PhysicalProperties.SensorSize.x)
                && Mathf.Approximately(a.PhysicalProperties.SensorSize.y, b.PhysicalProperties.SensorSize.y)
                && a.PhysicalProperties.GateFit == b.PhysicalProperties.GateFit
                && Mathf.Approximately(a.PhysicalProperties.FocusDistance, b.PhysicalProperties.FocusDistance)
                && Mathf.Approximately(a.PhysicalProperties.Iso, b.PhysicalProperties.Iso)
                && Mathf.Approximately(a.PhysicalProperties.ShutterSpeed, b.PhysicalProperties.ShutterSpeed)
                && Mathf.Approximately(a.PhysicalProperties.Aperture, b.PhysicalProperties.Aperture)
                && a.PhysicalProperties.BladeCount == b.PhysicalProperties.BladeCount
                && Mathf.Approximately(a.PhysicalProperties.Curvature.x, b.PhysicalProperties.Curvature.x)
                && Mathf.Approximately(a.PhysicalProperties.Curvature.y, b.PhysicalProperties.Curvature.y)
                && Mathf.Approximately(a.PhysicalProperties.BarrelClipping, b.PhysicalProperties.BarrelClipping)
                && Mathf.Approximately(a.PhysicalProperties.Anamorphism, b.PhysicalProperties.Anamorphism)
                ;
        }
    }
}
