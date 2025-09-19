using UnityEngine;

namespace Unity.Cinemachine.Samples
{
    // Interface for behaviours implementing teleportation
    public interface ITeleportable
    {
        // Teleport the object to a new worldspace location and rotation
        public void Teleport(Vector3 newPos, Quaternion newRot);
    }

    // This class will teleport an object and the main CinemachineCamera tracking it to a target Teleporter object.
    // This class co-operates with the ITeleportables interface which implements the teleportation.
    public class Teleporter : MonoBehaviour
    {
        // The target portal to which the player will be teleported
        public Teleporter TargetPortal;

        // Sets the teleported in receivce-only mode, which prevents received
        // objects from immediately being teloported out
        public bool ReceiveOnlyMode  { get; set; }

        void TeleportToTarget(Transform player)
        {
            if (ReceiveOnlyMode || TargetPortal == null)
                return;

            // Deactivate the target so it can receive player without re-teleporting
            TargetPortal.ReceiveOnlyMode = true;

            var pivot = transform.position;
            var destination = TargetPortal.transform;
            var rotDelta = Quaternion.FromToRotation(transform.forward, destination.forward);
            var posDelta = destination.position - pivot;

            // Teleport the player.  Either set the new pos/rot directly, or if a ITeleportable
            // is present, get it to do the teleportation.
            player.GetPositionAndRotation(out var playerPos, out var playerRot);
            var newPlayerPos = RotateAround(playerPos, pivot, rotDelta) + posDelta;
            var newPlayerRot = rotDelta * playerRot;
            if (player.TryGetComponent(out ITeleportable teleportable))
                teleportable.Teleport(newPlayerPos, newPlayerRot);
            else
                player.SetPositionAndRotation(newPlayerPos, newPlayerRot);

            // Teleport the camera.  This implementation only works for cameras which directly target the player.
            // If your camera target is a child of player, then you will need to do some extra work here
            // to find the appropriate target.
            var cameraTarget = player;

            // Now we iterate all active cameras targeting the player, teleporting each one.
            for (int i = 0; i < CinemachineCore.VirtualCameraCount; ++i)
            {
                var cam = CinemachineCore.GetVirtualCamera(i);
                if (cam.Follow != cameraTarget)
                    continue;

                // Note that we don't use the camera's transform since that doesn't always reflect the
                // actual position and rotation.  Instead, we snapshot the VirtualCamera's State member.
                var state = cam.State;

                // This call will seamlessly teleport the camera based on the player's change of position,
                // but it will not handle a change of rotation
                cam.OnTargetObjectWarped(cameraTarget, newPlayerPos - playerPos);

                // If the entire player-camera combo is also being rotated by the portal, we need to do some
                // additional work to tell the CinemachineCamera to rotate its state and teleport seamlessly.
                if (rotDelta != Quaternion.identity)
                {
                    // Here we grab the camera original position and put it through the same teleportation
                    // as the player, preserving the relationship between camera and player.
                    var camPos = state.GetFinalPosition();
                    var camRot = state.GetFinalOrientation();
                    var newCamPos = RotateAround(camPos, pivot, rotDelta) + posDelta;
                    var newCamRot = rotDelta * camRot;

                    // Now force the CinemachineCamera to be at the desired position and rotation.
                    // This will also position and rotate the internal camera state, so that no damping will occur
                    cam.ForceCameraPosition(newCamPos, newCamRot);
                }
            }

            // This is a helper function to rotate a point around a pivot using a quaternion rotation
            static Vector3 RotateAround(Vector3 p, Vector3 pivot, Quaternion rot) => rot * (p - pivot) + pivot;
        }

        void OnTriggerEnter(Collider other) => TeleportToTarget(other.transform);
        void OnTriggerExit(Collider other) => ReceiveOnlyMode = false;
    }
}
