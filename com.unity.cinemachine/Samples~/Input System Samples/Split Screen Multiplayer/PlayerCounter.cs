using UnityEngine;

namespace Unity.Cinemachine.Samples
{
    public class PlayerCounter : MonoBehaviour
    {
        public static int PlayerCount;

        public void PlayerJoined()
        {
            PlayerCount++;
        }

        public void PlayerLeft()
        {
            PlayerCount--;
        }
    }
}
