using Cinemachine;
using Cinemachine.Editor;
using Editor.Utility;
using UnityEditor;
using UnityEngine;

namespace Editor.Editors
{
    [CustomEditor(typeof(CinemachineSplineRoll))]
    public class CinemachineSplineRollEditor : EditorWithIcon
    {
        protected override string GetIconPath() =>
            ScriptableObjectUtility.CinemachineRelativeInstallPath + "/Editor/EditorResources/Icons/SplineTrack/" +
            (EditorGUIUtility.isProSkin ? "Dark/" : "Light/") + "DollyTrack@128.png";
    }
}
