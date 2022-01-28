using UnityEditor;
using UnityEngine;
using Cinemachine.Utility;
using System.Collections.Generic;

namespace Cinemachine.Editor
{
    [CustomEditor(typeof(CinemachineTimelineCamera))]
    internal sealed class CinemachineTimelineCameraEditor
        : CinemachineVirtualCameraBaseEditor<CinemachineTimelineCamera>
    { }
}
