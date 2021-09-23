namespace Cinemachine.Utility
{
    public interface ISceneToolInteractable
    {
        /// <summary>
        /// Can this be controlled by the specified sceneTool.
        /// </summary>
        /// <param name="sceneTool">SceneTool to check.</param>
        /// <returns>True, if this can be controlled by sceneTool. False, otherwise.</returns>
        public bool CanBeControllerBySceneTool(CinemachineSceneTool sceneTool);
    }
}
