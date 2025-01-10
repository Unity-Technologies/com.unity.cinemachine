using UnityEngine;

namespace Unity.Cinemachine.Samples
{
    /// <summary>
    /// This class inherits CinemachineCameraManagerBase, which is a convenient base class for
    /// making complex cameras by transitioning between a number of worker cameras, depending
    /// on some arbitrary game state.
    /// 
    /// In this case, we monitor the player's facing direction and motion, and select a camera
    /// with the appropriate settings.  CinemachineCameraManagerBase takes care of handling the blends.
    /// </summary>
    [ExecuteAlways]
    public class PlatformerCamera2D : CinemachineCameraManagerBase
    {
        public enum PlayerState
        {
            Right,
            Left,
            FallingRight,
            FallingLeft
        }

        [Space]
        public float FallingSpeedThreshold = 0.1f;

        // The cameras in these fields must be GameObject children of the manager camera.
        [Header("State Cameras")]
        [ChildCameraProperty] public CinemachineVirtualCameraBase RightCamera;
        [ChildCameraProperty] public CinemachineVirtualCameraBase LeftCamera;
        [ChildCameraProperty] public CinemachineVirtualCameraBase FallingRightCamera;
        [ChildCameraProperty] public CinemachineVirtualCameraBase FallingLeftCamera;

        Rigidbody2D m_Player;
        SimplePlayerAnimator m_PlayerAnimator;

        protected override void OnEnable()
        {
            base.OnEnable();
            var target = DefaultTarget.Enabled ? DefaultTarget.Target.TrackingTarget : null;
            if (target != null)
            {
                target.TryGetComponent(out m_Player);
                m_PlayerAnimator = target.GetComponentInChildren<SimplePlayerAnimator>();
            }
            if (m_Player == null)
                Debug.LogError("PlatformerCamera2D: Default target must be set to Player with a Rigidbody2D");
        }
   
        PlayerState GetPlayerState()
        {
            bool isLeft = false;
            bool isFalling = false;
            if (m_Player != null)
            {
                if (m_PlayerAnimator != null)
                    isLeft = Mathf.Abs(m_PlayerAnimator.transform.rotation.eulerAngles.y) > 90;
#if UNITY_6000_1_OR_NEWER
                isFalling = m_Player.linearVelocity.y < -FallingSpeedThreshold;
#else
                #pragma warning disable CS0618 // obsolete for 6000.0.0f11 and newer
                isFalling = m_Player.velocity.y < -FallingSpeedThreshold;
                #pragma warning restore CS0618
#endif
            }
            if (isFalling)
                return isLeft ? PlayerState.FallingLeft : PlayerState.FallingRight;
            return isLeft ? PlayerState.Left : PlayerState.Right;
        }

        /// <summary>
        /// Choose the appropriate child camera depending on player state.
        /// </summary>
        protected override CinemachineVirtualCameraBase ChooseCurrentCamera(Vector3 worldUp, float deltaTime)
        {
            return GetPlayerState() switch
            {
                PlayerState.Left => LeftCamera,
                PlayerState.FallingRight => FallingRightCamera,
                PlayerState.FallingLeft => FallingLeftCamera,
                _ => RightCamera,
            };
        }
    }
}
