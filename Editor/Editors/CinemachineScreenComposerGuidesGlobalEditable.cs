using System;
using UnityEditor;

namespace Cinemachine.Editor
{
    [InitializeOnLoad]
    class CinemachineScreenComposerGuidesGlobalEditable // why not static? (same Q for CinemachineStoryboardMute)
    {
        static CinemachineScreenComposerGuidesGlobalEditable()
        {
            CinemachineScreenComposerGuides.sEditableGameWindowGuides = Enabled;
        }

        public static string kEnabledKey = "EditableScreenComposerGuides_Enabled";
        public static bool Enabled
        {
            get => EditorPrefs.GetBool(kEnabledKey, true);
            set
            {
                if (value != CinemachineScreenComposerGuides.sEditableGameWindowGuides)
                {
                    EditorPrefs.SetBool(kEnabledKey, value);
                    CinemachineScreenComposerGuides.sEditableGameWindowGuides = value;
                }
            }
        }
    }
}
