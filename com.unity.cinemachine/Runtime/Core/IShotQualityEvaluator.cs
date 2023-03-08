namespace Unity.Cinemachine
{
    /// <summary>
    /// An abstract representation of a component that evaluates shot quality.
    /// Component (usually a CinemachineExtension) is expected to set state.ShotQuality
    /// late in the camera pipeline (after Aim).
    /// </summary>
    public interface IShotQualityEvaluator {}
}
