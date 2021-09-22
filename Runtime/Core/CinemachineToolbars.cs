namespace Cinemachine.Utility
{
    public enum CinemachineSceneTool
    {
        FoV,
        FarNearClip,
        FollowOffset,
        TrackedObjectOffset,
    };
    
    // TODO: KGB move this to where the enum
    public static class CinemachineSceneToolUtility
    {
        public static bool FoVToolIsOn { get; private set; }
        public static bool FarNearClipToolIsOn { get; private set; }
        public static bool FollowOffsetToolIsOn { get; private set; }
        public static bool TrackedObjectOffsetToolIsOn { get; private set; }

        static CinemachineSceneToolUtility()
        {
            FoVToolIsOn = FarNearClipToolIsOn = FollowOffsetToolIsOn = TrackedObjectOffsetToolIsOn = false;
        }
        
        public static void TrackedObjectOffsetToolSelection(UnityEngine.UIElements.ChangeEvent<bool> evt)
        {
            TrackedObjectOffsetToolIsOn = evt.newValue;
        }
        
        public static void FollowOffsetToolSelection(UnityEngine.UIElements.ChangeEvent<bool> evt)
        {
            FollowOffsetToolIsOn = evt.newValue;
        }
        
        public static void FOVToolSelectionToolSelection(UnityEngine.UIElements.ChangeEvent<bool> evt)
        {
            FoVToolIsOn = evt.newValue;
        }

        public static void FarNearClipSelectionToolSelection(UnityEngine.UIElements.ChangeEvent<bool> evt)
        {
            FarNearClipToolIsOn = evt.newValue;
        }
    }
}
