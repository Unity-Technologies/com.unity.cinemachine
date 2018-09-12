using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Cinemachine.Editor;

namespace Spectator.Editor
{
    internal static class SpectatorMenu
    {
        [MenuItem(CinemachineMenu.kCinemachineRootMenu + "SpectatorTuningConstants")]
        private static void CreateBlenderSettingAsset()
        {
            ScriptableObjectUtility.Create<SpectatorTuningConstants>();
        }
    }
}
