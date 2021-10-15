namespace Cinemachine.Editor
{
    interface ISceneToolAware
    {
        /// <summary>
        /// Draw inside the scene view here.
        /// Default implementation calls DrawSceneTools.
        /// </summary>
        void DrawSceneToolsOnSceneGUI() => DrawSceneTools();
        // TODO: probably this can be removed
        
        /// <summary>
        /// Implement your scene handles for your components and extensions here.
        ///
        /// Don't forget to register/unregister your components and extensions in their
        /// editor's OnEnable and OnDisable functions with CinemachineSceneToolUtility.RegisterTool/UnregisterTool. 
        /// </summary>
        void DrawSceneTools();
    }
}
