using UnityEngine;

namespace Unity.Cinemachine.Samples
{
    public class TagChanger : MonoBehaviour
    {
        public void PlayerTagChanger()
        {
            this.tag = "Player";
        }

        public void UntaggedTagChanger()
        {
            this.tag = "Untagged";
        }
    }
}