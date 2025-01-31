using UnityEngine;
using System.Collections.Generic;

namespace Unity.Cinemachine.Samples
{
    /// <summary>
    /// This is a custom camera manager that selects between an aiming camera child and a
    /// non-aiming camera child, depending on the value of some user input.
    ///
    /// The Aiming child is expected to have ThirdPersonFollow and ThirdPersonAim components,
    /// and to have a player as its Follow target.  The player is expected to have a
    /// SimplePlayerAimController behaviour on one of its children, to decouple aiminag and
    /// player rotation.
    /// </summary>
    [ExecuteAlways]
    public class AimCameraRig : CinemachineCameraManagerBase, Unity.Cinemachine.IInputAxisOwner
    {
        public InputAxis AimMode = InputAxis.DefaultMomentary;

        SimplePlayerAimController AimController;
        CinemachineVirtualCameraBase AimCamera;
        CinemachineVirtualCameraBase FreeCamera;

        bool IsAiming => AimMode.Value > 0.5f;

        /// Report the available input axes to the input axis controller.
        /// We use the Input Axis Controller because it works with both the Input package
        /// and the Legacy input system.  This is sample code and we
        /// want it to work everywhere.
        void IInputAxisOwner.GetInputAxes(List<IInputAxisOwner.AxisDescriptor> axes)
        {
            axes.Add(new () { DrivenAxis = () => ref AimMode, Name = "Aim" });
        }

        protected override void Start()
        {
            base.Start();

            // Find the player and the aiming camera.
            // We expect to have one camera with a CinemachineThirdPersonAim component
            // whose Follow target is a player with a SimplePlayerAimController child.
            for (int i = 0; i < ChildCameras.Count; ++i)
            {
                var cam = ChildCameras[i];
                if (!cam.isActiveAndEnabled)
                    continue;
                if (AimCamera == null
                    && cam.TryGetComponent<CinemachineThirdPersonAim>(out var aim)
                    && aim.NoiseCancellation)
                {
                    AimCamera = cam;
                    var player = AimCamera.Follow;
                    if (player != null)
                        AimController = player.GetComponentInChildren<SimplePlayerAimController>();
                }
                else if (FreeCamera == null)
                    FreeCamera = cam;
            }
            if (AimCamera == null)
                Debug.LogError("AimCameraRig: no valid CinemachineThirdPersonAim camera found among children");
            if (AimController == null)
                Debug.LogError("AimCameraRig: no valid SimplePlayerAimController target found");
            if (FreeCamera == null)
                Debug.LogError("AimCameraRig: no valid non-aiming camera found among children");
        }

        protected override CinemachineVirtualCameraBase ChooseCurrentCamera(Vector3 worldUp, float deltaTime)
        {
            var oldCam = (CinemachineVirtualCameraBase)LiveChild;
            var newCam = IsAiming ? AimCamera : FreeCamera;
            if (AimController != null && oldCam != newCam)
            {
                // Set the mode of the player aim controller.
                // We want the player rotation to be copuled to the camera when aiming, otherwise not.
                AimController.PlayerRotation = IsAiming
                    ? SimplePlayerAimController.CouplingMode.Coupled
                    : SimplePlayerAimController.CouplingMode.Decoupled;
                AimController.RecenterPlayer();
            }
            return newCam;
        }
    }
}
