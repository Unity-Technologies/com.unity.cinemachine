using UnityEngine;
using UnityEditor;
using UnityEngine.Splines;

namespace Unity.Cinemachine.Editor
{
    static class CinemachineMenu
    {
        const string m_CinemachineAssetsRootMenu = "Assets/Create/Cinemachine/";
        const string m_CinemachineGameObjectRootMenu = "GameObject/Cinemachine/";
        const int m_GameObjectMenuPriority = 11; // Right after Camera.

        // Assets Menu

        [MenuItem(m_CinemachineAssetsRootMenu + "BlenderSettings")]
        static void CreateBlenderSettingAsset()
        {
            ScriptableObjectUtility.Create<CinemachineBlenderSettings>();
        }

        [MenuItem(m_CinemachineAssetsRootMenu + "NoiseSettings")]
        static void CreateNoiseSettingAsset()
        {
            ScriptableObjectUtility.Create<NoiseSettings>();
        }

        [MenuItem(m_CinemachineAssetsRootMenu + "Fixed Signal Definition")]
        static void CreateFixedSignalDefinition()
        {
            ScriptableObjectUtility.Create<CinemachineFixedSignal>();
        }

        // GameObject Menu

        [MenuItem(m_CinemachineGameObjectRootMenu + "Cinemachine Camera", false, m_GameObjectMenuPriority)]
        static void CreateVirtualCamera(MenuCommand command)
        {
            CinemachineEditorAnalytics.SendCreateEvent("Virtual Camera");
            CreatePassiveCmCamera(parentObject: command.context as GameObject, select: true);
        }

        [MenuItem(m_CinemachineGameObjectRootMenu + "Targeted Cameras/Follow Camera", false, m_GameObjectMenuPriority)]
        static void CreateFollowCamera(MenuCommand command)
        {
            CinemachineEditorAnalytics.SendCreateEvent("Virtual Camera");
            var targetObject = command.context as GameObject;
            var parent = targetObject == null || targetObject.transform.parent == null
                ? null : targetObject.transform.parent.gameObject;
            var vcam = CreateCinemachineObject<CinemachineCamera>("CinemachineCamera", parent, true);
            if (targetObject != null)
                vcam.Follow = targetObject.transform;
            vcam.Lens = MatchSceneViewCamera(vcam.transform);

            Undo.AddComponent<CinemachineFollow>(vcam.gameObject);
            Undo.AddComponent<CinemachineRotationComposer>(vcam.gameObject);
        }

        [MenuItem(m_CinemachineGameObjectRootMenu + "Targeted Cameras/2D Camera", false, m_GameObjectMenuPriority)]
        static void Create2DCamera(MenuCommand command)
        {
            CinemachineEditorAnalytics.SendCreateEvent("2D Camera");
            var targetObject = command.context as GameObject;
            var parent = targetObject == null || targetObject.transform.parent == null
                ? null : targetObject.transform.parent.gameObject;
            var vcam = CreateCinemachineObject<CinemachineCamera>("CinemachineCamera", parent, true);
            if (targetObject != null)
                vcam.Follow = targetObject.transform;
            vcam.Lens = MatchSceneViewCamera(vcam.transform);

            Undo.AddComponent<CinemachinePositionComposer>(vcam.gameObject);
        }

        [MenuItem(m_CinemachineGameObjectRootMenu + "Targeted Cameras/FreeLook Camera", false, m_GameObjectMenuPriority)]
        static void CreateFreeLookCamera(MenuCommand command)
        {
            CinemachineEditorAnalytics.SendCreateEvent("FreeLook Camera");
            var targetObject = command.context as GameObject;
            var parent = targetObject == null || targetObject.transform.parent == null
                ? null : targetObject.transform.parent.gameObject;
            var vcam = CreatePassiveCmCamera("FreeLook Camera", parent, true);
            if (targetObject != null)
                vcam.Follow = targetObject.transform;
            Undo.AddComponent<CinemachineOrbitalFollow>(vcam.gameObject).OrbitStyle = CinemachineOrbitalFollow.OrbitStyles.ThreeRing;
            Undo.AddComponent<CinemachineRotationComposer>(vcam.gameObject);
            Undo.AddComponent<CinemachineInputAxisController>(vcam.gameObject);
            Undo.AddComponent<CinemachineFreeLookModifier>(vcam.gameObject).enabled = false;
        }

#if CINEMACHINE_PHYSICS
        [MenuItem(m_CinemachineGameObjectRootMenu + "Targeted Cameras/Third Person Aim Camera", false, m_GameObjectMenuPriority)]
        static void CreateThirdPersonAimCamera(MenuCommand command)
        {
            CinemachineEditorAnalytics.SendCreateEvent("Third Person Aim Camera");
            var targetObject = command.context as GameObject;
            var parent = targetObject == null || targetObject.transform.parent == null
                ? null : targetObject.transform.parent.gameObject;
            var vcam = CreatePassiveCmCamera("Third Person Aim Camera", parent, true);
            if (targetObject != null)
                vcam.Follow = targetObject.transform;
            var thirdPersonFollow = Undo.AddComponent<CinemachineThirdPersonFollow>(vcam.gameObject);
            thirdPersonFollow.ShoulderOffset = new Vector3(0.8f, -0.4f, 0f);
            thirdPersonFollow.CameraSide = 1;
            thirdPersonFollow.VerticalArmLength = 1.2f;
            thirdPersonFollow.CameraDistance = 4f;
            Undo.AddComponent<CinemachineThirdPersonAim>(vcam.gameObject);
        }
#endif

        [MenuItem(m_CinemachineGameObjectRootMenu + "Targeted Cameras/Target Group Camera", false, m_GameObjectMenuPriority)]
        static void CreateTargetGroupCamera(MenuCommand command)
        {
            CinemachineEditorAnalytics.SendCreateEvent("Target Group Camera");
            var targetObject = command.context as GameObject;
            var parent = targetObject == null || targetObject.transform.parent == null
                ? null : targetObject.transform.parent.gameObject;
            var vcam = CreateCinemachineObject<CinemachineCamera>("CinemachineCamera", parent, false);
            vcam.Lens = MatchSceneViewCamera(vcam.transform);

            Undo.AddComponent<CinemachineRotationComposer>(vcam.gameObject);
            Undo.AddComponent<CinemachineFollow>(vcam.gameObject);
            Undo.AddComponent<CinemachineGroupFraming>(vcam.gameObject).enabled = false;

            var targetGroup = CreateCinemachineObject<CinemachineTargetGroup>(
                "Target Group", parent, true);
            vcam.Follow = targetGroup.transform;
            targetGroup.AddMember(targetObject == null ? null : targetObject.transform, 1, 0.5f);
        }

        [MenuItem(m_CinemachineGameObjectRootMenu + "Sequencer Camera", false, m_GameObjectMenuPriority)]
        static void CreateBlendListCamera(MenuCommand command)
        {
            CinemachineEditorAnalytics.SendCreateEvent("Blend List Camera");
            var sequencer = CreateCinemachineObject<CinemachineSequencerCamera>(
                "Sequencer Camera", command.context as GameObject, true);

            // We give the camera a couple of children as an example of setup
            var childVcam1 = CreatePassiveCmCamera(parentObject: sequencer.gameObject);
            var childVcam2 = CreatePassiveCmCamera(parentObject: sequencer.gameObject);
            childVcam2.Lens.FieldOfView = 10;

            // Set up initial instruction set
            sequencer.Instructions = new ()
            {
                new () { Camera = childVcam1, Hold = 1 },
                new () { Camera = childVcam2, Blend = new () { Style = CinemachineBlendDefinition.Styles.EaseInOut, Time = 2f }}
            };
        }

#if CINEMACHINE_UNITY_ANIMATION
        [MenuItem(m_CinemachineGameObjectRootMenu + "State-Driven Camera", false, m_GameObjectMenuPriority)]
        static void CreateStateDivenCamera(MenuCommand command)
        {
            CinemachineEditorAnalytics.SendCreateEvent("State-Driven Camera");
            var stateDrivenCamera = CreateCinemachineObject<CinemachineStateDrivenCamera>(
                "State-Driven Camera", command.context as GameObject, true);

            // We give the camera a child as an example setup
            CreatePassiveCmCamera(parentObject: stateDrivenCamera.gameObject);
        }
#endif

#if CINEMACHINE_PHYSICS
        [MenuItem(m_CinemachineGameObjectRootMenu + "ClearShot Camera", false, m_GameObjectMenuPriority)]
        static void CreateClearShotCamera(MenuCommand command)
        {
            CinemachineEditorAnalytics.SendCreateEvent("ClearShot Camera");
            var clearShotCamera = CreateCinemachineObject<CinemachineClearShot>(
                "ClearShot Camera", command.context as GameObject, true);

            // We give the camera a child as an example setup
            var childVcam = CreatePassiveCmCamera(parentObject: clearShotCamera.gameObject);
            Undo.AddComponent<CinemachineDeoccluder>(childVcam.gameObject).AvoidObstacles.Enabled = false;
        }
#endif

        [MenuItem(m_CinemachineGameObjectRootMenu + "Mixing Camera", false, m_GameObjectMenuPriority)]
        static void CreateMixingCamera(MenuCommand command)
        {
            CinemachineEditorAnalytics.SendCreateEvent("Mixing Camera");
            var mixingCamera = CreateCinemachineObject<CinemachineMixingCamera>(
                "Mixing Camera", command.context as GameObject, true);

            // We give the camera a couple of children as an example of setup
            CreatePassiveCmCamera(parentObject: mixingCamera.gameObject);
            CreatePassiveCmCamera(parentObject: mixingCamera.gameObject);
        }

        [MenuItem(m_CinemachineGameObjectRootMenu + "Dolly Camera with Spline", false, m_GameObjectMenuPriority)]
        static void CreateDollyCameraWithPath(MenuCommand command)
        {
            CinemachineEditorAnalytics.SendCreateEvent("Dolly Camera with Track");
            var vcam = CreateCinemachineObject<CinemachineCamera>(
                "CinemachineCamera", command.context as GameObject, true);
            vcam.Lens = MatchSceneViewCamera(vcam.transform);
            Undo.AddComponent<CinemachineRotationComposer>(vcam.gameObject);
            var splineContainer = ObjectFactory.CreateGameObject(
                "Dolly Spline", typeof(SplineContainer), typeof(CinemachineSplineSmoother)).GetComponent<SplineContainer>();
            splineContainer.Spline.SetTangentMode(TangentMode.AutoSmooth);
            splineContainer.Spline.Add(new BezierKnot(Vector3.zero));
            splineContainer.Spline.Add(new BezierKnot(Vector3.right));
            var splineDolly = Undo.AddComponent<CinemachineSplineDolly>(vcam.gameObject);
            splineDolly.Spline = splineContainer;
        }

        [MenuItem(m_CinemachineGameObjectRootMenu + "Dolly Cart with Spline", false, m_GameObjectMenuPriority)]
        static void CreateDollyTrackWithCart(MenuCommand command)
        {
            CinemachineEditorAnalytics.SendCreateEvent("Dolly Track with Cart");
            var splineContainer = ObjectFactory.CreateGameObject(
                "Dolly Spline", typeof(SplineContainer), typeof(CinemachineSplineSmoother)).GetComponent<SplineContainer>();
            splineContainer.Spline.SetTangentMode(TangentMode.AutoSmooth);
            splineContainer.Spline.Add(new BezierKnot(Vector3.zero));
            splineContainer.Spline.Add(new BezierKnot(Vector3.right));
            CreateCinemachineObject<CinemachineSplineCart>(
                "Dolly Cart", command.context as GameObject, true).Spline = splineContainer;
        }

        /// <summary>
        /// Sets the specified <see cref="Transform"/> to match the position and
        /// rotation of the <see cref="SceneView"/> camera, and returns the scene view
        /// camera's lens settings.
        /// </summary>
        /// <param name="sceneObject">The <see cref="Transform"/> to match with the
        /// <see cref="SceneView"/> camera.</param>
        /// <returns>A <see cref="LensSettings"/> representing the scene view camera's lens</returns>
        public static LensSettings MatchSceneViewCamera(Transform sceneObject)
        {
            var lens = LensSettings.Default;

            // Take initial settings from the GameView camera, because we don't want to override
            // things like ortho vs perspective - we just want position and FOV
            var brain = GetOrCreateBrain();
            if (brain != null && brain.OutputCamera != null)
                lens = LensSettings.FromCamera(brain.OutputCamera);

            if (SceneView.lastActiveSceneView != null)
            {
                var src = SceneView.lastActiveSceneView.camera;
                // Respect scene view preferences - Create Objects at Origin:
                // only set position and rotation, if sceneObject is not in the exact center
                if (!AtOrigin(sceneObject.transform.position))
                    sceneObject.SetPositionAndRotation(src.transform.position, src.transform.rotation);
                if (lens.Orthographic == src.orthographic)
                {
                    if (src.orthographic)
                        lens.OrthographicSize = src.orthographicSize;
                    else
                        lens.FieldOfView = src.fieldOfView;
                }
            }
            return lens;

            static bool AtOrigin(Vector3 a) => a.x == 0f && a.y == 0f && a.z == 0f;
        }

        /// <summary>
        /// Creates a <see cref="CinemachineCamera"/> with no procedural components.
        /// </summary>
        public static CinemachineCamera CreatePassiveCmCamera(
            string name = "CinemachineCamera", GameObject parentObject = null, bool select = false)
        {
            var vcam = CreateCinemachineObject<CinemachineCamera>(name, parentObject, select);
            vcam.Lens = MatchSceneViewCamera(vcam.transform);
            return vcam;
        }

        /// <summary>
        /// Creates a Cinemachine <see cref="GameObject"/> in the scene with a specified component.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="Component"/> to add to the new <see cref="GameObject"/>.</typeparam>
        /// <param name="name">The name of the new <see cref="GameObject"/>.</param>
        /// <param name="parentObject">The <see cref="GameObject"/> to parent the new <see cref="GameObject"/> to.</param>
        /// <param name="select">Whether the new <see cref="GameObject"/> should be selected.</param>
        /// <returns>The instance of the component that is added to the new <see cref="GameObject"/>.</returns>
        static T CreateCinemachineObject<T>(string name, GameObject parentObject, bool select) where T : Component
        {
            var selected = Selection.activeObject;

            // We always enforce the existence of the CM brain
            GetOrCreateBrain();

            // We use ObjectFactory to create a new GameObject as it automatically supports undo/redo
            var go = ObjectFactory.CreateGameObject(name);
            var component = Undo.AddComponent<T>(go);

            // We ensure that the new object has a unique name, for example "Camera (1)".
            // This must be done after setting the parent in order to get an accurate unique name
            GameObjectUtility.EnsureUniqueNameForSibling(go);

            // Place vcam as set by the unity editor preferences
            ObjectFactory.PlaceGameObject(go, parentObject);

            Selection.activeObject = select ? go : selected;

            return component;
        }

        /// <summary>
        /// Gets the first loaded <see cref="CinemachineBrain"/>. Creates one on
        /// the <see cref="Camera.main"/> if none were found.
        /// </summary>
        public static CinemachineBrain GetOrCreateBrain()
        {
            if (CinemachineBrain.ActiveBrainCount > 0)
                return CinemachineBrain.GetActiveBrain(0);

            // Create a CinemachineBrain on the main camera
            var cam = Camera.main;
            if (cam == null)
#if UNITY_2023_1_OR_NEWER
                cam = Object.FindFirstObjectByType<Camera>(FindObjectsInactive.Exclude);
#else
                cam = Object.FindObjectOfType<Camera>();
#endif
            if (cam != null)
                return Undo.AddComponent<CinemachineBrain>(cam.gameObject);

            // No camera, just create a brain on an empty object
            var obj = ObjectFactory.CreateGameObject("CinemachineBrain", typeof(CinemachineBrain));
            Undo.RegisterCreatedObjectUndo(obj, "CinemachineBrain");
            return obj.GetComponent<CinemachineBrain>();
        }
    }
}
