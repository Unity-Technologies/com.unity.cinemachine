using Cinemachine;
using Cinemachine.Editor;
using Editor.Utility;
using UnityEditor;

namespace Editor.Editors
{
    [CustomEditor(typeof(CinemachineSplineCart))]
    [CanEditMultipleObjects]
    public class CinemachineSplineCartEditor : EditorWithIcon
    {
        protected override string GetIconPath() =>
            ScriptableObjectUtility.CinemachineRelativeInstallPath + "/Editor/EditorResources/Icons/SplineTrack/" +
            (EditorGUIUtility.isProSkin ? "Dark/" : "Light/") + "DollyTrack@128.png";
    }
}
