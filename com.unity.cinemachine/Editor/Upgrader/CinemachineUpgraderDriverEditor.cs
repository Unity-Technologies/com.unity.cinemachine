#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace Cinemachine.Editor
{
    /// <summary>
    /// Temporary class for easy testing only.
    /// </summary>
    [CustomEditor(typeof(CinemachineUpgraderDriver))]
    public class CinemachineUpgraderDriverEditor : UnityEditor.Editor
    {
        CinemachineUpgrader m_Upgrader;
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            var driver = target as CinemachineUpgraderDriver;
            if (driver == null)
                return;
            
            CinemachineCore.sShowHiddenObjects = driver.showHiddenGameObjects;
            
            if (driver.triggerUpgrade)
            {
                driver.triggerUpgrade = false;
                m_Upgrader = new CinemachineUpgrader();
                m_Upgrader.UpgradeAll();
            }
        }
    }
}
#endif
