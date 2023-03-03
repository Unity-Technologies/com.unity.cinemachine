using UnityEngine;

namespace Unity.Cinemachine.Examples
{
    public class TurnAroundPlayer : MonoBehaviour
    {
        public GameObject Player;
        SimplePlayerController m_PlayerController;
    
        void Start()
        {
            m_PlayerController = Player.GetComponent<SimplePlayerController>();
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(Player.tag)) 
                m_PlayerController.MoveZ.Value *= -1f; // flip direction
        }
    }
}
