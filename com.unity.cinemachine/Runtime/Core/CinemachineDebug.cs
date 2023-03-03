using UnityEngine;
using System.Collections.Generic;
using System.Text;

namespace Unity.Cinemachine.Utility
{
    /// <summary>Manages onscreen positions for Cinemachine debugging output</summary>
    class CinemachineDebug
    {
        static List<StringBuilder> m_AvailableStringBuilders;

#if CINEMACHINE_UNITY_IMGUI
        /// <summary>Reserve an on-screen rectangle for debugging output.</summary>
        /// <param name="client">The client caller.  This is used as a handle.</param>
        /// <param name="text">Sample text, for determining rectangle size</param>
        /// <param name="style">What style will be used to draw, used here for
        /// determining rect size</param>
        /// <returns>An area on the game screen large enough to print the text
        /// in the style indicated</returns>
        public static Rect GetScreenPos(Camera camera, string text, GUIStyle style)
        {
            var size = style.CalcSize(new GUIContent(text));
            var pos = Vector2.zero;
            if (camera != null)
            {
                var r = camera.rect; 
                pos.x += r.x * Screen.width;
                pos.y += (1 - r.y - r.height) * Screen.height;
            }
            return new Rect(pos, size);
        }
#endif

        /// <summary>
        /// Delegate for OnGUI debugging.  
        /// This will be called by the CinemachineBrain in its OnGUI (editor only)
        /// </summary>
        public delegate void OnGUIDelegate(CinemachineBrain brain);

        /// <summary>
        /// Delegate for OnGUI debugging.  
        /// This will be called by the CinemachineBrain in its OnGUI (editor only)
        /// </summary>
        public static OnGUIDelegate OnGUIHandlers;

        /// <summary>Get a pre-allocated StringBuilder from the pool</summary>
        /// <returns>The pre-allocated StringBuilder from the pool.  
        /// Client must call ReturnToPool when done</returns>
        public static StringBuilder SBFromPool()
        {
            if (m_AvailableStringBuilders == null || m_AvailableStringBuilders.Count == 0)
                return new StringBuilder();
            var sb = m_AvailableStringBuilders[m_AvailableStringBuilders.Count - 1];
            m_AvailableStringBuilders.RemoveAt(m_AvailableStringBuilders.Count - 1);
            sb.Length = 0;
            return sb;
        }

        /// <summary>Return a StringBuilder to the pre-allocated pool</summary>
        /// <param name="sb">The string builder object to return to the pool</param>
        public static void ReturnToPool(StringBuilder sb)
        {
            if (m_AvailableStringBuilders == null)
                m_AvailableStringBuilders = new List<StringBuilder>();
            m_AvailableStringBuilders.Add(sb);
        }
    }
}
