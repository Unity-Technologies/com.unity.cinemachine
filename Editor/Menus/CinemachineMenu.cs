using UnityEngine;
using UnityEditor;

namespace Cinemachine.Editor
{
    internal static class CinemachineMenu
    {
        private const string m_CinemachineAssetsRootMenu = "Assets/Create/Cinemachine/";
        private const string m_CinemachineGameObjectRootMenu = "GameObject/Cinemachine/";
        private const int m_GameObjectMenuPriority = 11; // Right after Camera.


        // Assets Menu.

        [MenuItem(m_CinemachineAssetsRootMenu + "BlenderSettings")]
        static void CreateBlenderSettingAsset()
        {
            ScriptableObjectUtility.Create<CinemachineBlenderSettings>();
        }

        [MenuItem(m_CinemachineAssetsRootMenu + "NoiseSettings")]
        private static void CreateNoiseSettingAsset()
        {
            ScriptableObjectUtility.Create<NoiseSettings>();
        }

        [MenuItem(m_CinemachineAssetsRootMenu + "Fixed Signal Definition")]
        private static void CreateFixedSignalDefinition()
        {
            ScriptableObjectUtility.Create<CinemachineFixedSignal>();
        }

        // GameObject Menu.

        [MenuItem(m_CinemachineGameObjectRootMenu + "Virtual Camera", false, m_GameObjectMenuPriority)]
        private static void CreateVirtualCamera(MenuCommand command)
        {
            CreateDefaultVirtualCamera(parentObject: command.context as GameObject, select: true);
        }

        [MenuItem(m_CinemachineGameObjectRootMenu + "Free Look Camera", false, m_GameObjectMenuPriority)]
        private static void CreateFreeLookCamera(MenuCommand command)
        {
            CreateCinemachineGameObject<CinemachineFreeLook>("Free Look Camera", command.context as GameObject);
        }

        [MenuItem(m_CinemachineGameObjectRootMenu + "Blend List Camera", false, m_GameObjectMenuPriority)]
        private static void CreateBlendListCamera(MenuCommand command)
        {
            var blendListCamera  = CreateCinemachineGameObject<CinemachineBlendListCamera>("Blend List Camera", command.context as GameObject);

            // We give the camera a couple of children as an example of setup.
            CinemachineVirtualCamera childVirtualCamera1 = CreateDefaultVirtualCamera(parentObject: blendListCamera.gameObject);
            CinemachineVirtualCamera childVirtualCamera2 = CreateDefaultVirtualCamera(parentObject: blendListCamera.gameObject);
            childVirtualCamera2.m_Lens.FieldOfView = 10;

            // Set up initial instruction set.
            blendListCamera.m_Instructions = new CinemachineBlendListCamera.Instruction[2];
            blendListCamera.m_Instructions[0].m_VirtualCamera = childVirtualCamera1;
            blendListCamera.m_Instructions[0].m_Hold = 1f;
            blendListCamera.m_Instructions[1].m_VirtualCamera = childVirtualCamera2;
            blendListCamera.m_Instructions[1].m_Blend.m_Style = CinemachineBlendDefinition.Style.EaseInOut;
            blendListCamera.m_Instructions[1].m_Blend.m_Time = 2f;
        }
        
        [MenuItem(m_CinemachineGameObjectRootMenu + "State-Driven Camera", false, m_GameObjectMenuPriority)]
        private static void CreateStateDivenCamera(MenuCommand command)
        {
            var stateDrivenCamera = CreateCinemachineGameObject<CinemachineStateDrivenCamera>("State-Driven Camera", command.context as GameObject);

            // We give the camera a child as an example setup.
            CreateDefaultVirtualCamera(parentObject: stateDrivenCamera.gameObject);
        }

        [MenuItem(m_CinemachineGameObjectRootMenu + "Clear Shot Camera", false, m_GameObjectMenuPriority)]
        static void CreateClearShotVirtualCamera(MenuCommand command)
        {
            var clearShotCamera = CreateCinemachineGameObject<CinemachineClearShot>("Clear Shot Camera", command.context as GameObject);

            // We give the camera a child as an example setup.
            CinemachineVirtualCamera childVirtualCamera = CreateDefaultVirtualCamera(parentObject: clearShotCamera.gameObject);
            var collider = Undo.AddComponent<CinemachineCollider>(childVirtualCamera.gameObject);
            collider.m_AvoidObstacles = false;
        }


        [MenuItem(m_CinemachineGameObjectRootMenu + "Dolly Camera with Track", false, m_GameObjectMenuPriority)]
        private static void CreateDollyCameraWithPath(MenuCommand command)
        {
            CinemachineSmoothPath path = CreateCinemachineGameObject<CinemachineSmoothPath>("Dolly Track", command.context as GameObject, false);
            CinemachineVirtualCamera virtualCamera = CreateCinemachineGameObject<CinemachineVirtualCamera>("Virtual Camera", command.context as GameObject, true);

            AddCinemachineComponent<CinemachineComposer>(virtualCamera);
            var trackedDolly = AddCinemachineComponent<CinemachineTrackedDolly>(virtualCamera);

            trackedDolly.m_Path = path;
        }

        [MenuItem(m_CinemachineGameObjectRootMenu + "Dolly Track with Cart", false, m_GameObjectMenuPriority)]
        private static void CreateDollyTrackWithCart(MenuCommand command)
        {
            CinemachineSmoothPath path = CreateCinemachineGameObject<CinemachineSmoothPath>("Dolly Track", command.context as GameObject, false);
            CinemachineDollyCart dollyCart = CreateCinemachineGameObject<CinemachineDollyCart>("Dolly Cart", command.context as GameObject, true);

            dollyCart.m_Path = path;
        }

        [MenuItem(m_CinemachineGameObjectRootMenu + "Target Group Camera", false, m_GameObjectMenuPriority)]
        private static void CreateTargetGroupCamera(MenuCommand command)
        {
            CinemachineVirtualCamera virtualCamera = CreateCinemachineGameObject<CinemachineVirtualCamera>("Virtual Camera", command.context as GameObject, false);

            AddCinemachineComponent<CinemachineGroupComposer>(virtualCamera);
            AddCinemachineComponent<CinemachineTransposer>(virtualCamera);

            CinemachineTargetGroup targetGroup = CreateCinemachineGameObject<CinemachineTargetGroup>("Target Group", command.context as GameObject, true);
            virtualCamera.LookAt = targetGroup.transform;
            virtualCamera.Follow = targetGroup.transform;
        }

        [MenuItem(m_CinemachineGameObjectRootMenu + "Mixing Camera", false, m_GameObjectMenuPriority)]
        private static void CreateMixingCamera(MenuCommand command)
        {
            CinemachineMixingCamera mixingCamera = CreateCinemachineGameObject<CinemachineMixingCamera>("Mixing Camera", command.context as GameObject);

            // We give the camera a couple of children as an example of setup.
            CreateDefaultVirtualCamera(parentObject: mixingCamera.gameObject);
            CreateDefaultVirtualCamera(parentObject: mixingCamera.gameObject);
        }

        [MenuItem(m_CinemachineGameObjectRootMenu + "2D Camera", false, m_GameObjectMenuPriority)]
        private static void Create2DCamera(MenuCommand command)
        {
            CinemachineVirtualCamera virtualCamera = CreateCinemachineGameObject<CinemachineVirtualCamera>("2D Camera", command.context as GameObject);
            AddCinemachineComponent<CinemachineFramingTransposer>(virtualCamera);
        }

        /// <summary>
        /// Sets the specified <see cref="CinemachineVirtualCamera"/> to match the position, rotation, and lens settings of the <see cref="SceneView"/> camera.
        /// </summary>
        /// <param name="virtualCamera">The <see cref="CinemachineVirtualCamera"/> to match with the <see cref="SceneView"/> settings to.</param>
        public static void CopySceneViewLook(CinemachineVirtualCamera virtualCamera)
        {
            if (SceneView.lastActiveSceneView != null)
            {
                Camera sceneViewCamera = SceneView.lastActiveSceneView.camera;
                virtualCamera.transform.SetPositionAndRotation(sceneViewCamera.transform.position, sceneViewCamera.transform.rotation);
                var lens = LensSettings.FromCamera(sceneViewCamera);
                // Don't grab these.
                lens.NearClipPlane = LensSettings.Default.NearClipPlane;
                lens.FarClipPlane = LensSettings.Default.FarClipPlane;
                virtualCamera.m_Lens = lens;
            }
        }

        /// <summary>
        /// Creates a <see cref="CinemachineVirtualCamera"/>, with standard components.
        /// </summary>
        public static CinemachineVirtualCamera CreateDefaultVirtualCamera(string name = "Virtual Camera", GameObject parentObject = null, bool select = false)
        {
            CinemachineVirtualCamera virtualCamera = CreateCinemachineGameObject<CinemachineVirtualCamera>(name, parentObject, select);

            AddCinemachineComponent<CinemachineComposer>(virtualCamera);
            AddCinemachineComponent<CinemachineTransposer>(virtualCamera);

            return virtualCamera;
        }

        /// <summary>
        /// Creates a Cinemachine <see cref="GameObject"/> in the scene with a specified component.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="Component"/> to add to the new <see cref="GameObject"/>.</typeparam>
        /// <param name="name">The name of the new <see cref="GameObject"/>.</param>
        /// <param name="parentObject">The <see cref="GameObject"/> to parent the new <see cref="GameObject"/> to.</param>
        /// <param name="select">Whether the new <see cref="GameObject"/> should be selected.</param>
        /// <returns>The instance of the component that is added to the new <see cref="GameObject"/>.</returns>
        public static T CreateCinemachineGameObject<T>(string name, GameObject parentObject, bool select = true) where T : Component
        {
            CinemachineEditorAnalytics.SendCreateEvent(name);

            CinemachineBrain brain = GetCreateBrain();
            // We use ObjectFactory to create a new GameObject as it automatically supports undo/redo.
            GameObject go = ObjectFactory.CreateGameObject(name);
            T component = go.AddComponent<T>();

            if (parentObject != null)
                Undo.SetTransformParent(go.transform, parentObject.transform, "Set parent of " + name);

            // We ensure that the new object has a unique name, for example "Camera (1)".
            // This must be done after setting the parent in order to get an accurate unique name.
            GameObjectUtility.EnsureUniqueNameForSibling(go);

            // We set the new object to be at the current pivot of the scene.
            // TODO: Support the "Place Objects At World Origin" preference option in 2020.3+, see GOCreationCommands.cs
            if (SceneView.lastActiveSceneView != null)
                go.transform.position = SceneView.lastActiveSceneView.pivot;

            // If we created a Virtual Camera we setup it's look.
            if (component is CinemachineVirtualCamera virtualCamera)
            {
                CopySceneViewLook(virtualCamera);

                if (brain != null && brain.OutputCamera != null)
                    virtualCamera.m_Lens = LensSettings.FromCamera(brain.OutputCamera);
            }

            if (select)
                Selection.activeGameObject = go;

            return component;
        }

        /// <summary>
        /// Gets the first loaded <see cref="CinemachineBrain"/>. Creates one on the <see cref="Camera.main"/> if none were found.
        /// </summary>
        private static CinemachineBrain GetCreateBrain()
        {
            CinemachineBrain brain = Object.FindObjectOfType<CinemachineBrain>();

            if (brain == null)
            {
                Camera cam = Camera.main;
                if (cam == null)
                    cam = Object.FindObjectOfType<Camera>();

                if (cam != null)
                    brain = Undo.AddComponent<CinemachineBrain>(cam.gameObject);
            }

            return brain;
        }

        /// <summary>
        /// Adds an component to the specified <see cref="CinemachineVirtualCamera"/>'s hidden component owner, that supports undo.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="Component"/> to add to the cinemachine pipeline.</typeparam>
        /// <param name="virtualCamera">The <see cref="CinemachineVirtualCamera"/> to add components to.</param>
        /// <returns>The instance of the componented that was added.</returns>
        private static T AddCinemachineComponent<T>(CinemachineVirtualCamera virtualCamera) where T : CinemachineComponentBase
        {
            // We can't use the CinemachineVirtualCamera.AddCinemachineComponent<T>() because we want to support undo/redo.
            GameObject componentOwner = virtualCamera.GetComponentOwner().gameObject;
            var component = Undo.AddComponent<T>(componentOwner);
            virtualCamera.InvalidateComponentPipeline();

            return component;
        }
    }
}
