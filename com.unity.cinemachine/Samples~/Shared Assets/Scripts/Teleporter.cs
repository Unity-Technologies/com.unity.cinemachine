using UnityEngine;

namespace Unity.Cinemachine.Samples
{
    //  Interface for behaviours implementing teleportation
    public interface ITeleportable
    {
        // Teleport the object to a worldspace new location and rotation
        public void Teleport(Vector3 newPos, Quaternion newRot);
    }

    // This class will teleport an object and the main CinemachineCamera tracking it to a target Teleporter object.
    // This class co-operates with ITeleportables which implement the teleportation.
    public class Teleporter : MonoBehaviour
    {
        public Teleporter TargetPortal;

        public bool Deactivated  { get; set; }

        void TeleportToTarget(Transform player)
        {
            if (Deactivated || TargetPortal == null)
                return;

            // Deactivate the target so it can receive player without re-teleporting
            TargetPortal.Deactivated = true;

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

            // Teleport the camera.
            // This call will seamlessly teleport the camera based on the player's change of position,
            // but it will not handle a change of rotation.  All cameras targeting player will be teleported
            // along with the player.
            CinemachineCore.OnTargetObjectWarped(player, newPlayerPos - playerPos);

            // Because the player-camera combo is also being rotated by the portal, we need to do some
            // additional work to tell the CinemachineCamera to rotate its state and teleport seamlessly
            if (CinemachineBrain.GetActiveBrain(0).ActiveVirtualCamera is CinemachineVirtualCameraBase cam
                && cam.Follow == player)
            {
                // Here we grab the actual camera position and put it through the same teleportation
                // as the player, preserving the relationship between camera and player
                Camera.main.transform.GetPositionAndRotation(out var camPos, out var camRot);
                var newCamPos = RotateAround(camPos, pivot, rotDelta) + posDelta;

                // Now force the CinemachineCamera to be at the desired position and rotation.
                // This will also position and rotate the internal camera state, so that no damping will occur
                cam.ForceCameraPosition(newCamPos, rotDelta * camRot);
            }

            // This is a helper function to rotate a point around a pivot using a quaternion rotation
            static Vector3 RotateAround(Vector3 p, Vector3 pivot, Quaternion rot) => rot * (p - pivot) + pivot;
        }

        void OnTriggerEnter(Collider other) => TeleportToTarget(other.transform);
        void OnTriggerExit(Collider other) => Deactivated = false;
    }
}
