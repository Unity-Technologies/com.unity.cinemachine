using Cinemachine;
using UnityEngine;

/// <summary>
/// This is an add-on behaviour that globally maps the touch control 
/// to standard input channels, such as mouse X and mouse Y.
/// Drop it on any game object in your scene.
/// </summary>
public class CinemachineTouchInputMapper : MonoBehaviour
{
    public float TouchSensitivityX = 10f;
    public float TouchSensitivityY = 10f;

    public string TouchXInputMapTo = "Mouse X";
    public string TouchYInputMapTo = "Mouse Y";

    void Start()
    {
        CinemachineCore.GetInputAxis = GetInputAxis;
    }

    private float GetInputAxis(string axisName)
    {
        if (Input.touchCount > 0)
        {
            if (axisName == TouchXInputMapTo)
                return Input.touches[0].deltaPosition.x / TouchSensitivityX;
            if (axisName == TouchYInputMapTo)
                return Input.touches[0].deltaPosition.y / TouchSensitivityY;
        }
        return Input.GetAxis(axisName);
    }
}
