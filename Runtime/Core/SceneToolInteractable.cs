namespace Cinemachine.Utility
{
    public interface ISceneToolInteractable
    {
        // /// <summary>
        // /// 
        // /// </summary>
        // /// <param name="sceneTool"></param>
        // /// <returns></returns>
        // public bool CanBeControllerBySceneTool(CinemachineSceneTool sceneTool);
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
        public void DrawSceneTools();
    }
}
