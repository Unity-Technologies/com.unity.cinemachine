using UnityEngine;

namespace Cinemachine.Examples
{
	[AddComponentMenu("")] // Don't display in add component menu
	public class ActivateCamOnPlay : MonoBehaviour
	{
		public CinemachineVirtualCameraBase vcam;

		void Start() 
		{
			if (vcam != null)
			{
				vcam.Prioritize();
			}
		}
	}
}