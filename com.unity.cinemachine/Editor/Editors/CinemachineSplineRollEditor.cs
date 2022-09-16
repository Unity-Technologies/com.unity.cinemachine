using Cinemachine;
using Cinemachine.Editor;
using UnityEditor;
using UnityEngine;

namespace Editor.Editors
{
    [CustomEditor(typeof(CinemachineSplineRoll))]
    public class CinemachineSplineRollEditor : BaseEditor<CinemachineSplineRoll>
    {
        protected override string GetIconPath() =>
            ScriptableObjectUtility.CinemachineRelativeInstallPath + "/Editor/EditorResources/Icons/SplineTrack/" +
            (EditorGUIUtility.isProSkin ? "Dark/" : "Light/") + "DollyTrack@128.png";
    }
}
