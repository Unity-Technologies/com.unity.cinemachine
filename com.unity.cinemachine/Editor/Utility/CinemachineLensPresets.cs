using UnityEngine;
using System;
using UnityEditor;
using System.Collections.Generic;

namespace Cinemachine.Editor
{
    /// <summary>
    /// User-definable named presets for lenses.  This is a Singleton asset, available in editor only
    /// </summary>
    [Serializable]
    public sealed class CinemachineLensPresets : ScriptableObject
    {
        private static CinemachineLensPresets sInstance = null;
        private static bool alreadySearched = false;

        /// <summary>Get the singleton instance of this object, or null if it doesn't exist</summary>
        public static CinemachineLensPresets InstanceIfExists
        {
            get
            {
                if (!alreadySearched)
                {
                    alreadySearched = true;
                    var guids = AssetDatabase.FindAssets("t:CinemachineLensPresets");
                    for (int i = 0; i < guids.Length && sInstance == null; ++i)
                        sInstance = AssetDatabase.LoadAssetAtPath<CinemachineLensPresets>(
                            AssetDatabase.GUIDToAssetPath(guids[i]));
                }
                return sInstance;
            }
        }

        /// <summary>Get the singleton instance of this object.  Creates asset if nonexistent</summary>
        public static CinemachineLensPresets Instance
        {
            get
            {
                if (InstanceIfExists == null)
                {
                    string newAssetPath = EditorUtility.SaveFilePanelInProject(
                            "Create Lens Presets asset", "CinemachineLensPresets", "asset",
                            "This editor-only file will contain the lens presets for this project");
                    if (!string.IsNullOrEmpty(newAssetPath))
                    {
                        sInstance = CreateInstance<CinemachineLensPresets>();
                        // Create some sample presets
                        List<Preset> defaults = new List<Preset>();
                        defaults.Add(new Preset() { m_Name = "21mm", m_FieldOfView = 60f } );
                        defaults.Add(new Preset() { m_Name = "35mm", m_FieldOfView = 38f } );
                        defaults.Add(new Preset() { m_Name = "58mm", m_FieldOfView = 23f } );
                        defaults.Add(new Preset() { m_Name = "80mm", m_FieldOfView = 17f } );
                        defaults.Add(new Preset() { m_Name = "125mm", m_FieldOfView = 10f } );
                        sInstance.m_Presets = defaults.ToArray();
                        AssetDatabase.CreateAsset(sInstance, newAssetPath);
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                    }
                }
                return sInstance;
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
            public string m_Name;

            /// <summary>
            /// This is the camera view in vertical degrees. For cinematic people, a 50mm lens
            /// on a super-35mm sensor would equal a 19.6 degree FOV
            /// </summary>
            [RangeSlider(1f, 179f)]
            [Tooltip("This is the camera view in vertical degrees. For cinematic people, "
                + " a 50mm lens on a super-35mm sensor would equal a 19.6 degree FOV")]
            public float m_FieldOfView;
        }
        /// <summary>The array containing Preset definitions for nonphysical cameras</summary>
        [Tooltip("The array containing Preset definitions, for nonphysical cameras")]
        public Preset[] m_Presets = Array.Empty<Preset>();

        /// <summary>Physical Lens Preset</summary>
        [Serializable]
        public struct PhysicalPreset
        {
            /// <summary>
            /// Name of the preset
            /// </summary>
            [Tooltip("Lens Name")]
            public string m_Name;

            /// <summary>
            /// This is the camera focal length in mm
            /// </summary>
            [Tooltip("This is the camera focal length in mm")]
            public float m_FocalLength;

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
        public PhysicalPreset[] m_PhysicalPresets = Array.Empty<PhysicalPreset>();

        /// <summary>Get the index of the preset that matches the lens settings</summary>
        /// <param name="verticalFOV">Vertical field of view</param>
        /// <returns>the preset index, or -1 if no matching preset</returns>
        public int GetMatchingPreset(float verticalFOV)
        {
            for (int i = 0; i < m_Presets.Length; ++i)
                if (Mathf.Approximately(m_Presets[i].m_FieldOfView, verticalFOV))
                    return i;
            return -1;
        }

        /// <summary>Get the index of the first preset that matches the preset name</summary>
        /// <param name="presetName">Name of the preset</param>
        /// <returns>the preset index, or -1 if no matching preset</returns>
        public int GetPresetIndex(string presetName)
        {
            for (int i = 0; i < m_Presets.Length; ++i)
                if (m_Presets[i].m_Name == presetName)
                    return i;
            return -1;
        }

        /// <summary>Get the index of the physical preset that matches the lens settings</summary>
        /// <param name="focalLength">Focal length to match</param>
        /// <returns>the preset index, or -1 if no matching preset</returns>
        public int GetMatchingPhysicalPreset(float focalLength)
        {
            for (int i = 0; i < m_PhysicalPresets.Length; ++i)
                if (Mathf.Approximately(m_PhysicalPresets[i].m_FocalLength, focalLength))
                    return i;
            return -1;
        }

        /// <summary>Get the index of the first physical preset that matches the preset name</summary>
        /// <param name="presetName">Name of the preset</param>
        /// <returns>the preset index, or -1 if no matching preset</returns>
        public int GetPhysicalPresetIndex(string presetName)
        {
            for (int i = 0; i < m_PhysicalPresets.Length; ++i)
                if (m_PhysicalPresets[i].m_Name == presetName)
                    return i;
            return -1;
        }

#if CINEMACHINE_HDRP
        /// <summary>Get the index of the physical preset that matches the lens settings</summary>
        /// <returns>the preset index, or -1 if no matching preset</returns>
        public int GetMatchingPhysicalPreset(
            float FocalLength,
            int Iso,
            float ShutterSpeed,
            float Aperture,
            int BladeCount,
            Vector2 Curvature,
            float BarrelClipping,
            float Anamorphism,
            Vector2 LensShift)
        {
            for (int i = 0; i < m_PhysicalPresets.Length; ++i)
            {
                if (Mathf.Approximately(m_PhysicalPresets[i].m_FocalLength, FocalLength)
                    && m_PhysicalPresets[i].Iso == Iso
                    && Mathf.Approximately(m_PhysicalPresets[i].ShutterSpeed, ShutterSpeed)
                    && Mathf.Approximately(m_PhysicalPresets[i].Aperture, Aperture)
                    && m_PhysicalPresets[i].BladeCount == BladeCount
                    && Mathf.Approximately(m_PhysicalPresets[i].Curvature.x, Curvature.x)
                    && Mathf.Approximately(m_PhysicalPresets[i].Curvature.y, Curvature.y)
                    && Mathf.Approximately(m_PhysicalPresets[i].BarrelClipping, BarrelClipping)
                    && Mathf.Approximately(m_PhysicalPresets[i].Anamorphism, Anamorphism)
                    && Mathf.Approximately(m_PhysicalPresets[i].LensShift.x, LensShift.x)
                    && Mathf.Approximately(m_PhysicalPresets[i].LensShift.y, LensShift.y))
                {
                    return i;
                }
            }
            return -1;
        }
#endif
    }
}
