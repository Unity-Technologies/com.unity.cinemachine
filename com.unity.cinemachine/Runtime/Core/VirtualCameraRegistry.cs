using System.Collections.Generic;
using UnityEngine.Assertions;

namespace Unity.Cinemachine
{
    /// <summary>Keeps track of CinemachineCameras.  Sorts them by priority, and 
    /// by nesting level, so cameras can get updated leafmost-first.</summary>
    sealed class VirtualCameraRegistry
    {
        /// <summary>List of all active ICinemachineCameras.</summary>
        readonly List<CinemachineVirtualCameraBase> m_ActiveCameras = new ();

        // Registry of all vcams that are present, active or not
        readonly List<List<CinemachineVirtualCameraBase>> m_AllCameras = new();

        bool m_ActiveCamerasAreSorted;
        int m_ActivationSequence;

        /// <summary>
        /// All cameras (active or not) sorted by nesting level.  This is to enable
        /// processing in leaf-first order.  Level 0 is root.
        /// </summary>
        public List<List<CinemachineVirtualCameraBase>> AllCamerasSortedByNestingLevel => m_AllCameras;

        /// <summary>
        /// List of all active CinemachineCameras for all brains.
        /// This list is kept sorted by priority.
        /// </summary>
        public int ActiveCameraCount => m_ActiveCameras.Count;

        /// <summary>Access the priority-sorted array of active ICinemachineCamera in the scene
        /// without generating garbage</summary>
        /// <param name="index">Index of the camera to access, range 0-VirtualCameraCount</param>
        /// <returns>The virtual camera at the specified index</returns>
        public CinemachineVirtualCameraBase GetActiveCamera(int index)
        {
            if (!m_ActiveCamerasAreSorted && m_ActiveCameras.Count > 1)
            {
                m_ActiveCameras.Sort((x, y) => 
                    x.Priority.Value == y.Priority.Value 
                        ? y.ActivationId.CompareTo(x.ActivationId) 
                        : y.Priority.Value.CompareTo(x.Priority.Value));
                m_ActiveCamerasAreSorted = true;
            }
            return m_ActiveCameras[index];
        }

        /// <summary>Called when a CinemachineCamera is enabled.</summary>
        public void AddActiveCamera(CinemachineVirtualCameraBase vcam)
        {
            Assert.IsFalse(m_ActiveCameras.Contains(vcam));
            vcam.ActivationId = m_ActivationSequence++;
            m_ActiveCameras.Add(vcam);
            m_ActiveCamerasAreSorted = false;
        }

        /// <summary>Called when a CinemachineCamera is disabled.</summary>
        public void RemoveActiveCamera(CinemachineVirtualCameraBase vcam)
        {
            if (m_ActiveCameras.Contains(vcam))
                m_ActiveCameras.Remove(vcam);
        }

        /// <summary>Called when a CinemachineCamera is destroyed.</summary>
        public void CameraDestroyed(CinemachineVirtualCameraBase vcam)
        {
            if (m_ActiveCameras.Contains(vcam))
                m_ActiveCameras.Remove(vcam);
        }

        /// <summary>Called when a vcam is enabled.</summary>
        public void CameraEnabled(CinemachineVirtualCameraBase vcam)
        {
            int parentLevel = 0;
            for (var p = vcam.ParentCamera; p != null; p = p.ParentCamera)
                ++parentLevel;
            while (m_AllCameras.Count <= parentLevel)
                m_AllCameras.Add(new List<CinemachineVirtualCameraBase>());
            m_AllCameras[parentLevel].Add(vcam);
        }

        /// <summary>Called when a vcam is disabled.</summary>
        public void CameraDisabled(CinemachineVirtualCameraBase vcam)
        {
            for (int i = 0; i < m_AllCameras.Count; ++i)
                m_AllCameras[i].Remove(vcam);
        }
    }
}
