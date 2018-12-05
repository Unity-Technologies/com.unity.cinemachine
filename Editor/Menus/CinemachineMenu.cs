using UnityEngine;
using UnityEditor;
using System;
using System.IO;

namespace Cinemachine.Editor
{
    internal static class CinemachineMenu
    {
        public const string kCinemachineRootMenu = "Assets/Create/Cinemachine/";

        [MenuItem(kCinemachineRootMenu + "BlenderSettings")]
        private static void CreateBlenderSettingAsset()
        {
            ScriptableObjectUtility.Create<CinemachineBlenderSettings>();
        }

        [MenuItem(kCinemachineRootMenu + "NoiseSettings")]
        private static void CreateNoiseSettingAsset()
        {
            ScriptableObjectUtility.Create<NoiseSettings>();
        }

        [MenuItem(kCinemachineRootMenu + "Fixed Signal Definition")]
        private static void CreateFixedSignalDefinition()
        {
            ScriptableObjectUtility.Create<CinemachineFixedSignal>();
        }
    
        [MenuItem("Cinemachine/Create Virtual Camera", false, 1)]
        public static CinemachineVirtualCamera CreateVirtualCamera()
        {
            return InternalCreateVirtualCamera(
                "CM vcam", true, typeof(CinemachineComposer), typeof(CinemachineTransposer));
        }

        [MenuItem("Cinemachine/Create FreeLook Camera", false, 1)]
        private static void CreateFreeLookCamera()
        {
            CreateCameraBrainIfAbsent();
            GameObject go = InspectorUtility.CreateGameObject(
                    GenerateUniqueObjectName(typeof(CinemachineFreeLook), "CM FreeLook"), 
                    typeof(CinemachineFreeLook));
            if (SceneView.lastActiveSceneView != null)
                go.transform.position = SceneView.lastActiveSceneView.pivot;
            Selection.activeGameObject = go;
        }

        [MenuItem("Cinemachine/Create Blend List Camera", false, 1)]
        private static void CreateBlendListCamera()
        {
            CreateCameraBrainIfAbsent();
            GameObject go = InspectorUtility.CreateGameObject(
                    GenerateUniqueObjectName(typeof(CinemachineBlendListCamera), "CM BlendListCamera"),
                    typeof(CinemachineBlendListCamera));
            if (SceneView.lastActiveSceneView != null)
                go.transform.position = SceneView.lastActiveSceneView.pivot;
            Undo.RegisterCreatedObjectUndo(go, "create Blend List camera");
            var vcam = go.GetComponent<CinemachineBlendListCamera>();
            Selection.activeGameObject = go;

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

        [MenuItem("Cinemachine/Create State-Driven Camera", false, 1)]
        private static void CreateStateDivenCamera()
        {
            CreateCameraBrainIfAbsent();
            GameObject go = InspectorUtility.CreateGameObject(
                    GenerateUniqueObjectName(typeof(CinemachineStateDrivenCamera), "CM StateDrivenCamera"),
                    typeof(CinemachineStateDrivenCamera));
            if (SceneView.lastActiveSceneView != null)
                go.transform.position = SceneView.lastActiveSceneView.pivot;
            Undo.RegisterCreatedObjectUndo(go, "create state driven camera");
            Selection.activeGameObject = go;

            // Give it a child
            Undo.SetTransformParent(CreateDefaultVirtualCamera().transform, go.transform, "create state driven camera");
        }

        [MenuItem("Cinemachine/Create ClearShot Camera", false, 1)]
        private static void CreateClearShotVirtualCamera()
        {
            CreateCameraBrainIfAbsent();
            GameObject go = InspectorUtility.CreateGameObject(
                    GenerateUniqueObjectName(typeof(CinemachineClearShot), "CM ClearShot"),
                    typeof(CinemachineClearShot));
            if (SceneView.lastActiveSceneView != null)
                go.transform.position = SceneView.lastActiveSceneView.pivot;
            Undo.RegisterCreatedObjectUndo(go, "create ClearShot camera");
            Selection.activeGameObject = go;

            // Give it a child
            var child = CreateDefaultVirtualCamera();
            Undo.SetTransformParent(child.transform, go.transform, "create ClearShot camera");
            var collider = Undo.AddComponent<CinemachineCollider>(child.gameObject);
            collider.m_AvoidObstacles = false;
            Undo.RecordObject(collider, "create ClearShot camera");
        }

        [MenuItem("Cinemachine/Create Dolly Camera with Track", false, 1)]
        private static void CreateDollyCameraWithPath()
        {
            CinemachineVirtualCamera vcam = InternalCreateVirtualCamera(
                    "CM vcam", true, typeof(CinemachineComposer), typeof(CinemachineTrackedDolly));
            GameObject go = InspectorUtility.CreateGameObject(
                    GenerateUniqueObjectName(typeof(CinemachineSmoothPath), "DollyTrack"),
                    typeof(CinemachineSmoothPath));
            if (SceneView.lastActiveSceneView != null)
                go.transform.position = SceneView.lastActiveSceneView.pivot;
            Undo.RegisterCreatedObjectUndo(go, "create track");
            CinemachineSmoothPath path = go.GetComponent<CinemachineSmoothPath>();
            var dolly = vcam.GetCinemachineComponent<CinemachineTrackedDolly>();
            Undo.RecordObject(dolly, "create track");
            dolly.m_Path = path;
        }

        [MenuItem("Cinemachine/Create Dolly Track with Cart", false, 1)]
        private static void CreateDollyTrackWithCart()
        {
            GameObject go = InspectorUtility.CreateGameObject(
                    GenerateUniqueObjectName(typeof(CinemachineSmoothPath), "DollyTrack"),
                    typeof(CinemachineSmoothPath));
            if (SceneView.lastActiveSceneView != null)
                go.transform.position = SceneView.lastActiveSceneView.pivot;
            Undo.RegisterCreatedObjectUndo(go, "create track");
            CinemachineSmoothPath path = go.GetComponent<CinemachineSmoothPath>();
            Selection.activeGameObject = go;

            go = InspectorUtility.CreateGameObject(
                GenerateUniqueObjectName(typeof(CinemachineDollyCart), "DollyCart"),
                typeof(CinemachineDollyCart));
            if (SceneView.lastActiveSceneView != null)
                go.transform.position = SceneView.lastActiveSceneView.pivot;
            Undo.RegisterCreatedObjectUndo(go, "create cart");
            CinemachineDollyCart cart = go.GetComponent<CinemachineDollyCart>();
            Undo.RecordObject(cart, "create track");
            cart.m_Path = path;
        }

        [MenuItem("Cinemachine/Create Target Group Camera", false, 1)]
        private static void CreateTargetGroupCamera()
        {
            CinemachineVirtualCamera vcam = InternalCreateVirtualCamera(
                    "CM vcam", true, typeof(CinemachineGroupComposer), typeof(CinemachineTransposer));
            GameObject go = InspectorUtility.CreateGameObject(
                    GenerateUniqueObjectName(typeof(CinemachineTargetGroup), "TargetGroup"),
                    typeof(CinemachineTargetGroup));
            if (SceneView.lastActiveSceneView != null)
                go.transform.position = SceneView.lastActiveSceneView.pivot;
            Undo.RegisterCreatedObjectUndo(go, "create target group");
            vcam.LookAt = go.transform;
            vcam.Follow = go.transform;
        }

        [MenuItem("Cinemachine/Create Mixing Camera", false, 1)]
        private static void CreateMixingCamera()
        {
            CreateCameraBrainIfAbsent();
            GameObject go = InspectorUtility.CreateGameObject(
                    GenerateUniqueObjectName(typeof(CinemachineMixingCamera), "CM MixingCamera"),
                    typeof(CinemachineMixingCamera));
            if (SceneView.lastActiveSceneView != null)
                go.transform.position = SceneView.lastActiveSceneView.pivot;
            Undo.RegisterCreatedObjectUndo(go, "create MixingCamera camera");
            Selection.activeGameObject = go;

            // Give it a couple of children
            Undo.SetTransformParent(CreateDefaultVirtualCamera().transform, go.transform, "create MixedCamera child");
            Undo.SetTransformParent(CreateDefaultVirtualCamera().transform, go.transform, "create MixingCamera child");
        }

        [MenuItem("Cinemachine/Create 2D Camera", false, 1)]
        private static void Create2DCamera()
        {
            InternalCreateVirtualCamera("CM vcam", true, typeof(CinemachineFramingTransposer));
        }

        [MenuItem("Cinemachine/Import Example Asset Package")]
        private static void ImportExamplePackage()
        {
            string pkgFile = ScriptableObjectUtility.CinemachineInstallPath 
                + "/Extras~/CinemachineExamples.unitypackage";
            if (!File.Exists(pkgFile))
                Debug.LogError("Missing file " + pkgFile);
            else
                AssetDatabase.ImportPackage(pkgFile, true);
        }

        [MenuItem("Cinemachine/Import Post Processing V2 Adapter Asset Package")]
        private static void ImportPostProcessingV2Package()
        {
            string pkgFile = ScriptableObjectUtility.CinemachineInstallPath 
                + "/Extras~/CinemachinePostProcessingV2.unitypackage";
            if (!File.Exists(pkgFile))
                Debug.LogError("Missing file " + pkgFile);
            else
                AssetDatabase.ImportPackage(pkgFile, true);
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
        /// Create a Virtual Camera, with components
        /// </summary>
        static CinemachineVirtualCamera InternalCreateVirtualCamera(
            string name, bool selectIt, params Type[] components)
        {
            // Create a new virtual camera
            CreateCameraBrainIfAbsent();
            GameObject go = InspectorUtility.CreateGameObject(
                    GenerateUniqueObjectName(typeof(CinemachineVirtualCamera), name),
                    typeof(CinemachineVirtualCamera));
            if (SceneView.lastActiveSceneView != null)
                go.transform.position = SceneView.lastActiveSceneView.pivot;
            Undo.RegisterCreatedObjectUndo(go, "create " + name);
            CinemachineVirtualCamera vcam = go.GetComponent<CinemachineVirtualCamera>();
            GameObject componentOwner = vcam.GetComponentOwner().gameObject;
            foreach (Type t in components)
                Undo.AddComponent(componentOwner, t);
            vcam.InvalidateComponentPipeline();
            if (selectIt)
                Selection.activeObject = go;
            return vcam;
        }

        /// <summary>
        /// If there is no CinemachineBrain in the scene, try to create one on the main camera
        /// </summary>
        public static void CreateCameraBrainIfAbsent()
        {
            CinemachineBrain[] brains = UnityEngine.Object.FindObjectsOfType(
                    typeof(CinemachineBrain)) as CinemachineBrain[];
            if (brains == null || brains.Length == 0)
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
                    Undo.AddComponent<CinemachineBrain>(cam.gameObject);
                }
            }
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
