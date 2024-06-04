using UnityEngine;

namespace Unity.Cinemachine.Samples
{
    /// <summary>This behaviour works in conjunction with PlayerOnSurface to keep the player
    /// parented to the surface it's standing on.  This is useful to prevent sliding
    /// when the surface is in motion</summary>
    [RequireComponent(typeof(SimplePlayerOnSurface))]
    public class ReparentPlayerToSurface : MonoBehaviour
    {
        SimplePlayerOnSurface m_PlayerOnSurface;

        void OnEnable()
        {
            if (TryGetComponent(out m_PlayerOnSurface))
                m_PlayerOnSurface.SurfaceChanged.AddListener(OnSurfaceChanged);
        }

        void OnDisable()
        {
            if (m_PlayerOnSurface != null)
                m_PlayerOnSurface.SurfaceChanged.RemoveListener(OnSurfaceChanged);
        }

        // newSurface may be null, in which case we should just uparent the player
        void OnSurfaceChanged(Collider newSurface)
        {
            transform.SetParent(newSurface == null ? null : newSurface.transform, true);
        }
    }
}
