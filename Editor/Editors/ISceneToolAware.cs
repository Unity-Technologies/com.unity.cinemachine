namespace Cinemachine.Editor
{
    interface ISceneToolAware
    {
#if UNITY_2021_2_OR_NEWER
        /// <summary>
        /// Draw inside the scene view here.
        /// Default implementation calls DrawSceneTools.
        /// </summary>
        void DrawSceneToolsOnSceneGUI();
        
        /// <summary>
        /// Implement your scene handles for your components and extensions here.
        ///
        /// Don't forget to register/unregister your components and extensions in their
        /// editor's OnEnable and OnDisable functions with CinemachineSceneToolUtility.RegisterTool/UnregisterTool. 
        /// </summary>
        void DrawSceneTools();
#endif
    }
}
