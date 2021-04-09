using System;
using UnityEngine;

namespace Cinemachine.Examples
{
    [ExecuteInEditMode]
    public class MouseScrollZoom2D : MonoBehaviour
    {
        [Range(0, 10)]
        public float ZoomMultiplier = 1f;
        [Range(0, 100)]
        public float MinZoom = 1f;
        [Range(0, 100)]
        public float MaxZoom = 50f;

        void Update()
        {
            float zoom = Input.mouseScrollDelta.y;
            m_VirtualCamera.m_Lens.OrthographicSize += zoom * ZoomMultiplier;
            m_VirtualCamera.m_Lens.OrthographicSize = 
                Mathf.Min(MaxZoom, Mathf.Max(MinZoom, m_VirtualCamera.m_Lens.OrthographicSize));
        }

        CinemachineVirtualCamera m_VirtualCamera;
        float m_OrthographicSizeInInspector;
        void Awake()
        {
            m_VirtualCamera = GetComponent<CinemachineVirtualCamera>();
            m_OrthographicSizeInInspector = m_VirtualCamera.m_Lens.OrthographicSize;
#if UNITY_EDITOR
            SaveDuringPlay.SaveDuringPlay.OnHotSave -= RestoreOriginalOrthographicSize;
            SaveDuringPlay.SaveDuringPlay.OnHotSave += RestoreOriginalOrthographicSize;
#endif
        }
#if UNITY_EDITOR
        void OnDestroy()
        {
            SaveDuringPlay.SaveDuringPlay.OnHotSave -= RestoreOriginalOrthographicSize;
        }
        
        void RestoreOriginalOrthographicSize()
        {
            m_VirtualCamera.m_Lens.OrthographicSize = m_OrthographicSizeInInspector;
        }
#endif
        void OnValidate()
        {
            if (MaxZoom < MinZoom)
            {
                MaxZoom = MinZoom;
            }
        }
    }
}
