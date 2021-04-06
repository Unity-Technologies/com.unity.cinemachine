using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class Zoom2D : MonoBehaviour
{
    private CinemachineVirtualCamera vcam;
    public float zoomSpeed;
    void Start()
    {
        vcam = GetComponent<CinemachineVirtualCamera>();
    }
    private void Update()
    {
            float zoom = Input.mouseScrollDelta.y;
            vcam.m_Lens.OrthographicSize = Mathf.Lerp(vcam.m_Lens.OrthographicSize, vcam.m_Lens.OrthographicSize + zoom * zoomSpeed, Time.deltaTime);
        
    }
}
