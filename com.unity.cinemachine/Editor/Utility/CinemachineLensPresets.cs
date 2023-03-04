using UnityEngine;
using System;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace Cinemachine.Editor
{
    /// <summary>
    /// User-definable named presets for lenses.  This is a Singleton asset, available in editor only
    /// </summary>
    [Serializable]
    public sealed class CinemachineLensPresets : ScriptableObject
    {
        static CinemachineLensPresets s_Instance = null;
        static bool s_AlreadySearched = false;

        /// <summary>Get the singleton instance of this object, or null if it doesn't exist</summary>
        public static CinemachineLensPresets InstanceIfExists
        {
            get
            {
                if (!s_AlreadySearched)
                {
                    s_AlreadySearched = true;
                    var guids = AssetDatabase.FindAssets("t:CinemachineLensPresets");
                    for (int i = 0; i < guids.Length && s_Instance == null; ++i)
                        s_Instance = AssetDatabase.LoadAssetAtPath<CinemachineLensPresets>(
                            AssetDatabase.GUIDToAssetPath(guids[i]));
                }
                return s_Instance;
            }
        }

        /// <summary>Get the singleton instance of this object.  Creates asset if nonexistent</summary>
        public static CinemachineLensPresets Instance
        {
            get
            {
                if (InstanceIfExists == null)
                {
                    var newAssetPath = EditorUtility.SaveFilePanelInProject(
                            "Create Lens Presets asset", "CinemachineLensPresets", "asset",
                            "This editor-only file will contain the lens presets for this project");
                    if (!string.IsNullOrEmpty(newAssetPath))
                    {
                        s_Instance = CreateInstance<CinemachineLensPresets>();
                        // Create some sample presets
                        s_Instance.Presets = new()
                        {
                            new () { Name = "21mm", FieldOfView = 60f },
                            new () { Name = "35mm", FieldOfView = 38f },
                            new () { Name = "58mm", FieldOfView = 23f },
                            new () { Name = "80mm", FieldOfView = 17f },
                            new () { Name = "125mm", FieldOfView = 10f }
                        };
                        AssetDatabase.CreateAsset(s_Instance, newAssetPath);
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                    }
                }
                return s_Instance;
            }
        }

        /// <summary>Lens Preset</summary>
        [Serializable]
        public struct Preset
        {
            /// <summary>
            /// Name of the preset
            /// </summary>
            [Tooltip("Lens Name")]
            [FormerlySerializedAs("m_Name")]
            public string Name;

            /// <summary>
            /// This is the camera view in vertical degrees. For cinematic people, a 50mm lens
            /// on a super-35mm sensor would equal a 19.6 degree FOV
            /// </summary>
            [RangeSlider(1f, 179f)]
            [Tooltip("This is the camera view in vertical degrees. For cinematic people, "
                + " a 50mm lens on a super-35mm sensor would equal a 19.6 degree FOV")]
            [FormerlySerializedAs("m_FieldOfView")]
            public float FieldOfView;
        }
        /// <summary>The array containing Preset definitions for nonphysical cameras</summary>
        [Tooltip("The array containing Preset definitions, for nonphysical cameras")]
        public List<Preset> Presets = new();

        /// <summary>Physical Lens Preset</summary>
        [Serializable]
        public struct PhysicalPreset
        {
            /// <summary>
            /// Name of the preset
            /// </summary>
            [Tooltip("Lens Name")]
            [FormerlySerializedAs("m_Name")]
            public string Name;

            /// <summary>
            /// This is the camera focal length in mm
            /// </summary>
            [Tooltip("This is the camera focal length in mm")]
            [FormerlySerializedAs("m_FocalLength")]
            public float FocalLength;

            /// <summary>For physical cameras only: how the image is fitted to the sensor 
            /// if the aspect ratios differ</summary>
            public Camera.GateFitMode GateFit;

            /// <summary>The actual size of the image sensor (in mm).</summary>
            [SensorSizeProperty] 
            public Vector2 SensorSize;
        
            /// <summary>Position of the gate relative to the film back</summary>
            public Vector2 LensShift;

#if CINEMACHINE_HDRP
            public int Iso;
            public float ShutterSpeed;
            [RangeSlider(Camera.kMinAperture, Camera.kMaxAperture)]
            public float Aperture;
            [RangeSlider(Camera.kMinBladeCount, Camera.kMaxBladeCount)]
            public int BladeCount;
            public Vector2 Curvature;
            [RangeSlider(0, 1)]
            public float BarrelClipping;
            [RangeSlider(-1, 1)]
            public float Anamorphism;
#endif
        }

        /// <summary>The array containing Preset definitions, for physical cameras</summary>
        [Tooltip("The array containing Preset definitions, for physical cameras")]
        public List<PhysicalPreset> PhysicalPresets = new();

        /// <summary>Get the index of the preset that matches the lens settings</summary>
        /// <param name="verticalFOV">Vertical field of view</param>
        /// <returns>the preset index, or -1 if no matching preset</returns>
        public int GetMatchingPreset(float verticalFOV) 
            => Presets.FindIndex(x => Mathf.Approximately(x.FieldOfView, verticalFOV));

        /// <summary>Get the index of the first preset that matches the preset name</summary>
        /// <param name="presetName">Name of the preset</param>
        /// <returns>the preset index, or -1 if no matching preset</returns>
        public int GetPresetIndex(string presetName)
            => Presets.FindIndex(x => x.Name == presetName);

        /// <summary>Get the index of the first physical preset that matches the preset name</summary>
        /// <param name="presetName">Name of the preset</param>
        /// <returns>the preset index, or -1 if no matching preset</returns>
        public int GetPhysicalPresetIndex(string presetName)
            => PhysicalPresets.FindIndex(x => x.Name == presetName);

        /// <summary>Get the index of the physical preset that matches the template</summary>
        /// <param name="p">Template nolding the preset tsettings.  Name is ignored.</param>
        /// <returns>the preset index, or -1 if no matching preset</returns>
        public int GetMatchingPhysicalPreset(PhysicalPreset p)
        {
            return PhysicalPresets.FindIndex(x => 
                Mathf.Approximately(x.FocalLength, p.FocalLength)
                && x.GateFit == p.GateFit
                && Mathf.Approximately(x.LensShift.x, p.LensShift.x)
                && Mathf.Approximately(x.LensShift.y, p.LensShift.y)
                && Mathf.Approximately(x.SensorSize.x, p.SensorSize.x)
                && Mathf.Approximately(x.SensorSize.y, p.SensorSize.y)
#if CINEMACHINE_HDRP
                && x.Iso == p.Iso
                && Mathf.Approximately(x.ShutterSpeed, p.ShutterSpeed)
                && Mathf.Approximately(x.Aperture, p.Aperture)
                &&x.BladeCount == p.BladeCount
                && Mathf.Approximately(x.Curvature.x, p.Curvature.x)
                && Mathf.Approximately(x.Curvature.y, p.Curvature.y)
                && Mathf.Approximately(x.BarrelClipping, p.BarrelClipping)
                && Mathf.Approximately(x.Anamorphism, p.Anamorphism)
#endif                
                );
        }
    }
}
