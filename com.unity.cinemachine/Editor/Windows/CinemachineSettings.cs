using UnityEngine;
using UnityEditor;
using System;

namespace Cinemachine.Editor
{
    class CinemachineSettings : AssetPostprocessor
    {
        // Represents a settings item that gets saved
        public abstract class Item<T> where T : IEquatable<T>
        {
            protected string m_Key;
            protected T m_DefaultValue;
            protected T m_CurrentValue;

            public Item(string key, T defaultValue) 
            { 
                m_Key = key; 
                m_DefaultValue = defaultValue; 
                m_CurrentValue = ReadPrefs();
            }

            public T Value 
            { 
                get => m_CurrentValue; 
                set 
                { 
                    if (!m_CurrentValue.Equals(value))
                    {
                        m_CurrentValue = value; 
                        WritePrefs(m_CurrentValue);
                    }
                }
            }

            public void Reset() => Value = m_DefaultValue;

            protected abstract T ReadPrefs();
            protected abstract void WritePrefs(T value);
        }

        // Type specializations for settings items
        public class BoolItem : Item<bool>
        {
            public BoolItem(string key, bool defaultValue) : base(key, defaultValue) {}
            protected override bool ReadPrefs() => EditorPrefs.GetBool(m_Key, m_DefaultValue);
            protected override void WritePrefs(bool value) => EditorPrefs.SetBool(m_Key, value);
        }

        public class IntItem : Item<int>
        {
            public IntItem(string key, int defaultValue) : base(key, defaultValue) {}
            protected override int ReadPrefs() => EditorPrefs.GetInt(m_Key, m_DefaultValue);
            protected override void WritePrefs(int value) => EditorPrefs.SetInt(m_Key, value);
        }

        public class FloatItem : Item<float>
        {
            public FloatItem(string key, float defaultValue) : base(key, defaultValue) {}
            protected override float ReadPrefs() => EditorPrefs.GetFloat(m_Key, m_DefaultValue);
            protected override void WritePrefs(float value) => EditorPrefs.SetFloat(m_Key, value);
        }

        public class StringItem : Item<string>
        {
            public StringItem(string key, string defaultValue) : base(key, defaultValue) {}
            protected override string ReadPrefs() => EditorPrefs.GetString(m_Key, m_DefaultValue);
            protected override void WritePrefs(string value) => EditorPrefs.SetString(m_Key, value);
        }
        
        public class ColorItem : Item<Color>
        {
            public ColorItem(string key, Color defaultValue) : base(key, defaultValue) {}
            protected override Color ReadPrefs() => UnpackColour(EditorPrefs.GetString(m_Key, PackColor(m_DefaultValue)));
            protected override void WritePrefs(Color value) => EditorPrefs.SetString(m_Key, PackColor(value));

            static Color UnpackColour(string str)
            {
                if (!string.IsNullOrEmpty(str))
                {
                    byte[] bytes = Base64Decode(str);
                    if ((bytes != null) && bytes.Length == 16)
                    {
                        float r = BitConverter.ToSingle(bytes, 0);
                        float g = BitConverter.ToSingle(bytes, 4);
                        float b = BitConverter.ToSingle(bytes, 8);
                        float a = BitConverter.ToSingle(bytes, 12);
                        return new Color(r, g, b, a);
                    }
                }
                return Color.white;
            }

            static string PackColor(Color col)
            {
                byte[] bytes = new byte[16];
                byte[] rBytes = BitConverter.GetBytes(col.r);
                byte[] gBytes = BitConverter.GetBytes(col.g);
                byte[] bBytes = BitConverter.GetBytes(col.b);
                byte[] aBytes = BitConverter.GetBytes(col.a);

                Buffer.BlockCopy(rBytes, 0, bytes, 0, 4);
                Buffer.BlockCopy(gBytes, 0, bytes, 4, 4);
                Buffer.BlockCopy(bBytes, 0, bytes, 8, 4);
                Buffer.BlockCopy(aBytes, 0, bytes, 12, 4);

                return Base64Encode(bytes);
            }

            static string Base64Encode(byte[] data) => Convert.ToBase64String(data);
            static byte[] Base64Decode(string base64EncodedData) => Convert.FromBase64String(base64EncodedData);
        }

        static void OnPostprocessAllAssets(
            string[] importedAssets, string[] deletedAssets, string[] movedAssets, 
            string[] movedFromAssetPaths, bool didDomainReload)
        {
            if (didDomainReload)
                EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
        }

        static void OnHierarchyGUI(int instanceID, Rect selectionRect)
        {
            var instance = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (instance == null)
                return; // object in process of being deleted

            if (CinemachineCorePrefs.ShowBrainIconInHierarchy.Value && instance.TryGetComponent<CinemachineBrain>(out _))
            {
                var texRect = new Rect(selectionRect.xMax - selectionRect.height, selectionRect.yMin, selectionRect.height, selectionRect.height);
                GUI.DrawTexture(texRect, CinemachineLogoTexture, ScaleMode.ScaleAndCrop);
            }
        }
        
        static Texture2D s_CinemachineLogoTexture = null;
        public static Texture2D CinemachineLogoTexture
        {
            get
            {
                if (s_CinemachineLogoTexture == null)
                    s_CinemachineLogoTexture = ssetDatabase.LoadAssetAtPath<Texture2D>(
                        $"{ScriptableObjectUtility.kPackageRoot}/Editor/EditorResources/cm_logo_sm.png");
                if (s_CinemachineLogoTexture != null)
                    s_CinemachineLogoTexture.hideFlags = HideFlags.DontSaveInEditor;
                return s_CinemachineLogoTexture;
            }
        }

        static Texture2D s_CinemachineHeader = null;
        static Texture2D CinemachineHeader
        {
            get
            {
                if (s_CinemachineHeader == null)
                    s_CinemachineHeader = AAssetDatabase.LoadAssetAtPath<Texture2D>(
                        $"{ScriptableObjectUtility.kPackageRoot}/Editor/EditorResources/cinemachine_header.tif");
                ;
                if (s_CinemachineHeader != null)
                    s_CinemachineHeader.hideFlags = HideFlags.DontSaveInEditor;
                return s_CinemachineHeader;
            }
        }

        public static event Action AdditionalCategories = CinemachineCorePrefs.DrawCoreSettings; // This one is first
        static Vector2 s_ScrollPosition = Vector2.zero;
        
        [SettingsProvider]
        static SettingsProvider CreateProjectSettingsProvider()
        {
            var provider = new SettingsProvider("Preferences/Cinemachine", SettingsScope.User);
            provider.guiHandler = (sarchContext) => OnGUI();
            return provider;
        }

        static void OnGUI()
        {
            if (CinemachineHeader != null)
            {
                const float kWidth = 350f;
                float aspectRatio = (float)CinemachineHeader.height / (float)CinemachineHeader.width;
                GUILayout.BeginScrollView(Vector2.zero, false, false, GUILayout.Width(kWidth), GUILayout.Height(kWidth * aspectRatio));
                var texRect = new Rect(0f, 0f, kWidth, kWidth * aspectRatio);

                GUILayout.BeginArea(texRect);
                GUI.DrawTexture(texRect, CinemachineHeader, ScaleMode.ScaleToFit);
                GUILayout.EndArea();

                GUILayout.EndScrollView();
            }

            s_ScrollPosition = GUILayout.BeginScrollView(s_ScrollPosition);
            AdditionalCategories();
            GUILayout.EndScrollView();
        }
    }
}
