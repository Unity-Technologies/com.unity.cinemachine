using Cinemachine;
using Cinemachine.Editor;
using UnityEditor;

namespace Editor.Utility
{
    public class EditorWithIcon : UnityEditor.Editor
    {
        protected virtual void Awake()
        {
            var icon = AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(GetIconPath());
            EditorGUIUtility.SetIconForObject(target, icon);
        }

        protected virtual string GetIconPath()
        {
            switch (target)
            {
                case CinemachineExtension:
                    return ScriptableObjectUtility.CinemachineRelativeInstallPath + "/Editor/EditorResources/Icons/CmExtensions/" +
                        (EditorGUIUtility.isProSkin ? "Dark/" : "Light/") + "CinemachineExtensions@256.png";
                case CinemachineComponentBase:
                    return ScriptableObjectUtility.CinemachineRelativeInstallPath + "/Editor/EditorResources/Icons/CmComponent/" +
                        (EditorGUIUtility.isProSkin ? "Dark/" : "Light/") + "CMComponent@256.png";
                default:
                    return ScriptableObjectUtility.CinemachineRelativeInstallPath + "/Editor/EditorResources/Icons/CmCamera/" +
                        (EditorGUIUtility.isProSkin ? "Dark/" : "Light/") + "VirtualCamera@256.png";
            }
        } 
    }
}
