using System;
using UnityEngine;

namespace Cinemachine.Examples
{
    [RequireComponent(typeof(CmCamera))]
    [SaveDuringPlay] // Enable SaveDuringPlay for this class
    public class MouseScrollZoom2D : MonoBehaviour
    {
        [Range(0, 10)]
        public float ZoomMultiplier = 1f;
        [Range(0, 100)]
        public float MinZoom = 1f;
        [Range(0, 100)]
        public float MaxZoom = 50f;

        CmCamera m_CmCamera;
        float m_OriginalOrthoSize, m_OriginalFieldOfView;

        void Awake()
        {
            m_CmCamera = GetComponent<CmCamera>();
            m_OriginalOrthoSize = m_CmCamera.Lens.OrthographicSize;
            m_OriginalFieldOfView = m_CmCamera.Lens.FieldOfView;

#if UNITY_EDITOR
            // This code shows how to play nicely with the CmCamera's SaveDuringPlay functionality
            SaveDuringPlay.SaveDuringPlay.OnHotSave -= RestoreLens;
            SaveDuringPlay.SaveDuringPlay.OnHotSave += RestoreLens;
#endif
        }

#if UNITY_EDITOR
        void OnDestroy()
        {
            SaveDuringPlay.SaveDuringPlay.OnHotSave -= RestoreLens;
        }
        
        void RestoreLens()
        {
            m_CmCamera.Lens.OrthographicSize = m_OriginalOrthoSize;
            m_CmCamera.Lens.FieldOfView = m_OriginalFieldOfView;
        }
#endif

        void OnValidate()
        {
            MaxZoom = Mathf.Max(MinZoom, MaxZoom);
        }

        void Update()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (m_CmCamera.Lens.Orthographic)
            {
                var zoom = m_CmCamera.Lens.OrthographicSize + Input.mouseScrollDelta.y * ZoomMultiplier;
                m_CmCamera.Lens.OrthographicSize = Mathf.Clamp(zoom, MinZoom, MaxZoom);
            }
            else
            {
                var zoom = m_CmCamera.Lens.FieldOfView + Input.mouseScrollDelta.y * ZoomMultiplier;
                m_CmCamera.Lens.FieldOfView = Mathf.Clamp(zoom, MinZoom, MaxZoom);
            }
#else
            InputSystemHelper.EnableBackendsWarningMessage();
#endif
        }
    }
}
