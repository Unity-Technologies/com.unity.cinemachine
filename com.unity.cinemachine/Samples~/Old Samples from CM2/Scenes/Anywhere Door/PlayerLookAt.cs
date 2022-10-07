using UnityEngine;

namespace Cinemachine.Examples
{
	public class PlayerLookAt : MonoBehaviour
	{
		public float speed = 5f;

		void Update()
		{
#if ENABLE_LEGACY_INPUT_MANAGER
			float horizontal = Input.GetAxis("Mouse X") * speed;
			float vertical = Input.GetAxis("Mouse Y") * speed;

			transform.Rotate(0f, horizontal, 0f, Space.World);
			transform.Rotate(-vertical, 0f, 0f, Space.Self);
#else
		InputSystemHelper.EnableBackendsWarningMessage();
#endif
		}
	}
}