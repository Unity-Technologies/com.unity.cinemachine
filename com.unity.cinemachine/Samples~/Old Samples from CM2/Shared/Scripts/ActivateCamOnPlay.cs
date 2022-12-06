using UnityEngine;

namespace Cinemachine.OldExamples
{

[AddComponentMenu("")] // Don't display in add component menu
public class ActivateCamOnPlay : MonoBehaviour
{
    public CinemachineVirtualCameraBase vcam;

	// Use this for initialization
	void Start () 
    {
	    if (vcam)
	    {
	        vcam.MoveToTopOfPrioritySubqueue();
	    }
	}
}

}