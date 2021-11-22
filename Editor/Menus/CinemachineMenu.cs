#if !UNITY_2019_3_OR_NEWER
#define CINEMACHINE_PHYSICS
#define CINEMACHINE_UNITY_ANIMATION
#endif

using UnityEngine;
using UnityEditor;
using System;

namespace Cinemachine.Editor
{
    internal static class CinemachineMenu
    {
        // Assets Menu
        const string m_CinemachineAssetsRootMenu = "Assets/Create/Cinemachine/";

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
        const string m_CinemachineGameObjectRootMenu = "GameObject/Cinemachine/";
        const int m_MenuPriority = 11; // right after Camera
        
        static void SetParentToMenuContextObject(GameObject child, MenuCommand command)
        {
            var go = command.context as GameObject;
            if (go != null)
                Undo.SetTransformParent(child.transform, go.transform, "set parent");
            Selection.activeObject = child;
        }

        [MenuItem(m_CinemachineGameObjectRootMenu + "Virtual Camera", false, m_MenuPriority)]
        static void CreateVirtualCamera(MenuCommand command)
        {
            CinemachineEditorAnalytics.SendCreateEvent("Virtual Camera");
            var go = InternalCreateVirtualCamera(
                "CM vcam", false, typeof(CinemachineComposer), typeof(CinemachineTransposer)).gameObject;
            SetParentToMenuContextObject(go, command);
        }

        [MenuItem(m_CinemachineGameObjectRootMenu + "FreeLook Camera", false, m_MenuPriority)]
        static void CreateFreeLookCamera(MenuCommand command)
        {
            CinemachineEditorAnalytics.SendCreateEvent("FreeLook Camera");
            CreateCameraBrainIfAbsent();
            GameObject go = InspectorUtility.CreateGameObject(
                    GenerateUniqueObjectName(typeof(CinemachineFreeLook), "CM FreeLook"),
                    typeof(CinemachineFreeLook));
            SetParentToMenuContextObject(go, command);
            if (SceneView.lastActiveSceneView != null)
                go.transform.position = SceneView.lastActiveSceneView.pivot;
        }

        [MenuItem(m_CinemachineGameObjectRootMenu + "Blend List Camera", false, m_MenuPriority)]
        static void CreateBlendListCamera(MenuCommand command)
        {
            CinemachineEditorAnalytics.SendCreateEvent("Blend List Camera");
            CreateCameraBrainIfAbsent();
            GameObject go = InspectorUtility.CreateGameObject(
                    GenerateUniqueObjectName(typeof(CinemachineBlendListCamera), "CM BlendListCamera"),
                    typeof(CinemachineBlendListCamera));
            SetParentToMenuContextObject(go, command);
            if (SceneView.lastActiveSceneView != null)
                go.transform.position = SceneView.lastActiveSceneView.pivot;
            Undo.RegisterCreatedObjectUndo(go, "create Blend List camera");
            var vcam = go.GetComponent<CinemachineBlendListCamera>();

            // Give it a couple of children
            var child1 = CreateDefaultVirtualCamera();
            Undo.SetTransformParent(child1.transform, go.transform, "create BlendListCam child");
            var child2 = CreateDefaultVirtualCamera();
            child2.m_Lens.FieldOfView = 10;
            Undo.SetTransformParent(child2.transform, go.transform, "create BlendListCam child");

            // Set up initial instruction set
            vcam.m_Instructions = new CinemachineBlendListCamera.Instruction[2];
            vcam.m_Instructions[0].m_VirtualCamera = child1;
            vcam.m_Instructions[0].m_Hold = 1f;
            vcam.m_Instructions[1].m_VirtualCamera = child2;
            vcam.m_Instructions[1].m_Blend.m_Style = CinemachineBlendDefinition.Style.EaseInOut;
            vcam.m_Instructions[1].m_Blend.m_Time = 2f;
        }
        
#if CINEMACHINE_UNITY_ANIMATION
        [MenuItem(m_CinemachineGameObjectRootMenu + "State-Driven Camera", false, m_MenuPriority)]
        static void CreateStateDivenCamera(MenuCommand command)
        {
            CinemachineEditorAnalytics.SendCreateEvent("State-Driven Camera");
            CreateCameraBrainIfAbsent();
            GameObject go = InspectorUtility.CreateGameObject(
                    GenerateUniqueObjectName(typeof(CinemachineStateDrivenCamera), "CM StateDrivenCamera"),
                    typeof(CinemachineStateDrivenCamera));
            SetParentToMenuContextObject(go, command);
            if (SceneView.lastActiveSceneView != null)
                go.transform.position = SceneView.lastActiveSceneView.pivot;
            Undo.RegisterCreatedObjectUndo(go, "create state driven camera");

            // Give it a child
            Undo.SetTransformParent(CreateDefaultVirtualCamera().transform, go.transform, "create state driven camera");
        }
#endif

#if CINEMACHINE_PHYSICS
        [MenuItem(m_CinemachineGameObjectRootMenu + "ClearShot Camera", false, m_MenuPriority)]
        static void CreateClearShotVirtualCamera(MenuCommand command)
        {
            CinemachineEditorAnalytics.SendCreateEvent("ClearShot Camera");
            CreateCameraBrainIfAbsent();
            GameObject go = InspectorUtility.CreateGameObject(
                    GenerateUniqueObjectName(typeof(CinemachineClearShot), "CM ClearShot"),
                    typeof(CinemachineClearShot));
            SetParentToMenuContextObject(go, command);
            if (SceneView.lastActiveSceneView != null)
                go.transform.position = SceneView.lastActiveSceneView.pivot;
            Undo.RegisterCreatedObjectUndo(go, "create ClearShot camera");

            // Give it a child
            var child = CreateDefaultVirtualCamera();
            Undo.SetTransformParent(child.transform, go.transform, "create ClearShot camera");
            var collider = Undo.AddComponent<CinemachineCollider>(child.gameObject);
            collider.m_AvoidObstacles = false;
            Undo.RecordObject(collider, "create ClearShot camera");
        }
#endif

        [MenuItem(m_CinemachineGameObjectRootMenu + "Dolly Camera with Track", false, m_MenuPriority)]
        static void CreateDollyCameraWithPath(MenuCommand command)
        {
            CinemachineEditorAnalytics.SendCreateEvent("Dolly Camera with Track");
            GameObject go = InspectorUtility.CreateGameObject(
                    GenerateUniqueObjectName(typeof(CinemachineSmoothPath), "DollyTrack"),
                    typeof(CinemachineSmoothPath));
            SetParentToMenuContextObject(go, command);
            if (SceneView.lastActiveSceneView != null)
                go.transform.position = SceneView.lastActiveSceneView.pivot;
            Undo.RegisterCreatedObjectUndo(go, "create track");
            CinemachineSmoothPath path = go.GetComponent<CinemachineSmoothPath>();

            CinemachineVirtualCamera vcam = InternalCreateVirtualCamera(
                    "CM vcam", true, typeof(CinemachineComposer), typeof(CinemachineTrackedDolly));
            SetParentToMenuContextObject(vcam.gameObject, command);
            vcam.GetCinemachineComponent<CinemachineTrackedDolly>().m_Path = path;
        }

        [MenuItem(m_CinemachineGameObjectRootMenu + "Dolly Track with Cart", false, m_MenuPriority)]
        static void CreateDollyTrackWithCart(MenuCommand command)
        {
            CinemachineEditorAnalytics.SendCreateEvent("Dolly Track with Cart");
            GameObject go = InspectorUtility.CreateGameObject(
                    GenerateUniqueObjectName(typeof(CinemachineSmoothPath), "DollyTrack"),
                    typeof(CinemachineSmoothPath));
            SetParentToMenuContextObject(go, command);
            if (SceneView.lastActiveSceneView != null)
                go.transform.position = SceneView.lastActiveSceneView.pivot;
            Undo.RegisterCreatedObjectUndo(go, "create track");
            CinemachineSmoothPath path = go.GetComponent<CinemachineSmoothPath>();

            go = InspectorUtility.CreateGameObject(
                GenerateUniqueObjectName(typeof(CinemachineDollyCart), "DollyCart"),
                typeof(CinemachineDollyCart));
            SetParentToMenuContextObject(go, command);
            if (SceneView.lastActiveSceneView != null)
                go.transform.position = SceneView.lastActiveSceneView.pivot;
            Undo.RegisterCreatedObjectUndo(go, "create cart");
            go.GetComponent<CinemachineDollyCart>().m_Path = path;
        }

        [MenuItem(m_CinemachineGameObjectRootMenu + "Target Group Camera", false, m_MenuPriority)]
        static void CreateTargetGroupCamera(MenuCommand command)
        {
            CinemachineEditorAnalytics.SendCreateEvent("Target Group Camera");
            CinemachineVirtualCamera vcam = InternalCreateVirtualCamera(
                    "CM vcam", true, typeof(CinemachineGroupComposer), typeof(CinemachineTransposer));
            GameObject go = InspectorUtility.CreateGameObject(
                    GenerateUniqueObjectName(typeof(CinemachineTargetGroup), "TargetGroup"),
                    typeof(CinemachineTargetGroup));
            SetParentToMenuContextObject(go, command);
            if (SceneView.lastActiveSceneView != null)
                go.transform.position = SceneView.lastActiveSceneView.pivot;
            Undo.RegisterCreatedObjectUndo(go, "create target group");
            vcam.LookAt = go.transform;
            vcam.Follow = go.transform;
        }

        [MenuItem(m_CinemachineGameObjectRootMenu + "Mixing Camera", false, m_MenuPriority)]
        static void CreateMixingCamera(MenuCommand command)
        {
            CinemachineEditorAnalytics.SendCreateEvent("Mixing Camera");
            CreateCameraBrainIfAbsent();
            GameObject go = InspectorUtility.CreateGameObject(
                    GenerateUniqueObjectName(typeof(CinemachineMixingCamera), "CM MixingCamera"),
                    typeof(CinemachineMixingCamera));
            SetParentToMenuContextObject(go, command);
            if (SceneView.lastActiveSceneView != null)
                go.transform.position = SceneView.lastActiveSceneView.pivot;
            Undo.RegisterCreatedObjectUndo(go, "create MixingCamera camera");

            // Give it a couple of children
            Undo.SetTransformParent(CreateDefaultVirtualCamera().transform, go.transform, "create MixedCamera child");
            Undo.SetTransformParent(CreateDefaultVirtualCamera().transform, go.transform, "create MixingCamera child");
        }

        [MenuItem(m_CinemachineGameObjectRootMenu + "2D Camera", false, m_MenuPriority)]
        static void Create2DCamera(MenuCommand command)
        {
            CinemachineEditorAnalytics.SendCreateEvent("2D Camera");
            var go = InternalCreateVirtualCamera(
                "CM vcam", true, typeof(CinemachineFramingTransposer)).gameObject;
            SetParentToMenuContextObject(go, command);
        }

        /// <summary>
        /// Create a default Virtual Camera, with standard components
        /// </summary>
        public static CinemachineVirtualCamera CreateDefaultVirtualCamera()
        {
            return InternalCreateVirtualCamera(
                "CM vcam", false, typeof(CinemachineComposer), typeof(CinemachineTransposer));
        }

        /// <summary>
        /// Create a static Virtual Camera, with no procedural components
        /// </summary>
        public static CinemachineVirtualCamera CreateStaticVirtualCamera()
        {
            return InternalCreateVirtualCamera("CM vcam", false);
        }

        /// <summary>
        /// Create a Virtual Camera, with components
        /// </summary>
        static CinemachineVirtualCamera InternalCreateVirtualCamera(
            string name, bool selectIt, params Type[] components)
        {
            // Create a new virtual camera
            var brain = CreateCameraBrainIfAbsent();
            GameObject go = InspectorUtility.CreateGameObject(
                    GenerateUniqueObjectName(typeof(CinemachineVirtualCamera), name),
                    typeof(CinemachineVirtualCamera));
            CinemachineVirtualCamera vcam = go.GetComponent<CinemachineVirtualCamera>();
            SetVcamFromSceneView(vcam);
            Undo.RegisterCreatedObjectUndo(go, "create " + name);
            GameObject componentOwner = vcam.GetComponentOwner().gameObject;
            foreach (Type t in components)
                Undo.AddComponent(componentOwner, t);
            vcam.InvalidateComponentPipeline();
            if (brain != null && brain.OutputCamera != null)
                vcam.m_Lens = LensSettings.FromCamera(brain.OutputCamera);
            if (selectIt)
                Selection.activeObject = go;
            return vcam;
        }

        public static void SetVcamFromSceneView(CinemachineVirtualCamera vcam)
        {
            if (SceneView.lastActiveSceneView != null)
            {
                vcam.transform.position = SceneView.lastActiveSceneView.camera.transform.position;
                vcam.transform.rotation = SceneView.lastActiveSceneView.camera.transform.rotation;
                var lens = LensSettings.FromCamera(SceneView.lastActiveSceneView.camera);
                // Don't grab these
                lens.NearClipPlane = LensSettings.Default.NearClipPlane;
                lens.FarClipPlane = LensSettings.Default.FarClipPlane;
                vcam.m_Lens = lens;
            }
        }

        /// <summary>
        /// If there is no CinemachineBrain in the scene, try to create one on the main camera
        /// </summary>
        public static CinemachineBrain CreateCameraBrainIfAbsent()
        {
            CinemachineBrain[] brains = UnityEngine.Object.FindObjectsOfType(
                    typeof(CinemachineBrain)) as CinemachineBrain[];
            CinemachineBrain brain = (brains != null && brains.Length > 0) ? brains[0] : null;
            if (brain == null)
            {
                Camera cam = Camera.main;
                if (cam == null)
                {
                    Camera[] cams = UnityEngine.Object.FindObjectsOfType(
                            typeof(Camera)) as Camera[];
                    if (cams != null && cams.Length > 0)
                        cam = cams[0];
                }
                if (cam != null)
                {
                    brain = Undo.AddComponent<CinemachineBrain>(cam.gameObject);
                }
            }
            return brain;
        }

        /// <summary>
        /// Generate a unique name with the given prefix by adding a suffix to it
        /// </summary>
        public static string GenerateUniqueObjectName(Type type, string prefix)
        {
            int count = 0;
            UnityEngine.Object[] all = Resources.FindObjectsOfTypeAll(type);
            foreach (UnityEngine.Object o in all)
            {
                if (o != null && o.name.StartsWith(prefix))
                {
                    string suffix = o.name.Substring(prefix.Length);
                    int i;
                    if (Int32.TryParse(suffix, out i) && i > count)
                        count = i;
                }
            }
            return prefix + (count + 1);
        }
    }
}
