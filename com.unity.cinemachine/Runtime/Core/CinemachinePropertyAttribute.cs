using UnityEngine;

namespace Cinemachine
{
    /// <summary>
    /// Property applied to legacy input axis name specification.  Used for custom drawing in the inspector.
    /// </summary>
    public sealed class InputAxisNamePropertyAttribute : PropertyAttribute {}

    /// <summary>
    /// Suppresses the top-level foldout on a complex property
    /// </summary>
    public sealed class HideFoldoutAttribute : PropertyAttribute {}
    
    /// <summary>
    /// Draw a foldout with an Enabled toggle that shadows a field inside the foldout
    /// </summary>
    public sealed class FoldoutWithEnabledButtonAttribute : PropertyAttribute 
    { 
        /// <summary>The name of the field controlling the enabled state</summary>
        public string EnabledPropertyName; 

        /// <summary>Text to display to the right of the toggle button when disabled</summary>
        public string ToggleDisabledText;

        /// <summary>Constructor</summary>
        /// <param name="enabledProperty">The name of the field controlling the enabled state</param>
        /// <param name="toggleText">Text to display to the right of the toggle button</param>
        public FoldoutWithEnabledButtonAttribute(string enabledProperty = "Enabled", string toggleText = "") 
        { 
            EnabledPropertyName = enabledProperty; 
            ToggleDisabledText = toggleText;
        }
    }

    /// <summary>
    /// Property applied to int or float fields to generate a slider in the inspector.
    /// </summary>
    public sealed class RangeSliderAttribute : PropertyAttribute 
    { 
        /// <summary>Minimum value for the range slider</summary>
        public float Min;
        /// <summary>Maximum value for the range slider</summary>
        public float Max;
        /// <summary>Constructor for the range slider attribute</summary>
        /// <param name="min">Minimum value for the range slider</param>
        /// <param name="max">Maximum value for the range slider</param>
        public RangeSliderAttribute(float min, float max) { Min = min; Max = max; }
    }
    
    /// <summary>
    /// Property applied to int or float fields to generate a minmax range slider in the inspector.
    /// </summary>
    public sealed class MinMaxRangeSliderAttribute : PropertyAttribute 
    { 
        /// <summary>Minimum value for the range slider</summary>
        public float Min;
        /// <summary>Maximum value for the range slider</summary>
        public float Max;
        /// <summary>Constructor for the range slider attribute</summary>
        /// <param name="min">Minimum value for the range slider</param>
        /// <param name="max">Maximum value for the range slider</param>
        public MinMaxRangeSliderAttribute(float min, float max) { Min = min; Max = max; }
    }

    /// <summary>
    /// Property applied to LensSetting properties.  
    /// Will cause the property drawer to hide the ModeOverride setting.
    /// </summary>
    public sealed class LensSettingsHideModeOverridePropertyAttribute : PropertyAttribute { }

    /// <summary>
    /// Property applied to Vcam Target fields.  Used for custom drawing in the inspector.
    /// </summary>
    public sealed class VcamTargetPropertyAttribute : PropertyAttribute { }

    /// <summary>Property field is a Tag.</summary>
    public sealed class TagFieldAttribute : PropertyAttribute {}
    
    /// <summary>Property should be treated as enum flags.</summary>
    public sealed class EnumMaskPropertyAttribute : PropertyAttribute {}
    
    /// <summary>
    /// Used for custom drawing in the inspector.  Inspector will show a foldout with the asset contents
    /// </summary>
    public sealed class CinemachineEmbeddedAssetPropertyAttribute : PropertyAttribute 
    {
        /// <summary>If true, inspector will display a warning if the embedded asset is null</summary>
        public bool WarnIfNull;

        /// <summary>Standard constructor</summary>
        /// <param name="warnIfNull">If true, inspector will display a warning if the embedded asset is null</param>
        public CinemachineEmbeddedAssetPropertyAttribute(bool warnIfNull = false)
        {
            WarnIfNull = warnIfNull;
        }
    }
    
    /// <summary>
    /// Property applied to Vector2 to treat (x, y) as (min, max).
    /// Used for custom drawing in the inspector.
    /// </summary>
    public sealed class Vector2AsRangeAttribute : PropertyAttribute {}

    /// <summary>
    /// Attribute used by camera pipeline authoring components to indicate
    /// which stage of the pipeline they belong in.
    /// </summary>
    public sealed class CameraPipelineAttribute : System.Attribute
    {
        /// <summary>Get the stage in the Camera Pipeline in which to position this component</summary>
        public CinemachineCore.Stage Stage { get; private set; }

        /// <summary>Constructor: Pipeline Stage is defined here.</summary>
        /// <param name="stage">The stage in the Camera Pipeline in which to position this component</param>
        public CameraPipelineAttribute(CinemachineCore.Stage stage) { Stage = stage; }
    }
}
