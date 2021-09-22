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
        //
        // /// <summary>
        // /// 
        // /// </summary>
        // /// <param name="sceneTool"></param>
        // /// <param name="delta"></param>
        // public void ProcessSceneToolEvent(CinemachineSceneTool sceneTool, UnityEngine.Vector3 delta);

        /// <summary>
        /// Draws scene tools in the editor window and processes them
        /// </summary>
        /// <param name="activeColor"></param>
        /// <param name="defaultColor"></param>
        /// <returns>True, if something was drawn.</returns>
        public bool DrawSceneTools(UnityEngine.Color activeColor, UnityEngine.Color defaultColor);
    }
}
