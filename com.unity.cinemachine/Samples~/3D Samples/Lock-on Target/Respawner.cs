using System;
using Cinemachine;
using UnityEngine;

public class Respawner : MonoBehaviour
{
    public Vector3 Position;
    public Vector3 Rotation;

    public void Respawn()
    {
        var positionDelta = Position - transform.position;
        var quat = Quaternion.Euler(Rotation);
        transform.SetLocalPositionAndRotation(Position, quat);

        if (TryGetComponent(out Rigidbody rigidbody))
        {
            rigidbody.velocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
        }
        CinemachineCore.Instance.GetActiveBrain(0).ActiveVirtualCamera.OnTargetObjectWarped(transform, positionDelta);
    }
}
