namespace Cinemachine
{
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
    /// Attribute used to indicate that an authoring component is a Camera Pipeline extension
    /// </summary>
    public sealed class CameraExtensionAttribute : System.Attribute {}
}
