using Cinemachine;
using Cinemachine.Editor;
using UnityEditor;
using UnityEngine;

namespace Editor.Editors
{
    [CustomEditor(typeof(CinemachineSplineRoll))]
    public class CinemachineSplineRollEditor : BaseEditor<CinemachineSplineRoll>
    {
        public void Awake()
        {
            var monoImporter = target as MonoImporter;
            if (monoImporter != null)
            {
                var monoScript = monoImporter.GetScript();
                var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(GetIconPath());
                monoImporter.SetIcon(icon);
                monoImporter.SaveAndReimport();
                EditorGUIUtility.SetIconForObject(monoScript, icon);
            }
        }
        
        string GetIconPath()
        {
            var isProSkin = EditorGUIUtility.isProSkin;
            Debug.Log(ScriptableObjectUtility.CinemachineRelativeInstallPath + "/Editor/EditorResources/Icons/Spline Track/" +
                (isProSkin ? "Dark/" : "Light/") + "DollyTrack@128.png");
            return ScriptableObjectUtility.CinemachineRelativeInstallPath + "/Editor/EditorResources/Icons/Spline Track/" +
                (isProSkin ? "Dark/" : "Light/") + "DollyTrack@128.png";
        }
    }
}
