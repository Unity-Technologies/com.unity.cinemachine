using UnityEditor.Timeline;

public class TimelineEditorExtensions 
{
    public static double ToLocalTime(double globalTime)
    {
        var window = TimelineEditor.window as TimelineWindow;
        if (window == null)
            return globalTime;
        return window.state.editSequence.ToLocalTime(globalTime);
    }
}
