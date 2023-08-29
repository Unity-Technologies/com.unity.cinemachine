using UnityEngine;

namespace Unity.Cinemachine.Samples
{
    public class MagnetGroupController : MonoBehaviour
    {
        public CinemachineTargetGroup TargetGroup;

        void Update()
        {
            if (TargetGroup == null || TargetGroup.IsEmpty)
                return;

            // We assume that the player is at group index 0
            var targets = TargetGroup.Targets;
            var playerPos = targets[0].Object.position;
            for (int i = 1; i < targets.Count; ++i)
            {
                var t = targets[i];
                if (t.Object != null && t.Object.TryGetComponent<Magnet>(out var magnet))
                {
                    // Reduce the member weight as the distance from player increases
                    var distance = (playerPos - t.Object.position).magnitude;
                    t.Weight = magnet.Strength * Mathf.Max(0, 1 - distance / magnet.Range);
                }
            }
        }
    }
}
