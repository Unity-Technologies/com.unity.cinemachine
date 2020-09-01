using System.Collections.Generic;
using UnityEditor;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineExternalCamera))]
    [CanEditMultipleObjects]
    internal class CinemachineExternalCameraEditor 
        : CinemachineVirtualCameraBaseEditor<CinemachineExternalCamera>
    {
        protected override void GetExcludedPropertiesInInspector(List<string> excluded)
        {
            base.GetExcludedPropertiesInInspector(excluded);
            excluded.Add("Extensions");
        }
    }
}
