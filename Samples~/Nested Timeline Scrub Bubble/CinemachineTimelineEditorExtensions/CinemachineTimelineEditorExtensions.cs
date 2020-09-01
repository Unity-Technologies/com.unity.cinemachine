using UnityEditor;

/// <summary>
/// This class installs the optional TimelineEditorExtension so that 
/// Cinemachine's Timeline Scrub Bubble feature works fully with nested timelines.
/// 
/// In the future, this extension will be deprecated when Timeline adds the missing API natively
/// </summary>
[InitializeOnLoad]
class CinemachineTimelineEditorExtensions 
{
    static CinemachineTimelineEditorExtensions() 
    { 
        CinemachineShotClipEditor.TimelineGlobalToLocalTime = TimelineEditorExtensions.ToLocalTime; 
    } 
}
