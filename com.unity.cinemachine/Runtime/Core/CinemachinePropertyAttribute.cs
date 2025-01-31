using System;
using UnityEngine;

namespace Unity.Cinemachine
{
    /// <summary>
    /// Property applied to legacy input axis name specification.  Used for custom drawing in the inspector.
    /// </summary>
    public sealed class InputAxisNamePropertyAttribute : PropertyAttribute {}

    /// <summary>
    /// Suppresses the top-level foldout on a complex property
    /// </summary>
    public sealed class HideFoldoutAttribute : PropertyAttribute {}

    /// <summary>Hide this property if a component of a given type is not present</summary>
    public sealed class HideIfNoComponentAttribute : PropertyAttribute
    {
        /// <summary>The name of the field controlling the enabled state</summary>
        public Type ComponentType;

        /// <summary>Constructor</summary>
        /// <param name="type">Type of the component to check for</param>
        public HideIfNoComponentAttribute(Type type) => ComponentType = type;
    }

    /// <summary>
    /// Draw a foldout with an Enabled toggle that shadows a field inside the foldout
    /// </summary>
    public class FoldoutWithEnabledButtonAttribute : PropertyAttribute
    {
        /// <summary>The name of the field controlling the enabled state</summary>
        public string EnabledPropertyName;

        /// <summary>Constructor</summary>
        /// <param name="enabledProperty">The name of the field controlling the enabled state</param>
        public FoldoutWithEnabledButtonAttribute(string enabledProperty = "Enabled")
            => EnabledPropertyName = enabledProperty;
    }

    /// <summary>
    /// Draw a FoldoutWithEnabledButtonAttribute on a single line
    /// </summary>
    public sealed class EnabledPropertyAttribute : FoldoutWithEnabledButtonAttribute
    {
        /// <summary>Text to display to the right of the toggle button when disabled</summary>
        public string ToggleDisabledText;

        /// <summary>Constructor</summary>
        /// <param name="enabledProperty">The name of the field controlling the enabled state</param>
        /// <param name="toggleText">Text to display to the right of the toggle button</param>
        public EnabledPropertyAttribute(string enabledProperty = "Enabled", string toggleText = "")
            : base(enabledProperty) => ToggleDisabledText = toggleText;
    }

    /// <summary>
    /// Property applied to int or float fields to generate a slider in the inspector.
    /// </summary>
    [Obsolete("Use RangeAttribute instead")]
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
    public sealed class LensSettingsHideModeOverridePropertyAttribute : PropertyAttribute {}

    /// <summary>Property to display a SensorSize field</summary>
    public sealed class SensorSizePropertyAttribute : PropertyAttribute {}

    /// <summary>Property field is a Tag.</summary>
    public sealed class TagFieldAttribute : PropertyAttribute {}

    /// <summary>
    /// Used for custom drawing in the inspector.  Inspector will show a foldout with the asset contents
    /// </summary>
    // GML TODO: delete this attribute
    public sealed class CinemachineEmbeddedAssetPropertyAttribute : PropertyAttribute
    {
        /// <summary>If true, inspector will display a warning if the embedded asset is null</summary>
        public bool WarnIfNull;

        /// <summary>Standard constructor</summary>
        /// <param name="warnIfNull">If true, inspector will display a warning if the embedded asset is null</param>
        public CinemachineEmbeddedAssetPropertyAttribute(bool warnIfNull = false) { WarnIfNull = warnIfNull; }
    }

    /// <summary>
    /// Property applied to Vector2 to treat (x, y) as (min, max).
    /// Used for custom drawing in the inspector.
    /// </summary>
    public sealed class Vector2AsRangeAttribute : PropertyAttribute {}

    /// <summary>
    /// Sets isDelayed to true for each float field of the vector.
    /// </summary>
    public sealed class DelayedVectorAttribute : PropertyAttribute {}

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

    /// <summary>
    /// Attribute used by inspector to display warnings about missing targets.
    /// This can be used on CinemachineComponents and CinemachineExtensions.
    /// </summary>
    public sealed class RequiredTargetAttribute : System.Attribute
    {
        /// <summary>Choices for which targets are required</summary>
        public enum RequiredTargets
        {
            /// <summary>No specific target is required.</summary>
            None,
            /// <summary>Tracking Target is required for the pipeline element to work</summary>
            Tracking,
            /// <summary>LookAt Target is required for the pipeline element to work</summary>
            LookAt,
            /// <summary>LookAt Target is required and must be a ICinemachineTargetGroup for the pipeline element to work</summary>
            GroupLookAt
        };

        /// <summary>Get the stage in the Camera Pipeline in which to position this component</summary>
        public RequiredTargets RequiredTarget { get; private set; }

        /// <summary>Constructor: Pipeline Stage is defined here.</summary>
        /// <param name="requiredTarget">Which targets are required</param>
        public RequiredTargetAttribute(RequiredTargets requiredTarget) { RequiredTarget = requiredTarget; }
    }

    /// <summary>
    /// Attribute applied to a CinemachineCameraManagerBase property to produce
    /// a child camera selector in the inspector.
    /// </summary>
    public sealed class ChildCameraPropertyAttribute : PropertyAttribute {}

    /// <summary>
    /// Draws BlenderSettings asset embedded within the inspector.
    /// </summary>
    public sealed class EmbeddedBlenderSettingsPropertyAttribute : PropertyAttribute {}
}
